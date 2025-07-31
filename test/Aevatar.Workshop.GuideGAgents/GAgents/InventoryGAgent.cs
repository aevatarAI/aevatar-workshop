using Microsoft.Extensions.Logging;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.Workshop.GuideGAgents.Events;

namespace Aevatar.Workshop.GuideGAgents.GAgents;

/// <summary>
/// Inventory management GAgent interface
/// </summary>
public interface IInventoryGAgent : IStateGAgent<InventoryState>
{
    Task AddProductAsync(Product product);
    Task UpdateStockAsync(string productId, int quantity);
    Task<Product?> GetProductAsync(string productId);
    Task<List<Product>> GetLowStockProductsAsync(int threshold = 10);
    Task<bool> CheckAvailabilityAsync(string productId, int quantity);
    Task ReserveInventoryAsync(string orderId, List<OrderItem> items);
    Task ReleaseReservationAsync(string orderId);
}

/// <summary>
/// Inventory state
/// </summary>
[GenerateSerializer]
public class InventoryState : StateBase
{
    [Id(0)] public Dictionary<string, Product> Products { get; set; } = new();
    [Id(1)] public Dictionary<string, List<InventoryReservation>> Reservations { get; set; } = new();
    [Id(2)] public Dictionary<string, int> StockLevels { get; set; } = new();
    [Id(3)] public Dictionary<string, int> ReservedQuantities { get; set; } = new();
    [Id(4)] public DateTime LastUpdated { get; set; }
}

/// <summary>
/// 库存状态日志事件基类
/// </summary>
[GenerateSerializer]
public abstract class InventoryStateLogEvent : StateLogEventBase<InventoryStateLogEvent> { }

/// <summary>
/// 产品添加日志事件
/// </summary>
[GenerateSerializer]
public class ProductAddedLogEvent : InventoryStateLogEvent
{
    [Id(0)] public Product Product { get; init; } = new();
}

/// <summary>
/// 库存更新日志事件
/// </summary>
[GenerateSerializer]
public class StockUpdatedLogEvent : InventoryStateLogEvent
{
    [Id(0)] public string ProductId { get; init; } = string.Empty;
    [Id(1)] public int PreviousQuantity { get; init; }
    [Id(2)] public int NewQuantity { get; init; }
    [Id(3)] public DateTime UpdatedAt { get; init; }
}

/// <summary>
/// 库存预留日志事件
/// </summary>
[GenerateSerializer]
public class InventoryReservedLogEvent : InventoryStateLogEvent
{
    [Id(0)] public string OrderId { get; init; } = string.Empty;
    [Id(1)] public List<InventoryReservation> Reservations { get; init; } = new();
    [Id(2)] public DateTime ReservedAt { get; init; }
}

/// <summary>
/// 库存预留释放日志事件
/// </summary>
[GenerateSerializer]
public class ReservationReleasedLogEvent : InventoryStateLogEvent
{
    [Id(0)] public string OrderId { get; init; } = string.Empty;
    [Id(1)] public DateTime ReleasedAt { get; init; }
}

/// <summary>
/// 库存管理 GAgent 实现
/// </summary>
[GAgent("inventory", "ecommerce")]
public class InventoryGAgent : GAgentBase<InventoryState, InventoryStateLogEvent>, IInventoryGAgent
{
    public override Task<string> GetDescriptionAsync()
        => Task.FromResult("Intelligent agent that manages product inventory and inventory reservations");

    public async Task AddProductAsync(Product product)
    {
        if (string.IsNullOrEmpty(product.Id))
            throw new ArgumentException("产品ID不能为空", nameof(product.Id));

        Logger.LogInformation("添加产品：{ProductId} - {ProductName}", product.Id, product.Name);

        RaiseEvent(new ProductAddedLogEvent { Product = product });
        await ConfirmEvents();

        Logger.LogInformation("产品 {ProductId} 已成功添加到库存", product.Id);
    }

    public async Task UpdateStockAsync(string productId, int quantity)
    {
        if (!State.Products.ContainsKey(productId))
            throw new InvalidOperationException($"产品 {productId} 不存在");

        var currentQuantity = State.StockLevels.GetValueOrDefault(productId, 0);
        
        Logger.LogInformation("更新产品 {ProductId} 库存：{CurrentQuantity} -> {NewQuantity}", 
            productId, currentQuantity, quantity);

        RaiseEvent(new StockUpdatedLogEvent
        {
            ProductId = productId,
            PreviousQuantity = currentQuantity,
            NewQuantity = quantity,
            UpdatedAt = DateTime.UtcNow
        });
        await ConfirmEvents();

        Logger.LogInformation("产品 {ProductId} 库存已更新为 {Quantity}", productId, quantity);
    }

