using Azure.Data.Tables;
using PartiTables.Interfaces;
using System.Linq.Expressions;
using System.Reflection;

namespace PartiTables;

/// <summary>
/// Repository for strongly-typed partition entities. Provides CRUD operations and LINQ support.
/// </summary>
public class PartitionRepository<T> where T : class, new()
{
    private readonly IPartitionClient _client;
    private readonly string _tableName;
    private readonly string _partitionKeyTemplate;
    private readonly Dictionary<string, (PropertyInfo prop, RowKeyPrefixAttribute attr)> _collections;
    private readonly Dictionary<Type, Func<string, bool>> _rowKeyMatchers;

    public PartitionRepository(TableServiceClient tableServiceClient, TableOptions globalOptions)
    {
        if (tableServiceClient == null)
            throw new ArgumentNullException(nameof(tableServiceClient));

        var type = typeof(T);
        var tableAttr = type.GetCustomAttribute<TablePartitionAttribute>()
            ?? throw new InvalidOperationException($"Type {type.Name} must have [TablePartition] attribute");

        _tableName = tableAttr.TableName;
        _partitionKeyTemplate = tableAttr.PartitionKeyTemplate;

        var tableClient = tableServiceClient.GetTableClient(_tableName);
        
        if (globalOptions?.CreateTableIfNotExists ?? true)
        {
            tableClient.CreateIfNotExists();
        }

        _client = new PartitionClient(tableClient, globalOptions?.ResiliencePolicy);

        _collections = new Dictionary<string, (PropertyInfo, RowKeyPrefixAttribute)>();
        _rowKeyMatchers = new Dictionary<Type, Func<string, bool>>();
        
        foreach (var prop in type.GetProperties())
        {
            var attr = prop.GetCustomAttribute<RowKeyPrefixAttribute>();
            if (attr != null)
            {
                _collections[prop.Name] = (prop, attr);
                
                if (prop.PropertyType.IsGenericType && 
                    prop.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    var itemType = prop.PropertyType.GetGenericArguments()[0];
                    _rowKeyMatchers[itemType] = BuildRowKeyMatcher(itemType, attr);
                }
            }
        }
    }

    private Func<string, bool> BuildRowKeyMatcher(Type itemType, RowKeyPrefixAttribute attr)
    {
        if (typeof(IRowKeyBuilder).IsAssignableFrom(itemType))
        {
            try
            {
                var sampleItem = Activator.CreateInstance(itemType);
                if (sampleItem is IRowKeyBuilder builder)
                {
                    var dummyParent = new T();
                    var parentType = typeof(T);
                    
                    foreach (var prop in parentType.GetProperties())
                    {
                        if (prop.PropertyType == typeof(string) && prop.CanWrite)
                        {
                            try
                            {
                                if (prop.Name.Contains("Id", StringComparison.OrdinalIgnoreCase))
                                    prop.SetValue(dummyParent, "SAMPLE-ID");
                            }
                            catch { }
                        }
                    }
                    
                    var dummyContext = new RowKeyContext(dummyParent, attr.Prefix, "dummy-partition");
                    var sampleRowKey = builder.BuildRowKey(dummyContext);
                    
                    return rowKey => MatchesPattern(rowKey, sampleRowKey);
                }
            }
            catch { }
        }
        
        if (!string.IsNullOrEmpty(attr.Prefix))
        {
            return rowKey => rowKey.StartsWith(attr.Prefix);
        }
        
        return rowKey => true;
    }

    private bool MatchesPattern(string rowKey, string pattern)
    {
        var patternParts = pattern.Split('-', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.ToLower()).ToList();
        var rowKeyParts = rowKey.ToLower().Split('-', StringSplitOptions.RemoveEmptyEntries).ToList();
        
        var keywords = new[] { "meta", "consent", "device", "profile", "audit", "log", "item", 
                               "detail", "user", "order", "payment", "setting", "feature", "quota",
                               "role", "session", "comment", "attachment", "task", "token", "mfa",
                               "password", "login", "resource", "security", "datachange" };
        
        var staticKeywords = patternParts
            .Where(p => keywords.Contains(p))
            .Distinct()
            .ToList();
        
        if (staticKeywords.Count == 0)
            return true;
        
        return staticKeywords.All(keyword => rowKeyParts.Contains(keyword));
    }

