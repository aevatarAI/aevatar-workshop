using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.Workshop.GAgent;
using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit.Abstractions;

namespace Aevatar.Workshop.Tests;

public sealed class ToolAIGAgentTests : AevatarWorkshopTestBase
{
    private readonly IGAgentFactory _gAgentFactory;
    private readonly IGAgentExecutor _gAgentExecutor;
    private readonly ITestOutputHelper _output;

    public ToolAIGAgentTests(ITestOutputHelper output)
    {
        _output = output;
        _gAgentFactory = GetRequiredService<IGAgentFactory>();
        _gAgentExecutor = GetRequiredService<IGAgentExecutor>();
    }

    [Fact]
    public async Task TestToolAIGAgent_ShouldRecognizeMathCalculation()
    {
        // Arrange
        var testToolAI = await _gAgentFactory.GetGAgentAsync<ITestToolAIGAgent>();
        await testToolAI.InitializeAsync(new InitializeDto
        {
            Instructions = "You are a Test ToolAI GAgent that can intelligently process complex instructions using math and time conversion tools",
            LLMConfig = new LLMConfigDto { SystemLLM = "DeepSeek" }
        });
        
        // Initialize the AI agent
        await InitializeAIGAgent(testToolAI);

        // Act
        var result = await testToolAI.ProcessComplexInstructionAsync("Calculate 25 * 4 + 10");

        // Assert
        result.ShouldNotBeNullOrEmpty();
        _output.WriteLine($"Math calculation result: {result}");
        
        // The result should contain "110" (25 * 4 + 10 = 110)
        result.ShouldContain("110");
    }

    [Fact]
    public async Task TestToolAIGAgent_ShouldRecognizeTimeConversion()
    {
        // Arrange
        var testToolAI = await _gAgentFactory.GetGAgentAsync<ITestToolAIGAgent>();
        
        // Initialize the AI agent
        await InitializeAIGAgent(testToolAI);

        // Act
        var result = await testToolAI.ProcessComplexInstructionAsync("What time is it in Tokyo right now?");

        // Assert
        result.ShouldNotBeNullOrEmpty();
        _output.WriteLine($"Time conversion result: {result}");
        
        // The result should contain time-related information
        result.ShouldContain(":");  // Should contain time with colon
    }

    [Fact]
    public async Task TestToolAIGAgent_ShouldHandleComplexInstruction()
    {
        // Arrange
        var testToolAI = await _gAgentFactory.GetGAgentAsync<ITestToolAIGAgent>();
        
        // Initialize the AI agent
        await InitializeAIGAgent(testToolAI);

        // Act - Complex instruction requiring both math and time
        var result = await testToolAI.ProcessComplexInstructionAsync(
            "If it's 3:00 PM in New York, what time is it in London? Also calculate how many minutes are in 2.5 hours.");

        // Assert
        result.ShouldNotBeNullOrEmpty();
        _output.WriteLine($"Complex instruction result: {result}");
        
        // The result should contain both time and calculation information
        result.Length.ShouldBeGreaterThan(50); // Expecting a substantial response
    }

    [Fact]
    public async Task MathGAgent_ShouldEvaluateExpressions()
    {
        // Arrange
        var mathGAgent = await _gAgentFactory.GetGAgentAsync<IMathGAgent>();

        // Act - Test various mathematical expressions
        var simpleResult = await mathGAgent.CalculateAsync("10 + 5");
        var complexResult = await mathGAgent.CalculateAsync("2^3 + sqrt(16)");
        var trigResult = await mathGAgent.CalculateAsync("sin(0) + cos(0)");

        // Assert
        simpleResult.ShouldBe(15);
        complexResult.ShouldBe(12); // 8 + 4
        trigResult.ShouldBe(1); // sin(0) = 0, cos(0) = 1
        
        _output.WriteLine($"Simple: 10 + 5 = {simpleResult}");
        _output.WriteLine($"Complex: 2^3 + sqrt(16) = {complexResult}");
        _output.WriteLine($"Trig: sin(0) + cos(0) = {trigResult}");
    }

