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
/// 通知状态
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
/// 通知状态日志事件基类
/// </summary>
[GenerateSerializer]
public abstract class NotificationStateLogEvent : StateLogEventBase<NotificationStateLogEvent> { }

/// <summary>
/// 通知发送日志事件
/// </summary>
[GenerateSerializer]
public class NotificationSentLogEvent : NotificationStateLogEvent
{
    [Id(0)] public NotificationRecord Record { get; init; } = new();
}

/// <summary>
/// 通知偏好更新日志事件
/// </summary>
[GenerateSerializer]
public class PreferencesUpdatedLogEvent : NotificationStateLogEvent
{
    [Id(0)] public string CustomerId { get; init; } = string.Empty;
    [Id(1)] public NotificationPreferences Preferences { get; init; } = new();
    [Id(2)] public DateTime UpdatedAt { get; init; }
}

/// <summary>
/// 通知服务 GAgent 实现
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
            throw new ArgumentException("客户ID不能为空", nameof(request.CustomerId));

        Logger.LogInformation("开始发送通知：客户 {CustomerId}，类型 {Type}，主题 {Subject}", 
            request.CustomerId, request.Type, request.Subject);

        // 检查客户通知偏好
        var preferences = State.CustomerPreferences.GetValueOrDefault(request.CustomerId, new NotificationPreferences());
        
        if (!ShouldSendNotification(request.Type, preferences))
        {
            Logger.LogInformation("根据客户偏好跳过通知：客户 {CustomerId}，类型 {Type}", 
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

        // 模拟发送通知
        var success = await SimulateSendNotificationAsync(record);
        if (!success)
        {
            record.Status = NotificationStatus.Failed;
            record.ErrorMessage = "通知发送失败，请稍后重试";
            Logger.LogWarning("通知发送失败：{NotificationId}", notificationId);
        }

        // 更新状态
        RaiseEvent(new NotificationSentLogEvent { Record = record });
        await ConfirmEvents();

        Logger.LogInformation("通知处理完成：{NotificationId}，结果 {Status}", 
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
            throw new ArgumentException("客户ID不能为空", nameof(customerId));

        if (preferences == null)
            throw new ArgumentNullException(nameof(preferences));

        Logger.LogInformation("更新客户 {CustomerId} 的通知偏好", customerId);

        RaiseEvent(new PreferencesUpdatedLogEvent
        {
            CustomerId = customerId,
            Preferences = preferences,
            UpdatedAt = DateTime.UtcNow
        });
        await ConfirmEvents();

        Logger.LogInformation("客户 {CustomerId} 的通知偏好已更新", customerId);
    }

    #region 事件处理器

    [EventHandler]
    public async Task HandleOrderNotificationAsync(OrderNotificationEvent @event)
    {
        Logger.LogInformation("收到订单通知事件：{OrderId}，类型 {Type}", @event.OrderId, @event.Type);

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

    #region 私有方法

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
        // 基于通知类型和用户偏好确定发送渠道
        return type switch
        {
            NotificationType.OrderConfirmation => preferences.PreferredChannel,
            NotificationType.PaymentConfirmation => preferences.PreferredChannel,
            NotificationType.OrderShipped => preferences.PreferredChannel,
            NotificationType.OrderDelivered => preferences.PreferredChannel,
            NotificationType.OrderCancelled => NotificationChannel.Email, // 重要通知使用邮件
            NotificationType.PaymentFailed => NotificationChannel.Email, // 重要通知使用邮件
            NotificationType.InventoryUnavailable => preferences.PreferredChannel,
            _ => NotificationChannel.Email
        };
    }

    private async Task<bool> SimulateSendNotificationAsync(NotificationRecord record)
    {
        // 模拟发送延迟
        await Task.Delay(new Random().Next(50, 200));

        // 模拟发送成功率（95%）
        return new Random().NextDouble() > 0.05;
    }

    #endregion

    protected override void GAgentTransitionState(NotificationState state, StateLogEventBase<NotificationStateLogEvent> @event)
    {
        switch (@event)
        {
            case NotificationSentLogEvent e:
                // 添加到客户通知历史
                if (!state.CustomerNotifications.ContainsKey(e.Record.CustomerId))
                    state.CustomerNotifications[e.Record.CustomerId] = new List<NotificationRecord>();
                
                state.CustomerNotifications[e.Record.CustomerId].Add(e.Record);

                // 更新统计信息
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
/// 通知请求
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
/// 通知记录
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
/// 通知偏好设置
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
/// 通知统计信息
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
/// 通知渠道
/// </summary>
public enum NotificationChannel
{
    Email,
    SMS,
    Push,
    InApp
}

/// <summary>
/// 通知状态
/// </summary>
public enum NotificationStatus
{
    Pending,
    Sent,
    Delivered,
    Failed,
    Read
}