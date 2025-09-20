using Akka.Actor;
using Akka.Event;
using Memorizer.Services;
using System.Text;
using System.Linq;

namespace Memorizer.Actors;

/// <summary>
/// Main chatbot actor that handles user sessions and coordinates with other actors
/// </summary>
public class ChatBotActor : ReceiveActor, IWithTimers
{
    private readonly string _sessionId;
    private readonly IActorRef _searchMemoryActor;
    private readonly IActorRef _decisionActor;
    private readonly ILlmService _llmService;
    private readonly ILoggingAdapter _logger;

    // Timer for session timeout
    private const string SessionTimerKey = "session-timeout";
    private static readonly TimeSpan SessionTimeout = TimeSpan.FromDays(3);

    // Current conversation context
    private readonly List<string> _conversationHistory = new();
    private readonly List<StreamingUpdate> _reasoningSteps = new();

    // Session-based conversation management
    protected readonly List<ConversationEntry> _conversationEntries = new();
    protected string _shortTermMemory = string.Empty;
    protected string _lastImportantResponse = string.Empty; // Store last important response separately
    private const int MaxConversationEntries = 10;
    private const int MaxShortTermMemoryLength = 500;
    private const int MaxLastResponseLength = 300;

    // Prompt for general responses with context
    private const string GeneralResponsePrompt = @"
You are a helpful AI assistant named ASKBot. You are having a conversation with a user.

{1}

Current User Query: {0}

Provide a helpful and concise response that takes the conversation context into account. Be conversational and maintain continuity with previous exchanges.";

    // Prompt for memory-based responses with context
    private const string MemoryBasedResponsePrompt = @"
You are a helpful AI assistant named ASKBot with access to stored memories. You are having a conversation with a user.

{2}

Current User Query: {0}

Relevant Information from Memory:
{1}

Based on the conversation context and the relevant information, provide a comprehensive and accurate answer to the user's query.
Maintain conversation continuity and reference previous context when appropriate.";

    public ITimerScheduler Timers { get; set; } = null!;

    public ChatBotActor(
        string sessionId,
        IActorRef searchMemoryActor,
        IActorRef decisionActor,
        ILlmService llmService)
    {
        _sessionId = sessionId;
        _searchMemoryActor = searchMemoryActor;
        _decisionActor = decisionActor;
        _llmService = llmService;
        _logger = Context.GetLogger();

        // Set up message handlers
        Receive<UserChatRequest>(HandleUserChatRequest);
        Receive<SearchMemoryResponse>(HandleSearchMemoryResponse);
        Receive<EvaluateRelevanceResponse>(HandleEvaluateRelevanceResponse);
        Receive<SessionTimeout>(HandleSessionTimeout);
        Receive<ResetSessionTimer>(HandleResetSessionTimer);
        Receive<ChatBotResponse>(HandleChatBotResponseFromPipeTo);
        Receive<Status.Failure>(HandlePipeToFailure);

        // Start session timer
        ResetSessionTimer();

        _logger.Info("ChatBotActor created for session {0}", _sessionId);
    }

    private void HandleUserChatRequest(UserChatRequest request)
    {
        _logger.Info("Processing chat request for session {0}: {1}", request.SessionId, request.Message);

        // Reset session timer on activity
        ResetSessionTimer();

        // Store the original sender for response
        var originalSender = Sender;

        // Store the current user message for later use
        var currentUserMessage = request.Message;

        // Add to conversation history
        _conversationHistory.Add($"User: {request.Message}");

        // Clear previous reasoning steps
        _reasoningSteps.Clear();

        // Add reasoning step
        AddReasoningStep("Analyzing user query...");

        try
        {
            // Send search request to SearchMemoryActor
            var searchRequest = new SearchMemoryRequest
            {
                Query = request.Message,
                SessionId = request.SessionId,
                MaxResults = 5,
                MinSimilarity = 0.3
            };

            AddReasoningStep("Searching for relevant memories...");

            // Store context for continuation
            Context.Become(WaitingForSearchResponse(request, originalSender));

            _searchMemoryActor.Tell(searchRequest, Self);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error processing chat request for session {0}", request.SessionId);
            SendErrorResponse(originalSender, request.SessionId, "An error occurred processing your request.");
        }
    }

