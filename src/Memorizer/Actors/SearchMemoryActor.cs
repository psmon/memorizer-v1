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

    public static Props Props(IStorage storage, ILlmService llmService)
    {
        return Akka.Actor.Props.Create(() => new SearchMemoryActor(storage, llmService));
    }
}