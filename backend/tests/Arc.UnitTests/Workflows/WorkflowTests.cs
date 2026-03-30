using FluentAssertions;
using Arc.Domain.Models;
using Arc.Domain.Exceptions;
namespace Arc.UnitTests.Workflows;


public sealed class WorkflowTests
{
    private readonly UserId _testUserId = new(Guid.NewGuid());

    [Fact]
    public void Constructor_WithValidInputs_CreatesWorkflow()
    {
        var tasks = new List<WorkflowTask>
        {
            new("task1", "Task 1", "http", new Dictionary<string, string>(), new List<string>())
        };

        var workflow = new Workflow(
            "wf-123",
            "Test Workflow",
            "Test Description",
            tasks,
            "manual",
            _testUserId,
            DateTime.UtcNow
        );

        workflow.Id.Should().Be("wf-123");
        workflow.Name.Should().Be("Test Workflow");
        workflow.Description.Should().Be("Test Description");
        workflow.Tasks.Should().HaveCount(1);
        workflow.TriggerType.Should().Be("manual");
        workflow.CreatedBy.Should().Be(_testUserId);
    }

    [Fact]
    public void Constructor_WithEmptyId_ThrowsDomainException()
    {
        var tasks = new List<WorkflowTask>
        {
            new("task1", "Task 1", "http", new Dictionary<string, string>(), new List<string>())
        };

        var act = () => new Workflow(
            "",
            "Test Workflow",
            "Test Description",
            tasks,
            "manual",
            _testUserId,
            DateTime.UtcNow
        );

        act.Should().Throw<DomainException>().WithMessage("Workflow ID cannot be empty");
    }

    [Fact]
    public void Constructor_WithEmptyName_ThrowsDomainException()
    {
        var tasks = new List<WorkflowTask>
        {
            new("task1", "Task 1", "http", new Dictionary<string, string>(), new List<string>())
        };

        var act = () => new Workflow(
            "wf-123",
            "",
            "Test Description",
            tasks,
            "manual",
            _testUserId,
            DateTime.UtcNow
        );

        act.Should().Throw<DomainException>().WithMessage("Workflow name cannot be empty");
    }

    [Fact]
    public void Constructor_WithNameTooLong_ThrowsDomainException()
    {
        var tasks = new List<WorkflowTask>
        {
            new("task1", "Task 1", "http", new Dictionary<string, string>(), new List<string>())
        };

        var act = () => new Workflow(
            "wf-123",
            new string('a', 201),
            "Test Description",
            tasks,
            "manual",
            _testUserId,
            DateTime.UtcNow
        );

        act.Should().Throw<DomainException>().WithMessage("Workflow name cannot exceed 200 characters");
    }

    [Fact]
    public void Constructor_WithDescriptionTooLong_ThrowsDomainException()
    {
        var tasks = new List<WorkflowTask>
        {
            new("task1", "Task 1", "http", new Dictionary<string, string>(), new List<string>())
        };

        var act = () => new Workflow(
            "wf-123",
            "Test Workflow",
            new string('a', 1001),
            tasks,
            "manual",
            _testUserId,
            DateTime.UtcNow
        );

        act.Should().Throw<DomainException>().WithMessage("Workflow description cannot exceed 1000 characters");
    }

    [Fact]
    public void Constructor_WithNoTasks_ThrowsDomainException()
    {
        var act = () => new Workflow(
            "wf-123",
            "Test Workflow",
            "Test Description",
            new List<WorkflowTask>(),
            "manual",
            _testUserId,
            DateTime.UtcNow
        );

        act.Should().Throw<DomainException>().WithMessage("Workflow must have at least one task");
    }

    [Fact]
    public void Constructor_WithInvalidTriggerType_ThrowsDomainException()
    {
        var tasks = new List<WorkflowTask>
        {
            new("task1", "Task 1", "http", new Dictionary<string, string>(), new List<string>())
        };

        var act = () => new Workflow(
            "wf-123",
            "Test Workflow",
            "Test Description",
            tasks,
            "invalid",
            _testUserId,
            DateTime.UtcNow
        );

        act.Should().Throw<DomainException>().WithMessage("Invalid trigger type: invalid");
    }

    [Fact]
    public void Constructor_WithDuplicateTaskIds_ThrowsDomainException()
    {
        var tasks = new List<WorkflowTask>
        {
            new("task1", "Task 1", "http", new Dictionary<string, string>(), new List<string>()),
            new("task1", "Task 2", "http", new Dictionary<string, string>(), new List<string>())
        };

        var act = () => new Workflow(
            "wf-123",
            "Test Workflow",
            "Test Description",
            tasks,
            "manual",
            _testUserId,
            DateTime.UtcNow
        );

        act.Should().Throw<DomainException>().WithMessage("Duplicate task IDs found: task1");
    }

    [Fact]
    public void Constructor_WithInvalidDependency_ThrowsDomainException()
    {
        var tasks = new List<WorkflowTask>
        {
            new("task1", "Task 1", "http", new Dictionary<string, string>(), new List<string> { "task2" })
        };

        var act = () => new Workflow(
            "wf-123",
            "Test Workflow",
            "Test Description",
            tasks,
            "manual",
            _testUserId,
            DateTime.UtcNow
        );

        act.Should().Throw<DomainException>().WithMessage("Task 'task1' references non-existent dependency 'task2'");
    }

    [Fact]
    public void Constructor_WithCircularDependency_ThrowsDomainException()
    {
        var tasks = new List<WorkflowTask>
        {
            new("task1", "Task 1", "http", new Dictionary<string, string>(), new List<string> { "task2" }),
            new("task2", "Task 2", "http", new Dictionary<string, string>(), new List<string> { "task1" })
        };

        var act = () => new Workflow(
            "wf-123",
            "Test Workflow",
            "Test Description",
            tasks,
            "manual",
            _testUserId,
            DateTime.UtcNow
        );

        act.Should().Throw<DomainException>().WithMessage("Workflow contains circular dependencies");
    }

    [Fact]
    public void Constructor_WithValidDependencies_CreatesWorkflow()
    {
        var tasks = new List<WorkflowTask>
        {
            new("task1", "Task 1", "http", new Dictionary<string, string>(), new List<string>()),
            new("task2", "Task 2", "http", new Dictionary<string, string>(), new List<string> { "task1" }),
            new("task3", "Task 3", "http", new Dictionary<string, string>(), new List<string> { "task1", "task2" })
        };

        var workflow = new Workflow(
            "wf-123",
            "Test Workflow",
            "Test Description",
            tasks,
            "manual",
            _testUserId,
            DateTime.UtcNow
        );

        workflow.Tasks.Should().HaveCount(3);
    }
}
