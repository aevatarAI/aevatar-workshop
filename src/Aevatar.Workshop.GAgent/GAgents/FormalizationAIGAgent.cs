using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.AIGAgent.State;
using Aevatar.Workshop.GAgent.Events;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Aevatar.Workshop.GAgent.GAgents;

/// <summary>
/// Formalization result with validation information
/// </summary>
[GenerateSerializer]
public class FormalizationResult
{
    [Id(0)] public string TheoryId { get; set; } = string.Empty;
    [Id(1)] public string OriginalContent { get; set; } = string.Empty;
    [Id(2)] public string FormalExpression { get; set; } = string.Empty;
    [Id(3)] public string FormalizationTool { get; set; } = string.Empty;
    [Id(4)] public bool IsValid { get; set; }
    [Id(5)] public string ValidationDetails { get; set; } = string.Empty;
    [Id(6)] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Id(7)] public List<string> SymbolicComponents { get; set; } = new();
    [Id(8)] public Dictionary<string, string> SymbolMapping { get; set; } = new();
    [Id(9)] public double ConfidenceScore { get; set; }
}

/// <summary>
/// State for formalization AI agent
/// </summary>
[GenerateSerializer]
public class FormalizationState : AIGAgentStateBase
{
    [Id(0)] public bool Initialized { get; set; }
    [Id(1)] public List<FormalizationResult> CompletedFormalizations { get; set; } = new();
    [Id(2)] public Dictionary<string, string> SymbolLibrary { get; set; } = new();
    [Id(3)] public List<string> SupportedTools { get; set; } = new();
    [Id(4)] public Dictionary<string, int> FormalizationStats { get; set; } = new();
    [Id(5)] public DateTime LastFormalizationTime { get; set; }
    [Id(6)] public List<string> PendingFormalizationIds { get; set; } = new();
}

/// <summary>
/// State log events for formalization
/// </summary>
[GenerateSerializer]
public class FormalizationStateLogEvent : StateLogEventBase<FormalizationStateLogEvent>;

[GenerateSerializer]
public class FormalizationInitializedLogEvent : FormalizationStateLogEvent
{
    [Id(0)] public string LLMSystem { get; set; } = string.Empty;
    [Id(1)] public List<string> SupportedTools { get; set; } = new();
}

[GenerateSerializer]
public class FormalizationCompletedLogEvent : FormalizationStateLogEvent
{
    [Id(0)] public FormalizationResult Result { get; set; } = new();
}

[GenerateSerializer]
public class FormalizationRequestedLogEvent : FormalizationStateLogEvent
{
    [Id(0)] public string TheoryId { get; set; } = string.Empty;
    [Id(1)] public string RequestedTool { get; set; } = string.Empty;
}

/// <summary>
/// Interface for formalization AI agent
/// </summary>
public interface IFormalizationAIGAgent : IStateGAgent<FormalizationState>
{
    Task<bool> InitializeAsync(string llmSystem);
    Task<FormalizationResult> FormalizeTheoryAsync(string theoryId, string theoryContent, string targetTool = "sympy");
    Task<bool> ValidateFormalExpressionAsync(string expression, string tool = "sympy");
    Task<string> ConvertBetweenToolsAsync(string expression, string fromTool, string toTool);
    Task<List<FormalizationResult>> GetFormalizationHistoryAsync(string theoryId);
    Task<Dictionary<string, int>> GetFormalizationStatsAsync();
    Task<List<string>> GetSupportedFormalizationToolsAsync();
    Task<string> GenerateProofSketchAsync(string formalExpression);
}

