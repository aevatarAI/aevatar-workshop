using Microsoft.Extensions.Logging;
using Aevatar.Core;
using Aevatar.Core.Abstractions;
using Aevatar.Workshop.GuideGAgents.Events;

namespace Aevatar.Workshop.GuideGAgents.GAgents;

/// <summary>
/// Payment processing GAgent interface
/// </summary>
public interface IPaymentGAgent : IStateGAgent<PaymentState>
{
    Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request);
    Task<PaymentResult> RefundPaymentAsync(string transactionId, decimal amount, string reason);
    Task<PaymentTransaction?> GetTransactionAsync(string transactionId);
    Task<List<PaymentTransaction>> GetTransactionsByOrderAsync(string orderId);
    Task<bool> ValidatePaymentMethodAsync(PaymentMethod paymentMethod);
}

/// <summary>
/// 支付状态
/// </summary>
[GenerateSerializer]
public class PaymentState : StateBase
{
    [Id(0)] public Dictionary<string, PaymentTransaction> Transactions { get; set; } = new();
    [Id(1)] public Dictionary<string, List<string>> OrderTransactions { get; set; } = new();
    [Id(2)] public decimal TotalProcessed { get; set; }
    [Id(3)] public decimal TotalRefunded { get; set; }
    [Id(4)] public int SuccessfulTransactions { get; set; }
    [Id(5)] public int FailedTransactions { get; set; }
    [Id(6)] public DateTime LastTransactionTime { get; set; }
}

/// <summary>
/// 支付状态日志事件基类
/// </summary>
[GenerateSerializer]
public abstract class PaymentStateLogEvent : StateLogEventBase<PaymentStateLogEvent> { }

/// <summary>
/// 支付处理日志事件
/// </summary>
[GenerateSerializer]
public class PaymentProcessedLogEvent : PaymentStateLogEvent
{
    [Id(0)] public PaymentTransaction Transaction { get; init; } = new();
}

/// <summary>
/// 退款处理日志事件
/// </summary>
[GenerateSerializer]
public class RefundProcessedLogEvent : PaymentStateLogEvent
{
    [Id(0)] public PaymentTransaction RefundTransaction { get; init; } = new();
    [Id(1)] public string OriginalTransactionId { get; init; } = string.Empty;
}

/// <summary>
/// 支付处理 GAgent 实现
/// </summary>
[GAgent("payment", "ecommerce")]
public class PaymentGAgent : GAgentBase<PaymentState, PaymentStateLogEvent>, IPaymentGAgent
{
    private readonly Random _random = new();

    public override Task<string> GetDescriptionAsync()
        => Task.FromResult("Intelligent agent that processes payments and refund operations");

    public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        if (string.IsNullOrEmpty(request.OrderId))
            throw new ArgumentException("订单ID不能为空", nameof(request.OrderId));

        if (request.Amount <= 0)
            throw new ArgumentException("支付金额必须大于0", nameof(request.Amount));

        Logger.LogInformation("开始处理支付：订单 {OrderId}，金额 {Amount:C}", request.OrderId, request.Amount);

        // 验证支付方式
        var isValidPaymentMethod = await ValidatePaymentMethodAsync(request.PaymentMethod);
        if (!isValidPaymentMethod)
        {
            var failureResult = new PaymentResult
            {
                IsSuccessful = false,
                TransactionId = string.Empty,
                ErrorMessage = "无效的支付方式",
                ProcessedAt = DateTime.UtcNow
            };

            Logger.LogWarning("支付失败 - 无效的支付方式：订单 {OrderId}", request.OrderId);
            return failureResult;
        }

        // 模拟支付处理
        var isSuccessful = await SimulatePaymentProcessingAsync(request);
        var transactionId = Guid.NewGuid().ToString("N");

        var transaction = new PaymentTransaction
        {
            Id = transactionId,
            OrderId = request.OrderId,
            Amount = request.Amount,
            PaymentMethod = request.PaymentMethod,
            Status = isSuccessful ? PaymentStatus.Completed : PaymentStatus.Failed,
            ProcessedAt = DateTime.UtcNow,
            ErrorMessage = isSuccessful ? string.Empty : "支付处理失败，请检查支付信息",
            Currency = request.Currency,
            Description = $"订单 {request.OrderId} 的支付"
        };

        // 更新状态
        RaiseEvent(new PaymentProcessedLogEvent { Transaction = transaction });
        await ConfirmEvents();

