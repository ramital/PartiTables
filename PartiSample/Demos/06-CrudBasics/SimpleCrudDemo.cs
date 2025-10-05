using Microsoft.Extensions.DependencyInjection;
using PartiTables;
using PartiSample.Models;

namespace PartiSample.Demos;

/// <summary>
/// DEMO 6: CRUD & Query Basics
/// 
/// Shows: Complete guide to all basic operations
/// Best for: Learning the fundamentals
/// 
/// Key Concepts:
/// - Create, Read, Update, Delete operations
/// - Query patterns and filters
/// - Aggregations and statistics
/// - Collection management
/// - Best practices
/// </summary>
public static class SimpleCrudDemo
{
    public static async Task RunAsync(IServiceProvider sp)
    {
        Console.WriteLine("=== DEMO 6: CRUD & Query Operations ===");
        Console.WriteLine("Complete guide to all basic operations\n");

        var repo = sp.GetRequiredService<PartitionRepository<TaskProject>>();
        var projectId = "project-alpha";

        // ???????????????????????????????????????????????????????????
        // CREATE: Insert new data
        // ???????????????????????????????????????????????????????????
        Console.WriteLine("?? CREATE Operations\n");

        var project = new TaskProject
        {
            ProjectId = projectId,
            ProjectName = "Alpha Release"
        };

        // Create multiple tasks
        project.Tasks.Add(new ProjectTask
        {
            Title = "Setup development environment",
            Description = "Install tools and configure workspace",
            Status = "Done",
            Priority = "High",
            AssignedTo = "alice@company.com",
            EstimatedHours = 4,
            Tags = new[] { "setup", "devops" },
            CompletedAt = DateTimeOffset.UtcNow.AddDays(-5)
        });

        project.Tasks.Add(new ProjectTask
        {
            Title = "Implement user authentication",
            Description = "Add login and registration features",
            Status = "InProgress",
            Priority = "Critical",
            AssignedTo = "bob@company.com",
            EstimatedHours = 16,
            Tags = new[] { "feature", "security" },
            DueDate = DateTimeOffset.UtcNow.AddDays(3)
        });

        project.Tasks.Add(new ProjectTask
        {
            Title = "Write API documentation",
            Description = "Document all REST endpoints",
            Status = "New",
            Priority = "Medium",
            AssignedTo = "carol@company.com",
            EstimatedHours = 8,
            Tags = new[] { "documentation" },
            DueDate = DateTimeOffset.UtcNow.AddDays(7)
        });

        project.Tasks.Add(new ProjectTask
        {
            Title = "Fix bug in payment processing",
            Description = "Orders failing with error code 500",
            Status = "Blocked",
            Priority = "Critical",
            AssignedTo = "bob@company.com",
            EstimatedHours = 6,
            Tags = new[] { "bug", "urgent" }
        });

        await repo.SaveAsync(project);
        Console.WriteLine($"? Created project: {project.ProjectName}");
        Console.WriteLine($"  Tasks: {project.Tasks.Count}\n");

        foreach (var task in project.Tasks)
        {
            Console.WriteLine($"  • {task.Title}");
            Console.WriteLine($"    {task.Status} | {task.Priority} | {task.AssignedTo}");
        }

        // ???????????????????????????????????????????????????????????
        // READ: Load data
        // ???????????????????????????????????????????????????????????
        Console.WriteLine("\n?? READ Operations\n");

        var loadedProject = await repo.FindAsync(projectId);
        if (loadedProject != null)
        {
            Console.WriteLine($"? Loaded: {loadedProject.ProjectName}");
            Console.WriteLine($"  Tasks: {loadedProject.Tasks.Count}");
            Console.WriteLine($"  Comments: {loadedProject.Comments.Count}");
            Console.WriteLine($"  Attachments: {loadedProject.Attachments.Count}");
        }

        // Read specific collection (efficient!)
        var tasksOnly = await repo.QueryCollectionAsync(projectId, p => p.Tasks);
        Console.WriteLine($"\n? Queried tasks only: {tasksOnly.Count}");
        Console.WriteLine("  (More efficient than loading entire project)");

        // ???????????????????????????????????????????????????????????
        // UPDATE: Modify existing data
        // ???????????????????????????????????????????????????????????
        Console.WriteLine("\n?? UPDATE Operations\n");

        if (loadedProject != null)
        {
            // Update task status
            var authTask = loadedProject.Tasks.First(t => t.Title.Contains("authentication"));
            authTask.Status = "Done";
            authTask.CompletedAt = DateTimeOffset.UtcNow;
            Console.WriteLine($"? Updated: {authTask.Title}");
            Console.WriteLine($"  Status: {authTask.Status}");

            // Add comments
            loadedProject.Comments.Add(new TaskComment
            {
                TaskId = authTask.TaskId,
                Author = "bob@company.com",
                Text = "Authentication feature completed and tested."
            });

            loadedProject.Comments.Add(new TaskComment
            {
                TaskId = authTask.TaskId,
                Author = "alice@company.com",
                Text = "Great work! Please update the docs."
            });

            // Add attachment
            loadedProject.Attachments.Add(new TaskAttachment
            {
                TaskId = authTask.TaskId,
                FileName = "test-results.pdf",
                FileType = "application/pdf",
                FileSizeBytes = 245000,
                StorageUrl = "https://storage.example.com/test-results.pdf",
                UploadedBy = "bob@company.com"
            });

            await repo.SaveAsync(loadedProject);
            Console.WriteLine("? All updates saved\n");
        }

        // ???????????????????????????????????????????????????????????
        // QUERY: Filter and search
        // ???????????????????????????????????????????????????????????
        Console.WriteLine("?? QUERY Operations\n");

        var currentProject = await repo.FindAsync(projectId);
        if (currentProject != null)
        {
            // Query 1: By Status
            Console.WriteLine("Query 1: Tasks by Status");
            var tasksByStatus = currentProject.Tasks.GroupBy(t => t.Status);
            foreach (var group in tasksByStatus)
            {
                Console.WriteLine($"  {group.Key}: {group.Count()}");
            }

            // Query 2: By Priority
            Console.WriteLine("\nQuery 2: High Priority Tasks");
            var highPriorityTasks = currentProject.Tasks.Where(t => 
                t.Priority == "High" || t.Priority == "Critical");
            Console.WriteLine($"  Found: {highPriorityTasks.Count()}");

            // Query 3: By Assignee
            Console.WriteLine("\nQuery 3: Tasks Assigned to Bob");
            var bobsTasks = currentProject.Tasks.Where(t => 
                t.AssignedTo == "bob@company.com");
            Console.WriteLine($"  Found: {bobsTasks.Count()}");

            // Query 4: Overdue
            Console.WriteLine("\nQuery 4: Overdue Tasks");
            var now = DateTimeOffset.UtcNow;
            var overdueTasks = currentProject.Tasks.Where(t => 
                t.DueDate.HasValue && t.DueDate < now && t.Status != "Done");
            Console.WriteLine($"  Found: {overdueTasks.Count()}");

            // Query 5: By Tag
            Console.WriteLine("\nQuery 5: Tasks with 'security' Tag");
            var securityTasks = currentProject.Tasks.Where(t => 
                t.Tags.Contains("security"));
            Console.WriteLine($"  Found: {securityTasks.Count()}");

            // Query 6: Aggregations
            Console.WriteLine("\nQuery 6: Statistics");
            var totalHours = currentProject.Tasks.Sum(t => t.EstimatedHours);
            var completedHours = currentProject.Tasks
                .Where(t => t.Status == "Done")
                .Sum(t => t.EstimatedHours);
            var progress = totalHours > 0 ? (completedHours * 100.0 / totalHours) : 0;
            
            Console.WriteLine($"  Total hours: {totalHours}");
            Console.WriteLine($"  Completed: {completedHours}");
            Console.WriteLine($"  Progress: {progress:F1}%");
        }

        // ╔═════════════════════════════════════════╗
        // DELETE: Remove data
        // ╚═════════════════════════════════════════╝
        Console.WriteLine("\n🗑️ DELETE Operations\n");

        // Delete 1: Remove items from collections
        Console.WriteLine("Delete 1: Remove Specific Items");
        var deleteProject = await repo.FindAsync(projectId);
        if (deleteProject != null)
        {
            // Remove completed tasks
            var completedTasks = deleteProject.Tasks.Where(t => t.Status == "Done").ToList();
            Console.WriteLine($"  Removing {completedTasks.Count} completed tasks");
            
            foreach (var task in completedTasks)
            {
                deleteProject.Tasks.Remove(task);
                Console.WriteLine($"  ✓ Removed: {task.Title}");
            }

            // Remove associated comments and attachments
            var completedTaskIds = completedTasks.Select(t => t.TaskId).ToHashSet();
            var commentsToRemove = deleteProject.Comments
                .Where(c => completedTaskIds.Contains(c.TaskId))
                .ToList();
            var attachmentsToRemove = deleteProject.Attachments
                .Where(a => completedTaskIds.Contains(a.TaskId))
                .ToList();

            foreach (var comment in commentsToRemove)
            {
                deleteProject.Comments.Remove(comment);
            }
            foreach (var attachment in attachmentsToRemove)
            {
                deleteProject.Attachments.Remove(attachment);
            }

            Console.WriteLine($"  ✓ Removed {commentsToRemove.Count} related comments");
            Console.WriteLine($"  ✓ Removed {attachmentsToRemove.Count} related attachments");

            await repo.SaveAsync(deleteProject);
            Console.WriteLine("  💾 Changes saved\n");

            // Show remaining data
            Console.WriteLine($"  Remaining tasks: {deleteProject.Tasks.Count}");
            foreach (var task in deleteProject.Tasks)
            {
                Console.WriteLine($"    • {task.Title} ({task.Status})");
            }
        }

        // Delete 2: Delete entire partition
        Console.WriteLine("\nDelete 2: Delete Entire Project");
        Console.WriteLine("  This will remove all data for the partition:");
        Console.WriteLine("  - All tasks");
        Console.WriteLine("  - All comments");
        Console.WriteLine("  - All attachments");
        Console.WriteLine();

        await repo.DeleteAsync(projectId);
        Console.WriteLine("  ✓ Project deleted successfully\n");

        // Verify deletion
        var verifyProject = await repo.FindAsync(projectId);
        if (verifyProject == null)
        {
            Console.WriteLine("  ✅ Verified: Project no longer exists");
        }

    }
}
