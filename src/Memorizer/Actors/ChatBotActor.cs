using Akka.Actor;
using Akka.Event;
using Memorizer.Services;
using System.Text;

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

    // Prompt for general responses
    private const string GeneralResponsePrompt = @"
You are a helpful AI assistant. Answer the following user query in a clear and informative way.

User Query: {0}

Provide a helpful and concise response.";

    // Prompt for memory-based responses
    private const string MemoryBasedResponsePrompt = @"
You are a helpful AI assistant with access to stored memories. Use the following relevant information to answer the user's query.

User Query: {0}

Relevant Information:
{1}

Based on the above information, provide a comprehensive and accurate answer to the user's query.
If the information references specific details, include them in your response.";

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

    private async void GenerateMemoryBasedResponse(
        UserChatRequest request,
        List<Models.Memory> relevantMemories,
        IActorRef originalSender)
    {
        try
        {
            AddReasoningStep("Generating response based on relevant memories...");

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
                    var prompt = string.Format(MemoryBasedResponsePrompt, request.Message, memoriesText);

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

                // Use general response as fallback
                GenerateGeneralResponse(request, originalSender);
                return;
            }

            // Add to conversation history
            _conversationHistory.Add($"Assistant (memory-based): {llmResponse}");

            // Create response
            var response = new ChatBotResponse
            {
                SessionId = request.SessionId,
                Message = llmResponse,
                Type = ResponseType.MemoryBased,
                ReferencedMemoryIds = usedMemoryIds,
                ReasoningSteps = _reasoningSteps.Select(r => r.Content).ToList()
            };

            originalSender.Tell(response);
            _logger.Info("Sent memory-based response for session {0} using {1} memories", request.SessionId, usedMemoryIds.Count);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error generating memory-based response for session {0}", request.SessionId);
            // Fall back to general response on any error
            AddReasoningStep("Failed to generate memory-based response, falling back to general AI...");
            GenerateGeneralResponse(request, originalSender);
        }
    }

    private async void GenerateGeneralResponse(UserChatRequest request, IActorRef originalSender)
    {
        try
        {
            AddReasoningStep("Generating general AI response...");

            var prompt = string.Format(GeneralResponsePrompt, request.Message);

            // Generate response using LLM
            var llmResponse = await _llmService.CompleteAsync(prompt);

            // Add to conversation history
            _conversationHistory.Add($"Assistant (general): {llmResponse}");

            // Create response
            var response = new ChatBotResponse
            {
                SessionId = request.SessionId,
                Message = llmResponse,
                Type = ResponseType.General,
                ReasoningSteps = _reasoningSteps.Select(r => r.Content).ToList()
            };

            originalSender.Tell(response);
            _logger.Info("Sent general response for session {0}", request.SessionId);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error generating general response for session {0}", request.SessionId);
            SendErrorResponse(originalSender, request.SessionId, "Failed to generate response.");
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

    private void SendErrorResponse(IActorRef sender, string sessionId, string errorMessage)
    {
        var response = new ChatBotResponse
        {
            SessionId = sessionId,
            Message = errorMessage,
            Type = ResponseType.Error,
            ReasoningSteps = _reasoningSteps.Select(r => r.Content).ToList()
        };

        sender.Tell(response);
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