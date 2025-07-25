using System.ComponentModel;
using System.Reflection;
using Aevatar.Core.Abstractions;

namespace Aevatar.Workshop.GAgent.Extensions;

/// <summary>
/// GAgent反射扩展方法
/// </summary>
public static class GAgentReflectionExtensions
{
    /// <summary>
    /// 从程序集中提取所有GAgent信息
    /// </summary>
    public static List<GAgentInfo> ExtractGAgentInfos(this Assembly assembly, params string[] filterByNamespace)
    {
        var gAgentInfos = new List<GAgentInfo>();

        // 查找所有继承自GAgentBase的类型
        var gAgentTypes = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && IsGAgentType(t))
            .Where(t => filterByNamespace.Length == 0 ||
                        filterByNamespace.Any(ns => t.Namespace?.Contains(ns) ?? false))
            .ToList();

        foreach (var type in gAgentTypes)
        {
            var info = ExtractGAgentInfo(type);
            if (info != null)
            {
                gAgentInfos.Add(info);
            }
        }

        return gAgentInfos;
    }

    /// <summary>
    /// 从特定类型提取GAgent信息
    /// </summary>
    public static GAgentInfo? ExtractGAgentInfo(this Type gAgentType)
    {
        if (!IsGAgentType(gAgentType))
        {
            return null;
        }

        // 获取GAgent属性
        var gAgentAttribute = gAgentType.GetCustomAttribute<GAgentAttribute>();

        // 获取GrainType
        var grainType = GetGrainType(gAgentType, gAgentAttribute);

        // 获取DisplayName (从DescriptionAttribute或DisplayNameAttribute)
        var displayName = gAgentType.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName
                          ?? gAgentType.GetCustomAttribute<DescriptionAttribute>()?.Description
                          ?? gAgentType.Name;

        // 获取Description (需要实例化后调用GetDescriptionAsync，这里先用类名或attribute)
        var description = gAgentType.GetCustomAttribute<DescriptionAttribute>()?.Description
                          ?? $"{gAgentType.Name} - 演示GAgent";

        // 获取EventHandlers
        var eventHandlers = ExtractEventHandlers(gAgentType);

        return new GAgentInfo
        {
            Name = gAgentType.Name,
            DisplayName = displayName,
            Description = description,
            GrainType = grainType,
            EventHandlers = eventHandlers,
            Type = gAgentType
        };
    }

    /// <summary>
    /// 提取事件处理器
    /// </summary>
    private static List<string> ExtractEventHandlers(Type gAgentType)
    {
        var handlers = new List<string>();

        // 获取所有方法（包括继承的）
        var methods = gAgentType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                                            BindingFlags.DeclaredOnly);

        foreach (var method in methods)
        {
            // 检查EventHandler属性
            if (method.GetCustomAttribute<EventHandlerAttribute>() != null)
            {
                var parameters = method.GetParameters();
                var eventTypeNames = string.Join(", ", parameters.Select(p => p.ParameterType.Name));
                handlers.Add($"{method.Name}({eventTypeNames})");
            }

            // 检查AllEventHandler属性
            if (method.GetCustomAttribute<AllEventHandlerAttribute>() != null)
            {
                var parameters = method.GetParameters();
                var eventTypeNames = string.Join(", ", parameters.Select(p => p.ParameterType.Name));
                handlers.Add($"{method.Name}({eventTypeNames}) [AllEventHandler]");
            }

            // 检查约定的HandleEventAsync方法
            if (method.Name == "HandleEventAsync" && !method.IsSpecialName)
            {
                var parameters = method.GetParameters();
                if (parameters.Length > 0 && typeof(EventBase).IsAssignableFrom(parameters[0].ParameterType))
                {
                    var eventTypeNames = string.Join(", ", parameters.Select(p => p.ParameterType.Name));
                    handlers.Add($"{method.Name}({eventTypeNames})");
                }
            }
        }

        return handlers;
    }

    /// <summary>
    /// 获取GrainType
    /// </summary>
    private static GrainType GetGrainType(Type gAgentType, GAgentAttribute? gAgentAttribute)
    {
        if (gAgentAttribute != null)
        {
            // 根据GAgentAttribute参数创建GrainType
            if (!string.IsNullOrEmpty(gAgentAttribute.Alias) && !string.IsNullOrEmpty(gAgentAttribute.Namespace))
            {
                return GrainType.Create($"{gAgentAttribute.Namespace}.{gAgentAttribute.Alias}");
            }

            if (!string.IsNullOrEmpty(gAgentAttribute.Alias))
            {
                return GrainType.Create(gAgentAttribute.Alias);
            }
        }

        // 默认使用类名的小写形式
        return GrainType.Create(gAgentType.Name.ToLowerInvariant());
    }

    /// <summary>
    /// 检查是否是GAgent类型
    /// </summary>
    private static bool IsGAgentType(Type type)
    {
        // 检查是否继承自GAgentBase
        var baseType = type.BaseType;
        while (baseType != null)
        {
            if (baseType.IsGenericType)
            {
                var genericTypeDef = baseType.GetGenericTypeDefinition();
                if (genericTypeDef.Name.StartsWith("GAgentBase"))
                {
                    return true;
                }
            }

            baseType = baseType.BaseType;
        }

        return false;
    }
}

/// <summary>
/// GAgent信息
/// </summary>
public class GAgentInfo
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public GrainType GrainType { get; set; }
    public List<string> EventHandlers { get; set; } = new();
    public Type Type { get; set; } = null!;
}