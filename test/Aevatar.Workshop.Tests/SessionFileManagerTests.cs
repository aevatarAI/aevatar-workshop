using Aevatar.Workshop.GAgent.GAgents;
using Aevatar.Workshop.GAgent.Services;
using Aevatar.Workshop.TestBase;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit.Abstractions;

namespace Aevatar.Workshop.Tests;

/// <summary>
/// Tests for SessionFileManagerService
/// </summary>
[Collection(ClusterCollection.Name)]
public sealed class SessionFileManagerTests : AevatarWorkshopTestBase<AevatarWorkshopTestModule>
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly ISessionFileManagerService _fileManager;

    public SessionFileManagerTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _fileManager = GetRequiredService<ISessionFileManagerService>();
    }

    [Fact]
    public async Task SessionFileManager_CreateSessionDirectory_ShouldCreateDirectoryAndReadme()
    {
        // Arrange
        var sessionId = $"test-session-{Guid.NewGuid()}";
        _testOutputHelper.WriteLine($"Testing session ID: {sessionId}");

        try
        {
            // Act
            var sessionPath = await _fileManager.CreateSessionDirectoryAsync(sessionId);

            // Assert
            sessionPath.ShouldNotBeNullOrEmpty();
            Directory.Exists(sessionPath).ShouldBeTrue();
            
            var readmePath = Path.Combine(sessionPath, "README.md");
            File.Exists(readmePath).ShouldBeTrue();
            
            var readmeContent = await File.ReadAllTextAsync(readmePath);
            readmeContent.ShouldContain($"# Reasoning Session: {sessionId}");
            readmeContent.ShouldContain("## Overview");
            readmeContent.ShouldContain("## Theory Types");
            
            _testOutputHelper.WriteLine($"✅ Session directory created: {sessionPath}");
            _testOutputHelper.WriteLine($"✅ README.md content preview: {readmeContent.Substring(0, Math.Min(200, readmeContent.Length))}...");
        }
        finally
        {
            // Cleanup
            var sessionPath = _fileManager.GetSessionDirectoryPath(sessionId);
            if (Directory.Exists(sessionPath))
            {
                Directory.Delete(sessionPath, true);
                _testOutputHelper.WriteLine($"🧹 Cleaned up session directory: {sessionPath}");
            }
        }
    }

    [Fact]
    public async Task SessionFileManager_SaveTheoryAsMarkdown_ShouldCreateMarkdownFile()
    {
        // Arrange
        var sessionId = $"test-session-{Guid.NewGuid()}";
        var theory = new TheoryElement
        {
            Id = "T1-1",
            Type = "T",
            Content = "Information conservation theorem: In the binary structure of the universe, information can neither be created nor destroyed, only transformed.",
            FormalExpression = "∀x ∈ U: I(x)_before = I(x)_after",
            PythonCode = "def verify_information_conservation(before, after):\n    return hash(before) == hash(after)",
            QualityScore = 0.95,
            IsVerified = true,
            ReasoningMethod = "deductive",
            CreatedAt = DateTime.UtcNow,
            Dependencies = new List<string> { "A1", "D1-1" },
            Metadata = new Dictionary<string, string>
            {
                { "domain", "information_theory" },
                { "complexity", "medium" }
            }
        };

        try
        {
            // Create session directory first
            await _fileManager.CreateSessionDirectoryAsync(sessionId);

            // Act
            var filePath = await _fileManager.SaveTheoryAsMarkdownAsync(sessionId, theory);

            // Assert
            filePath.ShouldNotBeNullOrEmpty();
            File.Exists(filePath).ShouldBeTrue();
            
            var fileName = Path.GetFileName(filePath);
            fileName.ShouldBe("T1-1_T.md");
            
            var markdownContent = await File.ReadAllTextAsync(filePath);
            markdownContent.ShouldContain("# Theory T1-1: T");
            markdownContent.ShouldContain("**Type**: Theorem");
            markdownContent.ShouldContain("**Quality Score**: 0.95");
            markdownContent.ShouldContain("**Verified**: ✅ Yes");
            markdownContent.ShouldContain("## Content");
            markdownContent.ShouldContain("Information conservation theorem");
            markdownContent.ShouldContain("## Formal Expression");
            markdownContent.ShouldContain("∀x ∈ U: I(x)_before = I(x)_after");
            markdownContent.ShouldContain("## Python Code");
            markdownContent.ShouldContain("def verify_information_conservation");
            markdownContent.ShouldContain("## Dependencies");
            markdownContent.ShouldContain("- A1");
            markdownContent.ShouldContain("- D1-1");
            markdownContent.ShouldContain("## Metadata");
            markdownContent.ShouldContain("- **domain**: information_theory");

            _testOutputHelper.WriteLine($"✅ Theory markdown file created: {filePath}");
            _testOutputHelper.WriteLine($"✅ Markdown content preview:\n{markdownContent.Substring(0, Math.Min(500, markdownContent.Length))}...");
        }
        finally
        {
            // Cleanup
            var sessionPath = _fileManager.GetSessionDirectoryPath(sessionId);
            if (Directory.Exists(sessionPath))
            {
                Directory.Delete(sessionPath, true);
                _testOutputHelper.WriteLine($"🧹 Cleaned up session directory: {sessionPath}");
            }
        }
    }

    [Fact]
    public async Task SessionFileManager_CreateSessionSummary_ShouldCreateSummaryFile()
    {
        // Arrange
        var sessionId = $"test-session-{Guid.NewGuid()}";
        var session = new ReasoningSession
        {
            SessionId = sessionId,
            Status = "completed",
            StartedAt = DateTime.UtcNow.AddMinutes(-30),
            CompletedAt = DateTime.UtcNow,
            CurrentIteration = 5,
            CurrentPhase = "completed",
            Config = new ReasoningSessionConfig
            {
                SessionId = sessionId,
                MaxIterations = 10,
                TargetDomain = "mathematical_foundations",
                QualityThreshold = 0.8,
                EnableAutoReview = true,
                EnabledReasoningMethods = new List<string> { "deductive", "inductive", "abductive" }
            },
            GeneratedTheoryIds = new List<string> { "T1-1", "P2-1", "L3-1" },
            AcceptedTheoryIds = new List<string> { "T1-1", "L3-1" },
            RejectedTheoryIds = new List<string> { "P2-1" },
            CompletedPhases = new List<string> { "initialization", "deductive_reasoning", "validation" }
        };

        try
        {
            // Create session directory first
            await _fileManager.CreateSessionDirectoryAsync(sessionId);

            // Act
            var summaryPath = await _fileManager.CreateSessionSummaryAsync(sessionId, session);

            // Assert
            summaryPath.ShouldNotBeNullOrEmpty();
            File.Exists(summaryPath).ShouldBeTrue();
            
            var fileName = Path.GetFileName(summaryPath);
            fileName.ShouldBe("session_summary.md");
            
            var summaryContent = await File.ReadAllTextAsync(summaryPath);
            summaryContent.ShouldContain("# Reasoning Session Summary");
            summaryContent.ShouldContain($"**Session ID**: {sessionId}");
            summaryContent.ShouldContain("**Status**: completed");
            summaryContent.ShouldContain("**Duration**: 30.0 minutes");
            summaryContent.ShouldContain("**Max Iterations**: 10");
            summaryContent.ShouldContain("**Target Domain**: mathematical_foundations");
            summaryContent.ShouldContain("**Generated Theories**: 3");
            summaryContent.ShouldContain("**Accepted Theories**: 2");
            summaryContent.ShouldContain("**Rejected Theories**: 1");
            summaryContent.ShouldContain("**Success Rate**: 66.7%");
            summaryContent.ShouldContain("## Generated Theories");
            summaryContent.ShouldContain("- [T1-1]");
            summaryContent.ShouldContain("✅ Accepted");
            summaryContent.ShouldContain("❌ Rejected");

            _testOutputHelper.WriteLine($"✅ Session summary created: {summaryPath}");
            _testOutputHelper.WriteLine($"✅ Summary content preview:\n{summaryContent.Substring(0, Math.Min(600, summaryContent.Length))}...");
        }
        finally
        {
            // Cleanup
            var sessionPath = _fileManager.GetSessionDirectoryPath(sessionId);
            if (Directory.Exists(sessionPath))
            {
                Directory.Delete(sessionPath, true);
                _testOutputHelper.WriteLine($"🧹 Cleaned up session directory: {sessionPath}");
            }
        }
    }

    [Fact]
    public void SessionFileManager_GetSessionDirectoryPath_ShouldReturnCorrectPath()
    {
        // Arrange
        var sessionId = "test-session-12345";

        // Act
        var sessionPath = _fileManager.GetSessionDirectoryPath(sessionId);

        // Assert
        sessionPath.ShouldNotBeNullOrEmpty();
        sessionPath.ShouldEndWith(sessionId);
        Path.GetFileName(sessionPath).ShouldBe(sessionId);
        
        _testOutputHelper.WriteLine($"✅ Session path: {sessionPath}");
    }
}