using System.Text;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.AIGAgent.State;
using Aevatar.Workshop.GAgent.Events;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Aevatar.Workshop.GAgent.GAgents;

/// <summary>
/// Equivalence review result between different representations
/// </summary>
[GenerateSerializer]
public class EquivalenceReview
{
    [Id(0)] public string TheoryId { get; set; } = string.Empty;
    [Id(1)] public string NaturalLanguageContent { get; set; } = string.Empty;
    [Id(2)] public string FormalExpression { get; set; } = string.Empty;
    [Id(3)] public string PythonCode { get; set; } = string.Empty;
    [Id(4)] public bool TheoryFormalizationEquivalent { get; set; }
    [Id(5)] public bool FormalizationProgramEquivalent { get; set; }
    [Id(6)] public bool TheoryProgramEquivalent { get; set; }
    [Id(7)] public string EquivalenceDetails { get; set; } = string.Empty;
    [Id(8)] public List<string> DiscrepancyReports { get; set; } = new();
    [Id(9)] public DateTime ReviewedAt { get; set; } = DateTime.UtcNow;
    [Id(10)] public double OverallEquivalenceScore { get; set; }
    [Id(11)] public string ReviewerRecommendation { get; set; } = string.Empty;
    [Id(12)] public bool RequiresRevision { get; set; }
}

/// <summary>
/// Review task for coordinating the equivalence checking process
/// </summary>
[GenerateSerializer]
public class ReviewTask
{
    [Id(0)] public string TaskId { get; set; } = string.Empty;
    [Id(1)] public string TheoryId { get; set; } = string.Empty;
    [Id(2)] public string Status { get; set; } = "pending"; // pending, running, completed, failed
    [Id(3)] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Id(4)] public DateTime? CompletedAt { get; set; }
    [Id(5)] public string CurrentStage { get; set; } = string.Empty; // formalization, verification, review
    [Id(6)] public List<string> CompletedStages { get; set; } = new();
    [Id(7)] public EquivalenceReview? FinalReview { get; set; }
}

/// <summary>
/// State for equivalence review coordinator
/// </summary>
[GenerateSerializer]
public class EquivalenceReviewState : AIGAgentStateBase
{
    [Id(0)] public bool Initialized { get; set; }
    [Id(1)] public List<ReviewTask> ActiveTasks { get; set; } = new();
    [Id(2)] public List<ReviewTask> CompletedTasks { get; set; } = new();
    [Id(3)] public Dictionary<string, EquivalenceReview> ReviewHistory { get; set; } = new();
    [Id(4)] public Dictionary<string, int> ReviewStats { get; set; } = new();
    [Id(5)] public DateTime LastReviewTime { get; set; }
    [Id(6)] public List<string> QualityMetrics { get; set; } = new();
}

/// <summary>
/// State log events for equivalence review
/// </summary>
[GenerateSerializer]
public class EquivalenceReviewStateLogEvent : StateLogEventBase<EquivalenceReviewStateLogEvent>;

[GenerateSerializer]
public class ReviewInitializedLogEvent : EquivalenceReviewStateLogEvent
{
    [Id(0)] public string LLMSystem { get; set; } = string.Empty;
    [Id(1)] public List<string> QualityMetrics { get; set; } = new();
}

[GenerateSerializer]
public class ReviewTaskStartedLogEvent : EquivalenceReviewStateLogEvent
{
    [Id(0)] public ReviewTask Task { get; set; } = new();
}

[GenerateSerializer]
public class ReviewTaskCompletedLogEvent : EquivalenceReviewStateLogEvent
{
    [Id(0)] public string TaskId { get; set; } = string.Empty;
    [Id(1)] public EquivalenceReview Review { get; set; } = new();
    [Id(2)] public bool Success { get; set; }
}

[GenerateSerializer]
public class EquivalenceCheckedLogEvent : EquivalenceReviewStateLogEvent
{
    [Id(0)] public string TheoryId { get; set; } = string.Empty;
    [Id(1)] public double EquivalenceScore { get; set; }
    [Id(2)] public bool RequiresRevision { get; set; }
}

