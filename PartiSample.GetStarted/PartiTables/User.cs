using PartiTables;

namespace PartiSample.GetStarted.PartiTables;

[TablePartition("Users", "{UserId}")]
public class User
{
    public string UserId { get; set; } = default!;
    
    [RowKeyPrefix("")]
    public List<Task> Tasks { get; set; } = new();
}

[RowKeyPattern("{UserId}-task-{TaskId}")]
public class Task : RowEntity
{
    public string TaskId { get; set; } = default!;
    public decimal load { get; set; }
}
