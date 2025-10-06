# PartiTables Demo Application

A comprehensive sample application demonstrating **PartiTables** - a powerful library for working with Azure Table Storage using partition-centric patterns.

##    Quick Start

```bash
# Run Azurite (Azure Storage Emulator)
azurite

# Run the demo application
cd PartiSample
dotnet run
```

##    What's Included

This demo application showcases **6 comprehensive examples** organized by complexity and use case:

| Demo | Description | Complexity | Tables |
|------|-------------|------------|--------|
| **1. Simple Fluent API** | Low-level API without models |   Beginner | 1 |
| **2. Healthcare Patients** | Medical records with declarative patterns |    Intermediate | 1 |
| **3. E-commerce Orders** | Customer orders and profiles |    Intermediate | 1 |
| **4. Multi-Table SaaS** | Enterprise tenant management |     Advanced | 3 |
| **5. Security & Auth** | Authentication & authorization |     Advanced | 2 |
| **6. CRUD & Query Basics** | Complete operations guide |   Beginner | 1 |

##   Key Features Demonstrated

### Basic Patterns
- CRUD operations (Create, Read, Update, Delete)
- Fluent API for dynamic scenarios
- Strongly-typed entity models
- Declarative RowKey patterns with `[RowKeyPattern]`
- Query by prefix patterns
- Batch operations

### Advanced Patterns
- Multi-table coordination
- Collection-specific queries
- Cross-table analytics
- Hierarchical data structures
- Time-sorted entities
- Security and audit patterns

##    Learning Path

### Beginners Start Here
1. **Demo 1** - Simple Fluent API
   - Understand basic concepts
   - Learn PartitionKey and RowKey
   - Try manual key construction

2. **Demo 6** - CRUD & Query Basics
   - Master all operations
   - Learn query patterns
   - Practice with real scenarios

### Intermediate Developers
3. **Demo 2** - Healthcare Patients
   - Work with domain models
   - Use `[RowKeyPattern]` attribute
   - Handle complex relationships

4. **Demo 3** - E-commerce Orders
   - Apply patterns to different domain
   - Manage customer data
   - Optimize queries

### Advanced Topics
5. **Demo 4** - Multi-Table SaaS
   - Coordinate multiple tables
   - Build enterprise architecture
   - Implement audit logging

6. **Demo 5** - Security & Auth
   - Separate authentication/authorization
   - Track security events
   - Manage tokens and permissions

##    Key Concepts

### Partition-Centric Design
PartiTables uses a **partition-first approach** where:
- All related data shares a PartitionKey
- Queries within a partition are fast
- RowKeys organize data within partitions

```csharp
// Example: All patient data in one partition
PartitionKey: "clinic-001"
RowKeys:
  - "patient-123-meta"
  - "patient-123-consent-c001-v1"
  - "patient-123-device-dev001"
```

### Declarative RowKey Patterns
Use the `[RowKeyPattern]` attribute for automatic key generation:

```csharp
[RowKeyPattern("{PatientId}-consent-{ConsentId}-v{Version}")]
public class Consent : RowEntity
{
    public string ConsentId { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public int Version { get; set; } = 1;
    public string Type { get; set; } = "Required";
}
```

**Benefits:**
-   Self-documenting - pattern visible at class level
-   60% less code than manual implementation
-   Automatic key generation from properties
-   Compile-time validated

### Efficient Queries
Load only what you need:

```csharp
// Load entire partition (slower)
var patient = await repo.FindAsync(partitionKey);

// Load specific collection (faster!)
var consents = await repo.QueryCollectionAsync(
    partitionKey, 
    p => p.Consents
);
```

##     Technologies Used

- **.NET 8** - Latest framework
- **Azure Table Storage** - NoSQL storage
- **Azurite** - Local storage emulator
- **PartiTables** - Partition-centric library
- **Polly** - Resilience policies

##    Documentation

Each demo folder contains:
- **README.md** - Detailed explanation
- **Code examples** - Production-ready patterns
- **Best practices** - Do's and don'ts
- **Use cases** - When to use each pattern

##    Development Setup

### Prerequisites
- .NET 8 SDK
- Azurite (Azure Storage Emulator)
- Visual Studio 2022 or VS Code

### Running Locally

1. **Start Azurite**
   ```bash
   azurite
   ```

2. **Run Application**
   ```bash
   cd PartiSample
   dotnet run
   ```

3. **Select Demo**
   - Choose from menu (1-6)
   - Follow console output
   - Check Azure Storage Explorer

### Verifying Results

Use **Azure Storage Explorer** or **Azurite Explorer** to:
- View created tables
- Inspect partition keys
- Examine row keys
- Query data directly

##    Code Style

### Comments
- **Clear and concise** - No unnecessary verbosity
- **Visual separators** - Easy to scan
- **Explanatory** - Why, not what
- **Examples** - Show expected output

### Organization
- **One concept per demo** - Focused learning
- **Progressive complexity** - Build on knowledge
- **Real-world scenarios** - Practical examples
- **Consistent patterns** - Easy to understand

##    Contributing

Improvements welcome! Consider:
- Additional demo scenarios
- Better comments and documentation
- Performance optimizations
- Bug fixes

##    License

This demo application is provided as-is for educational purposes.

##    Related Links

- [PartiTables Library](../PartiTables/)
- [Azure Table Storage Documentation](https://docs.microsoft.com/azure/storage/tables/)
- [Best Practices Guide](../docs/best-practices.md)

---

**Happy Learning!**   

Start with Demo 1 if you're new, or jump to any demo that interests you.
