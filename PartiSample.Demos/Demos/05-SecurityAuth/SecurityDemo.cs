using Microsoft.Extensions.DependencyInjection;
using PartiTables;
using PartiSample.Models;

namespace PartiSample.Demos;

/// <summary>
/// Demo 5: User Security System
/// Demonstrates authentication and authorization patterns with 2 separate tables
/// </summary>
public static class SecurityDemo
{
    public static async Task RunAsync(IServiceProvider sp)
    {
        Console.WriteLine("\n=== User Security System Demo ===\n");

        var userId = "user-alice-2024";

        // Get repositories for both security tables
        var credentialsRepo = sp.GetRequiredService<PartitionRepository<UserCredentials>>();
        var permissionsRepo = sp.GetRequiredService<PartitionRepository<UserPermissions>>();

        Console.WriteLine("▶ Setup Authentication\n");

        // TABLE 1: User Credentials
        var credentials = new UserCredentials
        {
            UserId = userId
        };

        // Add password history
        credentials.PasswordHistory.Add(new PasswordHistory
        {
            PasswordHash = "hash_abc123_v1", // In production: use bcrypt/argon2
            IsActive = true,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(90)
        });

        // Add MFA devices
        credentials.MfaDevices.Add(new MfaDevice
        {
            DeviceType = "TOTP",
            DeviceName = "Google Authenticator",
            SecretKey = "encrypted_secret_key_1",
            IsVerified = true
        });

        credentials.MfaDevices.Add(new MfaDevice
        {
            DeviceType = "SMS",
            DeviceName = "Mobile +1 555-0123",
            SecretKey = "encrypted_phone_number",
            IsVerified = true
        });

        // Add initial login attempts (successful registration)
        credentials.LoginAttempts.Add(new LoginAttempt
        {
            IpAddress = "192.168.1.100",
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)",
            IsSuccessful = true
        });

        await credentialsRepo.SaveAsync(credentials);
        Console.WriteLine($"  ✓ Saved to UserCredentials table\n");

        Console.WriteLine("▶ Setup Authorization\n");

        // TABLE 2: User Permissions
        var permissions = new UserPermissions
        {
            UserId = userId,
            Email = "alice@company.com"
        };

        // Assign roles
        permissions.Roles.Add(new RoleAssignment
        {
            RoleId = "role-editor",
            RoleName = "Editor",
            Scope = "Organization",
            ScopeId = "org-acme-corp",
            AssignedBy = "system"
        });

        permissions.Roles.Add(new RoleAssignment
        {
            RoleId = "role-viewer",
            RoleName = "Viewer",
            Scope = "Project",
            ScopeId = "project-123",
            AssignedBy = "admin@company.com"
        });

        // Grant specific resource permissions
        permissions.ResourcePermissions.Add(new ResourcePermission
        {
            ResourceType = "Document",
            ResourceId = "doc-budget-2024",
            Permissions = new[] { "read", "write", "share" },
            GrantedBy = "admin@company.com"
        });

