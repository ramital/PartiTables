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
/// RowKey pattern: {StoreId}-{TransactionId}
/// </summary>
[RowKeyPattern("{StoreId}-{TransactionId}")]
public class SalesTransaction : RowEntity
{
    public string TransactionId { get; set; } = default!;
    public string CustomerId { get; set; } = default!;
    public string ProductName { get; set; } = default!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalAmount { get; set; }
    public string Region { get; set; } = default!;
    public DateTimeOffset TransactionDate { get; set; }
}
