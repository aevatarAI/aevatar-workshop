using Microsoft.Extensions.Logging;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.Workshop.GuideGAgents.Events;

namespace Aevatar.Workshop.GuideGAgents.GAgents;

/// <summary>
/// Order GAgent interface
/// </summary>
public interface IOrderGAgent : IStateGAgent<OrderState>
{
    Task<string> SubmitOrderAsync(SubmitOrderRequest request);
    Task<OrderStatus> GetOrderStatusAsync(string orderId);
    Task<Order?> GetOrderAsync(string orderId);
    Task<List<Order>> GetOrdersByCustomerAsync(string customerId);
    Task CancelOrderAsync(string orderId, string reason);
}

/// <summary>
/// Order state
/// </summary>
[GenerateSerializer]
public class OrderState : StateBase
{
    [Id(0)] public Dictionary<string, Order> Orders { get; set; } = new();
    [Id(1)] public Dictionary<string, OrderStatus> OrderStatuses { get; set; } = new();
    [Id(2)] public Dictionary<string, List<string>> CustomerOrders { get; set; } = new();
    [Id(3)] public int TotalOrders { get; set; }
    [Id(4)] public decimal TotalRevenue { get; set; }
    [Id(5)] public DateTime LastOrderTime { get; set; }
}

/// <summary>
/// Order state log event base class
/// </summary>
[GenerateSerializer]
public abstract class OrderStateLogEvent : StateLogEventBase<OrderStateLogEvent> { }

/// <summary>
/// Order created log event
/// </summary>
[GenerateSerializer]
public class OrderCreatedLogEvent : OrderStateLogEvent
{
    [Id(0)] public string OrderId { get; init; } = string.Empty;
    [Id(1)] public Order Order { get; init; } = new();
}

/// <summary>
/// Order status changed log event
/// </summary>
[GenerateSerializer]
public class OrderStatusChangedLogEvent : OrderStateLogEvent
{
    [Id(0)] public string OrderId { get; init; } = string.Empty;
    [Id(1)] public OrderStatus NewStatus { get; init; }
    [Id(2)] public OrderStatus PreviousStatus { get; init; }
    [Id(3)] public string Reason { get; init; } = string.Empty;
    [Id(4)] public DateTime ChangedAt { get; init; }
}

/// <summary>
/// Order cancelled log event
/// </summary>
[GenerateSerializer]
public class OrderCancelledLogEvent : OrderStateLogEvent
{
    [Id(0)] public string OrderId { get; init; } = string.Empty;
    [Id(1)] public string Reason { get; init; } = string.Empty;
    [Id(2)] public DateTime CancelledAt { get; init; }
}

/// <summary>
/// Order GAgent implementation
/// </summary>
[GAgent("order", "ecommerce")]
public class OrderGAgent : GAgentBase<OrderState, OrderStateLogEvent>, IOrderGAgent
{
    public override Task<string> GetDescriptionAsync()
        => Task.FromResult("Intelligent agent that manages customer orders and order lifecycle");

    public async Task<string> SubmitOrderAsync(SubmitOrderRequest request)
    {
        Logger.LogInformation("Starting order submission processing, customer: {CustomerId}", request.CustomerId);

        // Validate request
        if (string.IsNullOrEmpty(request.CustomerId))
            throw new ArgumentException("Customer ID cannot be empty", nameof(request.CustomerId));
        
        if (request.Items == null || request.Items.Count == 0)
            throw new ArgumentException("Order items cannot be empty", nameof(request.Items));

        var orderId = Guid.NewGuid().ToString("N");
        var totalAmount = request.Items.Sum(i => i.Price * i.Quantity);
        
        var order = new Order
        {
            Id = orderId,
            CustomerId = request.CustomerId,
            Items = request.Items.ToList(),
            TotalAmount = totalAmount,
            CreatedAt = DateTime.UtcNow,
            Status = OrderStatus.Submitted,
            CustomerEmail = request.CustomerEmail,
            ShippingAddress = request.ShippingAddress,
            BillingAddress = request.BillingAddress ?? request.ShippingAddress
        };

        // Update state
        RaiseEvent(new OrderCreatedLogEvent { OrderId = orderId, Order = order });
        RaiseEvent(new OrderStatusChangedLogEvent 
        { 
            OrderId = orderId, 
            NewStatus = OrderStatus.Submitted,
            PreviousStatus = OrderStatus.None,
            Reason = "Order submitted",
            ChangedAt = DateTime.UtcNow
        });
        await ConfirmEvents();

        // Publish order submitted event
        await PublishAsync(new OrderSubmittedEvent
        {
            OrderId = orderId,
            Items = request.Items.Select(i => new OrderItem
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                Price = i.Price,
                SKU = i.SKU
            }).ToList(),
            CustomerId = request.CustomerId,
            TotalAmount = totalAmount,
            SubmittedAt = DateTime.UtcNow
        });

        Logger.LogInformation("Order {OrderId} successfully submitted, customer: {CustomerId}, amount: {Amount:C}", 
            orderId, request.CustomerId, totalAmount);

