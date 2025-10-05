# PartiTables - Quick Start (2 Minutes!)

Get started with PartiTables in just a few lines of code.

## Step 1: Setup (1 minute)

```csharp
using Microsoft.Extensions.DependencyInjection;
using PartiTables;
using PartiTables.Interfaces;

var services = new ServiceCollection();
services.AddPartiTablesForDevelopment("MyData"); // Local emulator
var client = services.BuildServiceProvider().GetRequiredService<IPartitionClient>();
```

## Step 2: Create & Save Data (30 seconds)

```csharp
// Simple fluent entity creation
var customer = PartitionEntity.Create("store-001", "customer-john")
    .Set("Name", "John Doe")
    .Set("Email", "john@example.com")
    .Set("Points", 1500)
    .Set("Status", "Gold");

await client.UpsertAsync(customer);
Console.WriteLine("? Customer saved!");
```

## Step 3: Read Data (30 seconds)

```csharp
// Get single entity
var found = await client.TryGetAsync("store-001", "customer-john");

if (found != null)
{
    Console.WriteLine($"Name: {found.GetString("Name")}");
    Console.WriteLine($"Email: {found.GetString("Email")}");
    Console.WriteLine($"Points: {found.GetInt32("Points")}");
}
```

## Complete Working Example

```csharp
using Microsoft.Extensions.DependencyInjection;
using PartiTables;
using PartiTables.Interfaces;

// Setup
var services = new ServiceCollection();
services.AddPartiTablesForDevelopment("QuickStart");
var client = services.BuildServiceProvider().GetRequiredService<IPartitionClient>();

var storeId = "store-001";

// CREATE - Add multiple related entities
await client.UpsertAsync(
    PartitionEntity.Create(storeId, "customer-123")
        .Set("Name", "Alice")
        .Set("Email", "alice@example.com")
        .Set("Tier", "Gold")
);

await client.UpsertAsync(
    PartitionEntity.Create(storeId, "order-456")
        .Set("CustomerId", "123")
        .Set("Total", 99.99m)
        .Set("Status", "Shipped")
);

await client.UpsertAsync(
    PartitionEntity.Create(storeId, "order-457")
        .Set("CustomerId", "123")
        .Set("Total", 149.99m)
        .Set("Status", "Processing")
);

Console.WriteLine("? Created customer and 2 orders");

// READ - Get all entities
var allEntities = await client.GetPartitionAsync(storeId);
Console.WriteLine($"? Store has {allEntities.Count} total entities");

// QUERY - Get specific type by prefix
var orders = await client.QueryByPrefixAsync(storeId, "order-");
Console.WriteLine($"? Found {orders.Count} orders");

foreach (var order in orders)
{
    Console.WriteLine($"  Order {order.RowKey}: ${order.GetDouble("Total")} - {order.GetString("Status")}");
}

// UPDATE - Modify existing
var customer = await client.GetAsync(storeId, "customer-123");
customer["Tier"] = "Platinum";
await client.UpdateAsync(customer);
Console.WriteLine("? Customer upgraded to Platinum");

// BATCH - Multiple operations atomically
var batch = new PartitionBatch(storeId);

batch.Upsert(PartitionEntity.Create(storeId, "order-458")
    .Set("CustomerId", "123")
    .Set("Total", 199.99m)
    .Set("Status", "Pending"));

batch.Upsert(PartitionEntity.Create(storeId, "customer-123-note")
    .Set("Note", "VIP customer"));

await client.SubmitAsync(batch);
Console.WriteLine($"? Batch saved {batch.Count} items atomically");

// DELETE
await client.DeleteAsync(storeId, "customer-123-note");
Console.WriteLine("? Note deleted");

Console.WriteLine("\n=== Done! ===");
```

## That's It! ??

You now know how to:
- ? Setup PartiTables
- ? Create entities with fluent syntax
- ? Read single entities and query by prefix
- ? Update and delete data
- ? Use batch operations

## Key Concepts

### Partition Key Strategy
Think of partition key as your "container":
```csharp
Partition Key: "store-001" (groups all data for this store)
Row Keys:
  - customer-{id}
  - order-{id}
  - product-{id}
```

### Row Key with Prefixes
Use prefixes to enable filtering:
```csharp
// Get all orders
var orders = await client.QueryByPrefixAsync("store-001", "order-");

// Get all customers
var customers = await client.QueryByPrefixAsync("store-001", "customer-");
```

### Fluent Entity Creation
Chain `.Set()` calls:
```csharp
var entity = PartitionEntity.Create("pk", "rk")
    .Set("Field1", "value1")
    .Set("Field2", 123)
    .Set("Field3", DateTime.Now);
```

### Type-Safe Reading
Use helper methods:
```csharp
var name = entity.GetString("Name");
var age = entity.GetInt32("Age");
var date = entity.GetDateTime("CreatedAt");
var price = entity.GetDouble("Price");
var active = entity.GetBoolean("IsActive");
```

## Next Steps

- Check out the full demo in `PatientPartitionSample/Program.cs`
- Read `README.md` for all features
- Design your partition/row key strategy

Happy coding! ??
