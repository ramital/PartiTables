using Azure.Data.Tables;

namespace PartiTables;

/// <summary>
/// Extension methods for fluent TableEntity property setting.
/// </summary>
public static class TableEntityExtensions
{
    public static TableEntity Set(this TableEntity entity, string key, object? value)
    {
        entity[key] = value;
        return entity;
    }
}
