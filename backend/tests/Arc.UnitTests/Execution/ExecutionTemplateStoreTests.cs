using FluentAssertions;
using Arc.Domain.Models;
using Arc.Infrastructure.Execution;


namespace Arc.UnitTests.Execution;
public sealed class ExecutionTemplateStoreTests
{
    // For SqliteExecutionTemplateStore tests
    private static readonly ExecutionGraph SampleGraph = new ExecutionGraph(new List<TaskNode>
    {
        new TaskNode("task_1", "Process {{input}}"),
        new TaskNode("task_2", "Validate {{input}}")
    });
    private static readonly List<WorkflowTask> SampleTasks = new()
    {
        new WorkflowTask("task_1", "Process {{input}}", "http", new Dictionary<string, string>(), new List<string>()),
        new WorkflowTask("task_2", "Validate {{input}}", "llm", new Dictionary<string, string>(), new List<string>())
    };

    private static readonly List<WorkflowTask> EmptyTasks = new();

    #region InMemoryExecutionTemplateStore Tests

    [Fact]
    public async Task InMemory_CreateAsync_WithValidTemplate_ShouldSucceed()
    {
        // Arrange
        var store = new InMemoryExecutionTemplateStore();

        // Act
        var result = await store.CreateAsync("TestTemplate", "A test template", SampleTasks, "manual");

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("TestTemplate");
        result.Description.Should().Be("A test template");
        result.UseCount.Should().Be(0);
        result.Tasks.Should().HaveCount(2);
    }

