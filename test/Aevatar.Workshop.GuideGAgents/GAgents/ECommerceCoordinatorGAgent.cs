using Microsoft.Extensions.Logging;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Aevatar.Workshop.GuideGAgents.GAgents;

/// <summary>
/// E-commerce coordinator GAgent interface
/// </summary>
public interface IECommerceCoordinatorGAgent : IStateGAgent<ECommerceCoordinatorState>
{
    Task InitializeSystemAsync();
    Task<string> ProcessOrderAsync(SubmitOrderRequest request);
    Task<OrderStatus> GetOrderStatusAsync(string orderId);
    Task<SystemHealthStatus> GetSystemHealthAsync();
    Task SeedTestDataAsync();
}

/// <summary>
/// 电商协调器状态
/// </summary>
[GenerateSerializer]
public class ECommerceCoordinatorState : StateBase
{
    [Id(0)] public bool IsInitialized { get; set; }
    [Id(1)] public DateTime InitializedAt { get; set; }
    [Id(2)] public Dictionary<string, Guid> RegisteredAgents { get; set; } = new();
    [Id(3)] public int TotalOrdersProcessed { get; set; }
    [Id(4)] public DateTime LastOrderTime { get; set; }
    [Id(5)] public Dictionary<string, int> AgentHealthScores { get; set; } = new();
}

/// <summary>
/// 电商协调器状态日志事件基类
/// </summary>
[GenerateSerializer]
public abstract class ECommerceCoordinatorStateLogEvent : StateLogEventBase<ECommerceCoordinatorStateLogEvent> { }

/// <summary>
/// 系统初始化日志事件
/// </summary>
[GenerateSerializer]
public class SystemInitializedLogEvent : ECommerceCoordinatorStateLogEvent
{
    [Id(0)] public DateTime InitializedAt { get; init; }
    [Id(1)] public Dictionary<string, Guid> RegisteredAgents { get; init; } = new();
}

/// <summary>
/// 订单处理日志事件
/// </summary>
[GenerateSerializer]
public class OrderProcessedLogEvent : ECommerceCoordinatorStateLogEvent
{
    [Id(0)] public string OrderId { get; init; } = string.Empty;
    [Id(1)] public DateTime ProcessedAt { get; init; }
}

/// <summary>
/// 电商协调器 GAgent 实现
/// </summary>
[GAgent("ecommerce-coordinator", "ecommerce")]
public class ECommerceCoordinatorGAgent : GAgentBase<ECommerceCoordinatorState, ECommerceCoordinatorStateLogEvent>, IECommerceCoordinatorGAgent
{
    // 固定的 GAgent ID，确保一致性
    private static readonly Guid ORDER_AGENT_ID = "order-agent".ToGuid();
    private static readonly Guid INVENTORY_AGENT_ID = "inventory-agent".ToGuid();
    private static readonly Guid PAYMENT_AGENT_ID = "payment-agent".ToGuid();
    private static readonly Guid NOTIFICATION_AGENT_ID = "notification-agent".ToGuid();

    private IGAgentFactory GAgentFactory => 
        ServiceProvider.GetRequiredService<IGAgentFactory>();

    public override Task<string> GetDescriptionAsync()
        => Task.FromResult("Central coordinator that orchestrates all GAgents in the e-commerce system");

