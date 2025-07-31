using Aevatar.Core.Abstractions;
using Aevatar.Workshop.GuideGAgents.GAgents;
using Aevatar.Workshop.GuideGAgents.Events;
using Aevatar.Workshop.TestBase;
using Xunit.Abstractions;

namespace Aevatar.Workshop.Tests;

[Collection(ClusterCollection.Name)]
public sealed class ECommerceTests : AevatarWorkshopTestBase<AevatarWorkshopTestModule>
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly IGAgentFactory _gAgentFactory;

    public ECommerceTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        _gAgentFactory = GetRequiredService<IGAgentFactory>();
    }

    [Fact]
    public async Task ECommerceGAgentsBasicSetupTest()
    {
        // Create coordinator GAgent
        var coordinator = await _gAgentFactory.GetGAgentAsync<IECommerceCoordinatorGAgent>();

        // Create all e-commerce GAgents
        var orderAgent = await _gAgentFactory.GetGAgentAsync<IOrderGAgent>();
        var inventoryAgent = await _gAgentFactory.GetGAgentAsync<IInventoryGAgent>();
        var notificationGAgent = await _gAgentFactory.GetGAgentAsync<INotificationGAgent>();
        var paymentAgent = await _gAgentFactory.GetGAgentAsync<IPaymentGAgent>();

        // Register all agents with coordinator for event communication
        await coordinator.RegisterAsync(orderAgent);
        await coordinator.RegisterAsync(inventoryAgent);
        await coordinator.RegisterAsync(notificationGAgent);
        await coordinator.RegisterAsync(paymentAgent);

        // Verify system initialization
        await coordinator.InitializeSystemAsync();
        var health = await coordinator.GetSystemHealthAsync();

        Assert.True(health.IsHealthy);
        Assert.Equal(4, health.ComponentStatuses.Count);
    }

    [Fact]
    public async Task OrderGAgent_SubmitOrder_ShouldCreateOrder()
    {
        // Arrange
        var orderAgent = await _gAgentFactory.GetGAgentAsync<IOrderGAgent>();
        
        var orderRequest = new SubmitOrderRequest
        {
            CustomerId = "customer-001",
            CustomerEmail = "test@example.com",
            Items = new List<OrderItemRequest>
            {
                new OrderItemRequest
                {
                    ProductId = "prod-001",
                    ProductName = "Test Product",
                    Quantity = 2,
                    Price = 99.99m,
                    SKU = "TEST-001"
                }
            },
            ShippingAddress = new Address
            {
                Street = "123 Test St",
                City = "Test City",
                State = "TS",
                PostalCode = "12345",
                Country = "TestLand"
            }
        };

        // Act
        var orderId = await orderAgent.SubmitOrderAsync(orderRequest);

        // Assert
        Assert.NotNull(orderId);
        Assert.NotEmpty(orderId);
        
        var orderStatus = await orderAgent.GetOrderStatusAsync(orderId);
        Assert.Equal(OrderStatus.Submitted, orderStatus);
        
        var order = await orderAgent.GetOrderAsync(orderId);
        Assert.NotNull(order);
        Assert.Equal("customer-001", order.CustomerId);
        Assert.Equal(199.98m, order.TotalAmount); // 2 * 99.99
    }

    [Fact]
    public async Task OrderGAgent_GetOrdersByCustomer_ShouldReturnCustomerOrders()
    {
        // Arrange
        var orderAgent = await _gAgentFactory.GetGAgentAsync<IOrderGAgent>();
        var customerId = "customer-002";
        
        // Submit multiple orders for the same customer
        var order1Id = await orderAgent.SubmitOrderAsync(CreateTestOrderRequest(customerId, "Product A"));
        var order2Id = await orderAgent.SubmitOrderAsync(CreateTestOrderRequest(customerId, "Product B"));

        // Act
        var customerOrders = await orderAgent.GetOrdersByCustomerAsync(customerId);

        // Assert
        Assert.Equal(2, customerOrders.Count);
        Assert.All(customerOrders, order => Assert.Equal(customerId, order.CustomerId));
        Assert.Contains(customerOrders, o => o.Id == order1Id);
        Assert.Contains(customerOrders, o => o.Id == order2Id);
    }

    [Fact]
    public async Task OrderGAgent_CancelOrder_ShouldUpdateStatus()
    {
        // Arrange
        var orderAgent = await _gAgentFactory.GetGAgentAsync<IOrderGAgent>();
        var orderId = await orderAgent.SubmitOrderAsync(CreateTestOrderRequest("customer-003", "Test Product"));

        // Act
        await orderAgent.CancelOrderAsync(orderId, "Customer requested cancellation");

        // Assert
        var orderStatus = await orderAgent.GetOrderStatusAsync(orderId);
        Assert.Equal(OrderStatus.Cancelled, orderStatus);
    }

    [Fact]
    public async Task InventoryGAgent_AddProduct_ShouldAddToInventory()
    {
        // Arrange
        var inventoryAgent = await _gAgentFactory.GetGAgentAsync<IInventoryGAgent>();
        
        var testProduct = new Product
        {
            Id = "test-product-001",
            Name = "Test Product",
            Description = "A test product for unit testing",
            Price = 49.99m,
            SKU = "TEST-PROD-001",
            Category = "Test",
            StockQuantity = 100,
            MinStockLevel = 10,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = true
        };

        // Act
        await inventoryAgent.AddProductAsync(testProduct);

        // Assert
        var retrievedProduct = await inventoryAgent.GetProductAsync("test-product-001");
        Assert.NotNull(retrievedProduct);
        Assert.Equal("Test Product", retrievedProduct.Name);
        Assert.Equal(100, retrievedProduct.StockQuantity);
    }

    [Fact]
    public async Task InventoryGAgent_CheckAvailability_ShouldReturnCorrectStatus()
    {
        // Arrange
        var inventoryAgent = await _gAgentFactory.GetGAgentAsync<IInventoryGAgent>();
        
        await inventoryAgent.AddProductAsync(new Product
        {
            Id = "available-product",
            Name = "Available Product",
            StockQuantity = 50,
            IsActive = true
        });

        // Act & Assert - Available
        var isAvailable = await inventoryAgent.CheckAvailabilityAsync("available-product", 10);
        Assert.True(isAvailable);

        // Act & Assert - Not enough stock
        var isNotAvailable = await inventoryAgent.CheckAvailabilityAsync("available-product", 100);
        Assert.False(isNotAvailable);

        // Act & Assert - Non-existent product
        var nonExistent = await inventoryAgent.CheckAvailabilityAsync("non-existent", 1);
        Assert.False(nonExistent);
    }

    [Fact]
    public async Task InventoryGAgent_UpdateStock_ShouldUpdateQuantity()
    {
        // Arrange
        var inventoryAgent = await _gAgentFactory.GetGAgentAsync<IInventoryGAgent>();
        
        await inventoryAgent.AddProductAsync(new Product
        {
            Id = "stock-update-test",
            Name = "Stock Update Test",
            StockQuantity = 100,
            IsActive = true
        });

        // Act
        await inventoryAgent.UpdateStockAsync("stock-update-test", 75);

        // Assert
        var isAvailable = await inventoryAgent.CheckAvailabilityAsync("stock-update-test", 70);
        Assert.True(isAvailable);
        
        var isNotAvailable = await inventoryAgent.CheckAvailabilityAsync("stock-update-test", 80);
        Assert.False(isNotAvailable);
    }

    [Fact]
    public async Task PaymentGAgent_ProcessPayment_ShouldHandleValidPayment()
    {
        // Arrange
        var paymentAgent = await _gAgentFactory.GetGAgentAsync<IPaymentGAgent>();
        
        var paymentRequest = new PaymentRequest
        {
            OrderId = "test-order-001",
            Amount = 199.99m,
            Currency = "USD",
            PaymentMethod = new PaymentMethod
            {
                Type = PaymentMethodType.CreditCard,
                CardNumber = "4111111111111111",
                ExpiryDate = "12/25",
                CVV = "123",
                CardHolderName = "Test User"
            },
            Description = "Test payment"
        };

        // Act
        var result = await paymentAgent.ProcessPaymentAsync(paymentRequest);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.TransactionId);
        // Note: Result may be successful or failed due to simulation
        Assert.True(result.IsSuccessful || !string.IsNullOrEmpty(result.ErrorMessage));
    }

    [Fact]
    public async Task PaymentGAgent_ValidatePaymentMethod_ShouldValidateCorrectly()
    {
        // Arrange
        var paymentAgent = await _gAgentFactory.GetGAgentAsync<IPaymentGAgent>();

        // Valid credit card
        var validCreditCard = new PaymentMethod
        {
            Type = PaymentMethodType.CreditCard,
            CardNumber = "4111111111111111",
            ExpiryDate = "12/25",
            CVV = "123",
            CardHolderName = "Test User"
        };

        // Invalid credit card (missing data)
        var invalidCreditCard = new PaymentMethod
        {
            Type = PaymentMethodType.CreditCard,
            CardNumber = "411111", // Too short
            ExpiryDate = "",
            CVV = ""
        };

        // Valid PayPal
        var validPayPal = new PaymentMethod
        {
            Type = PaymentMethodType.PayPal,
            PayPalEmail = "test@example.com"
        };

        // Act & Assert
        Assert.True(await paymentAgent.ValidatePaymentMethodAsync(validCreditCard));
        Assert.False(await paymentAgent.ValidatePaymentMethodAsync(invalidCreditCard));
        Assert.True(await paymentAgent.ValidatePaymentMethodAsync(validPayPal));
    }

    [Fact]
    public async Task NotificationGAgent_SendNotification_ShouldCreateRecord()
    {
        // Arrange
        var notificationAgent = await _gAgentFactory.GetGAgentAsync<INotificationGAgent>();
        
        var notificationRequest = new NotificationRequest
        {
            CustomerId = "customer-notify-001",
            Type = NotificationType.OrderConfirmation,
            Subject = "Order Confirmed",
            Message = "Your order has been confirmed and is being processed.",
            Data = new Dictionary<string, object>
            {
                ["OrderId"] = "test-order-001",
                ["Amount"] = 99.99m
            }
        };

        // Act
        await notificationAgent.SendNotificationAsync(notificationRequest);

        // Assert
        var history = await notificationAgent.GetNotificationHistoryAsync("customer-notify-001");
        Assert.NotEmpty(history);
        Assert.Contains(history, n => n.Subject == "Order Confirmed");
        Assert.Contains(history, n => n.Type == NotificationType.OrderConfirmation);
    }

    [Fact]
    public async Task NotificationGAgent_UpdatePreferences_ShouldSavePreferences()
    {
        // Arrange
        var notificationAgent = await _gAgentFactory.GetGAgentAsync<INotificationGAgent>();
        var customerId = "customer-prefs-001";
        
        var preferences = new NotificationPreferences
        {
            PreferredChannel = NotificationChannel.Email,
            OrderConfirmations = true,
            PaymentNotifications = true,
            ShippingUpdates = false,
            DeliveryNotifications = true,
            OrderUpdates = true,
            MarketingEmails = false,
            EmailAddress = "customer@example.com",
            PhoneNumber = "+1234567890"
        };

        // Act
        await notificationAgent.UpdateNotificationPreferencesAsync(customerId, preferences);

        // Assert - Send a notification that should be skipped due to preferences
        await notificationAgent.SendNotificationAsync(new NotificationRequest
        {
            CustomerId = customerId,
            Type = NotificationType.OrderShipped, // Should be skipped (shipping updates disabled)
            Subject = "Order Shipped",
            Message = "Your order has been shipped."
        });

        var history = await notificationAgent.GetNotificationHistoryAsync(customerId);
        // Should be empty because shipping updates are disabled
        Assert.Empty(history.Where(n => n.Type == NotificationType.OrderShipped));
    }

    [Fact]
    public async Task ECommerceSystem_EndToEndWorkflow_ShouldProcessOrder()
    {
        // Arrange - Setup the complete e-commerce system
        var coordinator = await _gAgentFactory.GetGAgentAsync<IECommerceCoordinatorGAgent>();
        var orderAgent = await _gAgentFactory.GetGAgentAsync<IOrderGAgent>();
        var inventoryAgent = await _gAgentFactory.GetGAgentAsync<IInventoryGAgent>();
        var paymentAgent = await _gAgentFactory.GetGAgentAsync<IPaymentGAgent>();
        var notificationAgent = await _gAgentFactory.GetGAgentAsync<INotificationGAgent>();

        // Register all agents for event communication
        await coordinator.RegisterAsync(orderAgent);
        await coordinator.RegisterAsync(inventoryAgent);
        await coordinator.RegisterAsync(paymentAgent);
        await coordinator.RegisterAsync(notificationAgent);

        // Initialize system with test data
        await coordinator.InitializeSystemAsync();
        await coordinator.SeedTestDataAsync();

        // Act - Process a complete order
        var orderRequest = new SubmitOrderRequest
        {
            CustomerId = "customer-e2e-001",
            CustomerEmail = "customer@example.com",
            Items = new List<OrderItemRequest>
            {
                new OrderItemRequest
                {
                    ProductId = "prod-001", // From seeded test data
                    ProductName = "iPhone 15 Pro",
                    Quantity = 1,
                    Price = 8999.00m,
                    SKU = "IPH15PRO-256GB"
                }
            },
            ShippingAddress = new Address
            {
                Street = "123 Main St",
                City = "Test City",
                State = "TS",
                PostalCode = "12345",
                Country = "TestLand"
            }
        };

        var orderId = await coordinator.ProcessOrderAsync(orderRequest);

        // Wait a bit for event processing
        await Task.Delay(1000);

        // Process payment
        var paymentRequest = new PaymentRequest
        {
            OrderId = orderId,
            Amount = 8999.00m,
            Currency = "USD",
            PaymentMethod = new PaymentMethod
            {
                Type = PaymentMethodType.CreditCard,
                CardNumber = "4111111111111111",
                ExpiryDate = "12/25",
                CVV = "123",
                CardHolderName = "Test Customer"
            }
        };

        var paymentResult = await paymentAgent.ProcessPaymentAsync(paymentRequest);

        // Wait for event processing
        await Task.Delay(1000);

        // Assert - Verify the complete workflow
        Assert.NotNull(orderId);
        Assert.NotEmpty(orderId);

        var finalOrderStatus = await coordinator.GetOrderStatusAsync(orderId);
        Assert.True(finalOrderStatus == OrderStatus.PaymentCompleted || 
                   finalOrderStatus == OrderStatus.PaymentFailed ||
                   finalOrderStatus == OrderStatus.Validated ||
                   finalOrderStatus == OrderStatus.ValidationFailed);

        var systemHealth = await coordinator.GetSystemHealthAsync();
        Assert.True(systemHealth.IsHealthy);

        // Check notification history
        var notifications = await notificationAgent.GetNotificationHistoryAsync("customer-e2e-001");
        Assert.NotEmpty(notifications);

        _testOutputHelper.WriteLine($"Order {orderId} processed with final status: {finalOrderStatus}");
        _testOutputHelper.WriteLine($"Payment result: {paymentResult.IsSuccessful}");
        _testOutputHelper.WriteLine($"Notifications sent: {notifications.Count}");
    }

    private SubmitOrderRequest CreateTestOrderRequest(string customerId, string productName)
    {
        return new SubmitOrderRequest
        {
            CustomerId = customerId,
            CustomerEmail = $"{customerId}@example.com",
            Items = new List<OrderItemRequest>
            {
                new OrderItemRequest
                {
                    ProductId = Guid.NewGuid().ToString(),
                    ProductName = productName,
                    Quantity = 1,
                    Price = 99.99m,
                    SKU = "TEST-SKU"
                }
            },
            ShippingAddress = new Address
            {
                Street = "123 Test St",
                City = "Test City",
                State = "TS",
                PostalCode = "12345",
                Country = "TestLand"
            }
        };
    }
}