using PartiTables;

namespace PartiSample.Models;

/// <summary>
/// Task management system for CRUD operations demo
/// Demonstrates all basic operations and query patterns
/// </summary>
[TablePartition("Tasks", "{ProjectId}")]
public class TaskProject
{
    public string ProjectId { get; set; } = default!;
    public string ProjectName { get; set; } = default!;
    
    [RowKeyPrefix("")]
    public List<ProjectTask> Tasks { get; set; } = new();
    
    [RowKeyPrefix("")]
    public List<TaskComment> Comments { get; set; } = new();
    
    [RowKeyPrefix("")]
    public List<TaskAttachment> Attachments { get; set; } = new();
}

/// <summary>
/// Project task entity
/// RowKey pattern: {ProjectId}-task-{TaskId}
/// </summary>
[RowKeyPattern("{ProjectId}-task-{TaskId}")]
public class ProjectTask : RowEntity
{
    public string TaskId { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string Status { get; set; } = "New"; // New, InProgress, Done, Blocked
    public string Priority { get; set; } = "Medium"; // Low, Medium, High, Critical
    public string AssignedTo { get; set; } = default!;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? DueDate { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public int EstimatedHours { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Task comment entity
/// RowKey pattern: {ProjectId}-comment-{TaskId}-{CommentId}
/// CommentId includes timestamp prefix for chronological sorting
/// </summary>
[RowKeyPattern("{ProjectId}-comment-{TaskId}-{CommentId}")]
public class TaskComment : RowEntity
{
    public string TaskId { get; set; } = default!;
    
    // CommentId with timestamp prefix for sorting: "20240131-103045-a1b2c3"
    public string CommentId { get; set; } = 
        $"{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid().ToString("N")[..6]}";
    
    public string Author { get; set; } = default!;
    public string Text { get; set; } = default!;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? EditedAt { get; set; }
}

/// <summary>
/// Task attachment entity
/// RowKey pattern: {ProjectId}-attachment-{TaskId}-{AttachmentId}
/// </summary>
[RowKeyPattern("{ProjectId}-attachment-{TaskId}-{AttachmentId}")]
public class TaskAttachment : RowEntity
{
    public string TaskId { get; set; } = default!;
    public string AttachmentId { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string FileName { get; set; } = default!;
    public string FileType { get; set; } = default!;
    public long FileSizeBytes { get; set; }
    public string StorageUrl { get; set; } = default!;
    public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;
    public string UploadedBy { get; set; } = default!;
}
