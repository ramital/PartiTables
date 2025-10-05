# PartiTables

**Azure Table Storage made ridiculously simple.**

Stop writing boilerplate. Start building features.

```csharp
// Setup (3 lines)
services.AddPartiTables(opts => opts.ConnectionString = connectionString);
services.AddPartitionRepository<Customer>();

// Save 10,000 entities (1 line)
await repo.SaveAsync(customer);

// Load them back (1 line)
var loaded = await repo.FindAsync("customer-123");

// Query with LINQ
var pending = loaded.Orders.Where(o => o.Status == "Pending");
```

## Why PartiTables?

Azure Table Storage is powerful but verbose. PartiTables gives you:

- **Zero boilerplate** - No manual entity mapping
- **Type-safe** - Strongly-typed entities
- **Auto-batching** - Handles 100-item limit automatically
- **Smart keys** - Auto-generates RowKeys
- **LINQ queries** - Natural C# syntax
- **DI-friendly** - Built for .NET apps
- **Automatic rollback** - Transaction safety across batches
- **Production-ready** - Resilience policies included

## Features

### Automatic Transaction Rollback

PartiTables automatically handles batch failures with complete rollback support:

```csharp
// Saving 1000+ items across multiple batches
var salesData = GenerateSalesData("store-001", 10_000);

try
{
    await repo.SaveAsync(salesData); // Auto-batches into 100-item chunks
    // ? All 10,000 items saved successfully
}
catch (Exception)
{
    // ? If any batch fails, ALL successfully saved batches are rolled back
    // Your data remains consistent - it's all-or-nothing!
}

// Verify: Either all data exists, or none exists
var loaded = await repo.FindAsync("store-001");
// loaded will have 10,000 transactions, or be null - never partial data
```

**How it works:**
- Automatically splits data into batches of 100 (Azure's limit)
- Tracks each successful batch submission
- If any batch fails, automatically deletes all previously saved entities
- Throws the original exception after rollback completes
- Guarantees consistency across multi-batch operations

**Edge cases handled:**
- ? Rollback failures throw `AggregateException` with details
- ? Empty batches are skipped
- ? Large datasets are properly batched during rollback
- ? Original exception is preserved and re-thrown

### Automatic Batching

Handle unlimited entities without worrying about Azure's 100-item batch limit:

```csharp
// Works seamlessly with any size dataset
var data = GenerateData(10_000); // 10,000 items
await repo.SaveAsync(data); // Automatically split into 100 batches

// Same applies to deletes
await repo.DeleteAsync(partitionKey); // Handles any number of entities
```

// ...existing code...
