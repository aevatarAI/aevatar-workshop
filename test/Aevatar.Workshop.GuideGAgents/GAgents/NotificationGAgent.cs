using Microsoft.Extensions.Logging;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.Workshop.GuideGAgents.Events;

namespace Aevatar.Workshop.GuideGAgents.GAgents;

/// <summary>
/// Notification service GAgent interface
/// </summary>
public interface INotificationGAgent : IStateGAgent<NotificationState>
{
    Task SendNotificationAsync(NotificationRequest request);
    Task<List<NotificationRecord>> GetNotificationHistoryAsync(string customerId, int limit = 50);
    Task<NotificationStats> GetStatsAsync();
    Task UpdateNotificationPreferencesAsync(string customerId, NotificationPreferences preferences);
}

/// <summary>
/// Notification state
/// </summary>
[GenerateSerializer]
public class NotificationState : StateBase
{
    [Id(0)] public Dictionary<string, List<NotificationRecord>> CustomerNotifications { get; set; } = new();
    [Id(1)] public Dictionary<string, NotificationPreferences> CustomerPreferences { get; set; } = new();
    [Id(2)] public int TotalNotificationsSent { get; set; }
    [Id(3)] public int EmailsSent { get; set; }
    [Id(4)] public int SMSSent { get; set; }
    [Id(5)] public int PushNotificationsSent { get; set; }
    [Id(6)] public DateTime LastNotificationTime { get; set; }
}

/// <summary>
/// Notification state log event base class
/// </summary>
[GenerateSerializer]
public abstract class NotificationStateLogEvent : StateLogEventBase<NotificationStateLogEvent> { }

/// <summary>
/// Notification sent log event
/// </summary>
[GenerateSerializer]
public class NotificationSentLogEvent : NotificationStateLogEvent
{
    [Id(0)] public NotificationRecord Record { get; init; } = new();
}

/// <summary>
/// Notification preference update log event
/// </summary>
[GenerateSerializer]
public class PreferencesUpdatedLogEvent : NotificationStateLogEvent
{
    [Id(0)] public string CustomerId { get; init; } = string.Empty;
    [Id(1)] public NotificationPreferences Preferences { get; init; } = new();
    [Id(2)] public DateTime UpdatedAt { get; init; }
}

/// <summary>
/// Notification service GAgent implementation
/// </summary>
[GAgent("notification", "ecommerce")]
public class NotificationGAgent : GAgentBase<NotificationState, NotificationStateLogEvent>, INotificationGAgent
{
    public override Task<string> GetDescriptionAsync()
        => Task.FromResult("Intelligent agent that sends various types of notifications and manages user preferences");

    public async Task SendNotificationAsync(NotificationRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        if (string.IsNullOrEmpty(request.CustomerId))
            throw new ArgumentException("Customer ID cannot be empty", nameof(request.CustomerId));

        Logger.LogInformation("Starting to send notification: Customer {CustomerId}, Type {Type}, Subject {Subject}", 
            request.CustomerId, request.Type, request.Subject);

        // Check customer notification preferences
        var preferences = State.CustomerPreferences.GetValueOrDefault(request.CustomerId, new NotificationPreferences());
        
        if (!ShouldSendNotification(request.Type, preferences))
        {
            Logger.LogInformation("Skipping notification based on customer preferences: Customer {CustomerId}, Type {Type}", 
                request.CustomerId, request.Type);
            return;
        }

        var notificationId = Guid.NewGuid().ToString("N");
        var record = new NotificationRecord
        {
            Id = notificationId,
            CustomerId = request.CustomerId,
            Type = request.Type,
            Channel = DetermineChannel(request.Type, preferences),
            Subject = request.Subject,
            Message = request.Message,
            Data = request.Data,
            SentAt = DateTime.UtcNow,
            Status = NotificationStatus.Sent
        };

        // Simulate sending notification
        var success = await SimulateSendNotificationAsync(record);
        if (!success)
        {
            record.Status = NotificationStatus.Failed;
            record.ErrorMessage = "Notification sending failed, please try again later";
            Logger.LogWarning("Notification sending failed: {NotificationId}", notificationId);
        }

        // Update state
        RaiseEvent(new NotificationSentLogEvent { Record = record });
        await ConfirmEvents();

        Logger.LogInformation("Notification processing completed: {NotificationId}, Result {Status}", 
            notificationId, record.Status);
    }

