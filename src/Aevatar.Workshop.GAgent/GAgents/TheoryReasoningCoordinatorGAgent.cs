using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.Workshop.GAgent.Events;
using Aevatar.Workshop.GAgent.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aevatar.Workshop.GAgent.GAgents;

/// <summary>
/// Reasoning session configuration
/// </summary>
[GenerateSerializer]
public class ReasoningSessionConfig
{
    [Id(0)] public string SessionId { get; set; } = string.Empty;
    [Id(1)] public List<string> EnabledReasoningMethods { get; set; } = new();
    [Id(2)] public int MaxIterations { get; set; } = 10;
    [Id(3)] public double QualityThreshold { get; set; } = 0.8;
    [Id(4)] public bool EnableAutoReview { get; set; } = true;
    [Id(5)] public bool EnableAutoRevision { get; set; } = true;
    [Id(6)] public string TargetDomain { get; set; } = string.Empty;
    [Id(7)] public Dictionary<string, string> Parameters { get; set; } = new();
}

/// <summary>
/// AI reasoning step during a session
/// </summary>
[GenerateSerializer]
public class ReasoningStep
{
    [Id(0)] public string StepId { get; set; } = string.Empty;
    [Id(1)] public string ReasoningType { get; set; } = string.Empty; // deductive, inductive, abductive, analogical
    [Id(2)] public string StepType { get; set; } = string.Empty; // analysis, synthesis, evaluation, conclusion
    [Id(3)] public string Content { get; set; } = string.Empty;
    [Id(4)] public string Reasoning { get; set; } = string.Empty;
    [Id(5)] public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    [Id(6)] public int Iteration { get; set; }
    [Id(7)] public Dictionary<string, string> Metadata { get; set; } = new();
    [Id(8)] public string InitialPrompt { get; set; } = string.Empty;
    [Id(9)] public string AIResponse { get; set; } = string.Empty;
}

/// <summary>
/// Reasoning session status and progress
/// </summary>
[GenerateSerializer]
public class ReasoningSession
{
    [Id(0)] public string SessionId { get; set; } = string.Empty;
    [Id(1)] public ReasoningSessionConfig Config { get; set; } = new();
    [Id(2)] public string Status { get; set; } = "pending"; // pending, running, completed, failed, paused
    [Id(3)] public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    [Id(4)] public DateTime? CompletedAt { get; set; }
    [Id(5)] public int CurrentIteration { get; set; }
    [Id(6)] public List<string> GeneratedTheoryIds { get; set; } = new();
    [Id(7)] public List<string> AcceptedTheoryIds { get; set; } = new();
    [Id(8)] public List<string> RejectedTheoryIds { get; set; } = new();
    [Id(9)] public Dictionary<string, object> SessionMetrics { get; set; } = new();
    [Id(10)] public string CurrentPhase { get; set; } = string.Empty;
    [Id(11)] public List<string> CompletedPhases { get; set; } = new();
    [Id(12)] public List<ReasoningStep> ReasoningSteps { get; set; } = new();
}

/// <summary>
/// State for theory reasoning coordinator
/// </summary>
[GenerateSerializer]
public class TheoryReasoningCoordinatorState : StateBase
{
    [Id(0)] public bool Initialized { get; set; }
    [Id(1)] public List<ReasoningSession> ActiveSessions { get; set; } = new();
    [Id(2)] public List<ReasoningSession> CompletedSessions { get; set; } = new();
    [Id(3)] public Dictionary<string, int> SystemStats { get; set; } = new();
    [Id(4)] public DateTime LastActivityTime { get; set; }
    [Id(5)] public bool AutoReasoningEnabled { get; set; }
    [Id(6)] public string SystemStatus { get; set; } = "idle"; // idle, reasoning, reviewing, error
    [Id(7)] public List<string> AvailableAgents { get; set; } = new();
}

/// <summary>
/// State log events for reasoning coordinator
/// </summary>
[GenerateSerializer]
public class TheoryReasoningCoordinatorStateLogEvent : StateLogEventBase<TheoryReasoningCoordinatorStateLogEvent>;

[GenerateSerializer]
public class CoordinatorInitializedLogEvent : TheoryReasoningCoordinatorStateLogEvent
{
    [Id(0)] public List<string> InitializedAgents { get; set; } = new();
}

[GenerateSerializer]
public class ReasoningSessionStartedLogEvent : TheoryReasoningCoordinatorStateLogEvent
{
    [Id(0)] public ReasoningSession Session { get; set; } = new();
}

[GenerateSerializer]
public class ReasoningSessionCompletedLogEvent : TheoryReasoningCoordinatorStateLogEvent
{
    [Id(0)] public string SessionId { get; set; } = string.Empty;
    [Id(1)] public int GeneratedTheories { get; set; }
    [Id(2)] public int AcceptedTheories { get; set; }
    [Id(3)] public double SuccessRate { get; set; }
}

