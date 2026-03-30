using System.Net;
using Arc.Api.DTOs;
using System.Text.Json;
using FluentAssertions;
using System.Net.Http.Json;
using Arc.Api.DTOs.Execution;
namespace Arc.IntegrationTests.Api;
using Microsoft.AspNetCore.Mvc.Testing;


public sealed class ExecutionProfileEndpointIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ExecutionProfileEndpointIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetProfile_WithValidExecution_ReturnsProfile()
    {
        await AuthenticateAsync();

        // Arrange - First create an execution
        var executeRequest = new ExecuteRequestDto("Task 1: Analyze data\nTask 2: Generate report\nTask 3: Send email");
        var executeResponse = await _client.PostAsJsonAsync("/api/execute", executeRequest);
        executeResponse.EnsureSuccessStatusCode();

        var executeResult = await executeResponse.Content.ReadFromJsonAsync<ExecuteResponseDto>();
        executeResult.Should().NotBeNull();
        var executionId = executeResult!.ExecutionId;

        // Act - Get the profile
        var profileResponse = await _client.GetAsync($"/api/executions/{executionId}/profile");

        // Assert
        profileResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var profile = await profileResponse.Content.ReadFromJsonAsync<ExecutionPerformanceProfileDto>();
        profile.Should().NotBeNull();
        profile!.ExecutionId.Should().Be(executionId);
        profile.TaskMetrics.Should().HaveCount(3);
        profile.CriticalPath.Should().NotBeNull();
        profile.ResourceUtilization.Should().NotBeNull();
        profile.ProfileGeneratedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));

        // Verify task metrics structure
        foreach (var taskMetric in profile.TaskMetrics)
        {
            taskMetric.TaskId.Should().NotBeNullOrEmpty();
            taskMetric.TaskName.Should().NotBeNullOrEmpty();
            taskMetric.ExecutionOrder.Should().BeGreaterThan(0);
            taskMetric.ExecutionTimeMs.Should().BeGreaterOrEqualTo(0);
            taskMetric.DependencyWaitTimeMs.Should().BeGreaterOrEqualTo(0);
            taskMetric.Dependencies.Should().NotBeNull();
        }

        // Verify critical path analysis
        profile.CriticalPath.CriticalPathTaskIds.Should().NotBeEmpty();
        profile.CriticalPath.TotalCriticalPathTimeMs.Should().BeGreaterOrEqualTo(0);
        profile.CriticalPath.CriticalPathPercentage.Should().BeInRange(0, 100);

        // Verify resource utilization
        profile.ResourceUtilization.TotalExecutionTimeMs.Should().BeGreaterOrEqualTo(0);
        profile.ResourceUtilization.ParallelizableTimeMs.Should().BeGreaterOrEqualTo(0);
        profile.ResourceUtilization.SequentialTimeMs.Should().BeGreaterOrEqualTo(0);
        profile.ResourceUtilization.ParallelizationEfficiency.Should().BeInRange(0, 100);
        profile.ResourceUtilization.MaxConcurrentTasks.Should().BeGreaterThan(0);
        profile.ResourceUtilization.AverageTaskExecutionTimeMs.Should().BeGreaterOrEqualTo(0);
    }

    [Fact]
    public async Task GetProfile_WithNonExistentExecution_ReturnsNotFound()
    {
        await AuthenticateAsync();

        // Arrange
        const string nonExistentId = "non-existent-execution-id";

        // Act
        var response = await _client.GetAsync($"/api/executions/{nonExistentId}/profile");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetProfile_WithEmptyExecutionId_ReturnsBadRequest()
    {
        await AuthenticateAsync();

        // Act
        var response = await _client.GetAsync("/api/executions/ /profile");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetProfile_DeterministicBehavior_SameExecutionProducesSimilarProfile()
    {
        await AuthenticateAsync();

        // Arrange - Create an execution
        var executeRequest = new ExecuteRequestDto("Task 1: Process input\nTask 2: Transform data");
        var executeResponse = await _client.PostAsJsonAsync("/api/execute", executeRequest);
        executeResponse.EnsureSuccessStatusCode();

        var executeResult = await executeResponse.Content.ReadFromJsonAsync<ExecuteResponseDto>();
        var executionId = executeResult!.ExecutionId;

        // Act - Get profile twice
        var profileResponse1 = await _client.GetAsync($"/api/executions/{executionId}/profile");
        var profileResponse2 = await _client.GetAsync($"/api/executions/{executionId}/profile");

        // Assert
        profileResponse1.StatusCode.Should().Be(HttpStatusCode.OK);
        profileResponse2.StatusCode.Should().Be(HttpStatusCode.OK);

        var profile1 = await profileResponse1.Content.ReadFromJsonAsync<ExecutionPerformanceProfileDto>();
        var profile2 = await profileResponse2.Content.ReadFromJsonAsync<ExecutionPerformanceProfileDto>();

        profile1.Should().NotBeNull();
        profile2.Should().NotBeNull();

        // Verify deterministic aspects (excluding ProfileGeneratedAtUtc)
        profile1!.ExecutionId.Should().Be(profile2!.ExecutionId);
        profile1.TaskMetrics.Should().HaveCount(profile2.TaskMetrics.Count);
        
        // Task metrics should be deterministic
        for (int i = 0; i < profile1.TaskMetrics.Count; i++)
        {
            var task1 = profile1.TaskMetrics.ElementAt(i);
            var task2 = profile2.TaskMetrics.ElementAt(i);
            
            task1.TaskId.Should().Be(task2.TaskId);
            task1.TaskName.Should().Be(task2.TaskName);
            task1.ExecutionOrder.Should().Be(task2.ExecutionOrder);
            task1.ExecutionTimeMs.Should().Be(task2.ExecutionTimeMs);
            task1.DependencyWaitTimeMs.Should().Be(task2.DependencyWaitTimeMs);
            task1.IsCriticalPath.Should().Be(task2.IsCriticalPath);
            task1.Dependencies.Should().BeEquivalentTo(task2.Dependencies);
        }

        // Critical path should be deterministic
        profile1.CriticalPath.CriticalPathTaskIds.Should().BeEquivalentTo(profile2.CriticalPath.CriticalPathTaskIds);
        profile1.CriticalPath.TotalCriticalPathTimeMs.Should().Be(profile2.CriticalPath.TotalCriticalPathTimeMs);
        profile1.CriticalPath.CriticalPathPercentage.Should().Be(profile2.CriticalPath.CriticalPathPercentage);

        // Resource utilization should be deterministic
        profile1.ResourceUtilization.Should().BeEquivalentTo(profile2.ResourceUtilization);
    }

    [Fact]
    public async Task GetProfile_WithComplexExecution_ReturnsDetailedMetrics()
    {
        await AuthenticateAsync();

        // Arrange - Create a more complex execution with multiple tasks
        var complexInput = @"
            Task 1: Initialize system
            Task 2: Load configuration
            Task 3: Validate inputs
            Task 4: Process data batch 1
            Task 5: Process data batch 2
            Task 6: Merge results
            Task 7: Generate final report
            ";

        var executeRequest = new ExecuteRequestDto(complexInput);
        var executeResponse = await _client.PostAsJsonAsync("/api/execute", executeRequest);
        executeResponse.EnsureSuccessStatusCode();

        var executeResult = await executeResponse.Content.ReadFromJsonAsync<ExecuteResponseDto>();
        var executionId = executeResult!.ExecutionId;

        // Act
        var profileResponse = await _client.GetAsync($"/api/executions/{executionId}/profile");

        // Assert
        profileResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var profile = await profileResponse.Content.ReadFromJsonAsync<ExecutionPerformanceProfileDto>();
        profile.Should().NotBeNull();
        profile!.TaskMetrics.Should().HaveCount(7);

        // Verify execution order is maintained
        var orderedTasks = profile.TaskMetrics.OrderBy(t => t.ExecutionOrder).ToList();
        for (int i = 0; i < orderedTasks.Count; i++)
        {
            orderedTasks[i].ExecutionOrder.Should().Be(i + 1);
        }

        // Verify all tasks are on critical path (sequential execution)
        profile.TaskMetrics.Should().OnlyContain(t => t.IsCriticalPath);

        // Verify critical path includes all tasks
        profile.CriticalPath.CriticalPathTaskIds.Should().HaveCount(7);
        profile.CriticalPath.CriticalPathPercentage.Should().Be(100.0);

        // Verify resource utilization for sequential execution
        profile.ResourceUtilization.MaxConcurrentTasks.Should().Be(1);
        profile.ResourceUtilization.ParallelizationEfficiency.Should().Be(0.0);
        profile.ResourceUtilization.SequentialTimeMs.Should().Be(profile.ResourceUtilization.TotalExecutionTimeMs);
        profile.ResourceUtilization.ParallelizableTimeMs.Should().Be(0);
    }

    [Fact]
    public async Task GetProfile_ResponseSerialization_IsValid()
    {
        await AuthenticateAsync();

        // Arrange
        var executeRequest = new ExecuteRequestDto("Task 1: Test serialization");
        var executeResponse = await _client.PostAsJsonAsync("/api/execute", executeRequest);
        executeResponse.EnsureSuccessStatusCode();

        var executeResult = await executeResponse.Content.ReadFromJsonAsync<ExecuteResponseDto>();
        var executionId = executeResult!.ExecutionId;

        // Act
        var profileResponse = await _client.GetAsync($"/api/executions/{executionId}/profile");
        var jsonContent = await profileResponse.Content.ReadAsStringAsync();

        // Assert
        profileResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        jsonContent.Should().NotBeNullOrEmpty();

        // Verify JSON can be deserialized
        var profile = JsonSerializer.Deserialize<ExecutionPerformanceProfileDto>(jsonContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        profile.Should().NotBeNull();
        profile!.ExecutionId.Should().Be(executionId);
    }

    private async Task AuthenticateAsync()
    {
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            username = $"profile-{Guid.NewGuid():N}",
            email = $"profile-{Guid.NewGuid():N}@example.com",
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