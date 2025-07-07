using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Text.RegularExpressions;

namespace Aevatar.Workshop.GAgent;

[GenerateSerializer]
public class MathGAgentState : StateBase
{
    [Id(0)] public List<string> CalculationHistory { get; set; } = [];
    [Id(1)] public double LastResult { get; set; }
}

[GenerateSerializer]
public class MathStateLogEvent : StateLogEventBase<MathStateLogEvent>;

[GenerateSerializer]
public class MathCalculationLogEvent : MathStateLogEvent
{
    [Id(0)] public string Expression { get; set; } = string.Empty;
    [Id(1)] public double Result { get; set; }
}

[GenerateSerializer]
public class MathCalculateEvent : EventBase
{
    [Id(0)] 
    [System.ComponentModel.Description("Mathematical expression to evaluate. Examples: '2+2', '10*5', 'sqrt(16)', 'sin(3.14)', '2^3', 'log(10)'")]
    public string Expression { get; set; } = string.Empty;
}

public interface IMathGAgent : IStateGAgent<MathGAgentState>
{
    Task<double> CalculateAsync(string expression);
}

[GAgent("math", "tools")]
public class MathGAgent : GAgentBase<MathGAgentState, MathStateLogEvent>, IMathGAgent
{
    public override Task<string> GetDescriptionAsync()
    {
        return Task.FromResult(
            "Mathematical calculation agent that can evaluate mathematical expressions and perform complex calculations");
    }

    public async Task<double> CalculateAsync(string expression)
    {
        try
        {
            Logger.LogInformation("Calculating expression: {Expression}", expression);

            // Clean the expression
            var cleanExpression = CleanExpression(expression);

            // Evaluate the expression
            var result = EvaluateExpression(cleanExpression);

            // Log the calculation
            RaiseEvent(new MathCalculationLogEvent
            {
                Expression = expression,
                Result = result
            });
            await ConfirmEvents();

            Logger.LogInformation("Calculation result: {Expression} = {Result}", expression, result);

            // Publish the result
            await PublishAsync(new RecordEvent
            {
                Message = $"Math calculation: {expression} = {result}"
            });

            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error calculating expression: {Expression}", expression);
            throw new InvalidOperationException($"Failed to calculate expression: {expression}", ex);
        }
    }

    [EventHandler]
    public async Task HandleCalculateEventAsync(MathCalculateEvent eventData)
    {
        Logger.LogInformation("Received math calculation request: {Expression}", eventData.Expression);
        await CalculateAsync(eventData.Expression);
    }

    [EventHandler]
    public async Task HandleGreetingEventAsync(GreetingEvent eventData)
    {
        // Handle expressions sent as greeting events (for compatibility)
        if (!string.IsNullOrWhiteSpace(eventData.Greeting))
        {
            Logger.LogInformation("Received math expression via greeting: {Expression}", eventData.Greeting);
            await CalculateAsync(eventData.Greeting);
        }
    }