    public Task<List<NotificationRecord>> GetNotificationHistoryAsync(string customerId, int limit = 50)
    {
        if (!State.CustomerNotifications.TryGetValue(customerId, out var notifications))
            return Task.FromResult(new List<NotificationRecord>());

        var history = notifications
            .OrderByDescending(n => n.SentAt)
            .Take(limit)
            .ToList();

        return Task.FromResult(history);
    }

    public Task<NotificationStats> GetStatsAsync()
    {
        var stats = new NotificationStats
        {
            TotalNotifications = State.TotalNotificationsSent,
            EmailsSent = State.EmailsSent,
            SMSSent = State.SMSSent,
            PushNotificationsSent = State.PushNotificationsSent,
            LastNotificationTime = State.LastNotificationTime,
            TotalCustomers = State.CustomerNotifications.Count
        };

        return Task.FromResult(stats);
    }

    public async Task UpdateNotificationPreferencesAsync(string customerId, NotificationPreferences preferences)
    {
        if (string.IsNullOrEmpty(customerId))
            throw new ArgumentException("Customer ID cannot be empty", nameof(customerId));

        if (preferences == null)
            throw new ArgumentNullException(nameof(preferences));

        Logger.LogInformation("Updating notification preferences for customer {CustomerId}", customerId);

        RaiseEvent(new PreferencesUpdatedLogEvent
        {
            CustomerId = customerId,
            Preferences = preferences,
            UpdatedAt = DateTime.UtcNow
        });
        await ConfirmEvents();

        Logger.LogInformation("Notification preferences for customer {CustomerId} have been updated", customerId);
    }

    #region Event Handlers

    [EventHandler]
    public async Task HandleOrderNotificationAsync(OrderNotificationEvent @event)
    {
        Logger.LogInformation("Received order notification event: {OrderId}, Type {Type}", @event.OrderId, @event.Type);

        var request = new NotificationRequest
        {
            CustomerId = @event.CustomerId,
            Type = @event.Type,
            Subject = @event.Subject,
            Message = @event.Message,
            Data = @event.Data.ToDictionary(kv => kv.Key, kv => kv.Value)
        };

        await SendNotificationAsync(request);
    }

    #endregion

    #region Private Methods

    private bool ShouldSendNotification(NotificationType type, NotificationPreferences preferences)
    {
        return type switch
        {
            NotificationType.OrderConfirmation => preferences.OrderConfirmations,
            NotificationType.PaymentConfirmation => preferences.PaymentNotifications,
            NotificationType.OrderShipped => preferences.ShippingUpdates,
            NotificationType.OrderDelivered => preferences.DeliveryNotifications,
            NotificationType.OrderCancelled => preferences.OrderUpdates,
            NotificationType.PaymentFailed => preferences.PaymentNotifications,
            NotificationType.InventoryUnavailable => preferences.OrderUpdates,
            _ => true
        };
    }

    private NotificationChannel DetermineChannel(NotificationType type, NotificationPreferences preferences)
    {
        // Determine sending channel based on notification type and user preferences
        return type switch
        {
            NotificationType.OrderConfirmation => preferences.PreferredChannel,
            NotificationType.PaymentConfirmation => preferences.PreferredChannel,
            NotificationType.OrderShipped => preferences.PreferredChannel,
            NotificationType.OrderDelivered => preferences.PreferredChannel,
            NotificationType.OrderCancelled => NotificationChannel.Email, // Important notifications use email
            NotificationType.PaymentFailed => NotificationChannel.Email, // Important notifications use email
            NotificationType.InventoryUnavailable => preferences.PreferredChannel,
            _ => NotificationChannel.Email
        };
    }

    private async Task<bool> SimulateSendNotificationAsync(NotificationRecord record)
    {
        // Simulate sending delay
        await Task.Delay(new Random().Next(50, 200));

        // Simulate sending success rate (95%)
        return new Random().NextDouble() > 0.05;
    }

