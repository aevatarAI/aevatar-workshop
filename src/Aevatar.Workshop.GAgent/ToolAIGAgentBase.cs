using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Agent;
using Aevatar.GAgents.AIGAgent.State;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System.ComponentModel;
using Aevatar.Core;
using Aevatar.Workshop;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;

namespace Aevatar.Workshop.GAgent;

public abstract class ToolAIGAgentBase<TState, TLogEvent> : AIGAgentBase<TState, TLogEvent>
    where TState : AIGAgentStateBase, new()
    where TLogEvent : StateLogEventBase<TLogEvent>
{
    protected readonly IGAgentFactory _gAgentFactory;
    protected readonly Aevatar.Workshop.IGAgentExecutor _gAgentExecutor;
    protected readonly IClusterClient _clusterClient;
    private bool _toolsRegistered = false;
    private Dictionary<string, GAgentInfo> _availableGAgents = new();

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
    /// 注册工具到 Semantic Kernel
    /// </summary>
    protected virtual async Task RegisterToolsAsync()
    {
        if (_toolsRegistered) return;

        try
        {
            // 发现所有可用的GAgent
            await DiscoverGAgentsAsync();

            // 使用KernelFunction注解的方式，工具会自动被Semantic Kernel发现
            _toolsRegistered = true;
            Logger.LogInformation($"Tools registered successfully. Found {_availableGAgents.Count} GAgents.");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to register tools to Semantic Kernel");
            throw;
        }
    }

    /// <summary>
    /// 构建动态的工具描述
    /// </summary>
    protected virtual string BuildToolDescription()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Call any GAgent in the system to perform specific tasks. Available GAgents:");
        sb.AppendLine();

        foreach (var gagent in _availableGAgents.Values.OrderBy(g => g.Key))
        {
            // 根据别名选择合适的图标
            var icon = gagent.Alias.ToLower() switch
            {
                "researcher" => "🔬",
                "writer" => "✍️",
                "recorder" => "📝",
                "alice" => "💬",
                "bob" => "🎮",
                _ => "🤖"
            };

            sb.AppendLine($"{icon} {gagent.Key} - {gagent.Description}");
        }

        sb.AppendLine();
        sb.AppendLine("Usage: Specify the alias, namespace, and task description. The system will automatically determine the appropriate event type.");

        return sb.ToString();
    }

    /// <summary>
    /// 调用系统中的任意GAgent（动态版本）
    /// </summary>
    [KernelFunction]
    [Description("Call any GAgent in the system to perform specific tasks.")]
    public virtual async Task<string> CallGAgentAsync(
        [Description("GAgent alias (e.g., 'researcher', 'writer', etc.)")]
        string alias,
        [Description("GAgent namespace (e.g., 'demo')")]
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
                return $"GAgent {key} not found. Available GAgents: {string.Join(", ", _availableGAgents.Keys)}";
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
    /// 生成GrainType名称
    /// </summary>
    protected virtual string GenerateGrainTypeName(GAgentInfo gagentInfo)
    {
        // 这应该匹配GAgentAttribute.GetGrainType的逻辑
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
        var propertyNames = new[] { "Message", "Content", "Data", "Text", "Greeting", "Task", "Input" };
        
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
    /// 处理复杂任务，使用工具增强的LLM
    /// </summary>
    protected virtual async Task<string> ProcessComplexTaskWithToolsAsync(string task)
    {
        Logger.LogInformation("Processing complex task with tools: {Task}", task);

        // 注册工具到 Semantic Kernel
        await RegisterToolsAsync();

        // 动态构建工具描述
        var toolDescription = BuildToolDescription();

        // 使用工具增强的 LLM 处理任务
        var prompt = $"""
            You are an intelligent AI agent that can coordinate with other agents to complete complex tasks.

            Current task: {task}

            You have access to the call_gagent tool which can call any of the following GAgents:
            {toolDescription}

            Analyze this task and determine what steps are needed. Use the call_gagent tool to delegate work to appropriate agents.
            Coordinate the results and provide a comprehensive response.
            
            IMPORTANT: When calling GAgents, use the exact alias and namespace as shown above.
            """;

        // 使用 LLM 处理任务
        var result = await ChatWithHistory(prompt);
        var responseContent = result?[0]?.Content ?? "No response generated";

        Logger.LogInformation("Complex task completed: {Task}", task);
        return responseContent;
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