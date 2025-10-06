# Demo 5: Security - Authentication & Authorization

## Overview
This demo shows **security patterns** using **2 separate Azure Tables** for authentication and authorization.

## What You'll Learn
- Separate authentication from authorization
- Password history and rotation
- Multi-factor authentication (MFA)
- Login attempt tracking
- Role-based access control (RBAC)
- Token management and revocation
- Security analytics

## Architecture
```
                                
   Table 1: UserCredentials     
   PartitionKey: UserId         
   - Password History           
   - Login Attempts             
   - MFA Devices                
                                

                                
   Table 2: UserPermissions     
   PartitionKey: UserId         
   - Role Assignments           
   - Resource Permissions       
   - Access Tokens              
                                
```

## Why Two Tables 
- **Security**: Authentication data isolated from authorization
- **Access Control**: Different teams manage different aspects
- **Performance**: Query only what you need
- **Compliance**: Separate audit trails

## Key Features

### Password Management
```csharp
var credentials = new UserCredentials { UserId = "alice" };

// Add password with history
credentials.PasswordHistory.Add(new PasswordHistory
{
    PasswordHash = "hash_abc123",
    IsActive = true,
    ExpiresAt = DateTimeOffset.UtcNow.AddDays(90)
});

// Password rotation
oldPassword.IsActive = false;
credentials.PasswordHistory.Add(new PasswordHistory
{
    PasswordHash = "hash_def456",
    IsActive = true
});
```

### Multi-Factor Authentication
```csharp
credentials.MfaDevices.Add(new MfaDevice
{
    DeviceType = "TOTP",
    DeviceName = "Google Authenticator",
    IsVerified = true
});
```

### Login Tracking
```csharp
credentials.LoginAttempts.Add(new LoginAttempt
{
    IpAddress = "192.168.1.100",
    IsSuccessful = true
});

// Detect suspicious activity
var failedAttempts = loginAttempts
    .Where(l => !l.IsSuccessful)
    .GroupBy(l => l.IpAddress)
    .Where(g => g.Count() >= 3);
```

### Role-Based Access Control
```csharp
var permissions = new UserPermissions { UserId = "alice" };

permissions.Roles.Add(new RoleAssignment
{
    RoleName = "Editor",
    Scope = "Organization",
    ScopeId = "acme-corp"
});
```

### Resource Permissions
```csharp
permissions.ResourcePermissions.Add(new ResourcePermission
{
    ResourceType = "Document",
    ResourceId = "doc-123",
    Permissions = new[] { "read", "write", "share" }
});
```

### Token Management
```csharp
permissions.AccessTokens.Add(new AccessToken
{
    TokenType = "Bearer",
    TokenHash = "hash_token_abc",
    Scopes = new[] { "read:profile", "write:documents" },
    ExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
});

// Revoke tokens
foreach (var token in tokens.Where(t => !t.IsRevoked))
{
    token.IsRevoked = true;
    token.RevokedReason = "Password changed";
}
```

## Security Patterns
  **Password History** - Track and prevent reuse  
  **Password Expiration** - Force rotation  
  **MFA Support** - Multiple device types  
  **Login Tracking** - Detect suspicious activity  
  **Role Hierarchy** - Granular permissions  
  **Resource-Level Permissions** - Fine-grained control  
  **Token Lifecycle** - Issue, refresh, revoke  
  **Security Analytics** - Monitor and alert  

## Security Analytics
```csharp
// Failed login detection
var suspiciousIPs = loginAttempts
    .Where(l => !l.IsSuccessful)
    .GroupBy(l => l.IpAddress)
    .Where(g => g.Count() >= 3)
    .Select(g => g.Key);

// Active MFA devices
var mfaDevices = await credentialsRepo
    .QueryCollectionAsync(userId, c => c.MfaDevices);
var verified = mfaDevices.Where(d => d.IsVerified);

// Token status
var activeTokens = tokens.Where(t => 
    !t.IsRevoked && 
    t.ExpiresAt > DateTimeOffset.UtcNow);
```

## Use Cases
-    User authentication systems
-     Authorization frameworks
-    Identity management
-    Compliance and audit
-    Security monitoring

## Best Practices
- Hash passwords with bcrypt/argon2
- Encrypt sensitive data at rest
- Use short-lived access tokens
- Implement refresh token rotation
- Monitor for suspicious activity
- Audit all security events

## Verification
Check Azure Storage Explorer to see both tables:
- `UserCredentials` - Authentication data
- `UserPermissions` - Authorization data

Both use `UserId` as PartitionKey for efficient queries.

## Next Steps
   Combine patterns from all demos for production systems
