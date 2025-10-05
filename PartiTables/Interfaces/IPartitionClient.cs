using Azure;
using Azure.Data.Tables;

namespace PartiTables.Interfaces;

/// <summary>
/// Interface for partition-scoped operations on Azure Table Storage.
/// All operations are scoped to a single partition for optimal performance.
/// </summary>
public interface IPartitionClient
{
    /// <summary>
    /// Retrieves all entities in a partition.
    /// </summary>
    /// <param name="partitionKey">The partition key.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A read-only list of entities in the partition.</returns>
    Task<IReadOnlyList<TableEntity>> GetPartitionAsync(string partitionKey, CancellationToken ct = default);

    /// <summary>
    /// Retrieves a single entity by its partition and row key.
    /// </summary>
    /// <param name="partitionKey">The partition key.</param>
    /// <param name="rowKey">The row key.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The entity if found.</returns>
    /// <exception cref="EntityNotFoundException">Thrown when the entity is not found.</exception>
    Task<TableEntity> GetAsync(string partitionKey, string rowKey, CancellationToken ct = default);

    /// <summary>
    /// Attempts to retrieve a single entity by its partition and row key.
    /// </summary>
    /// <param name="partitionKey">The partition key.</param>
    /// <param name="rowKey">The row key.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The entity if found, null otherwise.</returns>
    Task<TableEntity?> TryGetAsync(string partitionKey, string rowKey, CancellationToken ct = default);

    /// <summary>
    /// Queries entities by row key prefix within a partition.
    /// </summary>
    /// <param name="partitionKey">The partition key.</param>
    /// <param name="rowKeyPrefix">The row key prefix to filter by.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A read-only list of matching entities.</returns>
    Task<IReadOnlyList<TableEntity>> QueryByPrefixAsync(string partitionKey, string rowKeyPrefix, CancellationToken ct = default);

    /// <summary>
    /// Upserts a single entity (insert or update).
    /// </summary>
    /// <param name="entity">The entity to upsert.</param>
    /// <param name="mode">The update mode (Merge or Replace).</param>
    /// <param name="ct">Cancellation token.</param>
    Task UpsertAsync(ITableEntity entity, TableUpdateMode mode = TableUpdateMode.Merge, CancellationToken ct = default);

    /// <summary>
    /// Inserts a single entity (fails if it already exists).
    /// </summary>
    /// <param name="entity">The entity to insert.</param>
    /// <param name="ct">Cancellation token.</param>
    Task InsertAsync(ITableEntity entity, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing entity (fails if it doesn't exist).
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <param name="mode">The update mode (Merge or Replace).</param>
    /// <param name="ct">Cancellation token.</param>
    Task UpdateAsync(ITableEntity entity, TableUpdateMode mode = TableUpdateMode.Merge, CancellationToken ct = default);

    /// <summary>
    /// Deletes a single entity.
    /// </summary>
    /// <param name="partitionKey">The partition key.</param>
    /// <param name="rowKey">The row key.</param>
    /// <param name="etag">Optional ETag for optimistic concurrency.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeleteAsync(string partitionKey, string rowKey, ETag etag = default, CancellationToken ct = default);

    /// <summary>
    /// Submits a batch of operations within a single partition.
    /// </summary>
    /// <param name="batch">The batch to submit.</param>
    /// <param name="ct">Cancellation token.</param>
    Task SubmitAsync(PartitionBatch batch, CancellationToken ct = default);

    /// <summary>
    /// Checks if an entity exists.
    /// </summary>
    /// <param name="partitionKey">The partition key.</param>
    /// <param name="rowKey">The row key.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the entity exists, false otherwise.</returns>
    Task<bool> ExistsAsync(string partitionKey, string rowKey, CancellationToken ct = default);
}