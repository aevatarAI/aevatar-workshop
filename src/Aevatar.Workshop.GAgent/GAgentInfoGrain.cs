using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.GAgents.Executor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aevatar.Workshop.GAgent;

/// <summary>
/// DTO for transferring GAgent information
/// </summary>
[GenerateSerializer]
public class GAgentInfoDto
{
    [Id(0)] public string GrainType { get; set; } = string.Empty;
    [Id(1)] public List<string> EventTypes { get; set; } = new();
    [Id(2)] public string Description { get; set; } = string.Empty;
    [Id(3)] public string ConfigurationType { get; set; } = string.Empty;
}

/// <summary>
/// DTO for GAgent execution request
/// </summary>
[GenerateSerializer]
public class GAgentExecutionRequest
{
    [Id(0)] public string GrainType { get; set; } = string.Empty;
    [Id(1)] public string EventTypeName { get; set; } = string.Empty;
    [Id(2)] public string EventDataJson { get; set; } = string.Empty;
}

/// <summary>
/// DTO for GAgent execution result
/// </summary>
[GenerateSerializer]
public class GAgentExecutionResult
{
    [Id(0)] public bool Success { get; set; }
    [Id(1)] public string Result { get; set; } = string.Empty;
    [Id(2)] public string Error { get; set; } = string.Empty;
    [Id(3)] public string ResultGAgentId { get; set; } = string.Empty;
}

/// <summary>
/// Grain interface for providing GAgent information
/// </summary>
public interface IGAgentInfoGrain : IGrainWithGuidKey
{
    Task<List<GAgentInfoDto>> GetAllGAgentInfoAsync();
    Task<GAgentInfoDto?> GetGAgentDetailInfoAsync(string grainType);
    Task<List<string>> FindGAgentsByEventTypeAsync(string eventTypeName);
    Task<GAgentExecutionResult> ExecuteGAgentAsync(GAgentExecutionRequest request);
}

/// <summary>
/// Grain implementation that provides GAgent information from the Silo side
/// </summary>
public class GAgentInfoGrain : Grain, IGAgentInfoGrain
{
    private readonly IGAgentService _gAgentService;
    private readonly IGAgentExecutor _gAgentExecutor;
    private readonly ILogger<GAgentInfoGrain> _logger;

    public GAgentInfoGrain(
        IGAgentService gAgentService,
        IGAgentExecutor gAgentExecutor,
        ILogger<GAgentInfoGrain> logger)
    {
        _gAgentService = gAgentService;
        _gAgentExecutor = gAgentExecutor;
        _logger = logger;
    }

