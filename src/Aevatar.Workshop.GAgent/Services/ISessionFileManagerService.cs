using Aevatar.Workshop.GAgent.GAgents;

namespace Aevatar.Workshop.GAgent.Services;

/// <summary>
/// Service for managing session files and directories
/// </summary>
public interface ISessionFileManagerService
{
    /// <summary>
    /// Create session directory in the project root
    /// </summary>
    /// <param name="sessionId">Session identifier</param>
    /// <returns>Path to the created directory</returns>
    Task<string> CreateSessionDirectoryAsync(string sessionId);

    /// <summary>
    /// Save theory as markdown file in session directory
    /// </summary>
    /// <param name="sessionId">Session identifier</param>
    /// <param name="theory">Theory to save</param>
    /// <returns>Path to the saved file</returns>
    Task<string> SaveTheoryAsMarkdownAsync(string sessionId, TheoryElement theory);

    /// <summary>
    /// Update theory markdown file
    /// </summary>
    /// <param name="sessionId">Session identifier</param>
    /// <param name="theory">Updated theory</param>
    /// <returns>Path to the updated file</returns>
    Task<string> UpdateTheoryMarkdownAsync(string sessionId, TheoryElement theory);

    /// <summary>
    /// Create session summary markdown file
    /// </summary>
    /// <param name="sessionId">Session identifier</param>
    /// <param name="session">Reasoning session</param>
    /// <returns>Path to the summary file</returns>
    Task<string> CreateSessionSummaryAsync(string sessionId, ReasoningSession session);

    /// <summary>
    /// Get session directory path
    /// </summary>
    /// <param name="sessionId">Session identifier</param>
    /// <returns>Session directory path</returns>
    string GetSessionDirectoryPath(string sessionId);
}