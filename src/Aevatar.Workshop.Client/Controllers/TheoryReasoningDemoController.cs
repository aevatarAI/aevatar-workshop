using Aevatar.Core.Abstractions;
using Aevatar.Workshop.GAgent;
using Aevatar.Workshop.GAgent.GAgents;
using Aevatar.Workshop.GAgent.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aevatar.Workshop.Client.Controllers;

/// <summary>
/// Demo controller for the Theory Reasoning Engine system
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TheoryReasoningDemoController : ControllerBase
{
    private readonly IGAgentFactory _gAgentFactory;
    private readonly ILogger<TheoryReasoningDemoController> _logger;

    public TheoryReasoningDemoController(
        IGAgentFactory gAgentFactory,
        ILogger<TheoryReasoningDemoController> logger)
    {
        _gAgentFactory = gAgentFactory;
        _logger = logger;
    }

    /// <summary>
    /// Initialize the Theory Reasoning Engine system
    /// </summary>
    [HttpPost("initialize")]
    public async Task<IActionResult> InitializeSystemAsync([FromBody] InitializeRequest request)
    {
        try
        {
            _logger.LogInformation("Initializing Theory Reasoning Engine system...");

            var coordinator = await _gAgentFactory.GetGAgentAsync<ITheoryReasoningCoordinatorGAgent>(WorkshopReasoningConstants.CoordinatorId);
            var llmSystem = request.SystemLLM ?? "AzureOpenAI"; // Default fallback
            var success = await coordinator.InitializeSystemAsync(llmSystem);

            if (success)
            {
                var report = await coordinator.GenerateSystemReportAsync();
                return Ok(new
                {
                    success = true,
                    message = "Theory Reasoning Engine system initialized successfully",
                    systemReport = report
                });
            }
            else
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Failed to initialize Theory Reasoning Engine system"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing Theory Reasoning Engine system");
            return StatusCode(500, new
            {
                success = false,
                message = "Internal server error during initialization",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Start a reasoning session with specified configuration
    /// </summary>
    [HttpPost("start-reasoning")]
    public async Task<IActionResult> StartReasoningSessionAsync([FromBody] ReasoningSessionRequest request)
    {
        try
        {
            _logger.LogInformation("Starting reasoning session with config: {@Config}", request);

            var coordinator = await _gAgentFactory.GetGAgentAsync<ITheoryReasoningCoordinatorGAgent>(WorkshopReasoningConstants.CoordinatorId);

            var config = new ReasoningSessionConfig
            {
                EnabledReasoningMethods = request.EnabledMethods ?? new List<string> { "deductive", "inductive" },
                MaxIterations = request.MaxIterations ?? 5,
                QualityThreshold = request.QualityThreshold ?? 0.8,
                EnableAutoReview = request.EnableAutoReview ?? true,
                EnableAutoRevision = request.EnableAutoRevision ?? false,
                TargetDomain = request.TargetDomain ?? "mathematical_reasoning",
                Parameters = new Dictionary<string, string>
                {
                    ["SystemLLM"] = request.SystemLLM ?? "OpenAI"
                }
            };

            var sessionId = await coordinator.StartReasoningSessionAsync(config);

            return Ok(new
            {
                success = true,
                sessionId = sessionId,
                message = $"Reasoning session started with ID: {sessionId}",
                config = config
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting reasoning session");
            return StatusCode(500, new
            {
                success = false,
                message = "Failed to start reasoning session",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Get the status of a reasoning session
    /// </summary>
    [HttpGet("session-status/{sessionId}")]
    public async Task<IActionResult> GetSessionStatusAsync(string sessionId)
    {
        try
        {
            var coordinator = await _gAgentFactory.GetGAgentAsync<ITheoryReasoningCoordinatorGAgent>(WorkshopReasoningConstants.CoordinatorId);
            var session = await coordinator.GetSessionStatusAsync(sessionId);

            if (session == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = $"Session {sessionId} not found"
                });
            }

            // Get detailed theory information for display
            var knowledgeAgent = await _gAgentFactory.GetGAgentAsync<ITheoryKnowledgeGAgent>(WorkshopReasoningConstants.KnowledgeAgentId);
            var theorySummaries = new List<object>();

            foreach (var theoryId in session.GeneratedTheoryIds)
            {
                var theory = await knowledgeAgent.GetTheoryAsync(theoryId);
                if (theory != null)
                {
                    var status = session.AcceptedTheoryIds.Contains(theoryId) ? "accepted" :
                        session.RejectedTheoryIds.Contains(theoryId) ? "rejected" : "pending";

                    theorySummaries.Add(new
                    {
                        id = theory.Id,
                        fullId = theory.FullId,
                        type = theory.Type,
                        content = theory.Content.Length > 100
                            ? theory.Content.Substring(0, 100) + "..."
                            : theory.Content,
                        status = status,
                        qualityScore = theory.QualityScore,
                        createdAt = theory.CreatedAt
                    });
                }
            }

            return Ok(new
            {
                success = true,
                session = new
                {
                    sessionId = session.SessionId,
                    status = session.Status,
                    startedAt = session.StartedAt,
                    completedAt = session.CompletedAt,
                    currentIteration = session.CurrentIteration,
                    maxIterations = session.Config.MaxIterations,
                    currentPhase = session.CurrentPhase,
                    completedPhases = session.CompletedPhases,
                    generatedTheories = session.GeneratedTheoryIds.Count,
                    acceptedTheories = session.AcceptedTheoryIds.Count,
                    rejectedTheories = session.RejectedTheoryIds.Count,
                    successRate = session.GeneratedTheoryIds.Count > 0
                        ? (double)session.AcceptedTheoryIds.Count / session.GeneratedTheoryIds.Count
                        : 0.0,
                    theories = theorySummaries
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting session status for {SessionId}", sessionId);
            return StatusCode(500, new
            {
                success = false,
                message = "Failed to get session status",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Get generated theories from a reasoning session
    /// </summary>
    [HttpGet("generated-theories/{sessionId}")]
    public async Task<IActionResult> GetGeneratedTheoriesAsync(string sessionId)
    {
        try
        {
            var coordinator = await _gAgentFactory.GetGAgentAsync<ITheoryReasoningCoordinatorGAgent>(WorkshopReasoningConstants.CoordinatorId);
            var theoryIds = await coordinator.GetGeneratedTheoriesAsync(sessionId);

            // Get detailed theory information
            var knowledgeAgent = await _gAgentFactory.GetGAgentAsync<ITheoryKnowledgeGAgent>(WorkshopReasoningConstants.KnowledgeAgentId);
            var theories = new List<object>();

            foreach (var theoryId in theoryIds)
            {
                var theory = await knowledgeAgent.GetTheoryAsync(theoryId);
                if (theory != null)
                {
                    theories.Add(new
                    {
                        id = theory.Id,
                        fullId = theory.FullId,
                        type = theory.Type,
                        content = theory.Content,
                        formalExpression = theory.FormalExpression,
                        reasoningMethod = theory.ReasoningMethod,
                        qualityScore = theory.QualityScore,
                        isVerified = theory.IsVerified,
                        createdAt = theory.CreatedAt,
                        dependencies = theory.Dependencies
                    });
                }
            }

            return Ok(new
            {
                success = true,
                sessionId = sessionId,
                totalTheories = theories.Count,
                theories = theories
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting generated theories for session {SessionId}", sessionId);
            return StatusCode(500, new
            {
                success = false,
                message = "Failed to get generated theories",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Get system statistics and health status
    /// </summary>
    [HttpGet("system-stats")]
    public async Task<IActionResult> GetSystemStatsAsync()
    {
        try
        {
            var coordinator = await _gAgentFactory.GetGAgentAsync<ITheoryReasoningCoordinatorGAgent>(WorkshopReasoningConstants.CoordinatorId);
            var stats = await coordinator.GetSystemStatsAsync();
            var isHealthy = await coordinator.ValidateSystemHealthAsync();
            var activeSessions = await coordinator.GetActiveSessionsAsync();

            return Ok(new
            {
                success = true,
                stats = stats,
                systemHealth = isHealthy ? "HEALTHY" : "UNHEALTHY",
                activeSessions = activeSessions.Select(s => new
                {
                    sessionId = s.SessionId,
                    status = s.Status,
                    currentIteration = s.CurrentIteration,
                    startedAt = s.StartedAt
                }).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting system stats");
            return StatusCode(500, new
            {
                success = false,
                message = "Failed to get system stats",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Get system initialization status
    /// </summary>
    [HttpGet("system-status")]
    public async Task<IActionResult> GetSystemStatusAsync()
    {
        try
        {
            var coordinator = await _gAgentFactory.GetGAgentAsync<ITheoryReasoningCoordinatorGAgent>(WorkshopReasoningConstants.CoordinatorId);
            var state = await coordinator.GetStateAsync();

            return Ok(new
            {
                success = true,
                status = state.SystemStatus,
                initialized = state.Initialized,
                availableAgents = state.AvailableAgents,
                message = state.Initialized 
                    ? "System is ready for reasoning" 
                    : "System needs initialization"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting system status");
            return StatusCode(500, new
            {
                success = false,
                message = "Failed to get system status",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Get all reasoning sessions (active and completed)
    /// </summary>
    [HttpGet("sessions")]
    public async Task<IActionResult> GetSessionsAsync()
    {
        try
        {
            var coordinator = await _gAgentFactory.GetGAgentAsync<ITheoryReasoningCoordinatorGAgent>(WorkshopReasoningConstants.CoordinatorId);
            var state = await coordinator.GetStateAsync();

            var sessions = new List<object>();

            // Add active sessions
            foreach (var session in state.ActiveSessions)
            {
                sessions.Add(new
                {
                    sessionId = session.SessionId,
                    status = session.Status,
                    startedAt = session.StartedAt,
                    completedAt = session.CompletedAt,
                    currentIteration = session.CurrentIteration,
                    maxIterations = session.Config.MaxIterations,
                    currentPhase = session.CurrentPhase,
                    completedPhases = session.CompletedPhases,
                    generatedTheories = session.GeneratedTheoryIds.Count,
                    acceptedTheories = session.AcceptedTheoryIds.Count,
                    rejectedTheories = session.RejectedTheoryIds.Count,
                    successRate = session.GeneratedTheoryIds.Count > 0
                        ? (double)session.AcceptedTheoryIds.Count / session.GeneratedTheoryIds.Count
                        : 0.0,
                    config = session.Config
                });
            }

            // Add completed sessions
            foreach (var session in state.CompletedSessions)
            {
                sessions.Add(new
                {
                    sessionId = session.SessionId,
                    status = session.Status,
                    startedAt = session.StartedAt,
                    completedAt = session.CompletedAt,
                    currentIteration = session.CurrentIteration,
                    maxIterations = session.Config.MaxIterations,
                    currentPhase = session.CurrentPhase,
                    completedPhases = session.CompletedPhases,
                    generatedTheories = session.GeneratedTheoryIds.Count,
                    acceptedTheories = session.AcceptedTheoryIds.Count,
                    rejectedTheories = session.RejectedTheoryIds.Count,
                    successRate = session.GeneratedTheoryIds.Count > 0
                        ? (double)session.AcceptedTheoryIds.Count / session.GeneratedTheoryIds.Count
                        : 0.0,
                    config = session.Config
                });
            }

            return Ok(new
            {
                success = true,
                totalSessions = sessions.Count,
                activeSessions = state.ActiveSessions.Count,
                completedSessions = state.CompletedSessions.Count,
                sessions = sessions.OrderByDescending(s => ((dynamic)s).startedAt).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sessions");
            return StatusCode(500, new
            {
                success = false,
                message = "Failed to get sessions",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Get all theories from the knowledge base
    /// </summary>
    [HttpGet("theories")]
    public async Task<IActionResult> GetTheoriesAsync()
    {
        try
        {
            var knowledgeAgent = await _gAgentFactory.GetGAgentAsync<ITheoryKnowledgeGAgent>(WorkshopReasoningConstants.KnowledgeAgentId);
            var allTheories = await knowledgeAgent.GetAllTheoriesAsync();

            var theories = allTheories.Select(theory => new
            {
                id = theory.Id,
                fullId = theory.FullId,
                type = theory.Type,
                number = theory.Number,
                content = theory.Content,
                formalExpression = theory.FormalExpression,
                reasoningMethod = theory.ReasoningMethod,
                qualityScore = theory.QualityScore,
                isVerified = theory.IsVerified,
                createdAt = theory.CreatedAt,
                dependencies = theory.Dependencies,
                pythonVerificationCode = theory.PythonCode
            }).ToList();

            return Ok(new
            {
                success = true,
                totalTheories = theories.Count,
                theories = theories.OrderByDescending(t => t.createdAt).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting theories");
            return StatusCode(500, new
            {
                success = false,
                message = "Failed to get theories",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Get detailed information for a specific theory including markdown content
    /// </summary>
    [HttpGet("theory/{theoryId}")]
    public async Task<IActionResult> GetTheoryDetailAsync(string theoryId)
    {
        try
        {
            var knowledgeAgent = await _gAgentFactory.GetGAgentAsync<ITheoryKnowledgeGAgent>(WorkshopReasoningConstants.KnowledgeAgentId);
            var theory = await knowledgeAgent.GetTheoryAsync(theoryId);

            if (theory == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = $"Theory {theoryId} not found"
                });
            }

            // Get file manager service to generate markdown content
            var fileManager = HttpContext.RequestServices.GetRequiredService<ISessionFileManagerService>();
            var markdownContent = GenerateTheoryMarkdown(theory);

            return Ok(new
            {
                success = true,
                theory = new
                {
                    id = theory.Id,
                    fullId = theory.FullId,
                    type = theory.Type,
                    number = theory.Number,
                    content = theory.Content,
                    formalExpression = theory.FormalExpression,
                    pythonCode = theory.PythonCode,
                    reasoningMethod = theory.ReasoningMethod,
                    qualityScore = theory.QualityScore,
                    isVerified = theory.IsVerified,
                    createdAt = theory.CreatedAt,
                    dependencies = theory.Dependencies,
                    metadata = theory.Metadata,
                    markdownContent = markdownContent
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting theory detail for {TheoryId}", theoryId);
            return StatusCode(500, new
            {
                success = false,
                message = "Failed to get theory detail",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Generate markdown content for a theory (similar to SessionFileManagerService)
    /// </summary>
    private string GenerateTheoryMarkdown(TheoryElement theory)
    {
        var sb = new System.Text.StringBuilder();
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

    /// <summary>
    /// Manually trigger theory formalization
    /// </summary>
    [HttpPost("formalize-theory")]
    public async Task<IActionResult> FormalizeTheoryAsync([FromBody] FormalizationRequest request)
    {
        try
        {
            var formalizationAgent = await _gAgentFactory.GetGAgentAsync<IFormalizationAIGAgent>(WorkshopReasoningConstants.FormalizationAgentId);

            // Initialize if not already done
            var llmSystem = request.SystemLLM ?? "OpenAI"; // Default fallback
            await formalizationAgent.InitializeAsync(llmSystem);

            var result = await formalizationAgent.FormalizeTheoryAsync(
                request.TheoryId,
                request.TheoryContent,
                request.TargetTool ?? "sympy"
            );

            return Ok(new
            {
                success = true,
                formalizationResult = new
                {
                    theoryId = result.TheoryId,
                    originalContent = result.OriginalContent,
                    formalExpression = result.FormalExpression,
                    formalizationTool = result.FormalizationTool,
                    isValid = result.IsValid,
                    validationDetails = result.ValidationDetails,
                    confidenceScore = result.ConfidenceScore,
                    symbolicComponents = result.SymbolicComponents,
                    symbolMapping = result.SymbolMapping
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error formalizing theory {TheoryId}", request.TheoryId);
            return StatusCode(500, new
            {
                success = false,
                message = "Failed to formalize theory",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Manually trigger Python verification
    /// </summary>
    [HttpPost("verify-theory")]
    public async Task<IActionResult> VerifyTheoryAsync([FromBody] VerificationRequest request)
    {
        try
        {
            var verificationAgent = await _gAgentFactory.GetGAgentAsync<IPythonVerificationGAgent>(WorkshopReasoningConstants.VerificationAgentId);

            // Initialize if not already done
            await verificationAgent.InitializeAsync();

            var result = await verificationAgent.VerifyTheoryAsync(
                request.TheoryId,
                request.TheoryContent,
                request.FormalExpression
            );

            return Ok(new
            {
                success = true,
                verificationResult = new
                {
                    theoryId = result.TheoryId,
                    pythonCode = result.PythonCode,
                    testsPassed = result.TestsPassed,
                    testResults = result.TestResults,
                    executionTime = result.ExecutionTime,
                    passedTests = result.PassedTests,
                    totalTests = result.TotalTests,
                    testCases = result.TestCases.Select(tc => new
                    {
                        testName = tc.TestName,
                        testDescription = tc.TestDescription,
                        testCode = tc.TestCode
                    }).ToList(),
                    standardOutput = result.StandardOutput,
                    errorOutput = result.ErrorOutput
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying theory {TheoryId}", request.TheoryId);
            return StatusCode(500, new
            {
                success = false,
                message = "Failed to verify theory",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Perform equivalence review for a theory
    /// </summary>
    [HttpPost("review-equivalence")]
    public async Task<IActionResult> ReviewEquivalenceAsync([FromBody] EquivalenceReviewRequest request)
    {
        try
        {
            var reviewAgent = await _gAgentFactory.GetGAgentAsync<IEquivalenceReviewGAgent>(WorkshopReasoningConstants.ReviewAgentId);

            // Initialize if not already done
            var llmSystem = request.SystemLLM ?? "OpenAI"; // Default fallback
            await reviewAgent.InitializeAsync(llmSystem);

            var review = await reviewAgent.PerformEquivalenceReviewAsync(
                request.TheoryId,
                request.TheoryContent,
                request.FormalExpression,
                request.PythonCode
            );

            return Ok(new
            {
                success = true,
                equivalenceReview = new
                {
                    theoryId = review.TheoryId,
                    theoryFormalizationEquivalent = review.TheoryFormalizationEquivalent,
                    formalizationProgramEquivalent = review.FormalizationProgramEquivalent,
                    theoryProgramEquivalent = review.TheoryProgramEquivalent,
                    overallEquivalenceScore = review.OverallEquivalenceScore,
                    equivalenceDetails = review.EquivalenceDetails,
                    discrepancyReports = review.DiscrepancyReports,
                    requiresRevision = review.RequiresRevision,
                    reviewerRecommendation = review.ReviewerRecommendation,
                    reviewedAt = review.ReviewedAt
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reviewing equivalence for theory {TheoryId}", request.TheoryId);
            return StatusCode(500, new
            {
                success = false,
                message = "Failed to review equivalence",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Stop a running reasoning session
    /// </summary>
    [HttpPost("stop-session/{sessionId}")]
    public async Task<IActionResult> StopSessionAsync(string sessionId)
    {
        try
        {
            var coordinator = await _gAgentFactory.GetGAgentAsync<ITheoryReasoningCoordinatorGAgent>(WorkshopReasoningConstants.CoordinatorId);
            var success = await coordinator.StopReasoningSessionAsync(sessionId);

            if (success)
            {
                return Ok(new
                {
                    success = true,
                    message = $"Session {sessionId} stopped successfully"
                });
            }
            else
            {
                return NotFound(new
                {
                    success = false,
                    message = $"Session {sessionId} not found or already stopped"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping session {SessionId}", sessionId);
            return StatusCode(500, new
            {
                success = false,
                message = "Failed to stop session",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Generate a comprehensive system report
    /// </summary>
    [HttpGet("system-report")]
    public async Task<IActionResult> GenerateSystemReportAsync()
    {
        try
        {
            var coordinator = await _gAgentFactory.GetGAgentAsync<ITheoryReasoningCoordinatorGAgent>(WorkshopReasoningConstants.CoordinatorId);
            var report = await coordinator.GenerateSystemReportAsync();

            return Ok(new
            {
                success = true,
                report = report,
                generatedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating system report");
            return StatusCode(500, new
            {
                success = false,
                message = "Failed to generate system report",
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Get reasoning process and AI thinking steps for a session
    /// </summary>
    [HttpGet("session/{sessionId}/thinking-process")]
    public async Task<IActionResult> GetThinkingProcess(string sessionId)
    {
        try
        {
            var coordinator = await _gAgentFactory.GetGAgentAsync<ITheoryReasoningCoordinatorGAgent>(WorkshopReasoningConstants.CoordinatorId);
            var state = await coordinator.GetStateAsync();

            // Find the session
            var session = state.ActiveSessions.FirstOrDefault(s => s.SessionId == sessionId) ??
                          state.CompletedSessions.FirstOrDefault(s => s.SessionId == sessionId);

            if (session == null)
            {
                return NotFound(new { success = false, message = "Session not found" });
            }

            var response = new
            {
                success = true,
                sessionId = session.SessionId,
                status = session.Status,
                currentIteration = session.CurrentIteration,
                currentPhase = session.CurrentPhase,
                startedAt = session.StartedAt,
                completedAt = session.CompletedAt,
                thinkingSteps = session.ReasoningSteps.OrderBy(s => s.Timestamp).Select(step => new
                {
                    stepId = step.StepId,
                    reasoningType = step.ReasoningType,
                    stepType = step.StepType,
                    content = step.Content,
                    reasoning = step.Reasoning,
                    timestamp = step.Timestamp,
                    iteration = step.Iteration,
                    metadata = step.Metadata,
                    initialPrompt = step.InitialPrompt,
                    aiResponse = step.AIResponse
                }).ToList(),
                totalSteps = session.ReasoningSteps.Count,
                stepsByType = session.ReasoningSteps.GroupBy(s => s.StepType)
                    .ToDictionary(g => g.Key, g => g.Count()),
                stepsByReasoningType = session.ReasoningSteps.GroupBy(s => s.ReasoningType)
                    .ToDictionary(g => g.Key, g => g.Count())
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting thinking process for session {SessionId}", sessionId);
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get all thinking steps for all sessions
    /// </summary>
    [HttpGet("thinking-process/all")]
    public async Task<IActionResult> GetAllThinkingProcesses()
    {
        try
        {
            var coordinator = await _gAgentFactory.GetGAgentAsync<ITheoryReasoningCoordinatorGAgent>(WorkshopReasoningConstants.CoordinatorId);
            var state = await coordinator.GetStateAsync();

            var allSessions = state.ActiveSessions.Concat(state.CompletedSessions).ToList();
            var allThinkingSteps = new List<dynamic>();

            foreach (var session in allSessions)
            {
                foreach (var step in session.ReasoningSteps)
                {
                    allThinkingSteps.Add(new
                    {
                        sessionId = session.SessionId,
                        sessionStatus = session.Status,
                        stepId = step.StepId,
                        reasoningType = step.ReasoningType,
                        stepType = step.StepType,
                        content = step.Content,
                        reasoning = step.Reasoning,
                        timestamp = step.Timestamp,
                        iteration = step.Iteration,
                        metadata = step.Metadata,
                        initialPrompt = step.InitialPrompt,
                        aiResponse = step.AIResponse
                    });
                }
            }

            var response = new
            {
                success = true,
                totalSessions = allSessions.Count,
                totalSteps = allThinkingSteps.Count,
                thinkingSteps = allThinkingSteps.OrderBy(s => s.timestamp).ToList(),
                stepsByType = allThinkingSteps.GroupBy(s => s.stepType)
                    .ToDictionary(g => g.Key, g => g.Count()),
                stepsByReasoningType = allThinkingSteps.GroupBy(s => s.reasoningType)
                    .Where(g => !string.IsNullOrEmpty(g.Key))
                    .ToDictionary(g => g.Key, g => g.Count())
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all thinking processes");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }
}

/// <summary>
/// Request model for system initialization
/// </summary>
public class InitializeRequest
{
    public string? SystemLLM { get; set; }
}

/// <summary>
/// Request model for starting a reasoning session
/// </summary>
public class ReasoningSessionRequest
{
    public List<string>? EnabledMethods { get; set; }
    public int? MaxIterations { get; set; }
    public double? QualityThreshold { get; set; }
    public bool? EnableAutoReview { get; set; }
    public bool? EnableAutoRevision { get; set; }
    public string? TargetDomain { get; set; }
    public string? SystemLLM { get; set; }
}

/// <summary>
/// Request model for theory formalization
/// </summary>
public class FormalizationRequest
{
    public string TheoryId { get; set; } = string.Empty;
    public string TheoryContent { get; set; } = string.Empty;
    public string? TargetTool { get; set; }
    public string? SystemLLM { get; set; }
}

/// <summary>
/// Request model for theory verification
/// </summary>
public class VerificationRequest
{
    public string TheoryId { get; set; } = string.Empty;
    public string TheoryContent { get; set; } = string.Empty;
    public string? FormalExpression { get; set; }
    public string? SystemLLM { get; set; }
}


/// <summary>
/// Request model for equivalence review
/// </summary>
public class EquivalenceReviewRequest
{
    public string TheoryId { get; set; } = string.Empty;
    public string TheoryContent { get; set; } = string.Empty;
    public string FormalExpression { get; set; } = string.Empty;
    public string PythonCode { get; set; } = string.Empty;
    public string? SystemLLM { get; set; }
}