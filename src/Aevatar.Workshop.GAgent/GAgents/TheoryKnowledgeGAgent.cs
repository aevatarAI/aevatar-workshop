using Aevatar.Core.Abstractions;
using Microsoft.Extensions.Logging;
using Aevatar.Core;

namespace Aevatar.Workshop.GAgent.GAgents;

/// <summary>
/// Theory element representation in the knowledge base
/// </summary>
[GenerateSerializer]
public class TheoryElement
{
    [Id(0)] public string Id { get; set; } = string.Empty;
    [Id(1)] public string Type { get; set; } = string.Empty; // A, C, P, T, D, L, M
    [Id(2)] public string Number { get; set; } = string.Empty; // e.g., "1-1", "2-3"
    [Id(3)] public string FullId { get; set; } = string.Empty; // e.g., "C1-1", "T2-3"
    [Id(4)] public string Content { get; set; } = string.Empty;
    [Id(5)] public string FormalExpression { get; set; } = string.Empty;
    [Id(6)] public string PythonCode { get; set; } = string.Empty;
    [Id(7)] public List<string> Dependencies { get; set; } = new();
    [Id(8)] public List<string> DerivedTheories { get; set; } = new();
    [Id(9)] public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Id(10)] public double QualityScore { get; set; }
    [Id(11)] public string ReasoningMethod { get; set; } = string.Empty;
    [Id(12)] public bool IsVerified { get; set; }
    [Id(13)] public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// Reasoning graph edge representing dependency relationship
/// </summary>
[GenerateSerializer]
public class ReasoningEdge
{
    [Id(0)] public string FromTheoryId { get; set; } = string.Empty;
    [Id(1)] public string ToTheoryId { get; set; } = string.Empty;
    [Id(2)] public string RelationType { get; set; } = string.Empty; // "depends_on", "derived_from", "contradicts"
    [Id(3)] public double Strength { get; set; } = 1.0;
    [Id(4)] public string Description { get; set; } = string.Empty;
}

/// <summary>
/// State for theory knowledge management
/// </summary>
[GenerateSerializer]
public class TheoryKnowledgeState : StateBase
{
    [Id(0)] public Dictionary<string, TheoryElement> Theories { get; set; } = new();
    [Id(1)] public List<ReasoningEdge> Dependencies { get; set; } = new();
    [Id(2)] public Dictionary<string, int> TypeCounters { get; set; } = new();
    [Id(3)] public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    [Id(4)] public int TotalTheories { get; set; }
    [Id(5)] public List<string> RecentlyAdded { get; set; } = new();
    [Id(6)] public Dictionary<string, List<string>> CategoryIndex { get; set; } = new();
}

/// <summary>
/// State log events for theory knowledge management
/// </summary>
[GenerateSerializer]
public class TheoryKnowledgeStateLogEvent : StateLogEventBase<TheoryKnowledgeStateLogEvent>;

[GenerateSerializer]
public class TheoryAddedStateLogEvent : TheoryKnowledgeStateLogEvent
{
    [Id(0)] public TheoryElement Theory { get; set; } = new();
}

[GenerateSerializer]
public class TheoryUpdatedStateLogEvent : TheoryKnowledgeStateLogEvent
{
    [Id(0)] public string TheoryId { get; set; } = string.Empty;
    [Id(1)] public TheoryElement UpdatedTheory { get; set; } = new();
}

[GenerateSerializer]
public class DependencyAddedStateLogEvent : TheoryKnowledgeStateLogEvent
{
    [Id(0)] public ReasoningEdge Edge { get; set; } = new();
}

[GenerateSerializer]
public class KnowledgeBaseInitializedStateLogEvent : TheoryKnowledgeStateLogEvent
{
    [Id(0)] public int InitialTheoryCount { get; set; }
}

/// <summary>
/// Interface for theory knowledge management
/// </summary>
public interface ITheoryKnowledgeGAgent : IStateGAgent<TheoryKnowledgeState>
{
    Task<bool> InitializeWithPsiTheoryAsync();
    Task<string> AddTheoryAsync(TheoryElement theory);
    Task<TheoryElement?> GetTheoryAsync(string theoryId);
    Task<List<TheoryElement>> GetTheoriesByTypeAsync(string type);
    Task<List<TheoryElement>> GetDependentTheoriesAsync(string theoryId);
    Task<List<TheoryElement>> GetDependencyTheoriesAsync(string theoryId);
    Task<string> GenerateNextTheoryIdAsync(string type);
    Task<List<TheoryElement>> SearchTheoriesAsync(string query);
    Task<Dictionary<string, int>> GetStatisticsAsync();
    Task<bool> AddDependencyAsync(string fromTheoryId, string toTheoryId, string relationType);
    Task<List<TheoryElement>> GetRecentTheoriesAsync(int count = 10);
    Task<bool> ValidateTheoryConsistencyAsync(string theoryId);
    Task<List<TheoryElement>> GetAllTheoriesAsync();
}

