using Azure;
using Azure.Data.Tables;
using System.Text.Json;

namespace PartiTables;

/// <summary>
/// Base class for row entities within a partition.
/// </summary>
public abstract class RowEntity
{
    public virtual string RowKeyId { get; set; } = default!;

    internal string? _partitionKey;
    internal string? _rowKey;
    internal DateTimeOffset? _timestamp;
    internal ETag _etag;

    internal TableEntity ToTableEntity(string partitionKey)
    {
        var entity = new TableEntity(partitionKey, RowKeyId);
        
        var properties = GetType().GetProperties();
        foreach (var prop in properties)
        {
            if (prop.Name is "RowKeyId" or "_partitionKey" or "_rowKey" or "_timestamp" or "_etag")
                continue;

            var value = prop.GetValue(this);
            if (value != null)
            {
                if (value is DateTime dt && dt.Kind != DateTimeKind.Utc)
                {
                    value = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
                }
                
                if (IsComplexType(prop.PropertyType))
                {
                    entity[prop.Name] = JsonSerializer.Serialize(value);
                }
                else
                {
                    entity[prop.Name] = value;
                }
            }
        }

        return entity;
    }

    internal void FromTableEntity(TableEntity tableEntity)
    {
        _partitionKey = tableEntity.PartitionKey;
        _rowKey = tableEntity.RowKey;
        _timestamp = tableEntity.Timestamp;
        _etag = tableEntity.ETag;
        RowKeyId = tableEntity.RowKey;

        var properties = GetType().GetProperties();
        foreach (var prop in properties)
        {
            if (prop.Name is "RowKeyId" or "_partitionKey" or "_rowKey" or "_timestamp" or "_etag")
                continue;

            if (tableEntity.TryGetValue(prop.Name, out var value) && value != null)
            {
                try
                {
                    if (IsComplexType(prop.PropertyType) && value is string jsonString)
                    {
                        var deserializedValue = JsonSerializer.Deserialize(jsonString, prop.PropertyType);
                        prop.SetValue(this, deserializedValue);
                    }
                    else
                    {
                        var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                        var convertedValue = Convert.ChangeType(value, targetType);
                        prop.SetValue(this, convertedValue);
                    }
                }
                catch
                {
                    // Skip properties that can't be converted
                }
            }
        }
    }

    private static bool IsComplexType(Type type)
    {
        if (type == typeof(string) || 
            type == typeof(byte[]) || 
            type == typeof(bool) || 
            type == typeof(DateTime) || 
            type == typeof(DateTimeOffset) || 
            type == typeof(double) || 
            type == typeof(Guid) || 
            type == typeof(int) || 
            type == typeof(long) ||
            type == typeof(bool?) || 
            type == typeof(DateTime?) || 
            type == typeof(DateTimeOffset?) || 
            type == typeof(double?) || 
            type == typeof(Guid?) || 
            type == typeof(int?) || 
            type == typeof(long?))
        {
            return false;
        }

        return true;
    }
}