        permissions.ResourcePermissions.Add(new ResourcePermission
        {
            ResourceType = "Database",
            ResourceId = "db-analytics",
            Permissions = new[] { "read" },
            GrantedBy = "system",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30)
        });

        // Issue access tokens
        permissions.AccessTokens.Add(new AccessToken
        {
            TokenType = "Bearer",
            TokenHash = "hash_token_abc123",
            Scopes = new[] { "read:profile", "write:documents" },
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
        });

        permissions.AccessTokens.Add(new AccessToken
        {
            TokenType = "Refresh",
            TokenHash = "hash_refresh_xyz789",
            Scopes = new[] { "refresh" },
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30)
        });

        await permissionsRepo.SaveAsync(permissions);
        Console.WriteLine($"  ✓ Saved to UserPermissions table\n");

        Console.WriteLine("▶ Simulate Failed Login Attempts\n");

        // Simulate suspicious login attempts
        var creds = await credentialsRepo.FindAsync(userId);
        if (creds != null)
        {
            // Failed attempt 1
            creds.LoginAttempts.Add(new LoginAttempt
            {
                IpAddress = "203.0.113.50",
                UserAgent = "curl/7.68.0",
                IsSuccessful = false,
                FailureReason = "Invalid password"
            });

            // Failed attempt 2
            creds.LoginAttempts.Add(new LoginAttempt
            {
                IpAddress = "203.0.113.50",
                UserAgent = "curl/7.68.0",
                IsSuccessful = false,
                FailureReason = "Invalid password"
            });

            // Failed attempt 3
            creds.LoginAttempts.Add(new LoginAttempt
            {
                IpAddress = "203.0.113.50",
                UserAgent = "curl/7.68.0",
                IsSuccessful = false,
                FailureReason = "Account locked"
            });

            await credentialsRepo.SaveAsync(creds);
            Console.WriteLine("  ⚠️  3 failed attempts from suspicious IP\n");
        }

        Console.WriteLine("▶ Successful Login with MFA\n");

        // Simulate successful login
        if (creds != null)
        {
            creds.LoginAttempts.Add(new LoginAttempt
            {
                IpAddress = "192.168.1.100",
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)",
                IsSuccessful = true
            });

            // Update MFA device last used
            var totpDevice = creds.MfaDevices.First(d => d.DeviceType == "TOTP");
            totpDevice.LastUsedAt = DateTimeOffset.UtcNow;

            await credentialsRepo.SaveAsync(creds);
            Console.WriteLine("  ✓ Login successful\n");
        }

        Console.WriteLine("▶ Password Change & Token Revocation\n");

        // User changes password
        if (creds != null)
        {
            // Mark old password as inactive
            creds.PasswordHistory.First().IsActive = false;

            // Add new password
            creds.PasswordHistory.Add(new PasswordHistory
            {
                PasswordHash = "hash_def456_v2",
                IsActive = true,
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(90)
            });

            await credentialsRepo.SaveAsync(creds);
            Console.WriteLine("  ✓ Password changed");
        }

        // Revoke all existing tokens after password change
        var perms = await permissionsRepo.FindAsync(userId);
        if (perms != null)
        {
            foreach (var token in perms.AccessTokens.Where(t => !t.IsRevoked))
            {
                token.IsRevoked = true;
                token.RevokedAt = DateTimeOffset.UtcNow;
                token.RevokedReason = "Password changed";
            }

            // Issue new tokens
            perms.AccessTokens.Add(new AccessToken
            {
                TokenType = "Bearer",
                TokenHash = "hash_token_new123",
                Scopes = new[] { "read:profile", "write:documents" },
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
            });

            await permissionsRepo.SaveAsync(perms);
            Console.WriteLine("  ✓ Tokens revoked\n");
        }

        Console.WriteLine("▶ Security Analytics\n");

        // Query login attempts
        var loginAttempts = await credentialsRepo.QueryCollectionAsync(userId, c => c.LoginAttempts);
        var failedAttempts = loginAttempts.Where(l => !l.IsSuccessful).ToList();
        var successfulAttempts = loginAttempts.Where(l => l.IsSuccessful).ToList();

        Console.WriteLine($"  Login Attempts: {loginAttempts.Count} ({successfulAttempts.Count} success, {failedAttempts.Count} failed)");

        if (failedAttempts.Any())
        {
            var suspiciousIPs = failedAttempts.GroupBy(l => l.IpAddress)
                .Where(g => g.Count() >= 3)
                .Select(g => g.Key);
            
            if (suspiciousIPs.Any())
            {
                foreach (var ip in suspiciousIPs)
                {
                    var count = failedAttempts.Count(l => l.IpAddress == ip);
                    Console.WriteLine($"  ⚠️  Suspicious IP: {ip} ({count} attempts)");
                }
            }
        }

        // Query MFA devices
        var mfaDevices = await credentialsRepo.QueryCollectionAsync(userId, c => c.MfaDevices);
        Console.WriteLine($"  MFA Devices: {mfaDevices.Count} ({mfaDevices.Count(d => d.IsVerified)} verified)");

        // Query roles and permissions
        var roles = await permissionsRepo.QueryCollectionAsync(userId, p => p.Roles);
        var resources = await permissionsRepo.QueryCollectionAsync(userId, p => p.ResourcePermissions);
        var tokens = await permissionsRepo.QueryCollectionAsync(userId, p => p.AccessTokens);

        Console.WriteLine($"  Roles: {roles.Count}");
        Console.WriteLine($"  Resource Permissions: {resources.Count}");
        
        var activeTokens = tokens.Where(t => !t.IsRevoked && t.ExpiresAt > DateTimeOffset.UtcNow).ToList();
        var revokedTokens = tokens.Where(t => t.IsRevoked).ToList();
        Console.WriteLine($"  Tokens: {tokens.Count} ({activeTokens.Count} active, {revokedTokens.Count} revoked)");

        Console.WriteLine("\n✓ Demo Complete - 2 Tables Created");
    }
}