    public Task<Product?> GetProductAsync(string productId)
    {
        return Task.FromResult(
            State.Products.TryGetValue(productId, out var product) ? product : null
        );
    }

    public Task<List<Product>> GetLowStockProductsAsync(int threshold = 10)
    {
        var lowStockProducts = State.Products.Values
            .Where(p => (State.StockLevels.GetValueOrDefault(p.Id, 0) - 
                        State.ReservedQuantities.GetValueOrDefault(p.Id, 0)) < threshold)
            .ToList();

        return Task.FromResult(lowStockProducts);
    }

    public Task<bool> CheckAvailabilityAsync(string productId, int quantity)
    {
        if (!State.Products.ContainsKey(productId))
            return Task.FromResult(false);

        var availableQuantity = State.StockLevels.GetValueOrDefault(productId, 0) - 
                               State.ReservedQuantities.GetValueOrDefault(productId, 0);

        return Task.FromResult(availableQuantity >= quantity);
    }

    public async Task ReserveInventoryAsync(string orderId, List<OrderItem> items)
    {
        Logger.LogInformation("开始为订单 {OrderId} 预留库存", orderId);

        var reservations = new List<InventoryReservation>();
        var unavailableItems = new List<string>();

        // 检查所有商品的可用性
        foreach (var item in items)
        {
            var availableQuantity = State.StockLevels.GetValueOrDefault(item.ProductId, 0) - 
                                   State.ReservedQuantities.GetValueOrDefault(item.ProductId, 0);

            if (availableQuantity < item.Quantity)
            {
                unavailableItems.Add($"{item.ProductName} (需要: {item.Quantity}, 可用: {availableQuantity})");
                Logger.LogWarning("产品 {ProductId} 库存不足，需要: {Required}, 可用: {Available}", 
                    item.ProductId, item.Quantity, availableQuantity);
            }
            else
            {
                reservations.Add(new InventoryReservation
                {
                    ProductId = item.ProductId,
                    SKU = item.SKU,
                    ReservedQuantity = item.Quantity,
                    ReservedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddHours(2) // 2小时后过期
                });
            }
        }

        if (unavailableItems.Count > 0)
        {
            // 发布库存预留失败事件
            await PublishAsync(new InventoryReservationFailedEvent
            {
                OrderId = orderId,
                UnavailableItems = unavailableItems,
                Reason = "库存不足",
                FailedAt = DateTime.UtcNow
            });

            // 发布订单验证失败事件
            await PublishAsync(new OrderValidatedEvent
            {
                OrderId = orderId,
                IsValid = false,
                ValidationMessage = "库存不足",
                ValidationErrors = unavailableItems,
                ValidatedAt = DateTime.UtcNow
            });

            Logger.LogWarning("订单 {OrderId} 库存预留失败：{UnavailableItems}", 
                orderId, string.Join(", ", unavailableItems));
            return;
        }

        // 预留库存
        RaiseEvent(new InventoryReservedLogEvent
        {
            OrderId = orderId,
            Reservations = reservations,
            ReservedAt = DateTime.UtcNow
        });
        await ConfirmEvents();

        // 发布库存预留成功事件
        await PublishAsync(new InventoryReservedEvent
        {
            OrderId = orderId,
            Reservations = reservations,
            ReservedAt = DateTime.UtcNow
        });

        // 发布订单验证成功事件
        await PublishAsync(new OrderValidatedEvent
        {
            OrderId = orderId,
            IsValid = true,
            ValidationMessage = "库存验证通过",
            ValidationErrors = new List<string>(),
            ValidatedAt = DateTime.UtcNow
        });

        Logger.LogInformation("订单 {OrderId} 库存预留成功，预留 {Count} 个商品", orderId, reservations.Count);
    }

    public async Task ReleaseReservationAsync(string orderId)
    {
        if (!State.Reservations.ContainsKey(orderId))
        {
            Logger.LogWarning("尝试释放不存在的预留：{OrderId}", orderId);
            return;
        }

        Logger.LogInformation("释放订单 {OrderId} 的库存预留", orderId);

        RaiseEvent(new ReservationReleasedLogEvent
        {
            OrderId = orderId,
            ReleasedAt = DateTime.UtcNow
        });
        await ConfirmEvents();

        Logger.LogInformation("订单 {OrderId} 的库存预留已释放", orderId);
    }

