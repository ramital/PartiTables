using PartiTables;

namespace PartiTables.IntegrationTests.PartiTables;

/// <summary>
/// Sales data for a store - the partition root
/// All transactions for a store share the same partition
/// </summary>
[TablePartition("BigDataDemo", "{StoreId}")]
public class SalesData
{
    public string StoreId { get; set; } = default!;
    public string StoreName { get; set; } = default!;
    public string Region { get; set; } = default!;

    [RowKeyPrefix("")]
    public List<SalesTransaction> Transactions { get; set; } = new();
}

/// <summary>
/// Individual sales transaction
/// RowKey pattern: {storeId}-txn-{transactionId}
/// </summary>
public class SalesTransaction : RowEntity, IRowKeyBuilder
{
    public string TransactionId { get; set; } = default!;
    public string CustomerId { get; set; } = default!;
    public string ProductName { get; set; } = default!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalAmount { get; set; }
    public string Region { get; set; } = default!;
    public DateTimeOffset TransactionDate { get; set; }

    public string BuildRowKey(RowKeyContext context)
    {
        var storeId = context.GetParentProperty<string>("StoreId");
        return $"{storeId}-{TransactionId}";
    }
}
