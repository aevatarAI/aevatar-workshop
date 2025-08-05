using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.AIGAgent.State;
using Aevatar.Workshop.GAgent.Events;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Aevatar.Workshop.GAgent.GAgents;

/// <summary>
/// AI thinking step during reasoning
/// </summary>
[GenerateSerializer]
public class ThinkingStep
{
    [Id(0)] public string StepId { get; set; } = string.Empty;
    [Id(1)] public string StepType { get; set; } = string.Empty; // analysis, synthesis, evaluation, conclusion
    [Id(2)] public string Content { get; set; } = string.Empty;
    [Id(3)] public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    [Id(4)] public string Reasoning { get; set; } = string.Empty;
    [Id(5)] public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// Reasoning task definition
/// </summary>
[GenerateSerializer]
public class ReasoningTask
{
    [Id(0)] public string TaskId { get; set; } = string.Empty;
    [Id(1)] public string TaskType { get; set; } = string.Empty; // deductive, inductive, abductive, analogical
    [Id(2)] public string TargetDomain { get; set; } = string.Empty;
    [Id(3)] public List<string> SourceTheoryIds { get; set; } = new();
    [Id(4)] public string ReasoningGoal { get; set; } = string.Empty;
    [Id(5)] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Id(6)] public string Status { get; set; } = "pending"; // pending, running, completed, failed
    [Id(7)] public List<string> GeneratedTheoryIds { get; set; } = new();
    [Id(8)] public string PromptTemplate { get; set; } = string.Empty;
    [Id(9)] public List<ThinkingStep> ThinkingSteps { get; set; } = new();
    [Id(10)] public string InitialPrompt { get; set; } = string.Empty;
    [Id(11)] public string FinalResponse { get; set; } = string.Empty;
}

/// <summary>
/// State for auto reasoning AI agent
/// </summary>
[GenerateSerializer]
public class AutoReasoningState : AIGAgentStateBase
{
    [Id(0)] public bool Initialized { get; set; }
    [Id(1)] public List<ReasoningTask> ActiveTasks { get; set; } = new();
    [Id(2)] public List<ReasoningTask> CompletedTasks { get; set; } = new();
    [Id(3)] public Dictionary<string, int> ReasoningStats { get; set; } = new();
    [Id(4)] public DateTime LastReasoningTime { get; set; }
    [Id(5)] public List<string> ReasoningStrategies { get; set; } = new();
    [Id(6)] public string CurrentReasoningFocus { get; set; } = string.Empty;
}

/// <summary>
/// State log events for auto reasoning
/// </summary>
[GenerateSerializer]
public class AutoReasoningStateLogEvent : StateLogEventBase<AutoReasoningStateLogEvent>;

[GenerateSerializer]
public class ReasoningInitializedLogEvent : AutoReasoningStateLogEvent
{
    [Id(0)] public string LLMSystem { get; set; } = string.Empty;
    [Id(1)] public List<string> EnabledStrategies { get; set; } = new();
}

[GenerateSerializer]
public class ReasoningTaskStartedLogEvent : AutoReasoningStateLogEvent
{
    [Id(0)] public ReasoningTask Task { get; set; } = new();
}

[GenerateSerializer]
public class ReasoningTaskCompletedLogEvent : AutoReasoningStateLogEvent
{
    [Id(0)] public string TaskId { get; set; } = string.Empty;
    [Id(1)] public List<string> GeneratedTheoryIds { get; set; } = new();
    [Id(2)] public bool Success { get; set; }
    [Id(3)] public string CompletionDetails { get; set; } = string.Empty;
}

[GenerateSerializer]
public class TheoryGeneratedLogEvent : AutoReasoningStateLogEvent
{
    [Id(0)] public string TheoryId { get; set; } = string.Empty;
    [Id(1)] public string TheoryContent { get; set; } = string.Empty;
    [Id(2)] public string ReasoningMethod { get; set; } = string.Empty;
}