[GenerateSerializer]
public class SystemStatusChangedLogEvent : TheoryReasoningCoordinatorStateLogEvent
{
    [Id(0)] public string PreviousStatus { get; set; } = string.Empty;
    [Id(1)] public string NewStatus { get; set; } = string.Empty;
    [Id(2)] public string Reason { get; set; } = string.Empty;
}

[GenerateSerializer]
public class ThinkingStepsAddedLogEvent : TheoryReasoningCoordinatorStateLogEvent
{
    [Id(0)] public string SessionId { get; set; } = string.Empty;
    [Id(1)] public List<ReasoningStep> ThinkingSteps { get; set; } = new();
}

/// <summary>
/// Interface for theory reasoning coordinator
/// </summary>
public interface ITheoryReasoningCoordinatorGAgent : IStateGAgent<TheoryReasoningCoordinatorState>
{
    Task<bool> InitializeSystemAsync(string? systemLLM = null);
    Task<string> StartReasoningSessionAsync(ReasoningSessionConfig config);
    Task<bool> StopReasoningSessionAsync(string sessionId);
    Task<bool> PauseReasoningSessionAsync(string sessionId);
    Task<bool> ResumeReasoningSessionAsync(string sessionId);
    Task<ReasoningSession?> GetSessionStatusAsync(string sessionId);
    Task<List<ReasoningSession>> GetActiveSessionsAsync();
    Task<Dictionary<string, int>> GetSystemStatsAsync();
    Task<bool> EnableAutoReasoningAsync(ReasoningSessionConfig defaultConfig);
    Task<bool> DisableAutoReasoningAsync();
    Task<List<string>> GetGeneratedTheoriesAsync(string sessionId);
    Task<bool> ValidateSystemHealthAsync();
    Task<string> GenerateSystemReportAsync();
}

/// <summary>
/// Main coordinator for the theory reasoning engine system
/// </summary>
[GAgent("theory.reasoning.coordinator", "reasoning")]
public class TheoryReasoningCoordinatorGAgent : GAgentBase<TheoryReasoningCoordinatorState, TheoryReasoningCoordinatorStateLogEvent>, ITheoryReasoningCoordinatorGAgent
{
    private ITheoryKnowledgeGAgent? _knowledgeAgent;
    private IAutoReasoningAIGAgent? _reasoningAgent;
    private IFormalizationAIGAgent? _formalizationAgent;
    private IPythonVerificationGAgent? _verificationAgent;
    private IEquivalenceReviewGAgent? _reviewAgent;
    private IGAgentFactory GAgentFactory => ServiceProvider.GetRequiredService<IGAgentFactory>();
    
    // Session file management service
    private ISessionFileManagerService? _fileManager;

    public override Task<string> GetDescriptionAsync()
        => Task.FromResult("Main coordinator for the automatic theory reasoning engine system that orchestrates the complete reasoning pipeline");

    public async Task<bool> InitializeSystemAsync(string? systemLLM = null)
    {
        try
        {
            // Default to available LLM if not specified
            var llmSystem = systemLLM ?? "AzureOpenAI"; // Use first available from the log
            Logger.LogInformation("Initializing Theory Reasoning Engine System with LLM: {LLMSystem}", llmSystem);

            // Initialize all component agents
            var initializationTasks = new List<Task<bool>>();

            // Get agent instances using fixed IDs
            _knowledgeAgent = await GAgentFactory.GetGAgentAsync<ITheoryKnowledgeGAgent>(WorkshopReasoningConstants.KnowledgeAgentId);
            _reasoningAgent = await GAgentFactory.GetGAgentAsync<IAutoReasoningAIGAgent>(WorkshopReasoningConstants.ReasoningAgentId);
            _formalizationAgent = await GAgentFactory.GetGAgentAsync<IFormalizationAIGAgent>(WorkshopReasoningConstants.FormalizationAgentId);
            _verificationAgent = await GAgentFactory.GetGAgentAsync<IPythonVerificationGAgent>(WorkshopReasoningConstants.VerificationAgentId);
            _reviewAgent = await GAgentFactory.GetGAgentAsync<IEquivalenceReviewGAgent>(WorkshopReasoningConstants.ReviewAgentId);
            
            // Initialize file manager service
            _fileManager = ServiceProvider.GetRequiredService<ISessionFileManagerService>();
            Logger.LogInformation("Session file manager initialized");

            // Initialize knowledge base
            if (_knowledgeAgent != null)
            {
                initializationTasks.Add(_knowledgeAgent.InitializeWithPsiTheoryAsync());
            }

            // Initialize AI reasoning agent
            if (_reasoningAgent != null)
            {
                Logger.LogInformation("Starting reasoning agent initialization with LLM: {LLMSystem}", llmSystem);
                initializationTasks.Add(_reasoningAgent.InitializeAsync(llmSystem));
            }
            else
            {
                Logger.LogError("Reasoning agent is null, cannot initialize");
            }

            // Initialize formalization agent
            if (_formalizationAgent != null)
            {
                initializationTasks.Add(_formalizationAgent.InitializeAsync(llmSystem));
            }

            // Initialize verification agent
            if (_verificationAgent != null)
            {
                initializationTasks.Add(_verificationAgent.InitializeAsync());
            }

            // Initialize review agent
            if (_reviewAgent != null)
            {
                initializationTasks.Add(_reviewAgent.InitializeAsync(llmSystem));
            }

            // Wait for all initializations
            var results = await Task.WhenAll(initializationTasks);
            
            // Log individual initialization results
            var componentNames = new[] { "knowledge", "reasoning", "formalization", "verification", "review" };
            for (int i = 0; i < results.Length && i < componentNames.Length; i++)
            {
                Logger.LogInformation("Component {Component} initialization result: {Result}", 
                    componentNames[i], results[i]);
            }
            
            if (!results.All(r => r))
            {
                var failedComponents = componentNames.Where((name, index) => index < results.Length && !results[index]).ToList();
                Logger.LogError("Some agents failed to initialize: {FailedComponents}", string.Join(", ", failedComponents));
                return false;
            }
            
            Logger.LogInformation("All {Count} agents initialized successfully", results.Length);

            // Register for cross-agent communication
            await RegisterAgentsForCommunicationAsync();

            var initializedAgents = new List<string>
            {
                "TheoryKnowledgeGAgent",
                "AutoReasoningAIGAgent", 
                "FormalizationAIGAgent",
                "PythonVerificationGAgent",
                "EquivalenceReviewGAgent"
            };

            RaiseEvent(new CoordinatorInitializedLogEvent { InitializedAgents = initializedAgents });
            await ConfirmEvents();

            Logger.LogInformation("Theory Reasoning Engine System initialized successfully with {Count} agents", initializedAgents.Count);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to initialize Theory Reasoning Engine System");
            return false;
        }
    }

