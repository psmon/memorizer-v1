namespace Memorizer.Models;

public class CustomScript
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string ScriptContent { get; set; } = null!;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateCustomScriptRequest
{
    public string Name { get; set; } = null!;
    public string ScriptContent { get; set; } = null!;
    public bool IsActive { get; set; } = true;
}

public class UpdateCustomScriptRequest
{
    public string ScriptContent { get; set; } = null!;
    public bool IsActive { get; set; }
}