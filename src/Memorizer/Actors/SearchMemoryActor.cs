using Akka.Actor;
using Akka.Event;
using Memorizer.Services;
using System.Text.RegularExpressions;

namespace Memorizer.Actors;

/// <summary>
/// Actor responsible for searching memories based on user queries
/// </summary>
public sealed class SearchMemoryActor : ReceiveActor
{
    private readonly IStorage _storage;
    private readonly ILlmService _llmService;
    private readonly ILoggingAdapter _logger;

    // Prompts for LLM operations
    private const string SearchRequiredPrompt = @"
Analyze the following user query and determine if a memory search is needed.
A search is ALWAYS needed if the user is asking about:
- Technical concepts, frameworks, or technologies (e.g., Docker, Kubernetes, Reactive Streams, etc.)
- Programming languages, libraries, or tools
- Specific information that might be stored
- Past conversations or knowledge
- Technical details, documentation, or how-to guides
- Any reference to stored information
- Explanations about specific topics (using words like '알려줘', '설명해', 'tell me about', 'explain')

A search is NOT needed ONLY for:
- Simple greetings like '안녕' or 'hello' without other content
- Questions about the chatbot system itself (like '너는 누구야?')
- Meta commands to the chatbot

User Query: {0}

Respond with only 'YES' if search is needed, or 'NO' if not needed.";

    private const string AnalyzeQueryTypePrompt = @"
Analyze the following user query and determine how many different topics/subjects need to be searched.

Rules:
1. Single topic (1): The query asks about ONE specific topic
   - Example: ""A를 검색해 요약해주세요"" -> 1 topic: A
   - Example: ""Reactive Streams에 대해 알려줘"" -> 1 topic: Reactive Streams

2. Two topics (2): The query asks to compare or find relationships between TWO topics
   - Example: ""A와 B를 검색해 공통점을 찾아주세요"" -> 2 topics: A, B
   - Example: ""Docker와 Kubernetes의 차이점은?"" -> 2 topics: Docker, Kubernetes

3. Three topics (3): The query involves THREE distinct topics (maximum limit)
   - Example: ""A, B, C를 비교해주세요"" -> 3 topics: A, B, C

User Query: {0}

Respond in the following format:
TOPICS_COUNT: <number 1-3>
TOPICS: <comma-separated list of topics in Korean and English>
REASONING: <brief explanation in Korean>

Example response:
TOPICS_COUNT: 2
TOPICS: Docker 도커, Kubernetes 쿠버네티스
REASONING: 사용자가 두 개의 기술을 비교하려고 합니다.";

    private const string QueryTransformPrompt = @"
Transform the following user query into an optimized search query for memory retrieval.
Focus on extracting key concepts and technical terms.

IMPORTANT:
- If the query contains technical terms in Korean, include BOTH Korean and English versions
- For example: 'Reactive Stream' should become 'Reactive Streams Reactive Stream 리액티브 스트림'
- Include common variations and related terms

Original Query: {0}

Provide only the transformed search query with all relevant terms, nothing else.";

    private const string KeywordExtractionPrompt = @"
Extract 3-5 key keywords from the following query for search purposes.
Include both English and Korean versions of technical terms where applicable.
Focus on the most important concepts.

Query: {0}

Provide keywords separated by commas, nothing else.";

    public SearchMemoryActor(IStorage storage, ILlmService llmService)
    {
        _storage = storage;
        _llmService = llmService;
        _logger = Context.GetLogger();

        ReceiveAsync<SearchMemoryRequest>(HandleSearchMemoryRequest);
        ReceiveAsync<AnalyzeQueryTypeRequest>(HandleAnalyzeQueryTypeRequest);
        ReceiveAsync<MultiTopicSearchRequest>(HandleMultiTopicSearchRequest);
    }

