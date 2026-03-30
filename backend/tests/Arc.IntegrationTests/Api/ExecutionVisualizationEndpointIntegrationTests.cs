using System.Net;
using Arc.Api.DTOs;
using FluentAssertions;
using System.Net.Http.Json;
using Arc.Api.DTOs.Execution;
namespace Arc.IntegrationTests.Api;
using Microsoft.AspNetCore.Mvc.Testing;


public sealed class ExecutionVisualizationEndpointIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ExecutionVisualizationEndpointIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetVisualization_WithValidExecution_ReturnsVisualization()
    {
        await AuthenticateAsync();

        // Arrange - First create an execution
        var executeRequest = new ExecuteRequestDto("Task 1: Analyze data\nTask 2: Generate report\nTask 3: Send email");
        var executeResponse = await _client.PostAsJsonAsync("/api/execute", executeRequest);
        executeResponse.EnsureSuccessStatusCode();

        var executeResult = await executeResponse.Content.ReadFromJsonAsync<ExecuteResponseDto>();
        executeResult.Should().NotBeNull();
        var executionId = executeResult!.ExecutionId;

        // Act - Get the visualization
        var visualizationResponse = await _client.GetAsync($"/api/executions/{executionId}/visualization");

        // Assert
        visualizationResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var visualization = await visualizationResponse.Content.ReadFromJsonAsync<ExecutionVisualizationDto>();
        visualization.Should().NotBeNull();
        visualization!.ExecutionId.Should().Be(executionId);
        visualization.DependencyGraph.Should().HaveCount(3);
        visualization.ExecutionTimeline.Should().NotBeEmpty();
        visualization.CriticalPathTaskIds.Should().NotBeEmpty();
        visualization.ResourceAllocation.Should().NotBeEmpty();
        visualization.GeneratedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));

        // Verify dependency graph structure
        foreach (var node in visualization.DependencyGraph)
        {
            node.TaskId.Should().NotBeNullOrEmpty();
            node.TaskName.Should().NotBeNullOrEmpty();
            node.ExecutionOrder.Should().BeGreaterThan(0);
            node.Status.Should().NotBeNullOrEmpty();
            node.Dependencies.Should().NotBeNull();
            node.ExecutionTimeMs.Should().BeGreaterOrEqualTo(0);
        }

        // Verify timeline structure
        foreach (var evt in visualization.ExecutionTimeline)
        {
            evt.TaskId.Should().NotBeNullOrEmpty();
            evt.TaskName.Should().NotBeNullOrEmpty();
            evt.StartTime.Should().BeBefore(evt.EndTime);
            evt.DurationMs.Should().BeGreaterThan(0);
            evt.EventType.Should().Be("TaskExecution");
        }

        // Verify resource allocation
        foreach (var snapshot in visualization.ResourceAllocation)
        {
            snapshot.ActiveTasks.Should().BeGreaterOrEqualTo(0);
            snapshot.RunningTaskIds.Should().NotBeNull();
        }
    }

    [Fact]
    public async Task GetVisualization_WithNonExistentExecution_ReturnsNotFound()
    {
        await AuthenticateAsync();

        // Arrange
        const string nonExistentId = "non-existent-execution-id";

        // Act
        var response = await _client.GetAsync($"/api/executions/{nonExistentId}/visualization");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetVisualization_WithEmptyExecutionId_ReturnsBadRequest()
    {
        await AuthenticateAsync();

        // Act
        var response = await _client.GetAsync("/api/executions/ /visualization");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetVisualization_DeterministicBehavior_SameExecutionProducesSimilarVisualization()
    {
        await AuthenticateAsync();

        // Arrange - Create an execution
        var executeRequest = new ExecuteRequestDto("Task 1: Process input\nTask 2: Transform data");
        var executeResponse = await _client.PostAsJsonAsync("/api/execute", executeRequest);
        executeResponse.EnsureSuccessStatusCode();

        var executeResult = await executeResponse.Content.ReadFromJsonAsync<ExecuteResponseDto>();
        var executionId = executeResult!.ExecutionId;

        // Act - Get visualization twice
        var visualizationResponse1 = await _client.GetAsync($"/api/executions/{executionId}/visualization");
        var visualizationResponse2 = await _client.GetAsync($"/api/executions/{executionId}/visualization");

        // Assert
        visualizationResponse1.StatusCode.Should().Be(HttpStatusCode.OK);
        visualizationResponse2.StatusCode.Should().Be(HttpStatusCode.OK);

        var visualization1 = await visualizationResponse1.Content.ReadFromJsonAsync<ExecutionVisualizationDto>();
        var visualization2 = await visualizationResponse2.Content.ReadFromJsonAsync<ExecutionVisualizationDto>();

        visualization1.Should().NotBeNull();
        visualization2.Should().NotBeNull();

        // Verify deterministic aspects (excluding GeneratedAtUtc)
        visualization1!.ExecutionId.Should().Be(visualization2!.ExecutionId);
        visualization1.DependencyGraph.Should().HaveCount(visualization2.DependencyGraph.Count);
        visualization1.ExecutionTimeline.Should().HaveCount(visualization2.ExecutionTimeline.Count);
        visualization1.CriticalPathTaskIds.Should().BeEquivalentTo(visualization2.CriticalPathTaskIds);
        visualization1.ResourceAllocation.Should().HaveCount(visualization2.ResourceAllocation.Count);
    }

    private async Task AuthenticateAsync()
    {
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            username = $"visual-{Guid.NewGuid():N}",
            email = $"visual-{Guid.NewGuid():N}@example.com",
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

        _client.DefaultRequestHeaders.Remove("Cookie");
        _client.DefaultRequestHeaders.Add("Cookie", authCookie.Split(';')[0]);
    }
}