    private Receive WaitingForSearchResponse(UserChatRequest originalRequest, IActorRef originalSender)
    {
        return message =>
        {
            if (message is SearchMemoryResponse searchResponse)
            {
                HandleSearchMemoryResponseContinuation(searchResponse, originalRequest, originalSender);
                // Don't call SetupBaseHandlers here - HandleSearchMemoryResponseContinuation handles state transitions
                return true;
            }
            // Handle other message types while waiting
            else if (message is SessionTimeout timeout)
            {
                HandleSessionTimeout(timeout);
                return true;
            }
            else if (message is ResetSessionTimer reset)
            {
                HandleResetSessionTimer(reset);
                return true;
            }
            return false;
        };
    }

    private void SetupBaseHandlers()
    {
        Context.Become(message =>
        {
            switch (message)
            {
                case UserChatRequest req:
                    HandleUserChatRequest(req);
                    return true;
                case SearchMemoryResponse resp:
                    HandleSearchMemoryResponse(resp);
                    return true;
                case EvaluateRelevanceResponse eval:
                    HandleEvaluateRelevanceResponse(eval);
                    return true;
                case ChatBotResponse response:
                    HandleChatBotResponseFromPipeTo(response);
                    return true;
                case Status.Failure failure:
                    HandlePipeToFailure(failure);
                    return true;
                case SessionTimeout timeout:
                    HandleSessionTimeout(timeout);
                    return true;
                case ResetSessionTimer reset:
                    HandleResetSessionTimer(reset);
                    return true;
                default:
                    return false;
            }
        });
    }

    private void HandleSearchMemoryResponseContinuation(
        SearchMemoryResponse searchResponse,
        UserChatRequest originalRequest,
        IActorRef originalSender)
    {
        if (!searchResponse.SearchPerformed)
        {
            AddReasoningStep("No memory search required for this query.");
            // Generate general response immediately
            GenerateGeneralResponse(originalRequest, originalSender);
            // Return to base handlers since we're not waiting for evaluation
            SetupBaseHandlers();
        }
        else if (searchResponse.Memories.Count == 0)
        {
            AddReasoningStep($"No relevant memories found after {searchResponse.RetryAttempts} attempts.");
            // Generate general response
            GenerateGeneralResponse(originalRequest, originalSender);
            // Return to base handlers since we're not waiting for evaluation
            SetupBaseHandlers();
        }
        else
        {
            AddReasoningStep($"Found {searchResponse.Memories.Count} potential memories.");
            AddReasoningStep("Evaluating relevance of search results...");

            // Evaluate relevance of results
            var evaluateRequest = new EvaluateRelevanceRequest
            {
                SessionId = originalRequest.SessionId,
                Query = originalRequest.Message,
                Memories = searchResponse.Memories
            };

            Context.Become(WaitingForEvaluationResponse(originalRequest, originalSender));
            _decisionActor.Tell(evaluateRequest, Self);
        }
    }

    private Receive WaitingForEvaluationResponse(UserChatRequest originalRequest, IActorRef originalSender)
    {
        return message =>
        {
            if (message is EvaluateRelevanceResponse evalResponse)
            {
                HandleEvaluateRelevanceResponseContinuation(evalResponse, originalRequest, originalSender);
                // Don't call SetupBaseHandlers here - responses will handle state
                return true;
            }
            // Handle other message types while waiting
            else if (message is SessionTimeout timeout)
            {
                HandleSessionTimeout(timeout);
                return true;
            }
            else if (message is ResetSessionTimer reset)
            {
                HandleResetSessionTimer(reset);
                return true;
            }
            return false;
        };
    }

