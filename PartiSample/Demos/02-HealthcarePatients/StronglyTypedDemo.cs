using Microsoft.Extensions.DependencyInjection;
using PartiTables;
using PartiSample.Models;

namespace PartiSample.Demos;

/// <summary>
/// DEMO 2: Healthcare Patient Management
/// 
/// Shows: Strongly-typed entity models (EF-like)
/// Best for: Complex domain models, auto-generated keys
/// 
/// Key Concepts:
/// - Type-safe operations with C# classes
/// - Automatic RowKey generation via IRowKeyBuilder
/// - Collection properties for related data
/// - Query specific collections efficiently
/// </summary>
public static class StronglyTypedDemo
{
    public static async Task RunAsync(IServiceProvider sp)
    {
        Console.WriteLine("=== DEMO 2: Healthcare Patient Management ===");
        Console.WriteLine("Strongly-typed entity models with auto-generated keys\n");

        var repo = sp.GetRequiredService<PartitionRepository<Patient>>();
        var tenantId = "clinic-001";
        var patientId = "patient-456";

        // ???????????????????????????????????????????????????????????
        // CREATE: Build strongly-typed entity
        // ???????????????????????????????????????????????????????????
        Console.WriteLine("? Creating patient with related data...");
        
        var patient = new Patient
        {
            TenantId = tenantId,
            PatientId = patientId
        };

        // Add metadata (RowKey auto-generated as: patient-456-meta)
        patient.Meta.Add(new PatientMeta
        {
            FirstName = "Jane",
            LastName = "Smith",
            Email = "jane.smith@example.com",
            Status = "Active",
            DateOfBirth = new DateTime(1990, 3, 15)
        });

        // Add consents (RowKey: patient-456-consent-{id}-v{version})
        patient.Consents.Add(new Consent
        {
            Type = "DataSharing",
            Status = "Granted",
            Description = "Consent to share data with research partners"
        });

        patient.Consents.Add(new Consent
        {
            Type = "Marketing",
            Status = "Denied"
        });

        // Add devices (RowKey: patient-456-device-{deviceId})
        patient.Devices.Add(new DeviceLink
        {
            DeviceId = "dev-fitbit-001",
            Model = "Charge 5",
            Manufacturer = "FitBit",
            MappingStatus = "Active"
        });

        await repo.SaveAsync(patient);
        Console.WriteLine("  ? Patient saved with all related data");
        Console.WriteLine("    (RowKeys auto-generated!)\n");

        // ???????????????????????????????????????????????????????????
        // READ: Load complete entity
        // ???????????????????????????????????????????????????????????
        Console.WriteLine("? Loading patient...");
        
        var loaded = await repo.FindAsync(tenantId);
        if (loaded != null)
        {
            Console.WriteLine($"  ? Patient: {loaded.PatientId}\n");
            
            // Display metadata
            if (loaded.Meta.Count > 0)
            {
                var meta = loaded.Meta[0];
                Console.WriteLine($"  ?? Metadata:");
                Console.WriteLine($"    RowKey: {meta.RowKeyId}");
                Console.WriteLine($"    Name: {meta.FirstName} {meta.LastName}");
                Console.WriteLine($"    Email: {meta.Email}");
                Console.WriteLine($"    DOB: {meta.DateOfBirth?.ToShortDateString()}\n");
            }

            // Display consents
            Console.WriteLine($"  ?? Consents ({loaded.Consents.Count}):");
            foreach (var c in loaded.Consents)
            {
                Console.WriteLine($"    • {c.Type}: {c.Status}");
                Console.WriteLine($"      RowKey: {c.RowKeyId}");
            }

            // Display devices
            Console.WriteLine($"\n  ?? Devices ({loaded.Devices.Count}):");
            foreach (var d in loaded.Devices)
            {
                Console.WriteLine($"    • {d.Model} ({d.Manufacturer})");
                Console.WriteLine($"      RowKey: {d.RowKeyId}");
            }
        }

        // ???????????????????????????????????????????????????????????
        // QUERY: Get specific collection only (efficient!)
        // ???????????????????????????????????????????????????????????
        Console.WriteLine("\n? Querying consents only (fast!)...");
        
        var consentsOnly = await repo.QueryCollectionAsync(tenantId, p => p.Consents);
        Console.WriteLine($"  ? Retrieved {consentsOnly.Count} consents");
        Console.WriteLine("    (Didn't load meta or devices)\n");
        
        foreach (var consent in consentsOnly)
        {
            Console.WriteLine($"    • {consent.Type} (v{consent.Version}): {consent.Status}");
        }

        // ???????????????????????????????????????????????????????????
        // UPDATE: Modify and add new items
        // ???????????????????????????????????????????????????????????
        Console.WriteLine("\n? Updating patient...");
        
        if (loaded != null)
        {
            // Update existing
            loaded.Meta[0].Status = "Verified";
            
            // Add new (RowKey will be auto-generated)
            loaded.Consents.Add(new Consent
            {
                Type = "ThirdParty",
                Status = "Granted"
            });
            
            await repo.SaveAsync(loaded);
            Console.WriteLine("  ? Patient updated");
            Console.WriteLine("    • Changed status to 'Verified'");
            Console.WriteLine("    • Added new consent (key auto-generated)\n");
        }
    }
}
