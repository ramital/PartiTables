# Demo 3: E-commerce Order Management

## Overview
This demo shows the **same patterns as Demo 2** applied to a completely different business domain - e-commerce.

## What You'll Learn
- Apply healthcare patterns to e-commerce
- Manage customer orders and profiles
- Handle multiple collection types
- Query specific data efficiently
- Update orders and customer info

## Domain Model
```
Customer (Partition)
    Orders (many) - Purchase history
    Addresses (many) - Shipping/Billing
    PaymentMethods (many) - Cards, PayPal
    Preferences (many) - Settings
```

## RowKey Patterns
| Entity | Pattern | Example |
|--------|---------|---------|
| Order | `{customerId}-order-{timestamp}-{id}` | `cust-john-order-1234567890-a1b2c3` |
| Address | `{customerId}-address-{id}` | `cust-john-address-x7y8z9` |
| Payment | `{customerId}-payment-{id}` | `cust-john-payment-m3n4p5` |
| Preference | `{customerId}-pref-{key}-{id}` | `cust-john-pref-newsletter-q6r7s8` |

## Key Features

### Order Sorting by Date
```csharp
public string BuildRowKey(RowKeyContext context)
{
    var customerId = context.GetParentProperty<string>("CustomerId");
    var timestamp = OrderDate.ToUnixTimeSeconds();
    return $"{customerId}-order-{timestamp}-{OrderId}";
}
```
This pattern allows natural sorting by order date!

### Collection-Specific Queries
```csharp
// Get only orders (fast!)
var orders = await repo.QueryCollectionAsync(customerId, c => c.Orders);
var totalRevenue = orders.Sum(o => o.TotalAmount);
```

### Customer Analytics
```csharp
var deliveredOrders = orders.Count(o => o.Status == "Delivered");
var totalSpent = orders.Where(o => o.Status != "Cancelled")
                        .Sum(o => o.TotalAmount);
```

## Use Cases
-    Customer order history
-    Order tracking and fulfillment
-    Address management
-    Payment method storage
-    Customer preferences

## Key Insights
  Same code patterns across different domains  
  Flexible partition design  
  Efficient querying  
  Easy to understand and maintain  

## Next Steps
   **Demo 4** shows how to coordinate multiple tables for enterprise SaaS
