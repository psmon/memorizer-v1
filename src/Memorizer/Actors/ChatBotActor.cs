using Akka.Actor;
using Akka.Event;
using Memorizer.Services;
using Memorizer.Settings;
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
    private readonly ILlmExService? _llmExService;
    private readonly IMultiModalService? _multiModalService;
    private readonly IWebSearchService? _webSearchService;
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

    // Current request state for UseExtendedModel tracking
    protected bool _useExtendedModelForCurrentRequest = false;

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

    // Prompt for extracting optimal web search keywords from user message
    private const string WebSearchKeywordPrompt = @"Extract the best web search keywords from the following user question.
Return ONLY the search keywords (no explanation, no quotes, no prefix).
Keep it concise (2-5 words).

User question: {0}
Search keywords:";

    // Prompt for hybrid responses (memory + web search combined)
    private const string HybridResponsePrompt = @"
You are a helpful AI assistant named ASKBot with access to stored memories and web search results.

{3}

Current User Query: {0}

Relevant Information from Memory:
{1}

Additional Information from Web Search:
{2}

Based on the conversation context, the memory information, and the web search results, provide a comprehensive and accurate answer.
Clearly indicate which information comes from memory and which from web search when appropriate.
Cite web sources where appropriate by mentioning the title or URL.
Maintain conversation continuity and reference previous context when appropriate.";

    // Prompt for web search-based responses
    private const string WebSearchBasedResponsePrompt = @"
You are a helpful AI assistant named ASKBot. You are having a conversation with a user.
You found relevant information from web search results to help answer the question.

{2}

Current User Query: {0}

Web Search Results:
{1}