    #region 事件处理器

    [EventHandler]
    public async Task HandleOrderSubmittedAsync(OrderSubmittedEvent @event)
    {
        Logger.LogInformation("收到订单提交事件，开始验证库存：{OrderId}", @event.OrderId);

        await ReserveInventoryAsync(@event.OrderId, @event.Items);
    }

    [EventHandler]
    public async Task HandlePaymentProcessedAsync(PaymentProcessedEvent @event)
    {
        if (@event.IsSuccessful)
        {
            // 支付成功，将预留转为实际扣减
            if (State.Reservations.TryGetValue(@event.OrderId, out var reservations))
            {
                foreach (var reservation in reservations)
                {
                    var currentStock = State.StockLevels.GetValueOrDefault(reservation.ProductId, 0);
                    var newStock = Math.Max(0, currentStock - reservation.ReservedQuantity);
                    
                    RaiseEvent(new StockUpdatedLogEvent
                    {
                        ProductId = reservation.ProductId,
                        PreviousQuantity = currentStock,
                        NewQuantity = newStock,
                        UpdatedAt = DateTime.UtcNow
                    });
                }

                // 移除预留
                RaiseEvent(new ReservationReleasedLogEvent
                {
                    OrderId = @event.OrderId,
                    ReleasedAt = DateTime.UtcNow
                });

                await ConfirmEvents();

                Logger.LogInformation("订单 {OrderId} 支付成功，已扣减库存", @event.OrderId);
            }
        }
        else
        {
            // 支付失败，释放预留
            await ReleaseReservationAsync(@event.OrderId);
            Logger.LogInformation("订单 {OrderId} 支付失败，已释放库存预留", @event.OrderId);
        }
    }

    #endregion

    protected override void GAgentTransitionState(InventoryState state, StateLogEventBase<InventoryStateLogEvent> @event)
    {
        switch (@event)
        {
            case ProductAddedLogEvent e:
                state.Products[e.Product.Id] = e.Product;
                state.StockLevels[e.Product.Id] = e.Product.StockQuantity;
                state.ReservedQuantities[e.Product.Id] = 0;
                state.LastUpdated = DateTime.UtcNow;
                break;

            case StockUpdatedLogEvent e:
                state.StockLevels[e.ProductId] = e.NewQuantity;
                state.LastUpdated = e.UpdatedAt;
                break;

            case InventoryReservedLogEvent e:
                state.Reservations[e.OrderId] = e.Reservations;
                
                // 更新预留数量
                foreach (var reservation in e.Reservations)
                {
                    var currentReserved = state.ReservedQuantities.GetValueOrDefault(reservation.ProductId, 0);
                    state.ReservedQuantities[reservation.ProductId] = currentReserved + reservation.ReservedQuantity;
                }
                state.LastUpdated = e.ReservedAt;
                break;

            case ReservationReleasedLogEvent e:
                if (state.Reservations.TryGetValue(e.OrderId, out var reservations))
                {
                    // 减少预留数量
                    foreach (var reservation in reservations)
                    {
                        var currentReserved = state.ReservedQuantities.GetValueOrDefault(reservation.ProductId, 0);
                        state.ReservedQuantities[reservation.ProductId] = Math.Max(0, currentReserved - reservation.ReservedQuantity);
                    }
                    
                    // 移除预留记录
                    state.Reservations.Remove(e.OrderId);
                }
                state.LastUpdated = e.ReleasedAt;
                break;
        }
    }
}

/// <summary>
/// 产品实体
/// </summary>
[GenerateSerializer]
public class Product
{
    [Id(0)] public string Id { get; set; } = string.Empty;
    [Id(1)] public string Name { get; set; } = string.Empty;
    [Id(2)] public string Description { get; set; } = string.Empty;
    [Id(3)] public decimal Price { get; set; }
    [Id(4)] public string SKU { get; set; } = string.Empty;
    [Id(5)] public string Category { get; set; } = string.Empty;
    [Id(6)] public int StockQuantity { get; set; }
    [Id(7)] public int MinStockLevel { get; set; }
    [Id(8)] public DateTime CreatedAt { get; set; }
    [Id(9)] public DateTime UpdatedAt { get; set; }
    [Id(10)] public bool IsActive { get; set; } = true;
}