    private void HandleEvaluateRelevanceResponseContinuation(
        EvaluateRelevanceResponse evalResponse,
        UserChatRequest originalRequest,
        IActorRef originalSender)
    {
        if (evalResponse.HasRelevantMemories && evalResponse.RelevantMemories != null)
        {
            AddReasoningStep($"Found {evalResponse.RelevantMemories.Count} relevant memories.");
            AddReasoningStep($"Decision reasoning: {evalResponse.Reasoning}");
            // Generate memory-based response
            GenerateMemoryBasedResponse(originalRequest, evalResponse.RelevantMemories, originalSender);
        }
        else
        {
            AddReasoningStep("No relevant memories found for this query.");
            AddReasoningStep($"Decision reasoning: {evalResponse.Reasoning}");
            // Generate general response
            GenerateGeneralResponse(originalRequest, originalSender);
        }
        // Return to base handlers after starting response generation
        SetupBaseHandlers();
    }

    private void HandleSearchMemoryResponse(SearchMemoryResponse response)
    {
        // This handler is for unexpected responses
        _logger.Warning("Received unexpected SearchMemoryResponse for session {0}", response.SessionId);
    }

    private void HandleEvaluateRelevanceResponse(EvaluateRelevanceResponse response)
    {
        // This handler is for unexpected responses
        _logger.Warning("Received unexpected EvaluateRelevanceResponse for session {0}", response.SessionId);
    }

    private void GenerateMemoryBasedResponse(
        UserChatRequest request,
        List<Models.Memory> relevantMemories,
        IActorRef originalSender)
    {
        AddReasoningStep("Generating response based on relevant memories...");

        // Use async operation with manual result handling
        var self = Self;
        Task.Run(async () =>
        {
            try
            {
                var response = await GenerateMemoryBasedResponseAsync(request, relevantMemories, originalSender);
                self.Tell(response);
            }
            catch (Exception ex)
            {
                self.Tell(new Status.Failure(ex));
            }
        });
    }

    private async Task<ChatBotResponse> GenerateMemoryBasedResponseAsync(
        UserChatRequest request,
        List<Models.Memory> relevantMemories,
        IActorRef originalSender)
    {
        try
        {

            // Try with different memory counts if token overflow occurs
            int[] memoryCounts = { 3, 1 };
            string? llmResponse = null;
            List<Guid> usedMemoryIds = new List<Guid>();
            Exception? lastException = null;

            foreach (var count in memoryCounts)
            {
                try
                {
                    var memoriesToUse = relevantMemories.Take(count).ToList();
                    var memoriesText = FormatMemoriesForResponseWithLimit(memoriesToUse, count);
                    var conversationContext = GenerateConversationContext();
                    var prompt = string.Format(MemoryBasedResponsePrompt, request.Message, memoriesText, conversationContext);

                    // Generate response using LLM
                    llmResponse = await _llmService.CompleteAsync(prompt);
                    usedMemoryIds = memoriesToUse.Select(m => m.Id).ToList();

                    AddReasoningStep($"Successfully generated response using {count} memory/memories.");
                    break; // Success, exit the loop
                }
                catch (Exception ex) when (ex.Message.Contains("token") || ex.Message.Contains("context length"))
                {
                    lastException = ex;
                    _logger.Warning("Token overflow with {0} memories, retrying with fewer...", count);
                    AddReasoningStep($"Token limit exceeded with {count} memories, reducing memory count...");
                    continue; // Try with fewer memories
                }
                catch (Exception)
                {
                    // Other types of errors, don't retry
                    throw;
                }
            }

            // If all memory attempts failed, fall back to general LLM
            if (llmResponse == null)
            {
                _logger.Warning("All memory-based attempts failed, falling back to general LLM for session {0}", request.SessionId);
                AddReasoningStep("Memory-based generation failed due to token limits, using general AI response instead...");

                // Return a general response instead
                return await GenerateGeneralResponseAsync(request);
            }

            // Add to conversation history
            _conversationHistory.Add($"Assistant (memory-based): {llmResponse}");

            // Update conversation entries
            await UpdateConversationEntries(request.Message, llmResponse, true);

            // Create and return response
            var response = new ChatBotResponse
            {
                SessionId = request.SessionId,
                Message = llmResponse,
                Type = ResponseType.MemoryBased,
                ReferencedMemoryIds = usedMemoryIds,
                ReasoningSteps = _reasoningSteps.Select(r => r.Content).ToList()
            };

            _logger.Info("Generated memory-based response for session {0} using {1} memories", request.SessionId, usedMemoryIds.Count);
            return response;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error generating memory-based response for session {0}", request.SessionId);
            // Throw to let PipeTo handle the error
            throw;
        }
    }

