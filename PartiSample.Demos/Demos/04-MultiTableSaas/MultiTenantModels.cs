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

[RowKeyPattern("{TenantId}-setting-{Category}-{Key}")]
public class TenantSettings : RowEntity
{
    public string Category { get; set; } = default!;
    public string Key { get; set; } = default!;
    public string Value { get; set; } = default!;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

[RowKeyPattern("{TenantId}-feature-{FeatureName}")]
public class TenantFeature : RowEntity
{
    public string FeatureName { get; set; } = default!;
    public bool IsEnabled { get; set; }
    public int UsageLimit { get; set; }
    public DateTimeOffset EnabledDate { get; set; } = DateTimeOffset.UtcNow;
}

[RowKeyPattern("{TenantId}-quota-{ResourceType}")]
public class TenantQuota : RowEntity
{
    public string ResourceType { get; set; } = default!; // Users, Storage, APIRequests
    public int Limit { get; set; }
    public int Used { get; set; }
    public DateTimeOffset ResetDate { get; set; }
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

[RowKeyPattern("{TenantId}-user-{UserId}")]
public class User : RowEntity
{
    public string UserId { get; set; } = Guid.NewGuid().ToString("N")[..12];
    public string Email { get; set; } = default!;
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string Status { get; set; } = "Active"; // Active, Suspended, Deleted
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastLoginAt { get; set; }
}

[RowKeyPattern("{TenantId}-role-{UserId}-{RoleName}")]
public class UserRole : RowEntity
{
    public string UserId { get; set; } = default!;
    public string RoleName { get; set; } = default!; // Admin, Manager, User, ReadOnly
    public string[] Permissions { get; set; } = Array.Empty<string>();
    public DateTimeOffset AssignedAt { get; set; } = DateTimeOffset.UtcNow;
}

[RowKeyPattern("{TenantId}-session-{UserId}-{SessionId}")]
public class UserSession : RowEntity
{
    public string UserId { get; set; } = default!;
    
    // SessionId with timestamp prefix for chronological sorting: "20240131-103045-a1b2c3d4e5f6"
    public string SessionId { get; set; } = 
        $"{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid().ToString("N")[..12]}";
    
    public string IpAddress { get; set; } = default!;
    public string UserAgent { get; set; } = default!;
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
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

[RowKeyPattern("{TenantId}-audit-{EntryId}")]
public class AuditEntry : RowEntity
{
    // EntryId with timestamp prefix for chronological sorting: "20240131-103045-action-a1b2"
    public string EntryId { get; set; } = 
        $"{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid().ToString("N")[..6]}";
    
    public string UserId { get; set; } = default!;
    public string Action { get; set; } = default!; // Login, Logout, Create, Update, Delete
    public string ResourceType { get; set; } = default!;
    public string ResourceId { get; set; } = default!;
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public string Details { get; set; } = default!;
    public bool IsSuccess { get; set; }
}

[RowKeyPattern("{TenantId}-security-{EventId}")]
public class SecurityEvent : RowEntity
{
    // EventId with timestamp prefix for chronological sorting: "20240131-103045-type-a1b2"
    public string EventId { get; set; } = 
        $"{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid().ToString("N")[..6]}";
    
    public string EventType { get; set; } = default!; // FailedLogin, Unauthorized, SuspiciousActivity
    public string UserId { get; set; } = default!;
    public string IpAddress { get; set; } = default!;
    public string Severity { get; set; } = "Medium"; // Low, Medium, High, Critical
    public DateTimeOffset DetectedAt { get; set; } = DateTimeOffset.UtcNow;
    public string Description { get; set; } = default!;
}

[RowKeyPattern("{TenantId}-datachange-{ChangeId}")]
public class DataChange : RowEntity
{
    // ChangeId with timestamp prefix for chronological sorting: "20240131-103045-type-a1b2"
    public string ChangeId { get; set; } = 
        $"{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid().ToString("N")[..6]}";
    
    public string UserId { get; set; } = default!;
    public string EntityType { get; set; } = default!;
    public string EntityId { get; set; } = default!;
    public string ChangeType { get; set; } = default!; // Created, Updated, Deleted
    public DateTimeOffset ChangedAt { get; set; } = DateTimeOffset.UtcNow;
    public string OldValue { get; set; } = default!;
    public string NewValue { get; set; } = default!;
}
