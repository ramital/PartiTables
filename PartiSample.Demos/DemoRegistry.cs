using Microsoft.Extensions.DependencyInjection;
using PartiTables;
using PartiSample.Demos;
using Polly;

namespace PartiSample;

/// <summary>
/// Configuration for a demo scenario
/// </summary>
public class DemoConfiguration
{
    public string Id { get; init; } = default!;
    public string Name { get; init; } = default!;
    public string Description { get; init; } = default!;
    public string TableName { get; init; } = default!;
    public Type? RepositoryType { get; init; }
    public Type[]? AdditionalRepositoryTypes { get; init; }
    public Func<IServiceProvider, Task> RunAction { get; init; } = default!;
}

/// <summary>
/// Registry of all available demos with their configurations
/// Demos are organized by complexity and use case
/// </summary>
public static class DemoRegistry
{
    public static readonly List<DemoConfiguration> Demos = new()
    {
        new DemoConfiguration
        {
            Id = "1",
            Name = "Simple Fluent API",
            Description = "Low-level API without entity models (best for prototyping)",
            TableName = "SampleData",
            RepositoryType = null,
            RunAction = SimpleFluentDemo.RunAsync
        },
        
        new DemoConfiguration
        {
            Id = "6",
            Name = "CRUD & Query",
            Description = "Complete guide to Create, Read, Update, Delete, and Query",
            TableName = "Tasks",
            RepositoryType = typeof(Models.TaskProject),
            RunAction = SimpleCrudDemo.RunAsync
        },

        new DemoConfiguration
        {
            Id = "2",
            Name = "Healthcare - Patient Management",
            Description = "Strongly-typed entities for medical records with auto-generated keys",
            TableName = "PatientData",
            RepositoryType = typeof(Models.Patient),
            RunAction = StronglyTypedDemo.RunAsync
        },
        
        new DemoConfiguration
        {
            Id = "3",
            Name = "E-commerce - Order Management",
            Description = "Customer orders with addresses, payments, and preferences",
            TableName = "OrderData",
            RepositoryType = typeof(Models.CustomerOrders),
            RunAction = EcommerceDemo.RunAsync
        },

        new DemoConfiguration
        {
            Id = "4",
            Name = "Multi-Table SaaS",
            Description = "Enterprise scenario with 3 coordinated tables (config, users, audit)",
            TableName = "TenantConfig",
            RepositoryType = typeof(Models.TenantConfiguration),
            AdditionalRepositoryTypes = new[]
            {
                typeof(Models.TenantUsers),
                typeof(Models.TenantAuditLog)
            },
            RunAction = MultiTableDemo.RunAsync
        },
        
        new DemoConfiguration
        {
            Id = "5",
            Name = "Security - Auth & Authorization",
            Description = "User security with 2 tables (credentials + permissions)",
            TableName = "UserCredentials",
            RepositoryType = typeof(Models.UserCredentials),
            AdditionalRepositoryTypes = new[]
            {
                typeof(Models.UserPermissions)
            },
            RunAction = SecurityDemo.RunAsync
        }
    };

    public static DemoConfiguration? GetDemo(string id)
    {
        return Demos.FirstOrDefault(d => d.Id == id);
    }

    public static void DisplayMenu()
    {
        Console.WriteLine("????????????????????????????????????????");
        Console.WriteLine("         PartiTables Demo Menu");
        Console.WriteLine("????????????????????????????????????????\n");
        
        Console.WriteLine("BASIC DEMOS:");
        Console.WriteLine("  1. Simple Fluent API");
        Console.WriteLine("     ?? Low-level operations without models");
        Console.WriteLine("  6. CRUD & Query Basics");
        Console.WriteLine("     ?? Complete guide to all operations\n");
        
        Console.WriteLine("DOMAIN-SPECIFIC DEMOS:");
        Console.WriteLine("  2. Healthcare - Patient Management");
        Console.WriteLine("     ?? Medical records with auto-generated keys");
        Console.WriteLine("  3. E-commerce - Order Management");
        Console.WriteLine("     ?? Customer orders and profiles\n");
        
        Console.WriteLine("ADVANCED DEMOS:");
        Console.WriteLine("  4. Multi-Table SaaS");
        Console.WriteLine("     ?? 3 coordinated tables for enterprise");
        Console.WriteLine("  5. Security - Auth & Authorization");
        Console.WriteLine("     ?? 2 tables for credentials and permissions\n");

        Console.WriteLine("????????????????????????????????????????");
        Console.WriteLine("  0. Run ALL Demos (sequential)\n");
        Console.WriteLine("????????????????????????????????????????");
        Console.Write("Enter choice (0-6): ");
    }

    public static async Task RunAllDemosAsync(IServiceProvider serviceProvider)
    {
        Console.Clear();
        Console.WriteLine("??????????????????????????????????????????");
        Console.WriteLine("?       Running ALL Demos                ?");
        Console.WriteLine("??????????????????????????????????????????\n");

        var demoOrder = new[] { "1", "6", "2", "3", "4", "5" };
        
        for (int i = 0; i < demoOrder.Length; i++)
        {
            var demo = GetDemo(demoOrder[i]);
            if (demo == null) continue;

            Console.WriteLine($"\n[{i + 1}/{demoOrder.Length}] {demo.Name}");
            Console.WriteLine(new string('?', 60));

            var services = new ServiceCollection();
            ConfigureServices(services, demo);
            var sp = services.BuildServiceProvider();

            try
            {
                await demo.RunAction(sp);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n? Demo failed: {ex.Message}");
            }

            if (i < demoOrder.Length - 1)
            {
                Console.WriteLine($"\nPress Enter to continue to next demo...");
                Console.ReadLine();
                Console.Clear();
            }
        }

        Console.WriteLine("\n\n??????????????????????????????????????????");
        Console.WriteLine("?    All Demos Completed!                ?");
        Console.WriteLine("??????????????????????????????????????????");
    }

    public static void ConfigureServices(
        IServiceCollection services, 
        DemoConfiguration demo,
        string connectionString = "UseDevelopmentStorage=true")
    {
        // Configure PartiTables with primary table
        services.AddPartiTables(opts =>
        {
            opts.ConnectionString = connectionString;
            opts.TableName = demo.TableName;
            opts.ResiliencePolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(3, i => TimeSpan.FromMilliseconds(100 * i));
        });

        // Register primary repository if needed
        if (demo.RepositoryType != null)
        {
            var addRepositoryMethod = typeof(ServiceCollectionExtensions)
                .GetMethod(nameof(ServiceCollectionExtensions.AddPartitionRepository))!
                .MakeGenericMethod(demo.RepositoryType);
            
            addRepositoryMethod.Invoke(null, new object[] { services });
        }

        // Register additional repositories for multi-table demos
        if (demo.AdditionalRepositoryTypes != null)
        {
            foreach (var additionalType in demo.AdditionalRepositoryTypes)
            {
                var addRepositoryMethod = typeof(ServiceCollectionExtensions)
                    .GetMethod(nameof(ServiceCollectionExtensions.AddPartitionRepository))!
                    .MakeGenericMethod(additionalType);
                
                addRepositoryMethod.Invoke(null, new object[] { services });
            }
        }
    }
}