        // 发布支付处理完成事件
        await PublishAsync(new PaymentProcessedEvent
        {
            OrderId = request.OrderId,
            IsSuccessful = isSuccessful,
            Amount = request.Amount,
            PaymentMethod = request.PaymentMethod.Type.ToString(),
            TransactionId = transactionId,
            ErrorMessage = transaction.ErrorMessage,
            ProcessedAt = DateTime.UtcNow
        });

        var result = new PaymentResult
        {
            IsSuccessful = isSuccessful,
            TransactionId = transactionId,
            ErrorMessage = transaction.ErrorMessage,
            ProcessedAt = DateTime.UtcNow
        };

        Logger.LogInformation("支付处理完成：订单 {OrderId}，结果 {Result}，交易ID {TransactionId}", 
            request.OrderId, isSuccessful ? "成功" : "失败", transactionId);

        return result;
    }

    public async Task<PaymentResult> RefundPaymentAsync(string transactionId, decimal amount, string reason)
    {
        if (string.IsNullOrEmpty(transactionId))
            throw new ArgumentException("交易ID不能为空", nameof(transactionId));

        if (amount <= 0)
            throw new ArgumentException("退款金额必须大于0", nameof(amount));

        Logger.LogInformation("开始处理退款：交易 {TransactionId}，金额 {Amount:C}", transactionId, amount);

        if (!State.Transactions.TryGetValue(transactionId, out var originalTransaction))
        {
            var notFoundResult = new PaymentResult
            {
                IsSuccessful = false,
                TransactionId = string.Empty,
                ErrorMessage = "原始交易不存在",
                ProcessedAt = DateTime.UtcNow
            };

            Logger.LogWarning("退款失败 - 原始交易不存在：{TransactionId}", transactionId);
            return notFoundResult;
        }

        if (originalTransaction.Status != PaymentStatus.Completed)
        {
            var invalidStatusResult = new PaymentResult
            {
                IsSuccessful = false,
                TransactionId = string.Empty,
                ErrorMessage = "原始交易状态不允许退款",
                ProcessedAt = DateTime.UtcNow
            };

            Logger.LogWarning("退款失败 - 原始交易状态无效：{TransactionId}，状态：{Status}", 
                transactionId, originalTransaction.Status);
            return invalidStatusResult;
        }

        if (amount > originalTransaction.Amount)
        {
            var exceedsAmountResult = new PaymentResult
            {
                IsSuccessful = false,
                TransactionId = string.Empty,
                ErrorMessage = "退款金额超过原始交易金额",
                ProcessedAt = DateTime.UtcNow
            };

            Logger.LogWarning("退款失败 - 金额超限：{RefundAmount:C} > {OriginalAmount:C}", 
                amount, originalTransaction.Amount);
            return exceedsAmountResult;
        }

        // 模拟退款处理（通常成功率较高）
        var isSuccessful = _random.NextDouble() > 0.05; // 95% 成功率
        var refundTransactionId = Guid.NewGuid().ToString("N");

        var refundTransaction = new PaymentTransaction
        {
            Id = refundTransactionId,
            OrderId = originalTransaction.OrderId,
            Amount = -amount, // 负数表示退款
            PaymentMethod = originalTransaction.PaymentMethod,
            Status = isSuccessful ? PaymentStatus.Refunded : PaymentStatus.Failed,
            ProcessedAt = DateTime.UtcNow,
            ErrorMessage = isSuccessful ? string.Empty : "退款处理失败，请联系客服",
            Currency = originalTransaction.Currency,
            Description = $"订单 {originalTransaction.OrderId} 的退款 - {reason}"
        };

        // 更新状态
        RaiseEvent(new RefundProcessedLogEvent 
        { 
            RefundTransaction = refundTransaction,
            OriginalTransactionId = transactionId
        });
        await ConfirmEvents();

        var result = new PaymentResult
        {
            IsSuccessful = isSuccessful,
            TransactionId = refundTransactionId,
            ErrorMessage = refundTransaction.ErrorMessage,
            ProcessedAt = DateTime.UtcNow
        };

        Logger.LogInformation("退款处理完成：原交易 {OriginalTransactionId}，退款交易 {RefundTransactionId}，结果 {Result}", 
            transactionId, refundTransactionId, isSuccessful ? "成功" : "失败");

        return result;
    }

    public Task<PaymentTransaction?> GetTransactionAsync(string transactionId)
    {
        return Task.FromResult(
            State.Transactions.TryGetValue(transactionId, out var transaction) ? transaction : null
        );
    }

    public Task<List<PaymentTransaction>> GetTransactionsByOrderAsync(string orderId)
    {
        if (!State.OrderTransactions.TryGetValue(orderId, out var transactionIds))
            return Task.FromResult(new List<PaymentTransaction>());

        var transactions = transactionIds
            .Where(id => State.Transactions.ContainsKey(id))
            .Select(id => State.Transactions[id])
            .OrderByDescending(t => t.ProcessedAt)
            .ToList();

        return Task.FromResult(transactions);
    }

    public Task<bool> ValidatePaymentMethodAsync(PaymentMethod paymentMethod)
    {
        if (paymentMethod == null)
            return Task.FromResult(false);

        // 简单的支付方式验证逻辑
        switch (paymentMethod.Type)
        {
            case PaymentMethodType.CreditCard:
                return Task.FromResult(ValidateCreditCard(paymentMethod));
            case PaymentMethodType.DebitCard:
                return Task.FromResult(ValidateDebitCard(paymentMethod));
            case PaymentMethodType.PayPal:
                return Task.FromResult(ValidatePayPal(paymentMethod));
            case PaymentMethodType.BankTransfer:
                return Task.FromResult(ValidateBankTransfer(paymentMethod));
            case PaymentMethodType.DigitalWallet:
                return Task.FromResult(ValidateDigitalWallet(paymentMethod));
            default:
                return Task.FromResult(false);
        }
    }

    #region 事件处理器

    [EventHandler]
    public async Task HandleOrderValidatedAsync(OrderValidatedEvent @event)
    {
        // 只有当订单验证成功时才开始支付处理
        if (!@event.IsValid)
        {
            Logger.LogInformation("订单 {OrderId} 验证失败，跳过支付处理", @event.OrderId);
            return;
        }

        Logger.LogInformation("收到订单验证成功事件，准备处理支付：{OrderId}", @event.OrderId);

        // 这里可以自动触发支付处理，或者等待外部调用
        // 在实际系统中，可能需要等待用户确认支付信息
    }

    #endregion

    #region 私有方法

    private async Task<bool> SimulatePaymentProcessingAsync(PaymentRequest request)
    {
        // 模拟支付处理延迟
        await Task.Delay(_random.Next(100, 500));

        // 模拟支付成功率（85%）
        var successRate = 0.85;
        
        // 基于支付方式调整成功率
        switch (request.PaymentMethod.Type)
        {
            case PaymentMethodType.CreditCard:
                successRate = 0.88;
                break;
            case PaymentMethodType.DebitCard:
                successRate = 0.85;
                break;
            case PaymentMethodType.PayPal:
                successRate = 0.92;
                break;
            case PaymentMethodType.BankTransfer:
                successRate = 0.95;
                break;
            case PaymentMethodType.DigitalWallet:
                successRate = 0.90;
                break;
        }

        return _random.NextDouble() < successRate;
    }

    private bool ValidateCreditCard(PaymentMethod paymentMethod)
    {
        // 简单的信用卡验证逻辑
        return !string.IsNullOrEmpty(paymentMethod.CardNumber) &&
               paymentMethod.CardNumber.Length >= 13 &&
               !string.IsNullOrEmpty(paymentMethod.ExpiryDate) &&
               !string.IsNullOrEmpty(paymentMethod.CVV);
    }

    private bool ValidateDebitCard(PaymentMethod paymentMethod)
    {
        // 借记卡验证逻辑与信用卡类似
        return ValidateCreditCard(paymentMethod);
    }

    private bool ValidatePayPal(PaymentMethod paymentMethod)
    {
        // PayPal 验证逻辑
        return !string.IsNullOrEmpty(paymentMethod.PayPalEmail) &&
               paymentMethod.PayPalEmail.Contains("@");
    }

    private bool ValidateBankTransfer(PaymentMethod paymentMethod)
    {
        // 银行转账验证逻辑
        return !string.IsNullOrEmpty(paymentMethod.BankAccount) &&
               !string.IsNullOrEmpty(paymentMethod.RoutingNumber);
    }

    private bool ValidateDigitalWallet(PaymentMethod paymentMethod)
    {
        // 数字钱包验证逻辑
        return !string.IsNullOrEmpty(paymentMethod.WalletId);
    }

    #endregion

    protected override void GAgentTransitionState(PaymentState state, StateLogEventBase<PaymentStateLogEvent> @event)
    {
        switch (@event)
        {
            case PaymentProcessedLogEvent e:
                state.Transactions[e.Transaction.Id] = e.Transaction;
                
                // 更新订单交易索引
                if (!state.OrderTransactions.ContainsKey(e.Transaction.OrderId))
                    state.OrderTransactions[e.Transaction.OrderId] = new List<string>();
                state.OrderTransactions[e.Transaction.OrderId].Add(e.Transaction.Id);
                
                // 更新统计信息
                if (e.Transaction.Status == PaymentStatus.Completed)
                {
                    state.TotalProcessed += e.Transaction.Amount;
                    state.SuccessfulTransactions++;
                }
                else
                {
                    state.FailedTransactions++;
                }
                
                state.LastTransactionTime = e.Transaction.ProcessedAt;
                break;

            case RefundProcessedLogEvent e:
                state.Transactions[e.RefundTransaction.Id] = e.RefundTransaction;
                
                // 更新订单交易索引
                if (!state.OrderTransactions.ContainsKey(e.RefundTransaction.OrderId))
                    state.OrderTransactions[e.RefundTransaction.OrderId] = new List<string>();
                state.OrderTransactions[e.RefundTransaction.OrderId].Add(e.RefundTransaction.Id);
                
                // 更新统计信息
                if (e.RefundTransaction.Status == PaymentStatus.Refunded)
                {
                    state.TotalRefunded += Math.Abs(e.RefundTransaction.Amount);
                }
                
                state.LastTransactionTime = e.RefundTransaction.ProcessedAt;
                break;
        }
    }
}