    [Fact]
    public async Task TimeConverterGAgent_ShouldConvertTimeZones()
    {
        // Arrange
        var timeConverter = await _gAgentFactory.GetGAgentAsync<ITimeConverterGAgent>();

        // Act
        var utcTime = await timeConverter.GetTimeInZoneAsync("UTC");
        var tokyoTime = await timeConverter.GetTimeInZoneAsync("JST");
        
        // Test conversion
        var conversionResult = await timeConverter.ConvertTimeAsync("15:00", "EST", "PST");

        // Assert
        utcTime.ShouldNotBeNullOrEmpty();
        tokyoTime.ShouldNotBeNullOrEmpty();
        conversionResult.ShouldNotBeNullOrEmpty();
        
        _output.WriteLine($"UTC Time: {utcTime}");
        _output.WriteLine($"Tokyo Time: {tokyoTime}");
        _output.WriteLine($"Conversion: 15:00 EST to PST = {conversionResult}");
    }

    [Fact]
    public async Task TestToolAIGAgent_WithGAgentExecutor()
    {
        // Arrange
        var testToolAI = await _gAgentFactory.GetGAgentAsync<ITestToolAIGAgent>();
        await InitializeAIGAgent(testToolAI);
        
        var greetingEvent = new GreetingEvent
        {
            Greeting = "Calculate the square root of 144 and tell me what time it is in Paris"
        };

        // Act
        var result = await _gAgentExecutor.ExecuteGAgentEventHandler(testToolAI, greetingEvent);

        // Assert
        result.ShouldNotBeNullOrEmpty();
        _output.WriteLine($"GAgentExecutor result: {result}");
    }

    [Fact]
    public async Task ToolAIGAgent_ShouldDiscoverAvailableGAgents()
    {
        // Arrange
        var testToolAI = await _gAgentFactory.GetGAgentAsync<ITestToolAIGAgent>();
        await InitializeAIGAgent(testToolAI);

        // Act - Ask about available tools
        var result = await testToolAI.ProcessComplexInstructionAsync(
            "What tools do you have available? List them and give an example of using each.");

        // Assert
        result.ShouldNotBeNullOrEmpty();
        _output.WriteLine($"Available tools description: {result}");
        
        // Should mention both math and time converter
        result.ToLower().ShouldContain("math");
        result.ToLower().ShouldContain("time");
    }

    [Fact]
    public async Task ComplexScenario_PlanningWithCalculations()
    {
        // Arrange
        var testToolAI = await _gAgentFactory.GetGAgentAsync<ITestToolAIGAgent>();
        await InitializeAIGAgent(testToolAI);

        // Act - Complex scenario requiring multiple tool calls
        var instruction = @"I have a meeting at 2:30 PM EST. 
            How long do I have if it's currently 11:45 AM EST? 
            Also, if the meeting lasts 90 minutes, what time will it end in PST?";
        
        var result = await testToolAI.ProcessComplexInstructionAsync(instruction);

        // Assert
        result.ShouldNotBeNullOrEmpty();
        _output.WriteLine($"Complex scenario result: {result}");
        
        // Should contain time calculations and timezone conversions
        result.Length.ShouldBeGreaterThan(100);
    }

    private async Task InitializeAIGAgent(object aiGAgent)
    {
        // Use reflection to call InitializeAsync on the AI agent
        var initMethod = aiGAgent.GetType().GetMethod("InitializeAsync");
        if (initMethod != null)
        {
            var initDto = new InitializeDto
            {
                Instructions = "You are a helpful AI assistant that can use specialized tools to solve problems.",
                LLMConfig = new LLMConfigDto 
                { 
                    SystemLLM = "OpenAI"
                }
            };
            
            var task = initMethod.Invoke(aiGAgent, new object[] { initDto }) as Task;
            if (task != null)
            {
                await task;
            }
        }
    }
} 