using PartiTables;

namespace PartiSample.Models;

/// <summary>
/// Table 1: User Credentials
/// Stores authentication data per user
/// PartitionKey: UserId
/// </summary>
[TablePartition("UserCredentials", "{UserId}")]
public class UserCredentials
{
    public string UserId { get; set; } = default!;
    
    [RowKeyPrefix("")]
    public List<PasswordHistory> PasswordHistory { get; set; } = new();
    
    [RowKeyPrefix("")]
    public List<LoginAttempt> LoginAttempts { get; set; } = new();
    
    [RowKeyPrefix("")]
    public List<MfaDevice> MfaDevices { get; set; } = new();
}

public class PasswordHistory : RowEntity, IRowKeyBuilder
{
    public string PasswordHash { get; set; } = default!;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
    
    public string BuildRowKey(RowKeyContext context)
    {
        var userId = context.GetParentProperty<string>("UserId");
        var timestamp = CreatedAt.ToUnixTimeSeconds();
        return $"{userId}-password-{timestamp}";
    }
}

public class LoginAttempt : RowEntity, IRowKeyBuilder
{
    public DateTimeOffset AttemptedAt { get; set; } = DateTimeOffset.UtcNow;
    public string IpAddress { get; set; } = default!;
    public string UserAgent { get; set; } = default!;
    public bool IsSuccessful { get; set; }
    public string? FailureReason { get; set; }
    
    public string BuildRowKey(RowKeyContext context)
    {
        var userId = context.GetParentProperty<string>("UserId");
        var timestamp = AttemptedAt.ToUnixTimeSeconds();
        return $"{userId}-login-{timestamp}-{Guid.NewGuid():N}";
    }
}

public class MfaDevice : RowEntity, IRowKeyBuilder
{
    public string DeviceId { get; set; } = Guid.NewGuid().ToString("N")[..12];
    public string DeviceType { get; set; } = default!; // SMS, TOTP, Email
    public string DeviceName { get; set; } = default!;
    public string SecretKey { get; set; } = default!; // Encrypted in production
    public bool IsVerified { get; set; }
    public DateTimeOffset RegisteredAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastUsedAt { get; set; }
    
    public string BuildRowKey(RowKeyContext context)
    {
        var userId = context.GetParentProperty<string>("UserId");
        return $"{userId}-mfa-{DeviceId}";
    }
}

/// <summary>
/// Table 2: User Permissions
/// Stores authorization data per user
/// PartitionKey: UserId
/// </summary>
[TablePartition("UserPermissions", "{UserId}")]
public class UserPermissions
{
    public string UserId { get; set; } = default!;
    public string Email { get; set; } = default!;
    
    [RowKeyPrefix("")]
    public List<RoleAssignment> Roles { get; set; } = new();
    
    [RowKeyPrefix("")]
    public List<ResourcePermission> ResourcePermissions { get; set; } = new();
    
    [RowKeyPrefix("")]
    public List<AccessToken> AccessTokens { get; set; } = new();
}

public class RoleAssignment : RowEntity, IRowKeyBuilder
{
    public string RoleId { get; set; } = default!;
    public string RoleName { get; set; } = default!; // Admin, Editor, Viewer
    public string Scope { get; set; } = "Global"; // Global, Organization, Project
    public string? ScopeId { get; set; }
    public DateTimeOffset AssignedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ExpiresAt { get; set; }
    public string AssignedBy { get; set; } = default!;
    
    public string BuildRowKey(RowKeyContext context)
    {
        var userId = context.GetParentProperty<string>("UserId");
        return $"{userId}-role-{RoleId}-{Scope}";
    }
}

public class ResourcePermission : RowEntity, IRowKeyBuilder
{
    public string ResourceType { get; set; } = default!; // Document, Project, Database
    public string ResourceId { get; set; } = default!;
    public string[] Permissions { get; set; } = Array.Empty<string>(); // read, write, delete, share
    public DateTimeOffset GrantedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ExpiresAt { get; set; }
    public string GrantedBy { get; set; } = default!;
    
    public string BuildRowKey(RowKeyContext context)
    {
        var userId = context.GetParentProperty<string>("UserId");
        return $"{userId}-resource-{ResourceType}-{ResourceId}";
    }
}

public class AccessToken : RowEntity, IRowKeyBuilder
{
    public string TokenId { get; set; } = Guid.NewGuid().ToString("N")[..16];
    public string TokenHash { get; set; } = default!; // Hashed token
    public string TokenType { get; set; } = "Bearer"; // Bearer, API, Refresh
    public string[] Scopes { get; set; } = Array.Empty<string>();
    public DateTimeOffset IssuedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public string? RevokedReason { get; set; }
    
    public string BuildRowKey(RowKeyContext context)
    {
        var userId = context.GetParentProperty<string>("UserId");
        return $"{userId}-token-{TokenType}-{TokenId}";
    }
}
