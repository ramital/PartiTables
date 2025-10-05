using Azure.Data.Tables;
using Microsoft.Extensions.DependencyInjection;
using PartiTables;
using PartiSample.Models;

namespace PartiSample.Tests;

/// <summary>
/// Demonstrates that multiple tables are created and used independently
/// </summary>
public static class TableVerification
{
    public static async Task VerifyMultipleTablesAsync(IServiceProvider sp)
    {
        Console.WriteLine("\n=== Table Verification Test ===\n");

        var tableServiceClient = sp.GetRequiredService<TableServiceClient>();

        // List all tables
        Console.WriteLine("?? Listing all tables in storage account:\n");
        var tables = new List<string>();
        
        await foreach (var table in tableServiceClient.QueryAsync())
        {
            tables.Add(table.Name);
            Console.WriteLine($"  ? Table: {table.Name}");
        }

        // Verify expected tables exist
        Console.WriteLine("\n?? Verifying Security Demo tables:\n");
        
        var expectedTables = new[] { "UserCredentials", "UserPermissions" };
        foreach (var expectedTable in expectedTables)
        {
            if (tables.Contains(expectedTable))
            {
                Console.WriteLine($"  ? {expectedTable} - EXISTS");
                
                // Count entities in the table
                var tableClient = tableServiceClient.GetTableClient(expectedTable);
                var count = 0;
                await foreach (var entity in tableClient.QueryAsync<TableEntity>())
                {
                    count++;
                }
                Console.WriteLine($"    Entities: {count}");
            }
            else
            {
                Console.WriteLine($"  ? {expectedTable} - NOT FOUND");
            }
        }

        // Show partition keys in each table
        Console.WriteLine("\n?? Partition keys per table:\n");
        
        foreach (var tableName in new[] { "UserCredentials", "UserPermissions" })
        {
            if (!tables.Contains(tableName)) continue;

            var tableClient = tableServiceClient.GetTableClient(tableName);
            var partitionKeys = new HashSet<string>();
            
            await foreach (var entity in tableClient.QueryAsync<TableEntity>())
            {
                partitionKeys.Add(entity.PartitionKey);
            }

            Console.WriteLine($"  {tableName}:");
            foreach (var pk in partitionKeys.OrderBy(k => k))
            {
                Console.WriteLine($"    - {pk}");
            }
        }

        Console.WriteLine("\n=== Verification Complete ===\n");
    }

    public static async Task ShowTableDetailsAsync(IServiceProvider sp, string tableName, string partitionKey)
    {
        Console.WriteLine($"\n=== Table Details: {tableName} ===\n");
        Console.WriteLine($"PartitionKey: {partitionKey}\n");

        var tableServiceClient = sp.GetRequiredService<TableServiceClient>();
        var tableClient = tableServiceClient.GetTableClient(tableName);

        var entities = new List<TableEntity>();
        await foreach (var entity in tableClient.QueryAsync<TableEntity>(
            filter: $"PartitionKey eq '{partitionKey}'"))
        {
            entities.Add(entity);
        }

        Console.WriteLine($"Total Entities: {entities.Count}\n");
        Console.WriteLine("Row Keys:");
        
        foreach (var entity in entities.OrderBy(e => e.RowKey))
        {
            Console.WriteLine($"  - {entity.RowKey}");
        }

        Console.WriteLine();
    }
}