        return orderId;
    }

    public Task<OrderStatus> GetOrderStatusAsync(string orderId)
    {
        return Task.FromResult(
            State.OrderStatuses.TryGetValue(orderId, out var status) 
                ? status 
                : OrderStatus.NotFound
        );
    }

    public Task<Order?> GetOrderAsync(string orderId)
    {
        return Task.FromResult(
            State.Orders.TryGetValue(orderId, out var order) ? order : null
        );
    }

    public Task<List<Order>> GetOrdersByCustomerAsync(string customerId)
    {
        if (!State.CustomerOrders.TryGetValue(customerId, out var orderIds))
            return Task.FromResult(new List<Order>());

        var orders = orderIds
            .Where(id => State.Orders.ContainsKey(id))
            .Select(id => State.Orders[id])
            .OrderByDescending(o => o.CreatedAt)
            .ToList();

        return Task.FromResult(orders);
    }

    public async Task CancelOrderAsync(string orderId, string reason)
    {
        if (!State.Orders.TryGetValue(orderId, out var order))
            throw new InvalidOperationException($"Order {orderId} does not exist");

        var currentStatus = State.OrderStatuses[orderId];
        
        // Check if order can be cancelled
        if (currentStatus == OrderStatus.Shipped || 
            currentStatus == OrderStatus.Delivered || 
            currentStatus == OrderStatus.Cancelled)
        {
            throw new InvalidOperationException($"Order status is {currentStatus}, cannot be cancelled");
        }

        // Update state
        RaiseEvent(new OrderCancelledLogEvent
        {
            OrderId = orderId,
            Reason = reason,
            CancelledAt = DateTime.UtcNow
        });
        
        RaiseEvent(new OrderStatusChangedLogEvent
        {
            OrderId = orderId,
            NewStatus = OrderStatus.Cancelled,
            PreviousStatus = currentStatus,
            Reason = reason,
            ChangedAt = DateTime.UtcNow
        });
        
        await ConfirmEvents();

        Logger.LogInformation("Order {OrderId} cancelled, reason: {Reason}", orderId, reason);
    }

    #region Event Handlers

    [EventHandler]
    public async Task HandleOrderValidatedAsync(OrderValidatedEvent @event)
    {
        if (!State.Orders.ContainsKey(@event.OrderId))
        {
            Logger.LogWarning("Received validation result for unknown order: {OrderId}", @event.OrderId);
            return;
        }

        var newStatus = @event.IsValid ? OrderStatus.Validated : OrderStatus.ValidationFailed;
        var currentStatus = State.OrderStatuses[@event.OrderId];
        
        RaiseEvent(new OrderStatusChangedLogEvent 
        { 
            OrderId = @event.OrderId, 
            NewStatus = newStatus,
            PreviousStatus = currentStatus,
            Reason = @event.ValidationMessage,
            ChangedAt = DateTime.UtcNow
        });
        await ConfirmEvents();

        Logger.LogInformation("Order {OrderId} validation result: {IsValid}, message: {Message}", 
            @event.OrderId, @event.IsValid, @event.ValidationMessage);

        // Send notification if validation failed
        if (!@event.IsValid)
        {
            await PublishAsync(new OrderNotificationEvent
            {
                OrderId = @event.OrderId,
                CustomerId = State.Orders[@event.OrderId].CustomerId,
                Type = NotificationType.InventoryUnavailable,
                Subject = "Order validation failed",
                Message = $"Your order validation failed: {@event.ValidationMessage}"
            });
        }
    }

    [EventHandler]
    public async Task HandlePaymentProcessedAsync(PaymentProcessedEvent @event)
    {
        if (!State.Orders.ContainsKey(@event.OrderId))
        {
            Logger.LogWarning("Received payment result for unknown order: {OrderId}", @event.OrderId);
            return;
        }

        var newStatus = @event.IsSuccessful ? OrderStatus.PaymentCompleted : OrderStatus.PaymentFailed;
        var currentStatus = State.OrderStatuses[@event.OrderId];
        
        RaiseEvent(new OrderStatusChangedLogEvent 
        { 
            OrderId = @event.OrderId, 
            NewStatus = newStatus,
            PreviousStatus = currentStatus,
            Reason = @event.IsSuccessful ? "Payment successful" : @event.ErrorMessage,
            ChangedAt = DateTime.UtcNow
        });
        await ConfirmEvents();

        Logger.LogInformation("Order {OrderId} payment result: {IsSuccessful}, amount: {Amount:C}", 
            @event.OrderId, @event.IsSuccessful, @event.Amount);

        // Send payment notification
        var notificationType = @event.IsSuccessful ? 
            NotificationType.PaymentConfirmation : 
            NotificationType.PaymentFailed;
            
        await PublishAsync(new OrderNotificationEvent
        {
            OrderId = @event.OrderId,
            CustomerId = State.Orders[@event.OrderId].CustomerId,
            Type = notificationType,
            Subject = @event.IsSuccessful ? "Payment successful" : "Payment failed",
            Message = @event.IsSuccessful ? 
                $"Your order payment was successful, amount: {@event.Amount:C}" : 
                $"Payment failed: {@event.ErrorMessage}"
        });
    }

    [EventHandler]
    public async Task HandleOrderShippedAsync(OrderShippedEvent @event)
    {
        if (!State.Orders.ContainsKey(@event.OrderId))
        {
            Logger.LogWarning("Received shipping notification for unknown order: {OrderId}", @event.OrderId);
            return;
        }

        var currentStatus = State.OrderStatuses[@event.OrderId];
        
        RaiseEvent(new OrderStatusChangedLogEvent 
        { 
            OrderId = @event.OrderId, 
            NewStatus = OrderStatus.Shipped,
            PreviousStatus = currentStatus,
            Reason = $"Shipped, tracking number: {@event.TrackingNumber}",
            ChangedAt = DateTime.UtcNow
        });
        await ConfirmEvents();

        Logger.LogInformation("Order {OrderId} shipped, tracking number: {TrackingNumber}", 
            @event.OrderId, @event.TrackingNumber);

        // Send shipping notification
        await PublishAsync(new OrderNotificationEvent
        {
            OrderId = @event.OrderId,
            CustomerId = State.Orders[@event.OrderId].CustomerId,
            Type = NotificationType.OrderShipped,
            Subject = "Order shipped",
            Message = $"Your order has been shipped, tracking number: {@event.TrackingNumber}, estimated delivery: {@event.EstimatedDelivery:yyyy-MM-dd}",
            Data = new Dictionary<string, object>
            {
                ["TrackingNumber"] = @event.TrackingNumber,
                ["ShippingCarrier"] = @event.ShippingCarrier,
                ["EstimatedDelivery"] = @event.EstimatedDelivery
            }
        });
    }

    #endregion

    protected override void GAgentTransitionState(OrderState state, StateLogEventBase<OrderStateLogEvent> @event)
    {
        switch (@event)
        {
            case OrderCreatedLogEvent e:
                state.Orders[e.OrderId] = e.Order;
                
                // Update customer order index
                if (!state.CustomerOrders.ContainsKey(e.Order.CustomerId))
                    state.CustomerOrders[e.Order.CustomerId] = new List<string>();
                state.CustomerOrders[e.Order.CustomerId].Add(e.OrderId);
                
                // Update statistics
                state.TotalOrders++;
                state.TotalRevenue += e.Order.TotalAmount;
                state.LastOrderTime = e.Order.CreatedAt;
                break;
                
            case OrderStatusChangedLogEvent e:
                state.OrderStatuses[e.OrderId] = e.NewStatus;
                break;
                
            case OrderCancelledLogEvent e:
                // Order status already updated in OrderStatusChangedLogEvent
                break;
        }
    }
}