    private async Task HandleSearchMemoryRequest(SearchMemoryRequest request)
    {
        _logger.Info("Processing search request for session {0}", request.SessionId);

        try
        {
            // Step 1: Determine if search is needed
            var searchNeeded = await DetermineIfSearchNeeded(request.Query);
            _logger.Info("Search needed determination for '{0}': {1}", request.Query, searchNeeded);

            if (!searchNeeded)
            {
                _logger.Info("Search not needed for query: {0}", request.Query);
                var noSearchResponse = new SearchMemoryResponse
                {
                    SessionId = request.SessionId,
                    OriginalQuery = request.Query,
                    Memories = new List<Models.Memory>(),
                    SearchPerformed = false,
                    RetryAttempts = 0
                };
                Sender.Tell(noSearchResponse);
                return;
            }

            // Step 2: Transform query for better search
            var transformedQuery = await TransformQuery(request.Query);
            _logger.Info("Transformed query: {0} -> {1}", request.Query, transformedQuery);

            // Step 3: Perform initial search using embedding
            // Use a lower similarity threshold to ensure we find relevant memories
            var adjustedSimilarity = Math.Min(request.MinSimilarity, 0.2);
            var memories = await _storage.Search(
                transformedQuery,
                request.MaxResults,
                adjustedSimilarity,
                null); // No filter tags for general search

            var retryAttempts = 0;
            var extractedKeywords = new List<string>();

            // Step 4: If no results, try keyword-based search (up to 3 attempts)
            if (memories.Count == 0 && retryAttempts < 3)
            {
                _logger.Debug("No results found, attempting keyword search");
                extractedKeywords = await ExtractKeywords(request.Query);

                foreach (var keyword in extractedKeywords.Take(3))
                {
                    if (memories.Count > 0) break;

                    retryAttempts++;
                    _logger.Debug("Retry attempt {0} with keyword: {1}", retryAttempts, keyword);

                    memories = await _storage.Search(
                        keyword,
                        request.MaxResults,
                        adjustedSimilarity,
                        null);
                }
            }

            _logger.Info("Search completed for session {0}: {1} results found after {2} attempts",
                request.SessionId, memories.Count, retryAttempts);

            var response = new SearchMemoryResponse
            {
                SessionId = request.SessionId,
                OriginalQuery = request.Query,
                TransformedQuery = transformedQuery,
                Memories = memories,
                SearchPerformed = true,
                RetryAttempts = retryAttempts,
                ExtractedKeywords = extractedKeywords
            };

            Sender.Tell(response);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error processing search request for session {0}", request.SessionId);

            var errorResponse = new SearchMemoryResponse
            {
                SessionId = request.SessionId,
                OriginalQuery = request.Query,
                Memories = new List<Models.Memory>(),
                SearchPerformed = false,
                RetryAttempts = 0
            };

            Sender.Tell(errorResponse);
        }
    }

