using Microsoft.Extensions.DependencyInjection;
using PartiTables;
using PartiTables.IntegrationTests.PartiTables;
using PartiTables.IntegrationTests.TestData;
using FluentAssertions;
using FluentAssertions.Execution;

namespace PartiTables.IntegrationTests;

public class BigDataDemoTests
{
    private const string ConnectionString = "UseDevelopmentStorage=true";

    [Fact]
    public async Task BigData_Demo_ShowsSimplicityAtScale()
    {
        // ARRANGE
        var services = new ServiceCollection();
        services.AddPartiTables(opts =>
        {
            opts.ConnectionString = ConnectionString;
            opts.TableName = "BigDataDemo";
        });
        services.AddPartitionRepository<SalesData>();
        var provider = services.BuildServiceProvider();
        var repo = provider.GetRequiredService<PartitionRepository<SalesData>>();

        // CLEANUP - Delete any existing data from previous test runs
        await repo.DeleteAsync("store-001");

        var salesData = TestDataGenerators.GenerateSalesData("store-001", 10_000);

        // ACT - Save
        var saveStart = DateTimeOffset.UtcNow;
        await repo.SaveAsync(salesData);
        var saveTime = DateTimeOffset.UtcNow - saveStart;

        // ACT - Load
        var loadStart = DateTimeOffset.UtcNow;
        var loaded = await repo.FindAsync("store-001");
        var loadTime = DateTimeOffset.UtcNow - loadStart;

        // ACT - Query
        var totalRevenue = loaded!.Transactions.Sum(t => t.TotalAmount);

        var revenueByProduct = loaded.Transactions
            .GroupBy(t => t.ProductName)
            .Select(g => new { Product = g.Key, Revenue = g.Sum(t => t.TotalAmount) })
            .OrderByDescending(x => x.Revenue)
            .ToList();

        var topCustomers = loaded.Transactions
            .GroupBy(t => t.CustomerId)
            .Select(g => new 
            { 
                Customer = g.Key, 
                TotalSpent = g.Sum(t => t.TotalAmount),
                OrderCount = g.Count()
            })
            .OrderByDescending(x => x.TotalSpent)
            .Take(5)
            .ToList();

        var revenueByRegion = loaded.Transactions
            .GroupBy(t => t.Region)
            .Select(g => new { Region = g.Key, Revenue = g.Sum(t => t.TotalAmount) })
            .OrderByDescending(x => x.Revenue)
            .ToList();

        var monthlyRevenue = loaded.Transactions
            .GroupBy(t => new { t.TransactionDate.Year, t.TransactionDate.Month })
            .Select(g => new 
            { 
                Month = $"{g.Key.Year}-{g.Key.Month:D2}",
                Revenue = g.Sum(t => t.TotalAmount),
                Count = g.Count()
            })
            .OrderBy(x => x.Month)
            .Take(6)
            .ToList();

        var highValueOrders = loaded.Transactions
            .Where(t => t.TotalAmount > 1000 && t.Region == "North")
            .OrderByDescending(t => t.TotalAmount)
            .Take(10)
            .ToList();

        // ASSERT
        using (new AssertionScope())
        {
            loaded.Should().NotBeNull("data should be loaded successfully");
            loaded.Transactions.Should().HaveCount(10_000, "all transactions should be saved and loaded");
            
            saveTime.Should().BeLessThan(TimeSpan.FromSeconds(30), "save operation should be fast");
            loadTime.Should().BeLessThan(TimeSpan.FromSeconds(10), "load operation should be fast");
            
            totalRevenue.Should().BeGreaterThan(0, "store should have positive revenue");
            
            revenueByProduct.Should().HaveCount(5, "all 5 products should have sales");
            revenueByProduct.Should().OnlyContain(x => x.Revenue > 0, "all products should have positive revenue");
            revenueByProduct.Should().BeInDescendingOrder(x => x.Revenue, "products should be ordered by revenue");
            
            topCustomers.Should().HaveCount(5, "should return top 5 customers");
            topCustomers.Should().OnlyContain(x => x.TotalSpent > 0, "top customers should have positive spending");
            topCustomers.Should().BeInDescendingOrder(x => x.TotalSpent, "customers should be ordered by spending");
            
            revenueByRegion.Should().HaveCount(4, "all 4 regions should have sales");
            revenueByRegion.Should().OnlyContain(x => x.Revenue > 0, "all regions should have positive revenue");
            
            monthlyRevenue.Should().NotBeEmpty("should have monthly revenue data");
            monthlyRevenue.Should().HaveCountLessThanOrEqualTo(12, "should not exceed 12 months");
            
            highValueOrders.Should().NotBeEmpty("should find high-value orders");
            highValueOrders.Should().OnlyContain(x => x.TotalAmount > 1000, "all orders should be high value");
            highValueOrders.Should().OnlyContain(x => x.Region == "North", "all orders should be from North region");
        }
    }

