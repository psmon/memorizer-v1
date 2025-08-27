namespace Memorizer.Models;

public class GraphMemoryNode
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<string> Tags { get; set; } = new();
    public string? Summary { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class GraphRelationship
{
    public Guid FromId { get; set; }
    public Guid ToId { get; set; }
    public string Type { get; set; } = string.Empty;
    public double Weight { get; set; } = 1.0;
    public DateTime CreatedAt { get; set; }
    public Dictionary<string, object> Properties { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class GraphSearchResult
{
    public List<GraphMemoryNode> Nodes { get; set; } = new();
    public List<GraphRelationship> Relationships { get; set; } = new();
}

public class GraphVisualizationData
{
    public List<GraphNode> Nodes { get; set; } = new();
    public List<GraphEdge> Edges { get; set; } = new();
}

public class GraphNode
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Color { get; set; } = "#6366f1";
    public int Size { get; set; } = 10;
    public Dictionary<string, object> Data { get; set; } = new();
}

public class GraphEdge
{
    public string Id { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Color { get; set; } = "#94a3b8";
    public double Weight { get; set; } = 1.0;
}

public class NodeWord
{
    public string Name { get; set; } = string.Empty;
    public string Language { get; set; } = "en";
    public int Frequency { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<Guid> ConnectedMemories { get; set; } = new();
}

public class MemoryToWordRelationship
{
    public Guid MemoryId { get; set; }
    public string WordName { get; set; } = string.Empty;
    public double Relevance { get; set; } = 1.0;
    public DateTime CreatedAt { get; set; }
}