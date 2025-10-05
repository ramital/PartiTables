# PartiTables

**Azure Table Storage made simple for .NET**

## The Problem
Azure Table Storage is about 95% cheaper than other NoSQL solutions, but fewer developers use it because its API is cumbersome.

## The Solution

PartiTables makes Table Storage as easy to use as Entity Framework.

### Before vs After

  **Without PartiTables:**
```csharp
// The old way: manual TableEntity manipulation
var entity = new TableEntity("partition", "row-key");
entity["FirstName"] = "John";
entity["LastName"] = "Doe";
await tableClient.UpsertEntityAsync(entity);
// ... tedious manual parsing when reading
// ... complex batch operations
// ... manual retry logic
```

  **With PartiTables:**
```csharp
var patient = new Patient 
{ 
    PatientId = "patient-123",
    FirstName = "John",
    LastName = "Doe"
};

// Add related data - all saved together atomically
patient.Meta.Add(new PatientMeta { Email = "john@example.com" });
patient.Consents.Add(new Consent { Type = "DataSharing", Status = "Granted" });
patient.Devices.Add(new DeviceLink { DeviceId = "device-001", Model = "FitBit" });

await repo.SaveAsync(patient);
//   All related records saved in ONE batch operation
//   Automatic retry with Polly resilience
//   Strong typing, IntelliSense, compile-time safety
```

## Why Use It 

   **Save Money** - $10/month instead of $240/month  
  **Fast** - Partition-based queries are lightning quick  
    **Type-Safe** - IntelliSense, compile-time checking  
   **Batch Operations** - Save multiple entities atomically  
   **Auto-Retry** - Built-in resilience with Polly  
   **Less Code** - One class replaces hundreds of lines  

## Handles Big Data with Transaction Safety

### Automatic Multi-Batch Rollback
PartiTables automatically handles datasets larger than Azure's 100-item batch limit with **automatic rollback** if any batch fails:

```csharp
// Save 10,000 records across 100 batches
var salesData = GenerateSalesData("store-001", 10_000);

await repo.SaveAsync(salesData);
//    Automatically split into 100-item batches
//    If ANY batch fails, ALL previous batches are rolled back
//    Your data stays consistent - it's all-or-nothing!
```

**What happens:**
1. **Automatic batching** - Splits 10,000 items into 100 batches
2. **Sequential submission** - Submits batches one at a time
3. **Rollback on failure** - If batch #50 fails, batches 1-49 are automatically deleted
4. **Exception preserved** - Original error is re-thrown after cleanup

**Benefits:**
-   **No partial data** - Either all items save or none do
-   **Unlimited scale** - Handle 10,000+ items automatically
-   **Zero configuration** - Works out of the box
-   **Big Data** - See [integration tests](docs/BigData.md) 

> **Real-world example:** Saving 10,000 sales transactions takes ~5-10 seconds with full rollback protection. See [BigDataDemoTests.cs](PartiTables.IntegrationTests/BigDataDemoTests.cs) for working code.

## Stop Worrying About Data Loss

### Automatic Batch Operations
PartiTables automatically groups related entities into **batch transactions** within the same partition:

```csharp
var customer = new Customer { CustomerId = "cust-123" };
customer.Orders.Add(new Order { Amount = 99.99m, Items = 3 });
customer.Orders.Add(new Order { Amount = 45.50m, Items = 1 });
customer.Profile.Add(new Profile { Email = "customer@example.com" });

await repo.SaveAsync(customer);
//    All 3 entities saved in ONE atomic batch operation
//    Either all succeed or all fail (within partition)
```

**Benefits:**
-   No partial writes within a partition
-   Up to 100 operations per batch
-   Automatic grouping by partition key
-   SQL transaction-like behavior for related data

### Built-in Resilience with Polly

PartiTables uses **Polly** for automatic retry policies:

```csharp
// Automatic retry on transient failures
await repo.SaveAsync(patient);
//    Retries on network errors
//    Exponential backoff
//    Circuit breaker protection
```

**You don't need to:**
-   Write retry logic
-   Handle transient failures
-   Implement exponential backoff
-   Track failed operations

**PartiTables handles it all automatically.**

## How Data is Saved

PartiTables transforms your object graph into optimized Table Storage entities:

```csharp
// Your code
var patient = new Patient { PatientId = "patient-123" };
patient.Meta.Add(new PatientMeta { FirstName = "John", LastName = "Doe" });
patient.Consents.Add(new Consent { Type = "DataSharing" });
patient.Devices.Add(new DeviceLink { DeviceId = "device-001" });

await repo.SaveAsync(patient);
```

**What happens behind the scenes:**

```
   Batch Transaction to Table Storage
PartitionKey: clinic-001
   patient-123-meta          (Auto-generated RowKey)
   patient-123-consent-a7b8  (Auto-generated RowKey)
   patient-123-device-001    (Auto-generated RowKey)

  All saved atomically in one batch
  Automatic retry on failure
  Optimistic concurrency handled
```

