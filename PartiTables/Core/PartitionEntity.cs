using Azure;
using Azure.Data.Tables;

namespace PartiTables;

/// <summary>
/// Static factory for creating TableEntity instances with fluent API support.
/// </summary>
public static class PartitionEntity
{
    public static TableEntity Create(string partitionKey, string rowKey)
    {
        return new TableEntity(partitionKey, rowKey);
    }
}
