namespace Memorizer.Settings;

public sealed class Neo4jSettings
{
    public const string SectionName = "Neo4j";
    
    public string Uri { get; set; } = "bolt://localhost:7687";
    public string User { get; set; } = "neo4j";
    public string Password { get; set; } = "password";
    public string Database { get; set; } = "neo4j";
    public int MaxConnectionPoolSize { get; set; } = 100;
    public TimeSpan ConnectionAcquisitionTimeout { get; set; } = TimeSpan.FromMinutes(1);
}