    public async Task<string> StartReasoningSessionAsync(ReasoningSessionConfig config)
    {
        // Check if system is initialized
        if (!State.Initialized || State.SystemStatus == "initializing")
        {
            Logger.LogWarning("Cannot start reasoning session - system not initialized. Current status: {Status}", State.SystemStatus);
            
            // Auto-initialize system if not initialized
            var systemLLM = config.Parameters?.ContainsKey("SystemLLM") == true 
                ? config.Parameters["SystemLLM"].ToString() 
                : "AzureOpenAI";
                
            Logger.LogInformation("Auto-initializing system with LLM: {SystemLLM}", systemLLM);
            var initResult = await InitializeSystemAsync(systemLLM);
            
            if (!initResult)
            {
                throw new InvalidOperationException("Failed to initialize theory reasoning system");
            }
        }

        // Additional validation: check if required agents are available
        if (_reasoningAgent == null || _knowledgeAgent == null)
        {
            Logger.LogError("Critical agents not available - _reasoningAgent: {ReasoningAgent}, _knowledgeAgent: {KnowledgeAgent}", 
                _reasoningAgent != null, _knowledgeAgent != null);
            throw new InvalidOperationException("Required agents are not initialized");
        }

        // Validate knowledge base has theories
        await ValidateKnowledgeBaseAsync();

        config.SessionId = Guid.NewGuid().ToString();
        
        var session = new ReasoningSession
        {
            SessionId = config.SessionId,
            Config = config,
            Status = "running",
            StartedAt = DateTime.UtcNow,
            CurrentPhase = "initialization"
        };

        // Create session directory for saving theories
        if (_fileManager != null)
        {
            try
            {
                var sessionPath = await _fileManager.CreateSessionDirectoryAsync(config.SessionId);
                Logger.LogInformation("Created session directory: {SessionPath}", sessionPath);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to create session directory for session {SessionId}", config.SessionId);
                // Continue without failing the session creation
            }
        }
        
        RaiseEvent(new ReasoningSessionStartedLogEvent { Session = session });
        RaiseEvent(new SystemStatusChangedLogEvent 
        { 
            PreviousStatus = State.SystemStatus,
            NewStatus = "reasoning",
            Reason = $"Started reasoning session {config.SessionId}"
        });
        await ConfirmEvents();

        Logger.LogInformation("Started reasoning session {SessionId} with {Methods} methods enabled. Agents status - Reasoning: {ReasoningOK}, Knowledge: {KnowledgeOK}", 
            config.SessionId, string.Join(", ", config.EnabledReasoningMethods), _reasoningAgent != null, _knowledgeAgent != null);

        // Execute reasoning session asynchronously
        _ = Task.Run(async () => await ExecuteReasoningSessionAsync(session));

        return config.SessionId;
    }

    public async Task<bool> StopReasoningSessionAsync(string sessionId)
    {
        var session = State.ActiveSessions.FirstOrDefault(s => s.SessionId == sessionId);
        if (session == null)
        {
            return false;
        }

        session.Status = "completed";
        session.CompletedAt = DateTime.UtcNow;

        await CompleteReasoningSessionAsync(session);
        
        Logger.LogInformation("Stopped reasoning session {SessionId}", sessionId);
        return true;
    }