/// <summary>
/// Formalization AI agent that converts natural language theories to formal mathematical expressions
/// </summary>
[GAgent("formalization.ai", "reasoning")]
public class FormalizationAIGAgent : WorkshopAIGAgentBase<FormalizationState, FormalizationStateLogEvent>,
    IFormalizationAIGAgent
{
    private Kernel? _kernel;
    private ITheoryKnowledgeGAgent? _knowledgeAgent;

    public override Task<string> GetDescriptionAsync()
        => Task.FromResult(
            "AI agent that converts natural language theories to formal mathematical expressions using SymPy, Z3, and other tools");

    public async Task<bool> InitializeAsync(string llmSystem)
    {
        try
        {
            Logger.LogInformation("Initializing FormalizationAIGAgent with LLM system: {System}", llmSystem);

            var initDto = new InitializeDto
            {
                LLMConfig = new LLMConfigDto { SystemLLM = llmSystem },
                Instructions = GetFormalizationInstructions()
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

            _knowledgeAgent = await GAgentFactory.GetGAgentAsync<ITheoryKnowledgeGAgent>(Guid.NewGuid());

            var supportedTools = new List<string> { "sympy", "z3", "lean", "coq", "isabelle" };

            RaiseEvent(new FormalizationInitializedLogEvent
            {
                LLMSystem = llmSystem,
                SupportedTools = supportedTools
            });
            await ConfirmEvents();

            // Initialize symbol library with common mathematical symbols
            await InitializeSymbolLibraryAsync();

            Logger.LogInformation("FormalizationAIGAgent initialized successfully");
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to initialize FormalizationAIGAgent");
            return false;
        }
    }

    public async Task<FormalizationResult> FormalizeTheoryAsync(string theoryId, string theoryContent,
        string targetTool = "sympy")
    {
        Logger.LogInformation("Formalizing theory {TheoryId} using {Tool}", theoryId, targetTool);

        RaiseEvent(new FormalizationRequestedLogEvent
        {
            TheoryId = theoryId,
            RequestedTool = targetTool
        });
        await ConfirmEvents();

        var result = new FormalizationResult
        {
            TheoryId = theoryId,
            OriginalContent = theoryContent,
            FormalizationTool = targetTool,
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            // Generate formal expression using AI
            var formalExpression = await GenerateFormalExpressionAsync(theoryContent, targetTool);
            result.FormalExpression = formalExpression;

            // Validate the formal expression
            var isValid = await ValidateFormalExpressionAsync(formalExpression, targetTool);
            result.IsValid = isValid;

            // Calculate confidence score based on validation and complexity
            result.ConfidenceScore = CalculateConfidenceScore(formalExpression, isValid);

            // Extract symbolic components
            result.SymbolicComponents = ExtractSymbolicComponents(formalExpression);

            // Generate symbol mapping
            result.SymbolMapping = GenerateSymbolMapping(theoryContent, formalExpression);

            result.ValidationDetails = isValid
                ? "Formal expression validated successfully"
                : "Validation failed - expression may contain errors";

            RaiseEvent(new FormalizationCompletedLogEvent { Result = result });
            await ConfirmEvents();

            await PublishAsync(new FormalizationCompletedEvent
            {
                TheoryId = theoryId,
                FormalExpression = formalExpression,
                FormalizationTool = targetTool,
                IsValid = isValid,
                ValidationDetails = result.ValidationDetails
            });

            Logger.LogInformation("Completed formalization for theory {TheoryId}, valid: {IsValid}", theoryId, isValid);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error formalizing theory {TheoryId}", theoryId);
            result.IsValid = false;
            result.ValidationDetails = $"Formalization failed: {ex.Message}";
        }

        return result;
    }

    public async Task<bool> ValidateFormalExpressionAsync(string expression, string tool = "sympy")
    {
        if (string.IsNullOrEmpty(expression) || _kernel == null)
        {
            return false;
        }

        try
        {
            var validationPrompt = $@"Validate the following formal mathematical expression for {tool}:

Expression: {expression}

Check for:
1. Syntax correctness
2. Logical consistency  
3. Mathematical validity
4. Tool-specific formatting

Return 'VALID' if the expression is correct, or 'INVALID' with explanation if there are issues.";

            var chatService = _kernel.GetRequiredService<IChatCompletionService>();
            var chatHistory = new ChatHistory();
            chatHistory.AddSystemMessage(GetValidationInstructions(tool));
            chatHistory.AddUserMessage(validationPrompt);

            var response = await chatService.GetChatMessageContentAsync(chatHistory);
            var result = response.Content ?? "";

            return result.Contains("VALID") && !result.Contains("INVALID");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error validating expression: {Expression}", expression);
            return false;
        }
    }

    public async Task<string> ConvertBetweenToolsAsync(string expression, string fromTool, string toTool)
    {
        if (_kernel == null)
        {
            return string.Empty;
        }

        try
        {
            var conversionPrompt =
                $@"Convert the following mathematical expression from {fromTool} format to {toTool} format:

From ({fromTool}): {expression}

Ensure the mathematical meaning is preserved while adapting to the target tool's syntax and conventions.
Return only the converted expression.";

            var chatService = _kernel.GetRequiredService<IChatCompletionService>();
            var chatHistory = new ChatHistory();
            chatHistory.AddSystemMessage(GetConversionInstructions());
            chatHistory.AddUserMessage(conversionPrompt);

            var response = await chatService.GetChatMessageContentAsync(chatHistory);
            return response.Content ?? string.Empty;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error converting expression from {FromTool} to {ToTool}", fromTool, toTool);
            return string.Empty;
        }
    }

    public Task<List<FormalizationResult>> GetFormalizationHistoryAsync(string theoryId)
    {
        var history = State.CompletedFormalizations
            .Where(f => f.TheoryId == theoryId)
            .OrderByDescending(f => f.CreatedAt)
            .ToList();

        return Task.FromResult(history);
    }

    public Task<Dictionary<string, int>> GetFormalizationStatsAsync()
    {
        var stats = new Dictionary<string, int>(State.FormalizationStats)
        {
            ["Total"] = State.CompletedFormalizations.Count,
            ["Valid"] = State.CompletedFormalizations.Count(f => f.IsValid),
            ["Invalid"] = State.CompletedFormalizations.Count(f => !f.IsValid),
            ["Pending"] = State.PendingFormalizationIds.Count
        };

        foreach (var tool in State.SupportedTools)
        {
            stats[tool] = State.CompletedFormalizations.Count(f => f.FormalizationTool == tool);
        }

        return Task.FromResult(stats);
    }

    public Task<List<string>> GetSupportedFormalizationToolsAsync()
    {
        return Task.FromResult(State.SupportedTools.ToList());
    }

    public async Task<string> GenerateProofSketchAsync(string formalExpression)
    {
        if (_kernel == null)
        {
            return string.Empty;
        }

        try
        {
            var proofPrompt = $@"Generate a proof sketch for the following formal mathematical expression:

Expression: {formalExpression}

Provide a structured proof outline with:
1. Key steps in the proof
2. Required lemmas or theorems
3. Main proof techniques
4. Potential challenges or edge cases

Keep the sketch concise but comprehensive.";

            var chatService = _kernel.GetRequiredService<IChatCompletionService>();
            var chatHistory = new ChatHistory();
            chatHistory.AddSystemMessage(GetProofSketchInstructions());
            chatHistory.AddUserMessage(proofPrompt);

            var response = await chatService.GetChatMessageContentAsync(chatHistory);
            return response.Content ?? string.Empty;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error generating proof sketch for expression: {Expression}", formalExpression);
            return string.Empty;
        }
    }

    private async Task<string> GenerateFormalExpressionAsync(string theoryContent, string targetTool)
    {
        if (_kernel == null)
        {
            return string.Empty;
        }

        var formalizationPrompt =
            $@"Convert the following natural language theory to a formal mathematical expression using {targetTool}:

Theory: {theoryContent}

Requirements:
1. Use {targetTool} syntax and conventions
2. Preserve all mathematical meaning
3. Include appropriate quantifiers, operators, and symbols
4. Ensure the expression is syntactically correct
5. Add type annotations where necessary

Return only the formal expression without explanations.";

        var chatService = _kernel.GetRequiredService<IChatCompletionService>();
        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(GetFormalizationInstructions());
        chatHistory.AddUserMessage(formalizationPrompt);

        var response = await chatService.GetChatMessageContentAsync(chatHistory);
        return response.Content ?? string.Empty;
    }

    private double CalculateConfidenceScore(string expression, bool isValid)
    {
        double score = 0.0;

        // Base score from validation
        if (isValid) score += 0.5;

        // Score based on expression complexity and completeness
        if (!string.IsNullOrEmpty(expression))
        {
            score += 0.2;

            // Additional points for mathematical symbols
            if (expression.Contains("∀") || expression.Contains("∃")) score += 0.1;
            if (expression.Contains("→") || expression.Contains("↔")) score += 0.1;
            if (expression.Contains("∧") || expression.Contains("∨")) score += 0.1;
        }

        return Math.Min(1.0, score);
    }

    private List<string> ExtractSymbolicComponents(string expression)
    {
        var components = new List<string>();

        // Extract mathematical symbols and operators
        var symbols = new[] { "∀", "∃", "→", "↔", "∧", "∨", "¬", "∈", "⊆", "⊇", "∩", "∪", "≡", "≠", "≤", "≥" };

        foreach (var symbol in symbols)
        {
            if (expression.Contains(symbol))
            {
                components.Add(symbol);
            }
        }

        return components.Distinct().ToList();
    }

    private Dictionary<string, string> GenerateSymbolMapping(string theoryContent, string formalExpression)
    {
        var mapping = new Dictionary<string, string>();

        // This is a simplified mapping - in practice, you'd want more sophisticated NLP
        if (theoryContent.Contains("for all") && formalExpression.Contains("∀"))
        {
            mapping["for all"] = "∀";
        }

        if (theoryContent.Contains("exists") && formalExpression.Contains("∃"))
        {
            mapping["exists"] = "∃";
        }

        if (theoryContent.Contains("implies") && formalExpression.Contains("→"))
        {
            mapping["implies"] = "→";
        }

        return mapping;
    }

    private async Task InitializeSymbolLibraryAsync()
    {
        var symbolLibrary = new Dictionary<string, string>
        {
            // Logic symbols
            ["forall"] = "∀",
            ["exists"] = "∃",
            ["implies"] = "→",
            ["iff"] = "↔",
            ["and"] = "∧",
            ["or"] = "∨",
            ["not"] = "¬",

            // Set theory
            ["in"] = "∈",
            ["subset"] = "⊆",
            ["superset"] = "⊇",
            ["intersection"] = "∩",
            ["union"] = "∪",

            // Equivalence and comparison
            ["equivalent"] = "≡",
            ["not_equal"] = "≠",
            ["less_equal"] = "≤",
            ["greater_equal"] = "≥",

            // Special symbols
            ["infinity"] = "∞",
            ["phi"] = "φ",
            ["psi"] = "ψ",
            ["omega"] = "ω",
            ["alpha"] = "α",
            ["beta"] = "β",
            ["gamma"] = "γ",
            ["delta"] = "δ"
        };

        // Update state through event
        State.SymbolLibrary = symbolLibrary;
    }

    protected override void AIGAgentTransitionState(FormalizationState state,
        StateLogEventBase<FormalizationStateLogEvent> @event)
    {
        switch (@event)
        {
            case FormalizationInitializedLogEvent initEvent:
                state.Initialized = true;
                state.SupportedTools = initEvent.SupportedTools;
                foreach (var supportedTool in initEvent.SupportedTools)
                {
                    state.FormalizationStats[supportedTool] = 0;
                }

                break;

            case FormalizationRequestedLogEvent requestEvent:
                if (!state.PendingFormalizationIds.Contains(requestEvent.TheoryId))
                {
                    state.PendingFormalizationIds.Add(requestEvent.TheoryId);
                }

                break;

            case FormalizationCompletedLogEvent completedEvent:
                state.CompletedFormalizations.Add(completedEvent.Result);
                state.LastFormalizationTime = DateTime.UtcNow;

                // Update stats
                var formalizationTool = completedEvent.Result.FormalizationTool;
                state.FormalizationStats.TryGetValue(formalizationTool, out var count);
                state.FormalizationStats[formalizationTool] = count + 1;

                // Remove from pending
                state.PendingFormalizationIds.Remove(completedEvent.Result.TheoryId);
                break;
        }
    }

    private string GetFormalizationInstructions()
    {
        return
            @"You are an expert in mathematical formalization, skilled at converting natural language mathematical statements into formal expressions using various tools like SymPy, Z3, Lean, Coq, and Isabelle.

Your key capabilities include:
1. Converting natural language to formal mathematical notation
2. Ensuring syntactic correctness for specific tools
3. Preserving mathematical meaning and logical structure
4. Using appropriate quantifiers, operators, and type annotations
5. Validating formal expressions for correctness

When formalizing theories:
- Identify the mathematical structure and logical relationships
- Choose appropriate formal notation for the target tool
- Ensure all variables are properly quantified
- Use standard mathematical symbols and conventions
- Maintain logical equivalence with the original statement

Be precise, rigorous, and tool-aware in your formalizations.";
    }

    private string GetValidationInstructions(string tool)
    {
        return
            $@"You are a mathematical validation expert for {tool}. Your task is to validate formal mathematical expressions for:

1. Syntax correctness according to {tool} standards
2. Logical consistency and mathematical validity
3. Proper use of quantifiers and operators
4. Type correctness and variable binding
5. Tool-specific formatting requirements

For {tool}, pay special attention to:
- Proper syntax for mathematical operations
- Correct use of mathematical symbols
- Appropriate type declarations
- Valid logical structure

Return 'VALID' only if the expression is completely correct, otherwise return 'INVALID' with specific issues identified.";
    }

    private string GetConversionInstructions()
    {
        return @"You are an expert in converting mathematical expressions between different formal systems and tools.

Your task is to translate expressions while:
1. Preserving the exact mathematical meaning
2. Adapting syntax to the target tool's conventions
3. Maintaining logical equivalence
4. Using appropriate symbols and operators for the target system
5. Ensuring the result is syntactically correct

Common conversion patterns:
- SymPy ↔ Z3: Different syntax for functions and operators
- Lean ↔ Coq: Different type systems and proof tactics
- Isabelle ↔ Others: Different notation conventions

Focus on accuracy and tool-specific requirements.";
    }

    private string GetProofSketchInstructions()
    {
        return @"You are a mathematical proof assistant expert at generating structured proof sketches.

Your task is to analyze formal mathematical expressions and provide:
1. A high-level proof strategy
2. Key steps and milestones in the proof
3. Required lemmas, theorems, or axioms
4. Main proof techniques (induction, contradiction, construction, etc.)
5. Potential challenges or edge cases

Structure your proof sketches with:
- Clear step-by-step outline
- Logical flow and dependencies
- Identification of non-trivial steps
- Suggestions for proof techniques
- References to relevant mathematical knowledge

Keep sketches concise but comprehensive enough to guide actual proof development.";
    }
}