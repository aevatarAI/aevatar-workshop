using Aevatar.Core.Abstractions;

namespace Aevatar.Workshop.GuideGAgents.Events;

/// <summary>
/// Order submission event
/// </summary>
[GenerateSerializer]
public class OrderSubmittedEvent : EventBase
{
    [Id(0)] public string OrderId { get; init; } = string.Empty;
    [Id(1)] public List<OrderItem> Items { get; init; } = new();
    [Id(2)] public string CustomerId { get; init; } = string.Empty;
    [Id(3)] public decimal TotalAmount { get; init; }
    [Id(4)] public DateTime SubmittedAt { get; init; }
}

/// <summary>
/// Order validation completed event
/// </summary>
[GenerateSerializer]
public class OrderValidatedEvent : EventBase
{
    [Id(0)] public string OrderId { get; init; } = string.Empty;
    [Id(1)] public bool IsValid { get; init; }
    [Id(2)] public string ValidationMessage { get; init; } = string.Empty;
    [Id(3)] public List<string> ValidationErrors { get; init; } = new();
    [Id(4)] public DateTime ValidatedAt { get; init; }
}

/// <summary>
/// Payment processing completed event
/// </summary>
[GenerateSerializer]
public class PaymentProcessedEvent : EventBase
{
    [Id(0)] public string OrderId { get; init; } = string.Empty;
    [Id(1)] public bool IsSuccessful { get; init; }
    [Id(2)] public decimal Amount { get; init; }
    [Id(3)] public string PaymentMethod { get; init; } = string.Empty;
    [Id(4)] public string TransactionId { get; init; } = string.Empty;
    [Id(5)] public string ErrorMessage { get; init; } = string.Empty;
    [Id(6)] public DateTime ProcessedAt { get; init; }
}

/// <summary>
/// Inventory reservation event
/// </summary>
[GenerateSerializer]
public class InventoryReservedEvent : EventBase
{
    [Id(0)] public string OrderId { get; init; } = string.Empty;
    [Id(1)] public List<InventoryReservation> Reservations { get; init; } = new();
    [Id(2)] public DateTime ReservedAt { get; init; }
}

/// <summary>
/// Inventory reservation failed event
/// </summary>
[GenerateSerializer]
public class InventoryReservationFailedEvent : EventBase
{
    [Id(0)] public string OrderId { get; init; } = string.Empty;
    [Id(1)] public List<string> UnavailableItems { get; init; } = new();
    [Id(2)] public string Reason { get; init; } = string.Empty;
    [Id(3)] public DateTime FailedAt { get; init; }
}

/// <summary>
/// Order shipped event
/// </summary>
[GenerateSerializer]
public class OrderShippedEvent : EventBase
{
    [Id(0)] public string OrderId { get; init; } = string.Empty;
    [Id(1)] public string TrackingNumber { get; init; } = string.Empty;
    [Id(2)] public string ShippingCarrier { get; init; } = string.Empty;
    [Id(3)] public DateTime ShippedAt { get; init; }
    [Id(4)] public DateTime EstimatedDelivery { get; init; }
}

/// <summary>
/// Order notification event
/// </summary>
[GenerateSerializer]
public class OrderNotificationEvent : EventBase
{
    [Id(0)] public string OrderId { get; init; } = string.Empty;
    [Id(1)] public string CustomerId { get; init; } = string.Empty;
    [Id(2)] public NotificationType Type { get; init; }
    [Id(3)] public string Subject { get; init; } = string.Empty;
    [Id(4)] public string Message { get; init; } = string.Empty;
    [Id(5)] public Dictionary<string, object> Data { get; init; } = new();
}

/// <summary>
/// Order item
/// </summary>
[GenerateSerializer]
public class OrderItem
{
    [Id(0)] public string ProductId { get; set; } = string.Empty;
    [Id(1)] public string ProductName { get; set; } = string.Empty;
    [Id(2)] public int Quantity { get; set; }
    [Id(3)] public decimal Price { get; set; }
    [Id(4)] public string SKU { get; set; } = string.Empty;
}

/// <summary>
/// Inventory reservation information
/// </summary>
[GenerateSerializer]
public class InventoryReservation
{
    [Id(0)] public string ProductId { get; set; } = string.Empty;
    [Id(1)] public string SKU { get; set; } = string.Empty;
    [Id(2)] public int ReservedQuantity { get; set; }
    [Id(3)] public DateTime ReservedAt { get; set; }
    [Id(4)] public DateTime ExpiresAt { get; set; }
}

/// <summary>
/// Notification type
/// </summary>
public enum NotificationType
{
    OrderConfirmation,
    PaymentConfirmation,
    OrderShipped,
    OrderDelivered,
    OrderCancelled,
    PaymentFailed,
    InventoryUnavailable
}