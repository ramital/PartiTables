using PartiTables;

namespace PartiSample.Models;

/// <summary>
/// Table 1: Tenant Configuration
/// Stores tenant-level settings and metadata
/// PartitionKey: TenantId
/// </summary>
[TablePartition("TenantConfig", "{TenantId}")]
public class TenantConfiguration
{
    public string TenantId { get; set; } = default!;
    public string TenantName { get; set; } = default!;
    public string Plan { get; set; } = "Standard"; // Free, Standard, Premium, Enterprise

    [RowKeyPrefix("")]
    public List<TenantSettings> Settings { get; set; } = new();

    [RowKeyPrefix("")]
    public List<TenantFeature> Features { get; set; } = new();

    [RowKeyPrefix("")]
    public List<TenantQuota> Quotas { get; set; } = new();
}

public class TenantSettings : RowEntity, IRowKeyBuilder
{
    public string Category { get; set; } = default!;
    public string Key { get; set; } = default!;
    public string Value { get; set; } = default!;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public string BuildRowKey(RowKeyContext context)
    {
        var tenantId = context.GetParentProperty<string>("TenantId");
        return $"{tenantId}-setting-{Category}-{Key}";
    }
}

public class TenantFeature : RowEntity, IRowKeyBuilder
{
    public string FeatureName { get; set; } = default!;
    public bool IsEnabled { get; set; }
    public int UsageLimit { get; set; }
    public DateTimeOffset EnabledDate { get; set; } = DateTimeOffset.UtcNow;

    public string BuildRowKey(RowKeyContext context)
    {
        var tenantId = context.GetParentProperty<string>("TenantId");
        return $"{tenantId}-feature-{FeatureName}";
    }
}

public class TenantQuota : RowEntity, IRowKeyBuilder
{
    public string ResourceType { get; set; } = default!; // Users, Storage, APIRequests
    public int Limit { get; set; }
    public int Used { get; set; }
    public DateTimeOffset ResetDate { get; set; }

    public string BuildRowKey(RowKeyContext context)
    {
        var tenantId = context.GetParentProperty<string>("TenantId");
        return $"{tenantId}-quota-{ResourceType}";
    }
}

/// <summary>
/// Table 2: User Management
/// Stores users across all tenants
/// PartitionKey: TenantId (users grouped by tenant)
/// </summary>
[TablePartition("UserData", "{TenantId}")]
public class TenantUsers
{
    public string TenantId { get; set; } = default!;

    [RowKeyPrefix("")]
    public List<User> Users { get; set; } = new();

    [RowKeyPrefix("")]
    public List<UserRole> Roles { get; set; } = new();

    [RowKeyPrefix("")]
    public List<UserSession> Sessions { get; set; } = new();
}

public class User : RowEntity, IRowKeyBuilder
{
    public string UserId { get; set; } = Guid.NewGuid().ToString("N")[..12];
    public string Email { get; set; } = default!;
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string Status { get; set; } = "Active"; // Active, Suspended, Deleted
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastLoginAt { get; set; }

    public string BuildRowKey(RowKeyContext context)
    {
        var tenantId = context.GetParentProperty<string>("TenantId");
        return $"{tenantId}-user-{UserId}";
    }
}

public class UserRole : RowEntity, IRowKeyBuilder
{
    public string UserId { get; set; } = default!;
    public string RoleName { get; set; } = default!; // Admin, Manager, User, ReadOnly
    public string[] Permissions { get; set; } = Array.Empty<string>();
    public DateTimeOffset AssignedAt { get; set; } = DateTimeOffset.UtcNow;

    public string BuildRowKey(RowKeyContext context)
    {
        var tenantId = context.GetParentProperty<string>("TenantId");
        return $"{tenantId}-role-{UserId}-{RoleName}";
    }
}

public class UserSession : RowEntity, IRowKeyBuilder
{
    public string UserId { get; set; } = default!;
    public string SessionId { get; set; } = Guid.NewGuid().ToString("N");
    public string IpAddress { get; set; } = default!;
    public string UserAgent { get; set; } = default!;
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;

    public string BuildRowKey(RowKeyContext context)
    {
        var tenantId = context.GetParentProperty<string>("TenantId");
        var timestamp = StartedAt.ToUnixTimeSeconds();
        return $"{tenantId}-session-{UserId}-{timestamp}";
    }
}

/// <summary>
/// Table 3: Audit Logs
/// Cross-cutting concerns - tracks all activities
/// PartitionKey: TenantId (logs grouped by tenant)
/// </summary>
[TablePartition("AuditLogs", "{TenantId}")]
public class TenantAuditLog
{
    public string TenantId { get; set; } = default!;

    [RowKeyPrefix("")]
    public List<AuditEntry> Entries { get; set; } = new();

    [RowKeyPrefix("")]
    public List<SecurityEvent> SecurityEvents { get; set; } = new();

    [RowKeyPrefix("")]
    public List<DataChange> DataChanges { get; set; } = new();
}

public class AuditEntry : RowEntity, IRowKeyBuilder
{
    public string UserId { get; set; } = default!;
    public string Action { get; set; } = default!; // Login, Logout, Create, Update, Delete
    public string ResourceType { get; set; } = default!;
    public string ResourceId { get; set; } = default!;
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public string Details { get; set; } = default!;
    public bool IsSuccess { get; set; }

    public string BuildRowKey(RowKeyContext context)
    {
        var tenantId = context.GetParentProperty<string>("TenantId");
        var timestamp = Timestamp.ToUnixTimeSeconds();
        return $"{tenantId}-audit-{timestamp}-{Action}-{Guid.NewGuid():N}";
    }
}

public class SecurityEvent : RowEntity, IRowKeyBuilder
{
    public string EventType { get; set; } = default!; // FailedLogin, Unauthorized, SuspiciousActivity
    public string UserId { get; set; } = default!;
    public string IpAddress { get; set; } = default!;
    public string Severity { get; set; } = "Medium"; // Low, Medium, High, Critical
    public DateTimeOffset DetectedAt { get; set; } = DateTimeOffset.UtcNow;
    public string Description { get; set; } = default!;

    public string BuildRowKey(RowKeyContext context)
    {
        var tenantId = context.GetParentProperty<string>("TenantId");
        var timestamp = DetectedAt.ToUnixTimeSeconds();
        return $"{tenantId}-security-{timestamp}-{EventType}";
    }
}

public class DataChange : RowEntity, IRowKeyBuilder
{
    public string UserId { get; set; } = default!;
    public string EntityType { get; set; } = default!;
    public string EntityId { get; set; } = default!;
    public string ChangeType { get; set; } = default!; // Created, Updated, Deleted
    public DateTimeOffset ChangedAt { get; set; } = DateTimeOffset.UtcNow;
    public string OldValue { get; set; } = default!;
    public string NewValue { get; set; } = default!;

    public string BuildRowKey(RowKeyContext context)
    {
        var tenantId = context.GetParentProperty<string>("TenantId");
        var timestamp = ChangedAt.ToUnixTimeSeconds();
        return $"{tenantId}-datachange-{timestamp}-{EntityType}";
    }
}
