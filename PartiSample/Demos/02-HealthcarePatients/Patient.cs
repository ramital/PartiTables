using PartiTables;

namespace PartiSample.Models;

/// <summary>
/// Healthcare patient entity with related medical records
/// Uses auto-generated RowKeys via IRowKeyBuilder
/// </summary>
[TablePartition("PatientData", "{TenantId}")]
public class Patient
{
    public string TenantId { get; set; } = default!;
    public string PatientId { get; set; } = default!;

    [RowKeyPrefix("")]
    public List<PatientMeta> Meta { get; set; } = new();

    [RowKeyPrefix("")]
    public List<Consent> Consents { get; set; } = new();

    [RowKeyPrefix("")]
    public List<DeviceLink> Devices { get; set; } = new();
}

/// <summary>
/// Patient metadata (demographics, contact info)
/// RowKey pattern: {patientId}-meta
/// </summary>
public class PatientMeta : RowEntity, IRowKeyBuilder
{
    public string? Email { get; set; }
    public string? Status { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime? DateOfBirth { get; set; }

    public string BuildRowKey(RowKeyContext context)
    {
        var patientId = context.GetParentProperty<string>("PatientId");
        return $"{patientId}-meta";
    }
}

/// <summary>
/// Patient consent records (HIPAA compliance)
/// RowKey pattern: {patientId}-consent-{consentId}-v{version}
/// </summary>
public class Consent : RowEntity, IRowKeyBuilder
{
    public string ConsentId { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public int Version { get; set; } = 1;
    public string Type { get; set; } = "Required";
    public string Status { get; set; } = "Granted";
    public DateTimeOffset ConsentAt { get; set; } = DateTimeOffset.UtcNow;
    public string? Description { get; set; }

    public string BuildRowKey(RowKeyContext context)
    {
        var patientId = context.GetParentProperty<string>("PatientId");
        return $"{patientId}-consent-{ConsentId}-v{Version}";
    }
}

/// <summary>
/// Linked medical devices (wearables, monitors)
/// RowKey pattern: {patientId}-device-{deviceId}
/// </summary>
public class DeviceLink : RowEntity, IRowKeyBuilder
{
    public string DeviceId { get; set; } = default!;
    public string? Model { get; set; }
    public string? Manufacturer { get; set; }
    public string? MappingStatus { get; set; }

    public string BuildRowKey(RowKeyContext context)
    {
        var patientId = context.GetParentProperty<string>("PatientId");
        return $"{patientId}-device-{DeviceId}";
    }
}
