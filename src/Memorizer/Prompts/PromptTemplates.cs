using System.Text;

namespace Memorizer.Prompts;

/// <summary>
/// Centralized prompt templates for LLM operations
/// </summary>
public static class PromptTemplates
{
    /// <summary>
    /// System message for title generation
    /// </summary>
    public const string TitleGenerationSystemMessage = 
        "You are an expert at creating concise, descriptive titles for various types of content.";

    /// <summary>
    /// System message for keyword extraction
    /// </summary>
    public const string KeywordExtractionSystemMessage = 
        "You are an expert at extracting meaningful keywords from text content.";

    /// <summary>
    /// System message for graph query generation
    /// </summary>
    public const string GraphQuerySystemMessage = @"You are an AI assistant that converts natural language queries to Cypher queries for Neo4j graph database.
The database contains Memory nodes with properties: Id, Title, Type, Source, Text (truncated), CreatedAt, UpdatedAt.
Relationships include: RELATED_TO, EXTENDS, IMPLEMENTS, REFERENCES, SIMILAR_TO.
Generate valid Cypher queries that search the graph effectively.
Be creative with pattern matching and use appropriate WHERE clauses for text searches.";

    /// <summary>
    /// Creates a prompt for title generation
    /// </summary>
    public static string CreateTitleGenerationPrompt(
        string content,
        string contentType,
        string[]? existingTags = null,
        int maxTitleLength = 80)
    {
        var prompt = new StringBuilder();
        
        prompt.AppendLine("TASK: Generate a clear, descriptive title for the provided content that captures its main topic and purpose.");
        prompt.AppendLine();
        prompt.AppendLine("GUIDELINES:");
        prompt.AppendLine($"- Maximum title length: {maxTitleLength} characters");
        prompt.AppendLine("- Make it descriptive and searchable");
        prompt.AppendLine("- Capture the main topic or purpose");
        prompt.AppendLine("- Use natural language, avoid generic phrases");
        prompt.AppendLine("- Consider the content type and existing tags for context");
        prompt.AppendLine();
        prompt.AppendLine("CONTENT DETAILS:");
        prompt.AppendLine($"- Type: {contentType}");
        if (existingTags?.Length > 0)
        {
            prompt.AppendLine($"- Tags: {string.Join(", ", existingTags)}");
        }
        prompt.AppendLine($"- Length: {content.Length} characters");
        prompt.AppendLine();
        prompt.AppendLine("CONTENT TO ANALYZE:");
        prompt.AppendLine("```");
        // Truncate content if it's very long to avoid token limits
        var truncatedContent = content.Length > 2000 ? content[..2000] + "..." : content;
        prompt.AppendLine(truncatedContent);
        prompt.AppendLine("```");
        prompt.AppendLine();
        prompt.AppendLine("RESPOND WITH VALID JSON in this exact format:");
        prompt.AppendLine("""
        {
          "title": "Generated title here",
          "reasoning": "Brief explanation of why this title was chosen"
        }
        """);

        return prompt.ToString();
    }

    /// <summary>
    /// Creates a prompt for keyword extraction
    /// </summary>
    public static string CreateKeywordExtractionPrompt(
        string content,
        string contentType,
        int maxKeywords = 10)
    {
        var prompt = new StringBuilder();
        
        prompt.AppendLine("TASK: Extract the most important and relevant keywords from the provided content.");
        prompt.AppendLine();
        prompt.AppendLine("GUIDELINES:");
        prompt.AppendLine($"- Extract up to {maxKeywords} keywords");
        prompt.AppendLine("- Focus on technical terms, concepts, and significant entities");
        prompt.AppendLine("- Include both single words and meaningful multi-word phrases");
        prompt.AppendLine("- Prioritize domain-specific terminology");
        prompt.AppendLine("- Normalize keywords to lowercase");
        prompt.AppendLine("- Avoid common stop words unless they are part of technical terms");
        prompt.AppendLine("- Consider the content type for appropriate keyword selection");
        prompt.AppendLine();
        prompt.AppendLine("CONTENT DETAILS:");
        prompt.AppendLine($"- Type: {contentType}");
        prompt.AppendLine($"- Length: {content.Length} characters");
        prompt.AppendLine();
        prompt.AppendLine("CONTENT TO ANALYZE:");
        prompt.AppendLine("```");
        // Truncate content if it's very long to avoid token limits
        var truncatedContent = content.Length > 3000 ? content[..3000] + "..." : content;
        prompt.AppendLine(truncatedContent);
        prompt.AppendLine("```");
        prompt.AppendLine();
        prompt.AppendLine("RESPOND WITH VALID JSON in this exact format:");
        prompt.AppendLine("""
        {
          "keywords": ["keyword1", "keyword2", "keyword3"],
          "reasoning": "Brief explanation of keyword selection"
        }
        """);

        return prompt.ToString();
    }

