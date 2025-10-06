using PartiTables;

namespace PartiSample.GetStarted.PartiTables;

[TablePartition("Customers", "{CustomerId}")]
public class Customer
{
    public string CustomerId { get; set; } = default!;
    public string Name { get; set; } = default!;
    
    [RowKeyPrefix("")]
    public List<Order> Orders { get; set; } = new();
}

[RowKeyPattern("{CustomerId}-order-{OrderId}")]
public class Order : RowEntity
{
    public string OrderId { get; set; } = default!;
    public decimal Amount { get; set; }
}