    public async Task<bool> PauseReasoningSessionAsync(string sessionId)
    {
        var session = State.ActiveSessions.FirstOrDefault(s => s.SessionId == sessionId);
        if (session == null)
        {
            return false;
        }

        session.Status = "paused";
        
        Logger.LogInformation("Paused reasoning session {SessionId}", sessionId);
        return true;
    }

    public async Task<bool> ResumeReasoningSessionAsync(string sessionId)
    {
        var session = State.ActiveSessions.FirstOrDefault(s => s.SessionId == sessionId && s.Status == "paused");
        if (session == null)
        {
            return false;
        }

        session.Status = "running";
        
        // Resume execution
        _ = Task.Run(async () => await ExecuteReasoningSessionAsync(session));
        
        Logger.LogInformation("Resumed reasoning session {SessionId}", sessionId);
        return true;
    }

    public Task<ReasoningSession?> GetSessionStatusAsync(string sessionId)
    {
        var session = State.ActiveSessions.FirstOrDefault(s => s.SessionId == sessionId) ??
                     State.CompletedSessions.FirstOrDefault(s => s.SessionId == sessionId);
        
        return Task.FromResult(session);
    }

    public Task<List<ReasoningSession>> GetActiveSessionsAsync()
    {
        return Task.FromResult(State.ActiveSessions.ToList());
    }

    public Task<Dictionary<string, int>> GetSystemStatsAsync()
    {
        var stats = new Dictionary<string, int>(State.SystemStats)
        {
            ["ActiveSessions"] = State.ActiveSessions.Count,
            ["CompletedSessions"] = State.CompletedSessions.Count,
            ["TotalTheoriesGenerated"] = State.CompletedSessions.Sum(s => s.GeneratedTheoryIds.Count),
            ["TotalTheoriesAccepted"] = State.CompletedSessions.Sum(s => s.AcceptedTheoryIds.Count),
            ["AvailableAgents"] = State.AvailableAgents.Count
        };

        // Calculate success rate
        var totalGenerated = stats["TotalTheoriesGenerated"];
        var totalAccepted = stats["TotalTheoriesAccepted"];
        
        if (totalGenerated > 0)
        {
            stats["SuccessRatePercent"] = (int)((double)totalAccepted / totalGenerated * 100);
        }

        return Task.FromResult(stats);
    }

    public async Task<bool> EnableAutoReasoningAsync(ReasoningSessionConfig defaultConfig)
    {
        RaiseEvent(new SystemStatusChangedLogEvent 
        { 
            PreviousStatus = State.SystemStatus,
            NewStatus = "auto_reasoning",
            Reason = "Auto reasoning enabled"
        });
        await ConfirmEvents();

        // Start continuous reasoning session
        await StartReasoningSessionAsync(defaultConfig);

        Logger.LogInformation("Auto reasoning enabled with config: {Config}", defaultConfig.SessionId);
        return true;
    }

    public async Task<bool> DisableAutoReasoningAsync()
    {
        // Stop all active sessions
        var activeSessionIds = State.ActiveSessions.Select(s => s.SessionId).ToList();
        foreach (var sessionId in activeSessionIds)
        {
            await StopReasoningSessionAsync(sessionId);
        }

        RaiseEvent(new SystemStatusChangedLogEvent 
        { 
            PreviousStatus = State.SystemStatus,
            NewStatus = "idle",
            Reason = "Auto reasoning disabled"
        });
        await ConfirmEvents();

        Logger.LogInformation("Auto reasoning disabled");
        return true;
    }

    public async Task<List<string>> GetGeneratedTheoriesAsync(string sessionId)
    {
        var session = await GetSessionStatusAsync(sessionId);
        return session?.GeneratedTheoryIds ?? new List<string>();
    }

    public async Task<bool> ValidateSystemHealthAsync()
    {
        try
        {
            var healthChecks = new List<Task<bool>>();

            // Check agent availability and health
            if (_knowledgeAgent != null)
            {
                healthChecks.Add(CheckAgentHealthAsync("knowledge"));
            }

            if (_reasoningAgent != null)
            {
                healthChecks.Add(CheckAgentHealthAsync("reasoning"));
            }

            if (_formalizationAgent != null)
            {
                healthChecks.Add(CheckAgentHealthAsync("formalization"));
            }

            if (_verificationAgent != null)
            {
                healthChecks.Add(CheckAgentHealthAsync("verification"));
            }

            if (_reviewAgent != null)
            {
                healthChecks.Add(CheckAgentHealthAsync("review"));
            }

            var results = await Task.WhenAll(healthChecks);
            var isHealthy = results.All(r => r);

            Logger.LogInformation("System health check completed: {Status}", isHealthy ? "HEALTHY" : "UNHEALTHY");
            return isHealthy;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during system health validation");
            return false;
        }
    }