    public async Task InitializeSystemAsync()
    {
        if (State.IsInitialized)
        {
            Logger.LogInformation("电商系统已经初始化");
            return;
        }

        Logger.LogInformation("开始初始化电商系统");

        try
        {
            // 创建并注册所有 GAgent
            var orderAgent = await GAgentFactory.GetGAgentAsync<IOrderGAgent>(ORDER_AGENT_ID);
            var inventoryAgent = await GAgentFactory.GetGAgentAsync<IInventoryGAgent>(INVENTORY_AGENT_ID);
            var paymentAgent = await GAgentFactory.GetGAgentAsync<IPaymentGAgent>(PAYMENT_AGENT_ID);
            var notificationAgent = await GAgentFactory.GetGAgentAsync<INotificationGAgent>(NOTIFICATION_AGENT_ID);

            // 注册所有代理进行事件通信
            await RegisterAsync(orderAgent);
            await RegisterAsync(inventoryAgent);
            await RegisterAsync(paymentAgent);
            await RegisterAsync(notificationAgent);

            var registeredAgents = new Dictionary<string, Guid>
            {
                ["order"] = ORDER_AGENT_ID,
                ["inventory"] = INVENTORY_AGENT_ID,
                ["payment"] = PAYMENT_AGENT_ID,
                ["notification"] = NOTIFICATION_AGENT_ID
            };

            // 更新状态
            RaiseEvent(new SystemInitializedLogEvent
            {
                InitializedAt = DateTime.UtcNow,
                RegisteredAgents = registeredAgents
            });
            await ConfirmEvents();

            Logger.LogInformation("电商系统初始化完成，注册了 {AgentCount} 个 GAgent", registeredAgents.Count);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "电商系统初始化失败");
            throw;
        }
    }

    public async Task<string> ProcessOrderAsync(SubmitOrderRequest request)
    {
        if (!State.IsInitialized)
        {
            await InitializeSystemAsync();
        }

        Logger.LogInformation("开始处理订单：客户 {CustomerId}，商品数量 {ItemCount}", 
            request.CustomerId, request.Items.Count);

        try
        {
            var orderAgent = await GAgentFactory.GetGAgentAsync<IOrderGAgent>(ORDER_AGENT_ID);
            var orderId = await orderAgent.SubmitOrderAsync(request);

            // 记录订单处理
            RaiseEvent(new OrderProcessedLogEvent
            {
                OrderId = orderId,
                ProcessedAt = DateTime.UtcNow
            });
            await ConfirmEvents();

            Logger.LogInformation("订单处理完成：{OrderId}", orderId);
            return orderId;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "订单处理失败：客户 {CustomerId}", request.CustomerId);
            throw;
        }
    }

    public async Task<OrderStatus> GetOrderStatusAsync(string orderId)
    {
        var orderAgent = await GAgentFactory.GetGAgentAsync<IOrderGAgent>(ORDER_AGENT_ID);
        return await orderAgent.GetOrderStatusAsync(orderId);
    }

    public async Task<SystemHealthStatus> GetSystemHealthAsync()
    {
        var healthStatus = new SystemHealthStatus
        {
            IsHealthy = true,
            CheckedAt = DateTime.UtcNow,
            ComponentStatuses = new Dictionary<string, ComponentHealth>()
        };

        // 检查每个组件的健康状态
        try
        {
            var orderAgent = await GAgentFactory.GetGAgentAsync<IOrderGAgent>(ORDER_AGENT_ID);
            var orderDesc = await orderAgent.GetDescriptionAsync();
            healthStatus.ComponentStatuses["order"] = new ComponentHealth
            {
                IsHealthy = !string.IsNullOrEmpty(orderDesc),
                LastChecked = DateTime.UtcNow,
                Message = "Order service is running"
            };
        }
        catch (Exception ex)
        {
            healthStatus.IsHealthy = false;
            healthStatus.ComponentStatuses["order"] = new ComponentHealth
            {
                IsHealthy = false,
                LastChecked = DateTime.UtcNow,
                Message = $"Order service error: {ex.Message}"
            };
        }

        try
        {
            var inventoryAgent = await GAgentFactory.GetGAgentAsync<IInventoryGAgent>(INVENTORY_AGENT_ID);
            var inventoryDesc = await inventoryAgent.GetDescriptionAsync();
            healthStatus.ComponentStatuses["inventory"] = new ComponentHealth
            {
                IsHealthy = !string.IsNullOrEmpty(inventoryDesc),
                LastChecked = DateTime.UtcNow,
                Message = "Inventory service is running"
            };
        }
        catch (Exception ex)
        {
            healthStatus.IsHealthy = false;
            healthStatus.ComponentStatuses["inventory"] = new ComponentHealth
            {
                IsHealthy = false,
                LastChecked = DateTime.UtcNow,
                Message = $"Inventory service error: {ex.Message}"
            };
        }

        try
        {
            var paymentAgent = await GAgentFactory.GetGAgentAsync<IPaymentGAgent>(PAYMENT_AGENT_ID);
            var paymentDesc = await paymentAgent.GetDescriptionAsync();
            healthStatus.ComponentStatuses["payment"] = new ComponentHealth
            {
                IsHealthy = !string.IsNullOrEmpty(paymentDesc),
                LastChecked = DateTime.UtcNow,
                Message = "Payment service is running"
            };
        }
        catch (Exception ex)
        {
            healthStatus.IsHealthy = false;
            healthStatus.ComponentStatuses["payment"] = new ComponentHealth
            {
                IsHealthy = false,
                LastChecked = DateTime.UtcNow,
                Message = $"Payment service error: {ex.Message}"
            };
        }

        try
        {
            var notificationAgent = await GAgentFactory.GetGAgentAsync<INotificationGAgent>(NOTIFICATION_AGENT_ID);
            var notificationDesc = await notificationAgent.GetDescriptionAsync();
            healthStatus.ComponentStatuses["notification"] = new ComponentHealth
            {
                IsHealthy = !string.IsNullOrEmpty(notificationDesc),
                LastChecked = DateTime.UtcNow,
                Message = "Notification service is running"
            };
        }
        catch (Exception ex)
        {
            healthStatus.IsHealthy = false;
            healthStatus.ComponentStatuses["notification"] = new ComponentHealth
            {
                IsHealthy = false,
                LastChecked = DateTime.UtcNow,
                Message = $"Notification service error: {ex.Message}"
            };
        }

        return healthStatus;
    }

    public async Task SeedTestDataAsync()
    {
        Logger.LogInformation("开始填充测试数据");

        try
        {
            var inventoryAgent = await GAgentFactory.GetGAgentAsync<IInventoryGAgent>(INVENTORY_AGENT_ID);

            // 添加测试产品
            var products = new[]
            {
                new Product
                {
                    Id = "prod-001",
                    Name = "iPhone 15 Pro",
                    Description = "苹果最新旗舰手机",
                    Price = 8999.00m,
                    SKU = "IPH15PRO-256GB",
                    Category = "Electronics",
                    StockQuantity = 50,
                    MinStockLevel = 10,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsActive = true
                },
                new Product
                {
                    Id = "prod-002",
                    Name = "MacBook Pro M3",
                    Description = "苹果最新专业笔记本电脑",
                    Price = 15999.00m,
                    SKU = "MBP-M3-14",
                    Category = "Electronics",
                    StockQuantity = 30,
                    MinStockLevel = 5,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsActive = true
                },
                new Product
                {
                    Id = "prod-003",
                    Name = "AirPods Pro",
                    Description = "苹果无线降噪耳机",
                    Price = 1999.00m,
                    SKU = "APP-GEN2",
                    Category = "Electronics",
                    StockQuantity = 100,
                    MinStockLevel = 20,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsActive = true
                }
            };

            foreach (var product in products)
            {
                await inventoryAgent.AddProductAsync(product);
                Logger.LogInformation("添加测试产品：{ProductName}", product.Name);
            }

            Logger.LogInformation("测试数据填充完成，添加了 {ProductCount} 个产品", products.Length);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "测试数据填充失败");
            throw;
        }
    }

    protected override void GAgentTransitionState(ECommerceCoordinatorState state, StateLogEventBase<ECommerceCoordinatorStateLogEvent> @event)
    {
        switch (@event)
        {
            case SystemInitializedLogEvent e:
                state.IsInitialized = true;
                state.InitializedAt = e.InitializedAt;
                state.RegisteredAgents = e.RegisteredAgents;
                break;

            case OrderProcessedLogEvent e:
                state.TotalOrdersProcessed++;
                state.LastOrderTime = e.ProcessedAt;
                break;
        }
    }
}

