using Microsoft.Extensions.DependencyInjection;
using PartiTables;
using PartiSample.Models;

namespace PartiSample.Demos;

/// <summary>
/// Demo 4: Multi-Table SaaS Application
/// Advanced scenario showing coordination between 3 tables:
/// 1. TenantConfig - Tenant settings and features
/// 2. UserData - Users, roles, sessions
/// 3. AuditLogs - Audit trail and security events
/// </summary>
public static class MultiTableDemo
{
    public static async Task RunAsync(IServiceProvider sp)
    {
        Console.WriteLine("\n=== Multi-Table SaaS Application Demo ===\n");

        var tenantId = "tenant-acme-corp";

        // Get repositories for all 3 tables
        var configRepo = sp.GetRequiredService<PartitionRepository<TenantConfiguration>>();
        var userRepo = sp.GetRequiredService<PartitionRepository<TenantUsers>>();
        var auditRepo = sp.GetRequiredService<PartitionRepository<TenantAuditLog>>();

        Console.WriteLine("? Initialize Tenant Configuration\n");
        
        // TABLE 1: Tenant Configuration
        var tenantConfig = new TenantConfiguration
        {
            TenantId = tenantId,
            TenantName = "ACME Corporation",
            Plan = "Enterprise"
        };

        // Add settings
        tenantConfig.Settings.Add(new TenantSettings
        {
            Category = "Security",
            Key = "PasswordPolicy",
            Value = "Strong"
        });

        tenantConfig.Settings.Add(new TenantSettings
        {
            Category = "Billing",
            Key = "PaymentMethod",
            Value = "CreditCard"
        });

        // Add features
        tenantConfig.Features.Add(new TenantFeature
        {
            FeatureName = "AdvancedAnalytics",
            IsEnabled = true,
            UsageLimit = 1000
        });

        tenantConfig.Features.Add(new TenantFeature
        {
            FeatureName = "APIAccess",
            IsEnabled = true,
            UsageLimit = 10000
        });

        // Add quotas
        tenantConfig.Quotas.Add(new TenantQuota
        {
            ResourceType = "Users",
            Limit = 100,
            Used = 0,
            ResetDate = DateTimeOffset.UtcNow.AddMonths(1)
        });

        tenantConfig.Quotas.Add(new TenantQuota
        {
            ResourceType = "Storage",
            Limit = 1000, // GB
            Used = 0,
            ResetDate = DateTimeOffset.UtcNow.AddMonths(1)
        });

        await configRepo.SaveAsync(tenantConfig);
        Console.WriteLine($"  ? Saved to TenantConfig table ({tenantConfig.Plan} plan)\n");

        // Log this action
        var auditLog = new TenantAuditLog { TenantId = tenantId };
        auditLog.Entries.Add(new AuditEntry
        {
            UserId = "system",
            Action = "TenantCreated",
            ResourceType = "Tenant",
            ResourceId = tenantId,
            Details = $"Tenant '{tenantConfig.TenantName}' initialized with {tenantConfig.Plan} plan",
            IsSuccess = true
        });
        await auditRepo.SaveAsync(auditLog);

        Console.WriteLine("? Create Users and Assign Roles\n");

        // TABLE 2: User Management
        var tenantUsers = new TenantUsers { TenantId = tenantId };

        // Create admin user
        var adminUser = new User
        {
            Email = "admin@acme.com",
            FirstName = "John",
            LastName = "Admin",
            Status = "Active"
        };
        tenantUsers.Users.Add(adminUser);

        // Assign admin role
        tenantUsers.Roles.Add(new UserRole
        {
            UserId = adminUser.UserId,
            RoleName = "Admin",
            Permissions = new[] { "users.manage", "settings.edit", "billing.view", "audit.view" }
        });

        // Create manager user
        var managerUser = new User
        {
            Email = "manager@acme.com",
            FirstName = "Sarah",
            LastName = "Manager",
            Status = "Active"
        };
        tenantUsers.Users.Add(managerUser);

        tenantUsers.Roles.Add(new UserRole
        {
            UserId = managerUser.UserId,
            RoleName = "Manager",
            Permissions = new[] { "users.view", "reports.generate" }
        });

        // Create regular user
        var regularUser = new User
        {
            Email = "user@acme.com",
            FirstName = "Bob",
            LastName = "User",
            Status = "Active"
        };
        tenantUsers.Users.Add(regularUser);

        tenantUsers.Roles.Add(new UserRole
        {
            UserId = regularUser.UserId,
            RoleName = "User",
            Permissions = new[] { "dashboard.view", "profile.edit" }
        });

        await userRepo.SaveAsync(tenantUsers);
        Console.WriteLine($"  ? Saved to UserData table ({tenantUsers.Users.Count} users)\n");

        // Update quota
        var config = await configRepo.FindAsync(tenantId);
        if (config != null)
        {
            var userQuota = config.Quotas.First(q => q.ResourceType == "Users");
            userQuota.Used = tenantUsers.Users.Count;
            await configRepo.SaveAsync(config);
        }

        // Log user creation
        var audit = await auditRepo.FindAsync(tenantId);
        if (audit != null)
        {
            foreach (var user in tenantUsers.Users)
            {
                audit.Entries.Add(new AuditEntry
                {
                    UserId = "system",
                    Action = "UserCreated",
                    ResourceType = "User",
                    ResourceId = user.UserId,
                    Details = $"User {user.Email} created",
                    IsSuccess = true
                });
            }
            await auditRepo.SaveAsync(audit);
        }

        Console.WriteLine("? Simulate User Sessions\n");

        // Simulate user login sessions
        var users = await userRepo.FindAsync(tenantId);
        if (users != null)
        {
            // Admin logs in
            var admin = users.Users.First(u => u.Email == "admin@acme.com");
            admin.LastLoginAt = DateTimeOffset.UtcNow;
            
            users.Sessions.Add(new UserSession
            {
                UserId = admin.UserId,
                IpAddress = "192.168.1.100",
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)",
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(8)
            });

            // Manager logs in
            var manager = users.Users.First(u => u.Email == "manager@acme.com");
            manager.LastLoginAt = DateTimeOffset.UtcNow;
            
            users.Sessions.Add(new UserSession
            {
                UserId = manager.UserId,
                IpAddress = "192.168.1.101",
                UserAgent = "Mozilla/5.0 (Macintosh; Intel Mac OS X)",
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(8)
            });

            await userRepo.SaveAsync(users);
            Console.WriteLine($"  ? Active sessions: {users.Sessions.Count}\n");
        }

