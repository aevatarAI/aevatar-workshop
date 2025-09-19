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
/// Payment state
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
/// Payment state log event base class
/// </summary>
[GenerateSerializer]
public abstract class PaymentStateLogEvent : StateLogEventBase<PaymentStateLogEvent> { }

/// <summary>
/// Payment processing log event
/// </summary>
[GenerateSerializer]
public class PaymentProcessedLogEvent : PaymentStateLogEvent
{
    [Id(0)] public PaymentTransaction Transaction { get; init; } = new();
}

/// <summary>
/// Refund processing log event
/// </summary>
[GenerateSerializer]
public class RefundProcessedLogEvent : PaymentStateLogEvent
{
    [Id(0)] public PaymentTransaction RefundTransaction { get; init; } = new();
    [Id(1)] public string OriginalTransactionId { get; init; } = string.Empty;
}

/// <summary>
/// Payment processing GAgent implementation
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
            throw new ArgumentException("Order ID cannot be empty", nameof(request.OrderId));

        if (request.Amount <= 0)
            throw new ArgumentException("Payment amount must be greater than 0", nameof(request.Amount));

        Logger.LogInformation("Starting payment processing: Order {OrderId}, Amount {Amount:C}", request.OrderId, request.Amount);

        // Validate payment method
        var isValidPaymentMethod = await ValidatePaymentMethodAsync(request.PaymentMethod);
        if (!isValidPaymentMethod)
        {
            var failureResult = new PaymentResult
            {
                IsSuccessful = false,
                TransactionId = string.Empty,
                ErrorMessage = "Invalid payment method",
                ProcessedAt = DateTime.UtcNow
            };

            Logger.LogWarning("Payment failed - Invalid payment method: Order {OrderId}", request.OrderId);
            return failureResult;
        }

        // Simulate payment processing
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
            ErrorMessage = isSuccessful ? string.Empty : "Payment processing failed, please check payment information",
            Currency = request.Currency,
            Description = $"Payment for order {request.OrderId}"
        };

        // Update state
        RaiseEvent(new PaymentProcessedLogEvent { Transaction = transaction });
        await ConfirmEvents();

        // Publish payment processing completed event
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

        Logger.LogInformation("Payment processing completed: Order {OrderId}, Result {Result}, Transaction ID {TransactionId}", 
            request.OrderId, isSuccessful ? "Success" : "Failure", transactionId);

        return result;
    }

    public async Task<PaymentResult> RefundPaymentAsync(string transactionId, decimal amount, string reason)
    {
        if (string.IsNullOrEmpty(transactionId))
            throw new ArgumentException("Transaction ID cannot be empty", nameof(transactionId));

        if (amount <= 0)
            throw new ArgumentException("Refund amount must be greater than 0", nameof(amount));

        Logger.LogInformation("Starting refund processing: Transaction {TransactionId}, Amount {Amount:C}", transactionId, amount);

        if (!State.Transactions.TryGetValue(transactionId, out var originalTransaction))
        {
            var notFoundResult = new PaymentResult
            {
                IsSuccessful = false,
                TransactionId = string.Empty,
                ErrorMessage = "Original transaction does not exist",
                ProcessedAt = DateTime.UtcNow
            };

            Logger.LogWarning("Refund failed - Original transaction does not exist: {TransactionId}", transactionId);
            return notFoundResult;
        }

        if (originalTransaction.Status != PaymentStatus.Completed)
        {
            var invalidStatusResult = new PaymentResult
            {
                IsSuccessful = false,
                TransactionId = string.Empty,
                ErrorMessage = "Original transaction status does not allow refund",
                ProcessedAt = DateTime.UtcNow
            };

            Logger.LogWarning("Refund failed - Original transaction status invalid: {TransactionId}, Status: {Status}", 
                transactionId, originalTransaction.Status);
            return invalidStatusResult;
        }

        if (amount > originalTransaction.Amount)
        {
            var exceedsAmountResult = new PaymentResult
            {
                IsSuccessful = false,
                TransactionId = string.Empty,
                ErrorMessage = "Refund amount exceeds original transaction amount",
                ProcessedAt = DateTime.UtcNow
            };

            Logger.LogWarning("Refund failed - Amount limit exceeded: {RefundAmount:C} > {OriginalAmount:C}", 
                amount, originalTransaction.Amount);
            return exceedsAmountResult;
        }

        // Simulate refund processing (usually higher success rate)
        var isSuccessful = _random.NextDouble() > 0.05; // 95% success rate
        var refundTransactionId = Guid.NewGuid().ToString("N");

        var refundTransaction = new PaymentTransaction
        {
            Id = refundTransactionId,
            OrderId = originalTransaction.OrderId,
            Amount = -amount, // Negative number indicates refund
            PaymentMethod = originalTransaction.PaymentMethod,
            Status = isSuccessful ? PaymentStatus.Refunded : PaymentStatus.Failed,
            ProcessedAt = DateTime.UtcNow,
            ErrorMessage = isSuccessful ? string.Empty : "Refund processing failed, please contact customer service",
            Currency = originalTransaction.Currency,
            Description = $"Refund for order {originalTransaction.OrderId} - {reason}"
        };

        // Update state
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

        Logger.LogInformation("Refund processing completed: Original transaction {OriginalTransactionId}, Refund transaction {RefundTransactionId}, Result {Result}", 
            transactionId, refundTransactionId, isSuccessful ? "Success" : "Failure");

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

        // Simple payment method validation logic
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

    #region Event Handlers

    [EventHandler]
    public async Task HandleOrderValidatedAsync(OrderValidatedEvent @event)
    {
        // Only start payment processing when order validation is successful
        if (!@event.IsValid)
        {
            Logger.LogInformation("Order {OrderId} validation failed, skipping payment processing", @event.OrderId);
            return;
        }

        Logger.LogInformation("Received order validation success event, preparing to process payment: {OrderId}", @event.OrderId);

        // Can automatically trigger payment processing here, or wait for external call
        // In actual systems, may need to wait for user to confirm payment information
    }

    #endregion

    #region Private Methods

    private async Task<bool> SimulatePaymentProcessingAsync(PaymentRequest request)
    {
        // Simulate payment processing delay
        await Task.Delay(_random.Next(100, 500));

        // Simulate payment success rate (85%)
        var successRate = 0.85;
        
        // Adjust success rate based on payment method
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
        // Simple credit card validation logic
        return !string.IsNullOrEmpty(paymentMethod.CardNumber) &&
               paymentMethod.CardNumber.Length >= 13 &&
               !string.IsNullOrEmpty(paymentMethod.ExpiryDate) &&
               !string.IsNullOrEmpty(paymentMethod.CVV);
    }

    private bool ValidateDebitCard(PaymentMethod paymentMethod)
    {
        // Debit card validation logic similar to credit card
        return ValidateCreditCard(paymentMethod);
    }

    private bool ValidatePayPal(PaymentMethod paymentMethod)
    {
        // PayPal validation logic
        return !string.IsNullOrEmpty(paymentMethod.PayPalEmail) &&
               paymentMethod.PayPalEmail.Contains("@");
    }

    private bool ValidateBankTransfer(PaymentMethod paymentMethod)
    {
        // Bank transfer validation logic
        return !string.IsNullOrEmpty(paymentMethod.BankAccount) &&
               !string.IsNullOrEmpty(paymentMethod.RoutingNumber);
    }

    private bool ValidateDigitalWallet(PaymentMethod paymentMethod)
    {
        // Digital wallet validation logic
        return !string.IsNullOrEmpty(paymentMethod.WalletId);
    }

    #endregion

    protected override void GAgentTransitionState(PaymentState state, StateLogEventBase<PaymentStateLogEvent> @event)
    {
        switch (@event)
        {
            case PaymentProcessedLogEvent e:
                state.Transactions[e.Transaction.Id] = e.Transaction;
                
                // Update order transaction index
                if (!state.OrderTransactions.ContainsKey(e.Transaction.OrderId))
                    state.OrderTransactions[e.Transaction.OrderId] = new List<string>();
                state.OrderTransactions[e.Transaction.OrderId].Add(e.Transaction.Id);
                
                // Update statistics
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
                
                // Update order transaction index
                if (!state.OrderTransactions.ContainsKey(e.RefundTransaction.OrderId))
                    state.OrderTransactions[e.RefundTransaction.OrderId] = new List<string>();
                state.OrderTransactions[e.RefundTransaction.OrderId].Add(e.RefundTransaction.Id);
                
                // Update statistics
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
/// Payment request
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
/// Payment method
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
/// Payment result
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
/// Payment transaction
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
/// Payment method type
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
/// Payment state
/// </summary>
public enum PaymentStatus
{
    Pending,
    Completed,
    Failed,
    Refunded,
    Cancelled
}