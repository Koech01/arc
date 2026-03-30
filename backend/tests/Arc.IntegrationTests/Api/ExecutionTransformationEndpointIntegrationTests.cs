using System.Net;
using Arc.Api.DTOs;
using FluentAssertions;
using System.Net.Http.Json;
using Arc.Api.DTOs.Execution;
namespace Arc.IntegrationTests.Api;
using Microsoft.AspNetCore.Mvc.Testing;


public sealed class ExecutionTransformationEndpointIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ExecutionTransformationEndpointIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task TransformExecution_WithValidRequest_ReturnsTransformedExecution()
    {
        await AuthenticateAsync();

        // Arrange - First create an execution to transform
        var executeRequest = new ExecuteRequestDto("Task A\nTask B depends on Task A\nTask C depends on Task B");
        var executeResponse = await _client.PostAsJsonAsync("/api/execute", executeRequest);
        executeResponse.EnsureSuccessStatusCode();
        
        var executionResult = await executeResponse.Content.ReadFromJsonAsync<ExecuteResponseDto>();
        var originalExecutionId = executionResult!.ExecutionId;
        // Planner generates task-1, task-2, task-3
        var firstTaskId = executionResult.Tasks.OrderBy(t => t.ExecutionOrder).First().TaskId;

        var transformRequest = new ExecutionTransformationRequestDto(
            ExecutionId: originalExecutionId,
            TaskMappings: new[]
            {
                new TaskMappingRuleDto(firstTaskId, "transformed-task-1", "Transformed Task 1")
            },
            DependencyRewiring: Array.Empty<DependencyRewiringRuleDto>()
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/executions/transform", transformRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var transformResult = await response.Content.ReadFromJsonAsync<ExecutionTransformationResponseDto>();
        transformResult.Should().NotBeNull();
        transformResult!.TransformedExecutionId.Should().StartWith("transformed-");
        transformResult.TransformedTasks.Should().HaveCount(3);
        transformResult.Summary.OriginalExecutionId.Should().Be(originalExecutionId);
        transformResult.Summary.TasksMapped.Should().Be(1);
        transformResult.Summary.DependenciesRewired.Should().Be(0);
        
        var transformedTask1 = transformResult.TransformedTasks.First(t => t.TaskId == "transformed-task-1");
        transformedTask1.TaskName.Should().Be("Transformed Task 1");
    }

    [Fact]
    public async Task TransformExecution_WithSameRequest_ReturnsSameTransformedExecutionId()
    {
        await AuthenticateAsync();

        // Arrange - First create an execution to transform
        var executeRequest = new ExecuteRequestDto("Task X\nTask Y depends on Task X");
        var executeResponse = await _client.PostAsJsonAsync("/api/execute", executeRequest);
        executeResponse.EnsureSuccessStatusCode();
        
        var executionResult = await executeResponse.Content.ReadFromJsonAsync<ExecuteResponseDto>();
        var originalExecutionId = executionResult!.ExecutionId;

        var transformRequest = new ExecutionTransformationRequestDto(
            ExecutionId: originalExecutionId,
            TaskMappings: new[]
            {
                new TaskMappingRuleDto("task-x", "new-task-x")
            },
            DependencyRewiring: Array.Empty<DependencyRewiringRuleDto>()
        );

        // Act
        var response1 = await _client.PostAsJsonAsync("/api/executions/transform", transformRequest);
        var response2 = await _client.PostAsJsonAsync("/api/executions/transform", transformRequest);

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var result1 = await response1.Content.ReadFromJsonAsync<ExecutionTransformationResponseDto>();
        var result2 = await response2.Content.ReadFromJsonAsync<ExecutionTransformationResponseDto>();
        
        result1!.TransformedExecutionId.Should().Be(result2!.TransformedExecutionId);
    }

    [Fact]
    public async Task TransformExecution_WithNonExistentExecutionId_ReturnsNotFound()
    {
        await AuthenticateAsync();

        // Arrange
        var transformRequest = new ExecutionTransformationRequestDto(
            ExecutionId: "non-existent-execution-id",
            TaskMappings: Array.Empty<TaskMappingRuleDto>(),
            DependencyRewiring: Array.Empty<DependencyRewiringRuleDto>()
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/executions/transform", transformRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TransformExecution_WithEmptyExecutionId_ReturnsBadRequest()
    {
        await AuthenticateAsync();

        // Arrange
        var transformRequest = new ExecutionTransformationRequestDto(
            ExecutionId: "",
            TaskMappings: Array.Empty<TaskMappingRuleDto>(),
            DependencyRewiring: Array.Empty<DependencyRewiringRuleDto>()
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/executions/transform", transformRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task TransformExecution_WithDependencyRewiring_AppliesCorrectly()
    {
        await AuthenticateAsync();

        // Arrange - First create an execution to transform
        var executeRequest = new ExecuteRequestDto("Task P\nTask Q depends on Task P\nTask R depends on Task Q");
        var executeResponse = await _client.PostAsJsonAsync("/api/execute", executeRequest);
        executeResponse.EnsureSuccessStatusCode();
        
        var executionResult = await executeResponse.Content.ReadFromJsonAsync<ExecuteResponseDto>();
        var originalExecutionId = executionResult!.ExecutionId;

        var transformRequest = new ExecutionTransformationRequestDto(
            ExecutionId: originalExecutionId,
            TaskMappings: Array.Empty<TaskMappingRuleDto>(),
            DependencyRewiring: new[]
            {
                new DependencyRewiringRuleDto("task-q", new[] { "task-r" })
            }
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/executions/transform", transformRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var transformResult = await response.Content.ReadFromJsonAsync<ExecutionTransformationResponseDto>();
        transformResult.Should().NotBeNull();
        transformResult!.Summary.DependenciesRewired.Should().Be(1);
    }

    [Fact]
    public async Task TransformExecution_CanRetrieveTransformedExecutionViaGetEndpoint()
    {
        await AuthenticateAsync();

        // Arrange - First create an execution to transform
        var executeRequest = new ExecuteRequestDto("Task Alpha\nTask Beta depends on Task Alpha");
        var executeResponse = await _client.PostAsJsonAsync("/api/execute", executeRequest);
        executeResponse.EnsureSuccessStatusCode();
        
        var executionResult = await executeResponse.Content.ReadFromJsonAsync<ExecuteResponseDto>();
        var originalExecutionId = executionResult!.ExecutionId;
        // Planner generates task-1, task-2 - use actual first task ID
        var firstTaskId = executionResult.Tasks.OrderBy(t => t.ExecutionOrder).First().TaskId;

        var transformRequest = new ExecutionTransformationRequestDto(
            ExecutionId: originalExecutionId,
            TaskMappings: new[]
            {
                new TaskMappingRuleDto(firstTaskId, "task-1-v2", "Task 1 V2")
            },
            DependencyRewiring: Array.Empty<DependencyRewiringRuleDto>()
        );

        // Act - Transform execution
        var transformResponse = await _client.PostAsJsonAsync("/api/executions/transform", transformRequest);
        transformResponse.EnsureSuccessStatusCode();
        
        var transformResult = await transformResponse.Content.ReadFromJsonAsync<ExecutionTransformationResponseDto>();
        var transformedExecutionId = transformResult!.TransformedExecutionId;

        // Act - Retrieve transformed execution
        var getResponse = await _client.GetAsync($"/api/execute/{transformedExecutionId}");

        // Assert
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var retrievedExecution = await getResponse.Content.ReadFromJsonAsync<ExecuteResponseDto>();
        retrievedExecution.Should().NotBeNull();
        retrievedExecution!.ExecutionId.Should().Be(transformedExecutionId);
        retrievedExecution.Tasks.Should().HaveCount(2);
        
        var transformedTask = retrievedExecution.Tasks.First(t => t.TaskId == "task-1-v2");
        transformedTask.TaskName.Should().Be("Task 1 V2");
    }

    private async Task AuthenticateAsync()
    {
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            username = $"transform-{Guid.NewGuid():N}",
            email = $"transform-{Guid.NewGuid():N}@example.com",
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