using Azure.Data.Tables;

namespace PartiTables;

/// <summary>
/// Batch operations for a single partition. Max 100 operations per batch.
/// </summary>
public sealed class PartitionBatch
{
    private const int MaxBatchSize = 100;
    
    public string PartitionKey { get; }
    private readonly List<TableTransactionAction> _actions = new();
    private readonly HashSet<string> _rowKeys = new();

    public int Count => _actions.Count;

    public PartitionBatch(string partitionKey)
    {
        if (string.IsNullOrWhiteSpace(partitionKey))
            throw new ArgumentException("Partition key cannot be null or empty.", nameof(partitionKey));
        
        PartitionKey = partitionKey;
    }

    /// <summary>
    /// Adds an upsert (insert or update) operation. Replaces existing row key if already in batch.
    /// </summary>
    public void Upsert(ITableEntity entity, TableUpdateMode mode = TableUpdateMode.Merge)
    {
        ValidateEntity(entity);
        
        if (_rowKeys.Contains(entity.RowKey))
        {
            for (int i = _actions.Count - 1; i >= 0; i--)
            {
                if (_actions[i].Entity.RowKey == entity.RowKey)
                {
                    _actions.RemoveAt(i);
                    break;
                }
            }
        }
        else
        {
            _rowKeys.Add(entity.RowKey);
        }
        
        _actions.Add(new TableTransactionAction(
            mode == TableUpdateMode.Replace 
                ? TableTransactionActionType.UpsertReplace 
                : TableTransactionActionType.UpsertMerge, 
            entity));
        
        EnsureLimit();
    }

    /// <summary>
    /// Adds an insert operation. Fails if entity already exists.
    /// </summary>
    public void Insert(ITableEntity entity)
    {
        ValidateEntity(entity);
        CheckForDuplicateRowKey(entity.RowKey);
        _actions.Add(new TableTransactionAction(TableTransactionActionType.Add, entity));
        _rowKeys.Add(entity.RowKey);
        EnsureLimit();
    }

    /// <summary>
    /// Adds an update operation. Fails if entity doesn't exist.
    /// </summary>
    public void Update(ITableEntity entity, TableUpdateMode mode = TableUpdateMode.Merge)
    {
        ValidateEntity(entity);
        CheckForDuplicateRowKey(entity.RowKey);
        _actions.Add(new TableTransactionAction(
            mode == TableUpdateMode.Replace 
                ? TableTransactionActionType.UpdateReplace 
                : TableTransactionActionType.UpdateMerge, 
            entity));
        _rowKeys.Add(entity.RowKey);
        EnsureLimit();
    }

    /// <summary>
    /// Adds a delete operation. Replaces existing row key if already in batch.
    /// </summary>
    public void Delete(ITableEntity entity)
    {
        ValidateEntity(entity);
        
        if (_rowKeys.Contains(entity.RowKey))
        {
            for (int i = _actions.Count - 1; i >= 0; i--)
            {
                if (_actions[i].Entity.RowKey == entity.RowKey)
                {
                    _actions.RemoveAt(i);
                    break;
                }
            }
        }
        else
        {
            _rowKeys.Add(entity.RowKey);
        }
        
        _actions.Add(new TableTransactionAction(TableTransactionActionType.Delete, entity));
        EnsureLimit();
    }

    /// <summary>
    /// Adds a delete operation by row key. Replaces existing row key if already in batch.
    /// </summary>
    public void Delete(string rowKey)
    {
        if (string.IsNullOrWhiteSpace(rowKey))
            throw new ArgumentException("Row key cannot be null or empty.", nameof(rowKey));

        if (_rowKeys.Contains(rowKey))
        {
            for (int i = _actions.Count - 1; i >= 0; i--)
            {
                if (_actions[i].Entity.RowKey == rowKey)
                {
                    _actions.RemoveAt(i);
                    break;
                }
            }
        }
        else
        {
            _rowKeys.Add(rowKey);
        }
        
        var entity = new TableEntity(PartitionKey, rowKey);
        _actions.Add(new TableTransactionAction(TableTransactionActionType.Delete, entity));
        EnsureLimit();
    }

    public void Clear()
    {
        _actions.Clear();
        _rowKeys.Clear();
    }

    private void ValidateEntity(ITableEntity entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        if (entity.PartitionKey != PartitionKey)
            throw new InvalidPartitionKeyException(
                $"Entity partition key '{entity.PartitionKey}' does not match batch partition key '{PartitionKey}'.");
        
        if (string.IsNullOrWhiteSpace(entity.RowKey))
            throw new ArgumentException("Entity RowKey cannot be null or empty.", nameof(entity));
        
        ValidateRowKeyCharacters(entity.RowKey);
    }

    private void CheckForDuplicateRowKey(string rowKey)
    {
        if (_rowKeys.Contains(rowKey))
        {
            throw new InvalidOperationException(
                $"Duplicate row key '{rowKey}' detected in batch. Each entity must have a unique row key.");
        }
    }

    private void ValidateRowKeyCharacters(string rowKey)
    {
        var invalidChars = new[] { '/', '\\', '#', '?' };
        
        if (rowKey.Any(c => invalidChars.Contains(c) || char.IsControl(c)))
        {
            throw new ArgumentException(
                $"Row key '{rowKey}' contains invalid characters (/, \\, #, ? or control characters).", 
                nameof(rowKey));
        }
        
        if (System.Text.Encoding.UTF8.GetByteCount(rowKey) > 1024)
        {
            throw new ArgumentException($"Row key '{rowKey}' exceeds maximum size of 1KB.", nameof(rowKey));
        }
    }

    private void EnsureLimit()
    {
        if (_actions.Count > MaxBatchSize)
            throw new BatchLimitExceededException(_actions.Count, MaxBatchSize);
    }

    internal IReadOnlyList<TableTransactionAction> Actions => _actions;
}