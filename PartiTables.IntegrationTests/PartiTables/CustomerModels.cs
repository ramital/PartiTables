using PartiTables;

namespace PartiTables.IntegrationTests.PartiTables;

/// <summary>
/// Customer entity with orders and addresses
/// Uses TenantId as partition key for multi-tenant scenarios
/// </summary>
[TablePartition("CustomersTable", "{TenantId}")]
public class Customer
{
    public string TenantId { get; set; } = default!;
    public string CustomerId { get; set; } = default!;
    
    [RowKeyPrefix("")]
    public List<CustomerProfile> Profile { get; set; } = new();
    
    [RowKeyPrefix("")]
    public List<Order> Order { get; set; } = new();
    
    [RowKeyPrefix("")]
    public List<Address> Address { get; set; } = new();
}

/// <summary>
/// Customer profile information
/// RowKey pattern: {CustomerId}-profile
/// </summary>
[RowKeyPattern("{CustomerId}-profile")]
public class CustomerProfile : RowEntity
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
}

/// <summary>
/// Customer order record
/// RowKey pattern: {CustomerId}-order-{OrderId}
/// </summary>
[RowKeyPattern("{CustomerId}-order-{OrderId}")]
public class Order : RowEntity
{
    public string OrderId { get; set; } = default!;
    public decimal Amount { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Customer address record
/// RowKey pattern: {CustomerId}-address-{AddressId}
/// </summary>
[RowKeyPattern("{CustomerId}-address-{AddressId}")]
public class Address : RowEntity
{
    public string AddressId { get; set; } = default!;
    public string? Street { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
    public string? Country { get; set; }
}
