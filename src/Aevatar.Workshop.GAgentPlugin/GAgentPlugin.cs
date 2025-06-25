using Aevatar.Core.Abstractions;
using Orleans.Streams;
using System.Collections.Concurrent;

namespace Aevatar.Workshop.GAgentPlugin;

public class GAgentPlugin : IGAgentPlugin
{
    private readonly IGAgentFactory _gAgentFactory;
    private static readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _pendingResults = new();

    public GAgentPlugin(IGAgentFactory gAgentFactory)
    {
        _gAgentFactory = gAgentFactory;
    }

    public async Task<string> ExecuteGAgentEventHandler(GrainId grainId, EventBase @event)
    {
        var targetGAgent = await _gAgentFactory.GetGAgentAsync(grainId);
        var resultGAgent = await _gAgentFactory.GetGAgentAsync<IResultGAgent>();
        var publishingGAgent = await _gAgentFactory.GetGAgentAsync<IPublishingGAgent>();
        
        // 生成唯一的执行 ID
        var executionId = Guid.NewGuid().ToString();
        var taskCompletionSource = new TaskCompletionSource<string>();
        _pendingResults[executionId] = taskCompletionSource;
        
        // 注册 ResultGAgent 的通知回调
        ResultGAgent.RegisterCompletionCallback(executionId, (result) =>
        {
            if (_pendingResults.TryRemove(executionId, out var tcs))
            {
                tcs.SetResult(result);
            }
        });
        
        // 清理 ResultGAgent 的状态并设置执行 ID
        await resultGAgent.ClearResultAsync();
        await resultGAgent.SetExecutionIdAsync(executionId);
        
        // 发布事件
        await publishingGAgent.PublishEventAsync(@event, targetGAgent, resultGAgent);
        
        // 等待结果，设置超时
        try
        {
            return await taskCompletionSource.Task.WaitAsync(TimeSpan.FromMinutes(5));
        }
        catch (TimeoutException)
        {
            _pendingResults.TryRemove(executionId, out _);
            throw new TimeoutException($"ExecuteGAgentEventHandler timeout for execution {executionId}");
        }
    }
}