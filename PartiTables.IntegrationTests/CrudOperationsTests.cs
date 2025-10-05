using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.DependencyInjection;
using PartiTables;
using PartiTables.IntegrationTests.PartiTables;

namespace PartiTables.IntegrationTests;

/// <summary>
/// Comprehensive CRUD and Query integration tests
/// Validates data integrity across all operations
/// </summary>
public class CrudOperationsTests
{
    private const string ConnectionString = "UseDevelopmentStorage=true";

    [Fact]
    public async Task Create_SavesEntity_AndReturnsCorrectCount()
    {
        // ARRANGE
        var services = new ServiceCollection();
        services.AddPartiTables(opts =>
        {
            opts.ConnectionString = ConnectionString;
            opts.TableName = "CrudCreateTest";
        });
        services.AddPartitionRepository<ProjectData>();
        var provider = services.BuildServiceProvider();
        var repo = provider.GetRequiredService<PartitionRepository<ProjectData>>();

        await repo.DeleteAsync("project-create-001");

        var project = new ProjectData
        {
            ProjectId = "project-create-001",
            ProjectName = "Create Test Project"
        };

        // Add 3 tasks
        for (int i = 1; i <= 3; i++)
        {
            project.Tasks.Add(new TaskItem
            {
                TaskId = $"task-{i:D3}",
                Title = $"Task {i}",
                Status = "New"
            });
        }

        // Add 2 comments
        for (int i = 1; i <= 2; i++)
        {
            project.Comments.Add(new CommentItem
            {
                CommentId = $"comment-{i:D3}",
                Text = $"Comment {i}",
                Author = "TestUser"
            });
        }

        // ACT
        await repo.SaveAsync(project);
        var loaded = await repo.FindAsync("project-create-001");

        // ASSERT
        using (new AssertionScope())
        {
            loaded.Should().NotBeNull("project should be created successfully");
            loaded!.ProjectId.Should().Be("project-create-001", "project ID should match");
            loaded.Tasks.Should().HaveCount(3, "should have exactly 3 tasks");
            loaded.Comments.Should().HaveCount(2, "should have exactly 2 comments");

            // Verify each task was saved correctly
            loaded.Tasks.Select(t => t.TaskId).Should().BeEquivalentTo(
                new[] { "task-001", "task-002", "task-003" },
                "all task IDs should be preserved");

            // Verify each comment was saved correctly
            loaded.Comments.Select(c => c.CommentId).Should().BeEquivalentTo(
                new[] { "comment-001", "comment-002" },
                "all comment IDs should be preserved");
        }

        // CLEANUP
        await repo.DeleteAsync("project-create-001");
    }

    [Fact]
    public async Task Read_LoadsExactEntityCount_NoExtraData()
    {
        // ARRANGE
        var services = new ServiceCollection();
        services.AddPartiTables(opts =>
        {
            opts.ConnectionString = ConnectionString;
            opts.TableName = "CrudReadTest";
        });
        services.AddPartitionRepository<ProjectData>();
        var provider = services.BuildServiceProvider();
        var repo = provider.GetRequiredService<PartitionRepository<ProjectData>>();

        await repo.DeleteAsync("project-read-001");

        var project = new ProjectData
        {
            ProjectId = "project-read-001",
            ProjectName = "Read Test Project"
        };

        // Add known number of items
        project.Tasks.Add(new TaskItem { TaskId = "task-001", Title = "Task 1" });
        project.Tasks.Add(new TaskItem { TaskId = "task-002", Title = "Task 2" });
        project.Tasks.Add(new TaskItem { TaskId = "task-003", Title = "Task 3" });
        project.Tasks.Add(new TaskItem { TaskId = "task-004", Title = "Task 4" });
        project.Tasks.Add(new TaskItem { TaskId = "task-005", Title = "Task 5" });

        project.Comments.Add(new CommentItem { CommentId = "comment-001", Text = "C1" });
        project.Comments.Add(new CommentItem { CommentId = "comment-002", Text = "C2" });

        await repo.SaveAsync(project);

        // ACT - Read entire entity
        var loaded = await repo.FindAsync("project-read-001");

        // ACT - Read specific collections
        var tasksOnly = await repo.QueryCollectionAsync("project-read-001", p => p.Tasks);
        var commentsOnly = await repo.QueryCollectionAsync("project-read-001", p => p.Comments);

        // ASSERT
        using (new AssertionScope())
        {
            // Verify full entity load
            loaded.Should().NotBeNull();
            loaded!.Tasks.Should().HaveCount(5, "should load exactly 5 tasks");
            loaded.Comments.Should().HaveCount(2, "should load exactly 2 comments");

            // Verify collection-specific queries
            tasksOnly.Should().HaveCount(5, "should query exactly 5 tasks");
            commentsOnly.Should().HaveCount(2, "should query exactly 2 comments");

            // Verify no cross-contamination
            tasksOnly.Select(t => t.TaskId).Should().OnlyHaveUniqueItems("task IDs should be unique");
            commentsOnly.Select(c => c.CommentId).Should().OnlyHaveUniqueItems("comment IDs should be unique");

            // Verify data integrity
            tasksOnly.Should().AllSatisfy(t => 
                t.TaskId.Should().StartWith("task-", "all tasks should have correct prefix"));
            commentsOnly.Should().AllSatisfy(c => 
                c.CommentId.Should().StartWith("comment-", "all comments should have correct prefix"));
        }

        // CLEANUP
        await repo.DeleteAsync("project-read-001");
    }

