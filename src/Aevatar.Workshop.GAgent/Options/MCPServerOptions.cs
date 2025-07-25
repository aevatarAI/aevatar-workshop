namespace Aevatar.Workshop.GAgent.Options;

/// <summary>
/// MCP Server configuration options
/// </summary>
public class MCPServerOptions
{
    /// <summary>
    /// Dictionary of MCP server configurations, keyed by server name
    /// </summary>
    public Dictionary<string, MCPServerConfig> MCPServers { get; set; } = new();
}

/// <summary>
/// Individual MCP server configuration
/// </summary>
public class MCPServerConfig
{
    /// <summary>
    /// Command to execute the MCP server
    /// </summary>
    public string Command { get; set; } = string.Empty;

    /// <summary>
    /// Arguments for the command
    /// </summary>
    public List<string> Args { get; set; } = new();

    /// <summary>
    /// Environment variables for the server
    /// </summary>
    public Dictionary<string, string>? Env { get; set; }

    /// <summary>
    /// Description of what this server provides
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// URL for server documentation or info
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Initial delay in milliseconds before server is ready
    /// </summary>
    public int InitialDelayMs { get; set; } = 1000;

    /// <summary>
    /// Maximum number of retries if server fails to start
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Whether this server is enabled by default
    /// </summary>
    public bool Enabled { get; set; } = true;
}