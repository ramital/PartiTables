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
/// RowKey pattern: {projectId}-task-{taskId}
/// </summary>
public class ProjectTask : RowEntity, IRowKeyBuilder
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
    
    public string BuildRowKey(RowKeyContext context)
    {
        var projectId = context.GetParentProperty<string>("ProjectId");
        return $"{projectId}-task-{TaskId}";
    }
}

/// <summary>
/// Task comment entity
/// RowKey pattern: {projectId}-comment-{taskId}-{timestamp}
/// Allows sorting comments by creation time
/// </summary>
public class TaskComment : RowEntity, IRowKeyBuilder
{
    public string TaskId { get; set; } = default!;
    public string CommentId { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string Author { get; set; } = default!;
    public string Text { get; set; } = default!;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? EditedAt { get; set; }
    
    public string BuildRowKey(RowKeyContext context)
    {
        var projectId = context.GetParentProperty<string>("ProjectId");
        var timestamp = CreatedAt.ToUnixTimeSeconds();
        return $"{projectId}-comment-{TaskId}-{timestamp}";
    }
}

/// <summary>
/// Task attachment entity
/// RowKey pattern: {projectId}-attachment-{taskId}-{attachmentId}
/// </summary>
public class TaskAttachment : RowEntity, IRowKeyBuilder
{
    public string TaskId { get; set; } = default!;
    public string AttachmentId { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string FileName { get; set; } = default!;
    public string FileType { get; set; } = default!;
    public long FileSizeBytes { get; set; }
    public string StorageUrl { get; set; } = default!;
    public DateTimeOffset UploadedAt { get; set; } = DateTimeOffset.UtcNow;
    public string UploadedBy { get; set; } = default!;
    
    public string BuildRowKey(RowKeyContext context)
    {
        var projectId = context.GetParentProperty<string>("ProjectId");
        return $"{projectId}-attachment-{TaskId}-{AttachmentId}";
    }
}