    private void GenerateGeneralResponse(UserChatRequest request, IActorRef originalSender)
    {
        AddReasoningStep("Generating general AI response...");

        // Capture self reference before async operation
        var self = Self;

        // Use Task.Run to avoid blocking actor thread
        Task.Run(async () =>
        {
            try
            {
                var response = await GenerateGeneralResponseAsync(request);
                _logger.Info("About to send ChatBotResponse to self for session {0}, Type: {1}", response.SessionId, response.Type);
                self.Tell(response);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to generate general response for session {0}", request.SessionId);
                self.Tell(new Status.Failure(ex));
            }
        });
    }

    private async Task<ChatBotResponse> GenerateGeneralResponseAsync(UserChatRequest request)
    {
        try
        {
            var conversationContext = GenerateConversationContext();
            var prompt = string.Format(GeneralResponsePrompt, request.Message, conversationContext);

            // Generate response using LLM
            var llmResponse = await _llmService.CompleteAsync(prompt);

            // Add to conversation history
            _conversationHistory.Add($"Assistant (general): {llmResponse}");

            // Update conversation entries
            await UpdateConversationEntries(request.Message, llmResponse, false);

            // Create and return response
            var response = new ChatBotResponse
            {
                SessionId = request.SessionId,
                Message = llmResponse,
                Type = ResponseType.General,
                ReasoningSteps = _reasoningSteps.Select(r => r.Content).ToList()
            };

            _logger.Info("Generated general response for session {0}", request.SessionId);
            return response;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error generating general response for session {0}", request.SessionId);
            throw;
        }
    }

