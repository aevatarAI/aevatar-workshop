using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Agent;
using Aevatar.GAgents.AIGAgent.State;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.ComponentModel;
using Aevatar.Core;
using Aevatar.Workshop;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using System.Threading;
using System.Text.RegularExpressions;

namespace Aevatar.Workshop.GAgent;

public abstract class ToolAIGAgentBase<TState, TLogEvent> : AIGAgentBase<TState, TLogEvent>
    where TState : AIGAgentStateBase, new()
    where TLogEvent : StateLogEventBase<TLogEvent>
{
    protected readonly IGAgentFactory _gAgentFactory;
    protected readonly Aevatar.Workshop.IGAgentExecutor _gAgentExecutor;
    protected readonly IClusterClient _clusterClient;
    private Dictionary<string, GAgentInfo> _availableGAgents = new();
    private Kernel? _toolKernel;

    protected ToolAIGAgentBase()
    {
        _clusterClient = ServiceProvider.GetRequiredService<IClusterClient>();
        _gAgentFactory = new GAgentFactory(_clusterClient);
        _gAgentExecutor = ServiceProvider.GetRequiredService<Aevatar.Workshop.IGAgentExecutor>();
    }

    /// <summary>
    /// 动态发现系统中的所有GAgent
    /// </summary>
    protected virtual async Task<Dictionary<string, GAgentInfo>> DiscoverGAgentsAsync()
    {
        if (_availableGAgents.Any()) return _availableGAgents;

        var gAgentType = typeof(IGAgent);
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        var gAgentTypes = new List<Type>();

        // 扫描所有程序集中的GAgent
        foreach (var assembly in assemblies)
        {
            try
            {
                var types = assembly.GetTypes()
                    .Where(t => gAgentType.IsAssignableFrom(t) && 
                               t.IsClass && 
                               !t.IsAbstract &&
                               t.GetCustomAttribute<GAgentAttribute>() != null);
                gAgentTypes.AddRange(types);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, $"Failed to scan assembly {assembly.FullName} for GAgents");
            }
        }

        // 收集每个GAgent的信息
        foreach (var type in gAgentTypes)
        {
            try
            {
                var gagentAttr = type.GetCustomAttribute<GAgentAttribute>();
                if (gagentAttr == null) continue;

                // 获取GAgent特性的构造函数参数
                var attrData = type.GetCustomAttributesData()
                    .FirstOrDefault(a => a.AttributeType == typeof(GAgentAttribute));
                
                string alias = type.Name;
                string namespaceName = type.Namespace ?? "default";
                
                if (attrData != null && attrData.ConstructorArguments.Count > 0)
                {
                    // 第一个参数是alias
                    if (attrData.ConstructorArguments.Count >= 1 && attrData.ConstructorArguments[0].Value != null)
                    {
                        alias = attrData.ConstructorArguments[0].Value.ToString() ?? type.Name;
                    }
                    // 第二个参数是namespace
                    if (attrData.ConstructorArguments.Count >= 2 && attrData.ConstructorArguments[1].Value != null)
                    {
                        namespaceName = attrData.ConstructorArguments[1].Value.ToString() ?? namespaceName;
                    }
                }
                
                var key = $"{alias}.{namespaceName}";

                // 尝试获取描述
                string description = type.Name;
                try
                {
                    // 检查是否有Description特性
                    var descAttr = type.GetCustomAttribute<DescriptionAttribute>();
                    if (descAttr != null)
                    {
                        description = descAttr.Description;
                    }
                    else
                    {
                        // 尝试创建实例并调用GetDescriptionAsync
                        var gagent = await _gAgentFactory.GetGAgentAsync(alias, namespaceName);
                        if (gagent != null)
                        {
                            var getDescMethod = gagent.GetType().GetMethod("GetDescriptionAsync");
                            if (getDescMethod != null)
                            {
                                var descTask = getDescMethod.Invoke(gagent, null) as Task<string>;
                                if (descTask != null)
                                {
                                    description = await descTask;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogDebug(ex, $"Failed to get description for {key}");
                }

                _availableGAgents[key] = new GAgentInfo
                {
                    Type = type,
                    Alias = alias,
                    Namespace = namespaceName,
                    Description = description,
                    Key = key
                };

                Logger.LogInformation($"Discovered GAgent: {key} - {description}");
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, $"Failed to process GAgent type {type.FullName}");
            }
        }

        return _availableGAgents;
    }

    /// <summary>
    /// 创建并配置用于工具调用的Kernel
    /// </summary>
    protected virtual async Task<Kernel> GetOrCreateToolKernelAsync()
    {
        if (_toolKernel != null) return _toolKernel;

        // 创建一个新的Kernel用于工具调用
        var builder = Kernel.CreateBuilder();
        
        // 将当前实例作为插件添加到kernel
        // 这样CallGAgentAsync方法就可以被LLM调用
        _toolKernel = builder.Build();
        
        // 创建一个包含所有工具方法的插件
        var plugin = KernelPluginFactory.CreateFromObject(this, "ToolAIGAgentPlugin");
        _toolKernel.Plugins.Add(plugin);
        
        Logger.LogInformation("Created tool kernel with plugin containing CallGAgentAsync function");
        
        return _toolKernel;
    }

    /// <summary>
    /// 调用系统中的任意GAgent
    /// </summary>
    [KernelFunction]
    [Description("Call any GAgent in the system to perform specific tasks.")]
    public virtual async Task<string> CallGAgentAsync(
        [Description("GAgent alias (e.g., 'math', 'timeconverter')")]
        string alias,
        [Description("GAgent namespace (e.g., 'tools')")]
        string namespaceName,
        [Description("Task description or data to send to the GAgent")]
        string task)
    {
        try
        {
            // 确保已发现GAgent
            await DiscoverGAgentsAsync();

            var key = $"{alias}.{namespaceName}";
            Logger.LogInformation($"Calling GAgent: {key} with task: {task}");

            // 检查GAgent是否存在
            if (!_availableGAgents.ContainsKey(key))
            {
                // 尝试模糊匹配
                var fuzzyMatch = _availableGAgents.FirstOrDefault(kv => 
                    kv.Value.Alias.Equals(alias, StringComparison.OrdinalIgnoreCase) ||
                    kv.Key.Contains(alias, StringComparison.OrdinalIgnoreCase));
                
                if (fuzzyMatch.Value != null)
                {
                    key = fuzzyMatch.Key;
                    Logger.LogInformation($"Using fuzzy matched GAgent: {key}");
                }
                else
                {
                    return $"GAgent {key} not found. Available GAgents: {string.Join(", ", _availableGAgents.Keys)}";
                }
            }

            var gagentInfo = _availableGAgents[key];

            // 创建事件对象
            var eventToSend = await CreateEventForGAgentAsync(gagentInfo, task);

            // 使用GAgentExecutor执行调用
            var grainType = GrainType.Create(GenerateGrainTypeName(gagentInfo));
            var result = await _gAgentExecutor.ExecuteGAgentEventHandler(grainType, eventToSend);

            Logger.LogInformation($"GAgent {key} execution completed. Result: {result}");
            return result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, $"Error calling GAgent: {alias}.{namespaceName}");
            return $"Failed to call {alias}.{namespaceName}: {ex.Message}";
        }
    }

    /// <summary>
    /// 处理复杂任务，使用工具增强的LLM
    /// </summary>
    protected virtual async Task<string> ProcessComplexTaskWithToolsAsync(string task)
    {
        Logger.LogInformation("Processing complex task with tools: {Task}", task);

        try
        {
            // 发现所有可用的GAgent
            var availableGAgents = await DiscoverGAgentsAsync();
            Logger.LogInformation("Discovered {Count} GAgents", availableGAgents.Count);

            // 构建工具描述
            var toolsDescription = new StringBuilder();
            toolsDescription.AppendLine("Available tools through CallGAgentAsync function:");
            
            foreach (var gagent in availableGAgents.Values.OrderBy(g => g.Key))
            {
                toolsDescription.AppendLine($"- {gagent.Alias} (namespace: {gagent.Namespace}): {gagent.Description}");
            }

            // 创建增强的提示
            var enhancedTask = $"""
                {task}
                
                You have access to specialized tools via the CallGAgentAsync function.
                {toolsDescription}
                
                Use the appropriate tools to complete the task. The function signature is:
                CallGAgentAsync(alias, namespaceName, task)
                
                Examples:
                - For calculations: CallGAgentAsync("math", "tools", "25 * 4 + 10")
                - For time queries: CallGAgentAsync("timeconverter", "tools", "current time in Tokyo")
                """;

            // 使用父类的ChatWithHistory方法，它应该已经集成了Semantic Kernel
            var response = await ChatWithHistory(enhancedTask);
            
            if (response != null && response.Count > 0)
            {
                var content = response[0].Content ?? "";
                
                // 检查是否包含工具调用的迹象但没有实际执行
                if ((content.Contains("CallGAgentAsync") || content.Contains("would call") || content.Contains("should use")) 
                    && !content.Contains("Result:") && !content.Contains("110") && !content.Contains("GMT"))
                {
                    Logger.LogInformation("LLM described tool usage but didn't execute. Attempting direct execution.");
                    
                    // 尝试提取并执行工具调用
                    var executionResults = await ExtractAndExecuteToolCalls(task, content, availableGAgents);
                    if (executionResults.Any())
                    {
                        return string.Join("\n", executionResults);
                    }
                }
                
                return content;
            }
            
            return "Unable to process the task.";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error in ProcessComplexTaskWithToolsAsync");
            return $"Error: {ex.Message}";
        }
    }

    /// <summary>
    /// 从响应中提取并执行工具调用
    /// </summary>
    private async Task<List<string>> ExtractAndExecuteToolCalls(string originalTask, string llmResponse, Dictionary<string, GAgentInfo> availableGAgents)
    {
        var results = new List<string>();
        
        try
        {
            var taskLower = originalTask.ToLower();
            
            // 检测数学任务
            if (Regex.IsMatch(originalTask, @"\d+\s*[\+\-\*/]\s*\d+") || taskLower.Contains("calculate") || taskLower.Contains("math"))
            {
                var mathAgent = availableGAgents.Values.FirstOrDefault(g => g.Alias.ToLower() == "math");
                if (mathAgent != null)
                {
                    var result = await CallGAgentAsync(mathAgent.Alias, mathAgent.Namespace, originalTask);
                    results.Add(result);
                }
            }
            
            // 检测时间任务
            if (taskLower.Contains("time") || taskLower.Contains("timezone") || Regex.IsMatch(taskLower, @"\b(utc|est|pst|gmt|jst)\b"))
            {
                var timeAgent = availableGAgents.Values.FirstOrDefault(g => g.Alias.ToLower().Contains("time"));
                if (timeAgent != null)
                {
                    var result = await CallGAgentAsync(timeAgent.Alias, timeAgent.Namespace, originalTask);
                    results.Add(result);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error in ExtractAndExecuteToolCalls");
        }
        
        return results;
    }

    /// <summary>
    /// 生成GrainType名称
    /// </summary>
    protected virtual string GenerateGrainTypeName(GAgentInfo gagentInfo)
    {
        return $"{gagentInfo.Namespace}.{gagentInfo.Alias}";
    }

    /// <summary>
    /// 根据目标GAgent动态创建适当的事件对象
    /// </summary>
    protected virtual async Task<EventBase> CreateEventForGAgentAsync(GAgentInfo gagentInfo, string task)
    {
        // 分析GAgent的EventHandler方法，找出它接受的事件类型
        var eventHandlerMethods = gagentInfo.Type.GetMethods()
            .Where(m => m.GetCustomAttribute<EventHandlerAttribute>() != null)
            .ToList();

        if (eventHandlerMethods.Any())
        {
            // 获取第一个EventHandler方法的参数类型
            var firstHandler = eventHandlerMethods.First();
            var parameters = firstHandler.GetParameters();
            if (parameters.Length > 0)
            {
                var eventType = parameters[0].ParameterType;
                
                // 尝试创建该类型的事件实例
                if (typeof(EventBase).IsAssignableFrom(eventType))
                {
                    try
                    {
                        var eventInstance = Activator.CreateInstance(eventType) as EventBase;
                        if (eventInstance != null)
                        {
                            // 尝试设置常见的属性
                            SetEventProperties(eventInstance, task);
                            return eventInstance;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogDebug(ex, $"Failed to create event instance of type {eventType.Name}");
                    }
                }
            }
        }

        // 默认使用GreetingEvent
        return new GreetingEvent { Greeting = task };
    }

    /// <summary>
    /// 设置事件对象的属性
    /// </summary>
    protected virtual void SetEventProperties(EventBase eventInstance, string data)
    {
        var type = eventInstance.GetType();
        
        // 尝试设置常见的属性名
        var propertyNames = new[] { "Message", "Content", "Data", "Text", "Greeting", "Task", "Input", "Expression", "TimeInput" };
        
        foreach (var propName in propertyNames)
        {
            var prop = type.GetProperty(propName);
            if (prop != null && prop.PropertyType == typeof(string) && prop.CanWrite)
            {
                prop.SetValue(eventInstance, data);
                return;
            }
        }

        // 如果没有找到合适的字符串属性，尝试解析为JSON
        try
        {
            if (data.StartsWith("{") || data.StartsWith("["))
            {
                JsonConvert.PopulateObject(data, eventInstance);
            }
        }
        catch (Exception ex)
        {
            Logger.LogDebug(ex, "Failed to populate event object from JSON");
        }
    }

    /// <summary>
    /// GAgent信息类
    /// </summary>
    protected class GAgentInfo
    {
        public Type Type { get; set; } = null!;
        public string Alias { get; set; } = string.Empty;
        public string Namespace { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
    }
}