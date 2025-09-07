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
    /// System message for health check operations
    /// </summary>
    public const string HealthCheckSystemMessage = "You are a helpful assistant.";

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
    /// Creates a comprehensive prompt for Neo4j Cypher query generation from natural language
    /// </summary>
    public static string CreateGraphQueryPrompt(string naturalLanguageQuery)
    {
        var prompt = new StringBuilder();
        
        prompt.AppendLine("You are a Neo4j Cypher query expert. Convert the following natural language query to a precise Cypher query.");
        prompt.AppendLine();
        prompt.AppendLine("## Graph Database Schema");
        prompt.AppendLine();
        prompt.AppendLine("### Nodes");
        prompt.AppendLine("1. **Memory Node**");
        prompt.AppendLine("   - id: string (UUID) - unique identifier");
        prompt.AppendLine("   - type: string - values: 'reference', 'how-to', 'system', 'conversation', 'document'");
        prompt.AppendLine("   - source: string - origin of memory (e.g., 'LLM', 'user', 'system')");
        prompt.AppendLine("   - title: string - descriptive title");
        prompt.AppendLine("   - summary: string - detailed content/description");
        prompt.AppendLine("   - tags: string[] - array of keyword tags");
        prompt.AppendLine("   - confidence: double (0.0-1.0) - confidence score");
        prompt.AppendLine("   - createdAt: string (ISO datetime) - creation timestamp");
        prompt.AppendLine();
        prompt.AppendLine("2. **Word Node**");
        prompt.AppendLine("   - name: string - the keyword/word");
        prompt.AppendLine("   - language: string - language code (e.g., 'en', 'ko')");
        prompt.AppendLine("   - frequency: int - usage frequency count");
        prompt.AppendLine("   - createdAt: string (ISO datetime)");
        prompt.AppendLine("   - updatedAt: string (ISO datetime)");
        prompt.AppendLine();
        prompt.AppendLine("### Relationships");
        prompt.AppendLine("1. **RELATES_TO** (Memory -> Memory)");
        prompt.AppendLine("   - type: string - relationship subtype");
        prompt.AppendLine("   - Common types: 'extends', 'enhanced-version', 'supports', 'contradicts', 'implements', 'references', 'related-to', 'example-of', 'explains'");
        prompt.AppendLine("   - weight: double (0.0-1.0) - relationship strength");
        prompt.AppendLine("   - createdAt: string (ISO datetime)");
        prompt.AppendLine();
        prompt.AppendLine("2. **HAS_KEYWORD** (Memory -> Word)");
        prompt.AppendLine("   - relevance: double (0.0-1.0) - keyword relevance to memory");
        prompt.AppendLine("   - createdAt: string (ISO datetime)");
        prompt.AppendLine();
        prompt.AppendLine("## Query Guidelines");
        prompt.AppendLine("1. Use case-insensitive matching with CONTAINS for text searches");
        prompt.AppendLine("2. Always include LIMIT clause (default 50 unless specified)");
        prompt.AppendLine("3. Return nodes and relationships when traversing paths");
        prompt.AppendLine("4. Use DISTINCT when necessary to avoid duplicates");
        prompt.AppendLine("5. For keyword searches, utilize the Word nodes and HAS_KEYWORD relationships");
        prompt.AppendLine("6. Consider multiple search patterns (title, summary, tags) for comprehensive results");
        prompt.AppendLine("7. Use WITH clauses for complex aggregations");
        prompt.AppendLine("8. Order results by relevance when applicable");
        prompt.AppendLine();
        prompt.AppendLine($"## Natural Language Query: {naturalLanguageQuery}");
        prompt.AppendLine();
        prompt.AppendLine("## Comprehensive Examples with Relationship Context");
        prompt.AppendLine();
        prompt.AppendLine("### Type-based Searches with Connections");
        prompt.AppendLine("- \"Find all reference documents\" -> MATCH (m:Memory {type: 'reference'}) OPTIONAL MATCH (m)-[r:RELATES_TO]-(connected:Memory) RETURN m, r, connected LIMIT 50");
        prompt.AppendLine("- \"Show how-to guides\" -> MATCH (m:Memory {type: 'how-to'}) OPTIONAL MATCH (m)-[:HAS_KEYWORD]->(w:Word) RETURN m, w LIMIT 50");
        prompt.AppendLine("- \"Get system memories with their relationships\" -> MATCH (m:Memory {type: 'system'}) OPTIONAL MATCH (m)-[r]-(other) RETURN m, r, other LIMIT 50");
        prompt.AppendLine();
        prompt.AppendLine("### Enhanced Keyword and Tag Searches");
        prompt.AppendLine("- \"Find memories about Docker or Kubernetes\" -> MATCH (m:Memory)-[:HAS_KEYWORD]->(w:Word) WHERE toLower(w.name) IN ['docker', 'kubernetes', 'container', 'k8s', 'containerization'] WITH m, COLLECT(w) as keywords OPTIONAL MATCH (m)-[r:RELATES_TO]-(related:Memory) RETURN m, keywords, r, related LIMIT 50");
        prompt.AppendLine("- \"Show memories related to AI\" -> MATCH (m:Memory) WHERE toLower(m.title) CONTAINS 'ai' OR toLower(m.summary) CONTAINS 'artificial intelligence' OR ANY(tag IN m.tags WHERE toLower(tag) IN ['ai', 'artificial-intelligence', 'machine-learning', 'ml', 'deep-learning']) OPTIONAL MATCH (m)-[r]-(connected) RETURN m, r, connected LIMIT 50");
        prompt.AppendLine("- \"Find SSE or Server-Sent Events\" -> MATCH (m:Memory) WHERE toLower(m.title) CONTAINS 'sse' OR toLower(m.summary) CONTAINS 'server-sent' OR ANY(tag IN m.tags WHERE toLower(tag) CONTAINS 'sse' OR toLower(tag) CONTAINS 'server-sent') RETURN m LIMIT 50");
        prompt.AppendLine("- \"Search for reactive programming\" -> MATCH (m:Memory)-[:HAS_KEYWORD]->(w:Word) WHERE toLower(w.name) CONTAINS 'reactive' OR w.name IN ['rxjs', 'reactor', 'akka', 'flux', 'mono'] WITH m, COLLECT(w) as keywords RETURN m, keywords LIMIT 50");
        prompt.AppendLine();
        prompt.AppendLine("### Rich Relationship Queries");
        prompt.AppendLine("- \"Find memories that extend DDD concepts\" -> MATCH (m1:Memory)-[r:RELATES_TO {type: 'extends'}]->(m2:Memory) WHERE toLower(m2.title) CONTAINS 'ddd' OR toLower(m2.summary) CONTAINS 'domain-driven' OR ANY(tag IN m2.tags WHERE toLower(tag) IN ['ddd', 'domain-driven-design']) WITH m1, r, m2 OPTIONAL MATCH (m1)-[:HAS_KEYWORD]->(w:Word) RETURN m1, r, m2, COLLECT(DISTINCT w) as keywords LIMIT 50");
        prompt.AppendLine("- \"Show enhanced versions with context\" -> MATCH (original:Memory)<-[r:RELATES_TO {type: 'enhanced-version'}]-(enhanced:Memory) WITH original, r, enhanced OPTIONAL MATCH (enhanced)-[:HAS_KEYWORD]->(w:Word) RETURN original, r, enhanced, COLLECT(w) as keywords ORDER BY enhanced.createdAt DESC LIMIT 50");
        prompt.AppendLine("- \"Find examples of patterns\" -> MATCH (example:Memory)-[r:RELATES_TO {type: 'example-of'}]->(pattern:Memory) WHERE toLower(pattern.title) CONTAINS 'pattern' OR toLower(pattern.type) = 'pattern' WITH example, r, pattern OPTIONAL MATCH (example)-[r2]-(other:Memory) WHERE other.id <> pattern.id RETURN example, r, pattern, COLLECT(DISTINCT other) as relatedExamples LIMIT 30");
        prompt.AppendLine("- \"Show memories that support each other\" -> MATCH (m1:Memory)-[r:RELATES_TO {type: 'supports'}]-(m2:Memory) WHERE id(m1) < id(m2) RETURN m1, r, m2 LIMIT 30");
        prompt.AppendLine();
        prompt.AppendLine("### Advanced Analysis Queries");
        prompt.AppendLine("- \"Find the most connected memories\" -> MATCH (m:Memory) WITH m, SIZE([(m)-[]-() | 1]) as degree WHERE degree > 2 OPTIONAL MATCH (m)-[r]-(connected) RETURN m, degree, COLLECT(DISTINCT {node: connected, relationship: type(r)}) as connections ORDER BY degree DESC LIMIT 20");
        prompt.AppendLine("- \"Show most frequent keywords with their memories\" -> MATCH (w:Word)<-[:HAS_KEYWORD]-(m:Memory) WITH w, COUNT(DISTINCT m) as memoryCount, COLLECT(DISTINCT m.title)[..5] as sampleTitles WHERE memoryCount > 1 RETURN w, memoryCount, sampleTitles ORDER BY w.frequency DESC, memoryCount DESC LIMIT 20");
        prompt.AppendLine("- \"Find memories with common keywords\" -> MATCH (m1:Memory)-[:HAS_KEYWORD]->(w:Word)<-[:HAS_KEYWORD]-(m2:Memory) WHERE id(m1) < id(m2) WITH m1, m2, COLLECT(DISTINCT w.name) as common_keywords, COUNT(DISTINCT w) as keyword_count WHERE keyword_count > 2 RETURN m1, m2, common_keywords, keyword_count ORDER BY keyword_count DESC LIMIT 20");
        prompt.AppendLine("- \"Recent high-confidence memories with relationships\" -> MATCH (m:Memory) WHERE m.confidence > 0.8 AND m.createdAt > datetime() - duration('P30D') OPTIONAL MATCH (m)-[r:RELATES_TO]-(related:Memory) RETURN m, COLLECT(DISTINCT {memory: related, type: r.type}) as relationships ORDER BY m.createdAt DESC LIMIT 30");
        prompt.AppendLine();
        prompt.AppendLine("### Complex Graph Patterns");
        prompt.AppendLine("- \"Find reference documents with examples\" -> MATCH (ref:Memory {type: 'reference'})<-[r:RELATES_TO {type: 'example-of'}]-(example:Memory) WITH ref, COLLECT(example) as examples OPTIONAL MATCH (ref)-[:HAS_KEYWORD]->(w:Word) RETURN ref, examples, COLLECT(DISTINCT w) as keywords LIMIT 30");
        prompt.AppendLine("- \"Show knowledge clusters\" -> MATCH path = (m1:Memory)-[:RELATES_TO*1..3]-(m2:Memory) WHERE m1.id <> m2.id WITH m1, m2, path, length(path) as distance RETURN path ORDER BY distance LIMIT 20");
        prompt.AppendLine("- \"Find hub memories\" -> MATCH (m:Memory) WITH m, SIZE([(m)-[:HAS_KEYWORD]->() | 1]) as keywordCount, SIZE([(m)-[:RELATES_TO]-() | 1]) as relationCount WHERE keywordCount > 5 OR relationCount > 3 RETURN m, keywordCount, relationCount, (keywordCount + relationCount * 2) as hubScore ORDER BY hubScore DESC LIMIT 20");
        prompt.AppendLine("- \"Trace relationship chains\" -> MATCH path = (start:Memory)-[:RELATES_TO*1..4]->(end:Memory) WHERE start.type = 'reference' AND end.type = 'example' RETURN path LIMIT 10");
        prompt.AppendLine();
        prompt.AppendLine("### Source and Confidence Analysis");
        prompt.AppendLine("- \"Find LLM-generated memories with connections\" -> MATCH (m:Memory {source: 'LLM'}) OPTIONAL MATCH (m)-[r]-(connected:Memory) WITH m, COLLECT(DISTINCT connected) as connections RETURN m, connections, SIZE(connections) as connectionCount ORDER BY m.createdAt DESC LIMIT 50");
        prompt.AppendLine("- \"High confidence memory network\" -> MATCH (m:Memory) WHERE m.confidence >= 0.9 OPTIONAL MATCH (m)-[r:RELATES_TO]-(related:Memory) WHERE related.confidence >= 0.8 RETURN m, COLLECT(DISTINCT related) as highConfidenceNetwork ORDER BY m.createdAt DESC LIMIT 30");
        prompt.AppendLine("- \"Memory evolution over time\" -> MATCH (m1:Memory)-[r:RELATES_TO {type: 'enhanced-version'}]->(m2:Memory) WHERE m1.createdAt < m2.createdAt RETURN m1, r, m2, duration.between(m1.createdAt, m2.createdAt) as timeDiff ORDER BY timeDiff LIMIT 20");
        prompt.AppendLine();
        prompt.AppendLine("## IMPORTANT INSTRUCTIONS");
        prompt.AppendLine("1. Return ONLY a valid Neo4j Cypher query");
        prompt.AppendLine("2. Do NOT include any explanations, comments, or markdown");
        prompt.AppendLine("3. Do NOT include backticks or code blocks");
        prompt.AppendLine("4. Do NOT include any text before or after the query");
        prompt.AppendLine("5. The response must be directly executable in Neo4j");
        prompt.AppendLine();
        prompt.AppendLine("Generate the Cypher query now:");

        return prompt.ToString();
    }

    /// <summary>
    /// Creates a prompt for analyzing and suggesting relationships between memories
    /// </summary>
    public static string CreateRelationshipAnalysisPrompt(
        string sourceTitle,
        string sourceType,
        string sourceContent,
        List<(Guid id, string title, string type, double similarity)> candidates)
    {
        var prompt = new StringBuilder();
        
        prompt.AppendLine("Analyze the following memory and suggest relationships to other memories.");
        prompt.AppendLine("Source Memory:");
        prompt.AppendLine($"Title: {sourceTitle}");
        prompt.AppendLine($"Type: {sourceType}");
        prompt.AppendLine($"Content: {(sourceContent.Length > 500 ? sourceContent[..500] + "..." : sourceContent)}");
        prompt.AppendLine();
        prompt.AppendLine("Candidate Memories:");
        
        for (int i = 0; i < candidates.Count; i++)
        {
            var candidate = candidates[i];
            prompt.AppendLine($"{i + 1}. Title: {candidate.title}, Type: {candidate.type}, Similarity: {candidate.similarity:F2}");
        }
        
        prompt.AppendLine();
        prompt.AppendLine("For each relevant relationship, suggest a type from:");
        prompt.AppendLine("- extends (extends concepts)");
        prompt.AppendLine("- supports (provides supporting evidence)");
        prompt.AppendLine("- contradicts (presents opposing view)");
        prompt.AppendLine("- implements (practical implementation)");
        prompt.AppendLine("- references (direct reference)");
        prompt.AppendLine("- related-to (general relation)");
        prompt.AppendLine();
        prompt.AppendLine("Return as JSON array with format:");
        prompt.AppendLine("[{\"targetIndex\": 1, \"type\": \"extends\", \"confidence\": 0.8}]");

        return prompt.ToString();
    }
}