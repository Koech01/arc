using FluentAssertions;
using Arc.Api.DTOs.Workflows;
using Arc.Api.Validators.Workflows;
namespace Arc.UnitTests.Validators;


public sealed class CreateWorkflowRequestDtoValidatorTests
{
    private readonly CreateWorkflowRequestDtoValidator _validator = new();

    [Fact]
    public void Validate_WithValidData_ShouldPass()
    {
        var request = new CreateWorkflowRequestDto
        {
            Name = "Test Workflow",
            Description = "Test description",
            TriggerType = "manual",
            Tasks = new List<WorkflowTaskDto>
            {
                new()
                {
                    Id = "task-1",
                    Name = "Task 1",
                    AgentType = "llm",
                    Prompt = "Test prompt",
                    Config = new Dictionary<string, string>(),
                    Dependencies = new List<string>()
                }
            }
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_WithEmptyName_ShouldFail(string name)
    {
        var request = new CreateWorkflowRequestDto
        {
            Name = name,
            TriggerType = "manual",
            Tasks = new List<WorkflowTaskDto>
            {
                new()
                {
                    Id = "task-1",
                    Name = "Task 1",
                    AgentType = "llm",
                    Prompt = "Test prompt",
                    Config = new Dictionary<string, string>(),
                    Dependencies = new List<string>()
                }
            }
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name" && 
                                            e.ErrorMessage == "Workflow name is required");
    }

    [Fact]
    public void Validate_WithNameTooLong_ShouldFail()
    {
        var request = new CreateWorkflowRequestDto
        {
            Name = new string('a', 201),
            TriggerType = "manual",
            Tasks = new List<WorkflowTaskDto>
            {
                new()
                {
                    Id = "task-1",
                    Name = "Task 1",
                    AgentType = "llm",
                    Prompt = "Test prompt",
                    Config = new Dictionary<string, string>(),
                    DependsOn = new List<string>()
                }
            }
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name" && 
                                            e.ErrorMessage == "Workflow name must be between 1 and 200 characters");
    }

    [Fact]
    public void Validate_WithDescriptionTooLong_ShouldFail()
    {
        var request = new CreateWorkflowRequestDto
        {
            Name = "Test Workflow",
            Description = new string('a', 1001),
            TriggerType = "manual",
            Tasks = new List<WorkflowTaskDto>
            {
                new()
                {
                    Id = "task-1",
                    Name = "Task 1",
                    AgentType = "llm",
                    Prompt = "Test prompt",
                    Config = new Dictionary<string, string>(),
                    DependsOn = new List<string>()
                }
            }
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description" && 
                                            e.ErrorMessage == "Workflow description cannot exceed 1000 characters");
    }

    [Fact]
    public void Validate_WithEmptyTasks_ShouldFail()
    {
        var request = new CreateWorkflowRequestDto
        {
            Name = "Test Workflow",
            TriggerType = "manual",
            Tasks = new List<WorkflowTaskDto>()
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Tasks");
    }

    [Fact]
    public void Validate_WithNullTasks_ShouldFail()
    {
        var request = new CreateWorkflowRequestDto
        {
            Name = "Test Workflow",
            TriggerType = "manual",
            Tasks = null!
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Tasks");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_WithEmptyTriggerType_ShouldFail(string triggerType)
    {
        var request = new CreateWorkflowRequestDto
        {
            Name = "Test Workflow",
            TriggerType = triggerType,
            Tasks = new List<WorkflowTaskDto>
            {
                new()
                {
                    Id = "task-1",
                    Name = "Task 1",
                    AgentType = "llm",
                    Prompt = "Test prompt",
                    Config = new Dictionary<string, string>(),
                    DependsOn = new List<string>()
                }
            }
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TriggerType" && 
                                            e.ErrorMessage == "Trigger type is required");
    }

    [Fact]
    public void Validate_WithInvalidTriggerType_ShouldFail()
    {
        var request = new CreateWorkflowRequestDto
        {
            Name = "Test Workflow",
            TriggerType = "invalid",
            Tasks = new List<WorkflowTaskDto>
            {
                new()
                {
                    Id = "task-1",
                    Name = "Task 1",
                    AgentType = "llm",
                    Prompt = "Test prompt",
                    Config = new Dictionary<string, string>(),
                    DependsOn = new List<string>()
                }
            }
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "TriggerType" && 
                                            e.ErrorMessage == "Trigger type must be 'manual', 'scheduled', or 'webhook'");
    }

    [Theory]
    [InlineData("manual")]
    [InlineData("scheduled")]
    [InlineData("webhook")]
    public void Validate_WithValidTriggerTypes_ShouldPass(string triggerType)
    {
        var request = new CreateWorkflowRequestDto
        {
            Name = "Test Workflow",
            TriggerType = triggerType,
            Tasks = new List<WorkflowTaskDto>
            {
                new()
                {
                    Id = "task-1",
                    Name = "Task 1",
                    AgentType = "llm",
                    Prompt = "Test prompt",
                    Config = new Dictionary<string, string>(),
                    DependsOn = new List<string>()
                }
            }
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }
}

public sealed class WorkflowTaskDtoValidatorTests
{
    private readonly WorkflowTaskDtoValidator _validator = new();

    [Fact]
    public void Validate_WithValidData_ShouldPass()
    {
        var task = new WorkflowTaskDto
        {
            Id = "task-1",
            Name = "Test Task",
            AgentType = "llm",
            Prompt = "Test prompt",
            Config = new Dictionary<string, string>(),
            DependsOn = new List<string>()
        };

        var result = _validator.Validate(task);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_WithEmptyId_ShouldFail(string id)
    {
        var task = new WorkflowTaskDto
        {
            Id = id,
            Name = "Test Task",
            AgentType = "llm",
            Prompt = "Test prompt",
            Config = new Dictionary<string, string>(),
            DependsOn = new List<string>()
        };

        var result = _validator.Validate(task);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id" && 
                                            e.ErrorMessage == "Task ID is required");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_WithEmptyName_ShouldFail(string name)
    {
        var task = new WorkflowTaskDto
        {
            Id = "task-1",
            Name = name,
            AgentType = "llm",
            Prompt = "Test prompt",
            Config = new Dictionary<string, string>(),
            DependsOn = new List<string>()
        };

        var result = _validator.Validate(task);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name" && 
                                            e.ErrorMessage == "Task name is required");
    }

    [Fact]
    public void Validate_WithNameTooLong_ShouldFail()
    {
        var task = new WorkflowTaskDto
        {
            Id = "task-1",
            Name = new string('a', 101),
            AgentType = "llm",
            Prompt = "Test prompt",
            Config = new Dictionary<string, string>(),
            DependsOn = new List<string>()
        };

        var result = _validator.Validate(task);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name" && 
                                            e.ErrorMessage == "Task name must be between 1 and 100 characters");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_WithEmptyAgentType_ShouldFail(string agentType)
    {
        var task = new WorkflowTaskDto
        {
            Id = "task-1",
            Name = "Test Task",
            AgentType = agentType,
            Config = new Dictionary<string, string>(),
            DependsOn = new List<string>()
        };

        var result = _validator.Validate(task);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "AgentType" && 
                                            e.ErrorMessage == "Agent type is required");
    }

    [Fact]
    public void Validate_WithInvalidAgentType_ShouldFail()
    {
        var task = new WorkflowTaskDto
        {
            Id = "task-1",
            Name = "Test Task",
            AgentType = "invalid",
            Config = new Dictionary<string, string>(),
            DependsOn = new List<string>()
        };

        var result = _validator.Validate(task);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "AgentType" && 
                                            e.ErrorMessage == "Agent type must be 'http', 'python', 'sql', 'email', or 'llm'");
    }

    [Theory]
    [InlineData("http")]
    [InlineData("python")]
    [InlineData("sql")]
    [InlineData("email")]
    [InlineData("llm")]
    public void Validate_WithValidAgentTypes_ShouldPass(string agentType)
    {
        var task = new WorkflowTaskDto
        {
            Id = "task-1",
            Name = "Test Task",
            AgentType = agentType,
            Prompt = agentType == "llm" ? "test prompt" : null,
            Config = new Dictionary<string, string>(),
            DependsOn = new List<string>()
        };

        var result = _validator.Validate(task);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_WithLlmAgentTypeAndEmptyPrompt_ShouldFail(string prompt)
    {
        var task = new WorkflowTaskDto
        {
            Id = "task-1",
            Name = "Test Task",
            AgentType = "llm",
            Prompt = prompt,
            Config = new Dictionary<string, string>(),
            DependsOn = new List<string>()
        };

        var result = _validator.Validate(task);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Prompt" && 
                                            e.ErrorMessage == "Task prompt is required for LLM agent type");
    }

    [Fact]
    public void Validate_WithPromptTooLong_ShouldFail()
    {
        var task = new WorkflowTaskDto
        {
            Id = "task-1",
            Name = "Test Task",
            AgentType = "llm",
            Prompt = new string('a', 5001),
            Config = new Dictionary<string, string>(),
            DependsOn = new List<string>()
        };

        var result = _validator.Validate(task);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Prompt" && 
                                            e.ErrorMessage == "Task prompt cannot exceed 5000 characters");
    }
}