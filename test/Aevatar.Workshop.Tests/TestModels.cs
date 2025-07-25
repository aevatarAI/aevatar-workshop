namespace Aevatar.Workshop.Tests;

/// <summary>
/// Test model for MCP Server configuration
/// Mirrors the actual MCPServerConfig from Host project
/// </summary>
public class TestMCPServerConfig
{
    public string Command { get; set; } = string.Empty;
    public List<string> Args { get; set; } = new();
    public Dictionary<string, string>? Env { get; set; }
    public string? Description { get; set; }
    public string? Url { get; set; }
    public int InitialDelayMs { get; set; } = 1000;
    public int MaxRetries { get; set; } = 3;
    public bool Enabled { get; set; } = true;
}