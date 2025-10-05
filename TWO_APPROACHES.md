# PartiTables - Two Approaches for Azure Table Storage

A flexible library offering **two ways** to work with Azure Table Storage:
1. **Simple Fluent API** - No models, just keys and values
2. **Strongly-Typed Models** - EF-like entities with LINQ support

Choose the approach that fits your needs!

---

## ?? Approach 1: Simple Fluent API (No Models)

Perfect for quick prototypes, simple data, or when you want maximum flexibility.

### Quick Example

```csharp
var services = new ServiceCollection();
services.AddPartiTablesForDevelopment("MyData");
var client = services.BuildServiceProvider().GetRequiredService<IPartitionClient>();

// CREATE
var entity = PartitionEntity.Create("tenant-001", "customer-123")
    .Set("Name", "John Doe")
    .Set("Email", "john@example.com")
    .Set("Points", 1500);

await client.UpsertAsync(entity);

// READ
var found = await client.TryGetAsync("tenant-001", "customer-123");
Console.WriteLine(found.GetString("Name")); // "John Doe"

// QUERY by prefix
var customers = await client.QueryByPrefixAsync("tenant-001", "customer-");

// BATCH
var batch = new PartitionBatch("tenant-001");
batch.Upsert(entity1);
batch.Upsert(entity2);
await client.SubmitAsync(batch);
```

**Pros:**
- ? Zero boilerplate
- ? Maximum flexibility
- ? Schema-less
- ? Fast to write

**Cons:**
- ? No compile-time safety
- ? No IntelliSense for properties

---

## ?? Approach 2: Strongly-Typed Models (EF-Like)

Perfect for complex domains, team projects, or when you want type safety and IntelliSense.

### Define Your Model

```csharp
using PartiTables;

[TablePartition("PatientData", "{TenantId}")]
public class Patient
{
    public string TenantId { get; set; } = default!;
    public string PatientId { get; set; } = default!;

    [RowKeyPrefix("meta-")]
    public List<PatientMeta> Meta { get; set; } = new();

    [RowKeyPrefix("consent-")]
    public List<Consent> Consents { get; set; } = new();

    [RowKeyPrefix("device-")]
    public List<DeviceLink> Devices { get; set; } = new();

    public void BindRowKeys()
    {
        if (Meta.Count == 0) Meta.Add(new PatientMeta());
        
        foreach (var meta in Meta)
            meta.RowKeyId = $"meta-{PatientId}";
        
        foreach (var consent in Consents)
            consent.RowKeyId = $"consent-{PatientId}-{consent.ConsentId}-v{consent.Version}";
        
        foreach (var device in Devices)
            device.RowKeyId = $"device-{PatientId}-{device.DeviceId}";
    }
}

public class PatientMeta : RowEntity
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? Status { get; set; }
}

public class Consent : RowEntity
{
    public string ConsentId { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public int Version { get; set; } = 1;
    public string Type { get; set; } = "Required";
    public string Status { get; set; } = "Granted";
}

public class DeviceLink : RowEntity
{
    public string DeviceId { get; set; } = default!;
    public string? Model { get; set; }
    public string? Manufacturer { get; set; }
}
```

### Use Your Model

```csharp
// Setup
services.AddPartiTables(opts => {
    opts.ConnectionString = "UseDevelopmentStorage=true";
    opts.TableName = "PatientData";
});
services.AddPartitionRepository<Patient>();

var repo = sp.GetRequiredService<PartitionRepository<Patient>>();

// CREATE - Build entity with IntelliSense!
var patient = new Patient
{
    TenantId = "clinic-001",
    PatientId = "patient-123"
};

patient.Meta.Add(new PatientMeta
{
    FirstName = "John",
    LastName = "Doe",
    Email = "john@example.com",
    Status = "Active"
});

patient.Consents.Add(new Consent
{
    Type = "DataSharing",
    Status = "Granted"
});

patient.Devices.Add(new DeviceLink
{
    DeviceId = "fitbit-001",
    Model = "Charge 5",
    Manufacturer = "FitBit"
});

patient.BindRowKeys(); // Generate row keys
await repo.SaveAsync(patient); // Saves all rows in one batch!

// READ - Load entire entity
var loaded = await repo.FindAsync("clinic-001");
Console.WriteLine($"Patient: {loaded.Meta[0].FirstName} {loaded.Meta[0].LastName}");
Console.WriteLine($"Consents: {loaded.Consents.Count}");
Console.WriteLine($"Devices: {loaded.Devices.Count}");

// QUERY COLLECTION - Fast prefix query!
var consents = await repo.QueryCollectionAsync("clinic-001", p => p.Consents);
foreach (var consent in consents)
{
    Console.WriteLine($"{consent.Type}: {consent.Status}");
}

// UPDATE
loaded.Meta[0].Status = "Verified";
loaded.Consents.Add(new Consent { Type = "Marketing", Status = "Denied" });
loaded.BindRowKeys();
await repo.SaveAsync(loaded);
```