    [Fact]
    public async Task MultiStore_Demo_ShowsPartitioningStrategy()
    {
        // ARRANGE
        var services = new ServiceCollection();
        services.AddPartiTables(opts =>
        {
            opts.ConnectionString = ConnectionString;
            opts.TableName = "MultiStoreDemoNew";
        });
        services.AddPartitionRepository<SalesData>();
        var provider = services.BuildServiceProvider();
        var repo = provider.GetRequiredService<PartitionRepository<SalesData>>();

        var stores = new[] { "store-001", "store-002", "store-003", "store-004", "store-005" };

        // CLEANUP - Delete any existing data from previous test runs
        foreach (var storeId in stores)
        {
            await repo.DeleteAsync(storeId);
        }

        // ACT - Create and save stores
        foreach (var storeId in stores)
        {
            var storeData = TestDataGenerators.GenerateSalesData(storeId, 1_000);
            await repo.SaveAsync(storeData);
        }

        // ACT - Query single store
        var queryStart = DateTimeOffset.UtcNow;
        var store3Data = await repo.FindAsync("store-003");
        var queryTime = DateTimeOffset.UtcNow - queryStart;

        // ACT - Aggregate across stores
        long totalTransactions = 0;
        decimal totalRevenue = 0;
        var storeResults = new List<(string StoreId, int TransactionCount, decimal Revenue)>();

        foreach (var storeId in stores)
        {
            var data = await repo.FindAsync(storeId);
            if (data != null)
            {
                var storeTransactions = data.Transactions.Count;
                var storeRevenue = data.Transactions.Sum(t => t.TotalAmount);
                
                totalTransactions += storeTransactions;
                totalRevenue += storeRevenue;
                storeResults.Add((storeId, storeTransactions, storeRevenue));
            }
        }

        // ASSERT
        using (new AssertionScope())
        {
            store3Data.Should().NotBeNull("store data should be loaded");
            store3Data!.Transactions.Should().HaveCount(1_000, "each store should have 1000 transactions");
            store3Data.StoreId.Should().Be("store-003", "correct store data should be loaded");
            
            queryTime.Should().BeLessThan(TimeSpan.FromSeconds(5), "partition query should be fast");
            
            totalTransactions.Should().Be(5_000, "total should match sum of all stores");
            totalRevenue.Should().BeGreaterThan(0, "total revenue should be positive");
            
            storeResults.Should().HaveCount(5, "all stores should be loaded");
            storeResults.Should().OnlyContain(x => x.TransactionCount == 1_000, 
                "each store should have exactly 1000 transactions");
            storeResults.Should().OnlyContain(x => x.Revenue > 0, 
                "each store should have positive revenue");
            
            storeResults.Select(x => x.StoreId).Should().OnlyHaveUniqueItems(
                "each store should be isolated in its own partition");
        }
    }

    [Fact]
    public async Task SaveAsync_WithInvalidRowKey_RollsBackSuccessfulBatches()
    {
        // ARRANGE
        var services = new ServiceCollection();
        services.AddPartiTables(opts =>
        {
            opts.ConnectionString = ConnectionString;
            opts.TableName = "RollbackTestDemo";
        });
        services.AddPartitionRepository<SalesData>();
        var provider = services.BuildServiceProvider();
        var repo = provider.GetRequiredService<PartitionRepository<SalesData>>();

        // CLEANUP - Delete any existing data from previous test runs
        await repo.DeleteAsync("store-rollback-test");

        // Create valid data for first 150 transactions (2 batches worth)
        var salesData = TestDataGenerators.GenerateSalesData("store-rollback-test", 150);
        
        // Corrupt one transaction with an invalid RowKey (contains forbidden character '/')
        // This will be in the 2nd batch (transaction 101)
        salesData.Transactions[101].TransactionId = "store-rollback-test/INVALID/txn-000101";

        // ACT & ASSERT
        Exception? caughtException = null;
        try
        {
            await repo.SaveAsync(salesData);
        }
        catch (Exception ex)
        {
            caughtException = ex;
        }

        // ASSERT - Exception should be thrown
        using (new AssertionScope())
        {
            caughtException.Should().NotBeNull("SaveAsync should throw an exception when batch contains invalid row key");
            caughtException.Should().BeOfType<ArgumentException>("the error should indicate invalid row key characters");
            
            // Verify rollback occurred - no data should exist in the partition
            var loadedAfterFailure = await repo.FindAsync("store-rollback-test");
            loadedAfterFailure.Should().BeNull("all data should be rolled back after batch failure");
            
            // Double-check by querying the partition directly
            var allRows = await repo.QueryAsync("store-rollback-test");
            allRows.Should().BeEmpty("partition should be empty after rollback");
        }
    }
}
