# Demo 2: Healthcare Patient Management

## Overview
This demo shows **strongly-typed entity models** with **declarative RowKey patterns** using attributes.

## What You'll Learn
- Create type-safe entity models
- Use `[RowKeyPattern]` attribute for automatic key generation
- Query specific collections efficiently
- Manage one-to-many relationships
- Handle complex domain models

## Domain Model
```
Patient (Partition)
    Meta (1 record) - Demographics
    Consents (many) - HIPAA compliance
    Devices (many) - Linked wearables
```

## RowKey Patterns
| Entity | Pattern | Example |
|--------|---------|---------|
| PatientMeta | `{PatientId}-meta` | `patient-456-meta` |
| Consent | `{PatientId}-consent-{ConsentId}-v{Version}` | `patient-456-consent-a7b8c9-v1` |
| DeviceLink | `{PatientId}-device-{DeviceId}` | `patient-456-device-fitbit-001` |

## Key Features

### Declarative RowKey Patterns
```csharp
[RowKeyPattern("{PatientId}-consent-{ConsentId}-v{Version}")]
public class Consent : RowEntity
{
    public string ConsentId { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public int Version { get; set; } = 1;
    public string Type { get; set; } = "Required";
    public string Status { get; set; } = "Granted";
}
```

**Benefits:**
- ? Self-documenting - pattern is visible at a glance
- ? 60% less code than manual implementation
- ? Compile-time validated
- ? Automatic key generation from properties

### Type-Safe Operations
```csharp
var patient = new Patient { PatientId = "patient-456" };
patient.Meta.Add(new PatientMeta { FirstName = "Jane" });
await repo.SaveAsync(patient);
```

### Efficient Collection Queries
```csharp
// Load only consents (faster than loading entire patient)
var consents = await repo.QueryCollectionAsync(tenantId, p => p.Consents);
```

## Benefits
? IntelliSense and compile-time safety  
? No manual RowKey construction  
? Easy to maintain and refactor  
? Perfect for complex domains  

## Next Steps
? **Demo 3** shows the same pattern in an e-commerce context
