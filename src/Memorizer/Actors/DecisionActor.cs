using Akka.Actor;
using Akka.Event;
using Memorizer.Services;
using System.Text;

namespace Memorizer.Actors;

/// <summary>
/// Actor responsible for evaluating the relevance of search results to user queries
/// </summary>
public sealed class DecisionActor : ReceiveActor
{
    private readonly ILlmService _llmService;
    private readonly ILoggingAdapter _logger;

    // Prompt for relevance evaluation
    private const string RelevanceEvaluationPrompt = @"
You are evaluating whether the following search results are relevant to answer the user's query.
BE INCLUSIVE - if the memory contains ANY information about the topic, consider it relevant.

User Query: {0}

Search Results:
{1}

Analyze each search result and determine:
1. Does the memory contain ANY information about the topic mentioned in the query?
2. Could this information be useful for answering the question, even partially?
3. Is the memory about the same general subject area?

For technical queries (like 'Reactive Streams', 'Docker', 'Kubernetes', etc.),
if a memory contains information about that technology, it IS relevant.

Respond in the following format:
RELEVANT: YES or NO
REASONING: Brief explanation of your decision
RELEVANT_IDS: Comma-separated list of relevant memory IDs (if any)";

    public DecisionActor(ILlmService llmService)
    {
        _llmService = llmService;
        _logger = Context.GetLogger();

        ReceiveAsync<EvaluateRelevanceRequest>(HandleEvaluateRelevanceRequest);
    }

    private async Task HandleEvaluateRelevanceRequest(EvaluateRelevanceRequest request)
    {
        _logger.Info("Evaluating relevance for session {0} with {1} memories",
            request.SessionId, request.Memories.Count);

        try
        {
            // If no memories to evaluate, return no relevant memories
            if (request.Memories.Count == 0)
            {
                var noMemoriesResponse = new EvaluateRelevanceResponse
                {
                    SessionId = request.SessionId,
                    HasRelevantMemories = false,
                    RelevantMemories = null,
                    Reasoning = "No memories found to evaluate"
                };
                Sender.Tell(noMemoriesResponse);
                return;
            }

            // Format memories for evaluation
            var memoriesText = FormatMemoriesForEvaluation(request.Memories);

            // Evaluate relevance using LLM
            var prompt = string.Format(RelevanceEvaluationPrompt, request.Query, memoriesText);
            var llmResponse = await _llmService.CompleteAsync(prompt);

            // Parse LLM response
            var (hasRelevant, reasoning, relevantIds) = ParseRelevanceResponse(llmResponse);

            // Filter relevant memories
            List<Models.Memory>? relevantMemories = null;
            if (hasRelevant && relevantIds.Count > 0)
            {
                relevantMemories = request.Memories
                    .Where(m => relevantIds.Contains(m.Id))
                    .ToList();

                _logger.Debug("Found {0} relevant memories out of {1}",
                    relevantMemories.Count, request.Memories.Count);
            }

            var response = new EvaluateRelevanceResponse
            {
                SessionId = request.SessionId,
                HasRelevantMemories = hasRelevant,
                RelevantMemories = relevantMemories,
                Reasoning = reasoning
            };

            Sender.Tell(response);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error evaluating relevance for session {0}", request.SessionId);

            // On error, conservatively assume all memories might be relevant
            var errorResponse = new EvaluateRelevanceResponse
            {
                SessionId = request.SessionId,
                HasRelevantMemories = request.Memories.Count > 0,
                RelevantMemories = request.Memories,
                Reasoning = "Error during evaluation, including all results"
            };

            Sender.Tell(errorResponse);
        }
    }

    private string FormatMemoriesForEvaluation(List<Models.Memory> memories)
    {
        var sb = new StringBuilder();

        for (int i = 0; i < memories.Count; i++)
        {
            var memory = memories[i];
            sb.AppendLine($"--- Result {i + 1} (ID: {memory.Id}) ---");
            sb.AppendLine($"Type: {memory.Type}");
            sb.AppendLine($"Title: {memory.Title ?? "Untitled"}");

            // Include a snippet of the content (first 500 chars)
            var contentSnippet = memory.Text.Length > 500
                ? memory.Text.Substring(0, 500) + "..."
                : memory.Text;
            sb.AppendLine($"Content: {contentSnippet}");

            if (memory.Tags != null && memory.Tags.Length > 0)
            {
                sb.AppendLine($"Tags: {string.Join(", ", memory.Tags)}");
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }

    private (bool hasRelevant, string reasoning, List<Guid> relevantIds) ParseRelevanceResponse(string response)
    {
        try
        {
            var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            bool hasRelevant = false;
            string reasoning = "Unable to determine relevance";
            var relevantIds = new List<Guid>();

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                if (trimmedLine.StartsWith("RELEVANT:", StringComparison.OrdinalIgnoreCase))
                {
                    var value = trimmedLine.Substring("RELEVANT:".Length).Trim();
                    hasRelevant = value.Equals("YES", StringComparison.OrdinalIgnoreCase);
                }
                else if (trimmedLine.StartsWith("REASONING:", StringComparison.OrdinalIgnoreCase))
                {
                    reasoning = trimmedLine.Substring("REASONING:".Length).Trim();
                }
                else if (trimmedLine.StartsWith("RELEVANT_IDS:", StringComparison.OrdinalIgnoreCase))
                {
                    var idsString = trimmedLine.Substring("RELEVANT_IDS:".Length).Trim();
                    if (!string.IsNullOrWhiteSpace(idsString))
                    {
                        var idStrings = idsString.Split(',', StringSplitOptions.RemoveEmptyEntries);
                        foreach (var idStr in idStrings)
                        {
                            if (Guid.TryParse(idStr.Trim(), out var id))
                            {
                                relevantIds.Add(id);
                            }
                        }
                    }
                }
            }

            return (hasRelevant, reasoning, relevantIds);
        }
        catch (Exception ex)
        {
            _logger.Warning("Error parsing relevance response: {0}", ex.Message);
            // Default to considering results relevant if parsing fails
            return (true, "Parse error - including results", new List<Guid>());
        }
    }

    public static Props Props(ILlmService llmService)
    {
        return Akka.Actor.Props.Create(() => new DecisionActor(llmService));
    }
}