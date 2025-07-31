using Aevatar.Core.Abstractions;
using Aevatar.Core.Abstractions.Extensions;
using Aevatar.GAgents.AI.Options;
using Aevatar.GAgents.AIGAgent.Agent;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.AIGAgent.State;
using Aevatar.Workshop.GAgent.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Aevatar.Workshop.GAgent.GAgents;

public abstract class WorkshopAIGAgentBase<TState, TStateLogEvent> : AIGAgentBase<TState, TStateLogEvent>
    where TState : AIGAgentStateBase, new()
    where TStateLogEvent : StateLogEventBase<TStateLogEvent>, new()
{
    protected IGAgentFactory GAgentFactory => ServiceProvider.GetRequiredService<IGAgentFactory>();

    /// <summary>
    /// Override to get LLM configuration using ConfigManagerGAgent
    /// </summary>
    protected override async Task<LLMConfig?> GetLLMConfigAsync(LLMConfigDto llmConfigDto)
    {
        Logger.LogInformation("GetLLMConfigAsync called with SystemLLM: {SystemLLM}, HasSelfConfig: {HasSelfConfig}", 
            llmConfigDto.SystemLLM, llmConfigDto.SelfLLMConfig != null);
        
        if (llmConfigDto.SystemLLM.IsNullOrWhiteSpace() &&
            llmConfigDto.SelfLLMConfig == null)
        {
            Logger.LogWarning("Both SystemLLM and SelfLLMConfig are null/empty");
            return null;
        }

        if (!llmConfigDto.SystemLLM.IsNullOrWhiteSpace())
        {
            Logger.LogInformation("Attempting to resolve SystemLLM config for key: {Key}", llmConfigDto.SystemLLM);
            // Get config from ConfigManagerGAgent instead of IOptions
            var config = await ResolveSystemConfigAsync(llmConfigDto.SystemLLM);
            if (config == null)
            {
                Logger.LogWarning("Failed to resolve SystemLLM config for key: {Key}", llmConfigDto.SystemLLM);
            }
            return config;
        }

        Logger.LogInformation("Using SelfLLMConfig");
        return llmConfigDto.SelfLLMConfig?.ConvertToLLMConfig();
    }

    /// <summary>
    /// Override to resolve system configuration using ConfigManagerGAgent
    /// </summary>
    protected override async Task<LLMConfig?> ResolveSystemConfigAsync(string key)
    {
        try
        {
            Logger.LogInformation("Resolving SystemLLM config for key: {Key}", key);
            
            // Get ConfigManagerGAgent instance
            var configManager = await GAgentFactory.GetSystemLLMConfigGAgent();
            
            // Request configuration - we need the entire dictionary
            var requestEvent = new ConfigRequestEvent
            {
                ConfigType = typeof(SystemLLMConfigOptions).FullName!,
                // Don't specify ConfigKey - we need the entire dictionary
                ConfigKey = null
            };

            var response = await configManager.RequestConfigAsync(requestEvent);
            
            if (response.Success && !string.IsNullOrEmpty(response.ConfigJson))
            {
                // Deserialize as dictionary of LLMConfig
                var configDict = JsonConvert.DeserializeObject<Dictionary<string, LLMConfig>>(response.ConfigJson);
                
                if (configDict != null && configDict.TryGetValue(key, out var config))
                {
                    Logger.LogInformation("Successfully resolved config for key: {Key}", key);
                    return config;
                }
                else
                {
                    Logger.LogWarning("Config dictionary does not contain key: {Key}. Available keys: {Keys}", 
                        key, configDict?.Keys != null ? string.Join(", ", configDict.Keys) : "none");
                }
            }
            else
            {
                Logger.LogWarning("Failed to get config from ConfigManagerGAgent. Success: {Success}, HasJson: {HasJson}", 
                    response.Success, !string.IsNullOrEmpty(response.ConfigJson));
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error resolving SystemLLM config for key: {Key}", key);
        }

        Logger.LogWarning("Unable to resolve config for key: {Key}, returning null", key);
        return null;
    }
} 