## Quick Start

```bash
dotnet add package PartiTables
```

```csharp
// Define your model
[TablePartition("Customers", "{CustomerId}")]
public class Customer
{
    public string CustomerId { get; set; }
    public List<Order> Orders { get; set; } = new();
    public List<Address> Addresses { get; set; } = new();
}

// Use it like Entity Framework
var customer = new Customer { CustomerId = "cust-123" };
customer.Orders.Add(new Order { Amount = 99.99m });
customer.Addresses.Add(new Address { City = "Seattle" });

await repo.SaveAsync(customer);
//    Multiple entities saved in one batch operation!
```

## CRUD Operations Made Simple

PartiTables provides intuitive, Entity Framework-style operations:

### Create & Update
```csharp
// Create new entity
var customer = new Customer { CustomerId = "cust-123", Name = "Acme Corp" };
customer.Orders.Add(new Order { Amount = 99.99m, Status = "Pending" });
await repo.SaveAsync(customer);

// Update existing
var loaded = await repo.FindAsync("cust-123");
loaded.Orders[0].Status = "Shipped";
await repo.SaveAsync(loaded);
```

### Read
```csharp
// Load entire partition
var customer = await repo.FindAsync("cust-123");

// Load specific collection only (faster!)
var orders = await repo.QueryCollectionAsync("cust-123", c => c.Orders);

// Load with prefix filter
var recentOrders = await client.QueryByPrefixAsync("cust-123", "order-2024-");
```

### Delete
```csharp
// Delete entire partition
await repo.DeleteAsync("cust-123");

// Delete specific entity
var customer = await repo.FindAsync("cust-123");
customer.Orders.RemoveAt(0);
await repo.SaveAsync(customer);
```

## Powerful Querying

### Partition-Scoped Queries (Fast)
```csharp
// Get all data for a partition
var allCustomerData = await repo.FindAsync("customer-123");

// Get specific collection (more efficient)
var orders = await repo.QueryCollectionAsync("customer-123", c => c.Orders);

// Prefix-based queries
var orders2024 = await client.QueryByPrefixAsync("customer-123", "order-2024-");
```

### Collection Filtering
```csharp
// Load and filter in memory (for small datasets)
var customer = await repo.FindAsync("customer-123");
var pendingOrders = customer.Orders.Where(o => o.Status == "Pending").ToList();

// For larger datasets, query specific collection first
var allOrders = await repo.QueryCollectionAsync("customer-123", c => c.Orders);
var shipped = allOrders.Where(o => o.Status == "Shipped").ToList();
```

### Cross-Partition Queries
```csharp
// Note: Cross-partition queries are slower but supported
var allPendingOrders = await tableClient
    .QueryAsync<Order>(o => o.Status == "Pending")
    .ToListAsync();
```

**Query Best Practices:**
-   Query within a single partition for best performance
-   Use prefix-based queries for time-series data
-   Load only the collections you need
-    Avoid cross-partition queries when possible
-    Filter in memory for small result sets

## Try It Now

```bash
cd PartiSample
dotnet run
```

Choose from 6 interactive demos showing real-world scenarios.

   ## Perfect For

   📱 Multi-tenant SaaS  
   🛒 Customer orders & history  
   🏥 Healthcare records  
   📋 Audit logs  
   👤 User profiles  
   🌐 IoT device data  
   📊 Time-series metrics  
   🔔 Notification queues  
   📦 Inventory tracking  
   🎫 Event ticketing systems  
   💬 Chat message history  
   🔐 Session management

## How It Works

Related data shares a partition. Fast to query. Simple to use.

**Single Partition (Fast Queries):**
```
PartitionKey: customer-123
RowKeys:
   customer-123-order-001
   customer-123-order-002
   customer-123-address-001
```

**Cross-Table References:**
```
Table: Customers                Table: Orders
PartitionKey: customer-123      PartitionKey: order-001
RowKey: customer-123-profile    RowKeys:
                                   order-001-details
                                   order-001-metadata
                                Properties:
                                   CustomerId: "customer-123"     References customer

Table: Devices                  Table: Alerts
PartitionKey: device-456        PartitionKey: alert-789
RowKey: device-456-config       RowKeys:
                                   alert-789-details
                                   alert-789-metadata
                                Properties:
                                   DeviceId: "device-456"       References device
```

**Multi-Tenant Isolation:**
```
PartitionKey: tenant-789
RowKeys:
   tenant-789-user-001
   tenant-789-user-002
   tenant-789-setting-001
```

## Documentation

- [Demos](PartiSample/Demos/) - Real-world examples
- [Quick Start](PartiSample/README.md) - Get started guide

## Requirements

- .NET 8.0+
- Azure Storage or Azurite

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
