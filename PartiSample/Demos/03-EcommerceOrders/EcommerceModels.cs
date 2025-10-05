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
/// RowKey pattern: {customerId}-order-{timestamp}-{orderId}
/// Allows sorting by date while maintaining uniqueness
/// </summary>
public class Order : RowEntity, IRowKeyBuilder
{
    public string OrderId { get; set; } = Guid.NewGuid().ToString("N")[..12];
    public string Status { get; set; } = "Pending"; // Pending, Processing, Shipped, Delivered, Cancelled
    public decimal TotalAmount { get; set; }
    public DateTimeOffset OrderDate { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ShippedDate { get; set; }
    public string? TrackingNumber { get; set; }
    public int ItemCount { get; set; }

    public string BuildRowKey(RowKeyContext context)
    {
        var customerId = context.GetParentProperty<string>("CustomerId");
        var timestamp = OrderDate.ToUnixTimeSeconds();
        return $"{customerId}-order-{timestamp}-{OrderId}";
    }
}

/// <summary>
/// Shipping or billing address
/// RowKey pattern: {customerId}-address-{addressId}
/// </summary>
public class ShippingAddress : RowEntity, IRowKeyBuilder
{
    public string AddressId { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string AddressType { get; set; } = "Shipping"; // Shipping, Billing, Both
    public string Street { get; set; } = default!;
    public string City { get; set; } = default!;
    public string State { get; set; } = default!;
    public string ZipCode { get; set; } = default!;
    public string Country { get; set; } = "USA";
    public bool IsDefault { get; set; }

    public string BuildRowKey(RowKeyContext context)
    {
        var customerId = context.GetParentProperty<string>("CustomerId");
        return $"{customerId}-address-{AddressId}";
    }
}

/// <summary>
/// Payment method (credit card, PayPal, etc.)
/// RowKey pattern: {customerId}-payment-{paymentId}
/// </summary>
public class PaymentMethod : RowEntity, IRowKeyBuilder
{
    public string PaymentId { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string Type { get; set; } = "CreditCard"; // CreditCard, PayPal, BankTransfer
    public string LastFourDigits { get; set; } = default!;
    public string CardBrand { get; set; } = default!; // Visa, MasterCard, Amex
    public string ExpiryMonth { get; set; } = default!;
    public string ExpiryYear { get; set; } = default!;
    public bool IsDefault { get; set; }

    public string BuildRowKey(RowKeyContext context)
    {
        var customerId = context.GetParentProperty<string>("CustomerId");
        return $"{customerId}-payment-{PaymentId}";
    }
}

/// <summary>
/// Customer preferences and settings
/// RowKey pattern: {customerId}-pref-{key}-{preferenceId}
/// </summary>
public class CustomerPreference : RowEntity, IRowKeyBuilder
{
    public string PreferenceId { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string Category { get; set; } = default!; // Notifications, Privacy, etc.
    public string Key { get; set; } = default!; // newsletter, sms_notifications, etc.
    public string Value { get; set; } = default!;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public string BuildRowKey(RowKeyContext context)
    {
        var customerId = context.GetParentProperty<string>("CustomerId");
        return $"{customerId}-pref-{Key}-{PreferenceId}";
    }
}
