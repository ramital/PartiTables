using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.DependencyInjection;
using PartiTables;
using PartiTables.IntegrationTests.PartiTables;

namespace PartiTables.IntegrationTests;

/// <summary>
/// Tests for adding new items to existing loaded entities.
/// Reproduces the issue where RowKeys get malformed (e.g., "-order-ORD-004" instead of "cust-001-order-ORD-004")
/// when adding items after the initial save.
/// </summary>
public class AddToExistingEntityTests
{
    private const string ConnectionString = "UseDevelopmentStorage=true";

    [Fact]
    public async Task AddOrder_ToExistingCustomer_GeneratesCorrectRowKey()
    {
        // ARRANGE
        var services = new ServiceCollection();
        services.AddPartiTables(opts =>
        {
            opts.ConnectionString = ConnectionString;
            opts.TableName = "AddToExistingTest";
        });
        services.AddPartitionRepository<Customer>();
        var provider = services.BuildServiceProvider();
        var repo = provider.GetRequiredService<PartitionRepository<Customer>>();

        var tenantId = "tenant-001";

        // CLEANUP
        await repo.DeleteAsync(tenantId);

        // ACT - Step 1: Save initial customer with orders
        var customer = new Customer
        {
            TenantId = tenantId,
            CustomerId = "cust-001"
        };

        customer.Profile.Add(new CustomerProfile
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            Phone = "555-1234"
        });

        customer.Order.Add(new Order
        {
            OrderId = "ORD-001",
            Amount = 100.00m,
            Status = "Pending",
            OrderDate = DateTime.UtcNow
        });

        customer.Order.Add(new Order
        {
            OrderId = "ORD-002",
            Amount = 200.00m,
            Status = "Shipped",
            OrderDate = DateTime.UtcNow
        });

        await repo.SaveAsync(customer);

        // ACT - Step 2: Load existing customer and add NEW order (THIS IS WHERE THE BUG HAPPENS)
        var loadedCustomer = await repo.FindAsync(tenantId);
        loadedCustomer.Should().NotBeNull("customer should be loaded");

        // Add a new order - this should get RowKey "cust-001-order-ORD-003"
        // but the bug causes it to become "-order-ORD-003" (missing CustomerId prefix)
        loadedCustomer!.Order.Add(new Order
        {
            OrderId = "ORD-003",
            Amount = 150.00m,
            Status = "Pending",
            OrderDate = DateTime.UtcNow
        });

        await repo.SaveAsync(loadedCustomer);

        // ASSERT - Reload and verify the newly added order has correct RowKey
        var reloadedCustomer = await repo.FindAsync(tenantId);

        using (new AssertionScope())
        {
            reloadedCustomer.Should().NotBeNull("customer should be reloaded");
            reloadedCustomer!.Order.Should().HaveCount(3, "all 3 orders should exist");

            // Verify the original orders still have correct RowKeys
            var ord1 = reloadedCustomer.Order.FirstOrDefault(o => o.OrderId == "ORD-001");
            ord1.Should().NotBeNull("ORD-001 should exist");
            ord1!.RowKeyId.Should().Be($"{loadedCustomer.CustomerId}-order-ORD-001", "original order should have correct RowKey");

            var ord2 = reloadedCustomer.Order.FirstOrDefault(o => o.OrderId == "ORD-002");
            ord2.Should().NotBeNull("ORD-002 should exist");
            ord2!.RowKeyId.Should().Be($"{loadedCustomer.CustomerId}-order-ORD-002", "original order should have correct RowKey");

            // THE KEY TEST: The newly added order should have the correct RowKey
            var ord3 = reloadedCustomer.Order.FirstOrDefault(o => o.OrderId == "ORD-003");
            ord3.Should().NotBeNull("ORD-003 should exist");
            
            // This is the bug - it becomes "-order-ORD-003" instead of "cust-001-order-ORD-003"
            ord3!.RowKeyId.Should().Be($"{loadedCustomer.CustomerId}-order-ORD-003", 
                "newly added order should have correct RowKey with CustomerId prefix");
            
            ord3.RowKeyId.Should().NotStartWith("-", 
                "RowKey should NOT start with hyphen (this is the bug we're fixing)");
            
            ord3.RowKeyId.Should().Contain(loadedCustomer.CustomerId, 
                "RowKey should contain the CustomerId");
        }
    }

    [Fact]
    public async Task AddMultipleOrders_ToExistingCustomer_AllGenerateCorrectRowKeys()
    {
        // ARRANGE
        var services = new ServiceCollection();
        services.AddPartiTables(opts =>
        {
            opts.ConnectionString = ConnectionString;
            opts.TableName = "AddMultipleTest";
        });
        services.AddPartitionRepository<Customer>();
        var provider = services.BuildServiceProvider();
        var repo = provider.GetRequiredService<PartitionRepository<Customer>>();

        var tenantId = "tenant-002";

        // CLEANUP
        await repo.DeleteAsync(tenantId);

        // ACT - Initial save with one order
        var customer = new Customer
        {
            TenantId = tenantId,
            CustomerId = "cust-002"
        };

        customer.Profile.Add(new CustomerProfile
        {
            FirstName = "Jane",
            LastName = "Smith",
            Email = "jane.smith@example.com"
        });

        customer.Order.Add(new Order
        {
            OrderId = "ORD-001",
            Amount = 500.00m,
            Status = "Pending",
            OrderDate = DateTime.UtcNow
        });

        await repo.SaveAsync(customer);

        // ACT - Load and add multiple orders at once
        var loadedCustomer = await repo.FindAsync(tenantId);

        loadedCustomer!.Order.Add(new Order
        {
            OrderId = "ORD-002",
            Amount = 250.00m,
            Status = "Processing",
            OrderDate = DateTime.UtcNow
        });

        loadedCustomer.Order.Add(new Order
        {
            OrderId = "ORD-003",
            Amount = 750.00m,
            Status = "Shipped",
            OrderDate = DateTime.UtcNow
        });

        await repo.SaveAsync(loadedCustomer);

        // ASSERT
        var reloadedCustomer = await repo.FindAsync(tenantId);

        using (new AssertionScope())
        {
            reloadedCustomer.Should().NotBeNull();
            reloadedCustomer!.Order.Should().HaveCount(3, "all 3 orders should exist");

            // Verify ALL RowKeys are correct (no malformed keys)
            foreach (var order in reloadedCustomer.Order)
            {
                order.RowKeyId.Should().Be($"{loadedCustomer.CustomerId}-order-{order.OrderId}",
                    $"order {order.OrderId} should have correct RowKey");
                
                order.RowKeyId.Should().NotStartWith("-",
                    $"order {order.OrderId} RowKey should not start with hyphen");
                
                order.RowKeyId.Should().StartWith(loadedCustomer.CustomerId,
                    $"order {order.OrderId} RowKey should start with CustomerId");
            }
        }
    }
}