    [Fact]
    public async Task InMemory_CreateAsync_WithDuplicateName_ShouldThrow()
    {
        // Arrange
        var store = new InMemoryExecutionTemplateStore();
        await store.CreateAsync("DuplicateTemplate", "First", SampleTasks, "manual");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => store.CreateAsync("DuplicateTemplate", "Second", SampleTasks, "manual")
        );
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task InMemory_CreateAsync_WithInvalidName_ShouldThrow(string invalidName)
    {
        // Arrange
        var store = new InMemoryExecutionTemplateStore();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => store.CreateAsync(invalidName, "Description", SampleTasks, "manual")
        );
    }

    [Fact]
    public async Task InMemory_GetAsync_WithExistingTemplate_ShouldReturn()
    {
        // Arrange
        var store = new InMemoryExecutionTemplateStore();
        var created = await store.CreateAsync("GetTestTemplate", "Description", SampleTasks, "manual");

        // Act
        var retrieved = await store.GetAsync("GetTestTemplate");

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Name.Should().Be(created.Name);
    }

    [Fact]
    public async Task InMemory_GetAsync_WithNonexistentTemplate_ShouldReturnNull()
    {
        // Arrange
        var store = new InMemoryExecutionTemplateStore();

        // Act
        var result = await store.GetAsync("NonexistentTemplate");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task InMemory_GetAsync_WithWhitespaceOnlyName_ShouldReturnNull()
    {
        // Arrange
        var store = new InMemoryExecutionTemplateStore();

        // Act
        var result = await store.GetAsync("   ");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task InMemory_ListAsync_WithMultipleTemplates_ShouldReturnOrderedByName()
    {
        // Arrange
        var store = new InMemoryExecutionTemplateStore();
        await store.CreateAsync("Charlie", "C", SampleTasks, "manual");
        await store.CreateAsync("Alice", "A", SampleTasks, "manual");
        await store.CreateAsync("Bob", "B", SampleTasks, "manual");

        // Act
        var list = await store.ListAsync();

        // Assert
        list.Should().HaveCount(3);
        list.Select(t => t.Name).Should().Equal("Alice", "Bob", "Charlie");
    }

    [Fact]
    public async Task InMemory_ListAsync_WithEmptyStore_ShouldReturnEmpty()
    {
        // Arrange
        var store = new InMemoryExecutionTemplateStore();

        // Act
        var list = await store.ListAsync();

        // Assert
        list.Should().BeEmpty();
    }

    [Fact]
    public async Task InMemory_DeleteAsync_WithExistingTemplate_ShouldSucceed()
    {
        // Arrange
        var store = new InMemoryExecutionTemplateStore();
        await store.CreateAsync("DeleteTestTemplate", "Description", SampleTasks, "manual");

        // Act
        var deleted = await store.DeleteAsync("DeleteTestTemplate");
        var retrieved = await store.GetAsync("DeleteTestTemplate");

        // Assert
        deleted.Should().BeTrue();
        retrieved.Should().BeNull();
    }

    [Fact]
    public async Task InMemory_DeleteAsync_WithNonexistentTemplate_ShouldReturnFalse()
    {
        // Arrange
        var store = new InMemoryExecutionTemplateStore();

        // Act
        var result = await store.DeleteAsync("NonexistentTemplate");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task InMemory_InstantiateAsync_WithExistingTemplate_ShouldSucceed()
    {
        // Arrange
        var store = new InMemoryExecutionTemplateStore();
        await store.CreateAsync("InstantiateTest", "Description", SampleTasks, "manual");

        // Act
        var result = await store.InstantiateAsync("InstantiateTest", new Dictionary<string, string>
        {
            { "input", "TestData" }
        });

        // Assert
        result.Should().NotBeNull();
        result!.TemplateName.Should().Be("InstantiateTest");
            result.InstantiatedTasks.Should().HaveCount(2);
            result.InstantiatedTasks.FirstOrDefault()?.Name.Should().Contain("TestData");
    }

    [Fact]
    public async Task InMemory_InstantiateAsync_WithoutVariables_ShouldReturnUnmodifiedGraph()
    {
        // Arrange
        var store = new InMemoryExecutionTemplateStore();
        await store.CreateAsync("NoVarTest", "Description", SampleTasks, "manual");

        // Act
        var result = await store.InstantiateAsync("NoVarTest");

        // Assert
        result.Should().NotBeNull();
            result!.InstantiatedTasks.FirstOrDefault()?.Name.Should().Be("Process {{input}}");
    }

    [Fact]
    public async Task InMemory_InstantiateAsync_WithMultipleVariables_ShouldSubstituteAll()
    {
        // Arrange
        var tasks = new List<WorkflowTask>
        {
            new WorkflowTask("task_{{id}}", "Process {{env}} in {{location}}", "http", new Dictionary<string, string>(), new List<string>())
        };
        var store = new InMemoryExecutionTemplateStore();
        await store.CreateAsync("MultiVarTest", "Description", tasks, "manual");

        // Act
        var result = await store.InstantiateAsync("MultiVarTest", new Dictionary<string, string>
        {
            { "id", "001" },
            { "env", "Production" },
            { "location", "US-East" }
        });

        // Assert
        result.Should().NotBeNull();
            result!.InstantiatedTasks.FirstOrDefault()?.Id.Should().Be("task_001");
            result.InstantiatedTasks.FirstOrDefault()?.Name.Should().Be("Process Production in US-East");
    }

    [Fact]
    public async Task InMemory_InstantiateAsync_WithNonexistentTemplate_ShouldReturnNull()
    {
        // Arrange
        var store = new InMemoryExecutionTemplateStore();

        // Act
        var result = await store.InstantiateAsync("NonexistentTemplate");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task InMemory_InstantiateAsync_ShouldIncrementUseCount()
    {
        // Arrange
        var store = new InMemoryExecutionTemplateStore();
        await store.CreateAsync("UseCountTest", "Description", SampleTasks, "manual");

        // Act
        await store.InstantiateAsync("UseCountTest");
        await store.InstantiateAsync("UseCountTest");
        var template = await store.GetAsync("UseCountTest");

        // Assert
        template!.UseCount.Should().Be(2);
    }

    [Fact]
    public async Task InMemory_InstantiateAsync_WithCaseInsensitiveVariables_ShouldSubstitute()
    {
        // Arrange
        var tasks = new List<WorkflowTask>
        {
            new WorkflowTask("task_1", "Process {{INPUT}}", "http", new Dictionary<string, string>(), new List<string>())
        };
        var store = new InMemoryExecutionTemplateStore();
        await store.CreateAsync("CaseTest", "Description", tasks, "manual");

        // Act
        var result = await store.InstantiateAsync("CaseTest", new Dictionary<string, string>
        {
            { "input", "Data" }
        });

        // Assert
        result!.InstantiatedTasks.FirstOrDefault()?.Name.Should().Be("Process Data");
    }

    [Fact]
    public async Task InMemory_TemplateNameTrimmed_WhenCreatedAndRetrieved()
    {
        // Arrange
        var store = new InMemoryExecutionTemplateStore();

        // Act
        var created = await store.CreateAsync("  TemplateWithSpaces  ", "Description", SampleTasks, "manual");
        var retrieved = await store.GetAsync("  TemplateWithSpaces  ");

        // Assert
        created.Name.Should().Be("TemplateWithSpaces");
        retrieved.Should().NotBeNull();
    }

    #endregion

    #region SqliteExecutionTemplateStore Tests

    [Fact]
    public async Task Sqlite_CreateAsync_WithValidTemplate_ShouldSucceed()
    {
        // Arrange
        var connectionString = $"Data Source=:memory:";
        var store = new SqliteExecutionTemplateStore(connectionString);

        // Act
        var result = await store.CreateAsync("SqliteTestTemplate", "A sqlite test template", SampleTasks, "manual");

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("SqliteTestTemplate");
        result.Description.Should().Be("A sqlite test template");
        result.UseCount.Should().Be(0);
        result.Tasks.Should().HaveCount(2);
    }

    [Fact]
    public async Task Sqlite_CreateAsync_WithDuplicateName_ShouldThrow()
    {
        // Arrange
        var connectionString = $"Data Source=:memory:";
        var store = new SqliteExecutionTemplateStore(connectionString);
        await store.CreateAsync("SqliteDuplicate", "First", SampleTasks, "manual");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => store.CreateAsync("SqliteDuplicate", "Second", SampleTasks, "manual")
        );
    }

    [Fact]
    public async Task Sqlite_GetAsync_WithExistingTemplate_ShouldReturn()
    {
        // Arrange
        var connectionString = $"Data Source=:memory:";
        var store = new SqliteExecutionTemplateStore(connectionString);
        var created = await store.CreateAsync("SqliteGetTest", "Description", SampleTasks, "manual");

        // Act
        var retrieved = await store.GetAsync("SqliteGetTest");

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Name.Should().Be(created.Name);
        retrieved.Tasks.Should().HaveCount(2);
    }

    [Fact]
    public async Task Sqlite_ListAsync_WithMultipleTemplates_ShouldReturnOrderedByName()
    {
        // Arrange
        var connectionString = $"Data Source=:memory:";
        var store = new SqliteExecutionTemplateStore(connectionString);
        await store.CreateAsync("Charlie", "C", SampleTasks, "manual");
        await store.CreateAsync("Alice", "A", SampleTasks, "manual");
        await store.CreateAsync("Bob", "B", SampleTasks, "manual");

        // Act
        var list = await store.ListAsync();

        // Assert
        list.Should().HaveCount(3);
        list.Select(t => t.Name).Should().Equal("Alice", "Bob", "Charlie");
    }

    [Fact]
    public async Task Sqlite_DeleteAsync_WithExistingTemplate_ShouldSucceed()
    {
        // Arrange
        var connectionString = $"Data Source=:memory:";
        var store = new SqliteExecutionTemplateStore(connectionString);
        await store.CreateAsync("SqliteDeleteTest", "Description", SampleTasks, "manual");

        // Act
        var deleted = await store.DeleteAsync("SqliteDeleteTest");
        var retrieved = await store.GetAsync("SqliteDeleteTest");

        // Assert
        deleted.Should().BeTrue();
        retrieved.Should().BeNull();
    }

    [Fact]
    public async Task Sqlite_InstantiateAsync_WithExistingTemplate_ShouldSucceed()
    {
        // Arrange
        var connectionString = $"Data Source=:memory:";
        var store = new SqliteExecutionTemplateStore(connectionString);
        await store.CreateAsync("SqliteInstantiateTest", "Description", SampleTasks, "manual");

        // Act
        var result = await store.InstantiateAsync("SqliteInstantiateTest", new Dictionary<string, string>
        {
            { "input", "SqliteData" }
        });

        // Assert
        result.Should().NotBeNull();
        result!.TemplateName.Should().Be("SqliteInstantiateTest");
        result.InstantiatedTasks.FirstOrDefault()?.Name.Should().Contain("SqliteData");
    }

    [Fact]
    public async Task Sqlite_InstantiateAsync_ShouldIncrementUseCount()
    {
        // Arrange
        var connectionString = $"Data Source=:memory:";
        var store = new SqliteExecutionTemplateStore(connectionString);
        await store.CreateAsync("SqliteUseCountTest", "Description", SampleTasks, "manual");

        // Act
        await store.InstantiateAsync("SqliteUseCountTest");
        await store.InstantiateAsync("SqliteUseCountTest");
        var template = await store.GetAsync("SqliteUseCountTest");

        // Assert
        template!.UseCount.Should().Be(2);
    }

    [Fact]
    public async Task Sqlite_Persistence_WithMultipleConnections()
    {
        // Arrange
        var dbFile = $"test_templates_{Guid.NewGuid():N}.db";
        var connectionString = $"Data Source={dbFile}";

        try
        {
            var store1 = new SqliteExecutionTemplateStore(connectionString);
            await store1.CreateAsync("PersistenceTest", "For persistence verification", SampleTasks, "manual");

            // Act
            var store2 = new SqliteExecutionTemplateStore(connectionString);
            var retrieved = await store2.GetAsync("PersistenceTest");

            // Assert
            retrieved.Should().NotBeNull();
            retrieved!.Name.Should().Be("PersistenceTest");
        }
        finally
        {
            if (File.Exists(dbFile))
                File.Delete(dbFile);
        }
    }

    #endregion

    #region Variable Substitution Edge Cases

    [Fact]
    public async Task VariableSubstitution_WithWhitespaceInPlaceholders_ShouldWork()
    {
        // Arrange
        var tasks = new List<WorkflowTask>
        {
            new WorkflowTask("task_1", "Process {{ input }}", "http", new Dictionary<string, string>(), new List<string>())
        };
        var store = new InMemoryExecutionTemplateStore();
        await store.CreateAsync("WhitespaceTest", "Description", tasks, "manual");

        // Act
        var result = await store.InstantiateAsync("WhitespaceTest", new Dictionary<string, string>
        {
            { "input", "TestValue" }
        });

        // Assert
        result!.InstantiatedTasks.FirstOrDefault()?.Name.Should().Be("Process TestValue");
    }

    [Fact]
    public async Task VariableSubstitution_WithSpecialCharactersInValue_ShouldWork()
    {
        // Arrange
        var tasks = new List<WorkflowTask>
        {
            new WorkflowTask("task_1", "Execute {{command}}", "http", new Dictionary<string, string>(), new List<string>())
        };
        var store = new InMemoryExecutionTemplateStore();
        await store.CreateAsync("SpecialCharTest", "Description", tasks, "manual");

        // Act
        var result = await store.InstantiateAsync("SpecialCharTest", new Dictionary<string, string>
        {
            { "command", "SELECT * FROM users WHERE id > 5" }
        });

        // Assert
        result!.InstantiatedTasks.FirstOrDefault()?.Name.Should().Be("Execute SELECT * FROM users WHERE id > 5");
    }

    [Fact]
    public async Task VariableSubstitution_WithUnusedVariables_ShouldIgnore()
    {
        // Arrange
        var store = new InMemoryExecutionTemplateStore();
        await store.CreateAsync("UnusedVarTest", "Description", SampleTasks, "manual");

        // Act
        var result = await store.InstantiateAsync("UnusedVarTest", new Dictionary<string, string>
        {
            { "input", "Used" },
            { "unused", "IgnoredValue" }
        });

        // Assert
        result.Should().NotBeNull();
        result!.InstantiatedTasks.FirstOrDefault()?.Name.Should().Contain("Used");
    }

    #endregion
}