/// <summary>
/// 支付请求
/// </summary>
[GenerateSerializer]
public class PaymentRequest
{
    [Id(0)] public string OrderId { get; set; } = string.Empty;
    [Id(1)] public decimal Amount { get; set; }
    [Id(2)] public string Currency { get; set; } = "CNY";
    [Id(3)] public PaymentMethod PaymentMethod { get; set; } = new();
    [Id(4)] public string Description { get; set; } = string.Empty;
}

/// <summary>
/// 支付方式
/// </summary>
[GenerateSerializer]
public class PaymentMethod
{
    [Id(0)] public PaymentMethodType Type { get; set; }
    [Id(1)] public string CardNumber { get; set; } = string.Empty;
    [Id(2)] public string ExpiryDate { get; set; } = string.Empty;
    [Id(3)] public string CVV { get; set; } = string.Empty;
    [Id(4)] public string CardHolderName { get; set; } = string.Empty;
    [Id(5)] public string PayPalEmail { get; set; } = string.Empty;
    [Id(6)] public string BankAccount { get; set; } = string.Empty;
    [Id(7)] public string RoutingNumber { get; set; } = string.Empty;
    [Id(8)] public string WalletId { get; set; } = string.Empty;
}

/// <summary>
/// 支付结果
/// </summary>
[GenerateSerializer]
public class PaymentResult
{
    [Id(0)] public bool IsSuccessful { get; set; }
    [Id(1)] public string TransactionId { get; set; } = string.Empty;
    [Id(2)] public string ErrorMessage { get; set; } = string.Empty;
    [Id(3)] public DateTime ProcessedAt { get; set; }
}

/// <summary>
/// 支付交易
/// </summary>
[GenerateSerializer]
public class PaymentTransaction
{
    [Id(0)] public string Id { get; set; } = string.Empty;
    [Id(1)] public string OrderId { get; set; } = string.Empty;
    [Id(2)] public decimal Amount { get; set; }
    [Id(3)] public string Currency { get; set; } = "CNY";
    [Id(4)] public PaymentMethod PaymentMethod { get; set; } = new();
    [Id(5)] public PaymentStatus Status { get; set; }
    [Id(6)] public DateTime ProcessedAt { get; set; }
    [Id(7)] public string ErrorMessage { get; set; } = string.Empty;
    [Id(8)] public string Description { get; set; } = string.Empty;
}

/// <summary>
/// 支付方式类型
/// </summary>
public enum PaymentMethodType
{
    CreditCard,
    DebitCard,
    PayPal,
    BankTransfer,
    DigitalWallet
}

/// <summary>
/// 支付状态
/// </summary>
public enum PaymentStatus
{
    Pending,
    Completed,
    Failed,
    Refunded,
    Cancelled
}