    /// <summary>
    /// Loads an entity by partition key value.
    /// </summary>
    public async Task<T?> FindAsync(object partitionKeyValue, CancellationToken ct = default)
    {
        var partitionKey = ResolvePartitionKey(partitionKeyValue);
        var allRows = await _client.GetPartitionAsync(partitionKey, ct);
        
        if (allRows.Count == 0)
            return null;

        return MapToEntity(allRows, partitionKey);
    }

    /// <summary>
    /// Saves an entity (upserts all collections). Auto-generates row keys.
    /// Rolls back all changes if any batch fails.
    /// </summary>
    public async Task SaveAsync(T entity, CancellationToken ct = default)
    {
        var partitionKey = ExtractPartitionKey(entity);
        var batches = new List<PartitionBatch>();
        var currentBatch = new PartitionBatch(partitionKey);
        batches.Add(currentBatch);

        foreach (var (propName, (prop, attr)) in _collections)
        {
            var collection = prop.GetValue(entity) as System.Collections.IEnumerable;
            if (collection == null) continue;

            foreach (var item in collection)
            {
                if (item is RowEntity rowEntity)
                {
                    if (item is IRowKeyBuilder builder && string.IsNullOrEmpty(rowEntity.RowKeyId))
                    {
                        var context = new RowKeyContext(entity, attr.Prefix, partitionKey);
                        rowEntity.RowKeyId = builder.BuildRowKey(context);
                    }
                    
                    var tableEntity = rowEntity.ToTableEntity(partitionKey);
                    
                    // If current batch is full, create a new one
                    if (currentBatch.Count >= 100)
                    {
                        currentBatch = new PartitionBatch(partitionKey);
                        batches.Add(currentBatch);
                    }
                    
                    currentBatch.Upsert(tableEntity);
                }
            }
        }

        // Track successfully submitted batches for rollback
        var submittedBatches = new List<PartitionBatch>();
        
        try
        {
            // Submit all batches
            foreach (var batch in batches)
            {
                if (batch.Count > 0)
                {
                    await _client.SubmitAsync(batch, ct);
                    submittedBatches.Add(batch);
                }
            }
        }
        catch
        {
            // Rollback: Delete all successfully submitted entities
            await RollbackBatchesAsync(partitionKey, submittedBatches, ct);
            throw;
        }
    }

    private async Task RollbackBatchesAsync(string partitionKey, List<PartitionBatch> submittedBatches, CancellationToken ct)
    {
        if (submittedBatches.Count == 0)
            return;

        var rollbackBatches = new List<PartitionBatch>();
        var currentRollbackBatch = new PartitionBatch(partitionKey);
        rollbackBatches.Add(currentRollbackBatch);

        // Build delete operations for all submitted entities
        foreach (var batch in submittedBatches)
        {
            foreach (var action in batch.Actions)
            {
                if (currentRollbackBatch.Count >= 100)
                {
                    currentRollbackBatch = new PartitionBatch(partitionKey);
                    rollbackBatches.Add(currentRollbackBatch);
                }
                
                currentRollbackBatch.Delete(action.Entity.RowKey);
            }
        }

        // Execute rollback batches
        var rollbackExceptions = new List<Exception>();
        foreach (var rollbackBatch in rollbackBatches)
        {
            if (rollbackBatch.Count > 0)
            {
                try
                {
                    await _client.SubmitAsync(rollbackBatch, ct);
                }
                catch (Exception ex)
                {
                    // Track rollback failures but continue trying to rollback remaining batches
                    rollbackExceptions.Add(ex);
                }
            }
        }

        // If rollback failed, throw aggregate exception to inform caller of inconsistent state
        if (rollbackExceptions.Count > 0)
        {
            throw new AggregateException(
                $"Rollback failed for {rollbackExceptions.Count} batch(es). Data may be in an inconsistent state.",
                rollbackExceptions);
        }
    }

    /// <summary>
    /// Queries entities with optional predicate (in-memory filtering after load).
    /// </summary>
    public async Task<List<T>> QueryAsync(object partitionKeyValue, Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default)
    {
        var partitionKey = ResolvePartitionKey(partitionKeyValue);
        var allRows = await _client.GetPartitionAsync(partitionKey, ct);
        
        if (allRows.Count == 0)
            return new List<T>();

        var entity = MapToEntity(allRows, partitionKey);
        if (entity == null)
            return new List<T>();

        var results = new List<T> { entity };

        if (predicate != null)
        {
            var compiled = predicate.Compile();
            results = results.Where(compiled).ToList();
        }

        return results;
    }