    [Fact]
    public async Task Update_ModifiesExistingItems_AndAddsNew_CorrectCount()
    {
        // ARRANGE
        var services = new ServiceCollection();
        services.AddPartiTables(opts =>
        {
            opts.ConnectionString = ConnectionString;
            opts.TableName = "CrudUpdateTest";
        });
        services.AddPartitionRepository<ProjectData>();
        var provider = services.BuildServiceProvider();
        var repo = provider.GetRequiredService<PartitionRepository<ProjectData>>();

        await repo.DeleteAsync("project-update-001");

        var project = new ProjectData
        {
            ProjectId = "project-update-001",
            ProjectName = "Update Test"
        };

        project.Tasks.Add(new TaskItem 
        { 
            TaskId = "task-001", 
            Title = "Original Task 1", 
            Status = "New" 
        });
        project.Tasks.Add(new TaskItem 
        { 
            TaskId = "task-002", 
            Title = "Original Task 2", 
            Status = "New" 
        });

        await repo.SaveAsync(project);

        // ACT - Load, modify, and add new items
        var loaded = await repo.FindAsync("project-update-001");
        
        // Modify existing
        loaded!.Tasks[0].Status = "InProgress";
        loaded.Tasks[0].Title = "Updated Task 1";
        loaded.Tasks[1].Status = "Done";
        
        // Add new items
        loaded.Tasks.Add(new TaskItem 
        { 
            TaskId = "task-003", 
            Title = "New Task 3", 
            Status = "New" 
        });
        loaded.Comments.Add(new CommentItem 
        { 
            CommentId = "comment-001", 
            Text = "New comment", 
            Author = "Admin" 
        });

        await repo.SaveAsync(loaded);

        // Reload to verify
        var reloaded = await repo.FindAsync("project-update-001");

        // ASSERT
        using (new AssertionScope())
        {
            reloaded.Should().NotBeNull("updated project should exist");
            reloaded!.Tasks.Should().HaveCount(3, "should have 3 tasks after update");
            reloaded.Comments.Should().HaveCount(1, "should have 1 comment after update");

            // Verify modifications were persisted
            var task1 = reloaded.Tasks.First(t => t.TaskId == "task-001");
            task1.Status.Should().Be("InProgress", "task 1 status should be updated");
            task1.Title.Should().Be("Updated Task 1", "task 1 title should be updated");

            var task2 = reloaded.Tasks.First(t => t.TaskId == "task-002");
            task2.Status.Should().Be("Done", "task 2 status should be updated");

            // Verify new items were added
            reloaded.Tasks.Should().Contain(t => t.TaskId == "task-003", "new task should be added");
            reloaded.Comments.Should().Contain(c => c.CommentId == "comment-001", "new comment should be added");
        }

        // CLEANUP
        await repo.DeleteAsync("project-update-001");
    }

    [Fact]
    public async Task Delete_RemovesAllData_NoOrphans()
    {
        // ARRANGE
        var services = new ServiceCollection();
        services.AddPartiTables(opts =>
        {
            opts.ConnectionString = ConnectionString;
            opts.TableName = "CrudDeleteTest";
        });
        services.AddPartitionRepository<ProjectData>();
        var provider = services.BuildServiceProvider();
        var repo = provider.GetRequiredService<PartitionRepository<ProjectData>>();

        await repo.DeleteAsync("project-delete-001");

        var project = new ProjectData
        {
            ProjectId = "project-delete-001",
            ProjectName = "Delete Test"
        };

        // Add multiple items to ensure all are deleted
        for (int i = 1; i <= 10; i++)
        {
            project.Tasks.Add(new TaskItem 
            { 
                TaskId = $"task-{i:D3}", 
                Title = $"Task {i}" 
            });
        }

        for (int i = 1; i <= 5; i++)
        {
            project.Comments.Add(new CommentItem 
            { 
                CommentId = $"comment-{i:D3}", 
                Text = $"Comment {i}" 
            });
        }

        await repo.SaveAsync(project);

        // Verify data exists before delete
        var beforeDelete = await repo.FindAsync("project-delete-001");
        beforeDelete.Should().NotBeNull("data should exist before delete");
        beforeDelete!.Tasks.Should().HaveCount(10, "should have 10 tasks before delete");
        beforeDelete.Comments.Should().HaveCount(5, "should have 5 comments before delete");

        // ACT - Delete
        await repo.DeleteAsync("project-delete-001");

        // ASSERT
        var afterDelete = await repo.FindAsync("project-delete-001");
        using (new AssertionScope())
        {
            afterDelete.Should().BeNull("project should not exist after delete");
        }

        // Verify no orphaned data - try querying collections
        var orphanedTasks = await repo.QueryCollectionAsync("project-delete-001", p => p.Tasks);
        var orphanedComments = await repo.QueryCollectionAsync("project-delete-001", p => p.Comments);

        using (new AssertionScope())
        {
            orphanedTasks.Should().BeEmpty("no orphaned tasks should remain");
            orphanedComments.Should().BeEmpty("no orphaned comments should remain");
        }
    }