Based on the conversation context and the web search results, provide a comprehensive and accurate answer.
Cite sources where appropriate by mentioning the title or URL.
Maintain conversation continuity and reference previous context when appropriate.";

    public ITimerScheduler Timers { get; set; } = null!;

    public ChatBotActor(
        string sessionId,
        IActorRef searchMemoryActor,
        IActorRef decisionActor,
        ILlmService llmService,
        ILlmExService? llmExService = null,
        IMultiModalService? multiModalService = null,
        IWebSearchService? webSearchService = null)
    {
        _sessionId = sessionId;
        _searchMemoryActor = searchMemoryActor;
        _decisionActor = decisionActor;
        _llmService = llmService;
        _llmExService = llmExService;
        _multiModalService = multiModalService;
        _webSearchService = webSearchService;
        _logger = Context.GetLogger();

        // Set up message handlers
        Receive<UserChatRequest>(HandleUserChatRequest);
        Receive<SearchMemoryResponse>(HandleSearchMemoryResponse);
        Receive<AnalyzeQueryTypeResponse>(HandleAnalyzeQueryTypeResponse);
        Receive<MultiTopicSearchResponse>(HandleMultiTopicSearchResponse);
        Receive<EvaluateRelevanceResponse>(HandleEvaluateRelevanceResponse);
        Receive<SessionTimeout>(HandleSessionTimeout);
        Receive<ResetSessionTimer>(HandleResetSessionTimer);
        Receive<ChatBotResponse>(HandleChatBotResponseFromPipeTo);
        Receive<Status.Failure>(HandlePipeToFailure);
        Receive<GetConversationHistoryRequest>(HandleGetConversationHistoryRequest);

        // Start session timer
        ResetSessionTimer();

        _logger.Info("ChatBotActor created for session {0}", _sessionId);
    }

    private void HandleUserChatRequest(UserChatRequest request)
    {
        _logger.Info("Processing chat request for session {0}: {1} (MultiModal: {2}, ExtendedModel: {3})",
            request.SessionId, request.Message, request.IsMultiModal, request.UseExtendedModel);

        // Store UseExtendedModel state for current request
        _useExtendedModelForCurrentRequest = request.UseExtendedModel;

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
            // For multi-modal requests, use MultiModal service directly instead of memory search
            if (request.IsMultiModal)
            {
                AddReasoningStep("Processing multi-modal request (image + text)...");
                GenerateMultiModalResponse(request, originalSender);
                SetupBaseHandlers();
                return;
            }

            // Step 1: Analyze query type to determine if multi-topic search is needed
            AddReasoningStep("Analyzing query to determine search strategy...");

            var analyzeRequest = new AnalyzeQueryTypeRequest
            {
                Query = request.Message,
                SessionId = request.SessionId,
                UseExtendedModel = _useExtendedModelForCurrentRequest
            };

            // Store context for continuation
            Context.Become(WaitingForQueryTypeAnalysis(request, originalSender));

            _searchMemoryActor.Tell(analyzeRequest, Self);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error processing chat request for session {0}", request.SessionId);
            SendErrorResponse(originalSender, request.SessionId, "An error occurred processing your request.");
        }
    }

    private Receive WaitingForQueryTypeAnalysis(UserChatRequest originalRequest, IActorRef originalSender)
    {
        return message =>
        {
            if (message is AnalyzeQueryTypeResponse analysisResponse)
            {
                HandleAnalyzeQueryTypeResponseContinuation(analysisResponse, originalRequest, originalSender);
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
            else if (message is GetConversationHistoryRequest historyRequest)
            {
                HandleGetConversationHistoryRequest(historyRequest);
                return true;
            }
            return false;
        };
    }

    private void HandleAnalyzeQueryTypeResponseContinuation(
        AnalyzeQueryTypeResponse analysisResponse,
        UserChatRequest originalRequest,
        IActorRef originalSender)
    {
        AddReasoningStep($"Query analysis: {analysisResponse.DocumentTypesNeeded} topic(s) identified");
        AddReasoningStep($"Reasoning: {analysisResponse.Reasoning}");

        // If only one topic, use traditional single search
        if (analysisResponse.DocumentTypesNeeded == 1 || analysisResponse.Topics.Count == 0)
        {
            AddReasoningStep("Using single-topic search strategy...");

            var searchRequest = new SearchMemoryRequest
            {
                Query = originalRequest.Message,
                SessionId = originalRequest.SessionId,
                MaxResults = 5,
                MinSimilarity = 0.3,
                UseExtendedModel = _useExtendedModelForCurrentRequest
            };

            Context.Become(WaitingForSearchResponse(originalRequest, originalSender));
            _searchMemoryActor.Tell(searchRequest, Self);
        }
        else
        {
            // Multi-topic search
            AddReasoningStep($"Using multi-topic search strategy for topics: {string.Join(", ", analysisResponse.Topics)}");

            var multiSearchRequest = new MultiTopicSearchRequest
            {
                Query = originalRequest.Message,
                SessionId = originalRequest.SessionId,
                Topics = analysisResponse.Topics,
                ResultsPerTopic = 1, // 1 result per topic
                MinSimilarity = 0.3,
                UseExtendedModel = _useExtendedModelForCurrentRequest
            };

            Context.Become(WaitingForMultiTopicSearchResponse(originalRequest, originalSender));
            _searchMemoryActor.Tell(multiSearchRequest, Self);
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
            else if (message is GetConversationHistoryRequest historyRequest)
            {
                HandleGetConversationHistoryRequest(historyRequest);
                return true;
            }
            return false;
        };
    }

    private Receive WaitingForMultiTopicSearchResponse(UserChatRequest originalRequest, IActorRef originalSender)
    {
        return message =>
        {
            if (message is MultiTopicSearchResponse multiSearchResponse)
            {
                HandleMultiTopicSearchResponseContinuation(multiSearchResponse, originalRequest, originalSender);
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
            else if (message is GetConversationHistoryRequest historyRequest)
            {
                HandleGetConversationHistoryRequest(historyRequest);
                return true;
            }
            return false;
        };
    }

    private void HandleMultiTopicSearchResponseContinuation(
        MultiTopicSearchResponse multiSearchResponse,
        UserChatRequest originalRequest,
        IActorRef originalSender)
    {
        if (multiSearchResponse.AllMemories.Count == 0)
        {
            AddReasoningStep($"No memories found across {multiSearchResponse.TopicsSearched} topics.");
            if (_webSearchService != null)
            {
                GenerateWebSearchBasedResponse(originalRequest, originalSender);
            }
            else
            {
                GenerateGeneralResponse(originalRequest, originalSender);
            }
            SetupBaseHandlers();
        }
        else
        {
            AddReasoningStep($"Found {multiSearchResponse.AllMemories.Count} memories across {multiSearchResponse.TopicsSearched} topics.");

            // Log topic-specific results
            foreach (var topicResult in multiSearchResponse.TopicResults)
            {
                AddReasoningStep($"Topic '{topicResult.Key}': {topicResult.Value.Count} memory/memories found");
            }

            // Check per-topic coverage for hybrid search
            var emptyTopics = multiSearchResponse.TopicResults
                .Where(t => t.Value.Count == 0)
                .Select(t => t.Key)
                .ToList();

            if (emptyTopics.Count > 0 && _webSearchService != null)
            {
                // Hybrid: some topics have memory, others need web search
                AddReasoningStep($"Partial coverage: {emptyTopics.Count} topic(s) missing from memory, searching web...");
                foreach (var topic in emptyTopics)
                {
                    AddReasoningStep($"Web search needed for topic: {topic}");
                }
                GenerateHybridResponse(originalRequest, multiSearchResponse, emptyTopics, originalSender);
            }
            else
            {
                AddReasoningStep("Evaluating relevance of multi-topic search results...");

                // All topics have memory results - evaluate relevance as before
                var evaluateRequest = new EvaluateRelevanceRequest
                {
                    SessionId = originalRequest.SessionId,
                    Query = originalRequest.Message,
                    Memories = multiSearchResponse.AllMemories,
                    UseExtendedModel = _useExtendedModelForCurrentRequest
                };

                Context.Become(WaitingForEvaluationResponse(originalRequest, originalSender));
                _decisionActor.Tell(evaluateRequest, Self);
            }
        }
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
                case AnalyzeQueryTypeResponse analysis:
                    HandleAnalyzeQueryTypeResponse(analysis);
                    return true;
                case MultiTopicSearchResponse multiSearch:
                    HandleMultiTopicSearchResponse(multiSearch);
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
                case GetConversationHistoryRequest historyRequest:
                    HandleGetConversationHistoryRequest(historyRequest);
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
            if (_webSearchService != null)
            {
                GenerateWebSearchBasedResponse(originalRequest, originalSender);
            }
            else
            {
                GenerateGeneralResponse(originalRequest, originalSender);
            }
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
                Memories = searchResponse.Memories,
                UseExtendedModel = _useExtendedModelForCurrentRequest
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
            else if (message is GetConversationHistoryRequest historyRequest)
            {
                HandleGetConversationHistoryRequest(historyRequest);
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
            if (_webSearchService != null)
            {
                GenerateWebSearchBasedResponse(originalRequest, originalSender);
            }
            else
            {
                GenerateGeneralResponse(originalRequest, originalSender);
            }
        }
        // Return to base handlers after starting response generation
        SetupBaseHandlers();
    }

    private void HandleSearchMemoryResponse(SearchMemoryResponse response)
    {
        // This handler is for unexpected responses
        _logger.Warning("Received unexpected SearchMemoryResponse for session {0}", response.SessionId);
    }

    private void HandleAnalyzeQueryTypeResponse(AnalyzeQueryTypeResponse response)
    {
        // This handler is for unexpected responses
        _logger.Warning("Received unexpected AnalyzeQueryTypeResponse for session {0}", response.SessionId);
    }

    private void HandleMultiTopicSearchResponse(MultiTopicSearchResponse response)
    {
        // This handler is for unexpected responses
        _logger.Warning("Received unexpected MultiTopicSearchResponse for session {0}", response.SessionId);
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

                    // Generate response using LLM (or LLM-EX if enabled)
                    llmResponse = await CompleteWithLlmAsync(prompt);
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

            // Update conversation entries with referenced memory IDs
            await UpdateConversationEntries(request.Message, llmResponse, true, usedMemoryIds, request.ImageData, request.ImageFormat);

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

    private void GenerateMultiModalResponse(UserChatRequest request, IActorRef originalSender)
    {
        AddReasoningStep("Generating multi-modal AI response...");

        // Capture self reference before async operation
        var self = Self;

        // Use Task.Run to avoid blocking actor thread
        Task.Run(async () =>
        {
            try
            {
                var response = await GenerateMultiModalResponseAsync(request);
                _logger.Info("About to send MultiModal ChatBotResponse to self for session {0}, Type: {1}", response.SessionId, response.Type);
                self.Tell(response);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to generate multi-modal response for session {0}", request.SessionId);
                self.Tell(new Status.Failure(ex));
            }
        });
    }

    private async Task<ChatBotResponse> GenerateMultiModalResponseAsync(UserChatRequest request)
    {
        try
        {
            if (_multiModalService == null)
            {
                _logger.Warning("MultiModalService is not available for session {0}. Falling back to text-only response.", request.SessionId);
                AddReasoningStep("Multi-modal service not available, using text-only mode...");
                return await GenerateGeneralResponseAsync(request);
            }

            if (request.ImageData == null || request.ImageData.Length == 0)
            {
                _logger.Warning("Multi-modal request but no image data provided for session {0}", request.SessionId);
                return await GenerateGeneralResponseAsync(request);
            }

            AddReasoningStep($"Analyzing image with multi-modal model ({request.ImageFormat})...");

            // Call multi-modal service to analyze image with text prompt
            var llmResponse = await _multiModalService.AnalyzeImageAsync(
                request.ImageData,
                request.Message,
                request.ImageFormat ?? "png"
            );

            // Add to conversation history
            _conversationHistory.Add($"Assistant (multi-modal): {llmResponse}");

            // Update conversation entries
            await UpdateConversationEntries(request.Message, llmResponse, false, null, request.ImageData, request.ImageFormat);

            // Create and return response
            var response = new ChatBotResponse
            {
                SessionId = request.SessionId,
                Message = llmResponse,
                Type = ResponseType.General, // Multi-modal is treated as General type (not memory-based)
                ReasoningSteps = _reasoningSteps.Select(r => r.Content).ToList()
            };

            _logger.Info("Generated multi-modal response for session {0}", request.SessionId);
            return response;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error generating multi-modal response for session {0}", request.SessionId);
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

            // Generate response using LLM (or LLM-EX if enabled)
            var llmResponse = await CompleteWithLlmAsync(prompt);

            // Add to conversation history
            _conversationHistory.Add($"Assistant (general): {llmResponse}");

            // Update conversation entries
            await UpdateConversationEntries(request.Message, llmResponse, false, null, request.ImageData, request.ImageFormat);

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

    private void GenerateHybridResponse(
        UserChatRequest request,
        MultiTopicSearchResponse multiSearchResponse,
        List<string> emptyTopics,
        IActorRef originalSender)
    {
        AddReasoningStep("Generating hybrid response (memory + web search)...");

        var self = Self;
        Task.Run(async () =>
        {
            try
            {
                var response = await GenerateHybridResponseAsync(request, multiSearchResponse, emptyTopics);
                self.Tell(response);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Hybrid response failed for session {0}, falling back to memory-based", request.SessionId);
                try
                {
                    // Fallback: try memory-based response with available memories
                    var fallbackResponse = await GenerateMemoryBasedResponseAsync(request, multiSearchResponse.AllMemories, originalSender);
                    self.Tell(fallbackResponse);
                }
                catch (Exception fallbackEx)
                {
                    self.Tell(new Status.Failure(fallbackEx));
                }
            }
        });
        SetupBaseHandlers();
    }

    private async Task<ChatBotResponse> GenerateHybridResponseAsync(
        UserChatRequest request,
        MultiTopicSearchResponse multiSearchResponse,
        List<string> emptyTopics)
    {
        try
        {
            // 1. Web search for topics without memory results
            var allWebReferences = new List<WebSearchReference>();
            var webResultsBuilder = new StringBuilder();

            foreach (var topic in emptyTopics)
            {
                AddReasoningStep($"Extracting search keywords for topic: {topic}");
                var searchKeyword = await ExtractWebSearchKeyword(topic);
                AddReasoningStep($"Searching web for: {searchKeyword}");

                var searchResult = await _webSearchService!.SearchAsync(
                    WebSearchProvider.Naver,
                    searchKeyword,
                    maxResults: 3,
                    accessMode: WebSearchAccessMode.Headless);

                if (searchResult.Items.Count > 0)
                {
                    AddReasoningStep($"Web search found {searchResult.Items.Count} results for topic: {topic}");
                    foreach (var item in searchResult.Items)
                    {
                        allWebReferences.Add(new WebSearchReference
                        {
                            Title = item.Title,
                            Url = item.Url,
                            Snippet = item.Snippet
                        });
                    }
                    webResultsBuilder.AppendLine($"--- Topic: {topic} ---");
                    webResultsBuilder.Append(FormatWebSearchResults(searchResult.Items));
                }
                else
                {
                    AddReasoningStep($"No web search results for topic: {topic}");
                }
            }

            // 2. Build hybrid prompt with memory + web search results
            var memoriesText = FormatMemoriesForResponse(multiSearchResponse.AllMemories);
            var webResultsText = webResultsBuilder.ToString();
            var conversationContext = GenerateConversationContext();
            var usedMemoryIds = multiSearchResponse.AllMemories.Select(m => m.Id).ToList();

            string llmResponse;

            if (allWebReferences.Count > 0)
            {
                // Hybrid: both memory and web results available
                var prompt = string.Format(HybridResponsePrompt, request.Message, memoriesText, webResultsText, conversationContext);
                llmResponse = await CompleteWithLlmAsync(prompt);
                AddReasoningStep($"Generated hybrid response using {multiSearchResponse.AllMemories.Count} memories and {allWebReferences.Count} web references.");
            }
            else
            {
                // Web search returned nothing - use memory only
                var prompt = string.Format(MemoryBasedResponsePrompt, request.Message, memoriesText, conversationContext);
                llmResponse = await CompleteWithLlmAsync(prompt);
                AddReasoningStep($"Web search returned no results, generated response using {multiSearchResponse.AllMemories.Count} memories only.");
            }

            // 3. Update conversation history
            _conversationHistory.Add($"Assistant (hybrid): {llmResponse}");
            await UpdateConversationEntries(request.Message, llmResponse, true, usedMemoryIds, request.ImageData, request.ImageFormat, allWebReferences.Count > 0 ? allWebReferences : null);

            // 4. Return response with both memory IDs and web references
            var responseType = allWebReferences.Count > 0 ? ResponseType.HybridBased : ResponseType.MemoryBased;
            var response = new ChatBotResponse
            {
                SessionId = request.SessionId,
                Message = llmResponse,
                Type = responseType,
                ReferencedMemoryIds = usedMemoryIds,
                WebSearchReferences = allWebReferences.Count > 0 ? allWebReferences : null,
                ReasoningSteps = _reasoningSteps.Select(r => r.Content).ToList()
            };

            _logger.Info("Generated hybrid response for session {0} using {1} memories and {2} web references",
                request.SessionId, usedMemoryIds.Count, allWebReferences.Count);
            return response;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error generating hybrid response for session {0}", request.SessionId);
            throw;
        }
    }

    private void GenerateWebSearchBasedResponse(UserChatRequest request, IActorRef originalSender)
    {
        AddReasoningStep("Searching the web for relevant information...");

        var self = Self;
        Task.Run(async () =>
        {
            try
            {
                var response = await GenerateWebSearchBasedResponseAsync(request);
                self.Tell(response);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Web search failed for session {0}, falling back to general response", request.SessionId);
                try
                {
                    var fallbackResponse = await GenerateGeneralResponseAsync(request);
                    self.Tell(fallbackResponse);
                }
                catch (Exception fallbackEx)
                {
                    self.Tell(new Status.Failure(fallbackEx));
                }
            }
        });
    }

    private async Task<string> ExtractWebSearchKeyword(string userMessage)
    {
        try
        {
            var prompt = string.Format(WebSearchKeywordPrompt, userMessage);
            var keyword = await CompleteWithLlmAsync(prompt);
            return string.IsNullOrWhiteSpace(keyword) ? userMessage : keyword.Trim();
        }
        catch (Exception ex)
        {
            _logger.Warning("Error extracting web search keyword: {0}. Using original message.", ex.Message);
            return userMessage;
        }
    }

    private async Task<ChatBotResponse> GenerateWebSearchBasedResponseAsync(UserChatRequest request)
    {
        AddReasoningStep("Extracting optimal search keywords...");
        var searchKeyword = await ExtractWebSearchKeyword(request.Message);
        AddReasoningStep($"Search keyword: {searchKeyword}");

        AddReasoningStep("Performing web search (Headless mode)...");

        var searchResult = await _webSearchService!.SearchAsync(
            WebSearchProvider.Naver,
            searchKeyword,
            maxResults: 5,
            accessMode: WebSearchAccessMode.Headless);

        if (searchResult.Items.Count == 0)
        {
            AddReasoningStep("Web search returned no results, falling back to general response.");
            return await GenerateGeneralResponseAsync(request);
        }

        AddReasoningStep($"Web search found {searchResult.Items.Count} results.");

        var webReferences = searchResult.Items.Select(item => new WebSearchReference
        {
            Title = item.Title,
            Url = item.Url,
            Snippet = item.Snippet
        }).ToList();

        var webResultsText = FormatWebSearchResults(searchResult.Items);
        var conversationContext = GenerateConversationContext();
        var prompt = string.Format(WebSearchBasedResponsePrompt, request.Message, webResultsText, conversationContext);

        var llmResponse = await CompleteWithLlmAsync(prompt);

        _conversationHistory.Add($"Assistant (web-search-based): {llmResponse}");

        await UpdateConversationEntries(request.Message, llmResponse, false, null, request.ImageData, request.ImageFormat, webReferences);

        var response = new ChatBotResponse
        {
            SessionId = request.SessionId,
            Message = llmResponse,
            Type = ResponseType.WebSearchBased,
            WebSearchReferences = webReferences,
            ReasoningSteps = _reasoningSteps.Select(r => r.Content).ToList()
        };

        _logger.Info("Generated web-search-based response for session {0} with {1} web references", request.SessionId, webReferences.Count);
        return response;
    }

    private static string FormatWebSearchResults(IReadOnlyList<WebSearchItem> items)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            sb.AppendLine($"--- Web Result {i + 1} ---");
            sb.AppendLine($"Title: {item.Title}");
            sb.AppendLine($"URL: {item.Url}");
            sb.AppendLine($"Snippet: {item.Snippet}");
            sb.AppendLine();
        }
        return sb.ToString();
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

    private void HandleGetConversationHistoryRequest(GetConversationHistoryRequest request)
    {
        _logger.Debug("Retrieving conversation history for session {0}. Total entries: {1}",
            _sessionId, _conversationEntries.Count);

        // Return conversation history to sender
        Sender.Tell(new GetConversationHistoryResponse
        {
            SessionId = _sessionId,
            ConversationEntries = new List<ConversationEntry>(_conversationEntries)
        });
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

    private async Task UpdateConversationEntries(string userMessage, string botResponse, bool usedMemorySearch, List<Guid>? referencedMemoryIds = null, byte[]? imageData = null, string? imageFormat = null, List<WebSearchReference>? webSearchReferences = null)
    {
        // Create new entry
        var newEntry = new ConversationEntry
        {
            UserMessage = userMessage,
            BotResponse = botResponse,
            UsedMemorySearch = usedMemorySearch,
            ReferencedMemoryIds = referencedMemoryIds,
            WebSearchReferences = webSearchReferences,
            ImageData = imageData,
            ImageFormat = imageFormat
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

    /// <summary>
    /// Complete prompt using appropriate LLM service based on UseExtendedModel flag
    /// </summary>
    protected async Task<string> CompleteWithLlmAsync(string prompt)
    {
        if (_useExtendedModelForCurrentRequest && _llmExService != null)
        {
            _logger.Info("Using LLM-EX (extended model) for completion");
            return await _llmExService.CompleteAsync(prompt);
        }
        else
        {
            return await _llmService.CompleteAsync(prompt);
        }
    }

    public static Props Props(
        string sessionId,
        IActorRef searchMemoryActor,
        IActorRef decisionActor,
        ILlmService llmService,
        ILlmExService? llmExService = null,
        IMultiModalService? multiModalService = null,
        IWebSearchService? webSearchService = null)
    {
        return Akka.Actor.Props.Create(() =>
            new ChatBotActor(sessionId, searchMemoryActor, decisionActor, llmService, llmExService, multiModalService, webSearchService));
    }
}