using Aevatar.Core.Abstractions;

namespace Aevatar.Workshop.GAgent.Events;

/// <summary>
/// Base event for theory reasoning system
/// </summary>
[GenerateSerializer]
public class TheoryReasoningEvent : EventBase
{
    [Id(0)] public string ReasoningSessionId { get; set; } = string.Empty;
    [Id(1)] public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Event triggered when a new theory is proposed
/// </summary>
[GenerateSerializer]
public class TheoryProposedEvent : TheoryReasoningEvent
{
    [Id(0)] public string TheoryId { get; set; } = string.Empty;
    [Id(1)] public string TheoryType { get; set; } = string.Empty; // A, C, P, T, D, L, M
    [Id(2)] public string TheoryNumber { get; set; } = string.Empty;
    [Id(3)] public string TheoryContent { get; set; } = string.Empty;
    [Id(4)] public List<string> Dependencies { get; set; } = new();
    [Id(5)] public string ReasoningMethod { get; set; } = string.Empty;
}

/// <summary>
/// Event triggered when formalization is completed
/// </summary>
[GenerateSerializer]
public class FormalizationCompletedEvent : TheoryReasoningEvent
{
    [Id(0)] public string TheoryId { get; set; } = string.Empty;
    [Id(1)] public string FormalExpression { get; set; } = string.Empty;
    [Id(2)] public string FormalizationTool { get; set; } = string.Empty; // SymPy, Z3, etc.
    [Id(3)] public bool IsValid { get; set; }
    [Id(4)] public string ValidationDetails { get; set; } = string.Empty;
}

/// <summary>
/// Event triggered when Python verification is completed
/// </summary>
[GenerateSerializer]
public class VerificationCompletedEvent : TheoryReasoningEvent
{
    [Id(0)] public string TheoryId { get; set; } = string.Empty;
    [Id(1)] public string PythonCode { get; set; } = string.Empty;
    [Id(2)] public bool TestsPassed { get; set; }
    [Id(3)] public string TestResults { get; set; } = string.Empty;
    [Id(4)] public List<string> TestCases { get; set; } = new();
    [Id(5)] public double ExecutionTime { get; set; }
}

/// <summary>
/// Event triggered when equivalence checking is completed
/// </summary>
[GenerateSerializer]
public class EquivalenceCheckedEvent : TheoryReasoningEvent
{
    [Id(0)] public string TheoryId { get; set; } = string.Empty;
    [Id(1)] public bool TheoryFormalizationEquivalent { get; set; }
    [Id(2)] public bool FormalizationProgramEquivalent { get; set; }
    [Id(3)] public bool TheoryProgramEquivalent { get; set; }
    [Id(4)] public string EquivalenceDetails { get; set; } = string.Empty;
    [Id(5)] public List<string> DiscrepancyReports { get; set; } = new();
}

/// <summary>
/// Event triggered when a theory is accepted into the knowledge base
/// </summary>
[GenerateSerializer]
public class TheoryAcceptedEvent : TheoryReasoningEvent
{
    [Id(0)] public string TheoryId { get; set; } = string.Empty;
    [Id(1)] public string FinalTheoryContent { get; set; } = string.Empty;
    [Id(2)] public string FinalFormalization { get; set; } = string.Empty;
    [Id(3)] public string FinalPythonCode { get; set; } = string.Empty;
    [Id(4)] public double QualityScore { get; set; }
    [Id(5)] public DateTime AcceptedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Event triggered when a theory is rejected
/// </summary>
[GenerateSerializer]
public class TheoryRejectedEvent : TheoryReasoningEvent
{
    [Id(0)] public string TheoryId { get; set; } = string.Empty;
    [Id(1)] public string RejectionReason { get; set; } = string.Empty;
    [Id(2)] public List<string> Issues { get; set; } = new();
}

/// <summary>
/// Event triggered when a reasoning task is started
/// </summary>
[GenerateSerializer]
public class ReasoningTaskStartedEvent : TheoryReasoningEvent
{
    [Id(0)] public string TaskId { get; set; } = string.Empty;
    [Id(1)] public string TaskType { get; set; } = string.Empty; // deductive, inductive, abductive, analogical
    [Id(2)] public string TargetDomain { get; set; } = string.Empty;
    [Id(3)] public List<string> SourceTheories { get; set; } = new();
    [Id(4)] public string ReasoningGoal { get; set; } = string.Empty;
}

/// <summary>
/// Event triggered when a reasoning task is completed
/// </summary>
[GenerateSerializer]
public class ReasoningTaskCompletedEvent : TheoryReasoningEvent
{
    [Id(0)] public string TaskId { get; set; } = string.Empty;
    [Id(1)] public List<string> GeneratedTheoryIds { get; set; } = new();
    [Id(2)] public bool Success { get; set; }
    [Id(3)] public string CompletionDetails { get; set; } = string.Empty;
    [Id(4)] public double ReasoningTime { get; set; }
}

/// <summary>
/// AI thinking step data structure for events
/// </summary>
[GenerateSerializer]
public class ThinkingStepData
{
    [Id(0)] public string StepId { get; set; } = string.Empty;
    [Id(1)] public string StepType { get; set; } = string.Empty; // analysis, synthesis, evaluation, conclusion
    [Id(2)] public string Content { get; set; } = string.Empty;
    [Id(3)] public string Reasoning { get; set; } = string.Empty;
    [Id(4)] public Dictionary<string, string> Metadata { get; set; } = new();
    [Id(5)] public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Event triggered when AI thinking steps are generated during reasoning
/// </summary>
[GenerateSerializer]
public class ThinkingStepsGeneratedEvent : TheoryReasoningEvent
{
    [Id(0)] public string TaskId { get; set; } = string.Empty;
    [Id(1)] public string ReasoningType { get; set; } = string.Empty; // deductive, inductive, etc.
    [Id(2)] public List<ThinkingStepData> ThinkingSteps { get; set; } = new();
    [Id(3)] public string IterationPhase { get; set; } = string.Empty;
    [Id(4)] public int CurrentIteration { get; set; }
}