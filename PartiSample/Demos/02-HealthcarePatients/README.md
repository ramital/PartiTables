# Demo 2: Healthcare Patient Management

## Overview
This demo shows **strongly-typed entity models** similar to Entity Framework, with automatic RowKey generation.

## What You'll Learn
- Create type-safe entity models
- Implement `IRowKeyBuilder` for automatic key generation
- Query specific collections efficiently
- Manage one-to-many relationships
- Handle complex domain models

## Domain Model
```
Patient (Partition)
??? Meta (1 record) - Demographics
??? Consents (many) - HIPAA compliance
??? Devices (many) - Linked wearables
```

## RowKey Patterns
| Entity | Pattern | Example |
|--------|---------|---------|
| PatientMeta | `{patientId}-meta` | `patient-456-meta` |
| Consent | `{patientId}-consent-{id}-v{ver}` | `patient-456-consent-a7b8c9-v1` |
| DeviceLink | `{patientId}-device-{deviceId}` | `patient-456-device-dev-fitbit-001` |

## Key Features

### Auto-Generated RowKeys
```csharp
public class Consent : RowEntity, IRowKeyBuilder
{
    public string BuildRowKey(RowKeyContext context)
    {
        var patientId = context.GetParentProperty<string>("PatientId");
        return $"{patientId}-consent-{ConsentId}-v{Version}";
    }
}
```

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
?? **Demo 3** shows the same pattern in an e-commerce context
