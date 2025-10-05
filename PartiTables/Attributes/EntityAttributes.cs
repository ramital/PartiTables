namespace PartiTables;

/// <summary>
/// Marks a class as a table partition entity. Specifies the table name and partition key template.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class TablePartitionAttribute : Attribute
{
    public string TableName { get; }
    public string PartitionKeyTemplate { get; }

    public TablePartitionAttribute(string tableName, string partitionKeyTemplate)
    {
        TableName = tableName;
        PartitionKeyTemplate = partitionKeyTemplate;
    }
}

/// <summary>
/// Marks a collection property with a row key prefix for grouping related entities.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class RowKeyPrefixAttribute : Attribute
{
    public string Prefix { get; }
    public string? IdPropertyName { get; }

    public RowKeyPrefixAttribute(string prefix, string? idPropertyName = null)
    {
        Prefix = prefix;
        IdPropertyName = idPropertyName;
    }
}
