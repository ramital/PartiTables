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

[RowKeyPattern("{UserId}-password-{PasswordId}")]
public class PasswordHistory : RowEntity
{
    // PasswordId with timestamp prefix for chronological sorting: "20240131-103045-a1b2"
    public string PasswordId { get; set; } = 
        $"{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid().ToString("N")[..6]}";
    
    public string PasswordHash { get; set; } = default!;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
}

[RowKeyPattern("{UserId}-login-{AttemptId}")]
public class LoginAttempt : RowEntity
{
    // AttemptId with timestamp prefix for chronological sorting: "20240131-103045-a1b2c3"
    public string AttemptId { get; set; } = 
        $"{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid().ToString("N")[..8]}";
    
    public DateTimeOffset AttemptedAt { get; set; } = DateTimeOffset.UtcNow;
    public string IpAddress { get; set; } = default!;
    public string UserAgent { get; set; } = default!;
    public bool IsSuccessful { get; set; }
    public string? FailureReason { get; set; }
}

[RowKeyPattern("{UserId}-mfa-{DeviceId}")]
public class MfaDevice : RowEntity
{
    public string DeviceId { get; set; } = Guid.NewGuid().ToString("N")[..12];
    public string DeviceType { get; set; } = default!; // SMS, TOTP, Email
    public string DeviceName { get; set; } = default!;
    public string SecretKey { get; set; } = default!; // Encrypted in production
    public bool IsVerified { get; set; }
    public DateTimeOffset RegisteredAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastUsedAt { get; set; }
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

[RowKeyPattern("{UserId}-role-{RoleId}-{Scope}")]
public class RoleAssignment : RowEntity
{
    public string RoleId { get; set; } = default!;
    public string RoleName { get; set; } = default!; // Admin, Editor, Viewer
    public string Scope { get; set; } = "Global"; // Global, Organization, Project
    public string? ScopeId { get; set; }
    public DateTimeOffset AssignedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ExpiresAt { get; set; }
    public string AssignedBy { get; set; } = default!;
}

[RowKeyPattern("{UserId}-resource-{ResourceType}-{ResourceId}")]
public class ResourcePermission : RowEntity
{
    public string ResourceType { get; set; } = default!; // Document, Project, Database
    public string ResourceId { get; set; } = default!;
    public string[] Permissions { get; set; } = Array.Empty<string>(); // read, write, delete, share
    public DateTimeOffset GrantedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ExpiresAt { get; set; }
    public string GrantedBy { get; set; } = default!;
}

[RowKeyPattern("{UserId}-token-{TokenType}-{TokenId}")]
public class AccessToken : RowEntity
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
}
