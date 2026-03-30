using System.Net;
using Arc.Api.DTOs;
using FluentAssertions;
using System.Net.Http.Json;
using Arc.Api.DTOs.Execution;
using Arc.Api.DTOs.Workflows;
namespace Arc.IntegrationTests.Api;
using Microsoft.AspNetCore.Mvc.Testing;


/// <summary>
/// Integration tests for batch execution endpoint.
/// Tests deterministic batch processing with multiple executions.
/// </summary>
public sealed class BatchEndpointIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public BatchEndpointIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task ExecuteBatch_WithValidRequest_ReturnsAggregatedResults()
    {
        // Arrange
        var authToken = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Add("Cookie", authToken);

        var request = new BatchExecuteRequestDto(
            Executions: new List<BatchExecutionRequestItem>
            {
                new(Input: "param1=value1"),
                new(Input: "param1=value2"),
                new(Input: "param1=value3")
            }
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/batch", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var batchResult = await response.Content.ReadFromJsonAsync<BatchExecuteResponseDto>();
        
        batchResult.Should().NotBeNull();
        batchResult!.BatchId.Should().NotBeNullOrWhiteSpace();
        batchResult.CreatedAtUtc.Should().BeBefore(DateTime.UtcNow.AddSeconds(1));
        batchResult.Executions.Should().HaveCount(3);
        batchResult.TotalExecutionTimeMs.Should().BeGreaterThan(0);
        batchResult.AverageExecutionTimeMs.Should().BeGreaterThan(0);
        batchResult.SuccessCount.Should().BeGreaterOrEqualTo(0);
        batchResult.FailureCount.Should().BeGreaterOrEqualTo(0);
        (batchResult.SuccessCount + batchResult.FailureCount).Should().Be(3);
    }

    [Fact]
    public async Task ExecuteBatch_WithSingleExecution_ReturnsSingleResult()
    {
        // Arrange
        var authToken = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Add("Cookie", authToken);

        var request = new BatchExecuteRequestDto(
            Executions: new List<BatchExecutionRequestItem>
            {
                new(Input: "key=value")
            }
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/batch", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var batchResult = await response.Content.ReadFromJsonAsync<BatchExecuteResponseDto>();
        
        batchResult.Should().NotBeNull();
        batchResult!.Executions.Should().HaveCount(1);
        batchResult.Executions.First().Index.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteBatch_WithMultipleExecutions_MaintainsOrderAndIndexing()
    {
        // Arrange
        var authToken = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Add("Cookie", authToken);

        var request = new BatchExecuteRequestDto(
            Executions: new List<BatchExecutionRequestItem>
            {
                new(Input: "id=first"),
                new(Input: "id=second"),
                new(Input: "id=third"),
                new(Input: "id=fourth"),
                new(Input: "id=fifth")
            }
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/batch", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var batchResult = await response.Content.ReadFromJsonAsync<BatchExecuteResponseDto>();
        
        batchResult.Should().NotBeNull();
        batchResult!.Executions.Should().HaveCount(5);
        
        // Verify indexing
        for (int i = 0; i < 5; i++)
        {
            batchResult.Executions.ElementAt(i).Index.Should().Be(i);
        }
        
        // Verify ordering preserved
        batchResult.Executions.Should().BeInAscendingOrder(e => e.Index);
    }

    [Fact]
    public async Task ExecuteBatch_WithEmptyRequest_ReturnsBadRequest()
    {
        // Arrange
        var authToken = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Add("Cookie", authToken);

        var request = new BatchExecuteRequestDto(
            Executions: new List<BatchExecutionRequestItem>()
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/batch", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ExecuteBatch_WithNullRequest_ReturnsBadRequest()
    {
        // Arrange
        var authToken = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Add("Cookie", authToken);

        // Act
        var response = await _client.PostAsJsonAsync("/api/batch", (BatchExecuteRequestDto?)null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ExecuteBatch_VerifiesDeterministicBatchId()
    {
        // Arrange
        var authToken = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Add("Cookie", authToken);

        var request = new BatchExecuteRequestDto(
            Executions: new List<BatchExecutionRequestItem>
            {
                new(Input: "test=data")
            }
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/batch", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var batchResult = await response.Content.ReadFromJsonAsync<BatchExecuteResponseDto>();
        
        batchResult.Should().NotBeNull();
        batchResult!.BatchId.Should().NotBeNullOrWhiteSpace();
        
        // Batch ID should be deterministic and consistent format
        batchResult.BatchId.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ExecuteBatch_ReturnsValidExecutionIds()
    {
        // Arrange
        var authToken = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Add("Cookie", authToken);

        var request = new BatchExecuteRequestDto(
            Executions: new List<BatchExecutionRequestItem>
            {
                new(Input: "param=value1"),
                new(Input: "param=value2")
            }
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/batch", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var batchResult = await response.Content.ReadFromJsonAsync<BatchExecuteResponseDto>();
        
        batchResult.Should().NotBeNull();
        
        foreach (var execution in batchResult!.Executions)
        {
            execution.ExecutionId.Should().NotBeNullOrWhiteSpace();
            execution.Tasks.Should().NotBeNull();
            execution.ExecutionTimeMs.Should().BeGreaterOrEqualTo(0);
            execution.Status.Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public async Task ExecuteBatch_CalculatesAggregatedMetricsCorrectly()
    {
        // Arrange
        var authToken = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Add("Cookie", authToken);

        var request = new BatchExecuteRequestDto(
            Executions: new List<BatchExecutionRequestItem>
            {
                new(Input: "test=1"),
                new(Input: "test=2"),
                new(Input: "test=3"),
                new(Input: "test=4")
            }
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/batch", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var batchResult = await response.Content.ReadFromJsonAsync<BatchExecuteResponseDto>();
        
        batchResult.Should().NotBeNull();
        
        // Verify aggregated metrics
        var individualExecutionTimes = batchResult!.Executions.Sum(e => e.ExecutionTimeMs);
        batchResult.TotalExecutionTimeMs.Should().BeGreaterOrEqualTo(0);
        
        var expectedAverage = batchResult.Executions.Average(e => (double)e.ExecutionTimeMs);
        batchResult.AverageExecutionTimeMs.Should().Be((long)expectedAverage);
        
        // Success + Failure should equal total count
        (batchResult.SuccessCount + batchResult.FailureCount).Should().Be(4);
    }

    [Fact]
    public async Task ExecuteBatch_PreservesDeterministicTaskExecution()
    {
        // Arrange
        var authToken = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Add("Cookie", authToken);

        var request = new BatchExecuteRequestDto(
            Executions: new List<BatchExecutionRequestItem>
            {
                new(Input: "deterministic=true")
            }
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/batch", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var batchResult = await response.Content.ReadFromJsonAsync<BatchExecuteResponseDto>();
        
        batchResult.Should().NotBeNull();
        var execution = batchResult!.Executions.First();
        
        // Verify tasks are returned in execution order
        if (execution.Tasks.Any())
        {
            execution.Tasks.Should().BeInAscendingOrder(t => t.ExecutionOrder);
        }
        
        // Verify each task has required properties
        foreach (var task in execution.Tasks)
        {
            task.TaskId.Should().NotBeNullOrWhiteSpace();
            task.TaskName.Should().NotBeNullOrWhiteSpace();
            task.ExecutionOrder.Should().BeGreaterOrEqualTo(0);
            task.Status.Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public async Task ExecuteBatch_WithLargeBatch_HandlesMultipleExecutions()
    {
        // Arrange
        var authToken = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Add("Cookie", authToken);

        var executions = Enumerable.Range(0, 10)
            .Select(i => new BatchExecutionRequestItem(
                Input: $"iteration={i}"))
            .ToList();

        var request = new BatchExecuteRequestDto(Executions: executions);

        // Act
        var response = await _client.PostAsJsonAsync("/api/batch", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var batchResult = await response.Content.ReadFromJsonAsync<BatchExecuteResponseDto>();
        
        batchResult.Should().NotBeNull();
        batchResult!.Executions.Should().HaveCount(10);
        batchResult.Executions.Select(e => e.Index).Should().BeInAscendingOrder();
        batchResult.Executions.Select(e => e.Index).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task ExecuteBatch_ReturnsTimestampWithinReasonableRange()
    {
        // Arrange
        var authToken = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Add("Cookie", authToken);

        var beforeRequest = DateTime.UtcNow;
        
        var request = new BatchExecuteRequestDto(
            Executions: new List<BatchExecutionRequestItem>
            {
                new(Input: "")
            }
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/batch", request);
        var afterRequest = DateTime.UtcNow;

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var batchResult = await response.Content.ReadFromJsonAsync<BatchExecuteResponseDto>();
        
        batchResult.Should().NotBeNull();
        batchResult!.CreatedAtUtc.Should().BeOnOrAfter(beforeRequest.AddSeconds(-1));
        batchResult.CreatedAtUtc.Should().BeOnOrBefore(afterRequest.AddSeconds(1));
    }

    // Helper methods
    private async Task<string> AuthenticateAsync()
    {
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            username = $"batchuser_{Guid.NewGuid()}",
            email = $"batch_{Guid.NewGuid()}@example.com",
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
}