    private async Task<bool> DetermineIfSearchNeeded(string query)
    {
        try
        {
            var prompt = string.Format(SearchRequiredPrompt, query);
            var response = await _llmService.CompleteAsync(prompt);

            return response.Trim().Equals("YES", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.Warning("Error determining if search needed: {0}. Defaulting to YES.", ex.Message);
            return true; // Default to searching if LLM fails
        }
    }

    private async Task<string> TransformQuery(string query)
    {
        try
        {
            var prompt = string.Format(QueryTransformPrompt, query);
            var transformedQuery = await _llmService.CompleteAsync(prompt);

            return string.IsNullOrWhiteSpace(transformedQuery) ? query : transformedQuery.Trim();
        }
        catch (Exception ex)
        {
            _logger.Warning("Error transforming query: {0}. Using original query.", ex.Message);
            return query;
        }
    }

    private async Task<List<string>> ExtractKeywords(string query)
    {
        try
        {
            var prompt = string.Format(KeywordExtractionPrompt, query);
            var response = await _llmService.CompleteAsync(prompt);

            var keywords = response
                .Split(',')
                .Select(k => k.Trim())
                .Where(k => !string.IsNullOrWhiteSpace(k))
                .ToList();

            return keywords;
        }
        catch (Exception ex)
        {
            _logger.Warning("Error extracting keywords: {0}. Using fallback extraction.", ex.Message);

            // Fallback: simple keyword extraction
            var words = Regex.Split(query, @"\W+")
                .Where(w => w.Length > 2)
                .Distinct()
                .ToList();

            return words;
        }
    }

    private async Task HandleAnalyzeQueryTypeRequest(AnalyzeQueryTypeRequest request)
    {
        _logger.Info("Analyzing query type for session {0}: {1}", request.SessionId, request.Query);

        try
        {
            var prompt = string.Format(AnalyzeQueryTypePrompt, request.Query);
            var llmResponse = await _llmService.CompleteAsync(prompt);

            var (topicsCount, topics, reasoning) = ParseAnalyzeQueryTypeResponse(llmResponse);

            _logger.Info("Query analysis complete for session {0}: {1} topics identified",
                request.SessionId, topicsCount);

            var response = new AnalyzeQueryTypeResponse
            {
                SessionId = request.SessionId,
                DocumentTypesNeeded = topicsCount,
                Topics = topics,
                Reasoning = reasoning
            };

            Sender.Tell(response);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error analyzing query type for session {0}", request.SessionId);

            // Default to single topic search on error
            var errorResponse = new AnalyzeQueryTypeResponse
            {
                SessionId = request.SessionId,
                DocumentTypesNeeded = 1,
                Topics = new List<string> { request.Query },
                Reasoning = "Error during analysis, defaulting to single topic search"
            };

            Sender.Tell(errorResponse);
        }
    }

    private async Task HandleMultiTopicSearchRequest(MultiTopicSearchRequest request)
    {
        _logger.Info("Processing multi-topic search for session {0} with {1} topics",
            request.SessionId, request.Topics.Count);

        try
        {
            var topicResults = new Dictionary<string, List<Models.Memory>>();
            var allMemories = new List<Models.Memory>();
            var seenMemoryIds = new HashSet<Guid>();

            // Search for each topic separately
            foreach (var topic in request.Topics.Take(3)) // Limit to max 3 topics
            {
                _logger.Debug("Searching for topic: {0}", topic);

                // Transform the topic query
                var transformedQuery = await TransformQuery(topic);
                _logger.Debug("Transformed topic query: {0} -> {1}", topic, transformedQuery);

                // Search for memories related to this topic
                var adjustedSimilarity = Math.Min(request.MinSimilarity, 0.2);
                var memories = await _storage.Search(
                    transformedQuery,
                    request.ResultsPerTopic,
                    adjustedSimilarity,
                    null);

                // If no results, try keyword-based search
                if (memories.Count == 0)
                {
                    _logger.Debug("No results for topic {0}, trying keyword search", topic);
                    var keywords = await ExtractKeywords(topic);

                    foreach (var keyword in keywords.Take(2))
                    {
                        if (memories.Count > 0) break;

                        memories = await _storage.Search(
                            keyword,
                            request.ResultsPerTopic,
                            adjustedSimilarity,
                            null);
                    }
                }

                // Store results for this topic
                topicResults[topic] = memories;

                // Add to combined results (avoiding duplicates)
                foreach (var memory in memories)
                {
                    if (!seenMemoryIds.Contains(memory.Id))
                    {
                        allMemories.Add(memory);
                        seenMemoryIds.Add(memory.Id);
                    }
                }

                _logger.Debug("Found {0} memories for topic: {1}", memories.Count, topic);
            }

            _logger.Info("Multi-topic search complete for session {0}: {1} total memories found",
                request.SessionId, allMemories.Count);

            var response = new MultiTopicSearchResponse
            {
                SessionId = request.SessionId,
                OriginalQuery = request.Query,
                TopicResults = topicResults,
                AllMemories = allMemories,
                TopicsSearched = topicResults.Count
            };

            Sender.Tell(response);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error processing multi-topic search for session {0}", request.SessionId);

            var errorResponse = new MultiTopicSearchResponse
            {
                SessionId = request.SessionId,
                OriginalQuery = request.Query,
                TopicResults = new Dictionary<string, List<Models.Memory>>(),
                AllMemories = new List<Models.Memory>(),
                TopicsSearched = 0
            };

            Sender.Tell(errorResponse);
        }
    }

    private (int topicsCount, List<string> topics, string reasoning) ParseAnalyzeQueryTypeResponse(string response)
    {
        try
        {
            var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            int topicsCount = 1;
            var topics = new List<string>();
            string reasoning = "Unable to parse response";

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                if (trimmedLine.StartsWith("TOPICS_COUNT:", StringComparison.OrdinalIgnoreCase))
                {
                    var countStr = trimmedLine.Substring("TOPICS_COUNT:".Length).Trim();
                    if (int.TryParse(countStr, out var count))
                    {
                        topicsCount = Math.Max(1, Math.Min(3, count)); // Clamp between 1 and 3
                    }
                }
                else if (trimmedLine.StartsWith("TOPICS:", StringComparison.OrdinalIgnoreCase))
                {
                    var topicsStr = trimmedLine.Substring("TOPICS:".Length).Trim();
                    topics = topicsStr
                        .Split(',')
                        .Select(t => t.Trim())
                        .Where(t => !string.IsNullOrWhiteSpace(t))
                        .Take(3) // Maximum 3 topics
                        .ToList();
                }
                else if (trimmedLine.StartsWith("REASONING:", StringComparison.OrdinalIgnoreCase))
                {
                    reasoning = trimmedLine.Substring("REASONING:".Length).Trim();
                }
            }

            // If no topics were extracted, default to count of 1
            if (topics.Count == 0)
            {
                topicsCount = 1;
            }

            return (topicsCount, topics, reasoning);
        }
        catch (Exception ex)
        {
            _logger.Warning("Error parsing query type analysis response: {0}", ex.Message);
            return (1, new List<string>(), "Parse error - defaulting to single topic");
        }
    }

    public static Props Props(IStorage storage, ILlmService llmService)
    {
        return Akka.Actor.Props.Create(() => new SearchMemoryActor(storage, llmService));
    }
}