# Demo 4: Multi-Table SaaS Application

## Overview
This demo shows **advanced multi-table coordination** for enterprise SaaS applications using **3 separate Azure Tables**.

## What You'll Learn
- Coordinate data across multiple tables
- Design enterprise SaaS architecture
- Manage tenant configuration, users, and audit logs
- Cross-table queries and analytics
- Best practices for multi-table scenarios

## Architecture
```
???????????????????????????????
?  Table 1: TenantConfig      ?
?  - Settings                 ?
?  - Features                 ?
?  - Quotas                   ?
???????????????????????????????

???????????????????????????????
?  Table 2: UserData          ?
?  - Users                    ?
?  - Roles                    ?
?  - Sessions                 ?
???????????????????????????????

???????????????????????????????
?  Table 3: AuditLogs         ?
?  - Audit Entries            ?
?  - Security Events          ?
?  - Data Changes             ?
???????????????????????????????
```

## Why Multiple Tables?
- **Separation of Concerns**: Config, users, and audit have different access patterns
- **Security**: Audit logs isolated from operational data
- **Performance**: Query only what you need
- **Compliance**: Separate audit trail for regulations

## Key Features

### Tenant Configuration
```csharp
var config = new TenantConfiguration 
{ 
    TenantId = "acme-corp",
    Plan = "Enterprise" 
};
config.Features.Add(new TenantFeature 
{ 
    FeatureName = "AdvancedAnalytics",
    IsEnabled = true 
});
```

### User Management
```csharp
var users = new TenantUsers { TenantId = "acme-corp" };
users.Users.Add(new User { Email = "admin@acme.com" });
users.Roles.Add(new UserRole 
{ 
    UserId = userId,
    RoleName = "Admin" 
});
```

### Audit Trail
```csharp
var audit = new TenantAuditLog { TenantId = "acme-corp" };
audit.Entries.Add(new AuditEntry 
{
    Action = "UserCreated",
    UserId = "system",
    IsSuccess = true
});
```

## Cross-Table Operations
```csharp
// Update quota when user is created
var config = await configRepo.FindAsync(tenantId);
config.Quotas.First(q => q.ResourceType == "Users").Used++;
await configRepo.SaveAsync(config);

// Log the action
var audit = await auditRepo.FindAsync(tenantId);
audit.Entries.Add(new AuditEntry { Action = "UserCreated" });
await auditRepo.SaveAsync(audit);
```

## Enterprise Patterns
  **RBAC** (Role-Based Access Control)  
  **Quota Management**  
  **Audit Logging**  
  **Session Tracking**  
  **Security Events**  
  **Feature Flags**  

## Use Cases
-    Multi-tenant SaaS platforms
-    Enterprise applications
-    Compliance-required systems
-    Analytics and reporting

## Next Steps
   **Demo 5** shows security-focused multi-table patterns
