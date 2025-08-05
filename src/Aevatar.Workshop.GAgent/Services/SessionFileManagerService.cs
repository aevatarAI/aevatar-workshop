using System.Text;
using Microsoft.Extensions.Logging;
using Aevatar.Workshop.GAgent.GAgents;

namespace Aevatar.Workshop.GAgent.Services;

/// <summary>
/// Implementation of session file manager service
/// </summary>
public class SessionFileManagerService : ISessionFileManagerService
{
    private readonly ILogger<SessionFileManagerService> _logger;
    private readonly string _projectRootPath;

    public SessionFileManagerService(ILogger<SessionFileManagerService> logger)
    {
        _logger = logger;
        
        // Find project root directory (where .sln file is located)
        _projectRootPath = FindProjectRoot();
        _logger.LogInformation("Session file manager initialized. Project root: {ProjectRoot}", _projectRootPath);
    }

    public async Task<string> CreateSessionDirectoryAsync(string sessionId)
    {
        var sessionPath = GetSessionDirectoryPath(sessionId);
        
        try
        {
            if (!Directory.Exists(sessionPath))
            {
                Directory.CreateDirectory(sessionPath);
                _logger.LogInformation("Created session directory: {SessionPath}", sessionPath);
                
                // Create README.md for the session
                var readmePath = Path.Combine(sessionPath, "README.md");
                var readmeContent = GenerateSessionReadme(sessionId);
                await File.WriteAllTextAsync(readmePath, readmeContent, Encoding.UTF8);
            }
            
            return sessionPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create session directory: {SessionPath}", sessionPath);
            throw;
        }
    }

    public async Task<string> SaveTheoryAsMarkdownAsync(string sessionId, TheoryElement theory)
    {
        var sessionPath = GetSessionDirectoryPath(sessionId);
        var fileName = $"{theory.Id}_{theory.Type}.md";
        var filePath = Path.Combine(sessionPath, fileName);

        try
        {
            var markdownContent = GenerateTheoryMarkdown(theory);
            await File.WriteAllTextAsync(filePath, markdownContent, Encoding.UTF8);
            
            _logger.LogInformation("Saved theory {TheoryId} to: {FilePath}", theory.Id, filePath);
            return filePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save theory {TheoryId} to markdown: {FilePath}", theory.Id, filePath);
            throw;
        }
    }

    public async Task<string> UpdateTheoryMarkdownAsync(string sessionId, TheoryElement theory)
    {
        // For updates, we append to the existing file with version information
        var sessionPath = GetSessionDirectoryPath(sessionId);
        var fileName = $"{theory.Id}_{theory.Type}.md";
        var filePath = Path.Combine(sessionPath, fileName);

        try
        {
            var updateContent = GenerateTheoryUpdateMarkdown(theory);
            
            if (File.Exists(filePath))
            {
                await File.AppendAllTextAsync(filePath, updateContent, Encoding.UTF8);
            }
            else
            {
                // If file doesn't exist, create it
                return await SaveTheoryAsMarkdownAsync(sessionId, theory);
            }
            
            _logger.LogInformation("Updated theory {TheoryId} markdown: {FilePath}", theory.Id, filePath);
            return filePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update theory {TheoryId} markdown: {FilePath}", theory.Id, filePath);
            throw;
        }
    }

    public async Task<string> CreateSessionSummaryAsync(string sessionId, ReasoningSession session)
    {
        var sessionPath = GetSessionDirectoryPath(sessionId);
        var fileName = "session_summary.md";
        var filePath = Path.Combine(sessionPath, fileName);

        try
        {
            var summaryContent = GenerateSessionSummaryMarkdown(session);
            await File.WriteAllTextAsync(filePath, summaryContent, Encoding.UTF8);
            
            _logger.LogInformation("Created session summary: {FilePath}", filePath);
            return filePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create session summary: {FilePath}", filePath);
            throw;
        }
    }

    public string GetSessionDirectoryPath(string sessionId)
    {
        // Add prefix to clearly identify reasoning session directories
        var prefixedSessionId = $"reasoning-session-{sessionId}";
        return Path.Combine(_projectRootPath, prefixedSessionId);
    }

    private string FindProjectRoot()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var directory = new DirectoryInfo(currentDirectory);

        // Search up the directory tree for .sln file
        while (directory != null)
        {
            if (directory.GetFiles("*.sln").Any())
            {
                return directory.FullName;
            }
            directory = directory.Parent;
        }

