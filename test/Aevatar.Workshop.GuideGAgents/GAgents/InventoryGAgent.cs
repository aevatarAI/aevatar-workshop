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
/// Inventory state log event base class
/// </summary>
[GenerateSerializer]
public abstract class InventoryStateLogEvent : StateLogEventBase<InventoryStateLogEvent> { }

/// <summary>
/// Product added log event
/// </summary>
[GenerateSerializer]
public class ProductAddedLogEvent : InventoryStateLogEvent
{
    [Id(0)] public Product Product { get; init; } = new();
}

/// <summary>
/// Inventory update log event
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
/// Inventory reservation log event
/// </summary>
[GenerateSerializer]
public class InventoryReservedLogEvent : InventoryStateLogEvent
{
    [Id(0)] public string OrderId { get; init; } = string.Empty;
    [Id(1)] public List<InventoryReservation> Reservations { get; init; } = new();
    [Id(2)] public DateTime ReservedAt { get; init; }
}

/// <summary>
/// Inventory reservation release log event
/// </summary>
[GenerateSerializer]
public class ReservationReleasedLogEvent : InventoryStateLogEvent
{
    [Id(0)] public string OrderId { get; init; } = string.Empty;
    [Id(1)] public DateTime ReleasedAt { get; init; }
}

/// <summary>
/// Inventory management GAgent implementation
/// </summary>
[GAgent("inventory", "ecommerce")]
public class InventoryGAgent : GAgentBase<InventoryState, InventoryStateLogEvent>, IInventoryGAgent
{
    public override Task<string> GetDescriptionAsync()
        => Task.FromResult("Intelligent agent that manages product inventory and inventory reservations");

    public async Task AddProductAsync(Product product)
    {
        if (string.IsNullOrEmpty(product.Id))
            throw new ArgumentException("Product ID cannot be empty", nameof(product.Id));

        Logger.LogInformation("Adding product: {ProductId} - {ProductName}", product.Id, product.Name);

        RaiseEvent(new ProductAddedLogEvent { Product = product });
        await ConfirmEvents();

        Logger.LogInformation("Product {ProductId} successfully added to inventory", product.Id);
    }

    public async Task UpdateStockAsync(string productId, int quantity)
    {
        if (!State.Products.ContainsKey(productId))
            throw new InvalidOperationException($"Product {productId} does not exist");

        var currentQuantity = State.StockLevels.GetValueOrDefault(productId, 0);
        
        Logger.LogInformation("Updating product {ProductId} inventory: {CurrentQuantity} -> {NewQuantity}", 
            productId, currentQuantity, quantity);

        RaiseEvent(new StockUpdatedLogEvent
        {
            ProductId = productId,
            PreviousQuantity = currentQuantity,
            NewQuantity = quantity,
            UpdatedAt = DateTime.UtcNow
        });
        await ConfirmEvents();

        Logger.LogInformation("Product {ProductId} inventory updated to {Quantity}", productId, quantity);
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
        Logger.LogInformation("Starting to reserve inventory for order {OrderId}", orderId);

        var reservations = new List<InventoryReservation>();
        var unavailableItems = new List<string>();

        // Check availability of all items
        foreach (var item in items)
        {
            var availableQuantity = State.StockLevels.GetValueOrDefault(item.ProductId, 0) - 
                                   State.ReservedQuantities.GetValueOrDefault(item.ProductId, 0);

            if (availableQuantity < item.Quantity)
            {
                unavailableItems.Add($"{item.ProductName} (Required: {item.Quantity}, Available: {availableQuantity})");
                Logger.LogWarning("Product {ProductId} insufficient inventory, Required: {Required}, Available: {Available}", 
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
                    ExpiresAt = DateTime.UtcNow.AddHours(2) // Expires after 2 hours
                });
            }
        }

        if (unavailableItems.Count > 0)
        {
            // Publish inventory reservation failed event
            await PublishAsync(new InventoryReservationFailedEvent
            {
                OrderId = orderId,
                UnavailableItems = unavailableItems,
                Reason = "Insufficient inventory",
                FailedAt = DateTime.UtcNow
            });

            // Publish order validation failed event
            await PublishAsync(new OrderValidatedEvent
            {
                OrderId = orderId,
                IsValid = false,
                ValidationMessage = "Insufficient inventory",
                ValidationErrors = unavailableItems,
                ValidatedAt = DateTime.UtcNow
            });

            Logger.LogWarning("Order {OrderId} inventory reservation failed: {UnavailableItems}", 
                orderId, string.Join(", ", unavailableItems));
            return;
        }

        // Reserve inventory
        RaiseEvent(new InventoryReservedLogEvent
        {
            OrderId = orderId,
            Reservations = reservations,
            ReservedAt = DateTime.UtcNow
        });
        await ConfirmEvents();

        // Publish inventory reservation success event
        await PublishAsync(new InventoryReservedEvent
        {
            OrderId = orderId,
            Reservations = reservations,
            ReservedAt = DateTime.UtcNow
        });

        // Publish order validation success event
        await PublishAsync(new OrderValidatedEvent
        {
            OrderId = orderId,
            IsValid = true,
            ValidationMessage = "Inventory validation passed",
            ValidationErrors = new List<string>(),
            ValidatedAt = DateTime.UtcNow
        });

        Logger.LogInformation("Order {OrderId} inventory reservation successful, reserved {Count} items", orderId, reservations.Count);
    }

    public async Task ReleaseReservationAsync(string orderId)
    {
        if (!State.Reservations.ContainsKey(orderId))
        {
            Logger.LogWarning("Attempting to release non-existent reservation: {OrderId}", orderId);
            return;
        }

        Logger.LogInformation("Releasing inventory reservation for order {OrderId}", orderId);

        RaiseEvent(new ReservationReleasedLogEvent
        {
            OrderId = orderId,
            ReleasedAt = DateTime.UtcNow
        });
        await ConfirmEvents();

        Logger.LogInformation("Inventory reservation for order {OrderId} has been released", orderId);
    }

    #region Event Handlers

    [EventHandler]
    public async Task HandleOrderSubmittedAsync(OrderSubmittedEvent @event)
    {
        Logger.LogInformation("Received order submission event, starting inventory validation: {OrderId}", @event.OrderId);

        await ReserveInventoryAsync(@event.OrderId, @event.Items);
    }

    [EventHandler]
    public async Task HandlePaymentProcessedAsync(PaymentProcessedEvent @event)
    {
        if (@event.IsSuccessful)
        {
            // Payment successful, convert reservation to actual deduction
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

                // Remove reservation
                RaiseEvent(new ReservationReleasedLogEvent
                {
                    OrderId = @event.OrderId,
                    ReleasedAt = DateTime.UtcNow
                });

                await ConfirmEvents();

                Logger.LogInformation("Order {OrderId} payment successful, inventory deducted", @event.OrderId);
            }
        }
        else
        {
            // Payment failed, release reservation
            await ReleaseReservationAsync(@event.OrderId);
            Logger.LogInformation("Order {OrderId} payment failed, inventory reservation released", @event.OrderId);
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
                
                // Update reserved quantity
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
                    // Reduce reserved quantity
                    foreach (var reservation in reservations)
                    {
                        var currentReserved = state.ReservedQuantities.GetValueOrDefault(reservation.ProductId, 0);
                        state.ReservedQuantities[reservation.ProductId] = Math.Max(0, currentReserved - reservation.ReservedQuantity);
                    }
                    
                    // Remove reservation record
                    state.Reservations.Remove(e.OrderId);
                }
                state.LastUpdated = e.ReleasedAt;
                break;
        }
    }
}

/// <summary>
/// Product entity
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