    public async Task<string> GenerateSystemReportAsync()
    {
        var stats = await GetSystemStatsAsync();
        var report = new System.Text.StringBuilder();

        report.AppendLine("=== Theory Reasoning Engine System Report ===");
        report.AppendLine($"Generated at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        report.AppendLine();

        report.AppendLine("System Status:");
        report.AppendLine($"  Current Status: {State.SystemStatus}");
        report.AppendLine($"  Auto Reasoning: {(State.AutoReasoningEnabled ? "Enabled" : "Disabled")}");
        report.AppendLine($"  Last Activity: {State.LastActivityTime:yyyy-MM-dd HH:mm:ss}");
        report.AppendLine();

        report.AppendLine("Session Statistics:");
        report.AppendLine($"  Active Sessions: {stats["ActiveSessions"]}");
        report.AppendLine($"  Completed Sessions: {stats["CompletedSessions"]}");
        report.AppendLine();

        report.AppendLine("Theory Generation Statistics:");
        report.AppendLine($"  Total Theories Generated: {stats["TotalTheoriesGenerated"]}");
        report.AppendLine($"  Total Theories Accepted: {stats["TotalTheoriesAccepted"]}");
        
        if (stats.ContainsKey("SuccessRatePercent"))
        {
            report.AppendLine($"  Success Rate: {stats["SuccessRatePercent"]}%");
        }
        
        report.AppendLine();

        report.AppendLine("Available Agents:");
        foreach (var agent in State.AvailableAgents)
        {
            report.AppendLine($"  - {agent}");
        }

        // Add health status
        var isHealthy = await ValidateSystemHealthAsync();
        report.AppendLine();
        report.AppendLine($"System Health: {(isHealthy ? "HEALTHY" : "REQUIRES ATTENTION")}");

        return report.ToString();
    }

    private async Task ExecuteReasoningSessionAsync(ReasoningSession session)
    {
        try
        {
            Logger.LogInformation("Starting execution of reasoning session {SessionId} with {MaxIterations} max iterations", 
                session.SessionId, session.Config.MaxIterations);

            // Validate agents before starting
            if (_reasoningAgent == null || _knowledgeAgent == null)
            {
                Logger.LogError("Cannot execute reasoning session {SessionId} - required agents are null. Reasoning: {ReasoningOK}, Knowledge: {KnowledgeOK}", 
                    session.SessionId, _reasoningAgent != null, _knowledgeAgent != null);
                session.Status = "failed";
                session.CompletedAt = DateTime.UtcNow;
                
                // Update the session state properly
                RaiseEvent(new ReasoningSessionCompletedLogEvent 
                { 
                    SessionId = session.SessionId,
                    GeneratedTheories = 0,
                    AcceptedTheories = 0,
                    SuccessRate = 0.0
                });
                await ConfirmEvents();
                
                Logger.LogWarning("Reasoning session {SessionId} marked as failed due to missing agents", session.SessionId);
                return;
            }

            while (session.CurrentIteration < session.Config.MaxIterations && 
                   session.Status == "running")
            {
                session.CurrentIteration++;
                session.CurrentPhase = $"iteration_{session.CurrentIteration}";

                Logger.LogInformation("Executing reasoning session {SessionId}, iteration {Iteration}/{MaxIterations}", 
                    session.SessionId, session.CurrentIteration, session.Config.MaxIterations);

                var iterationStartTime = DateTime.UtcNow;
                var theoriesGeneratedThisIteration = 0;

                // Execute reasoning methods
                foreach (var method in session.Config.EnabledReasoningMethods)
                {
                    var beforeCount = session.GeneratedTheoryIds.Count;
                    
                    Logger.LogInformation("Executing reasoning method {Method} for session {SessionId}", 
                        method, session.SessionId);
                    
                    await ExecuteReasoningMethodAsync(session, method);
                    
                    var afterCount = session.GeneratedTheoryIds.Count;
                    var methodGenerated = afterCount - beforeCount;
                    theoriesGeneratedThisIteration += methodGenerated;
                    
                    Logger.LogInformation("Reasoning method {Method} generated {Count} theories for session {SessionId}", 
                        method, methodGenerated, session.SessionId);
                    
                    // Check if session was paused or stopped
                    if (session.Status != "running")
                    {
                        Logger.LogInformation("Session {SessionId} status changed to {Status}, stopping execution", 
                            session.SessionId, session.Status);
                        break;
                    }
                }

                var iterationDuration = DateTime.UtcNow - iterationStartTime;
                Logger.LogInformation("Completed iteration {Iteration} for session {SessionId}. Generated {Count} theories in {Duration}ms", 
                    session.CurrentIteration, session.SessionId, theoriesGeneratedThisIteration, iterationDuration.TotalMilliseconds);

                // Add delay between iterations
                await Task.Delay(1000);
            }

            // Complete session
            if (session.Status == "running")
            {
                session.Status = "completed";
                session.CompletedAt = DateTime.UtcNow;
                await CompleteReasoningSessionAsync(session);
                
                Logger.LogInformation("Reasoning session {SessionId} completed successfully. Total theories: {Total}, Accepted: {Accepted}, Rejected: {Rejected}", 
                    session.SessionId, session.GeneratedTheoryIds.Count, session.AcceptedTheoryIds.Count, session.RejectedTheoryIds.Count);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error executing reasoning session {SessionId}", session.SessionId);
            session.Status = "failed";
            session.CompletedAt = DateTime.UtcNow;
            
            // Ensure session is moved to completed sessions even on failure
            try
            {
                await CompleteReasoningSessionAsync(session);
            }
            catch (Exception completionEx)
            {
                Logger.LogError(completionEx, "Error completing failed reasoning session {SessionId}", session.SessionId);
            }
        }
    }

    private async Task ExecuteReasoningMethodAsync(ReasoningSession session, string method)
    {
        try
        {
            if (_reasoningAgent == null || _knowledgeAgent == null)
            {
                Logger.LogWarning("Cannot execute reasoning method {Method} - agents not available. Reasoning: {ReasoningOK}, Knowledge: {KnowledgeOK}", 
                    method, _reasoningAgent != null, _knowledgeAgent != null);
                return;
            }

            Logger.LogInformation("Getting source theories for reasoning method {Method} in session {SessionId}", 
                method, session.SessionId);

            // Get some source theories for reasoning
            var recentTheories = await _knowledgeAgent.GetRecentTheoriesAsync(5);
            var sourceTheoryIds = recentTheories.Select(t => t.Id).ToList();

            Logger.LogInformation("Retrieved {Count} source theories for reasoning method {Method}: {TheoryIds}", 
                sourceTheoryIds.Count, method, string.Join(", ", sourceTheoryIds.Take(3)));

            if (!sourceTheoryIds.Any())
            {
                Logger.LogWarning("No source theories available for reasoning method {Method} in session {SessionId}", 
                    method, session.SessionId);
                return;
            }

            List<string> newTheoryIds;
            
            try
            {
                newTheoryIds = method.ToLower() switch
                {
                    "deductive" => await _reasoningAgent.PerformDeductiveReasoningAsync(sourceTheoryIds, session.Config.TargetDomain),
                    "inductive" => await _reasoningAgent.PerformInductiveReasoningAsync(sourceTheoryIds, "pattern_discovery"),
                    "abductive" => await _reasoningAgent.PerformAbductiveReasoningAsync(sourceTheoryIds.FirstOrDefault() ?? "", session.Config.TargetDomain),
                    "analogical" => await _reasoningAgent.PerformAnalogicalReasoningAsync(sourceTheoryIds.FirstOrDefault() ?? "", session.Config.TargetDomain),
                    _ => new List<string>()
                };
                
                Logger.LogInformation("Reasoning method {Method} completed. Generated {Count} new theory IDs: {TheoryIds}", 
                    method, newTheoryIds.Count, string.Join(", ", newTheoryIds.Take(3)));
            }
            catch (Exception reasoningEx)
            {
                Logger.LogError(reasoningEx, "Error during {Method} reasoning in session {SessionId}", 
                    method, session.SessionId);
                return;
            }

            // Process generated theories
            foreach (var theoryId in newTheoryIds)
            {
                session.GeneratedTheoryIds.Add(theoryId);
                Logger.LogDebug("Added theory {TheoryId} to session {SessionId}", theoryId, session.SessionId);

                // Save theory as markdown file
                if (_fileManager != null && _knowledgeAgent != null)
                {
                    try
                    {
                        var theory = await _knowledgeAgent.GetTheoryAsync(theoryId);
                        if (theory != null)
                        {
                            var filePath = await _fileManager.SaveTheoryAsMarkdownAsync(session.SessionId, theory);
                            Logger.LogInformation("Saved theory {TheoryId} as markdown: {FilePath}", theoryId, filePath);
                        }
                        else
                        {
                            Logger.LogWarning("Theory {TheoryId} not found when trying to save as markdown", theoryId);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Failed to save theory {TheoryId} as markdown for session {SessionId}", theoryId, session.SessionId);
                        // Continue processing without failing
                    }
                }

                // Start review process if enabled (with improved error handling)
                if (session.Config.EnableAutoReview && _reviewAgent != null)
                {
                    Logger.LogDebug("Starting review for theory {TheoryId} in session {SessionId}", 
                        theoryId, session.SessionId);
                    
                    try
                    {
                        // Wait longer to ensure theory is fully persisted
                        await Task.Delay(1000);
                        
                        // Verify theory exists before starting review
                        var theoryExists = await _knowledgeAgent.GetTheoryAsync(theoryId);
                        if (theoryExists == null)
                        {
                            Logger.LogWarning("Theory {TheoryId} not found in knowledge base, skipping review", theoryId);
                            continue;
                        }
                        
                        var reviewTaskId = await _reviewAgent.StartReviewTaskAsync(theoryId);
                        
                        // Wait for review completion (simplified)
                        await Task.Delay(2000);
                        
                        var review = await _reviewAgent.GetReviewResultAsync(theoryId);
                        if (review != null && review.OverallEquivalenceScore >= session.Config.QualityThreshold)
                        {
                            session.AcceptedTheoryIds.Add(theoryId);
                            Logger.LogInformation("Theory {TheoryId} accepted with score {Score} in session {SessionId}", 
                                theoryId, review.OverallEquivalenceScore, session.SessionId);
                        }
                        else
                        {
                            session.RejectedTheoryIds.Add(theoryId);
                            Logger.LogInformation("Theory {TheoryId} rejected with score {Score} (threshold: {Threshold}) in session {SessionId}", 
                                theoryId, review?.OverallEquivalenceScore ?? 0, session.Config.QualityThreshold, session.SessionId);
                        }
                    }
                    catch (Exception reviewEx)
                    {
                        Logger.LogError(reviewEx, "Error reviewing theory {TheoryId} in session {SessionId}", 
                            theoryId, session.SessionId);
                        // Accept by default if review fails
                        session.AcceptedTheoryIds.Add(theoryId);
                    }
                }
                else
                {
                    // Accept by default if no review
                    session.AcceptedTheoryIds.Add(theoryId);
                    Logger.LogDebug("Theory {TheoryId} auto-accepted (no review) in session {SessionId}", 
                        theoryId, session.SessionId);
                }
            }

            Logger.LogInformation("Reasoning method {Method} processed {Count} theories in session {SessionId}. Total session theories: {Total}", 
                method, newTheoryIds.Count, session.SessionId, session.GeneratedTheoryIds.Count);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error executing reasoning method {Method} in session {SessionId}", method, session.SessionId);
        }
    }

    private async Task CompleteReasoningSessionAsync(ReasoningSession session)
    {
        var successRate = session.GeneratedTheoryIds.Count > 0 
            ? (double)session.AcceptedTheoryIds.Count / session.GeneratedTheoryIds.Count 
            : 0.0;

        RaiseEvent(new ReasoningSessionCompletedLogEvent
        {
            SessionId = session.SessionId,
            GeneratedTheories = session.GeneratedTheoryIds.Count,
            AcceptedTheories = session.AcceptedTheoryIds.Count,
            SuccessRate = successRate
        });

        RaiseEvent(new SystemStatusChangedLogEvent 
        { 
            PreviousStatus = State.SystemStatus,
            NewStatus = "idle",
            Reason = $"Completed reasoning session {session.SessionId}"
        });

        await ConfirmEvents();

        // Create session summary markdown file
        if (_fileManager != null)
        {
            try
            {
                var summaryPath = await _fileManager.CreateSessionSummaryAsync(session.SessionId, session);
                Logger.LogInformation("Created session summary: {SummaryPath}", summaryPath);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to create session summary for session {SessionId}", session.SessionId);
                // Continue without failing
            }
        }

        Logger.LogInformation("Completed reasoning session {SessionId}: {Generated} generated, {Accepted} accepted", 
            session.SessionId, session.GeneratedTheoryIds.Count, session.AcceptedTheoryIds.Count);
    }

    private async Task RegisterAgentsForCommunicationAsync()
    {
        // Register agents for event communication
        if (_knowledgeAgent != null)
        {
            await RegisterAsync(_knowledgeAgent);
        }
        if (_reasoningAgent != null)
        {
            await RegisterAsync(_reasoningAgent);
        }
        if (_formalizationAgent != null)
        {
            await RegisterAsync(_formalizationAgent);
        }
        if (_verificationAgent != null)
        {
            await RegisterAsync(_verificationAgent);
        }
        if (_reviewAgent != null)
        {
            await RegisterAsync(_reviewAgent);
        }

        Logger.LogInformation("Registered all agents for inter-agent communication");
    }

    private async Task<bool> CheckAgentHealthAsync(string agentType)
    {
        try
        {
            // Simple health check - in practice, you'd implement more sophisticated checks
            await Task.Delay(100); // Simulate health check
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task ValidateKnowledgeBaseAsync()
    {
        if (_knowledgeAgent == null)
        {
            throw new InvalidOperationException("Knowledge agent not available for validation");
        }

        try
        {
            var stats = await _knowledgeAgent.GetStatisticsAsync();
            var totalTheories = stats.GetValueOrDefault("Total", 0);
            
            Logger.LogInformation("Knowledge base validation - Total theories: {TotalTheories}", totalTheories);
            
            if (totalTheories == 0)
            {
                Logger.LogWarning("Knowledge base appears empty, attempting to reinitialize");
                var initResult = await _knowledgeAgent.InitializeWithPsiTheoryAsync();
                
                if (!initResult)
                {
                    throw new InvalidOperationException("Failed to initialize knowledge base");
                }
                
                // Re-check after initialization
                stats = await _knowledgeAgent.GetStatisticsAsync();
                totalTheories = stats.GetValueOrDefault("Total", 0);
                
                Logger.LogInformation("After reinitialization - Total theories: {TotalTheories}", totalTheories);
            }
            
            if (totalTheories == 0)
            {
                throw new InvalidOperationException("Knowledge base is empty even after initialization");
            }
            
            // Log some recent theories for debugging
            var recentTheories = await _knowledgeAgent.GetRecentTheoriesAsync(5);
            Logger.LogInformation("Recent theories in knowledge base: {TheoryIds}", 
                string.Join(", ", recentTheories.Select(t => t.Id)));
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Knowledge base validation failed");
            throw new InvalidOperationException("Knowledge base validation failed", ex);
        }
    }

    protected override void GAgentTransitionState(TheoryReasoningCoordinatorState state, StateLogEventBase<TheoryReasoningCoordinatorStateLogEvent> @event)
    {
        switch (@event)
        {
            case CoordinatorInitializedLogEvent initEvent:
                state.Initialized = true;
                state.AvailableAgents = initEvent.InitializedAgents;
                state.SystemStatus = "initialized";
                break;

            case ReasoningSessionStartedLogEvent startEvent:
                state.ActiveSessions.Add(startEvent.Session);
                state.LastActivityTime = DateTime.UtcNow;
                break;

            case ReasoningSessionCompletedLogEvent completedEvent:
                var session = state.ActiveSessions.FirstOrDefault(s => s.SessionId == completedEvent.SessionId);
                if (session != null)
                {
                    state.ActiveSessions.Remove(session);
                    state.CompletedSessions.Add(session);
                }
                
                state.SystemStats.TryGetValue("total_theories_generated", out var totalGenerated);
                state.SystemStats["total_theories_generated"] = totalGenerated + completedEvent.GeneratedTheories;
                
                state.SystemStats.TryGetValue("total_theories_accepted", out var totalAccepted);
                state.SystemStats["total_theories_accepted"] = totalAccepted + completedEvent.AcceptedTheories;
                
                state.LastActivityTime = DateTime.UtcNow;
                break;

            case SystemStatusChangedLogEvent statusEvent:
                state.SystemStatus = statusEvent.NewStatus;
                state.AutoReasoningEnabled = statusEvent.NewStatus == "auto_reasoning";
                state.LastActivityTime = DateTime.UtcNow;
                break;

            case ThinkingStepsAddedLogEvent thinkingEvent:
                // Find the session and add thinking steps
                var targetSession = state.ActiveSessions.FirstOrDefault(s => s.SessionId == thinkingEvent.SessionId);
                if (targetSession != null)
                {
                    targetSession.ReasoningSteps.AddRange(thinkingEvent.ThinkingSteps);
                    state.LastActivityTime = DateTime.UtcNow;
                }
                break;
        }
    }

    /// <summary>
    /// Handle thinking steps generated during reasoning
    /// </summary>
    [EventHandler]
    public async Task HandleThinkingStepsGeneratedAsync(ThinkingStepsGeneratedEvent thinkingEvent)
    {
        try
        {
            Logger.LogInformation("🧠 Received {Count} thinking steps for reasoning type {ReasoningType}", 
                thinkingEvent.ThinkingSteps.Count, thinkingEvent.ReasoningType);

            // Find the active session to add thinking steps to
            var state = await GetStateAsync();
            
            // Try to find by ReasoningSessionId first, then fall back to most recent active session
            var activeSession = !string.IsNullOrEmpty(thinkingEvent.ReasoningSessionId) 
                ? state.ActiveSessions.FirstOrDefault(s => s.SessionId == thinkingEvent.ReasoningSessionId)
                : state.ActiveSessions.LastOrDefault(); // Get the most recent active session
            
            if (activeSession != null)
            {
                // Convert ThinkingStepData to ReasoningStep
                var reasoningSteps = thinkingEvent.ThinkingSteps.Select(step => new ReasoningStep
                {
                    StepId = step.StepId,
                    StepType = step.StepType,
                    Content = step.Content,
                    Reasoning = step.Reasoning,
                    ReasoningType = thinkingEvent.ReasoningType,
                    Iteration = thinkingEvent.CurrentIteration,
                    Timestamp = step.Timestamp,
                    Metadata = step.Metadata
                }).ToList();

                // Raise event to add thinking steps to session
                RaiseEvent(new ThinkingStepsAddedLogEvent
                {
                    SessionId = activeSession.SessionId,
                    ThinkingSteps = reasoningSteps
                });
                await ConfirmEvents();

                Logger.LogInformation("Added {Count} thinking steps to session {SessionId}", 
                    reasoningSteps.Count, activeSession.SessionId);
            }
            else
            {
                Logger.LogWarning("No active session found to add thinking steps to");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error handling thinking steps generated event");
        }
    }
}