    public async Task<List<GAgentInfoDto>> GetAllGAgentInfoAsync()
    {
        try
        {
            var allGAgents = await _gAgentService.GetAllAvailableGAgentInformation();
            var result = new List<GAgentInfoDto>();

            foreach (var (grainType, eventTypes) in allGAgents)
            {
                var dto = new GAgentInfoDto
                {
                    GrainType = grainType.ToString()!,
                    EventTypes = eventTypes.Select(t => t.FullName ?? t.Name).ToList()
                };
                result.Add(dto);
            }

            _logger.LogInformation("Retrieved {Count} GAgent types", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all GAgent information");
            throw;
        }
    }

    public async Task<GAgentInfoDto?> GetGAgentDetailInfoAsync(string grainType)
    {
        try
        {
            var parsedGrainType = GrainType.Create(grainType);
            var detailInfo = await _gAgentService.GetGAgentDetailInfoAsync(parsedGrainType);

            return new GAgentInfoDto
            {
                GrainType = detailInfo.GrainType.ToString()!,
                Description = detailInfo.Description,
                EventTypes = detailInfo.SupportedEventTypes.Select(t => t.FullName ?? t.Name).ToList(),
                ConfigurationType = detailInfo.ConfigurationType?.FullName ?? "None"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get GAgent detail info for {GrainType}", grainType);
            return null;
        }
    }

    public async Task<List<string>> FindGAgentsByEventTypeAsync(string eventTypeName)
    {
        try
        {
            // Find the event type by name
            var allGAgents = await _gAgentService.GetAllAvailableGAgentInformation();
            Type? eventType = null;

            foreach (var (_, eventTypes) in allGAgents)
            {
                eventType = eventTypes.FirstOrDefault(t =>
                    t.Name == eventTypeName || t.FullName == eventTypeName);
                if (eventType != null) break;
            }

            if (eventType == null)
            {
                _logger.LogWarning("Event type {EventTypeName} not found", eventTypeName);
                return new List<string>();
            }

            var grainTypes = await _gAgentService.FindGAgentsByEventTypeAsync(eventType);
            return grainTypes.Select(gt => gt.ToString()!).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to find GAgents by event type {EventTypeName}", eventTypeName);
            return new List<string>();
        }
    }

    public async Task<GAgentExecutionResult> ExecuteGAgentAsync(GAgentExecutionRequest request)
    {
        try
        {
            var grainType = GrainType.Create(request.GrainType);

            // Find the event type
            var allGAgents = await _gAgentService.GetAllAvailableGAgentInformation();
            Type? eventType = null;

            if (allGAgents.TryGetValue(grainType, out var eventTypes))
            {
                eventType = eventTypes.FirstOrDefault(t =>
                    t.Name == request.EventTypeName || t.FullName == request.EventTypeName);
            }

            if (eventType == null)
            {
                return new GAgentExecutionResult
                {
                    Success = false,
                    Error = $"Event type {request.EventTypeName} not found for GAgent {request.GrainType}"
                };
            }

            // Deserialize the event data
            var @event = Newtonsoft.Json.JsonConvert.DeserializeObject(
                request.EventDataJson, eventType) as EventBase;

            if (@event == null)
            {
                return new GAgentExecutionResult
                {
                    Success = false,
                    Error = "Failed to deserialize event data"
                };
            }

            // Use a simpler approach - directly execute and get result
            var resultGAgentId = Guid.NewGuid();
            var gAgentFactory = new GAgentFactory(ServiceProvider.GetRequiredService<IClusterClient>());

            // Get the GAgents
            var targetGAgent = await gAgentFactory.GetGAgentAsync(GrainId.Create(grainType, Guid.NewGuid().ToString()));
            var resultGAgent = await gAgentFactory.GetGAgentAsync<IResultGAgent>(resultGAgentId);
            var publishingGAgent = await gAgentFactory.GetGAgentAsync<IPublishingGAgent>();

            // Set up ResultGAgent
            await resultGAgent.SetExecutionContextAsync(
                resultGAgentId.ToString(),
                AevatarCoreConstants.StreamProvider,
                AevatarGAgentExecutorConstants.GAgentExecutorStreamNamespace);

            // Subscribe ResultGAgent to the target GAgent
            await targetGAgent.RegisterAsync(resultGAgent);

            // Subscribe target GAgent to PublishingGAgent
            await publishingGAgent.RegisterAsync(targetGAgent);

            // Publish the event
            await publishingGAgent.PublishEventAsync(@event);

            // Wait for the result
            string? finalResult = null;
            for (int i = 0; i < 10; i++) // Poll for up to 10 seconds
            {
                await Task.Delay(1000);
                var state = await resultGAgent.GetStateAsync();
                if (!string.IsNullOrEmpty(state.Result))
                {
                    finalResult = state.Result;
                    break;
                }
            }

            // Clean up
            await targetGAgent.UnregisterAsync(resultGAgent);
            await publishingGAgent.UnregisterAsync(targetGAgent);

            return new GAgentExecutionResult
            {
                Success = true,
                Result = finalResult ?? "PENDING", // Special marker for frontend to poll
                ResultGAgentId = resultGAgentId.ToString()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute GAgent");
            return new GAgentExecutionResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }
}