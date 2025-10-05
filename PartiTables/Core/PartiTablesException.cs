namespace PartiTables;

public class PartiTablesException : Exception
{
    public PartiTablesException(string message) : base(message) { }
    public PartiTablesException(string message, Exception inner) : base(message, inner) { }
}

public sealed class InvalidPartitionKeyException : PartiTablesException
{
    public InvalidPartitionKeyException(string message) : base(message) { }
}

public sealed class BatchLimitExceededException : PartiTablesException
{
    public int CurrentCount { get; }
    public int MaxCount { get; }

    public BatchLimitExceededException(int currentCount, int maxCount) 
        : base($"Batch limit exceeded. Current: {currentCount}, Max: {maxCount}")
    {
        CurrentCount = currentCount;
        MaxCount = maxCount;
    }
}

public sealed class EntityNotFoundException : PartiTablesException
{
    public string PartitionKey { get; }
    public string RowKey { get; }

    public EntityNotFoundException(string partitionKey, string rowKey) 
        : base($"Entity not found: PartitionKey='{partitionKey}', RowKey='{rowKey}'")
    {
        PartitionKey = partitionKey;
        RowKey = rowKey;
    }
}

public sealed class ConfigurationException : PartiTablesException
{
    public ConfigurationException(string message) : base(message) { }
}