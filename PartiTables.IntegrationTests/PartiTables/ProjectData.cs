namespace PartiTables.IntegrationTests.PartiTables;

/// <summary>
/// Test models for CRUD operations validation
/// </summary>
[TablePartition("CrudTestTable", "{ProjectId}")]
public class ProjectData
{
    public string ProjectId { get; set; } = default!;
    public string ProjectName { get; set; } = default!;

    [RowKeyPrefix("")]
    public List<TaskItem> Tasks { get; set; } = new();

    [RowKeyPrefix("")]
    public List<CommentItem> Comments { get; set; } = new();
}

[RowKeyPattern("{ProjectId}-task-{TaskId}")]
public class TaskItem : RowEntity
{
    public string TaskId { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Status { get; set; } = "New";
}

[RowKeyPattern("{ProjectId}-comment-{CommentId}")]
public class CommentItem : RowEntity
{
    public string CommentId { get; set; } = default!;
    public string Text { get; set; } = default!;
    public string? Author { get; set; }
}