/// <summary>
/// Submit order request
/// </summary>
[GenerateSerializer]
public class SubmitOrderRequest
{
    [Id(0)] public string CustomerId { get; set; } = string.Empty;
    [Id(1)] public string CustomerEmail { get; set; } = string.Empty;
    [Id(2)] public List<OrderItemRequest> Items { get; set; } = new();
    [Id(3)] public Address ShippingAddress { get; set; } = new();
    [Id(4)] public Address? BillingAddress { get; set; }
}

/// <summary>
/// Order item request
/// </summary>
[GenerateSerializer]
public class OrderItemRequest
{
    [Id(0)] public string ProductId { get; set; } = string.Empty;
    [Id(1)] public string ProductName { get; set; } = string.Empty;
    [Id(2)] public int Quantity { get; set; }
    [Id(3)] public decimal Price { get; set; }
    [Id(4)] public string SKU { get; set; } = string.Empty;
}

/// <summary>
/// Address information
/// </summary>
[GenerateSerializer]
public class Address
{
    [Id(0)] public string Street { get; set; } = string.Empty;
    [Id(1)] public string City { get; set; } = string.Empty;
    [Id(2)] public string State { get; set; } = string.Empty;
    [Id(3)] public string PostalCode { get; set; } = string.Empty;
    [Id(4)] public string Country { get; set; } = string.Empty;
}

/// <summary>
/// Order entity
/// </summary>
[GenerateSerializer]
public class Order
{
    [Id(0)] public string Id { get; set; } = string.Empty;
    [Id(1)] public string CustomerId { get; set; } = string.Empty;
    [Id(2)] public string CustomerEmail { get; set; } = string.Empty;
    [Id(3)] public List<OrderItemRequest> Items { get; set; } = new();
    [Id(4)] public decimal TotalAmount { get; set; }
    [Id(5)] public DateTime CreatedAt { get; set; }
    [Id(6)] public OrderStatus Status { get; set; }
    [Id(7)] public Address ShippingAddress { get; set; } = new();
    [Id(8)] public Address BillingAddress { get; set; } = new();
}

/// <summary>
/// Order status enumeration
/// </summary>
public enum OrderStatus
{
    None,
    Submitted,
    Validated,
    ValidationFailed,
    PaymentCompleted,
    PaymentFailed,
    InProduction,
    Shipped,
    Delivered,
    Cancelled,
    Refunded,
    NotFound
}