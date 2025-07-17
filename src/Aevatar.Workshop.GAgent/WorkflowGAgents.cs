using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.GroupChat.Core.Dto;
using GroupChat.GAgent;
using GroupChat.GAgent.Feature.Common;
using GroupChat.GAgent.GEvent;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using System.Globalization;
using GroupChat.GAgent.Dto;

namespace Aevatar.Workshop.GAgent;

[GenerateSerializer]
public class MathGAgentConfigDto : MemberConfigDto
{
}

[GenerateSerializer]
public class TimeConverterGAgentConfigDto : MemberConfigDto
{
}

[GenerateSerializer]
public class DataProcessorGAgentConfigDto : MemberConfigDto
{
    [Id(0)] public string ProcessingMode { get; set; } = "transform"; // transform, filter, aggregate
}

// Interfaces
public interface IWorkflowMathGAgent : IStateGAgent<WorkflowMathGAgentState>, IGAgent
{
    Task<double> CalculateAsync(string expression);
}

public interface IWorkflowTimeConverterGAgent : IStateGAgent<WorkflowTimeConverterGAgentState>, IGAgent
{
    Task<string> ConvertTimeAsync(string time, string fromZone, string toZone);
}

public interface IDataProcessorGAgent : IStateGAgent<DataProcessorGAgentState>, IGAgent
{
    Task<string> ProcessDataAsync(string data, string mode);
}

// Math GAgent Implementation
[GAgent("workflowmath", "workflow")]
public class WorkflowMathGAgent : MemberGAgentBase<WorkflowMathGAgentState, MathGAgentLogEvent, EventBase, MathGAgentConfigDto>, IWorkflowMathGAgent
{
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Math GAgent - Performs mathematical calculations in workflows");
    }
    
    public async Task<double> CalculateAsync(string expression)
    {
        try
        {
            // Simple math expression evaluator
            var result = EvaluateExpression(expression);
            Logger.LogInformation($"Calculated: {expression} = {result}");
            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"Failed to calculate expression: {expression}");
            throw;
        }
    }

    public Task ConfigAsync(MathGAgentConfigDto config)
    {
        throw new NotImplementedException();
    }

    protected override Task<int> GetInterestValueAsync(Guid blackboardId)
    {
        // Math agent has high interest when there are mathematical expressions
        return Task.FromResult(80);
    }
    
    protected override async Task<ChatResponse> ChatAsync(Guid blackboardId, List<ChatMessage>? coordinatorMessages)
    {
        var response = new ChatResponse();
        
        if (coordinatorMessages == null || coordinatorMessages.Count == 0)
        {
            response.Content = "No input provided for calculation";
            return response;
        }
        
        var lastMessage = coordinatorMessages.Last();
        var input = lastMessage.Content;
        
        try
        {
            // Extract mathematical expressions from input
            var expressions = ExtractMathExpressions(input);
            var results = new List<string>();
            
            foreach (var expr in expressions)
            {
                var result = await CalculateAsync(expr);
                results.Add($"{expr} = {result}");
            }
            
            if (results.Any())
            {
                response.Content = $"Calculations complete:\n{string.Join("\n", results)}";
            }
            else
            {
                response.Content = "No mathematical expressions found in the input";
            }
        }
        catch (Exception ex)
        {
            response.Content = $"Error during calculation: {ex.Message}";
        }
        
        return response;
    }
    
    private List<string> ExtractMathExpressions(string input)
    {
        // Simple regex to find math expressions
        var expressions = new List<string>();
        
        // Look for patterns like "calculate X" or "what is X"
        var patterns = new[]
        {
            @"calculate\s+(.+?)(?:\s|$)",
            @"what\s+is\s+(.+?)(?:\s|$)",
            @"compute\s+(.+?)(?:\s|$)",
            @"(\d+\s*[\+\-\*\/]\s*\d+)"
        };
        
        foreach (var pattern in patterns)
        {
            var matches = Regex.Matches(input, pattern, RegexOptions.IgnoreCase);
            foreach (Match match in matches)
            {
                if (match.Groups.Count > 1)
                {
                    expressions.Add(match.Groups[1].Value.Trim());
                }
            }
        }
        
        return expressions;
    }
    
    private double EvaluateExpression(string expression)
    {
        // Very simple expression evaluator for demo purposes
        // In production, use a proper expression evaluator library
        expression = expression.Replace(" ", "");
        
        // Handle basic operations
        if (expression.Contains("+"))
        {
            var parts = expression.Split('+');
            return double.Parse(parts[0]) + double.Parse(parts[1]);
        }
        else if (expression.Contains("-"))
        {
            var parts = expression.Split('-');
            return double.Parse(parts[0]) - double.Parse(parts[1]);
        }
        else if (expression.Contains("*"))
        {
            var parts = expression.Split('*');
            return double.Parse(parts[0]) * double.Parse(parts[1]);
        }
        else if (expression.Contains("/"))
        {
            var parts = expression.Split('/');
            return double.Parse(parts[0]) / double.Parse(parts[1]);
        }
        
        return double.Parse(expression);
    }
}