/// <summary>
/// Interface for equivalence review coordinator
/// </summary>
public interface IEquivalenceReviewGAgent : IStateGAgent<EquivalenceReviewState>
{
    Task<bool> InitializeAsync(string llmSystem);
    Task<string> StartReviewTaskAsync(string theoryId);
    Task<EquivalenceReview> PerformEquivalenceReviewAsync(string theoryId, string theoryContent, string formalExpression, string pythonCode);
    Task<bool> CheckTheoryFormalizationEquivalenceAsync(string theoryContent, string formalExpression);
    Task<bool> CheckFormalizationProgramEquivalenceAsync(string formalExpression, string pythonCode);
    Task<bool> CheckTheoryProgramEquivalenceAsync(string theoryContent, string pythonCode);
    Task<List<ReviewTask>> GetActiveReviewTasksAsync();
    Task<EquivalenceReview?> GetReviewResultAsync(string theoryId);
    Task<Dictionary<string, int>> GetReviewStatsAsync();
    Task<string> GenerateRevisionRecommendationsAsync(EquivalenceReview review);
}

/// <summary>
/// Equivalence review coordinator that ensures three-way equivalence between theory, formalization, and verification
/// </summary>
[GAgent("equivalence.review", "reasoning")]
public class EquivalenceReviewGAgent : WorkshopAIGAgentBase<EquivalenceReviewState, EquivalenceReviewStateLogEvent>,
    IEquivalenceReviewGAgent
{
    private Kernel? _kernel;
    private ITheoryKnowledgeGAgent? _knowledgeAgent;
    private IFormalizationAIGAgent? _formalizationAgent;
    private IPythonVerificationGAgent? _verificationAgent;

    public override Task<string> GetDescriptionAsync()
        => Task.FromResult(
            "Coordinates equivalence review process ensuring three-way equivalence between natural language theory, formal expression, and Python verification code");

    public async Task<bool> InitializeAsync(string llmSystem)
    {
        try
        {
            Logger.LogInformation("Initializing EquivalenceReviewGAgent with LLM system: {System}", llmSystem);

            var initDto = new InitializeDto
            {
                LLMConfig = new LLMConfigDto { SystemLLM = llmSystem },
                Instructions = GetReviewInstructions()
            };

            var success = await base.InitializeAsync(initDto);
            if (!success)
            {
                throw new InvalidOperationException("Failed to initialize AI agent");
            }

            _kernel = GetKernelFromBrain();
            if (_kernel == null)
            {
                throw new InvalidOperationException("Failed to get kernel from brain");
            }

            // Get other agents
            _knowledgeAgent = await GAgentFactory.GetGAgentAsync<ITheoryKnowledgeGAgent>(Guid.NewGuid());
            _formalizationAgent = await GAgentFactory.GetGAgentAsync<IFormalizationAIGAgent>(Guid.NewGuid());
            _verificationAgent = await GAgentFactory.GetGAgentAsync<IPythonVerificationGAgent>(Guid.NewGuid());

            var qualityMetrics = new List<string>
            {
                "semantic_equivalence", "logical_consistency", "mathematical_validity",
                "computational_accuracy", "completeness", "precision"
            };

            RaiseEvent(new ReviewInitializedLogEvent
            {
                LLMSystem = llmSystem,
                QualityMetrics = qualityMetrics
            });
            await ConfirmEvents();

            // Subscribe to events from other agents
            await SubscribeToReasoningEventsAsync();

            Logger.LogInformation("EquivalenceReviewGAgent initialized successfully");
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to initialize EquivalenceReviewGAgent");
            return false;
        }
    }

    public async Task<string> StartReviewTaskAsync(string theoryId)
    {
        var task = new ReviewTask
        {
            TaskId = Guid.NewGuid().ToString(),
            TheoryId = theoryId,
            Status = "running",
            CreatedAt = DateTime.UtcNow,
            CurrentStage = "formalization"
        };

        RaiseEvent(new ReviewTaskStartedLogEvent { Task = task });
        await ConfirmEvents();

        Logger.LogInformation("Started review task {TaskId} for theory {TheoryId}", task.TaskId, theoryId);

        // Execute review process asynchronously
        _ = Task.Run(async () => await ExecuteReviewProcessAsync(task));

        return task.TaskId;
    }

    public async Task<EquivalenceReview> PerformEquivalenceReviewAsync(string theoryId, string theoryContent,
        string formalExpression, string pythonCode)
    {
        Logger.LogInformation("Performing equivalence review for theory {TheoryId}", theoryId);

        var review = new EquivalenceReview
        {
            TheoryId = theoryId,
            NaturalLanguageContent = theoryContent,
            FormalExpression = formalExpression,
            PythonCode = pythonCode,
            ReviewedAt = DateTime.UtcNow
        };

        try
        {
            // Check theory-formalization equivalence
            review.TheoryFormalizationEquivalent =
                await CheckTheoryFormalizationEquivalenceAsync(theoryContent, formalExpression);

            // Check formalization-program equivalence
            review.FormalizationProgramEquivalent =
                await CheckFormalizationProgramEquivalenceAsync(formalExpression, pythonCode);

            // Check theory-program equivalence
            review.TheoryProgramEquivalent = await CheckTheoryProgramEquivalenceAsync(theoryContent, pythonCode);

            // Calculate overall equivalence score
            review.OverallEquivalenceScore = CalculateOverallEquivalenceScore(review);

            // Generate detailed equivalence analysis
            review.EquivalenceDetails = await GenerateEquivalenceAnalysisAsync(review);

            // Identify discrepancies
            review.DiscrepancyReports = await IdentifyDiscrepanciesAsync(review);

            // Determine if revision is required
            review.RequiresRevision = review.OverallEquivalenceScore < 0.8 || review.DiscrepancyReports.Count > 0;

            // Generate reviewer recommendation
            review.ReviewerRecommendation = await GenerateRevisionRecommendationsAsync(review);

            RaiseEvent(new EquivalenceCheckedLogEvent
            {
                TheoryId = theoryId,
                EquivalenceScore = review.OverallEquivalenceScore,
                RequiresRevision = review.RequiresRevision
            });
            await ConfirmEvents();

            await PublishAsync(new EquivalenceCheckedEvent
            {
                TheoryId = theoryId,
                TheoryFormalizationEquivalent = review.TheoryFormalizationEquivalent,
                FormalizationProgramEquivalent = review.FormalizationProgramEquivalent,
                TheoryProgramEquivalent = review.TheoryProgramEquivalent,
                EquivalenceDetails = review.EquivalenceDetails,
                DiscrepancyReports = review.DiscrepancyReports
            });

            Logger.LogInformation("Completed equivalence review for theory {TheoryId}, score: {Score}",
                theoryId, review.OverallEquivalenceScore);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error performing equivalence review for theory {TheoryId}", theoryId);
            review.EquivalenceDetails = $"Review failed: {ex.Message}";
            review.RequiresRevision = true;
        }

        return review;
    }

    public async Task<bool> CheckTheoryFormalizationEquivalenceAsync(string theoryContent, string formalExpression)
    {
        if (_kernel == null || string.IsNullOrEmpty(theoryContent) || string.IsNullOrEmpty(formalExpression))
        {
            return false;
        }

        try
        {
            var equivalencePrompt =
                $@"Analyze the equivalence between the natural language theory and its formal mathematical expression:

Natural Language Theory:
{theoryContent}

Formal Expression:
{formalExpression}

Evaluate if they express the same mathematical concepts with equivalent:
1. Semantic meaning
2. Logical structure  
3. Mathematical relationships
4. Quantifier scope and binding
5. Operational definitions

Return 'EQUIVALENT' if they are semantically and logically equivalent, or 'NOT_EQUIVALENT' with explanation.";

            var chatService = _kernel.GetRequiredService<IChatCompletionService>();
            var chatHistory = new ChatHistory();
            chatHistory.AddSystemMessage(GetEquivalenceCheckInstructions());
            chatHistory.AddUserMessage(equivalencePrompt);

            var response = await chatService.GetChatMessageContentAsync(chatHistory);
            var result = response.Content ?? "";

            return result.Contains("EQUIVALENT") && !result.Contains("NOT_EQUIVALENT");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error checking theory-formalization equivalence");
            return false;
        }
    }

    public async Task<bool> CheckFormalizationProgramEquivalenceAsync(string formalExpression, string pythonCode)
    {
        if (_kernel == null || string.IsNullOrEmpty(formalExpression) || string.IsNullOrEmpty(pythonCode))
        {
            return false;
        }

        try
        {
            var equivalencePrompt =
                $@"Analyze the equivalence between the formal mathematical expression and its Python implementation:

Formal Expression:
{formalExpression}

Python Code:
{pythonCode}

Evaluate if the Python code correctly implements the formal expression with equivalent:
1. Mathematical operations
2. Logical conditions
3. Computational procedures
4. Variable bindings
5. Algorithmic structure

Return 'EQUIVALENT' if the Python code correctly implements the formal expression, or 'NOT_EQUIVALENT' with explanation.";

            var chatService = _kernel.GetRequiredService<IChatCompletionService>();
            var chatHistory = new ChatHistory();
            chatHistory.AddSystemMessage(GetEquivalenceCheckInstructions());
            chatHistory.AddUserMessage(equivalencePrompt);

            var response = await chatService.GetChatMessageContentAsync(chatHistory);
            var result = response.Content ?? "";

            return result.Contains("EQUIVALENT") && !result.Contains("NOT_EQUIVALENT");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error checking formalization-program equivalence");
            return false;
        }
    }

    public async Task<bool> CheckTheoryProgramEquivalenceAsync(string theoryContent, string pythonCode)
    {
        if (_kernel == null || string.IsNullOrEmpty(theoryContent) || string.IsNullOrEmpty(pythonCode))
        {
            return false;
        }

        try
        {
            var equivalencePrompt =
                $@"Analyze the equivalence between the natural language theory and its Python verification:

Natural Language Theory:
{theoryContent}

Python Code:
{pythonCode}

Evaluate if the Python code correctly captures and verifies the theory with equivalent:
1. Conceptual representation
2. Testable properties
3. Verification procedures
4. Computational validation
5. Theory constraints

Return 'EQUIVALENT' if the Python code correctly represents and verifies the theory, or 'NOT_EQUIVALENT' with explanation.";

            var chatService = _kernel.GetRequiredService<IChatCompletionService>();
            var chatHistory = new ChatHistory();
            chatHistory.AddSystemMessage(GetEquivalenceCheckInstructions());
            chatHistory.AddUserMessage(equivalencePrompt);

            var response = await chatService.GetChatMessageContentAsync(chatHistory);
            var result = response.Content ?? "";

            return result.Contains("EQUIVALENT") && !result.Contains("NOT_EQUIVALENT");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error checking theory-program equivalence");
            return false;
        }
    }

    public Task<List<ReviewTask>> GetActiveReviewTasksAsync()
    {
        return Task.FromResult(State.ActiveTasks.ToList());
    }

    public Task<EquivalenceReview?> GetReviewResultAsync(string theoryId)
    {
        State.ReviewHistory.TryGetValue(theoryId, out var review);
        return Task.FromResult(review);
    }

    public Task<Dictionary<string, int>> GetReviewStatsAsync()
    {
        var stats = new Dictionary<string, int>(State.ReviewStats)
        {
            ["ActiveTasks"] = State.ActiveTasks.Count,
            ["CompletedTasks"] = State.CompletedTasks.Count,
            ["TotalReviews"] = State.ReviewHistory.Count,
            ["EquivalentReviews"] = State.ReviewHistory.Values.Count(r => r.OverallEquivalenceScore >= 0.8),
            ["RequiringRevision"] = State.ReviewHistory.Values.Count(r => r.RequiresRevision)
        };

        if (State.ReviewHistory.Count > 0)
        {
            stats["AverageEquivalenceScore"] =
                (int)(State.ReviewHistory.Values.Average(r => r.OverallEquivalenceScore) * 100);
        }

        return Task.FromResult(stats);
    }

    public async Task<string> GenerateRevisionRecommendationsAsync(EquivalenceReview review)
    {
        if (_kernel == null)
        {
            return string.Empty;
        }

        try
        {
            var recommendationPrompt = $@"Generate specific revision recommendations based on the equivalence review:

Theory: {review.NaturalLanguageContent}
Formal: {review.FormalExpression}
Python: {review.PythonCode}

Equivalence Results:
- Theory ↔ Formal: {review.TheoryFormalizationEquivalent}
- Formal ↔ Python: {review.FormalizationProgramEquivalent}
- Theory ↔ Python: {review.TheoryProgramEquivalent}

Overall Score: {review.OverallEquivalenceScore:F2}

Discrepancies:
{string.Join("\n", review.DiscrepancyReports)}

Provide specific, actionable recommendations for improving equivalence across all three representations.";

            var chatService = _kernel.GetRequiredService<IChatCompletionService>();
            var chatHistory = new ChatHistory();
            chatHistory.AddSystemMessage(GetRevisionRecommendationInstructions());
            chatHistory.AddUserMessage(recommendationPrompt);

            var response = await chatService.GetChatMessageContentAsync(chatHistory);
            return response.Content ?? string.Empty;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error generating revision recommendations");
            return "Error generating recommendations";
        }
    }

    private async Task ExecuteReviewProcessAsync(ReviewTask task)
    {
        try
        {
            if (_knowledgeAgent == null)
            {
                throw new InvalidOperationException("Knowledge agent not available");
            }

            // Ensure knowledge base is properly initialized before proceeding
            await EnsureKnowledgeBaseInitializedAsync();

            // Get theory information with retry logic
            var theory = await GetTheoryWithRetryAsync(task.TheoryId);
            if (theory == null)
            {
                Logger.LogError("Theory {TheoryId} not found after retries. Available theories: {AvailableTheories}", 
                    task.TheoryId, await GetAvailableTheoryIdsAsync());
                throw new InvalidOperationException($"Theory {task.TheoryId} not found");
            }

            // Stage 1: Ensure formalization exists
            task.CurrentStage = "formalization";
            string formalExpression = theory.FormalExpression;
            if (string.IsNullOrEmpty(formalExpression) && _formalizationAgent != null)
            {
                var formalizationResult = await _formalizationAgent.FormalizeTheoryAsync(task.TheoryId, theory.Content);
                formalExpression = formalizationResult.FormalExpression;
            }

            task.CompletedStages.Add("formalization");

            // Stage 2: Ensure verification exists
            task.CurrentStage = "verification";
            string pythonCode = theory.PythonCode;
            if (string.IsNullOrEmpty(pythonCode) && _verificationAgent != null)
            {
                var verificationResult =
                    await _verificationAgent.VerifyTheoryAsync(task.TheoryId, theory.Content, formalExpression);
                pythonCode = verificationResult.PythonCode;
            }

            task.CompletedStages.Add("verification");

            // Stage 3: Perform equivalence review
            task.CurrentStage = "review";
            var review =
                await PerformEquivalenceReviewAsync(task.TheoryId, theory.Content, formalExpression, pythonCode);
            task.CompletedStages.Add("review");

            // Complete task
            task.Status = "completed";
            task.CompletedAt = DateTime.UtcNow;
            task.FinalReview = review;

            RaiseEvent(new ReviewTaskCompletedLogEvent
            {
                TaskId = task.TaskId,
                Review = review,
                Success = true
            });
            await ConfirmEvents();

            Logger.LogInformation("Completed review task {TaskId} for theory {TheoryId}", task.TaskId, task.TheoryId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error executing review task {TaskId}", task.TaskId);
            task.Status = "failed";
            task.CompletedAt = DateTime.UtcNow;
        }
    }

    private double CalculateOverallEquivalenceScore(EquivalenceReview review)
    {
        double score = 0.0;
        int totalChecks = 3;

        if (review.TheoryFormalizationEquivalent) score += 1.0;
        if (review.FormalizationProgramEquivalent) score += 1.0;
        if (review.TheoryProgramEquivalent) score += 1.0;

        return score / totalChecks;
    }

    private async Task<string> GenerateEquivalenceAnalysisAsync(EquivalenceReview review)
    {
        var analysis = new StringBuilder();

        analysis.AppendLine($"Equivalence Analysis for Theory {review.TheoryId}:");
        analysis.AppendLine($"Overall Score: {review.OverallEquivalenceScore:F2}");
        analysis.AppendLine();

        analysis.AppendLine("Pairwise Equivalence Results:");
        analysis.AppendLine($"  Theory ↔ Formalization: {(review.TheoryFormalizationEquivalent ? "✓" : "✗")}");
        analysis.AppendLine($"  Formalization ↔ Program: {(review.FormalizationProgramEquivalent ? "✓" : "✗")}");
        analysis.AppendLine($"  Theory ↔ Program: {(review.TheoryProgramEquivalent ? "✓" : "✗")}");

        if (review.OverallEquivalenceScore >= 0.8)
        {
            analysis.AppendLine("\nConclusion: Strong equivalence across all representations.");
        }
        else if (review.OverallEquivalenceScore >= 0.6)
        {
            analysis.AppendLine("\nConclusion: Moderate equivalence with some discrepancies requiring attention.");
        }
        else
        {
            analysis.AppendLine("\nConclusion: Significant discrepancies detected. Revision strongly recommended.");
        }

        return analysis.ToString();
    }

    private async Task<List<string>> IdentifyDiscrepanciesAsync(EquivalenceReview review)
    {
        var discrepancies = new List<string>();

        if (!review.TheoryFormalizationEquivalent)
        {
            discrepancies.Add("Theory and formalization express different mathematical concepts");
        }

        if (!review.FormalizationProgramEquivalent)
        {
            discrepancies.Add("Python code does not correctly implement the formal expression");
        }

        if (!review.TheoryProgramEquivalent)
        {
            discrepancies.Add("Python verification does not adequately test the theory properties");
        }

        return discrepancies;
    }

    private async Task SubscribeToReasoningEventsAsync()
    {
        // This would subscribe to events from other reasoning agents
        // Implementation depends on the specific event subscription mechanism
        Logger.LogInformation("Subscribed to reasoning events from other agents");
    }

    protected override void AIGAgentTransitionState(EquivalenceReviewState state,
        StateLogEventBase<EquivalenceReviewStateLogEvent> @event)
    {
        switch (@event)
        {
            case ReviewInitializedLogEvent initEvent:
                state.Initialized = true;
                state.QualityMetrics = initEvent.QualityMetrics;
                foreach (var metric in initEvent.QualityMetrics)
                {
                    state.ReviewStats[metric] = 0;
                }

                break;

            case ReviewTaskStartedLogEvent startEvent:
                state.ActiveTasks.Add(startEvent.Task);
                break;

            case ReviewTaskCompletedLogEvent completedEvent:
                var task = state.ActiveTasks.FirstOrDefault(t => t.TaskId == completedEvent.TaskId);
                if (task != null)
                {
                    state.ActiveTasks.Remove(task);
                    task.Status = completedEvent.Success ? "completed" : "failed";
                    state.CompletedTasks.Add(task);

                    if (completedEvent.Review != null)
                    {
                        state.ReviewHistory[completedEvent.Review.TheoryId] = completedEvent.Review;
                    }
                }

                state.LastReviewTime = DateTime.UtcNow;
                break;

            case EquivalenceCheckedLogEvent checkedEvent:
                state.ReviewStats.TryGetValue("total_reviews", out var count);
                state.ReviewStats["total_reviews"] = count + 1;

                if (checkedEvent.EquivalenceScore >= 0.8)
                {
                    state.ReviewStats.TryGetValue("high_quality", out var highCount);
                    state.ReviewStats["high_quality"] = highCount + 1;
                }

                break;
        }
    }

    private string GetReviewInstructions()
    {
        return
            @"You are an expert in mathematical equivalence review, specialized in ensuring consistency across different representations of mathematical theories.

Your primary task is to verify three-way equivalence between:
1. Natural language mathematical theories
2. Formal mathematical expressions
3. Python verification code

Key evaluation criteria:
- Semantic equivalence: Same mathematical meaning
- Logical consistency: Preserved logical structure
- Mathematical validity: Correct mathematical relationships
- Computational accuracy: Faithful implementation
- Completeness: No missing essential components
- Precision: Accurate representation of constraints

When reviewing equivalence:
- Analyze each pairwise relationship thoroughly
- Identify specific discrepancies and their implications
- Provide actionable recommendations for improvement
- Consider both syntactic and semantic equivalence
- Evaluate computational feasibility and correctness

Be rigorous, systematic, and provide detailed justifications for equivalence judgments.";
    }

    private string GetEquivalenceCheckInstructions()
    {
        return
            @"You are a mathematical equivalence expert. Your task is to determine if two representations express the same mathematical concepts.

Evaluation criteria:
1. Semantic equivalence: Do they mean the same thing mathematically?
2. Logical structure: Is the logical flow preserved?
3. Mathematical relationships: Are all mathematical relationships maintained?
4. Scope and binding: Are quantifiers and variables handled consistently?
5. Operational definitions: Are operations defined equivalently?

Return 'EQUIVALENT' only if the representations are mathematically and logically equivalent.
Return 'NOT_EQUIVALENT' with specific reasons if there are discrepancies.

Be precise and thorough in your analysis.";
    }

    private string GetRevisionRecommendationInstructions()
    {
        return
            @"You are a mathematical revision expert. Generate specific, actionable recommendations to improve equivalence between theory representations.

Your recommendations should:
1. Address specific discrepancies identified
2. Provide concrete steps for improvement
3. Maintain mathematical rigor and accuracy
4. Ensure consistency across all representations
5. Be implementable by domain experts

Structure recommendations clearly with:
- Priority level (high/medium/low)
- Specific changes needed
- Expected impact on equivalence
- Implementation guidance

Focus on practical, achievable improvements that will enhance equivalence scores.";
    }

    private async Task<TheoryElement?> GetTheoryWithRetryAsync(string theoryId, int maxRetries = 3)
    {
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            Logger.LogDebug("Attempting to get theory {TheoryId}, attempt {Attempt}/{MaxRetries}", 
                theoryId, attempt, maxRetries);
                
            var theory = await _knowledgeAgent.GetTheoryAsync(theoryId);
            if (theory != null)
            {
                Logger.LogDebug("Successfully retrieved theory {TheoryId} on attempt {Attempt}", theoryId, attempt);
                return theory;
            }
            
            Logger.LogWarning("Theory {TheoryId} not found on attempt {Attempt}, waiting before retry", theoryId, attempt);
            
            if (attempt < maxRetries)
            {
                // Wait with exponential backoff
                await Task.Delay(1000 * attempt);
            }
        }
        
        Logger.LogError("Failed to retrieve theory {TheoryId} after {MaxRetries} attempts", theoryId, maxRetries);
        return null;
    }

    private async Task<string> GetAvailableTheoryIdsAsync()
    {
        try
        {
            var recentTheories = await _knowledgeAgent.GetRecentTheoriesAsync(10);
            var theoryIds = recentTheories.Select(t => t.Id).ToList();
            return string.Join(", ", theoryIds);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting available theory IDs");
            return "Unable to retrieve available theories";
        }
    }

    private async Task EnsureKnowledgeBaseInitializedAsync()
    {
        try
        {
            // Check if knowledge base has theories
            var stats = await _knowledgeAgent.GetStatisticsAsync();
            var totalTheories = stats.GetValueOrDefault("Total", 0);
            
            if (totalTheories == 0)
            {
                Logger.LogWarning("Knowledge base appears empty, attempting to initialize");
                var initResult = await _knowledgeAgent.InitializeWithPsiTheoryAsync();
                
                if (!initResult)
                {
                    throw new InvalidOperationException("Failed to initialize knowledge base");
                }
                
                // Wait a moment for initialization to complete
                await Task.Delay(500);
                
                // Re-check after initialization
                stats = await _knowledgeAgent.GetStatisticsAsync();
                totalTheories = stats.GetValueOrDefault("Total", 0);
                
                Logger.LogInformation("Knowledge base initialized with {TotalTheories} theories", totalTheories);
            }
            else
            {
                Logger.LogDebug("Knowledge base already contains {TotalTheories} theories", totalTheories);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error ensuring knowledge base initialization");
            throw;
        }
    }
}