using PartiTables;

namespace PartiSample.Models;

/// <summary>
/// Healthcare patient entity with related medical records
/// Uses auto-generated RowKeys via RowKeyPattern attribute
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
/// RowKey pattern: {PatientId}-meta
/// </summary>
[RowKeyPattern("{PatientId}-meta")]
public class PatientMeta : RowEntity
{
    public string? Email { get; set; }
    public string? Status { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public DateTime? DateOfBirth { get; set; }
}

/// <summary>
/// Patient consent records (HIPAA compliance)
/// RowKey pattern: {PatientId}-consent-{ConsentId}-v{Version}
/// </summary>
[RowKeyPattern("{PatientId}-consent-{ConsentId}-v{Version}")]
public class Consent : RowEntity
{
    public string ConsentId { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public int Version { get; set; } = 1;
    public string Type { get; set; } = "Required";
    public string Status { get; set; } = "Granted";
    public DateTimeOffset ConsentAt { get; set; } = DateTimeOffset.UtcNow;
    public string? Description { get; set; }
}

/// <summary>
/// Linked medical devices (wearables, monitors)
/// RowKey pattern: {PatientId}-device-{DeviceId}
/// </summary>
[RowKeyPattern("{PatientId}-device-{DeviceId}")]
public class DeviceLink : RowEntity
{
    public string DeviceId { get; set; } = default!;
    public string? Model { get; set; }
    public string? Manufacturer { get; set; }
    public string? MappingStatus { get; set; }
}
