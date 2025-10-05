using Azure.Data.Tables;
using Microsoft.Extensions.DependencyInjection;
using PartiTables.Interfaces;

namespace PartiTables;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds PartiTables services to the DI container.
    /// </summary>
    public static IServiceCollection AddPartiTables(
        this IServiceCollection services,
        Action<TableOptions> configure)
    {
        if (services is null)
            throw new ArgumentNullException(nameof(services));

        if (configure is null)
            throw new ArgumentNullException(nameof(configure));

        var options = new TableOptions();
        configure(options);
        options.Validate();

        services.AddSingleton(options);

        services.AddSingleton(provider =>
        {
            var opts = provider.GetRequiredService<TableOptions>();
            return new TableServiceClient(opts.ConnectionString);
        });

        services.AddSingleton(provider =>
        {
            var opts = provider.GetRequiredService<TableOptions>();
            var serviceClient = provider.GetRequiredService<TableServiceClient>();
            var tableClient = serviceClient.GetTableClient(opts.TableName);

            if (opts.CreateTableIfNotExists)
            {
                tableClient.CreateIfNotExists();
            }

            return tableClient;
        });

        services.AddSingleton<IPartitionClient>(provider =>
        {
            var tableClient = provider.GetRequiredService<TableClient>();
            var opts = provider.GetRequiredService<TableOptions>();
            return new PartitionClient(tableClient, opts.ResiliencePolicy);
        });

        return services;
    }

    /// <summary>
    /// Adds PartiTables with default options for local development (Azurite).
    /// </summary>
    public static IServiceCollection AddPartiTablesForDevelopment(
        this IServiceCollection services,
        string tableName = "DefaultTable")
    {
        return services.AddPartiTables(opts =>
        {
            opts.ConnectionString = "UseDevelopmentStorage=true";
            opts.TableName = tableName;
        });
    }

    /// <summary>
    /// Registers a repository for a specific entity type.
    /// </summary>
    public static IServiceCollection AddPartitionRepository<T>(this IServiceCollection services)
        where T : class, new()
    {
        services.AddScoped<PartitionRepository<T>>();
        return services;
    }
}