        // Log login events
        audit = await auditRepo.FindAsync(tenantId);
        if (audit != null)
        {
            audit.Entries.Add(new AuditEntry
            {
                UserId = adminUser.UserId,
                Action = "Login",
                ResourceType = "Session",
                ResourceId = "session-1",
                Details = "Admin user logged in successfully",
                IsSuccess = true
            });

            audit.Entries.Add(new AuditEntry
            {
                UserId = managerUser.UserId,
                Action = "Login",
                ResourceType = "Session",
                ResourceId = "session-2",
                Details = "Manager user logged in successfully",
                IsSuccess = true
            });

            await auditRepo.SaveAsync(audit);
        }

        Console.WriteLine("? Simulate Security Event\n");

        // Simulate failed login attempt
        audit = await auditRepo.FindAsync(tenantId);
        if (audit != null)
        {
            audit.SecurityEvents.Add(new SecurityEvent
            {
                EventType = "FailedLogin",
                UserId = "unknown",
                IpAddress = "198.51.100.50",
                Severity = "Medium",
                Description = "Multiple failed login attempts detected"
            });

            audit.Entries.Add(new AuditEntry
            {
                UserId = "unknown",
                Action = "Login",
                ResourceType = "Session",
                ResourceId = "failed-attempt",
                Details = "Failed login attempt from suspicious IP",
                IsSuccess = false
            });

            await auditRepo.SaveAsync(audit);
            Console.WriteLine("  ??  Security event logged\n");
        }

        Console.WriteLine("? Tenant Analytics Dashboard\n");

        // Reload all data for analytics
        var finalConfig = await configRepo.FindAsync(tenantId);
        var finalUsers = await userRepo.FindAsync(tenantId);
        var finalAudit = await auditRepo.FindAsync(tenantId);

        Console.WriteLine($"  Tenant: {finalConfig?.TenantName} ({finalConfig?.Plan})");
        
        if (finalConfig != null)
        {
            Console.WriteLine($"  Resource Usage:");
            foreach (var quota in finalConfig.Quotas)
            {
                var percentage = (quota.Used * 100.0) / quota.Limit;
                Console.WriteLine($"    • {quota.ResourceType}: {quota.Used}/{quota.Limit} ({percentage:F0}%)");
            }
        }

        if (finalUsers != null)
        {
            Console.WriteLine($"  Users: {finalUsers.Users.Count} total, {finalUsers.Sessions.Count(s => s.IsActive)} active sessions");
        }

        if (finalAudit != null)
        {
            Console.WriteLine($"  Audit: {finalAudit.Entries.Count} entries, {finalAudit.SecurityEvents.Count} security events");
        }

        Console.WriteLine("\n? Demo Complete - 3 Tables Coordinated");
        Console.WriteLine("  • TenantConfig (settings, features, quotas)");
        Console.WriteLine("  • UserData (users, roles, sessions)");
        Console.WriteLine("  • AuditLogs (audit trail, security events)");
    }
}
