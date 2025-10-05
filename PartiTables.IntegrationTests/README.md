# PartiTables Integration Tests

##    Big Data Demo

Integration tests demonstrating PartiTables simplicity at scale with 10,000+ records using **Bogus** for realistic fake data.

### What's Demonstrated

#### Test 1: Single Store with 10,000 Transactions
-   **Simple Setup**: 3 lines of configuration
-   **Bogus Data**: Realistic fake data generation
-   **Plain Models**: C# classes in `TestModels/`
-   **One-Line Operations**: Save and load thousands of records
-   **LINQ Queries**: Complex analytics with familiar syntax
-   **Fast Performance**: Thousands of records per second
-   **FluentAssertions**: Expressive test validation

**Analytics Queries:**
- Total revenue calculation
- Revenue grouped by product
- Top 5 customers by spend
- Revenue by geographic region
- Monthly revenue trends
- Complex multi-filter queries

#### Test 2: Multi-Store with 5,000 Transactions
-   **Partitioning Strategy**: 5 stores, 1,000 transactions each
-   **Fast Queries**: Partition-scoped queries in milliseconds
-   **Isolation**: Each store's data is separate
-   **Aggregation**: Cross-store analytics

#### Test 3: Rollback on Batch Failure ? NEW
-   **Transaction Safety**: Automatic rollback across multiple batches
-   **Failure Simulation**: Tests invalid data (forbidden RowKey characters)
-   **Data Consistency**: Verifies all-or-nothing behavior
-   **Multi-Batch Handling**: 150 items across 2 batches, failure in batch 2
-   **Verification**: Confirms no partial data remains after failure

**What's Tested:**
```csharp
// Create 150 transactions (2 batches)
var salesData = TestDataGenerators.GenerateSalesData("store-rollback-test", 150);

// Corrupt transaction #101 with invalid RowKey (in 2nd batch)
salesData.Transactions[101].TransactionId = "store/INVALID/txn-000101";

try
{
    await repo.SaveAsync(salesData);
}
catch (ArgumentException)
{
    // ? Exception thrown as expected
}

// Verify: NO data exists (complete rollback)
var loaded = await repo.FindAsync("store-rollback-test");
loaded.Should().BeNull(); // All data rolled back!
```

**Rollback Guarantees:**
- ? Batch 1 (100 items) saved successfully
- ? Batch 2 (50 items) fails due to invalid character
-   Batch 1 is automatically deleted (rolled back)
-   No partial data remains in storage
-   Original exception is re-thrown after cleanup

##    Bogus Integration

We use **Bogus** to generate realistic test data. See `TestData/TestDataGenerators.cs` for faker configurations.

### Simple Data Generation

```csharp
// Generate 10,000 realistic transactions in one line!
var salesData = TestDataGenerators.GenerateSalesData("store-001", 10_000);

// Behind the scenes, Bogus creates:
// - Random customer IDs
// - Random product names
// - Random quantities (1-10)
// - Random prices ($10-$500)
// - Calculated totals
// - Random regions
// - Random dates throughout 2024
```

### Custom Fakers

```csharp
var transactionFaker = new Faker<SalesTransaction>()
    .RuleFor(t => t.TransactionId, f => $"txn-{f.IndexFaker:D6}")
    .RuleFor(t => t.CustomerId, f => $"customer-{f.Random.Int(1, 50):D3}")
    .RuleFor(t => t.ProductName, f => f.PickRandom("Widget", "Gadget", "Doohickey"))
    .RuleFor(t => t.Quantity, f => f.Random.Int(1, 10))
    .RuleFor(t => t.UnitPrice, f => f.Random.Decimal(10, 500))
    .RuleFor(t => t.TotalAmount, (f, t) => t.Quantity * t.UnitPrice)
    .RuleFor(t => t.Region, f => f.PickRandom("North", "South", "East", "West"))
    .RuleFor(t => t.TransactionDate, f => f.Date.Between(
        new DateTime(2024, 1, 1), 
        new DateTime(2024, 12, 31)));

var transactions = transactionFaker.Generate(10_000);
```