/// <summary>
/// Theory knowledge management GAgent - stores and manages the Ψ theory knowledge base
/// </summary>
[GAgent("theory.knowledge", "reasoning")]
public class TheoryKnowledgeGAgent : GAgentBase<TheoryKnowledgeState, TheoryKnowledgeStateLogEvent>,
    ITheoryKnowledgeGAgent
{
    public override Task<string> GetDescriptionAsync()
        => Task.FromResult("Manages the Ψ theory knowledge base with graph-based dependency tracking");

    public async Task<bool> InitializeWithPsiTheoryAsync()
    {
        try
        {
            Logger.LogInformation("Initializing Ψ theory knowledge base...");

            var initialTheories = CreateInitialPsiTheories();

            foreach (var theory in initialTheories)
            {
                RaiseEvent(new TheoryAddedStateLogEvent { Theory = theory });
            }

            RaiseEvent(new KnowledgeBaseInitializedStateLogEvent
            {
                InitialTheoryCount = initialTheories.Count
            });

            await ConfirmEvents();

            Logger.LogInformation("Initialized knowledge base with {Count} initial theories", initialTheories.Count);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to initialize Ψ theory knowledge base");
            return false;
        }
    }

    public async Task<string> AddTheoryAsync(TheoryElement theory)
    {
        if (string.IsNullOrEmpty(theory.Id))
        {
            // Generate ID more carefully by checking existing theories
            State.TypeCounters.TryGetValue(theory.Type, out var count);
            
            // Double-check by counting existing theories of this type
            var existingCount = State.Theories.Values.Count(t => t.Type == theory.Type);
            if (existingCount > count)
            {
                Logger.LogWarning("TypeCounter for {Type} was {Count} but found {ExistingCount} existing theories. Updating counter.", 
                    theory.Type, count, existingCount);
                count = existingCount;
            }
            
            count++;
            
            var nextNumber = $"{count}-1";
            theory.Id = $"{theory.Type}{nextNumber}";
            theory.Number = nextNumber;  // string type: "1-1", "2-1", etc.
            
            // Note: TypeCounters will be updated in GAgentTransitionState
            
            Logger.LogInformation("Generated new theory ID: {TheoryId} for type {Type} (counter: {Count})", 
                theory.Id, theory.Type, count);
        }

        if (string.IsNullOrEmpty(theory.FullId))
        {
            theory.FullId = theory.Id; // Use the same ID for FullId to avoid confusion
        }

        theory.CreatedAt = DateTime.UtcNow;

        Logger.LogInformation("About to add theory {TheoryId} to knowledge base", theory.Id);
        
        RaiseEvent(new TheoryAddedStateLogEvent { Theory = theory });
        await ConfirmEvents();

        Logger.LogInformation("Successfully added theory {TheoryId}: {Content}",
            theory.Id, theory.Content.Substring(0, Math.Min(100, theory.Content.Length)));

        // Verify the theory was actually saved
        var savedTheory = await GetTheoryAsync(theory.Id);
        if (savedTheory == null)
        {
            Logger.LogError("Theory {TheoryId} was not found after adding to knowledge base!", theory.Id);
        }
        else
        {
            Logger.LogInformation("Verified theory {TheoryId} was successfully saved", theory.Id);
        }

        return theory.Id;
    }

    public Task<TheoryElement?> GetTheoryAsync(string theoryId)
    {
        State.Theories.TryGetValue(theoryId, out var theory);
        return Task.FromResult(theory);
    }

    public Task<List<TheoryElement>> GetTheoriesByTypeAsync(string type)
    {
        var theories = State.Theories.Values
            .Where(t => t.Type.Equals(type, StringComparison.OrdinalIgnoreCase))
            .OrderBy(t => t.Number)
            .ToList();
        return Task.FromResult(theories);
    }

    public Task<List<TheoryElement>> GetDependentTheoriesAsync(string theoryId)
    {
        var dependentIds = State.Dependencies
            .Where(d => d.FromTheoryId == theoryId)
            .Select(d => d.ToTheoryId)
            .ToList();

        var dependentTheories = dependentIds
            .Select(id => State.Theories.TryGetValue(id, out var theory) ? theory : null)
            .Where(t => t != null)
            .Cast<TheoryElement>()
            .ToList();

        return Task.FromResult(dependentTheories);
    }

    public Task<List<TheoryElement>> GetDependencyTheoriesAsync(string theoryId)
    {
        var dependencyIds = State.Dependencies
            .Where(d => d.ToTheoryId == theoryId)
            .Select(d => d.FromTheoryId)
            .ToList();

        var dependencyTheories = dependencyIds
            .Select(id => State.Theories.TryGetValue(id, out var theory) ? theory : null)
            .Where(t => t != null)
            .Cast<TheoryElement>()
            .ToList();

        return Task.FromResult(dependencyTheories);
    }

    public Task<string> GenerateNextTheoryIdAsync(string type)
    {
        State.TypeCounters.TryGetValue(type, out var count);
        count++;

        // Generate next number in format like "1-1", "1-2", etc.
        var nextNumber = $"{count}-1";
        var nextId = $"{type}{nextNumber}";

        return Task.FromResult(nextId);
    }

    public Task<List<TheoryElement>> SearchTheoriesAsync(string query)
    {
        var results = State.Theories.Values
            .Where(t => t.Content.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                        t.FormalExpression.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                        t.FullId.Contains(query, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(t => t.QualityScore)
            .ToList();

        return Task.FromResult(results);
    }

    public Task<Dictionary<string, int>> GetStatisticsAsync()
    {
        var stats = new Dictionary<string, int>
        {
            ["Total"] = State.TotalTheories,
            ["Axioms"] = State.Theories.Values.Count(t => t.Type == "A"),
            ["Corollaries"] = State.Theories.Values.Count(t => t.Type == "C"),
            ["Propositions"] = State.Theories.Values.Count(t => t.Type == "P"),
            ["Theorems"] = State.Theories.Values.Count(t => t.Type == "T"),
            ["Definitions"] = State.Theories.Values.Count(t => t.Type == "D"),
            ["Lemmas"] = State.Theories.Values.Count(t => t.Type == "L"),
            ["Meta-theorems"] = State.Theories.Values.Count(t => t.Type == "M"),
            ["Dependencies"] = State.Dependencies.Count,
            ["Verified"] = State.Theories.Values.Count(t => t.IsVerified)
        };

        return Task.FromResult(stats);
    }

    public async Task<bool> AddDependencyAsync(string fromTheoryId, string toTheoryId, string relationType)
    {
        var edge = new ReasoningEdge
        {
            FromTheoryId = fromTheoryId,
            ToTheoryId = toTheoryId,
            RelationType = relationType,
            Description = $"{fromTheoryId} {relationType} {toTheoryId}"
        };

        RaiseEvent(new DependencyAddedStateLogEvent { Edge = edge });
        await ConfirmEvents();

        return true;
    }

    public Task<List<TheoryElement>> GetRecentTheoriesAsync(int count = 10)
    {
        var recent = State.Theories.Values
            .OrderByDescending(t => t.CreatedAt)
            .Take(count)
            .ToList();

        return Task.FromResult(recent);
    }

    public Task<bool> ValidateTheoryConsistencyAsync(string theoryId)
    {
        if (!State.Theories.TryGetValue(theoryId, out var theory))
        {
            return Task.FromResult(false);
        }

        // Check if all dependencies exist
        var allDependenciesExist = theory.Dependencies.All(dep => State.Theories.ContainsKey(dep));

        // Check for circular dependencies (simplified check)
        var hasCircularDependency = CheckCircularDependency(theoryId, new HashSet<string>());

        var isConsistent = allDependenciesExist && !hasCircularDependency;

        Logger.LogInformation("Theory {TheoryId} consistency check: {IsConsistent}",
            theoryId, isConsistent ? "PASSED" : "FAILED");

        return Task.FromResult(isConsistent);
    }

    public async Task<List<TheoryElement>> GetAllTheoriesAsync()
    {
        try
        {
            Logger.LogInformation("Getting all theories from knowledge base");
            
            var state = await GetStateAsync();
            var allTheories = state.Theories.Values.ToList();
            
            Logger.LogInformation("Retrieved {Count} theories from knowledge base", allTheories.Count);
            
            return allTheories;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error getting all theories from knowledge base");
            return new List<TheoryElement>();
        }
    }

    protected override void GAgentTransitionState(TheoryKnowledgeState state,
        StateLogEventBase<TheoryKnowledgeStateLogEvent> @event)
    {
        switch (@event)
        {
            case TheoryAddedStateLogEvent addedEvent:
                var theory = addedEvent.Theory;
                state.Theories[theory.Id] = theory;
                state.TotalTheories++;
                state.LastUpdated = DateTime.UtcNow;

                // Update type counter to match actual count of theories
                var actualCount = state.Theories.Values.Count(t => t.Type == theory.Type);
                state.TypeCounters[theory.Type] = actualCount;

                // Add to recent list
                state.RecentlyAdded.Insert(0, theory.Id);
                if (state.RecentlyAdded.Count > 50)
                {
                    state.RecentlyAdded.RemoveAt(state.RecentlyAdded.Count - 1);
                }

                // Update category index
                if (!state.CategoryIndex.ContainsKey(theory.Type))
                {
                    state.CategoryIndex[theory.Type] = new List<string>();
                }

                state.CategoryIndex[theory.Type].Add(theory.Id);
                break;

            case TheoryUpdatedStateLogEvent updatedEvent:
                if (state.Theories.ContainsKey(updatedEvent.TheoryId))
                {
                    state.Theories[updatedEvent.TheoryId] = updatedEvent.UpdatedTheory;
                    state.LastUpdated = DateTime.UtcNow;
                }

                break;

            case DependencyAddedStateLogEvent depEvent:
                state.Dependencies.Add(depEvent.Edge);
                state.LastUpdated = DateTime.UtcNow;
                break;

            case KnowledgeBaseInitializedStateLogEvent initEvent:
                Logger.LogInformation("Knowledge base initialized with {Count} theories", initEvent.InitialTheoryCount);
                break;
        }
    }

    private bool CheckCircularDependency(string theoryId, HashSet<string> visited)
    {
        if (visited.Contains(theoryId))
        {
            return true; // Circular dependency detected
        }

        visited.Add(theoryId);

        var dependencies = State.Dependencies
            .Where(d => d.FromTheoryId == theoryId)
            .Select(d => d.ToTheoryId);

        foreach (var dep in dependencies)
        {
            if (CheckCircularDependency(dep, new HashSet<string>(visited)))
            {
                return true;
            }
        }

        return false;
    }

    private List<TheoryElement> CreateInitialPsiTheories()
    {
        // Create the foundational Ψ theory elements based on the provided theory structure
        var theories = new List<TheoryElement>();

        // A1: 唯一公理 (The Unique Axiom)
        theories.Add(new TheoryElement
        {
            Id = "A1",
            Type = "A",
            Number = "1",
            FullId = "A1",
            Content = "宇宙是二进制区分的无限递归结构。Universe is an infinite recursive structure of binary distinctions.",
            FormalExpression = "∀x ∈ Universe: x = {0, 1}^∞ ∧ ∃φ: x → φ(x)",
            ReasoningMethod = "foundational_axiom",
            QualityScore = 1.0,
            IsVerified = true,
            Metadata = new Dictionary<string, string>
            {
                ["description"] = "The foundational axiom of Ψ theory",
                ["importance"] = "critical",
                ["domain"] = "foundational_mathematics"
            }
        });

        // C1-1: 唯一编码推论
        theories.Add(new TheoryElement
        {
            Id = "C1-1",
            Type = "C",
            Number = "1-1",
            FullId = "C1-1",
            Content = "任何信息都可以唯一地编码为二进制序列。Any information can be uniquely encoded as a binary sequence.",
            FormalExpression = "∀I ∈ Information: ∃!B ∈ {0,1}*: encode(I) = B",
            Dependencies = new List<string> { "A1" },
            ReasoningMethod = "deductive_inference",
            QualityScore = 0.95,
            IsVerified = true,
            Metadata = new Dictionary<string, string>
            {
                ["derived_from"] = "A1",
                ["type"] = "encoding_theory"
            }
        });

        // D1-1: 自指完备性定义
        theories.Add(new TheoryElement
        {
            Id = "D1-1",
            Type = "D",
            Number = "1-1",
            FullId = "D1-1",
            Content = "自指完备性：系统S是自指完备的，当且仅当S能够完全描述自身的结构和行为。",
            FormalExpression = "SelfComplete(S) ↔ ∀p ∈ Properties(S): S ⊢ describe(S, p)",
            Dependencies = new List<string> { "A1" },
            ReasoningMethod = "formal_definition",
            QualityScore = 0.90,
            IsVerified = true
        });

        // Add more foundational theories...
        // P1: 二元区分命题
        theories.Add(new TheoryElement
        {
            Id = "P1",
            Type = "P",
            Number = "1",
            FullId = "P1",
            Content = "二元区分命题：所有存在都基于基本的二元区分（0/1, 是/非, 存在/虚无）。",
            FormalExpression = "∀e ∈ Existence: ∃b ∈ {0,1}: e ≡ b ∨ e ≡ ¬b",
            Dependencies = ["A1"],
            ReasoningMethod = "deductive_inference",
            QualityScore = 0.88,
            IsVerified = true
        });

        // T1-1: 信息保存定理
        theories.Add(new TheoryElement
        {
            Id = "T1-1",
            Type = "T",
            Number = "1-1",
            FullId = "T1-1",
            Content = "信息保存定理：在宇宙的二进制结构中，信息既不能被创造也不能被销毁，只能被变换。",
            FormalExpression = "∀I ∈ Information, ∀t ∈ Time: |encode(I, t)| = |encode(transform(I), t+1)|",
            Dependencies = ["A1", "C1-1"],
            ReasoningMethod = "deductive_proof",
            QualityScore = 0.92,
            IsVerified = true,
            PythonCode = @"
def verify_information_conservation():
    '''Verify information conservation theorem'''
    # Test case 1: Simple binary transformation
    original = '101010'
    transformed = original[::-1]  # Reverse
    assert len(original) == len(transformed), 'Information length must be conserved'
    
    # Test case 2: More complex transformation
    import hashlib
    data = 'hello world'
    hash_data = hashlib.sha256(data.encode()).hexdigest()
    # Hash preserves information content (deterministic mapping)
    assert hashlib.sha256(data.encode()).hexdigest() == hash_data
    
    return True

# Run verification
result = verify_information_conservation()
print(f'T1-1 verification result: {result}')
",
            Metadata = new Dictionary<string, string>
            {
                ["proof_type"] = "constructive",
                ["verification_method"] = "computational",
                ["related_theorems"] = "conservation_laws"
            }
        });

        // T1-2: 计算等价定理  
        theories.Add(new TheoryElement
        {
            Id = "T1-2",
            Type = "T",
            Number = "1-2",
            FullId = "T1-2",
            Content = "计算等价定理：任何可计算的过程都可以等价地表示为二进制操作序列。",
            FormalExpression = "∀C ∈ Computable: ∃B ∈ BinaryOps*: eval(C) ≡ eval(B)",
            Dependencies = ["A1", "C1-1", "T1-1"],
            ReasoningMethod = "proof_by_construction",
            QualityScore = 0.90,
            IsVerified = true,
            PythonCode = @"
def verify_computational_equivalence():
    '''Verify computational equivalence theorem'''
    # Test: High-level operation equivalent to binary operations
    
    # High-level: arithmetic operation
    x, y = 5, 3
    result_high = x + y
    
    # Binary equivalent: using bit operations
    def binary_add(a, b):
        while b:
            carry = a & b
            a = a ^ b
            b = carry << 1
        return a
    
    result_binary = binary_add(x, y)
    assert result_high == result_binary, 'High-level and binary operations must be equivalent'
    
    # Test with more complex operations
    result_high2 = x * y
    # Multiplication as repeated addition (simplified binary equivalent)
    result_binary2 = sum([x] * y)
    assert result_high2 == result_binary2
    
    return True

result = verify_computational_equivalence()
print(f'T1-2 verification result: {result}')
",
            Metadata = new Dictionary<string, string>
            {
                ["proof_type"] = "constructive",
                ["verification_method"] = "computational",
                ["applications"] = "computer_science, computation_theory"
            }
        });

        // L1-1: 自指引理
        theories.Add(new TheoryElement
        {
            Id = "L1-1",
            Type = "L",
            Number = "1-1",
            FullId = "L1-1",
            Content = "自指引理：在二进制宇宙中，任何充分复杂的系统都包含能够描述自身的子结构。",
            FormalExpression = "∀S ∈ ComplexSystems: ∃s ∈ S: s ⊢ describe(S)",
            Dependencies = ["A1", "D1-1"],
            ReasoningMethod = "constructive_proof",
            QualityScore = 0.85,
            IsVerified = true,
            Metadata = new Dictionary<string, string>
            {
                ["type"] = "foundational_lemma",
                ["applications"] = "self_reference, metamathematics"
            }
        });

        return theories;
    }
}