    private string CleanExpression(string expression)
    {
        // Remove any non-mathematical characters and standardize the expression
        expression = expression.Trim();

        // Replace common mathematical terms with operators
        expression = expression.Replace("plus", "+", StringComparison.OrdinalIgnoreCase);
        expression = expression.Replace("minus", "-", StringComparison.OrdinalIgnoreCase);
        expression = expression.Replace("times", "*", StringComparison.OrdinalIgnoreCase);
        expression = expression.Replace("multiplied by", "*", StringComparison.OrdinalIgnoreCase);
        expression = expression.Replace("divided by", "/", StringComparison.OrdinalIgnoreCase);
        expression = expression.Replace("power", "^", StringComparison.OrdinalIgnoreCase);
        expression = expression.Replace("squared", "^2", StringComparison.OrdinalIgnoreCase);
        expression = expression.Replace("cubed", "^3", StringComparison.OrdinalIgnoreCase);

        // Handle mathematical functions
        expression = Regex.Replace(expression, @"square root of (\d+)", "sqrt($1)", RegexOptions.IgnoreCase);
        expression = Regex.Replace(expression, @"sqrt\(([^)]+)\)", "Math.Sqrt($1)", RegexOptions.IgnoreCase);
        expression = Regex.Replace(expression, @"sin\(([^)]+)\)", "Math.Sin($1)", RegexOptions.IgnoreCase);
        expression = Regex.Replace(expression, @"cos\(([^)]+)\)", "Math.Cos($1)", RegexOptions.IgnoreCase);
        expression = Regex.Replace(expression, @"tan\(([^)]+)\)", "Math.Tan($1)", RegexOptions.IgnoreCase);
        expression = Regex.Replace(expression, @"log\(([^)]+)\)", "Math.Log($1)", RegexOptions.IgnoreCase);
        expression = Regex.Replace(expression, @"ln\(([^)]+)\)", "Math.Log($1)", RegexOptions.IgnoreCase);
        expression = Regex.Replace(expression, @"exp\(([^)]+)\)", "Math.Exp($1)", RegexOptions.IgnoreCase);
        expression = Regex.Replace(expression, @"abs\(([^)]+)\)", "Math.Abs($1)", RegexOptions.IgnoreCase);

        // Replace ^ with Math.Pow
        expression = Regex.Replace(expression, @"(\d+\.?\d*)\s*\^\s*(\d+\.?\d*)", "Math.Pow($1,$2)");

        return expression;
    }

    private double EvaluateExpression(string expression)
    {
        // Use DataTable.Compute for simple expressions
        try
        {
            var table = new DataTable();
            var result = table.Compute(expression, null);
            return Convert.ToDouble(result);
        }
        catch
        {
            // If DataTable fails, try manual evaluation for Math functions
            return EvaluateComplexExpression(expression);
        }
    }

    private double EvaluateComplexExpression(string expression)
    {
        // This is a simplified evaluator for expressions with Math functions
        // In a real implementation, you might want to use a proper expression evaluator library

        try
        {
            // Replace Math functions with placeholders and evaluate
            var result = expression;

            // Handle Math.Pow
            while (result.Contains("Math.Pow"))
            {
                var match = Regex.Match(result, @"Math\.Pow\(([^,]+),([^)]+)\)");
                if (match.Success)
                {
                    var baseVal = EvaluateSimpleExpression(match.Groups[1].Value);
                    var expVal = EvaluateSimpleExpression(match.Groups[2].Value);
                    var powResult = Math.Pow(baseVal, expVal);
                    result = result.Replace(match.Value, powResult.ToString());
                }
                else break;
            }

            // Handle other Math functions
            result = EvaluateMathFunctions(result);

            // Evaluate the final expression
            return EvaluateSimpleExpression(result);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to evaluate complex expression: {expression}", ex);
        }
    }

    private string EvaluateMathFunctions(string expression)
    {
        var functions = new Dictionary<string, Func<double, double>>
        {
            ["Math.Sqrt"] = Math.Sqrt,
            ["Math.Sin"] = Math.Sin,
            ["Math.Cos"] = Math.Cos,
            ["Math.Tan"] = Math.Tan,
            ["Math.Log"] = Math.Log,
            ["Math.Exp"] = Math.Exp,
            ["Math.Abs"] = Math.Abs
        };

        foreach (var func in functions)
        {
            while (expression.Contains(func.Key))
            {
                var pattern = $@"{Regex.Escape(func.Key)}\(([^)]+)\)";
                var match = Regex.Match(expression, pattern);
                if (match.Success)
                {
                    var arg = EvaluateSimpleExpression(match.Groups[1].Value);
                    var result = func.Value(arg);
                    expression = expression.Replace(match.Value, result.ToString());
                }
                else break;
            }
        }

        return expression;
    }

    private double EvaluateSimpleExpression(string expression)
    {
        var table = new DataTable();
        var result = table.Compute(expression.Trim(), null);
        return Convert.ToDouble(result);
    }

    protected override void GAgentTransitionState(MathGAgentState state, StateLogEventBase<MathStateLogEvent> @event)
    {
        switch (@event)
        {
            case MathCalculationLogEvent calc:
                state.CalculationHistory.Add($"{calc.Expression} = {calc.Result}");
                state.LastResult = calc.Result;
                break;
        }
    }
}