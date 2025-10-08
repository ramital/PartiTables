# PartiTables

**Azure Table Storage made simple for .NET**

[![NuGet](https://img.shields.io/nuget/v/PartiTables.svg)](https://www.nuget.org/packages/PartiTables/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

## 🧠 Why PartiTables?

Azure Table Storage is **95% cheaper** than other NoSQL solutions and blazing fast but painful to use. PartiTables fixes that by providing easy data access patterns.

**Type-Safe** - IntelliSense, compile-time checking  
**Auto-Retry** - Built-in resilience with Polly  
**Batch Operations** - Save multiple entities atomically  
**Less Code** - One class replaces hundreds of lines  
**Save Money** - Azure Storage is cheap and scales infinitely

---

## 🎯 What PartiTables Does For You

PartiTables transforms your C# objects into Azure Table Storage's NoSQL structure automatically. Here's what happens behind the scenes:

### Your Simple C# Code:
```csharp
var customer = new Customer { CustomerId = "tenantA_123", Name = "John Doe" };
customer.Orders.Add(new Order { OrderId = "order-001", Amount = 99.99m });
customer.Orders.Add(new Order { OrderId = "order-002", Amount = 45.50m });

await repo.SaveAsync(customer);
```

### 💡 Why This NoSQL Pattern is Powerful:

✅ **Lightning Fast Queries** - All customer data in one partition (single server lookup)  
✅ **Atomic Transactions** - All operations within a partition succeed or fail together  
✅ **Perfect Multi-Tenancy** - Each tenant isolated by PartitionKey  
✅ **Infinite Scale** - Add millions of partitions (customers/tenants) without performance loss  
✅ **Dirt Cheap** - Pay only for storage ($0.06/GB) and operations ($0.005 per 10k transactions)  
✅ **No Joins Needed** - All related data retrieved in one query  

### Real-World Multi-Entity Example:

```csharp
var user = new User { UserId = "tenantA_123" };
user.Consents.Add(new Consent { ConsentId = "consent-001", Type = "DataSharing", Timestamp = DateTime.UtcNow });
user.Devices.Add(new Device { DeviceId = "device-D123", Model = "iPhone 14" });
user.AuditLogs.Add(new AuditLog { Action = "Login", PerformedAt = DateTime.UtcNow });

await repo.SaveAsync(user);
```

**Resulting Table Structure:**

| PartitionKey | RowKey | Properties |
|-------------|--------|------------|
| `tenantA_123` | `user12_meta` | FirstName, LastName, DOB, Email, Status |
| `tenantA_123` | `user12_consent#20251002` | ConsentId, Type, Timestamp, Status |
| `tenantA_123` | `user12_consent#20250915` | ConsentId, Type, Timestamp, Status |
| `tenantA_123` | `user12_device#D123` | DeviceId, SerialNumber, Model, RegisteredAt |
| `tenantA_123` | `user12_audit#ULID123` | Action, PerformedAt, ActorId |

**🚀 Benefits:**
- Query all user data with ONE request: `await repo.FindAsync("tenantA_123")`
- Query just consents: `await repo.QueryCollectionAsync("tenantA_123", u => u.Consents)`
- Save everything atomically in ONE batch transaction
- Perfect for GDPR/compliance: Delete all user data with one partition delete

---

## 📦 Installation

```sh
dotnet add package PartiTables
```

---

## 🚀 Quick Start

### Initialize Repository  
```csharp
services.AddPartiTables(opts => {
    opts.ConnectionString = "UseDevelopmentStorage=true"; // or your Azure connection string
    opts.TableName = "Default";
});
services.AddPartitionRepository<Customer>();
services.AddPartitionRepository<User>();

// Use it
var custRepo = serviceProvider.GetRequiredService<PartitionRepository<Customer>>();
var userRepo = serviceProvider.GetRequiredService<PartitionRepository<User>>();
```

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
var patient = new Patient { PatientId = "patient-123" };
patient.Meta.Add(new PatientMeta { FirstName = "John", Email = "john@example.com" });
patient.Consents.Add(new Consent { Type = "DataSharing" });

await repo.SaveAsync(patient);
//   ✅ All related records saved in ONE batch operation
//   ✅ Automatic retry with Polly resilience
//   ✅ RowKeys auto-generated from patterns
//   ✅ Strong typing, IntelliSense, compile-time safety
```

---

## 💡 Key Features

### Define Your Model with Declarative Patterns
```csharp
[TablePartition("Customers", "{CustomerId}")]
public class Customer
{
    public string CustomerId { get; set; }
    
    [RowKeyPrefix("")]
    public List<Order> Orders { get; set; } = new();
    
    [RowKeyPrefix("")]
    public List<Address> Addresses { get; set; } = new();
}

[RowKeyPattern("{CustomerId}-order-{OrderId}")]
public class Order : RowEntity
{
    public string OrderId { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public decimal Amount { get; set; }
    public string Status { get; set; } = "Pending";
}

[RowKeyPattern("{CustomerId}-address-{AddressId}")]
public class Address : RowEntity
{
    public string AddressId { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string City { get; set; } = default!;
}
```

**Benefits:**
- ✅ Self-documenting - pattern visible at a glance
- ✅ 60% less code than manual implementation
- ✅ Automatic key generation from properties
- ✅ Type-safe and compile-time validated



### CRUD Operations
```csharp
// Create
var customer = new Customer { CustomerId = "cust-123" };
customer.Orders.Add(new Order { Amount = 99.99m, Status = "Pending" });
await repo.SaveAsync(customer);

// Read
var customer = await repo.FindAsync("cust-123");
var orders = await repo.QueryCollectionAsync("cust-123", c => c.Orders);

// Update
loaded.Orders[0].Status = "Shipped";
await repo.SaveAsync(loaded);

// Delete
await repo.DeleteAsync("cust-123");
```

### Automatic Batch Transactions
```csharp
var customer = new Customer { CustomerId = "cust-123" };
customer.Orders.Add(new Order { Amount = 99.99m });
customer.Orders.Add(new Order { Amount = 45.50m });
customer.Addresses.Add(new Address { City = "Seattle" });

await repo.SaveAsync(customer);
//   ✅ All entities saved in ONE atomic batch operation
//   ✅ Either all succeed or all fail (within partition)
//   ✅ Up to 100 operations per batch
//   ✅ Automatic grouping by partition key
```

### Built-in Resilience with Polly
```csharp
// Automatic retry on transient failures
await repo.SaveAsync(customer);
//   ✅ Retries on network errors
//   ✅ Exponential backoff
//   ✅ Circuit breaker protection
```

**You don't need to:**
- ❌ Write retry logic
- ❌ Handle transient failures
- ❌ Implement exponential backoff
- ❌ Track failed operations

### Handles Big Data with Automatic Rollback
PartiTables automatically handles datasets larger than Azure's 100-item batch limit with **automatic rollback** if any batch fails:

```csharp
// Save 10,000 records across 100 batches
var salesData = GenerateSalesData("store-001", 10_000);

await repo.SaveAsync(salesData);
//   ✅ Automatically split into 100-item batches
//   ✅ If ANY batch fails, ALL previous batches are rolled back
//   ✅ Your data stays consistent - it's all-or-nothing!
```

**What happens:**
1. **Automatic batching** - Splits into 100-item batches
2. **Sequential submission** - Submits batches one at a time
3. **Rollback on failure** - If any batch fails, previous batches are automatically deleted
4. **Exception preserved** - Original error is re-thrown after cleanup

**Benefits:**
- ✅ **No partial data** - Either all items save or none do
- ✅ **Unlimited scale** - Handle 10,000+ items automatically
- ✅ **Zero configuration** - Works out of the box

---

## 🔍 Powerful Querying

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

### Query Best Practices
- ✅ Query within a single partition for best performance
- ✅ Use prefix-based queries for time-series data
- ✅ Load only the collections you need
- ✅ Avoid cross-partition queries when possible
- ✅ Filter in memory for small result sets

---

## 🏗️ How It Works

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
✅ Batch Transaction to Table Storage
PartitionKey: clinic-001
   patient-123-meta          (Auto-generated from pattern)
   patient-123-consent-a7b8  (Auto-generated from pattern)
   patient-123-device-001    (Auto-generated from pattern)

✅ All saved atomically in one batch
✅ Automatic retry on failure
✅ Optimistic concurrency handled
```

**Single Partition (Fast Queries):**
```
PartitionKey: customer-123
RowKeys:
   customer-123-order-001
   customer-123-order-002
   customer-123-address-001
```

**Multi-Tenant Isolation:**
```
PartitionKey: tenant-789
RowKeys:
   tenant-789-user-001
   tenant-789-user-002
   tenant-789-setting-001
```

---

## 🎯 Perfect For

📱 Multi-tenant SaaS | 🛒 Customer orders & history | 🏥 Healthcare records  
📋 Audit logs | 👤 User profiles | 🌐 IoT device data  
📊 Time-series metrics | 🔔 Notification queues | 📦 Inventory tracking  
🎫 Event ticketing | 💬 Chat history | 🔐 Session management

---

## 🧪 Try It Now


**Interactive demos:**
```bash
cd PartiSample
dotnet run
```

Choose from 6 interactive demos showing real-world scenarios.

---

## 📚 Documentation

- [Get Started Example](PartiSample.GetStarted/Program.cs) - Simple setup
- [Sample Demos](PartiSample.Demos/) - Real-world examples
- [Quick Start Guide](PartiSample.Demos/README.md) - Get started
- [Big Data Tests](PartiTables.IntegrationTests/BigDataDemoTests.cs) - 10,000+ item examples

---

## ⚙️ Requirements

- .NET 8.0+
- Azure Storage or Azurite (local development)

---

## 📜 License

MIT License — © 2025 

---

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.
