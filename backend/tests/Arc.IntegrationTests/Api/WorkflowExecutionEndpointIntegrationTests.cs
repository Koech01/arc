using System.Net;
using FluentAssertions;
using System.Net.Http.Json;
using Arc.Api.DTOs;
using Arc.Api.DTOs.Workflows;
namespace Arc.IntegrationTests.Api;
using Microsoft.AspNetCore.Mvc.Testing;


public sealed class WorkflowExecutionEndpointIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public WorkflowExecutionEndpointIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task ExecuteWorkflow_WithValidWorkflow_ReturnsExecutionResult()
    {
        var authToken = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Add("Cookie", authToken);

        var workflowId = await CreateTestWorkflowAsync();

        var response = await _client.PostAsync($"/api/workflows/{workflowId}/execute", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<WorkflowExecutionResponseDto>();
        result.Should().NotBeNull();
        result!.ExecutionId.Should().NotBeNullOrEmpty();
        result.WorkflowId.Should().Be(workflowId);
        result.Tasks.Should().HaveCount(3);
        result.Tasks.Should().BeInAscendingOrder(t => t.ExecutionOrder);
    }

    [Fact]
    public async Task ExecuteWorkflow_WithNonExistentWorkflow_ReturnsNotFound()
    {
        var authToken = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Add("Cookie", authToken);

        var response = await _client.PostAsync("/api/workflows/nonexistent-id/execute", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ExecuteWorkflow_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await _client.PostAsync("/api/workflows/some-id/execute", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ExecuteWorkflow_WithOtherUsersWorkflow_ReturnsForbidden()
    {
        var user1Token = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Add("Cookie", user1Token);
        var workflowId = await CreateTestWorkflowAsync();
        _client.DefaultRequestHeaders.Remove("Cookie");

        var user2Token = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Add("Cookie", user2Token);

        var response = await _client.PostAsync($"/api/workflows/{workflowId}/execute", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ExecuteWorkflow_MultipleTimes_GeneratesDeterministicExecutionIds()
    {
        var authToken = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Add("Cookie", authToken);

        var workflowId = await CreateTestWorkflowAsync();

        var response1 = await _client.PostAsync($"/api/workflows/{workflowId}/execute", null);
        var result1 = await response1.Content.ReadFromJsonAsync<WorkflowExecutionResponseDto>();

        var response2 = await _client.PostAsync($"/api/workflows/{workflowId}/execute", null);
        var result2 = await response2.Content.ReadFromJsonAsync<WorkflowExecutionResponseDto>();

        result1!.ExecutionId.Should().Be(result2!.ExecutionId);
        result1.Tasks.Should().BeEquivalentTo(result2.Tasks);
    }

    [Fact]
    public async Task ExecuteWorkflow_WithDependencies_RespectsExecutionOrder()
    {
        var authToken = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Add("Cookie", authToken);

        var workflowId = await CreateTestWorkflowAsync();

        var response = await _client.PostAsync($"/api/workflows/{workflowId}/execute", null);
        var result = await response.Content.ReadFromJsonAsync<WorkflowExecutionResponseDto>();

        result.Should().NotBeNull();
        var task1 = result!.Tasks.First(t => t.TaskId == "task1");
        var task2 = result.Tasks.First(t => t.TaskId == "task2");
        var task3 = result.Tasks.First(t => t.TaskId == "task3");

        task1.ExecutionOrder.Should().BeLessThan(task2.ExecutionOrder);
        task1.ExecutionOrder.Should().BeLessThan(task3.ExecutionOrder);
        task2.ExecutionOrder.Should().BeLessThan(task3.ExecutionOrder);
    }

    [Fact]
    public async Task ExecuteWorkflow_StoresExecutionResultForRetrieval()
    {
        var authToken = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Add("Cookie", authToken);

        var workflowId = await CreateTestWorkflowAsync();

        var executeResponse = await _client.PostAsync($"/api/workflows/{workflowId}/execute", null);
        var executeResult = await executeResponse.Content.ReadFromJsonAsync<WorkflowExecutionResponseDto>();

        var retrieveResponse = await _client.GetAsync($"/api/execute/{executeResult!.ExecutionId}");

        retrieveResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var retrievedResult = await retrieveResponse.Content.ReadFromJsonAsync<ExecuteResponseDto>();
        retrievedResult.Should().NotBeNull();
        retrievedResult!.ExecutionId.Should().Be(executeResult!.ExecutionId);
    }

    private async Task<string> AuthenticateAsync()
    {
        var registerRequest = new
        {
            username = $"workflow-exec-{Guid.NewGuid():N}",
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

    private async Task<string> CreateTestWorkflowAsync()
    {
        var request = new CreateWorkflowRequestDto
        {
            Name = $"Test Workflow {Guid.NewGuid()}",
            Description = "Integration test workflow",
            Tasks = new List<WorkflowTaskDto>
            {
                new()
                {
                    Id = "task1",
                    Name = "First Task",
                    AgentType = "http",
                    Config = new Dictionary<string, string>(),
                    Dependencies = new List<string>()
                },
                new()
                {
                    Id = "task2",
                    Name = "Second Task",
                    AgentType = "python",
                    Config = new Dictionary<string, string>(),
                    Dependencies = new List<string> { "task1" }
                },
                new()
                {
                    Id = "task3",
                    Name = "Third Task",
                    AgentType = "sql",
                    Config = new Dictionary<string, string>(),
                    Dependencies = new List<string> { "task1", "task2" }
                }
            },
            TriggerType = "manual"
        };

        var response = await _client.PostAsJsonAsync("/api/workflows", request);
        var workflow = await response.Content.ReadFromJsonAsync<WorkflowResponseDto>();
        return workflow!.Id;
    }
}