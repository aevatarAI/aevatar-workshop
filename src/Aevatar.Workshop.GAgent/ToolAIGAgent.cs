using System.ComponentModel;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AI.Common;
using Aevatar.GAgents.AIGAgent.Agent;
using Aevatar.GAgents.AIGAgent.State;
using Aevatar.Workshop.GAgentPlugin;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace Aevatar.Workshop.GAgent;

/// <summary>
/// 抽象的工具 AI GAgent，集成 Semantic Kernel 和 GAgent Plugin，
/// 允许 LLM 在思考过程中调用系统中的其他 GAgent
/// </summary>
public abstract class ToolAIGAgent<TState, TLogEvent> : AIGAgentBase<TState, TLogEvent>
    where TState : AIGAgentStateBase, new()
    where TLogEvent : StateLogEventBase<TLogEvent>, new()
{
    private readonly IGAgentFactory _gAgentFactory;
    private readonly IClusterClient _clusterClient;
    private GAgentPluginStreams? _gAgentPlugin;
    private GAgentToolPlugin? _toolPlugin;
    private bool _isKernelInitialized = false;

    protected ToolAIGAgent(IGAgentFactory gAgentFactory, IClusterClient clusterClient)
    {
        _gAgentFactory = gAgentFactory;
        _clusterClient = clusterClient;
    }

    /// <summary>
    /// 初始化 Kernel 和工具插件
    /// </summary>
    protected virtual async Task InitializeKernelAsync()
    {
        if (_isKernelInitialized) return;

        try
        {
            // 初始化 GAgent Plugin
            _gAgentPlugin = new GAgentPluginStreams(_gAgentFactory, _clusterClient);
            
            // 初始化工具插件
            _toolPlugin = new GAgentToolPlugin(_gAgentPlugin, _gAgentFactory);
            
            // 注册工具插件到 Kernel - 通过 Semantic Kernel 的方式
            // 由于我们继承自 AIGAgentBase，工具插件将在 ChatWithHistory 调用时自动可用
            
            // 注册自定义工具
            await RegisterCustomToolsAsync();
            
            _isKernelInitialized = true;
            Logger.LogInformation("ToolAIGAgent kernel initialized successfully with GAgent tools");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to initialize ToolAIGAgent kernel");
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
            "Available GAgent Tools:",
            "- call_gagent: Call any GAgent in the system by its identifier",
            "- research: Call ResearcherGAgent for research tasks",
            "- write: Call WriterGAgent for writing tasks",
            "- record: Call RecorderGAgent to record messages"
        };

        // 添加自定义工具描述
        var customDescriptions = await GetCustomToolsDescriptionAsync();
        if (!string.IsNullOrEmpty(customDescriptions))
        {
            descriptions.Add(customDescriptions);
        }

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
        return chatResult?[0]?.Content ?? string.Empty;
    }

    /// <summary>
    /// 使用工具增强的聊天方法（带历史记录）
    /// </summary>
    protected async Task<string> ChatWithToolsAsync(string prompt, List<ChatMessage> history, bool includeToolsDescription = true)
    {
        await InitializeKernelAsync();

        var enhancedPrompt = prompt;
        if (includeToolsDescription)
        {
            var toolsDescription = await GetAvailableToolsDescriptionAsync();
            enhancedPrompt = $"{prompt}\n\n{toolsDescription}";
        }

        var chatResult = await ChatWithHistory(enhancedPrompt, history);
        return chatResult?[0]?.Content ?? string.Empty;
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

public class GAgentToolPlugin
{
    private readonly GAgentPluginStreams _gAgentPlugin;
    private readonly IGAgentFactory _gAgentFactory;

    public GAgentToolPlugin(GAgentPluginStreams gAgentPlugin, IGAgentFactory gAgentFactory)
    {
        _gAgentPlugin = gAgentPlugin;
        _gAgentFactory = gAgentFactory;
    }

    [KernelFunction("call_gagent")]
    [Description("Call any GAgent in the system by its grain ID")]
    public async Task<string> CallGAgent(
        [Description("The grain ID of the target GAgent")]
        string grainId,
        [Description("The event data as JSON string")]
        string eventData)
    {
        try
        {
            // TODO: Use reflection to know @event
            var @event = new GreetingEvent { Greeting = eventData };
            return await _gAgentPlugin.ExecuteGAgentEventHandler(GrainId.Parse(grainId), @event);
        }
        catch (Exception ex)
        {
            return $"Error calling GAgent {grainId}: {ex.Message}";
        }
    }
}