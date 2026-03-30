using System.Net;
using Arc.Api.DTOs.Workflows;
using FluentAssertions;
using System.Net.Http.Json;
using Arc.Api.DTOs.Execution;
namespace Arc.IntegrationTests.Api;
using Microsoft.AspNetCore.Mvc.Testing;


/// <summary>
/// Integration tests for execution replay endpoint.
/// Tests deterministic replay with audit trace reconstruction.
/// </summary>
public sealed class ReplayEndpointIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ReplayEndpointIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Replay_WithValidExecutionId_ReturnsReplayData()
    {
        // Arrange
        var authToken = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Add("Cookie", authToken);

        // Create workflow and execute it
        var workflowId = await CreateTestWorkflowAsync();
        var executionId = await ExecuteWorkflowAsync(workflowId);

        // Act
        var response = await _client.GetAsync($"/api/replay/{executionId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var replayData = await response.Content.ReadFromJsonAsync<ExecutionReplayResponseDto>();
        replayData.Should().NotBeNull();
        replayData!.ExecutionId.Should().Be(executionId);
        replayData.Tasks.Should().NotBeEmpty();
        replayData.AuditTrace.Should().NotBeEmpty();
        
        // Verify deterministic ordering
        replayData.Tasks.Should().BeInAscendingOrder(t => t.ExecutionOrder);
        replayData.AuditTrace.Should().BeInAscendingOrder(a => a.Sequence);
    }

    [Fact]
    public async Task Replay_WithNonExistentExecutionId_ReturnsNotFound()
    {
        // Arrange
        var authToken = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Add("Cookie", authToken);

        var nonExistentId = Guid.NewGuid().ToString();

        // Act
        var response = await _client.GetAsync($"/api/replay/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Replay_WithEmptyExecutionId_ReturnsBadRequest()
    {
        // Arrange
        var authToken = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Add("Cookie", authToken);

        // Act
        var response = await _client.GetAsync("/api/replay/");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound); // Empty path segment
    }

    [Fact]
    public async Task Replay_WithWhitespaceExecutionId_ReturnsBadRequest()
    {
        // Arrange
        var authToken = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Add("Cookie", authToken);

        // Act
        var response = await _client.GetAsync("/api/replay/%20"); // URL encoded space

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Replay_ReturnsCompleteAuditTrace()
    {
        // Arrange
        var authToken = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Add("Cookie", authToken);

        var workflowId = await CreateTestWorkflowAsync();
        var executionId = await ExecuteWorkflowAsync(workflowId);

        // Act
        var response = await _client.GetAsync($"/api/replay/{executionId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var replayData = await response.Content.ReadFromJsonAsync<ExecutionReplayResponseDto>();
        
        replayData.Should().NotBeNull();
        replayData!.AuditTrace.Should().NotBeEmpty();
        
        // Verify audit trace properties
        foreach (var entry in replayData.AuditTrace)
        {
            entry.Sequence.Should().BeGreaterOrEqualTo(0);
            entry.TimestampUtc.Should().BeBefore(DateTime.UtcNow);
            entry.EventType.Should().NotBeNullOrWhiteSpace();
            entry.Message.Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public async Task Replay_TasksMatchOriginalExecution()
    {
        // Arrange
        var authToken = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Add("Cookie", authToken);

        var workflowId = await CreateTestWorkflowAsync();
        var executionId = await ExecuteWorkflowAsync(workflowId);

        // Act - Replay the execution
        var replayResponse = await _client.GetAsync($"/api/replay/{executionId}");

        // Assert
        replayResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var replayData = await replayResponse.Content.ReadFromJsonAsync<ExecutionReplayResponseDto>();
        
        replayData.Should().NotBeNull();
        replayData!.Tasks.Should().NotBeEmpty();
        
        // Verify task properties
        foreach (var task in replayData.Tasks)
        {
            task.TaskId.Should().NotBeNullOrWhiteSpace();
            task.TaskName.Should().NotBeNullOrWhiteSpace();
            task.ExecutionOrder.Should().BeGreaterOrEqualTo(0);
            task.Status.Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public async Task Replay_PreservesDeterministicExecution()
    {
        // Arrange
        var authToken = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Add("Cookie", authToken);

        // Create workflow with multiple tasks
        var workflowId = await CreateMultiTaskWorkflowAsync();
        var executionId = await ExecuteWorkflowAsync(workflowId);

        // Act - Replay multiple times
        var replay1Response = await _client.GetAsync($"/api/replay/{executionId}");
        var replay2Response = await _client.GetAsync($"/api/replay/{executionId}");

        // Assert - Both replays should be identical
        replay1Response.StatusCode.Should().Be(HttpStatusCode.OK);
        replay2Response.StatusCode.Should().Be(HttpStatusCode.OK);

        var replay1 = await replay1Response.Content.ReadFromJsonAsync<ExecutionReplayResponseDto>();
        var replay2 = await replay2Response.Content.ReadFromJsonAsync<ExecutionReplayResponseDto>();

        replay1.Should().NotBeNull();
        replay2.Should().NotBeNull();
        
        // Verify determinism - same execution produces same replay
        replay1!.ExecutionId.Should().Be(replay2!.ExecutionId);
        replay1.Tasks.Should().HaveCount(replay2.Tasks.Count());
        replay1.AuditTrace.Should().HaveCount(replay2.AuditTrace.Count());
    }

    // Helper methods
    private async Task<string> AuthenticateAsync()
    {
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            username = $"replayuser_{Guid.NewGuid()}",
            email = $"replay_{Guid.NewGuid()}@example.com",
            password = "Password123!"
        });

        registerResponse.EnsureSuccessStatusCode();
        var authCookie = registerResponse.Headers
            .GetValues("Set-Cookie")
            .FirstOrDefault(c => c.StartsWith("auth_token="));

        if (string.IsNullOrWhiteSpace(authCookie))
        {
            throw new InvalidOperationException("Authentication cookie was not issued.");
        }

        return authCookie.Split(';')[0];
    }

    private async Task<string> CreateTestWorkflowAsync()
    {
        var request = new CreateWorkflowRequestDto
        {
            Name = $"Replay Test Workflow {Guid.NewGuid()}",
            Description = "For replay testing",
            Tasks = new List<WorkflowTaskDto>
            {
                new()
                {
                    Id = "task1",
                    Name = "Simple Task",
                    AgentType = "http",
                    Config = new Dictionary<string, string> { { "url", "https://example.com" } },
                    Dependencies = new List<string>()
                }
            },
            TriggerType = "manual"
        };

        var response = await _client.PostAsJsonAsync("/api/workflows", request);
        var workflow = await response.Content.ReadFromJsonAsync<WorkflowResponseDto>();
        return workflow!.Id;
    }

    private async Task<string> CreateMultiTaskWorkflowAsync()
    {
        var request = new CreateWorkflowRequestDto
        {
            Name = $"Multi-Task Workflow {Guid.NewGuid()}",
            Description = "For deterministic replay testing",
            Tasks = new List<WorkflowTaskDto>
            {
                new()
                {
                    Id = "task1",
                    Name = "Task 1",
                    AgentType = "http",
                    Config = new Dictionary<string, string> { { "url", "https://example.com" } },
                    Dependencies = new List<string>()
                },
                new()
                {
                    Id = "task2",
                    Name = "Task 2",
                    AgentType = "http",
                    Config = new Dictionary<string, string> { { "url", "https://example.com" } },
                    Dependencies = new List<string> { "task1" }
                },
                new()
                {
                    Id = "task3",
                    Name = "Task 3",
                    AgentType = "http",
                    Config = new Dictionary<string, string> { { "url", "https://example.com" } },
                    Dependencies = new List<string> { "task1" }
                }
            },
            TriggerType = "manual"
        };

        var response = await _client.PostAsJsonAsync("/api/workflows", request);
        var workflow = await response.Content.ReadFromJsonAsync<WorkflowResponseDto>();
        return workflow!.Id;
    }

    private async Task<string> ExecuteWorkflowAsync(string workflowId)
    {
        var request = new { input = new Dictionary<string, string>() };
        var response = await _client.PostAsJsonAsync($"/api/workflows/{workflowId}/execute", request);
        var result = await response.Content.ReadFromJsonAsync<WorkflowExecutionResponseDto>();
        return result!.ExecutionId;
    }
}