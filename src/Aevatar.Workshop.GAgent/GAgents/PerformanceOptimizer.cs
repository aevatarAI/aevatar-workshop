using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Aevatar.Workshop.GAgent.GAgents;

/// <summary>
/// Performance optimization utilities for the Theory Reasoning Engine
/// Provides caching, batching, and concurrency optimization features
/// </summary>
public class PerformanceOptimizer
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<PerformanceOptimizer> _logger;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _operationSemaphores;
    private readonly ConcurrentQueue<LLMBatchRequest> _llmBatchQueue;
    private readonly Timer _batchProcessingTimer;

    // Performance configuration
    private const int MAX_CONCURRENT_LLM_CALLS = 3;
    private const int BATCH_SIZE = 5;
    private const int BATCH_TIMEOUT_MS = 2000;
    private const int CACHE_EXPIRY_MINUTES = 30;

    public PerformanceOptimizer(IMemoryCache memoryCache, ILogger<PerformanceOptimizer> logger)
    {
        _memoryCache = memoryCache;
        _logger = logger;
        _operationSemaphores = new ConcurrentDictionary<string, SemaphoreSlim>();
        _llmBatchQueue = new ConcurrentQueue<LLMBatchRequest>();
        
        // Initialize batch processing timer
        _batchProcessingTimer = new Timer(ProcessLLMBatch, null, BATCH_TIMEOUT_MS, BATCH_TIMEOUT_MS);
    }

    #region Caching Optimizations

    /// <summary>
    /// Cached theory retrieval with smart invalidation
    /// </summary>
    public async Task<T?> GetCachedAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null) where T : class
    {
        if (_memoryCache.TryGetValue(key, out T? cached))
        {
            _logger.LogDebug("Cache hit for key: {CacheKey}", key);
            return cached;
        }

        _logger.LogDebug("Cache miss for key: {CacheKey}, executing factory", key);
        var result = await factory();
        
        if (result != null)
        {
            var cacheExpiry = expiry ?? TimeSpan.FromMinutes(CACHE_EXPIRY_MINUTES);
            _memoryCache.Set(key, result, cacheExpiry);
        }

        return result;
    }

    /// <summary>
    /// Batch cache operations for improved performance
    /// </summary>
    public async Task<Dictionary<string, T?>> GetCachedBatchAsync<T>(
        Dictionary<string, Func<Task<T>>> keyFactoryPairs) where T : class
    {
        var results = new Dictionary<string, T?>();
        var missingKeys = new Dictionary<string, Func<Task<T>>>();

        // Check cache for all keys
        foreach (var kvp in keyFactoryPairs)
        {
            if (_memoryCache.TryGetValue(kvp.Key, out T? cached))
            {
                results[kvp.Key] = cached;
            }
            else
            {
                missingKeys[kvp.Key] = kvp.Value;
            }
        }

        // Execute factories for missing keys in parallel
        if (missingKeys.Count > 0)
        {
            var tasks = missingKeys.Select(async kvp =>
            {
                var result = await kvp.Value();
                if (result != null)
                {
                    _memoryCache.Set(kvp.Key, result, TimeSpan.FromMinutes(CACHE_EXPIRY_MINUTES));
                }
                return new { Key = kvp.Key, Value = result };
            });

            var factoryResults = await Task.WhenAll(tasks);
            foreach (var factoryResult in factoryResults)
            {
                results[factoryResult.Key] = factoryResult.Value;
            }
        }

        _logger.LogDebug("Batch cache operation: {CacheHits} hits, {CacheMisses} misses", 
            keyFactoryPairs.Count - missingKeys.Count, missingKeys.Count);

        return results;
    }

    /// <summary>
    /// Invalidate cache entries with pattern matching
    /// </summary>
    public void InvalidateCachePattern(string pattern)
    {
        // Note: MemoryCache doesn't have built-in pattern invalidation
        // In production, consider using Redis or implementing a custom cache wrapper
        _logger.LogInformation("Cache invalidation requested for pattern: {Pattern}", pattern);
    }

    #endregion

    #region Concurrency Optimizations

    /// <summary>
    /// Execute operation with controlled concurrency
    /// </summary>
    public async Task<T> ExecuteWithConcurrencyLimitAsync<T>(
        string operationType, 
        Func<Task<T>> operation, 
        int maxConcurrency = 5)
    {
        var semaphore = _operationSemaphores.GetOrAdd(operationType, 
            _ => new SemaphoreSlim(maxConcurrency, maxConcurrency));

        await semaphore.WaitAsync();
        try
        {
            _logger.LogDebug("Executing {OperationType} with concurrency control", operationType);
            return await operation();
        }
        finally
        {
            semaphore.Release();
        }
    }

    /// <summary>
    /// Batch processing for similar operations
    /// </summary>
    public async Task<List<TResult>> ProcessBatchAsync<TInput, TResult>(
        IEnumerable<TInput> inputs,
        Func<TInput, Task<TResult>> processor,
        int batchSize = 10)
    {
        var results = new List<TResult>();
        var inputList = inputs.ToList();

        _logger.LogDebug("Processing batch of {Count} items with batch size {BatchSize}", 
            inputList.Count, batchSize);

        for (int i = 0; i < inputList.Count; i += batchSize)
        {
            var batch = inputList.Skip(i).Take(batchSize);
            var batchTasks = batch.Select(processor);
            var batchResults = await Task.WhenAll(batchTasks);
            results.AddRange(batchResults);

            // Optional: Add small delay between batches to prevent overwhelming
            if (i + batchSize < inputList.Count)
            {
                await Task.Delay(50);
            }
        }

        return results;
    }

    #endregion

    #region LLM Call Optimizations

    /// <summary>
    /// Queue LLM request for batch processing
    /// </summary>
    public async Task<string> QueueLLMRequestAsync(string prompt, string model = "AzureOpenAI")
    {
        var request = new LLMBatchRequest
        {
            Id = Guid.NewGuid().ToString(),
            Prompt = prompt,
            Model = model,
            TaskCompletionSource = new TaskCompletionSource<string>()
        };

        _llmBatchQueue.Enqueue(request);
        _logger.LogDebug("Queued LLM request {RequestId} for batch processing", request.Id);

        return await request.TaskCompletionSource.Task;
    }

    /// <summary>
    /// Process queued LLM requests in batches
    /// </summary>
    private async void ProcessLLMBatch(object? state)
    {
        var batch = new List<LLMBatchRequest>();
        
        // Collect batch
        while (batch.Count < BATCH_SIZE && _llmBatchQueue.TryDequeue(out var request))
        {
            batch.Add(request);
        }

        if (batch.Count == 0) return;

        _logger.LogDebug("Processing LLM batch of {BatchSize} requests", batch.Count);

        try
        {
            // Group by model for efficiency
            var modelGroups = batch.GroupBy(r => r.Model);
            
            foreach (var modelGroup in modelGroups)
            {
                await ProcessModelBatch(modelGroup.ToList());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing LLM batch");
            
            // Complete all tasks with error
            foreach (var request in batch)
            {
                request.TaskCompletionSource.SetException(ex);
            }
        }
    }

    /// <summary>
    /// Process requests for a specific model
    /// </summary>
    private async Task ProcessModelBatch(List<LLMBatchRequest> modelBatch)
    {
        var semaphore = new SemaphoreSlim(MAX_CONCURRENT_LLM_CALLS);
        var tasks = modelBatch.Select(async request =>
        {
            await semaphore.WaitAsync();
            try
            {
                // Here you would integrate with your actual LLM service
                // For now, simulate the call
                await Task.Delay(1000); // Simulate LLM latency
                var response = $"Mock LLM response for: {request.Prompt.Substring(0, Math.Min(50, request.Prompt.Length))}...";
                
                request.TaskCompletionSource.SetResult(response);
            }
            catch (Exception ex)
            {
                request.TaskCompletionSource.SetException(ex);
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
    }

    #endregion

    #region Memory Optimizations

    /// <summary>
    /// Optimize object serialization for state management
    /// </summary>
    public async Task<T> OptimizeStateOperationAsync<T>(Func<Task<T>> stateOperation)
    {
        // Force garbage collection before large state operations
        var beforeMemory = GC.GetTotalMemory(false);
        
        try
        {
            return await stateOperation();
        }
        finally
        {
            var afterMemory = GC.GetTotalMemory(false);
            var memoryDiff = afterMemory - beforeMemory;
            
            if (memoryDiff > 10 * 1024 * 1024) // 10 MB threshold
            {
                _logger.LogInformation("Large memory allocation detected: {MemoryDiff} bytes, triggering GC", 
                    memoryDiff);
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }
    }

    /// <summary>
    /// Get memory usage statistics
    /// </summary>
    public MemoryStats GetMemoryStats()
    {
        return new MemoryStats
        {
            TotalMemory = GC.GetTotalMemory(false),
            Generation0Collections = GC.CollectionCount(0),
            Generation1Collections = GC.CollectionCount(1),
            Generation2Collections = GC.CollectionCount(2),
            CacheEntryCount = GetCacheEntryCount()
        };
    }

    /// <summary>
    /// Get approximate cache entry count
    /// </summary>
    private int GetCacheEntryCount()
    {
        // MemoryCache doesn't expose entry count directly
        // In production, consider implementing a cache wrapper that tracks this
        return 0;
    }

    #endregion

    #region Performance Monitoring

    /// <summary>
    /// Track operation performance metrics
    /// </summary>
    public async Task<PerformanceMetrics> MeasureOperationAsync<T>(
        string operationName, 
        Func<Task<T>> operation)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var startMemory = GC.GetTotalMemory(false);
        Exception? exception = null;

        try
        {
            await operation();
        }
        catch (Exception ex)
        {
            exception = ex;
            throw;
        }
        finally
        {
            stopwatch.Stop();
            var endMemory = GC.GetTotalMemory(false);

            var metrics = new PerformanceMetrics
            {
                OperationName = operationName,
                Duration = stopwatch.Elapsed,
                MemoryAllocated = endMemory - startMemory,
                Success = exception == null,
                Timestamp = DateTime.UtcNow
            };

            _logger.LogInformation("Operation {OperationName} completed in {Duration}ms, " +
                                 "memory allocated: {MemoryAllocated} bytes, success: {Success}",
                operationName, metrics.Duration.TotalMilliseconds, 
                metrics.MemoryAllocated, metrics.Success);
        }

        return new PerformanceMetrics();
    }

    #endregion

    public void Dispose()
    {
        _batchProcessingTimer?.Dispose();
        foreach (var semaphore in _operationSemaphores.Values)
        {
            semaphore.Dispose();
        }
        _operationSemaphores.Clear();
    }
}

#region Supporting Classes

public class LLMBatchRequest
{
    public string Id { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public TaskCompletionSource<string> TaskCompletionSource { get; set; } = new();
}

public class MemoryStats
{
    public long TotalMemory { get; set; }
    public int Generation0Collections { get; set; }
    public int Generation1Collections { get; set; }
    public int Generation2Collections { get; set; }
    public int CacheEntryCount { get; set; }
}

public class PerformanceMetrics
{
    public string OperationName { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public long MemoryAllocated { get; set; }
    public bool Success { get; set; }
    public DateTime Timestamp { get; set; }
}

#endregion