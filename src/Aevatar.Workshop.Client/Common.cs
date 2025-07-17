using Aevatar.Core.Abstractions;
using Aevatar.GAgents.AIGAgent.Agent;
using Aevatar.GAgents.AIGAgent.Dtos;
using Aevatar.GAgents.Basic.BasicGAgents.GroupGAgent;
using Aevatar.GAgents.Basic.PublishGAgent;
using Aevatar.GAgents.GroupChat.Core.Dto;
using Aevatar.GAgents.GroupChat.WorkflowCoordinator;
using Aevatar.GAgents.GroupChat.WorkflowCoordinator.Dto;
using Aevatar.GAgents.GroupChat.WorkflowCoordinator.GEvent;
using Aevatar.Workshop.GAgent;
using GroupChat.GAgent.Feature.Common;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Aevatar.Workshop.Client;

public static class Common
{
    // Support multiple named recorders for different demos
    private static readonly ConcurrentDictionary<string, IStateGAgent<RecorderGAgentState>> Recorders = new();

    public static void SetRecorder(string demoKey, IStateGAgent<RecorderGAgentState> recorder)
        => Recorders[demoKey] = recorder;

    public static IStateGAgent<RecorderGAgentState>? GetRecorder(string demoKey)
        => Recorders.TryGetValue(demoKey, out var rec) ? rec : null;
}

public static class WorkflowDemo
{
    public static async Task RunSimpleWorkflowAsync(IGAgentFactory gAgentFactory, ILogger logger)
    {
        logger.LogInformation("Starting WorkflowDemo...");

        try
        {
            // Create workflow coordinator
            var workflowId = Guid.NewGuid();
            var coordinator = await gAgentFactory.GetGAgentAsync<IWorkflowCoordinatorGAgent>(workflowId);

            // Create group for workflow
            var groupId = Guid.NewGuid();
            var groupAgent = await gAgentFactory.GetGAgentAsync<IGroupGAgent>(groupId);

                        // Create work units
            var mathAgent = await gAgentFactory.GetGAgentAsync<IWorkflowMathGAgent>(Guid.NewGuid());
            await mathAgent.ConfigAsync(new MathGAgentConfigDto { MemberName = "Calculator" });
            
            var timeAgent = await gAgentFactory.GetGAgentAsync<IWorkflowTimeConverterGAgent>(Guid.NewGuid());
            await timeAgent.ConfigAsync(new TimeConverterGAgentConfigDto { MemberName = "TimeKeeper" });

            var dataAgent = await gAgentFactory.GetGAgentAsync<IDataProcessorGAgent>(Guid.NewGuid());
            await dataAgent.ConfigAsync(new DataProcessorGAgentConfigDto
            {
                MemberName = "DataProcessor",
                ProcessingMode = "aggregate"
            });

            // Register all agents with the group
            await groupAgent.RegisterAsync(mathAgent);
            await groupAgent.RegisterAsync(timeAgent);
            await groupAgent.RegisterAsync(dataAgent);
            await groupAgent.RegisterAsync(coordinator);

            // Configure workflow: Math -> Time -> Data
            var workflowUnits = new List<WorkflowUnitDto>
            {
                new WorkflowUnitDto
                {
                    GrainId = mathAgent.GetGrainId().ToString(),
                    NextGrainId = timeAgent.GetGrainId().ToString()
                },
                new WorkflowUnitDto
                {
                    GrainId = timeAgent.GetGrainId().ToString(),
                    NextGrainId = dataAgent.GetGrainId().ToString()
                },
                new WorkflowUnitDto
                {
                    GrainId = dataAgent.GetGrainId().ToString(),
                    NextGrainId = null // Terminal node
                }
            };

            await coordinator.ConfigAsync(new WorkflowCoordinatorConfigDto
            {
                WorkflowUnitList = workflowUnits,
                InitContent = "Calculate 100 + 50, then get current time, finally aggregate the results"
            });

            logger.LogInformation("Workflow configured successfully");

            // Start the workflow
            var publishingGAgent = await gAgentFactory.GetGAgentAsync<Core.Abstractions.IPublishingGAgent>(Guid.NewGuid());
            await publishingGAgent.PublishEventAsync(new StartWorkflowCoordinatorEvent
            {
                InitContent = "Start processing: calculate 100 + 50"
            }, coordinator);

            logger.LogInformation("Workflow started");

            // Wait for completion
            await Task.Delay(5000);

            var state = await coordinator.GetStateAsync();
            logger.LogInformation($"Workflow status: {state.WorkflowStatus}");

            logger.LogInformation("WorkflowDemo completed");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in WorkflowDemo");
            throw;
        }
    }
}