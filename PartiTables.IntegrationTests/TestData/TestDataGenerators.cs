using Bogus;
using PartiTables.IntegrationTests.PartiTables;

namespace PartiTables.IntegrationTests.TestData;

/// <summary>
/// Provides Bogus fakers for generating test data
/// </summary>
public static class TestDataGenerators
{
    public static Faker<SalesTransaction> CreateTransactionFaker()
    {
        return new Faker<SalesTransaction>()
            .RuleFor(t => t.TransactionId, f => $"txn-{f.IndexFaker:D6}")
            .RuleFor(t => t.CustomerId, f => $"customer-{f.Random.Int(1, 50):D3}")
            .RuleFor(t => t.ProductName, f => f.PickRandom(
                "Widget", "Gadget", "Doohickey", "Thingamajig", "Whatchamacallit"))
            .RuleFor(t => t.Quantity, f => f.Random.Int(1, 10))
            .RuleFor(t => t.UnitPrice, f => f.Random.Decimal(10, 500))
            .RuleFor(t => t.TotalAmount, (f, t) => t.Quantity * t.UnitPrice)
            .RuleFor(t => t.Region, f => f.PickRandom("North", "South", "East", "West"))
            .RuleFor(t => t.TransactionDate, f => f.Date.Between(
                new DateTime(2024, 1, 1), 
                new DateTime(2024, 12, 31)));
    }

    public static Faker<SalesData> CreateSalesDataFaker()
    {
        return new Faker<SalesData>()
            .RuleFor(s => s.StoreId, f => $"store-{f.Random.Int(1, 100):D3}")
            .RuleFor(s => s.StoreName, f => f.Company.CompanyName())
            .RuleFor(s => s.Region, f => f.PickRandom("North", "South", "East", "West"));
    }

    public static SalesData GenerateSalesData(string storeId, int transactionCount = 1000)
    {
        var storeFaker = CreateSalesDataFaker();
        var transactionFaker = CreateTransactionFaker();

        var salesData = storeFaker.Generate();
        salesData.StoreId = storeId;
        salesData.StoreName = $"Store {storeId.Split('-')[1]}";
        
        var transactions = transactionFaker.Generate(transactionCount);
        foreach (var txn in transactions)
        {
            txn.TransactionId = $"{storeId}-{txn.TransactionId}";
        }
        
        salesData.Transactions.AddRange(transactions);
        return salesData;
    }
}