    /// <summary>
    /// Creates a prompt for graph query generation
    /// </summary>
    public static string CreateGraphQueryPrompt(string naturalLanguageQuery)
    {
        var prompt = new StringBuilder();
        
        prompt.AppendLine("Convert the following natural language query to a Cypher query:");
        prompt.AppendLine($"\"{naturalLanguageQuery}\"");
        prompt.AppendLine();
        prompt.AppendLine("Guidelines:");
        prompt.AppendLine("- Return only the Cypher query, no explanations");
        prompt.AppendLine("- Use MATCH patterns for traversal");
        prompt.AppendLine("- Use WHERE clauses for filtering");
        prompt.AppendLine("- Use CONTAINS for text searches in Title or Text properties");
        prompt.AppendLine("- Return relevant node properties");
        prompt.AppendLine("- Limit results to 50 unless specified otherwise");
        prompt.AppendLine();
        prompt.AppendLine("Example patterns:");
        prompt.AppendLine("- Find related memories: MATCH (m:Memory)-[:RELATED_TO]-(related:Memory)");
        prompt.AppendLine("- Search by title: MATCH (m:Memory) WHERE m.Title CONTAINS 'search term'");
        prompt.AppendLine("- Find by type: MATCH (m:Memory {Type: 'reference'})");
        prompt.AppendLine("- Complex patterns: MATCH (m:Memory)-[:EXTENDS]->(parent:Memory)-[:IMPLEMENTS]->(interface:Memory)");

        return prompt.ToString();
    }

    /// <summary>
    /// Creates a prompt for relationship suggestion between memories
    /// </summary>
    public static string CreateRelationshipSuggestionPrompt(
        string sourceTitle,
        string sourceText,
        string targetTitle,
        string targetText)
    {
        var prompt = new StringBuilder();
        
        prompt.AppendLine("Analyze these two memories and suggest potential relationships:");
        prompt.AppendLine();
        prompt.AppendLine("SOURCE MEMORY:");
        prompt.AppendLine($"Title: {sourceTitle}");
        prompt.AppendLine($"Content: {(sourceText.Length > 500 ? sourceText[..500] + "..." : sourceText)}");
        prompt.AppendLine();
        prompt.AppendLine("TARGET MEMORY:");
        prompt.AppendLine($"Title: {targetTitle}");
        prompt.AppendLine($"Content: {(targetText.Length > 500 ? targetText[..500] + "..." : targetText)}");
        prompt.AppendLine();
        prompt.AppendLine("Available relationship types:");
        prompt.AppendLine("- RELATED_TO: General relationship between related concepts");
        prompt.AppendLine("- EXTENDS: Source extends or builds upon target");
        prompt.AppendLine("- IMPLEMENTS: Source implements concepts from target");
        prompt.AppendLine("- REFERENCES: Source references or cites target");
        prompt.AppendLine("- SIMILAR_TO: Memories have similar content or concepts");
        prompt.AppendLine();
        prompt.AppendLine("Respond with JSON:");
        prompt.AppendLine("""
        {
          "relationships": [
            {
              "type": "RELATIONSHIP_TYPE",
              "confidence": 0.0,
              "reasoning": "Brief explanation"
            }
          ]
        }
        """);
        prompt.AppendLine();
        prompt.AppendLine("Only suggest relationships with confidence > 0.7");

        return prompt.ToString();
    }
}