/// <summary>
/// Interface for auto reasoning AI agent
/// </summary>
public interface IAutoReasoningAIGAgent : IStateGAgent<AutoReasoningState>
{
    Task<bool> InitializeAsync(string llmSystem);
    Task<string> StartReasoningTaskAsync(ReasoningTask task);
    Task<List<string>> PerformDeductiveReasoningAsync(List<string> premiseTheoryIds, string targetDomain);
    Task<List<string>> PerformInductiveReasoningAsync(List<string> exampleTheoryIds, string pattern);
    Task<List<string>> PerformAbductiveReasoningAsync(string conclusionTheory, string domain);
    Task<List<string>> PerformAnalogicalReasoningAsync(string sourceTheoryId, string targetDomain);
    Task<List<ReasoningTask>> GetActiveTasksAsync();
    Task<Dictionary<string, int>> GetReasoningStatsAsync();
    Task<string> GenerateTheoryContentAsync(string reasoningType, List<string> sourceTheories, string targetDomain);
}

/// <summary>
/// Auto reasoning AI agent that generates new theories using various reasoning methods
/// </summary>
[GAgent("auto.reasoning.ai", "reasoning")]
public class AutoReasoningAIGAgent : WorkshopAIGAgentBase<AutoReasoningState, AutoReasoningStateLogEvent>,
    IAutoReasoningAIGAgent
{
    private Kernel? _kernel;
    private ITheoryKnowledgeGAgent? _knowledgeAgent;

    public override Task<string> GetDescriptionAsync()
        => Task.FromResult("AI agent that performs automatic reasoning to generate new theories from existing ones");

    public async Task<bool> InitializeAsync(string llmSystem)
    {
        try
        {
            Logger.LogInformation("Initializing AutoReasoningAIGAgent with LLM system: {System}", llmSystem);

            var initDto = new InitializeDto
            {
                LLMConfig = new LLMConfigDto { SystemLLM = llmSystem },
                Instructions = GetReasoningInstructions()
            };

            var success = await base.InitializeAsync(initDto);
            if (!success)
            {
                throw new InvalidOperationException("Failed to initialize AI agent");
            }

            _kernel = GetKernelFromBrain();
            if (_kernel == null)
            {
                Logger.LogError("Failed to get kernel from brain - Brain may not be properly initialized");
                throw new InvalidOperationException("Failed to get kernel from brain");
            }
            Logger.LogInformation("Successfully obtained kernel from brain");

            // Get knowledge agent using the same GUID as defined in coordinator
            var knowledgeAgentId = WorkshopReasoningConstants.KnowledgeAgentId;
            try
            {
                _knowledgeAgent = await GAgentFactory.GetGAgentAsync<ITheoryKnowledgeGAgent>(knowledgeAgentId);
                Logger.LogInformation("Successfully obtained knowledge agent using fixed GUID: {AgentId}", knowledgeAgentId);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to get knowledge agent");
                throw;
            }

            var strategies = new List<string> { "deductive", "inductive", "abductive", "analogical" };

            RaiseEvent(new ReasoningInitializedLogEvent
            {
                LLMSystem = llmSystem,
                EnabledStrategies = strategies
            });
            await ConfirmEvents();

            Logger.LogInformation("AutoReasoningAIGAgent initialized successfully");
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to initialize AutoReasoningAIGAgent");
            return false;
        }
    }

    public async Task<string> StartReasoningTaskAsync(ReasoningTask task)
    {
        task.TaskId = Guid.NewGuid().ToString();
        task.Status = "running";
        task.CreatedAt = DateTime.UtcNow;

        RaiseEvent(new ReasoningTaskStartedLogEvent { Task = task });
        await ConfirmEvents();

        await PublishAsync(new ReasoningTaskStartedEvent
        {
            TaskId = task.TaskId,
            TaskType = task.TaskType,
            TargetDomain = task.TargetDomain,
            SourceTheories = task.SourceTheoryIds,
            ReasoningGoal = task.ReasoningGoal
        });

        Logger.LogInformation("Started reasoning task {TaskId} of type {TaskType}", task.TaskId, task.TaskType);

        // Execute reasoning asynchronously
        _ = Task.Run(async () => await ExecuteReasoningTaskAsync(task));

        return task.TaskId;
    }

    public async Task<List<string>> PerformDeductiveReasoningAsync(List<string> premiseTheoryIds, string targetDomain)
    {
        Logger.LogInformation("Performing deductive reasoning from {Count} premises in domain {Domain}",
            premiseTheoryIds.Count, targetDomain);

        var taskId = Guid.NewGuid().ToString();
        var thinkingSteps = new List<ThinkingStep>();
        
        Logger.LogInformation("🧠 Starting deductive reasoning - creating thinking steps");
        
        // Record initial thinking step
        thinkingSteps.Add(new ThinkingStep
        {
            StepId = Guid.NewGuid().ToString(),
            StepType = "analysis",
            Content = $"Starting deductive reasoning with {premiseTheoryIds.Count} premise theories in domain: {targetDomain}",
            Reasoning = "Analyzing input premises to understand the logical foundation for deduction",
            Metadata = new Dictionary<string, string>
            {
                ["premise_count"] = premiseTheoryIds.Count.ToString(),
                ["target_domain"] = targetDomain
            }
        });
        
        Logger.LogInformation("🧠 Added analysis thinking step");

        var premises = new List<TheoryElement>();
        if (_knowledgeAgent != null)
        {
            foreach (var id in premiseTheoryIds)
            {
                var theory = await _knowledgeAgent.GetTheoryAsync(id);
                if (theory != null)
                {
                    premises.Add(theory);
                    thinkingSteps.Add(new ThinkingStep
                    {
                        StepId = Guid.NewGuid().ToString(),
                        StepType = "analysis",
                        Content = $"Retrieved premise theory {theory.FullId}: {theory.Content}",
                        Reasoning = "Gathering all premise theories to form the logical foundation",
                        Metadata = new Dictionary<string, string>
                        {
                            ["theory_id"] = theory.FullId,
                            ["theory_type"] = theory.Type
                        }
                    });
                }
            }
        }

        // Record synthesis thinking step
        thinkingSteps.Add(new ThinkingStep
        {
            StepId = Guid.NewGuid().ToString(),
            StepType = "synthesis",
            Content = "About to engage AI reasoning engine for deductive inference",
            Reasoning = "Using AI to perform logical deduction from gathered premises",
            Metadata = new Dictionary<string, string>
            {
                ["reasoning_type"] = "deductive",
                ["ai_engine"] = "sequential-thinking"
            }
        });

        var newTheoryContent = await GenerateTheoryContentAsync("deductive", premiseTheoryIds, targetDomain);

        if (!string.IsNullOrEmpty(newTheoryContent))
        {
            thinkingSteps.Add(new ThinkingStep
            {
                StepId = Guid.NewGuid().ToString(),
                StepType = "evaluation",
                Content = $"AI generated theory content (length: {newTheoryContent.Length} chars)",
                Reasoning = "Evaluating the AI-generated theory for validity and coherence",
                Metadata = new Dictionary<string, string>
                {
                    ["content_length"] = newTheoryContent.Length.ToString(),
                    ["generation_success"] = "true"
                }
            });

            var theoryElement = ParseGeneratedTheory(newTheoryContent, "deductive");
            if (theoryElement != null && _knowledgeAgent != null)
            {
                var theoryId = await _knowledgeAgent.AddTheoryAsync(theoryElement);

                // Wait a moment to ensure the theory is fully persisted
                await Task.Delay(100);
                
                // Verify the theory was saved before announcing it
                var savedTheory = await _knowledgeAgent.GetTheoryAsync(theoryId);
                if (savedTheory != null)
                {
                    thinkingSteps.Add(new ThinkingStep
                    {
                        StepId = Guid.NewGuid().ToString(),
                        StepType = "conclusion",
                        Content = $"Successfully created and verified theory {theoryId}",
                        Reasoning = "Theory has been persisted and verified in the knowledge base",
                        Metadata = new Dictionary<string, string>
                        {
                            ["theory_id"] = theoryId,
                            ["verification_success"] = "true"
                        }
                    });

                    RaiseEvent(new TheoryGeneratedLogEvent
                    {
                        TheoryId = theoryId,
                        TheoryContent = newTheoryContent,
                        ReasoningMethod = "deductive"
                    });
                    await ConfirmEvents();

                    Logger.LogInformation("Theory {TheoryId} successfully generated and verified", theoryId);
                    
                    // Log thinking steps for this reasoning process
                    foreach (var step in thinkingSteps)
                    {
                        Logger.LogInformation("🧠 AI Thinking [{StepType}]: {Content} | Reasoning: {Reasoning}", 
                            step.StepType.ToUpper(), step.Content, step.Reasoning);
                    }
                    
                    // Publish thinking steps event for coordinator to capture
                    Logger.LogInformation("🧠 Publishing {Count} thinking steps for deductive reasoning", thinkingSteps.Count);
                    await PublishThinkingStepsAsync(thinkingSteps, "deductive", Guid.NewGuid().ToString());
                    Logger.LogInformation("🧠 Thinking steps published successfully");
                    
                    return new List<string> { theoryId };
                }
                else
                {
                    thinkingSteps.Add(new ThinkingStep
                    {
                        StepId = Guid.NewGuid().ToString(),
                        StepType = "conclusion",
                        Content = $"Failed to verify theory {theoryId} after generation",
                        Reasoning = "Theory creation appeared successful but verification failed",
                        Metadata = new Dictionary<string, string>
                        {
                            ["theory_id"] = theoryId,
                            ["verification_success"] = "false"
                        }
                    });
                    
                    Logger.LogError("Failed to verify theory {TheoryId} after generation", theoryId);
                    return new List<string>();
                }
            }
        }
        else
        {
            thinkingSteps.Add(new ThinkingStep
            {
                StepId = Guid.NewGuid().ToString(),
                StepType = "conclusion",
                Content = "AI failed to generate theory content",
                Reasoning = "The AI reasoning engine did not produce valid theory content",
                Metadata = new Dictionary<string, string>
                {
                    ["generation_success"] = "false",
                    ["error_reason"] = "empty_response"
                }
            });
        }

        return new List<string>();
    }

    public async Task<List<string>> PerformInductiveReasoningAsync(List<string> exampleTheoryIds, string pattern)
    {
        Logger.LogInformation("Performing inductive reasoning from {Count} examples with pattern {Pattern}",
            exampleTheoryIds.Count, pattern);

        var newTheoryContent = await GenerateTheoryContentAsync("inductive", exampleTheoryIds, pattern);

        if (!string.IsNullOrEmpty(newTheoryContent))
        {
            var theoryElement = ParseGeneratedTheory(newTheoryContent, "inductive");
            if (theoryElement != null && _knowledgeAgent != null)
            {
                var theoryId = await _knowledgeAgent.AddTheoryAsync(theoryElement);

                // Wait a moment to ensure the theory is fully persisted
                await Task.Delay(100);
                
                // Verify the theory was saved before announcing it
                var savedTheory = await _knowledgeAgent.GetTheoryAsync(theoryId);
                if (savedTheory != null)
                {
                    RaiseEvent(new TheoryGeneratedLogEvent
                    {
                        TheoryId = theoryId,
                        TheoryContent = newTheoryContent,
                        ReasoningMethod = "inductive"
                    });
                    await ConfirmEvents();

                    Logger.LogInformation("Theory {TheoryId} successfully generated and verified", theoryId);
                    return new List<string> { theoryId };
                }
                else
                {
                    Logger.LogError("Failed to verify theory {TheoryId} after generation", theoryId);
                    return new List<string>();
                }
            }
        }

        return new List<string>();
    }

    public async Task<List<string>> PerformAbductiveReasoningAsync(string conclusionTheory, string domain)
    {
        Logger.LogInformation("Performing abductive reasoning for conclusion in domain {Domain}", domain);

        var newTheoryContent =
            await GenerateTheoryContentAsync("abductive", new List<string> { conclusionTheory }, domain);

        if (!string.IsNullOrEmpty(newTheoryContent))
        {
            var theoryElement = ParseGeneratedTheory(newTheoryContent, "abductive");
            if (theoryElement != null && _knowledgeAgent != null)
            {
                var theoryId = await _knowledgeAgent.AddTheoryAsync(theoryElement);

                // Wait a moment to ensure the theory is fully persisted
                await Task.Delay(100);
                
                // Verify the theory was saved before announcing it
                var savedTheory = await _knowledgeAgent.GetTheoryAsync(theoryId);
                if (savedTheory != null)
                {
                    RaiseEvent(new TheoryGeneratedLogEvent
                    {
                        TheoryId = theoryId,
                        TheoryContent = newTheoryContent,
                        ReasoningMethod = "abductive"
                    });
                    await ConfirmEvents();

                    Logger.LogInformation("Theory {TheoryId} successfully generated and verified", theoryId);
                    return new List<string> { theoryId };
                }
                else
                {
                    Logger.LogError("Failed to verify theory {TheoryId} after generation", theoryId);
                    return new List<string>();
                }
            }
        }

        return new List<string>();
    }

    public async Task<List<string>> PerformAnalogicalReasoningAsync(string sourceTheoryId, string targetDomain)
    {
        Logger.LogInformation("Performing analogical reasoning from theory {SourceId} to domain {Domain}",
            sourceTheoryId, targetDomain);

        var newTheoryContent =
            await GenerateTheoryContentAsync("analogical", new List<string> { sourceTheoryId }, targetDomain);

        if (!string.IsNullOrEmpty(newTheoryContent))
        {
            var theoryElement = ParseGeneratedTheory(newTheoryContent, "analogical");
            if (theoryElement != null && _knowledgeAgent != null)
            {
                var theoryId = await _knowledgeAgent.AddTheoryAsync(theoryElement);

                // Wait a moment to ensure the theory is fully persisted
                await Task.Delay(100);
                
                // Verify the theory was saved before announcing it
                var savedTheory = await _knowledgeAgent.GetTheoryAsync(theoryId);
                if (savedTheory != null)
                {
                    RaiseEvent(new TheoryGeneratedLogEvent
                    {
                        TheoryId = theoryId,
                        TheoryContent = newTheoryContent,
                        ReasoningMethod = "analogical"
                    });
                    await ConfirmEvents();

                    Logger.LogInformation("Theory {TheoryId} successfully generated and verified", theoryId);
                    return new List<string> { theoryId };
                }
                else
                {
                    Logger.LogError("Failed to verify theory {TheoryId} after generation", theoryId);
                    return new List<string>();
                }
            }
        }

        return new List<string>();
    }

    public Task<List<ReasoningTask>> GetActiveTasksAsync()
    {
        return Task.FromResult(State.ActiveTasks.ToList());
    }

    public Task<Dictionary<string, int>> GetReasoningStatsAsync()
    {
        var stats = new Dictionary<string, int>(State.ReasoningStats)
        {
            ["ActiveTasks"] = State.ActiveTasks.Count,
            ["CompletedTasks"] = State.CompletedTasks.Count,
            ["TotalTasks"] = State.ActiveTasks.Count + State.CompletedTasks.Count
        };

        return Task.FromResult(stats);
    }

    public async Task<string> GenerateTheoryContentAsync(string reasoningType, List<string> sourceTheories,
        string targetDomain)
    {
        Logger.LogInformation("Starting theory generation - Type: {ReasoningType}, Source theories: {SourceCount}, Domain: {Domain}", 
            reasoningType, sourceTheories.Count, targetDomain);
            
        if (_kernel == null || _knowledgeAgent == null)
        {
            Logger.LogError("Cannot generate theory content - Kernel: {KernelStatus}, KnowledgeAgent: {KnowledgeStatus}", 
                _kernel != null ? "Available" : "NULL", _knowledgeAgent != null ? "Available" : "NULL");
            return string.Empty;
        }

        try
        {
            // Gather source theory contents
            var sourceContents = new List<string>();
            foreach (var theoryId in sourceTheories)
            {
                var theory = await _knowledgeAgent.GetTheoryAsync(theoryId);
                if (theory != null)
                {
                    sourceContents.Add($"{theory.FullId}: {theory.Content}");
                }
            }

            var prompt = reasoningType.ToLower() switch
            {
                "deductive" => GenerateDeductivePrompt(sourceContents, targetDomain),
                "inductive" => GenerateInductivePrompt(sourceContents, targetDomain),
                "abductive" => GenerateAbductivePrompt(sourceContents, targetDomain),
                "analogical" => GenerateAnalogicalPrompt(sourceContents, targetDomain),
                _ => GenerateGenericReasoningPrompt(sourceContents, targetDomain)
            };

            Logger.LogInformation("Sending prompt to LLM for {ReasoningType} reasoning. Prompt length: {Length}", 
                reasoningType, prompt.Length);

            // Try to use sequential thinking for better reasoning process
            var response = await ChatWithHistoryAndToolsAsync(
                $"Please use sequential thinking to solve this {reasoningType} reasoning task step by step:\n\n{prompt}"
            );
            
            var content = response.Response ?? string.Empty;
            
            Logger.LogInformation("LLM response received for {ReasoningType}. Response length: {Length}, Empty: {IsEmpty}", 
                reasoningType, content.Length, string.IsNullOrEmpty(content));
            
            // Log the reasoning process for debugging
            if (!string.IsNullOrEmpty(content))
            {
                Logger.LogInformation("AI Reasoning Process for {ReasoningType}:\n--- PROMPT ---\n{Prompt}\n--- RESPONSE ---\n{Response}\n--- END ---", 
                    reasoningType, prompt, content);
            }
            else
            {
                Logger.LogWarning("LLM returned empty response for {ReasoningType} reasoning", reasoningType);
            }
            
            return content;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error generating theory content for {ReasoningType}", reasoningType);
            return string.Empty;
        }
    }

    private async Task ExecuteReasoningTaskAsync(ReasoningTask task)
    {
        try
        {
            List<string> results = task.TaskType.ToLower() switch
            {
                "deductive" => await PerformDeductiveReasoningAsync(task.SourceTheoryIds, task.TargetDomain),
                "inductive" => await PerformInductiveReasoningAsync(task.SourceTheoryIds, task.TargetDomain),
                "abductive" => await PerformAbductiveReasoningAsync(
                    task.SourceTheoryIds.FirstOrDefault() ?? "", task.TargetDomain),
                "analogical" => await PerformAnalogicalReasoningAsync(
                    task.SourceTheoryIds.FirstOrDefault() ?? "", task.TargetDomain),
                _ => new List<string>()
            };

            task.Status = "completed";
            task.GeneratedTheoryIds = results;

            RaiseEvent(new ReasoningTaskCompletedLogEvent
            {
                TaskId = task.TaskId,
                GeneratedTheoryIds = results,
                Success = results.Count > 0,
                CompletionDetails = $"Generated {results.Count} new theories"
            });
            await ConfirmEvents();

            await PublishAsync(new ReasoningTaskCompletedEvent
            {
                TaskId = task.TaskId,
                GeneratedTheoryIds = results,
                Success = results.Count > 0,
                ReasoningTime = (DateTime.UtcNow - task.CreatedAt).TotalSeconds
            });

            Logger.LogInformation("Completed reasoning task {TaskId} with {Count} results",
                task.TaskId, results.Count);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error executing reasoning task {TaskId}", task.TaskId);
            task.Status = "failed";
        }
    }

    private TheoryElement? ParseGeneratedTheory(string content, string reasoningMethod)
    {
        try
        {
            // More robust parsing logic to handle various AI response formats
            var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            string? theoryType = null;
            string? theoryContent = null;
            string? formalExpression = null;

            // Try multiple parsing strategies
            bool foundStructuredFormat = false;

            // Strategy 1: Try structured format with labels
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (trimmedLine.StartsWith("Type:", StringComparison.OrdinalIgnoreCase))
                {
                    theoryType = trimmedLine.Substring(5).Trim();
                    foundStructuredFormat = true;
                }
                else if (trimmedLine.StartsWith("Content:", StringComparison.OrdinalIgnoreCase))
                {
                    theoryContent = trimmedLine.Substring(8).Trim();
                    foundStructuredFormat = true;
                }
                else if (trimmedLine.StartsWith("Formal:", StringComparison.OrdinalIgnoreCase) ||
                         trimmedLine.StartsWith("Formal Expression:", StringComparison.OrdinalIgnoreCase))
                {
                    var colonIndex = trimmedLine.IndexOf(':');
                    if (colonIndex > 0 && colonIndex < trimmedLine.Length - 1)
                    {
                        formalExpression = trimmedLine.Substring(colonIndex + 1).Trim();
                    }
                    foundStructuredFormat = true;
                }
            }

            // Strategy 2: If no structured format found, use heuristics
            if (!foundStructuredFormat)
            {
                Logger.LogInformation("No structured format found, using heuristic parsing for content: {Content}", 
                    content.Substring(0, Math.Min(100, content.Length)));

                // Default to Proposition if no type specified
                theoryType = "P";
                
                // Use the entire content as theory content, cleaning it up
                theoryContent = content.Trim();
                
                // Try to extract formal expressions from common patterns
                var formalPatterns = new[] { "∀", "∃", "→", "⊨", "⊢", "∧", "∨", "¬", "≡" };
                foreach (var line in lines)
                {
                    if (formalPatterns.Any(pattern => line.Contains(pattern)))
                    {
                        formalExpression = line.Trim();
                        break;
                    }
                }
            }

            // Strategy 3: Extract type from keywords in content if not explicitly found
            if (string.IsNullOrEmpty(theoryType) && !string.IsNullOrEmpty(theoryContent))
            {
                theoryType = ExtractTypeFromContent(theoryContent);
            }

            if (!string.IsNullOrEmpty(theoryType) && !string.IsNullOrEmpty(theoryContent))
            {
                // Normalize theory type to single letter format
                theoryType = NormalizeTheoryType(theoryType);
                
                Logger.LogInformation("Successfully parsed theory - Type: {Type}, Content: {Content}, Method: {Method}", 
                    theoryType, theoryContent.Substring(0, Math.Min(50, theoryContent.Length)), reasoningMethod);
                
                return new TheoryElement
                {
                    Type = theoryType,
                    Content = theoryContent,
                    FormalExpression = formalExpression ?? "",
                    ReasoningMethod = reasoningMethod,
                    QualityScore = 0.7, // Slightly higher initial score for successfully parsed theories
                    IsVerified = false,
                    CreatedAt = DateTime.UtcNow
                };
            }
            else
            {
                Logger.LogWarning("Failed to parse theory - Type: {Type}, Content available: {HasContent}, Raw: {RawContent}", 
                    theoryType, !string.IsNullOrEmpty(theoryContent), content.Substring(0, Math.Min(200, content.Length)));
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error parsing generated theory content: {Content}", content.Substring(0, Math.Min(200, content.Length)));
        }

        return null;
    }

    private string ExtractTypeFromContent(string content)
    {
        var lowerContent = content.ToLower();
        
        // Check for explicit type mentions
        if (lowerContent.Contains("theorem") || lowerContent.Contains("prove") || lowerContent.Contains("证明"))
            return "T";
        if (lowerContent.Contains("axiom") || lowerContent.Contains("公理"))
            return "A";
        if (lowerContent.Contains("corollary") || lowerContent.Contains("推论"))
            return "C";
        if (lowerContent.Contains("definition") || lowerContent.Contains("定义"))
            return "D";
        if (lowerContent.Contains("lemma") || lowerContent.Contains("引理"))
            return "L";
            
        // Default to proposition
        return "P";
    }

    private string NormalizeTheoryType(string theoryType)
    {
        // Normalize theory types to single letter format
        var normalized = theoryType.ToUpper().Trim();
        
        // Extract first letter if it contains additional text
        if (normalized.StartsWith("A") || normalized.Contains("AXIOM"))
            return "A";
        if (normalized.StartsWith("C") || normalized.Contains("COROLLARY"))
            return "C";
        if (normalized.StartsWith("P") || normalized.Contains("PROPOSITION"))
            return "P";
        if (normalized.StartsWith("T") || normalized.Contains("THEOREM"))
            return "T";
        if (normalized.StartsWith("D") || normalized.Contains("DEFINITION"))
            return "D";
            
        // Default to theorem if unclear
        Logger.LogWarning("Unknown theory type '{Type}', defaulting to 'T' (Theorem)", theoryType);
        return "T";
    }

    protected override void AIGAgentTransitionState(AutoReasoningState state,
        StateLogEventBase<AutoReasoningStateLogEvent> @event)
    {
        switch (@event)
        {
            case ReasoningInitializedLogEvent initEvent:
                state.Initialized = true;
                state.ReasoningStrategies = initEvent.EnabledStrategies;
                foreach (var strategy in initEvent.EnabledStrategies)
                {
                    state.ReasoningStats[strategy] = 0;
                }

                break;

            case ReasoningTaskStartedLogEvent startEvent:
                state.ActiveTasks.Add(startEvent.Task);
                break;

            case ReasoningTaskCompletedLogEvent completedEvent:
                var task = state.ActiveTasks.FirstOrDefault(t => t.TaskId == completedEvent.TaskId);
                if (task != null)
                {
                    state.ActiveTasks.Remove(task);
                    task.Status = completedEvent.Success ? "completed" : "failed";
                    task.GeneratedTheoryIds = completedEvent.GeneratedTheoryIds;
                    state.CompletedTasks.Add(task);
                }

                state.LastReasoningTime = DateTime.UtcNow;
                break;

            case TheoryGeneratedLogEvent generatedEvent:
                state.ReasoningStats.TryGetValue(generatedEvent.ReasoningMethod, out var count);
                state.ReasoningStats[generatedEvent.ReasoningMethod] = count + 1;
                break;
        }
    }

    private string GetReasoningInstructions()
    {
        return
            @"You are an advanced mathematical reasoning AI specialized in generating new theories from existing ones using various reasoning methods.

Your primary task is to analyze existing mathematical theories and generate new, valid theories using:
1. Deductive reasoning: Draw logical conclusions from premises
2. Inductive reasoning: Identify patterns and generalize
3. Abductive reasoning: Infer the best explanation for observations
4. Analogical reasoning: Apply patterns from one domain to another

When generating new theories, follow these guidelines:
- Maintain logical consistency with existing theories
- Use proper mathematical notation and formal expressions
- Provide clear content descriptions in both natural language and formal notation
- Ensure new theories add meaningful insights to the knowledge base
- Specify the reasoning method used

Output format for new theories:
Type: [A|C|P|T|D|L|M]
Content: [Natural language description]
Formal: [Mathematical formal expression]
Dependencies: [List of prerequisite theory IDs]
Reasoning: [Brief explanation of the reasoning process]

Be creative but rigorous. Generate theories that extend the Ψ theory framework while maintaining mathematical validity.";
    }

    private string GenerateDeductivePrompt(List<string> sourceContents, string targetDomain)
    {
        return $@"Using deductive reasoning, generate a new theory based on the following premises:

Premises:
{string.Join("\n", sourceContents)}

Target Domain: {targetDomain}

Apply logical deduction to derive a new theorem, corollary, or proposition that necessarily follows from these premises. Ensure the conclusion is logically valid and adds value to the knowledge base.";
    }

    private string GenerateInductivePrompt(List<string> sourceContents, string targetDomain)
    {
        return $@"Using inductive reasoning, analyze the following examples and identify a general pattern:

Examples:
{string.Join("\n", sourceContents)}

Target Domain: {targetDomain}

Look for patterns, regularities, or common structures across these examples. Generate a general theory that captures the underlying pattern while being applicable to the target domain.";
    }

    private string GenerateAbductivePrompt(List<string> sourceContents, string targetDomain)
    {
        return $@"Using abductive reasoning, provide the best explanation for the following observation:

Observation:
{string.Join("\n", sourceContents)}

Target Domain: {targetDomain}

Generate a theory that best explains the given observation. This should be a plausible hypothesis that, if true, would account for the observed phenomena.";
    }

    private string GenerateAnalogicalPrompt(List<string> sourceContents, string targetDomain)
    {
        return
            $@"Using analogical reasoning, transfer the structure or pattern from the source theory to the target domain:

Source Theory:
{string.Join("\n", sourceContents)}

Target Domain: {targetDomain}

Identify the key structural relationships in the source theory and create an analogous theory in the target domain. Maintain the logical structure while adapting the content to the new domain.";
    }

    private string GenerateGenericReasoningPrompt(List<string> sourceContents, string targetDomain)
    {
        return $@"Generate a new theory related to the following existing theories:

Source Theories:
{string.Join("\n", sourceContents)}

Target Domain: {targetDomain}

Use any appropriate reasoning method to generate a meaningful new theory that extends or builds upon the existing theories in the target domain.";
    }

    /// <summary>
    /// Publish thinking steps as events for coordinator to capture
    /// </summary>
    private async Task PublishThinkingStepsAsync(List<ThinkingStep> thinkingSteps, string reasoningType, string taskId)
    {
        try
        {
            // Convert ThinkingStep to ThinkingStepData for event
            var stepData = thinkingSteps.Select(step => new ThinkingStepData
            {
                StepId = step.StepId,
                StepType = step.StepType,
                Content = step.Content,
                Reasoning = step.Reasoning,
                Metadata = step.Metadata ?? new Dictionary<string, string>(),
                Timestamp = step.Timestamp
            }).ToList();

            var thinkingEvent = new ThinkingStepsGeneratedEvent
            {
                ReasoningSessionId = "", // Will be set by coordinator
                TaskId = taskId,
                ReasoningType = reasoningType,
                ThinkingSteps = stepData,
                IterationPhase = $"reasoning_{reasoningType}",
                CurrentIteration = 1, // This could be passed as parameter
                Timestamp = DateTime.UtcNow
            };

            await PublishAsync(thinkingEvent);
            Logger.LogInformation("Published {Count} thinking steps for {ReasoningType} reasoning", 
                stepData.Count, reasoningType);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error publishing thinking steps for {ReasoningType} reasoning", reasoningType);
        }
    }
}