// Time Converter GAgent Implementation
[GAgent("workflowtime", "workflow")]
public class WorkflowTimeConverterGAgent : MemberGAgentBase<WorkflowTimeConverterGAgentState, TimeConverterGAgentLogEvent, EventBase, TimeConverterGAgentConfigDto>, IWorkflowTimeConverterGAgent
{
    private static readonly Dictionary<string, TimeZoneInfo> TimeZones = InitializeTimeZones();
    
    private static Dictionary<string, TimeZoneInfo> InitializeTimeZones()
    {
        var zones = new Dictionary<string, TimeZoneInfo>();
        zones["UTC"] = TimeZoneInfo.Utc;
        
        // Try different time zone IDs for cross-platform compatibility
        TryAddTimeZone(zones, "EST", "Eastern Standard Time", "America/New_York");
        TryAddTimeZone(zones, "PST", "Pacific Standard Time", "America/Los_Angeles");
        TryAddTimeZone(zones, "CST", "Central Standard Time", "America/Chicago");
        TryAddTimeZone(zones, "JST", "Tokyo Standard Time", "Asia/Tokyo");
        TryAddTimeZone(zones, "GMT", "GMT Standard Time", "Europe/London");
        
        return zones;
    }
    
    private static void TryAddTimeZone(Dictionary<string, TimeZoneInfo> zones, string key, params string[] possibleIds)
    {
        foreach (var id in possibleIds)
        {
            try
            {
                zones[key] = TimeZoneInfo.FindSystemTimeZoneById(id);
                return;
            }
            catch (TimeZoneNotFoundException)
            {
                // Try next ID
            }
        }
        // If all fail, use UTC as fallback
        zones[key] = TimeZoneInfo.Utc;
    }
    
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Time Converter GAgent - Converts times between different time zones");
    }
    
    public async Task<string> ConvertTimeAsync(string time, string fromZone, string toZone)
    {
        try
        {
            var dateTime = DateTime.Parse(time);
            
            if (!TimeZones.TryGetValue(fromZone.ToUpper(), out var fromTz))
                throw new ArgumentException($"Unknown time zone: {fromZone}");
                
            if (!TimeZones.TryGetValue(toZone.ToUpper(), out var toTz))
                throw new ArgumentException($"Unknown time zone: {toZone}");
            
            var utcTime = TimeZoneInfo.ConvertTimeToUtc(dateTime, fromTz);
            var convertedTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, toTz);
            
            return convertedTime.ToString("yyyy-MM-dd HH:mm:ss");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"Failed to convert time: {time} from {fromZone} to {toZone}");
            throw;
        }
    }
    
    protected override Task<int> GetInterestValueAsync(Guid blackboardId)
    {
        return Task.FromResult(75);
    }
    
    protected override async Task<ChatResponse> ChatAsync(Guid blackboardId, List<ChatMessage>? coordinatorMessages)
    {
        var response = new ChatResponse();
        
        if (coordinatorMessages == null || coordinatorMessages.Count == 0)
        {
            response.Content = "No input provided for time conversion";
            return response;
        }
        
        var lastMessage = coordinatorMessages.Last();
        var input = lastMessage.Content;
        
        try
        {
            // Extract time conversion request from input
            if (input.Contains("current time", StringComparison.OrdinalIgnoreCase))
            {
                var currentTime = DateTime.Now;
                response.Content = $"Current time: {currentTime:yyyy-MM-dd HH:mm:ss}";
            }
            else if (input.Contains("convert", StringComparison.OrdinalIgnoreCase))
            {
                // Simple parsing - in production use more sophisticated NLP
                response.Content = "Please specify time in format: 'convert HH:mm from ZONE to ZONE'";
            }
            else
            {
                response.Content = "Time conversion service ready. Ask me to convert times between time zones.";
            }
        }
        catch (Exception ex)
        {
            response.Content = $"Error during time conversion: {ex.Message}";
        }
        
        return response;
    }
}

