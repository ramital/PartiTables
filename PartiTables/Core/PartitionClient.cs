using Azure;
using Azure.Data.Tables;
using PartiTables.Interfaces;
using Polly;

namespace PartiTables;

/// <summary>
/// Client for partition-scoped operations on Azure Table Storage.
/// </summary>
public sealed class PartitionClient : IPartitionClient
{
    private readonly TableClient _table;
    private readonly IAsyncPolicy? _policy;

    public PartitionClient(TableClient tableClient, IAsyncPolicy? policy = null)
    {
        _table = tableClient ?? throw new ArgumentNullException(nameof(tableClient));
        _policy = policy;
    }

    /// <summary>
    /// Gets all entities in a partition.
    /// </summary>
    public async Task<IReadOnlyList<TableEntity>> GetPartitionAsync(string partitionKey, CancellationToken ct = default)
    {
        ValidatePartitionKey(partitionKey);
        
        var list = new List<TableEntity>();
        var query = _table.QueryAsync<TableEntity>(x => x.PartitionKey == partitionKey, cancellationToken: ct);
        await foreach (var entity in WrapQuery(query, ct))
        {
            list.Add(entity);
        }
        return list;
    }

    /// <summary>
    /// Gets a single entity. Throws if not found.
    /// </summary>
    public async Task<TableEntity> GetAsync(string partitionKey, string rowKey, CancellationToken ct = default)
    {
        ValidatePartitionKey(partitionKey);
        ValidateRowKey(rowKey);

        var entity = await TryGetAsync(partitionKey, rowKey, ct);
        if (entity == null)
            throw new EntityNotFoundException(partitionKey, rowKey);

        return entity;
    }

    /// <summary>
    /// Tries to get an entity. Returns null if not found.
    /// </summary>
    public async Task<TableEntity?> TryGetAsync(string partitionKey, string rowKey, CancellationToken ct = default)
    {
        ValidatePartitionKey(partitionKey);
        ValidateRowKey(rowKey);

        try
        {
            var response = await ExecuteWithPolicy(
                c => _table.GetEntityAsync<TableEntity>(partitionKey, rowKey, cancellationToken: c), 
                ct);
            return response.Value;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    /// <summary>
    /// Queries entities by row key prefix.
    /// </summary>
    public async Task<IReadOnlyList<TableEntity>> QueryByPrefixAsync(string partitionKey, string rowKeyPrefix, CancellationToken ct = default)
    {
        ValidatePartitionKey(partitionKey);
        
        var (from, to) = RowKeyRange.ForPrefix(rowKeyPrefix ?? string.Empty);
        string filter = TableClient.CreateQueryFilter($"PartitionKey eq {partitionKey} and RowKey ge {from} and RowKey lt {to}");
        
        var list = new List<TableEntity>();
        var query = _table.QueryAsync<TableEntity>(filter: filter, cancellationToken: ct);
        await foreach (var entity in WrapQuery(query, ct))
        {
            list.Add(entity);
        }
        return list;
    }

    public async Task UpsertAsync(ITableEntity entity, TableUpdateMode mode = TableUpdateMode.Merge, CancellationToken ct = default)
    {
        ValidateEntity(entity);
        await ExecuteWithPolicy(c => _table.UpsertEntityAsync(entity, mode, c), ct);
    }

    public async Task InsertAsync(ITableEntity entity, CancellationToken ct = default)
    {
        ValidateEntity(entity);
        await ExecuteWithPolicy(c => _table.AddEntityAsync(entity, c), ct);
    }

    public async Task UpdateAsync(ITableEntity entity, TableUpdateMode mode = TableUpdateMode.Merge, CancellationToken ct = default)
    {
        ValidateEntity(entity);
        await ExecuteWithPolicy(c => _table.UpdateEntityAsync(entity, entity.ETag, mode, c), ct);
    }

    public async Task DeleteAsync(string partitionKey, string rowKey, ETag etag = default, CancellationToken ct = default)
    {
        ValidatePartitionKey(partitionKey);
        ValidateRowKey(rowKey);
        await ExecuteWithPolicy(c => _table.DeleteEntityAsync(partitionKey, rowKey, etag, c), ct);
    }

    /// <summary>
    /// Submits a batch of operations atomically.
    /// </summary>
    public async Task SubmitAsync(PartitionBatch batch, CancellationToken ct = default)
    {
        if (batch is null) 
            throw new ArgumentNullException(nameof(batch));
        
        if (batch.Actions.Count == 0) 
            return;

        await ExecuteWithPolicy(c => _table.SubmitTransactionAsync(batch.Actions, c), ct);
    }

    public async Task<bool> ExistsAsync(string partitionKey, string rowKey, CancellationToken ct = default)
    {
        var entity = await TryGetAsync(partitionKey, rowKey, ct);
        return entity != null;
    }

    private async Task<T> ExecuteWithPolicy<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct)
    {
        if (_policy is null)
            return await action(ct);
        
        return await _policy.ExecuteAsync(action, ct);
    }

    private async IAsyncEnumerable<TableEntity> WrapQuery(
        AsyncPageable<TableEntity> pageable, 
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        if (_policy is null)
        {
            await foreach (var entity in pageable.WithCancellation(ct))
            {
                yield return entity;
            }
        }
        else
        {
            var list = new List<TableEntity>();
            await _policy.ExecuteAsync(async token =>
            {
                await foreach (var entity in pageable.WithCancellation(token))
                {
                    list.Add(entity);
                }
            }, ct);
            
            foreach (var entity in list)
            {
                yield return entity;
            }
        }
    }

    private static void ValidatePartitionKey(string partitionKey)
    {
        if (string.IsNullOrWhiteSpace(partitionKey))
            throw new ArgumentException("Partition key cannot be null or empty.", nameof(partitionKey));
    }

    private static void ValidateRowKey(string rowKey)
    {
        if (string.IsNullOrWhiteSpace(rowKey))
            throw new ArgumentException("Row key cannot be null or empty.", nameof(rowKey));
    }

    private static void ValidateEntity(ITableEntity entity)
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));

        ValidatePartitionKey(entity.PartitionKey);
        ValidateRowKey(entity.RowKey);
    }
}