**Pros:**
- ? Compile-time type safety
- ? Full IntelliSense support
- ? Clear domain model
- ? Collection properties map to row prefixes
- ? Fast queries with `QueryCollectionAsync`
- ? Automatic batching

**Cons:**
- ? Requires model definitions
- ? Need to call `BindRowKeys()` before save

---

## ?? Comparison

| Feature | Simple Fluent | Strongly-Typed |
|---------|---------------|----------------|
| **Setup Time** | 30 seconds | 5-10 minutes |
| **Code Lines** | Fewer | More (models) |
| **Type Safety** | ? No | ? Yes |
| **IntelliSense** | ? Limited | ? Full |
| **Flexibility** | ? Maximum | ?? Schema-based |
| **Refactoring** | ? Hard | ? Easy |
| **Team Projects** | ? Risky | ? Great |
| **Learning Curve** | ? 5 min | ?? 15 min |
| **Performance** | ? Fast | ? Fast |

---

## ?? Which Approach Should You Use?

### Use **Simple Fluent API** when:
- ? Rapid prototyping
- ? Simple CRUD operations
- ? Schema changes frequently
- ? Small projects or scripts
- ? You want minimal code

### Use **Strongly-Typed Models** when:
- ? Complex domain models
- ? Team projects (need IntelliSense)
- ? You want compile-time safety
- ? Multiple related collections per entity
- ? Long-term maintainability matters

### Use **Both** when:
- ? Most of your entities are complex (use models)
- ? Some operations are simple (use fluent API)
- ? You can mix and match in the same project!

---

## ?? Key Concepts

### Partition Key Strategy

Both approaches use the same partition key strategy:

```
Partition Key: "tenant-001" (groups all data for one tenant)

Row Keys:
  meta-patient-123
  consent-patient-123-c001-v1
  consent-patient-123-c002-v1
  device-patient-123-dev001
```

### Row Key Prefixes

Enables efficient queries:

```csharp
// Get all consents for a patient
var consents = await client.QueryByPrefixAsync("tenant-001", "consent-patient-123-");

// Or with strongly-typed:
var consents = await repo.QueryCollectionAsync("tenant-001", p => p.Consents);
```

### Attributes

**`[TablePartition]`** - Defines table name and partition key template
```csharp
[TablePartition("TableName", "{PropertyName}")]
```

**`[RowKeyPrefix]`** - Maps collection to row key prefix
```csharp
[RowKeyPrefix("consent-")]
public List<Consent> Consents { get; set; }
```

---

## ?? Installation & Setup

```csharp
var services = new ServiceCollection();

// Configure PartiTables
services.AddPartiTables(opts => {
    opts.ConnectionString = "YOUR_CONNECTION_STRING";
    opts.TableName = "YourTable";
    opts.ResiliencePolicy = Policy
        .Handle<Exception>()
        .WaitAndRetryAsync(3, i => TimeSpan.FromMilliseconds(100 * i));
});

// For simple API - just inject IPartitionClient
var client = sp.GetRequiredService<IPartitionClient>();

// For strongly-typed - register repository
services.AddPartitionRepository<YourEntity>();
var repo = sp.GetRequiredService<PartitionRepository<YourEntity>>();
```

---

## ?? Complete Examples

Run the demo app and choose which approach to see:

```bash
dotnet run --project PatientPartitionSample
```

Output:
```
=== PartiTables Demo ===

Choose demo:
1. Simple fluent API (no models)
2. Strongly-typed models (EF-like)

Enter choice (1 or 2):
```

---

## ?? Pro Tips

### 1. Mixing Both Approaches

```csharp
// Use strongly-typed for main entities
var patient = await patientRepo.FindAsync(tenantId);

// Use fluent API for quick one-offs
await client.UpsertAsync(
    PartitionEntity.Create(tenantId, "audit-log-001")
        .Set("Action", "Login")
        .Set("Timestamp", DateTime.UtcNow)
);
```

### 2. Batch Performance

```csharp
// Strongly-typed automatically batches on SaveAsync()
patient.BindRowKeys();
await repo.SaveAsync(patient); // One batch for all rows!

// Fluent API requires manual batching
var batch = new PartitionBatch(tenantId);
batch.Upsert(entity1);
batch.Upsert(entity2);
await client.SubmitAsync(batch);
```

### 3. Query Performance

Both approaches use the same underlying prefix queries, so performance is identical:

```csharp
// Fluent
var consents = await client.QueryByPrefixAsync(tenantId, "consent-patient-123-");

// Strongly-typed
var consents = await repo.QueryCollectionAsync(tenantId, p => p.Consents);
```

---

## ?? Best of Both Worlds!

PartiTables gives you flexibility:
- Start with **simple fluent API** for rapid development
- Migrate to **strongly-typed models** as your domain solidifies
- Mix and match both approaches in the same project

Choose what works best for each use case! ??
