using Aevatar.Core.Abstractions;
using Orleans.Streams;

namespace Aevatar.Workshop.GAgentPlugin;

// 结果通知事件
[GenerateSerializer]
public class ExecutionCompletedEvent
{
    [Id(0)] public string ExecutionId { get; set; } = string.Empty;
    [Id(1)] public string Result { get; set; } = string.Empty;
}

public class GAgentPluginStreams : IGAgentPlugin
{
    private readonly IGAgentFactory _gAgentFactory;
    private readonly IClusterClient _clusterClient;

    public GAgentPluginStreams(IGAgentFactory gAgentFactory, IClusterClient clusterClient)
    {
        _gAgentFactory = gAgentFactory;
        _clusterClient = clusterClient;
    }

    public async Task<string> ExecuteGAgentEventHandler(GrainId grainId, EventBase @event)
    {
        var targetGAgent = await _gAgentFactory.GetGAgentAsync(grainId);
        var resultGAgent = await _gAgentFactory.GetGAgentAsync<IResultGAgentStreams>();
        var publishingGAgent = await _gAgentFactory.GetGAgentAsync<IPublishingGAgent>();

        var executionId = Guid.NewGuid().ToString();

        // 获取结果通知流
        var streamProvider = _clusterClient.GetStreamProvider(AevatarCoreConstants.StreamProvider);
        var resultStream =
            streamProvider.GetStream<ExecutionCompletedEvent>(GAgentPluginConstants.GAgentPluginStreamNamespace,
                executionId);

        // 订阅结果流
        var resultTask = new TaskCompletionSource<string>();
        var subscription = await resultStream.SubscribeAsync((result, token) =>
        {
            resultTask.SetResult(result.Result);
            return Task.CompletedTask;
        });

        try
        {
            await resultGAgent.SetExecutionContextAsync(executionId, AevatarCoreConstants.StreamProvider,
                GAgentPluginConstants.GAgentPluginStreamNamespace);

            await publishingGAgent.PublishEventAsync(@event, targetGAgent, resultGAgent);

            // 等待结果，设置超时
            return await resultTask.Task.WaitAsync(TimeSpan.FromMinutes(5));
        }
        catch (TimeoutException)
        {
            throw new TimeoutException($"ExecuteGAgentEventHandler timeout for execution {executionId}");
        }
        finally
        {
            // 清理订阅
            await subscription.UnsubscribeAsync();
        }
    }
}