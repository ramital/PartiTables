using Microsoft.Extensions.DependencyInjection;
using PartiTables;
using PartiSample.Models;

namespace PartiSample.Demos;

/// <summary>
/// DEMO 3: E-commerce Order Management
/// 
/// Shows: Same pattern as Demo 2, different business domain
/// Best for: Showing library flexibility across industries
/// 
/// Key Concepts:
/// - Reusable patterns across domains
/// - Managing customer profiles with orders
/// - Multiple collection types in one partition
/// - Efficient collection-specific queries
/// </summary>
public static class EcommerceDemo
{
    public static async Task RunAsync(IServiceProvider sp)
    {
        Console.WriteLine("=== DEMO 3: E-commerce Order Management ===");
        Console.WriteLine("Customer profiles with orders, addresses, and payments\n");

        var repo = sp.GetRequiredService<PartitionRepository<CustomerOrders>>();
        var customerId = "cust-john-doe-123";

        // ???????????????????????????????????????????????????????????
        // CREATE: Build customer profile
        // ???????????????????????????????????????????????????????????
        Console.WriteLine("? Creating customer profile...");
        
        var customer = new CustomerOrders
        {
            CustomerId = customerId,
            CustomerEmail = "john.doe@email.com"
        };

        // Add shipping address
        customer.Addresses.Add(new ShippingAddress
        {
            AddressType = "Shipping",
            Street = "123 Main Street",
            City = "Seattle",
            State = "WA",
            ZipCode = "98101",
            Country = "USA",
            IsDefault = true
        });

        // Add payment method
        customer.PaymentMethods.Add(new PaymentMethod
        {
            Type = "CreditCard",
            CardBrand = "Visa",
            LastFourDigits = "4242",
            ExpiryMonth = "12",
            ExpiryYear = "2025",
            IsDefault = true
        });

        // Add preferences
        customer.Preferences.Add(new CustomerPreference
        {
            Category = "Notifications",
            Key = "newsletter",
            Value = "enabled"
        });

        await repo.SaveAsync(customer);
        Console.WriteLine("  ? Customer profile created\n");

        // ???????????????????????????????????????????????????????????
        // ADD ORDERS: Customer purchase history
        // ???????????????????????????????????????????????????????????
        Console.WriteLine("? Adding customer orders...");
        
        var loadedCustomer = await repo.FindAsync(customerId);
        if (loadedCustomer != null)
        {
            loadedCustomer.Orders.Add(new Order
            {
                Status = "Delivered",
                TotalAmount = 149.99m,
                ItemCount = 3,
                OrderDate = DateTimeOffset.UtcNow.AddDays(-10),
                ShippedDate = DateTimeOffset.UtcNow.AddDays(-8),
                TrackingNumber = "TRK1234567890"
            });

            loadedCustomer.Orders.Add(new Order
            {
                Status = "Processing",
                TotalAmount = 79.50m,
                ItemCount = 2,
                OrderDate = DateTimeOffset.UtcNow.AddDays(-2)
            });

            loadedCustomer.Orders.Add(new Order
            {
                Status = "Pending",
                TotalAmount = 299.99m,
                ItemCount = 1,
                OrderDate = DateTimeOffset.UtcNow
            });

            await repo.SaveAsync(loadedCustomer);
            Console.WriteLine($"  ? Added {loadedCustomer.Orders.Count} orders\n");
        }

        // ???????????????????????????????????????????????????????????
        // READ: Display customer profile
        // ???????????????????????????????????????????????????????????
        Console.WriteLine("? Loading complete customer profile...");
        
        var profile = await repo.FindAsync(customerId);
        if (profile != null)
        {
            Console.WriteLine($"  ? Customer: {profile.CustomerEmail}\n");
            
            // Orders
            Console.WriteLine($"  ?? Orders ({profile.Orders.Count}):");
            foreach (var order in profile.Orders.OrderByDescending(o => o.OrderDate))
            {
                Console.WriteLine($"    • Order #{order.OrderId}: ${order.TotalAmount:F2}");
                Console.WriteLine($"      Status: {order.Status} | Items: {order.ItemCount}");
                Console.WriteLine($"      Date: {order.OrderDate:yyyy-MM-dd}");
            }

            // Addresses
            Console.WriteLine($"\n  ?? Addresses ({profile.Addresses.Count}):");
            foreach (var addr in profile.Addresses)
            {
                var defaultTag = addr.IsDefault ? " (Default)" : "";
                Console.WriteLine($"    • {addr.AddressType}{defaultTag}");
                Console.WriteLine($"      {addr.Street}, {addr.City}, {addr.State} {addr.ZipCode}");
            }

            // Payment Methods
            Console.WriteLine($"\n  ?? Payment Methods ({profile.PaymentMethods.Count}):");
            foreach (var payment in profile.PaymentMethods)
            {
                var defaultTag = payment.IsDefault ? " (Default)" : "";
                Console.WriteLine($"    • {payment.CardBrand} ****{payment.LastFourDigits}{defaultTag}");
                Console.WriteLine($"      Expires: {payment.ExpiryMonth}/{payment.ExpiryYear}");
            }

            // Preferences
            Console.WriteLine($"\n  ??  Preferences ({profile.Preferences.Count}):");
            foreach (var pref in profile.Preferences)
            {
                Console.WriteLine($"    • {pref.Key}: {pref.Value}");
            }
        }

        // ???????????????????????????????????????????????????????????
        // QUERY: Get specific collections only
        // ???????????????????????????????????????????????????????????
        Console.WriteLine("\n? Querying orders only (efficient!)...");
        
        var ordersOnly = await repo.QueryCollectionAsync(customerId, c => c.Orders);
        var totalRevenue = ordersOnly.Sum(o => o.TotalAmount);
        var deliveredCount = ordersOnly.Count(o => o.Status == "Delivered");
        
        Console.WriteLine($"  ? Retrieved {ordersOnly.Count} orders");
        Console.WriteLine($"    Total Revenue: ${totalRevenue:F2}");
        Console.WriteLine($"    Delivered: {deliveredCount}\n");

        // ???????????????????????????????????????????????????????????
        // UPDATE: Modify profile and orders
        // ???????????????????????????????????????????????????????????
        Console.WriteLine("? Updating customer data...");
        
        if (profile != null)
        {
            // Add billing address
            profile.Addresses.Add(new ShippingAddress
            {
                AddressType = "Billing",
                Street = "456 Business Ave",
                City = "Portland",
                State = "OR",
                ZipCode = "97201",
                Country = "USA",
                IsDefault = false
            });

            // Update order status
            var processingOrder = profile.Orders.FirstOrDefault(o => o.Status == "Processing");
            if (processingOrder != null)
            {
                processingOrder.Status = "Shipped";
                processingOrder.ShippedDate = DateTimeOffset.UtcNow;
                processingOrder.TrackingNumber = "TRK9876543210";
            }

            await repo.SaveAsync(profile);
            Console.WriteLine("  ? Added billing address");
            Console.WriteLine("  ? Updated order status to 'Shipped'\n");
        }
    }
}
