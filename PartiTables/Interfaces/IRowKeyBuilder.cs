namespace PartiTables.Interfaces;

/// <summary>
/// Interface for entities that can generate their own row keys.
/// </summary>
public interface IRowKeyBuilder
{
    string BuildRowKey(RowKeyContext context);
}

/// <summary>
/// Context information available when building row keys.
/// </summary>
public class RowKeyContext
{
    public object ParentEntity { get; }
    public string Prefix { get; }
    public string PartitionKey { get; }

    public RowKeyContext(object parentEntity, string prefix, string partitionKey)
    {
        ParentEntity = parentEntity;
        Prefix = prefix;
        PartitionKey = partitionKey;
    }

    public TValue? GetParentProperty<TValue>(string propertyName)
    {
        var prop = ParentEntity.GetType().GetProperty(propertyName);
        if (prop == null) return default;
        
        var value = prop.GetValue(ParentEntity);
        return value is TValue typedValue ? typedValue : default;
    }
}