        // Fallback to current directory if .sln not found
        _logger.LogWarning("Could not find .sln file, using current directory as project root: {CurrentDirectory}", currentDirectory);
        return currentDirectory;
    }

    private string GenerateSessionReadme(string sessionId)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# Reasoning Session: {sessionId}");
        sb.AppendLine();
        sb.AppendLine($"Generated on: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine();
        sb.AppendLine("## Overview");
        sb.AppendLine("This directory contains all theories generated during this reasoning session.");
        sb.AppendLine();
        sb.AppendLine("## File Structure");
        sb.AppendLine("- `session_summary.md` - Summary of the entire reasoning session");
        sb.AppendLine("- `{TheoryId}_{Type}.md` - Individual theory files");
        sb.AppendLine();
        sb.AppendLine("## Theory Types");
        sb.AppendLine("- **A**: Axioms - Fundamental assumptions");
        sb.AppendLine("- **C**: Corollaries - Direct consequences");
        sb.AppendLine("- **D**: Definitions - Formal definitions");
        sb.AppendLine("- **L**: Lemmas - Supporting propositions");
        sb.AppendLine("- **P**: Propositions - General statements");
        sb.AppendLine("- **T**: Theorems - Proven statements");
        sb.AppendLine();
        
        return sb.ToString();
    }

    private string GenerateTheoryMarkdown(TheoryElement theory)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# Theory {theory.Id}: {theory.Type}");
        sb.AppendLine();
        sb.AppendLine($"**Created**: {theory.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"**Type**: {GetTheoryTypeName(theory.Type)}");
        sb.AppendLine($"**Quality Score**: {theory.QualityScore:F2}");
        sb.AppendLine($"**Verified**: {(theory.IsVerified ? "✅ Yes" : "❌ No")}");
        sb.AppendLine($"**Reasoning Method**: {theory.ReasoningMethod}");
        sb.AppendLine();

        if (!string.IsNullOrEmpty(theory.Content))
        {
            sb.AppendLine("## Content");
            sb.AppendLine(theory.Content);
            sb.AppendLine();
        }

        if (!string.IsNullOrEmpty(theory.FormalExpression))
        {
            sb.AppendLine("## Formal Expression");
            sb.AppendLine("```");
            sb.AppendLine(theory.FormalExpression);
            sb.AppendLine("```");
            sb.AppendLine();
        }

        if (!string.IsNullOrEmpty(theory.PythonCode))
        {
            sb.AppendLine("## Python Code");
            sb.AppendLine("```python");
            sb.AppendLine(theory.PythonCode);
            sb.AppendLine("```");
            sb.AppendLine();
        }

        if (theory.Dependencies?.Any() == true)
        {
            sb.AppendLine("## Dependencies");
            foreach (var dep in theory.Dependencies)
            {
                sb.AppendLine($"- {dep}");
            }
            sb.AppendLine();
        }

        if (theory.Metadata?.Any() == true)
        {
            sb.AppendLine("## Metadata");
            foreach (var meta in theory.Metadata)
            {
                sb.AppendLine($"- **{meta.Key}**: {meta.Value}");
            }
            sb.AppendLine();
        }

        sb.AppendLine("---");
        sb.AppendLine($"*Generated by Aevatar Theory Reasoning Engine*");
        sb.AppendLine();

        return sb.ToString();
    }

    private string GenerateTheoryUpdateMarkdown(TheoryElement theory)
    {
        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine($"## Update - {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"**Quality Score**: {theory.QualityScore:F2}");
        sb.AppendLine($"**Verified**: {(theory.IsVerified ? "✅ Yes" : "❌ No")}");
        sb.AppendLine();

        return sb.ToString();
    }

    private string GenerateSessionSummaryMarkdown(ReasoningSession session)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# Reasoning Session Summary");
        sb.AppendLine();
        sb.AppendLine($"**Session ID**: {session.SessionId}");
        sb.AppendLine($"**Started**: {session.StartedAt:yyyy-MM-dd HH:mm:ss} UTC");
        if (session.CompletedAt.HasValue)
        {
            sb.AppendLine($"**Completed**: {session.CompletedAt.Value:yyyy-MM-dd HH:mm:ss} UTC");
            var duration = session.CompletedAt.Value - session.StartedAt;
            sb.AppendLine($"**Duration**: {duration.TotalMinutes:F1} minutes");
        }
        sb.AppendLine($"**Status**: {session.Status}");
        sb.AppendLine($"**Current Iteration**: {session.CurrentIteration}");
        sb.AppendLine($"**Current Phase**: {session.CurrentPhase}");
        sb.AppendLine();

        sb.AppendLine("## Configuration");
        sb.AppendLine($"**Max Iterations**: {session.Config.MaxIterations}");
        sb.AppendLine($"**Target Domain**: {session.Config.TargetDomain}");
        sb.AppendLine($"**Quality Threshold**: {session.Config.QualityThreshold}");
        sb.AppendLine($"**Auto Review**: {session.Config.EnableAutoReview}");
        sb.AppendLine($"**Enabled Methods**: {string.Join(", ", session.Config.EnabledReasoningMethods)}");
        sb.AppendLine();

        sb.AppendLine("## Results");
        sb.AppendLine($"**Generated Theories**: {session.GeneratedTheoryIds.Count}");
        sb.AppendLine($"**Accepted Theories**: {session.AcceptedTheoryIds.Count}");
        sb.AppendLine($"**Rejected Theories**: {session.RejectedTheoryIds.Count}");
        
        if (session.GeneratedTheoryIds.Count > 0)
        {
            var successRate = (double)session.AcceptedTheoryIds.Count / session.GeneratedTheoryIds.Count * 100;
            sb.AppendLine($"**Success Rate**: {successRate:F1}%");
        }
        sb.AppendLine();

        if (session.GeneratedTheoryIds.Any())
        {
            sb.AppendLine("## Generated Theories");
            foreach (var theoryId in session.GeneratedTheoryIds)
            {
                var status = session.AcceptedTheoryIds.Contains(theoryId) ? "✅ Accepted" :
                           session.RejectedTheoryIds.Contains(theoryId) ? "❌ Rejected" : "⏳ Pending";
                sb.AppendLine($"- [{theoryId}]({theoryId}*.md) - {status}");
            }
            sb.AppendLine();
        }

        if (session.CompletedPhases?.Any() == true)
        {
            sb.AppendLine("## Completed Phases");
            foreach (var phase in session.CompletedPhases)
            {
                sb.AppendLine($"- {phase}");
            }
            sb.AppendLine();
        }

        sb.AppendLine("---");
        sb.AppendLine($"*Generated by Aevatar Theory Reasoning Engine*");
        sb.AppendLine();

        return sb.ToString();
    }

    private string GetTheoryTypeName(string type)
    {
        return type.ToUpper() switch
        {
            "A" => "Axiom",
            "C" => "Corollary", 
            "D" => "Definition",
            "L" => "Lemma",
            "P" => "Proposition",
            "T" => "Theorem",
            _ => "Unknown"
        };
    }
}