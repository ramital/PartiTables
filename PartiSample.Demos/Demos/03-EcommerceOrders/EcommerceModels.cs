using PartiTables;

namespace PartiSample.Models;

/// <summary>
/// E-commerce customer entity with orders and profile data
/// Uses customer ID as partition key
/// </summary>
[TablePartition("OrderData", "{CustomerId}")]
public class CustomerOrders
{
    public string CustomerId { get; set; } = default!;
    public string CustomerEmail { get; set; } = default!;

    [RowKeyPrefix("")]
    public List<Order> Orders { get; set; } = new();

    [RowKeyPrefix("")]
    public List<ShippingAddress> Addresses { get; set; } = new();

    [RowKeyPrefix("")]
    public List<PaymentMethod> PaymentMethods { get; set; } = new();

    [RowKeyPrefix("")]
    public List<CustomerPreference> Preferences { get; set; } = new();
}

/// <summary>
/// Customer order record
/// RowKey pattern: {CustomerId}-order-{OrderId}
/// OrderId includes timestamp for chronological sorting
/// </summary>
[RowKeyPattern("{CustomerId}-order-{OrderId}")]
public class Order : RowEntity
{
    // OrderId with timestamp prefix for sorting: "20240131-a1b2c3d4e5f6"
    public string OrderId { get; set; } =
        $"{DateTimeOffset.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..12]}";

    public string Status { get; set; } = "Pending"; // Pending, Processing, Shipped, Delivered, Cancelled
    public decimal TotalAmount { get; set; }
    public DateTimeOffset OrderDate { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ShippedDate { get; set; }
    public string? TrackingNumber { get; set; }
    public int ItemCount { get; set; }
}

/// <summary>
/// Shipping or billing address
/// RowKey pattern: {CustomerId}-address-{AddressId}
/// </summary>
[RowKeyPattern("{CustomerId}-address-{AddressId}")]
public class ShippingAddress : RowEntity
{
    public string AddressId { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string AddressType { get; set; } = "Shipping"; // Shipping, Billing, Both
    public string Street { get; set; } = default!;
    public string City { get; set; } = default!;
    public string State { get; set; } = default!;
    public string ZipCode { get; set; } = default!;
    public string Country { get; set; } = "USA";
    public bool IsDefault { get; set; }
}

/// <summary>
/// Payment method (credit card, PayPal, etc.)
/// RowKey pattern: {CustomerId}-payment-{PaymentId}
/// </summary>
[RowKeyPattern("{CustomerId}-payment-{PaymentId}")]
public class PaymentMethod : RowEntity
{
    public string PaymentId { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string Type { get; set; } = "CreditCard"; // CreditCard, PayPal, BankTransfer
    public string LastFourDigits { get; set; } = default!;
    public string CardBrand { get; set; } = default!; // Visa, MasterCard, Amex
    public string ExpiryMonth { get; set; } = default!;
    public string ExpiryYear { get; set; } = default!;
    public bool IsDefault { get; set; }
}

/// <summary>
/// Customer preferences and settings
/// RowKey pattern: {CustomerId}-pref-{Key}-{PreferenceId}
/// </summary>
[RowKeyPattern("{CustomerId}-pref-{Key}-{PreferenceId}")]
public class CustomerPreference : RowEntity
{
    public string PreferenceId { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string Category { get; set; } = default!; // Notifications, Privacy, etc.
    public string Key { get; set; } = default!; // newsletter, sms_notifications, etc.
    public string Value { get; set; } = default!;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