// Data Processor GAgent Implementation
[GAgent("dataprocessor", "workflow")]
public class DataProcessorGAgent : MemberGAgentBase<DataProcessorGAgentState, DataProcessorGAgentLogEvent, EventBase, DataProcessorGAgentConfigDto>, IDataProcessorGAgent
{
    private string _processingMode = "transform";
    
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult("Data Processor GAgent - Processes and transforms data in workflows");
    }
    
    protected override async Task PerformConfigAsync(DataProcessorGAgentConfigDto configuration)
    {
        await base.PerformConfigAsync(configuration);
        _processingMode = configuration.ProcessingMode;
    }
    
    public async Task<string> ProcessDataAsync(string data, string mode)
    {
        try
        {
            return mode.ToLower() switch
            {
                "transform" => TransformData(data),
                "filter" => FilterData(data),
                "aggregate" => AggregateData(data),
                _ => throw new ArgumentException($"Unknown processing mode: {mode}")
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"Failed to process data with mode: {mode}");
            throw;
        }
    }
    
    protected override Task<int> GetInterestValueAsync(Guid blackboardId)
    {
        return Task.FromResult(70);
    }
    
    protected override async Task<ChatResponse> ChatAsync(Guid blackboardId, List<ChatMessage>? coordinatorMessages)
    {
        var response = new ChatResponse();
        
        if (coordinatorMessages == null || coordinatorMessages.Count == 0)
        {
            response.Content = "No data provided for processing";
            return response;
        }
        
        var input = string.Join("\n", coordinatorMessages.Select(m => m.Content));
        
        try
        {
            var result = await ProcessDataAsync(input, _processingMode);
            response.Content = $"Data processed ({_processingMode} mode):\n{result}";
        }
        catch (Exception ex)
        {
            response.Content = $"Error during data processing: {ex.Message}";
        }
        
        return response;
    }
    
    private string TransformData(string data)
    {
        // Example: Convert to uppercase and add timestamp
        return $"[TRANSFORMED {DateTime.Now:HH:mm:ss}] {data.ToUpper()}";
    }
    
    private string FilterData(string data)
    {
        // Example: Filter lines containing numbers
        var lines = data.Split('\n');
        var filtered = lines.Where(line => Regex.IsMatch(line, @"\d+"));
        return string.Join("\n", filtered);
    }
    
    private string AggregateData(string data)
    {
        // Example: Count words and lines
        var lines = data.Split('\n').Length;
        var words = data.Split(' ', '\n', '\t').Where(w => !string.IsNullOrWhiteSpace(w)).Count();
        var chars = data.Length;
        
        return $"Aggregation Results:\nLines: {lines}\nWords: {words}\nCharacters: {chars}";
    }
}

// State classes
[GenerateSerializer]
public class WorkflowMathGAgentState : MemberState
{
    [Id(0)] public List<string> CalculationHistory { get; set; } = new();
}

[GenerateSerializer]
public class WorkflowTimeConverterGAgentState : MemberState
{
    [Id(0)] public List<string> ConversionHistory { get; set; } = new();
}

[GenerateSerializer]
public class DataProcessorGAgentState : MemberState
{
    [Id(0)] public string ProcessingMode { get; set; } = "transform";
    [Id(1)] public int ProcessedCount { get; set; }
}

// Log event classes
[GenerateSerializer]
public class MathGAgentLogEvent : StateLogEventBase<MathGAgentLogEvent>
{
}

[GenerateSerializer]
public class TimeConverterGAgentLogEvent : StateLogEventBase<TimeConverterGAgentLogEvent>
{
}

[GenerateSerializer]
public class DataProcessorGAgentLogEvent : StateLogEventBase<DataProcessorGAgentLogEvent>
{
} 