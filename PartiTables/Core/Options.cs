using Polly;

namespace PartiTables;

/// <summary>
/// Configuration options for PartiTables.
/// </summary>
public sealed class TableOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public IAsyncPolicy? ResiliencePolicy { get; set; }
    public bool CreateTableIfNotExists { get; set; } = true;

    internal void Validate()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
            throw new ConfigurationException("ConnectionString is required.");

        if (string.IsNullOrWhiteSpace(TableName))
            throw new ConfigurationException("TableName is required.");

        if (!IsValidTableName(TableName))
            throw new ConfigurationException(
                $"Invalid table name '{TableName}'. Must be 3-63 characters, start with letter, alphanumeric only.");
    }

    private static bool IsValidTableName(string tableName)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            return false;

        if (tableName.Length < 3 || tableName.Length > 63)
            return false;

        if (!char.IsLetter(tableName[0]))
            return false;

        return tableName.All(char.IsLetterOrDigit);
    }
}