    private string FormatMemoriesForResponse(List<Models.Memory> memories)
    {
        var sb = new StringBuilder();
        const int maxContentLength = 500; // Limit content per memory to prevent token overflow
        const int maxMemories = 3; // Limit to top 3 most relevant memories

        var topMemories = memories.Take(maxMemories).ToList();

        for (int i = 0; i < topMemories.Count; i++)
        {
            var memory = topMemories[i];
            sb.AppendLine($"--- Memory {i + 1} ---");
            if (!string.IsNullOrWhiteSpace(memory.Title))
            {
                sb.AppendLine($"Title: {memory.Title}");
            }
            sb.AppendLine($"Type: {memory.Type}");

            // Truncate content if too long
            var content = memory.Text ?? string.Empty;
            if (content.Length > maxContentLength)
            {
                content = content.Substring(0, maxContentLength) + "... [truncated]";
            }
            sb.AppendLine($"Content: {content}");

            if (memory.Tags != null && memory.Tags.Length > 0)
            {
                sb.AppendLine($"Tags: {string.Join(", ", memory.Tags)}");
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private string FormatMemoriesForResponseWithLimit(List<Models.Memory> memories, int memoryCount)
    {
        var sb = new StringBuilder();

        // Adjust content length based on memory count
        int maxContentLength = memoryCount switch
        {
            1 => 1500,  // Allow more content for single memory
            3 => 500,   // Restrict content for multiple memories
            _ => 500
        };

        for (int i = 0; i < memories.Count && i < memoryCount; i++)
        {
            var memory = memories[i];
            sb.AppendLine($"--- Memory {i + 1} ---");
            if (!string.IsNullOrWhiteSpace(memory.Title))
            {
                sb.AppendLine($"Title: {memory.Title}");
            }
            sb.AppendLine($"Type: {memory.Type}");

            // Truncate content if too long
            var content = memory.Text ?? string.Empty;
            if (content.Length > maxContentLength)
            {
                content = content.Substring(0, maxContentLength) + "... [truncated]";
            }
            sb.AppendLine($"Content: {content}");

            if (memory.Tags != null && memory.Tags.Length > 0)
            {
                sb.AppendLine($"Tags: {string.Join(", ", memory.Tags)}");
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private void SendErrorResponse(IActorRef sender, string sessionId, string errorMessage, IActorRef? self = null)
    {
        var response = new ChatBotResponse
        {
            SessionId = sessionId,
            Message = errorMessage,
            Type = ResponseType.Error,
            ReasoningSteps = _reasoningSteps.Select(r => r.Content).ToList()
        };

        // Use provided self or try to get from context (may be null in async context)
        var selfRef = self ?? Self;

        if (selfRef != null)
        {
            // Send to self so StreamingChatBotActor can intercept and forward through SSE
            selfRef.Tell(response);
            // Also notify original sender if it's not self (for backward compatibility)
            if (!sender.Equals(selfRef))
            {
                sender.Tell(response);
            }
        }
        else
        {
            // Fallback: just send to original sender if self is not available
            sender.Tell(response);
        }
    }

    protected virtual void AddReasoningStep(string step)
    {
        var update = new StreamingUpdate
        {
            SessionId = _sessionId,
            UpdateType = StreamUpdateType.Reasoning,
            Content = step
        };

        _reasoningSteps.Add(update);

        // In future, this would be sent via SSE
        _logger.Debug("Reasoning step: {0}", step);
    }

    private void ResetSessionTimer()
    {
        Timers.StartSingleTimer(
            SessionTimerKey,
            new SessionTimeout { SessionId = _sessionId },
            SessionTimeout);

        _logger.Debug("Reset session timer for {0}", _sessionId);
    }

    private void HandleResetSessionTimer(ResetSessionTimer message)
    {
        if (message.SessionId == _sessionId)
        {
            ResetSessionTimer();
        }
    }

    protected virtual void HandleChatBotResponseFromPipeTo(ChatBotResponse response)
    {
        // This response comes from Tell after async operation
        _logger.Info("[{0}] HandleChatBotResponseFromPipeTo called for session {1}, ResponseType: {2}, MemoryCount: {3}",
            this.GetType().Name,
            response.SessionId,
            response.Type,
            response.ReferencedMemoryIds?.Count ?? 0);

        _logger.Info("Successfully generated response for session {0} using {1} memory/memories.",
            response.SessionId,
            response.ReferencedMemoryIds?.Count ?? 0);

        // Default behavior: send to parent
        // StreamingChatBotActor will override to send through SSE
        if (Context.Parent != null)
        {
            _logger.Info("[{0}] Sending response to parent: {1}", this.GetType().Name, Context.Parent);
            Context.Parent.Tell(response);
        }
    }

    private void HandlePipeToFailure(Status.Failure failure)
    {
        _logger.Error(failure.Cause, "PipeTo operation failed");

        // Extract session ID from the failure context if possible
        if (failure.Cause.Message.Contains("session"))
        {
            // Try to extract session ID and send error response
            var errorResponse = new ChatBotResponse
            {
                SessionId = _sessionId,
                Message = "An error occurred while generating the response. Please try again.",
                Type = ResponseType.Error,
                ReasoningSteps = _reasoningSteps.Select(r => r.Content).ToList()
            };

            Self.Tell(errorResponse);
        }
    }

    private void HandleSessionTimeout(SessionTimeout message)
    {
        _logger.Info("Session {0} timed out after 3 days of inactivity. Stopping actor.", _sessionId);

        // Clean up resources
        _conversationHistory.Clear();
        _reasoningSteps.Clear();

        // Stop the actor
        Context.Stop(Self);
    }

    protected override void PostStop()
    {
        _logger.Info("ChatBotActor for session {0} stopped", _sessionId);
        base.PostStop();
    }

    private string GenerateConversationContext()
    {
        var sb = new StringBuilder();

        // Add short-term memory if exists
        if (!string.IsNullOrWhiteSpace(_shortTermMemory))
        {
            sb.AppendLine("Session Context (Important Information):");
            sb.AppendLine(_shortTermMemory);
            sb.AppendLine();
        }

        // Selectively add last important response if it's relevant
        // Only include if it contains substantial information
        if (!string.IsNullOrWhiteSpace(_lastImportantResponse) && _lastImportantResponse.Length > 50)
        {
            sb.AppendLine("Previous Response Context:");
            sb.AppendLine(_lastImportantResponse);
            sb.AppendLine();
        }

        // Add recent conversation history
        if (_conversationEntries.Any())
        {
            sb.AppendLine("Recent Conversation:");
            foreach (var entry in _conversationEntries.TakeLast(3)) // Show last 3 exchanges
            {
                sb.AppendLine($"User: {entry.UserMessage}");
                sb.AppendLine($"Assistant: {entry.BotResponse}");
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }

    private async Task UpdateConversationEntries(string userMessage, string botResponse, bool usedMemorySearch)
    {
        // Create new entry
        var newEntry = new ConversationEntry
        {
            UserMessage = userMessage,
            BotResponse = botResponse,
            UsedMemorySearch = usedMemorySearch
        };

        // Check if we need to prune old conversations
        if (_conversationEntries.Count >= MaxConversationEntries)
        {
            // Extract important context from oldest entry before removing
            var oldestEntry = _conversationEntries.First();
            await ExtractAndUpdateShortTermMemory(oldestEntry);

            // Remove oldest entry
            _conversationEntries.RemoveAt(0);
        }

        // Add new entry
        _conversationEntries.Add(newEntry);

        // Store the last important response separately
        await UpdateLastImportantResponse(botResponse, usedMemorySearch);

        _logger.Debug("Updated conversation entries for session {0}. Total entries: {1}",
            _sessionId, _conversationEntries.Count);
    }

    private async Task ExtractAndUpdateShortTermMemory(ConversationEntry entryToExtract)
    {
        try
        {
            // Prompt to extract important information
            var extractionPrompt = $@"
Extract the most important information from the following conversation exchange that should be remembered for future context.
Focus on key facts, user preferences, topics discussed, or any specific information mentioned.
Keep the extraction concise (max 100 words).

User: {entryToExtract.UserMessage}
Assistant: {entryToExtract.BotResponse}

Current short-term memory: {_shortTermMemory}

Provide an updated short-term memory that combines the current memory with new important information.
Maximum length: {MaxShortTermMemoryLength} characters.";

            // Use LLM to extract important context
            var extractedContext = await _llmService.CompleteAsync(extractionPrompt);

            // Update short-term memory, ensuring it doesn't exceed max length
            if (!string.IsNullOrWhiteSpace(extractedContext))
            {
                _shortTermMemory = extractedContext.Length > MaxShortTermMemoryLength
                    ? extractedContext.Substring(0, MaxShortTermMemoryLength)
                    : extractedContext;

                _logger.Debug("Updated short-term memory for session {0}: {1}",
                    _sessionId, _shortTermMemory);
            }
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to extract context from conversation for session {0}", _sessionId);
            // Continue without updating short-term memory
        }
    }

    private async Task UpdateLastImportantResponse(string botResponse, bool usedMemorySearch)
    {
        try
        {
            // Only update if the response contains substantial information
            // Prioritize memory-based responses or longer responses
            if (usedMemorySearch || botResponse.Length > 100)
            {
                // Extract the most important parts of the response
                var extractionPrompt = $@"
Extract the key information from this assistant response that might be useful for future conversation context.
Focus on facts, data, explanations, or specific information provided.
Keep it concise (max 300 characters).

Assistant Response: {botResponse}

Extract only the most important information:";

                var extractedResponse = await _llmService.CompleteAsync(extractionPrompt);

                if (!string.IsNullOrWhiteSpace(extractedResponse))
                {
                    _lastImportantResponse = extractedResponse.Length > MaxLastResponseLength
                        ? extractedResponse.Substring(0, MaxLastResponseLength)
                        : extractedResponse;

                    _logger.Debug("Updated last important response for session {0}", _sessionId);
                }
            }
            else if (botResponse.Length > 50)
            {
                // For shorter responses, store directly if meaningful
                _lastImportantResponse = botResponse.Length > MaxLastResponseLength
                    ? botResponse.Substring(0, MaxLastResponseLength)
                    : botResponse;
            }
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to update last important response for session {0}", _sessionId);
            // Continue without updating
        }
    }

    public static Props Props(
        string sessionId,
        IActorRef searchMemoryActor,
        IActorRef decisionActor,
        ILlmService llmService)
    {
        return Akka.Actor.Props.Create(() =>
            new ChatBotActor(sessionId, searchMemoryActor, decisionActor, llmService));
    }
}