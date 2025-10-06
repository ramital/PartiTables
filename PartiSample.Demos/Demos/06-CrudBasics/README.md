# Demo 6: CRUD & Query Basics

## Overview
This demo provides a **comprehensive guide** to all basic operations with PartiTables.

## What You'll Learn
- **C**reate - Insert new entities and collections
- **R**ead - Load full partitions or specific collections
- **U**pdate - Modify existing data
- **D**elete - Remove entities
- **Query** - Filter, sort, aggregate data

## Domain Model
```
TaskProject (Partition)
??? Tasks (many) - Project tasks
??? Comments (many) - Task comments
??? Attachments (many) - File attachments
```

## Operations Covered

### CREATE
```csharp
var project = new TaskProject { ProjectId = "project-alpha" };
project.Tasks.Add(new ProjectTask { Title = "Setup environment" });
await repo.SaveAsync(project);
```

### READ
```csharp
// Load entire partition
var project = await repo.FindAsync(projectId);

// Load specific collection (faster!)
var tasks = await repo.QueryCollectionAsync(projectId, p => p.Tasks);
```

### UPDATE
```csharp
var project = await repo.FindAsync(projectId);
project.Tasks[0].Status = "Done";
await repo.SaveAsync(project);
```

### DELETE
```csharp
await repo.DeleteAsync(projectId);
```

### QUERY
```csharp
// Filter by status
var doneTasks = project.Tasks.Where(t => t.Status == "Done");

// Filter by priority
var highPriority = project.Tasks.Where(t => 
    t.Priority == "High" || t.Priority == "Critical");

// Group by status
var grouped = project.Tasks.GroupBy(t => t.Status);

// Aggregations
var totalHours = project.Tasks.Sum(t => t.EstimatedHours);
var progress = (completedHours * 100.0) / totalHours;

// Complex filters
var urgent = project.Tasks.Where(t =>
    t.Priority == "Critical" &&
    t.Status != "Done" &&
    t.DueDate < DateTime.Now);
```

## Query Patterns Demonstrated
1. ? Group by (status, priority, assignee)
2. ? Filter by property
3. ? Date-based filtering
4. ? Array contains (tags)
5. ? Related entity queries
6. ? Collection-specific queries
7. ? Aggregations (sum, count, average)
8. ? Multi-condition filters
9. ? Sorting and limiting
10. ? Join-like patterns

## Key Insights
? All queries work on in-memory data after partition load  
? Query specific collections for better performance  
? LINQ provides powerful filtering and aggregation  
? Multiple entity types share the same partition  

## Best Practices
- Load only what you need (use `QueryCollectionAsync`)
- Use LINQ for complex queries
- Keep related data in the same partition
- Use meaningful RowKey patterns