/// <summary>
/// 电商服务类
/// </summary>
public class ECommerceService
{
    private readonly IGAgentFactory _gAgentFactory;
    private readonly ILogger<ECommerceService> _logger;

    // 协调器 ID
    private static readonly Guid COORDINATOR_ID = "ecommerce-coordinator".ToGuid();

    public ECommerceService(IGAgentFactory gAgentFactory, ILogger<ECommerceService> logger)
    {
        _gAgentFactory = gAgentFactory;
        _logger = logger;
    }

    /// <summary>
    /// 初始化电商系统
    /// </summary>
    public async Task InitializeAsync()
    {
        _logger.LogInformation("初始化电商服务");

        var coordinator = await _gAgentFactory.GetGAgentAsync<IECommerceCoordinatorGAgent>(COORDINATOR_ID);
        await coordinator.InitializeSystemAsync();
        await coordinator.SeedTestDataAsync();

        _logger.LogInformation("电商服务初始化完成");
    }

    /// <summary>
    /// 处理订单
    /// </summary>
    public async Task<string> ProcessOrderAsync(SubmitOrderRequest request)
    {
        var coordinator = await _gAgentFactory.GetGAgentAsync<IECommerceCoordinatorGAgent>(COORDINATOR_ID);
        return await coordinator.ProcessOrderAsync(request);
    }

    /// <summary>
    /// 获取订单状态
    /// </summary>
    public async Task<OrderStatus> GetOrderStatusAsync(string orderId)
    {
        var coordinator = await _gAgentFactory.GetGAgentAsync<IECommerceCoordinatorGAgent>(COORDINATOR_ID);
        return await coordinator.GetOrderStatusAsync(orderId);
    }

    /// <summary>
    /// 获取系统健康状态
    /// </summary>
    public async Task<SystemHealthStatus> GetSystemHealthAsync()
    {
        var coordinator = await _gAgentFactory.GetGAgentAsync<IECommerceCoordinatorGAgent>(COORDINATOR_ID);
        return await coordinator.GetSystemHealthAsync();
    }

    /// <summary>
    /// 模拟支付处理
    /// </summary>
    public async Task<PaymentResult> ProcessPaymentAsync(string orderId, PaymentRequest paymentRequest)
    {
        var paymentAgent = await _gAgentFactory.GetGAgentAsync<IPaymentGAgent>("payment-agent".ToGuid());
        return await paymentAgent.ProcessPaymentAsync(paymentRequest);
    }
}

/// <summary>
/// 系统健康状态
/// </summary>
[GenerateSerializer]
public class SystemHealthStatus
{
    [Id(0)] public bool IsHealthy { get; set; }
    [Id(1)] public DateTime CheckedAt { get; set; }
    [Id(2)] public Dictionary<string, ComponentHealth> ComponentStatuses { get; set; } = new();
}

/// <summary>
/// 组件健康状态
/// </summary>
[GenerateSerializer]
public class ComponentHealth
{
    [Id(0)] public bool IsHealthy { get; set; }
    [Id(1)] public DateTime LastChecked { get; set; }
    [Id(2)] public string Message { get; set; } = string.Empty;
}

/// <summary>
/// GUID 扩展方法
/// </summary>
public static class StringExtensions
{
    public static Guid ToGuid(this string input)
    {
        using var md5 = System.Security.Cryptography.MD5.Create();
        var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
        return new Guid(hash);
    }
}