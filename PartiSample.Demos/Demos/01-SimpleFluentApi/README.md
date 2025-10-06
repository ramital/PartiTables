# Demo 1: Simple Fluent API

## Overview
This demo shows the **low-level fluent API** for working directly with Azure Table Storage without entity models.

## What You'll Learn
- Direct entity creation with `PartitionEntity.Create()`
- Manual RowKey construction and management
- Basic CRUD operations (Create, Read, Update, Delete)
- Query by prefix pattern
- Batch operations

## When to Use This Approach
? **Good for:**
- Quick prototypes
- Simple key-value scenarios
- Direct control over storage structure
- Dynamic schemas

? **Avoid for:**
- Complex domain models
- Type-safe operations
- Auto-generated keys

## Key Code Patterns

### Create an Entity
```csharp
var entity = PartitionEntity.Create(partitionKey, rowKey)
    .Set("PropertyName", value)
    .Set("AnotherProperty", anotherValue);
    
await client.UpsertAsync(entity);
```

### Query by Prefix
```csharp
var results = await client.QueryByPrefixAsync(partitionKey, "prefix-");
```

### Batch Operations
```csharp
var batch = new PartitionBatch(partitionKey);
batch.Upsert(entity1);
batch.Upsert(entity2);
await client.SubmitAsync(batch);
```

## Next Steps
?? Check out **Demo 2** for strongly-typed entity models
