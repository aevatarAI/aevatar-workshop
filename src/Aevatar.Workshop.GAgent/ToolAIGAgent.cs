 using System.ComponentModel;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AIGAgent.Agent;
using Aevatar.GAgents.AIGAgent.State;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Aevatar.Workshop.GAgent;

/// <summary>
/// 抽象的工具 AI GAgent，集成 GAgent Plugin，
/// 允许 LLM 通过结构化输出调用系统中的其他 GAgent
/// </summary>
public abstract class ToolAIGAgent<TState, TLogEvent> : AIGAgentBase<TState, TLogEvent>
    where TState : AIGAgentStateBase, new()
    where TLogEvent : StateLogEventBase<TLogEvent>, new()
{
    private readonly IGAgentFactory _gAgentFactory;
    private readonly IClusterClient _clusterClient;
    private Aevatar.Workshop.GAgentPlugin? _gAgentPlugin;
    private bool _isKernelInitialized = false;

    protected ToolAIGAgent(IGAgentFactory gAgentFactory, IClusterClient clusterClient)
    {
        _gAgentFactory = gAgentFactory;
        _clusterClient = clusterClient;
    }

    /// <summary>
    /// 初始化 GAgent Plugin
    /// </summary>
    protected virtual async Task InitializeKernelAsync()
    {
        if (_isKernelInitialized) return;

        try
        {
            // 初始化 GAgent Plugin
            _gAgentPlugin = new Aevatar.Workshop.GAgentPlugin(_gAgentFactory, _clusterClient);
            
            // 注册自定义工具
            await RegisterCustomToolsAsync();

            _isKernelInitialized = true;
            Logger.LogInformation("ToolAIGAgent initialized successfully with GAgent tools");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to initialize ToolAIGAgent");
            throw;
        }
    }

    /// <summary>
    /// 注册自定义工具，子类可重写以添加特定工具
    /// </summary>
    protected virtual Task RegisterCustomToolsAsync()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// 获取可用工具的描述信息
    /// </summary>
    protected virtual async Task<string> GetAvailableToolsDescriptionAsync()
    {
        var descriptions = new List<string>
        {
            "Available Tools (respond with JSON to call tools):",
            "- call_gagent: {\"tool\": \"call_gagent\", \"alias\": \"gagent_alias\", \"namespace\": \"demo\", \"event_type\": \"GreetingEvent\", \"event_data\": {\"Greeting\": \"message\"}}",
            "- research: {\"tool\": \"research\", \"query\": \"research topic\"}",
            "- write: {\"tool\": \"write\", \"content\": \"content to write about\"}",
            "- record: {\"tool\": \"record\", \"message\": \"message to record\"}"
        };

        // 添加自定义工具描述
        var customDescriptions = await GetCustomToolsDescriptionAsync();
        if (!string.IsNullOrEmpty(customDescriptions))
        {
            descriptions.Add(customDescriptions);
        }

        descriptions.Add("");
        descriptions.Add("IMPORTANT: If you want to use any tools, respond with valid JSON. If you just want to provide a direct answer, respond normally without JSON.");

        return string.Join("\n", descriptions);
    }

    /// <summary>
    /// 获取自定义工具描述，子类可重写
    /// </summary>
    protected virtual Task<string> GetCustomToolsDescriptionAsync()
    {
        return Task.FromResult(string.Empty);
    }

    /// <summary>
    /// 使用工具增强的聊天方法
    /// </summary>
    protected async Task<string> ChatWithToolsAsync(string prompt, bool includeToolsDescription = true)
    {
        await InitializeKernelAsync();

        var enhancedPrompt = prompt;
        if (includeToolsDescription)
        {
            var toolsDescription = await GetAvailableToolsDescriptionAsync();
            enhancedPrompt = $"{prompt}\n\n{toolsDescription}";
        }

        var chatResult = await ChatWithHistory(enhancedPrompt);
        var response = chatResult?[0]?.Content ?? string.Empty;

        // 尝试解析和执行工具调用
        var toolResult = await TryExecuteToolCallAsync(response);
        if (!string.IsNullOrEmpty(toolResult))
        {
            // 如果执行了工具调用，将结果反馈给LLM
            var followUpPrompt = $"Tool execution result: {toolResult}\n\nPlease provide a comprehensive response based on this result.";
            var finalResult = await ChatWithHistory(followUpPrompt);
            return finalResult?[0]?.Content ?? toolResult;
        }

        return response;
    }

    /// <summary>
    /// 尝试解析和执行工具调用
    /// </summary>
    private async Task<string> TryExecuteToolCallAsync(string response)
    {
        try
        {
            // 尝试解析JSON工具调用
            if (response.Trim().StartsWith("{") && response.Trim().EndsWith("}"))
            {
                var toolCall = JsonSerializer.Deserialize<Dictionary<string, object>>(response);
                if (toolCall != null && toolCall.ContainsKey("tool"))
                {
                    var toolName = toolCall["tool"].ToString();
                    return await ExecuteToolAsync(toolName, toolCall);
                }
            }
        }
        catch (JsonException)
        {
            // 不是JSON，正常处理
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error executing tool call");
            return $"Error executing tool: {ex.Message}";
        }

        return string.Empty;
    }

    /// <summary>
    /// 执行具体的工具调用
    /// </summary>
    private async Task<string> ExecuteToolAsync(string toolName, Dictionary<string, object> parameters)
    {
        switch (toolName.ToLower())
        {
            case "call_gagent":
                return await ExecuteCallGAgentTool(parameters);
            case "research":
                return await ExecuteResearchTool(parameters);
            case "write":
                return await ExecuteWriteTool(parameters);
            case "record":
                return await ExecuteRecordTool(parameters);
            default:
                return $"Unknown tool: {toolName}";
        }
    }

    /// <summary>
    /// 执行call_gagent工具
    /// </summary>
    private async Task<string> ExecuteCallGAgentTool(Dictionary<string, object> parameters)
    {
        try
        {
            var alias = parameters["alias"].ToString();
            var ns = parameters["namespace"].ToString();
            var eventType = parameters["event_type"].ToString();
            var eventData = parameters["event_data"];

            // 创建事件
            EventBase @event = eventType switch
            {
                "GreetingEvent" => new GreetingEvent { Greeting = GetStringFromObject(eventData, "Greeting") },
                "RecordEvent" => new RecordEvent { Message = GetStringFromObject(eventData, "Message") },
                _ => new GreetingEvent { Greeting = eventData?.ToString() ?? "" }
            };

            return await CallGAgentAsync(alias, ns, @event);
        }
        catch (Exception ex)
        {
            return $"Error calling GAgent: {ex.Message}";
        }
    }

    /// <summary>
    /// 执行research工具
    /// </summary>
    private async Task<string> ExecuteResearchTool(Dictionary<string, object> parameters)
    {
        var query = parameters["query"].ToString();
        var researchEvent = new GreetingEvent { Greeting = $"Research: {query}" };
        return await CallGAgentAsync("researcher", "demo", researchEvent);
    }

    /// <summary>
    /// 执行write工具
    /// </summary>
    private async Task<string> ExecuteWriteTool(Dictionary<string, object> parameters)
    {
        var content = parameters["content"].ToString();
        var writeEvent = new GreetingEvent { Greeting = $"Write: {content}" };
        return await CallGAgentAsync("writer", "demo", writeEvent);
    }

    /// <summary>
    /// 执行record工具
    /// </summary>
    private async Task<string> ExecuteRecordTool(Dictionary<string, object> parameters)
    {
        var message = parameters["message"].ToString();
        var recordEvent = new RecordEvent { Message = message };
        return await CallGAgentAsync("recorder", "demo", recordEvent);
    }

    /// <summary>
    /// 从对象中获取字符串值
    /// </summary>
    private string GetStringFromObject(object obj, string key)
    {
        if (obj is JsonElement element && element.ValueKind == JsonValueKind.Object)
        {
            if (element.TryGetProperty(key, out var property))
            {
                return property.GetString() ?? "";
            }
        }
        return obj?.ToString() ?? "";
    }

    /// <summary>
    /// 直接调用指定的 GAgent
    /// </summary>
    protected async Task<string> CallGAgentAsync(string grainId, EventBase @event)
    {
        await InitializeKernelAsync();

        if (_gAgentPlugin == null)
            throw new InvalidOperationException("GAgent plugin not initialized");

        return await _gAgentPlugin.ExecuteGAgentEventHandler(GrainId.Parse(grainId), @event);
    }

    /// <summary>
    /// 直接调用指定的 GAgent（通过别名和命名空间）
    /// </summary>
    protected async Task<string> CallGAgentAsync(string alias, string @namespace, EventBase @event)
    {
        await InitializeKernelAsync();

        if (_gAgentPlugin == null)
            throw new InvalidOperationException("GAgent plugin not initialized");

        var targetGAgent = await _gAgentFactory.GetGAgentAsync(alias, @namespace);
        var grainId = targetGAgent.GetGrainId();
        return await _gAgentPlugin.ExecuteGAgentEventHandler(grainId, @event);
    }
}