    #endregion

    protected override void GAgentTransitionState(NotificationState state, StateLogEventBase<NotificationStateLogEvent> @event)
    {
        switch (@event)
        {
            case NotificationSentLogEvent e:
                // Add to customer notification history
                if (!state.CustomerNotifications.ContainsKey(e.Record.CustomerId))
                    state.CustomerNotifications[e.Record.CustomerId] = new List<NotificationRecord>();
                
                state.CustomerNotifications[e.Record.CustomerId].Add(e.Record);

                // Update statistics
                state.TotalNotificationsSent++;
                
                switch (e.Record.Channel)
                {
                    case NotificationChannel.Email:
                        state.EmailsSent++;
                        break;
                    case NotificationChannel.SMS:
                        state.SMSSent++;
                        break;
                    case NotificationChannel.Push:
                        state.PushNotificationsSent++;
                        break;
                }

                state.LastNotificationTime = e.Record.SentAt;
                break;

            case PreferencesUpdatedLogEvent e:
                state.CustomerPreferences[e.CustomerId] = e.Preferences;
                break;
        }
    }
}

/// <summary>
/// Notification request
/// </summary>
[GenerateSerializer]
public class NotificationRequest
{
    [Id(0)] public string CustomerId { get; set; } = string.Empty;
    [Id(1)] public NotificationType Type { get; set; }
    [Id(2)] public string Subject { get; set; } = string.Empty;
    [Id(3)] public string Message { get; set; } = string.Empty;
    [Id(4)] public Dictionary<string, object> Data { get; set; } = new();
}

/// <summary>
/// Notification record
/// </summary>
[GenerateSerializer]
public class NotificationRecord
{
    [Id(0)] public string Id { get; set; } = string.Empty;
    [Id(1)] public string CustomerId { get; set; } = string.Empty;
    [Id(2)] public NotificationType Type { get; set; }
    [Id(3)] public NotificationChannel Channel { get; set; }
    [Id(4)] public string Subject { get; set; } = string.Empty;
    [Id(5)] public string Message { get; set; } = string.Empty;
    [Id(6)] public Dictionary<string, object> Data { get; set; } = new();
    [Id(7)] public DateTime SentAt { get; set; }
    [Id(8)] public NotificationStatus Status { get; set; }
    [Id(9)] public string ErrorMessage { get; set; } = string.Empty;
}

/// <summary>
/// Notification preferences
/// </summary>
[GenerateSerializer]
public class NotificationPreferences
{
    [Id(0)] public NotificationChannel PreferredChannel { get; set; } = NotificationChannel.Email;
    [Id(1)] public bool OrderConfirmations { get; set; } = true;
    [Id(2)] public bool PaymentNotifications { get; set; } = true;
    [Id(3)] public bool ShippingUpdates { get; set; } = true;
    [Id(4)] public bool DeliveryNotifications { get; set; } = true;
    [Id(5)] public bool OrderUpdates { get; set; } = true;
    [Id(6)] public bool MarketingEmails { get; set; } = false;
    [Id(7)] public string EmailAddress { get; set; } = string.Empty;
    [Id(8)] public string PhoneNumber { get; set; } = string.Empty;
    [Id(9)] public string PushToken { get; set; } = string.Empty;
}

/// <summary>
/// Notification statistics
/// </summary>
[GenerateSerializer]
public class NotificationStats
{
    [Id(0)] public int TotalNotifications { get; set; }
    [Id(1)] public int EmailsSent { get; set; }
    [Id(2)] public int SMSSent { get; set; }
    [Id(3)] public int PushNotificationsSent { get; set; }
    [Id(4)] public DateTime LastNotificationTime { get; set; }
    [Id(5)] public int TotalCustomers { get; set; }
}

/// <summary>
/// Notification channel
/// </summary>
public enum NotificationChannel
{
    Email,
    SMS,
    Push,
    InApp
}

/// <summary>
/// Notification state
/// </summary>
public enum NotificationStatus
{
    Pending,
    Sent,
    Delivered,
    Failed,
    Read
}