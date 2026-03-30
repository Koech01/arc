using System.Net;
using FluentAssertions;
using System.Net.Http.Json;
using Arc.Api.DTOs.Workflows;
namespace Arc.IntegrationTests.Api;
using Microsoft.AspNetCore.Mvc.Testing;


public sealed class WorkflowEndpointIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public WorkflowEndpointIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task CreateWorkflow_WithValidRequest_ReturnsCreated()
    {
        var authToken = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Add("Cookie", authToken);

        var request = new CreateWorkflowRequestDto
        {
            Name = $"Test Workflow {Guid.NewGuid()}",
            Description = "Integration test workflow",
            Tasks = new List<WorkflowTaskDto>
            {
                new()
                {
                    Id = "task1",
                    Name = "HTTP Task",
                    AgentType = "http",
                    Config = new Dictionary<string, string> { { "url", "https://example.com" } },
                    Dependencies = new List<string>()
                }
            },
            TriggerType = "manual"
        };

        var response = await _client.PostAsJsonAsync("/api/workflows", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var workflow = await response.Content.ReadFromJsonAsync<WorkflowResponseDto>();
        workflow.Should().NotBeNull();
        workflow!.Name.Should().Be(request.Name);
        workflow.Description.Should().Be(request.Description);
    }

    [Fact]
    public async Task CreateWorkflow_WithDuplicateName_ReturnsConflict()
    {
        var authToken = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Add("Cookie", authToken);

        var workflowName = $"Duplicate Workflow {Guid.NewGuid()}";
        var request = new CreateWorkflowRequestDto
        {
            Name = workflowName,
            Description = "First workflow",
            Tasks = new List<WorkflowTaskDto>
            {
                new()
                {
                    Id = "task1",
                    Name = "Task 1",
                    AgentType = "http",
                    Config = new Dictionary<string, string>(),
                    Dependencies = new List<string>()
                }
            },
            TriggerType = "manual"
        };

        await _client.PostAsJsonAsync("/api/workflows", request);

        var duplicateRequest = new CreateWorkflowRequestDto
        {
            Name = workflowName,
            Description = "Duplicate workflow",
            Tasks = new List<WorkflowTaskDto>
            {
                new()
                {
                    Id = "task2",
                    Name = "Task 2",
                    AgentType = "http",
                    Config = new Dictionary<string, string>(),
                    Dependencies = new List<string>()
                }
            },
            TriggerType = "manual"
        };

        var response = await _client.PostAsJsonAsync("/api/workflows", duplicateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateWorkflow_WithInvalidTriggerType_ReturnsBadRequest()
    {
        var authToken = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Add("Cookie", authToken);

        var request = new CreateWorkflowRequestDto
        {
            Name = "Invalid Workflow",
            Description = "Test",
            Tasks = new List<WorkflowTaskDto>
            {
                new()
                {
                    Id = "task1",
                    Name = "Task 1",
                    AgentType = "http",
                    Config = new Dictionary<string, string>(),
                    Dependencies = new List<string>()
                }
            },
            TriggerType = "invalid"
        };

        var response = await _client.PostAsJsonAsync("/api/workflows", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetWorkflow_WithValidId_ReturnsWorkflow()
    {
        var authToken = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Add("Cookie", authToken);

        var createRequest = new CreateWorkflowRequestDto
        {
            Name = $"Get Test Workflow {Guid.NewGuid()}",
            Description = "Test workflow",
            Tasks = new List<WorkflowTaskDto>
            {
                new()
                {
                    Id = "task1",
                    Name = "Task 1",
                    AgentType = "http",
                    Config = new Dictionary<string, string>(),
                    Dependencies = new List<string>()
                }
            },
            TriggerType = "manual"
        };

        var createResponse = await _client.PostAsJsonAsync("/api/workflows", createRequest);
        var createdWorkflow = await createResponse.Content.ReadFromJsonAsync<WorkflowResponseDto>();

        var getResponse = await _client.GetAsync($"/api/workflows/{createdWorkflow!.Id}");

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var workflow = await getResponse.Content.ReadFromJsonAsync<WorkflowDetailDto>();
        workflow.Should().NotBeNull();
        workflow!.Id.Should().Be(createdWorkflow.Id);
        workflow.Name.Should().Be(createRequest.Name);
        workflow.Tasks.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetWorkflow_WithInvalidId_ReturnsNotFound()
    {
        var authToken = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Add("Cookie", authToken);

        var response = await _client.GetAsync("/api/workflows/nonexistent-id");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ListWorkflows_ReturnsUserWorkflows()
    {
        var authToken = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Add("Cookie", authToken);

        var response = await _client.GetAsync("/api/workflows");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var workflows = await response.Content.ReadFromJsonAsync<List<WorkflowResponseDto>>();
        workflows.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteWorkflow_WithValidId_ReturnsNoContent()
    {
        var authToken = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Add("Cookie", authToken);

        var createRequest = new CreateWorkflowRequestDto
        {
            Name = $"Delete Test Workflow {Guid.NewGuid()}",
            Description = "Test workflow",
            Tasks = new List<WorkflowTaskDto>
            {
                new()
                {
                    Id = "task1",
                    Name = "Task 1",
                    AgentType = "http",
                    Config = new Dictionary<string, string>(),
                    Dependencies = new List<string>()
                }
            },
            TriggerType = "manual"
        };

        var createResponse = await _client.PostAsJsonAsync("/api/workflows", createRequest);
        var createdWorkflow = await createResponse.Content.ReadFromJsonAsync<WorkflowResponseDto>();

        var deleteResponse = await _client.DeleteAsync($"/api/workflows/{createdWorkflow!.Id}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await _client.GetAsync($"/api/workflows/{createdWorkflow.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateWorkflow_WithCircularDependencies_ReturnsBadRequest()
    {
        var authToken = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Add("Cookie", authToken);

        var request = new CreateWorkflowRequestDto
        {
            Name = $"Circular Workflow {Guid.NewGuid()}",
            Description = "Test circular dependencies",
            Tasks = new List<WorkflowTaskDto>
            {
                new()
                {
                    Id = "task1",
                    Name = "Task 1",
                    AgentType = "http",
                    Config = new Dictionary<string, string>(),
                    Dependencies = new List<string> { "task2" }
                },
                new()
                {
                    Id = "task2",
                    Name = "Task 2",
                    AgentType = "http",
                    Config = new Dictionary<string, string>(),
                    Dependencies = new List<string> { "task1" }
                }
            },
            TriggerType = "manual"
        };

        var response = await _client.PostAsJsonAsync("/api/workflows", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task<string> AuthenticateAsync()
    {
        var registerRequest = new
        {
            username = $"workflow-{Guid.NewGuid():N}",
            email = $"test-{Guid.NewGuid()}@example.com",
            password = "TestPassword123!",
            role = "User"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        response.EnsureSuccessStatusCode();
        var authCookie = response.Headers
            .GetValues("Set-Cookie")
            .FirstOrDefault(c => c.StartsWith("auth_token="));

        if (string.IsNullOrWhiteSpace(authCookie))
        {
            throw new InvalidOperationException("Authentication cookie was not issued.");
        }

        return authCookie.Split(';')[0];
    }
}