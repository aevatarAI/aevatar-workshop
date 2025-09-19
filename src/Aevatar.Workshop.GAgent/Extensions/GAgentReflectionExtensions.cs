using System.ComponentModel;
using System.Reflection;
using Aevatar.Core.Abstractions;

namespace Aevatar.Workshop.GAgent.Extensions;

/// <summary>
/// GAgent reflection extension methods
/// </summary>
public static class GAgentReflectionExtensions
{
    /// <summary>
    /// Extract all GAgent information from assembly
    /// </summary>
    public static List<GAgentInfo> ExtractGAgentInfos(this Assembly assembly, params string[] filterByNamespace)
    {
        var gAgentInfos = new List<GAgentInfo>();

        // Find all types inherited from GAgentBase
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
    /// Extract GAgent information from specific type
    /// </summary>
    public static GAgentInfo? ExtractGAgentInfo(this Type gAgentType)
    {
        if (!IsGAgentType(gAgentType))
        {
            return null;
        }

        // Get GAgent attribute
        var gAgentAttribute = gAgentType.GetCustomAttribute<GAgentAttribute>();

        // Get GrainType
        var grainType = GetGrainType(gAgentType, gAgentAttribute);

        // Get DisplayName (from DescriptionAttribute or DisplayNameAttribute)
        var displayName = gAgentType.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName
                          ?? gAgentType.GetCustomAttribute<DescriptionAttribute>()?.Description
                          ?? gAgentType.Name;

        // Get Description (need to call GetDescriptionAsync after instantiation, use class name or attribute here first)
        var description = gAgentType.GetCustomAttribute<DescriptionAttribute>()?.Description
                          ?? $"{gAgentType.Name} - Demo GAgent";

        // Get EventHandlers
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
    /// Extract event handlers
    /// </summary>
    private static List<string> ExtractEventHandlers(Type gAgentType)
    {
        var handlers = new List<string>();

        // Get all methods (including inherited)
        var methods = gAgentType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                                            BindingFlags.DeclaredOnly);

        foreach (var method in methods)
        {
            // Check EventHandler attribute
            if (method.GetCustomAttribute<EventHandlerAttribute>() != null)
            {
                var parameters = method.GetParameters();
                var eventTypeNames = string.Join(", ", parameters.Select(p => p.ParameterType.Name));
                handlers.Add($"{method.Name}({eventTypeNames})");
            }

            // Check AllEventHandler attribute
            if (method.GetCustomAttribute<AllEventHandlerAttribute>() != null)
            {
                var parameters = method.GetParameters();
                var eventTypeNames = string.Join(", ", parameters.Select(p => p.ParameterType.Name));
                handlers.Add($"{method.Name}({eventTypeNames}) [AllEventHandler]");
            }

            // Check conventional HandleEventAsync method
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
    /// Get GrainType
    /// </summary>
    private static GrainType GetGrainType(Type gAgentType, GAgentAttribute? gAgentAttribute)
    {
        if (gAgentAttribute != null)
        {
            // Create GrainType based on GAgentAttribute parameters
            if (!string.IsNullOrEmpty(gAgentAttribute.Alias) && !string.IsNullOrEmpty(gAgentAttribute.Namespace))
            {
                return GrainType.Create($"{gAgentAttribute.Namespace}.{gAgentAttribute.Alias}");
            }

            if (!string.IsNullOrEmpty(gAgentAttribute.Alias))
            {
                return GrainType.Create(gAgentAttribute.Alias);
            }
        }

        // Default to lowercase form of class name
        return GrainType.Create(gAgentType.Name.ToLowerInvariant());
    }

    /// <summary>
    /// Check if it's a GAgent type
    /// </summary>
    private static bool IsGAgentType(Type type)
    {
        // Check if inherited from GAgentBase
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
/// GAgent information
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