    [Fact]
    public async Task Query_ReturnsExactMatchingItems_NoFalsePositives()
    {
        // ARRANGE
        var services = new ServiceCollection();
        services.AddPartiTables(opts =>
        {
            opts.ConnectionString = ConnectionString;
            opts.TableName = "CrudQueryTest";
        });
        services.AddPartitionRepository<ProjectData>();
        var provider = services.BuildServiceProvider();
        var repo = provider.GetRequiredService<PartitionRepository<ProjectData>>();

        await repo.DeleteAsync("project-query-001");

        var project = new ProjectData
        {
            ProjectId = "project-query-001",
            ProjectName = "Query Test"
        };

        // Add items with different statuses
        project.Tasks.Add(new TaskItem { TaskId = "task-001", Title = "T1", Status = "New" });
        project.Tasks.Add(new TaskItem { TaskId = "task-002", Title = "T2", Status = "New" });
        project.Tasks.Add(new TaskItem { TaskId = "task-003", Title = "T3", Status = "InProgress" });
        project.Tasks.Add(new TaskItem { TaskId = "task-004", Title = "T4", Status = "InProgress" });
        project.Tasks.Add(new TaskItem { TaskId = "task-005", Title = "T5", Status = "Done" });
        project.Tasks.Add(new TaskItem { TaskId = "task-006", Title = "T6", Status = "Done" });
        project.Tasks.Add(new TaskItem { TaskId = "task-007", Title = "T7", Status = "Done" });

        await repo.SaveAsync(project);

        // ACT
        var loaded = await repo.FindAsync("project-query-001");

        var newTasks = loaded!.Tasks.Where(t => t.Status == "New").ToList();
        var inProgressTasks = loaded.Tasks.Where(t => t.Status == "InProgress").ToList();
        var doneTasks = loaded.Tasks.Where(t => t.Status == "Done").ToList();
        var allTasks = loaded.Tasks.ToList();

        // ASSERT
        using (new AssertionScope())
        {
            allTasks.Should().HaveCount(7, "should load all 7 tasks");
            newTasks.Should().HaveCount(2, "should have exactly 2 New tasks");
            inProgressTasks.Should().HaveCount(2, "should have exactly 2 InProgress tasks");
            doneTasks.Should().HaveCount(3, "should have exactly 3 Done tasks");

            // Verify no false positives
            newTasks.Should().AllSatisfy(t => t.Status.Should().Be("New"));
            inProgressTasks.Should().AllSatisfy(t => t.Status.Should().Be("InProgress"));
            doneTasks.Should().AllSatisfy(t => t.Status.Should().Be("Done"));

            // Verify sum equals total
            (newTasks.Count + inProgressTasks.Count + doneTasks.Count).Should().Be(7,
                "sum of filtered tasks should equal total");
        }

        // CLEANUP
        await repo.DeleteAsync("project-query-001");
    }