##    Test Structure

Clean AAA pattern (Arrange-Act-Assert):

```csharp
// ARRANGE
var services = new ServiceCollection();
services.AddPartiTables(opts => { /* ... */ });
var salesData = TestDataGenerators.GenerateSalesData("store-001", 10_000);

// ACT
await repo.SaveAsync(salesData);
var loaded = await repo.FindAsync("store-001");

// ASSERT
using (new AssertionScope())
{
    loaded.Should().NotBeNull();
    loaded.Transactions.Should().HaveCount(10_000);
    saveTime.Should().BeLessThan(TimeSpan.FromSeconds(30));
}
```

##    Running the Tests

### Prerequisites
1. **Start Azurite**:
   ```bash
   azurite
   ```

2. **Run Tests**:
   ```bash
   cd PartiTables.IntegrationTests
   dotnet test
   ```

   With detailed output:
   ```bash
   dotnet test --logger "console;verbosity=detailed"
   ```

##    Key Features

### Simple API
```csharp
// Setup (3 lines)
services.AddPartiTables(opts => {
    opts.ConnectionString = "UseDevelopmentStorage=true";
    opts.TableName = "BigDataDemo";
});
services.AddPartitionRepository<SalesData>();

// Generate fake data (1 line with Bogus!)
var salesData = TestDataGenerators.GenerateSalesData("store-001", 10_000);

// Save 10,000 records (1 line)
await repo.SaveAsync(salesData);

// Load all data (1 line)
var loaded = await repo.FindAsync("store-001"));

// Query with LINQ
var totalRevenue = loaded.Transactions.Sum(t => t.TotalAmount);
var topProducts = loaded.Transactions
    .GroupBy(t => t.ProductName)
    .OrderByDescending(g => g.Sum(t => t.TotalAmount));

// Assert with FluentAssertions
loaded.Should().NotBeNull();
loaded.Transactions.Should().HaveCount(10_000);
topProducts.Should().BeInDescendingOrder(x => x.Revenue);
```

### No Need For
-   Complex query languages
-   Manual batching logic
-   Row/column mapping code
-   Entity conversion boilerplate
-   Transaction management
-   Manual test data creation

### You Get
-   Type safety and IntelliSense
-   Automatic batching (up to 100 ops)
-   Efficient partition-based queries
-   LINQ support for analytics
-   Fast performance at scale
-   Realistic test data with Bogus
-   Expressive test assertions

##    Performance

Demonstrated performance metrics:
- **Write**: ~1,000-2,000 records/second
- **Read**: 10,000 records in 2-3 seconds
- **Query**: Partition queries in <100ms

*Actual performance varies by network latency, batch size, and entity complexity.*

##     Data Model

Models located in `TestModels/SalesModels.cs`:

```
SalesData (Partition Root)
     StoreId (PartitionKey)
     Transactions[] (Collection)
         TransactionId
         CustomerId
         ProductName
         Quantity, UnitPrice, TotalAmount
         Region
         TransactionDate
```

**Storage Layout:**
```
Table: BigDataDemo
PartitionKey: store-001
RowKeys:
     store-001-txn-000000
     store-001-txn-000001
     ... (10,000 total)
```

##    Dependencies

- **PartiTables** - Main library
- **xUnit** - Test framework
- **FluentAssertions** - Expressive assertions
- **Bogus** - Fake data generation
- **Microsoft.Extensions.DependencyInjection** - DI container

##    Resources

- [Main README](../README.md)
- [Demo Application](../PartiSample/)
- [PartiTables Library](../PartiTables/)
- [FluentAssertions Guide](FLUENT_ASSERTIONS_GUIDE.md)
- [Quick Start](QUICK_START.md)
- [Bogus Documentation](https://github.com/bchavez/Bogus)

---

**Try it yourself!** These tests prove working with big data in Azure Table Storage is simple.