    /// <summary>
    /// Queries a specific collection without loading the entire entity. Most efficient.
    /// </summary>
    public async Task<List<TRow>> QueryCollectionAsync<TRow>(
        object partitionKeyValue,
        Expression<Func<T, List<TRow>>> collectionSelector,
        CancellationToken ct = default) where TRow : RowEntity, new()
    {
        var partitionKey = ResolvePartitionKey(partitionKeyValue);
        
        var memberExpr = collectionSelector.Body as MemberExpression
            ?? throw new ArgumentException("Expression must be a property selector");
        
        var propName = memberExpr.Member.Name;
        
        if (!_collections.TryGetValue(propName, out var collectionInfo))
            throw new InvalidOperationException($"Property {propName} is not marked with [RowKeyPrefix]");

        var (_, attr) = collectionInfo;
        var itemType = typeof(TRow);
        
        var matcher = _rowKeyMatchers.TryGetValue(itemType, out var m) 
            ? m 
            : (rowKey => !string.IsNullOrEmpty(attr.Prefix) && rowKey.StartsWith(attr.Prefix));
        
        var allRows = await _client.GetPartitionAsync(partitionKey, ct);
        
        var results = new List<TRow>();
        foreach (var tableEntity in allRows.Where(r => matcher(r.RowKey)))
        {
            var rowEntity = new TRow();
            rowEntity.FromTableEntity(tableEntity);
            results.Add(rowEntity);
        }

        return results;
    }

    /// <summary>
    /// Deletes all rows in the partition.
    /// </summary>
    public async Task DeleteAsync(object partitionKeyValue, CancellationToken ct = default)
    {
        var partitionKey = ResolvePartitionKey(partitionKeyValue);
        var allRows = await _client.GetPartitionAsync(partitionKey, ct);
        
        if (allRows.Count == 0)
            return;

        var batches = new List<PartitionBatch>();
        var currentBatch = new PartitionBatch(partitionKey);
        batches.Add(currentBatch);

        foreach (var row in allRows)
        {
            // If current batch is full, create a new one
            if (currentBatch.Count >= 100)
            {
                currentBatch = new PartitionBatch(partitionKey);
                batches.Add(currentBatch);
            }
            
            currentBatch.Delete(row.RowKey);
        }

        // Submit all batches
        foreach (var batch in batches)
        {
            if (batch.Count > 0)
            {
                await _client.SubmitAsync(batch, ct);
            }
        }
    }

    private string ResolvePartitionKey(object partitionKeyValue)
    {
        return partitionKeyValue.ToString() ?? throw new ArgumentNullException(nameof(partitionKeyValue));
    }

    private string ExtractPartitionKey(T entity)
    {
        var template = _partitionKeyTemplate.Trim('{', '}');
        var prop = typeof(T).GetProperty(template)
            ?? throw new InvalidOperationException($"Property {template} not found on {typeof(T).Name}");
        
        var value = prop.GetValue(entity);
        return value?.ToString() ?? throw new InvalidOperationException($"Partition key value is null");
    }

    private T? MapToEntity(IReadOnlyList<TableEntity> rows, string partitionKey)
    {
        if (rows.Count == 0)
            return null;

        var entity = new T();

        var pkTemplate = _partitionKeyTemplate.Trim('{', '}');
        var pkProp = typeof(T).GetProperty(pkTemplate);
        if (pkProp != null)
        {
            pkProp.SetValue(entity, partitionKey);
        }

        foreach (var (propName, (prop, attr)) in _collections)
        {
            var collectionType = prop.PropertyType;
            if (!collectionType.IsGenericType || collectionType.GetGenericTypeDefinition() != typeof(List<>))
                continue;

            var itemType = collectionType.GetGenericArguments()[0];
            var list = Activator.CreateInstance(collectionType) as System.Collections.IList
                ?? throw new InvalidOperationException($"Cannot create list for {propName}");

            var matcher = _rowKeyMatchers.TryGetValue(itemType, out var m)
                ? m
                : (rowKey => !string.IsNullOrEmpty(attr.Prefix) && rowKey.StartsWith(attr.Prefix));

            var matchingRows = rows.Where(r => matcher(r.RowKey)).ToList();

            foreach (var tableEntity in matchingRows)
            {
                if (Activator.CreateInstance(itemType) is RowEntity rowEntity)
                {
                    rowEntity.FromTableEntity(tableEntity);
                    list.Add(rowEntity);
                }
            }

            prop.SetValue(entity, list);
        }

        return entity;
    }
}