    [Fact]
    public async Task CompleteLifecycle_CreateReadUpdateDelete_AllCorrect()
    {
        // ARRANGE
        var services = new ServiceCollection();
        services.AddPartiTables(opts =>
        {
            opts.ConnectionString = ConnectionString;
            opts.TableName = "CrudLifecycleTest";
        });
        services.AddPartitionRepository<ProjectData>();
        var provider = services.BuildServiceProvider();
        var repo = provider.GetRequiredService<PartitionRepository<ProjectData>>();

        await repo.DeleteAsync("project-lifecycle-001");

        // ACT & ASSERT - Step 1: CREATE
        var project = new ProjectData
        {
            ProjectId = "project-lifecycle-001",
            ProjectName = "Lifecycle Test"
        };

        project.Tasks.Add(new TaskItem { TaskId = "task-001", Title = "Initial Task", Status = "New" });
        project.Comments.Add(new CommentItem { CommentId = "comment-001", Text = "Initial Comment" });

        await repo.SaveAsync(project);

        var afterCreate = await repo.FindAsync("project-lifecycle-001");
        using (new AssertionScope())
        {
            afterCreate.Should().NotBeNull("should exist after create");
            afterCreate!.Tasks.Should().HaveCount(1, "should have 1 task after create");
            afterCreate.Comments.Should().HaveCount(1, "should have 1 comment after create");
        }

        // ACT & ASSERT - Step 2: READ
        var read = await repo.FindAsync("project-lifecycle-001");
        using (new AssertionScope())
        {
            read.Should().NotBeNull("should exist on read");
            read!.Tasks[0].Title.Should().Be("Initial Task", "title should match");
            read.Comments[0].Text.Should().Be("Initial Comment", "comment should match");
        }

        // ACT & ASSERT - Step 3: UPDATE
        read!.Tasks[0].Status = "Done";
        read.Tasks[0].Title = "Updated Task";
        read.Tasks.Add(new TaskItem { TaskId = "task-002", Title = "Second Task", Status = "New" });
        read.Comments.Add(new CommentItem { CommentId = "comment-002", Text = "Second Comment" });

        await repo.SaveAsync(read);

        var afterUpdate = await repo.FindAsync("project-lifecycle-001");
        using (new AssertionScope())
        {
            afterUpdate.Should().NotBeNull("should exist after update");
            afterUpdate!.Tasks.Should().HaveCount(2, "should have 2 tasks after update");
            afterUpdate.Comments.Should().HaveCount(2, "should have 2 comments after update");
            afterUpdate.Tasks[0].Status.Should().Be("Done", "status should be updated");
            afterUpdate.Tasks[0].Title.Should().Be("Updated Task", "title should be updated");
        }

        // ACT & ASSERT - Step 4: DELETE
        await repo.DeleteAsync("project-lifecycle-001");

        var afterDelete = await repo.FindAsync("project-lifecycle-001");
        afterDelete.Should().BeNull("should not exist after delete");

        // Verify complete cleanup
        var tasksAfterDelete = await repo.QueryCollectionAsync("project-lifecycle-001", p => p.Tasks);
        var commentsAfterDelete = await repo.QueryCollectionAsync("project-lifecycle-001", p => p.Comments);

        using (new AssertionScope())
        {
            tasksAfterDelete.Should().BeEmpty("no tasks should remain after delete");
            commentsAfterDelete.Should().BeEmpty("no comments should remain after delete");
        }
    }

    [Fact]
    public async Task MultipleEntities_IndependentPartitions_NoInterference()
    {
        // ARRANGE
        var services = new ServiceCollection();
        services.AddPartiTables(opts =>
        {
            opts.ConnectionString = ConnectionString;
            opts.TableName = "CrudMultiEntityTest";
        });
        services.AddPartitionRepository<ProjectData>();
        var provider = services.BuildServiceProvider();
        var repo = provider.GetRequiredService<PartitionRepository<ProjectData>>();

        await repo.DeleteAsync("project-multi-001");
        await repo.DeleteAsync("project-multi-002");
        await repo.DeleteAsync("project-multi-003");

        // ACT - Create 3 separate projects
        for (int i = 1; i <= 3; i++)
        {
            var project = new ProjectData
            {
                ProjectId = $"project-multi-{i:D3}",
                ProjectName = $"Project {i}"
            };

            for (int j = 1; j <= i * 2; j++)
            {
                project.Tasks.Add(new TaskItem 
                { 
                    TaskId = $"task-{j:D3}", 
                    Title = $"Task {j}" 
                });
            }

            await repo.SaveAsync(project);
        }

        // ASSERT - Verify each project has correct data
        var project1 = await repo.FindAsync("project-multi-001");
        var project2 = await repo.FindAsync("project-multi-002");
        var project3 = await repo.FindAsync("project-multi-003");

        using (new AssertionScope())
        {
            project1.Should().NotBeNull("project 1 should exist");
            project2.Should().NotBeNull("project 2 should exist");
            project3.Should().NotBeNull("project 3 should exist");

            project1!.Tasks.Should().HaveCount(2, "project 1 should have 2 tasks");
            project2!.Tasks.Should().HaveCount(4, "project 2 should have 4 tasks");
            project3!.Tasks.Should().HaveCount(6, "project 3 should have 6 tasks");

            // Verify no cross-partition contamination
            project1.ProjectId.Should().Be("project-multi-001");
            project2.ProjectId.Should().Be("project-multi-002");
            project3.ProjectId.Should().Be("project-multi-003");
        }

        // CLEANUP
        await repo.DeleteAsync("project-multi-001");
        await repo.DeleteAsync("project-multi-002");
        await repo.DeleteAsync("project-multi-003");
    }
}
