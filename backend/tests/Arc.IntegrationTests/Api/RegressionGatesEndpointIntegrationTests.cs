using System.Net;
using Arc.Api.DTOs.Workflows;
using FluentAssertions;
using System.Net.Http.Json;
using Arc.Api.DTOs.Execution;
using Arc.Api.DTOs.RegressionGates;
namespace Arc.IntegrationTests.Api;
using Microsoft.AspNetCore.Mvc.Testing;


/// <summary>
/// Integration tests for regression gates endpoint.
/// Tests golden execution management and regression gate creation/testing.
/// </summary>
public sealed class RegressionGatesEndpointIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public RegressionGatesEndpointIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task CreateRegressionGate_WithValidRequest_ReturnsOk()
    {
        // Arrange
        var authToken = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Add("Cookie", authToken);

        // Create workflow, execute it, and mark as golden
        var workflowId = await CreateTestWorkflowAsync();
        var executionId = await ExecuteWorkflowAsync(workflowId);
        await MarkExecutionAsGoldenAsync(executionId, "Baseline");

        var request = new CreateRegressionGateRequestDto
        {
            Name = $"Test Gate {Guid.NewGuid()}",
            Description = "Integration test regression gate",
            GoldenExecutionId = executionId,
            WorkflowId = workflowId,
            Rules = new List<DivergenceRuleDto>
            {
                new() { Type = "similarity_percentage", Threshold = 0.95 },
                new() { Type = "max_task_divergence", Threshold = 0.90 }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/regression-gates", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var gate = await response.Content.ReadFromJsonAsync<RegressionGateResponseDto>();
        gate.Should().NotBeNull();
        gate!.Name.Should().Be(request.Name);
        gate.Description.Should().Be(request.Description);
        gate.GoldenExecutionId.Should().Be(executionId);
        gate.WorkflowId.Should().Be(workflowId);
        gate.Rules.Should().HaveCount(2);
        gate.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task CreateRegressionGate_WithNonExistentGoldenExecution_ReturnsNotFound()
    {
        // Arrange
        var authToken = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Add("Cookie", authToken);

        var request = new CreateRegressionGateRequestDto
        {
            Name = "Invalid Gate",
            Description = "Should fail",
            GoldenExecutionId = "nonexistent-execution-id",
            Rules = new List<DivergenceRuleDto>
            {
                new() { Type = "similarity_percentage", Threshold = 0.95 }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/regression-gates", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ListRegressionGates_ReturnsUserGates()
    {
        // Arrange
        var authToken = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Add("Cookie", authToken);

        // Create a regression gate first
        var workflowId = await CreateTestWorkflowAsync();
        var executionId = await ExecuteWorkflowAsync(workflowId);
        await MarkExecutionAsGoldenAsync(executionId, "Baseline");

        var createRequest = new CreateRegressionGateRequestDto
        {
            Name = $"List Test Gate {Guid.NewGuid()}",
            Description = "For listing test",
            GoldenExecutionId = executionId,
            WorkflowId = workflowId,
            Rules = new List<DivergenceRuleDto>
            {
                new() { Type = "similarity_percentage", Threshold = 0.95 }
            }
        };

        await _client.PostAsJsonAsync("/api/regression-gates", createRequest);

        // Act
        var response = await _client.GetAsync("/api/regression-gates");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var gates = await response.Content.ReadFromJsonAsync<List<RegressionGateResponseDto>>();
        gates.Should().NotBeNull();
        gates!.Should().NotBeEmpty();
        gates.Should().Contain(g => g.Name == createRequest.Name);
    }

    [Fact]
    public async Task ListRegressionGates_WithWorkflowIdFilter_ReturnsFilteredGates()
    {
        // Arrange
        var authToken = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Add("Cookie", authToken);

        var workflowId1 = await CreateTestWorkflowAsync();
        var executionId1 = await ExecuteWorkflowAsync(workflowId1);
        await MarkExecutionAsGoldenAsync(executionId1, "Baseline 1");

        var workflowId2 = await CreateTestWorkflowAsync();
        var executionId2 = await ExecuteWorkflowAsync(workflowId2);
        await MarkExecutionAsGoldenAsync(executionId2, "Baseline 2");

        var gateName1 = $"Gate WF1 {Guid.NewGuid()}";
        await _client.PostAsJsonAsync("/api/regression-gates", new CreateRegressionGateRequestDto
        {
            Name = gateName1,
            GoldenExecutionId = executionId1,
            WorkflowId = workflowId1,
            Rules = new List<DivergenceRuleDto> { new() { Type = "similarity_percentage", Threshold = 0.95 } }
        });

        await _client.PostAsJsonAsync("/api/regression-gates", new CreateRegressionGateRequestDto
        {
            Name = $"Gate WF2 {Guid.NewGuid()}",
            GoldenExecutionId = executionId2,
            WorkflowId = workflowId2,
            Rules = new List<DivergenceRuleDto> { new() { Type = "similarity_percentage", Threshold = 0.95 } }
        });

        // Act
        var response = await _client.GetAsync($"/api/regression-gates?workflowId={workflowId1}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var gates = await response.Content.ReadFromJsonAsync<List<RegressionGateResponseDto>>();
        gates.Should().NotBeNull();
        gates!.Should().HaveCountGreaterOrEqualTo(1);
        gates.Should().Contain(g => g.Name == gateName1);
        gates.All(g => g.WorkflowId == workflowId1).Should().BeTrue();
    }

    [Fact]
    public async Task GetRegressionGate_WithValidId_ReturnsGate()
    {
        // Arrange
        var authToken = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Add("Cookie", authToken);

        var workflowId = await CreateTestWorkflowAsync();
        var executionId = await ExecuteWorkflowAsync(workflowId);
        await MarkExecutionAsGoldenAsync(executionId, "Baseline");

        var createResponse = await _client.PostAsJsonAsync("/api/regression-gates", new CreateRegressionGateRequestDto
        {
            Name = $"Get Test Gate {Guid.NewGuid()}",
            GoldenExecutionId = executionId,
            WorkflowId = workflowId,
            Rules = new List<DivergenceRuleDto> { new() { Type = "similarity_percentage", Threshold = 0.95 } }
        });

        var createdGate = await createResponse.Content.ReadFromJsonAsync<RegressionGateResponseDto>();

        // Act
        var response = await _client.GetAsync($"/api/regression-gates/{createdGate!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var gate = await response.Content.ReadFromJsonAsync<RegressionGateResponseDto>();
        gate.Should().NotBeNull();
        gate!.Id.Should().Be(createdGate.Id);
        gate.Name.Should().Be(createdGate.Name);
    }

    [Fact]
    public async Task GetRegressionGate_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var authToken = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Add("Cookie", authToken);

        var nonExistentId = Guid.NewGuid().ToString();

        // Act
        var response = await _client.GetAsync($"/api/regression-gates/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetRegressionGate_WithMalformedId_ReturnsBadRequest()
    {
        // Arrange
        var authToken = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Add("Cookie", authToken);

        // Act
        var response = await _client.GetAsync("/api/regression-gates/invalid-id-format");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteRegressionGate_WithValidId_ReturnsNoContent()
    {
        // Arrange
        var authToken = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Add("Cookie", authToken);

        var workflowId = await CreateTestWorkflowAsync();
        var executionId = await ExecuteWorkflowAsync(workflowId);
        await MarkExecutionAsGoldenAsync(executionId, "Baseline");

        var createResponse = await _client.PostAsJsonAsync("/api/regression-gates", new CreateRegressionGateRequestDto
        {
            Name = $"Delete Test Gate {Guid.NewGuid()}",
            GoldenExecutionId = executionId,
            WorkflowId = workflowId,
            Rules = new List<DivergenceRuleDto> { new() { Type = "similarity_percentage", Threshold = 0.95 } }
        });

        var createdGate = await createResponse.Content.ReadFromJsonAsync<RegressionGateResponseDto>();

        // Act
        var response = await _client.DeleteAsync($"/api/regression-gates/{createdGate!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify deletion
        var getResponse = await _client.GetAsync($"/api/regression-gates/{createdGate.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ToggleRegressionGate_ChangesActiveStatus()
    {
        // Arrange
        var authToken = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Add("Cookie", authToken);

        var workflowId = await CreateTestWorkflowAsync();
        var executionId = await ExecuteWorkflowAsync(workflowId);
        await MarkExecutionAsGoldenAsync(executionId, "Baseline");

        var createResponse = await _client.PostAsJsonAsync("/api/regression-gates", new CreateRegressionGateRequestDto
        {
            Name = $"Toggle Test Gate {Guid.NewGuid()}",
            GoldenExecutionId = executionId,
            WorkflowId = workflowId,
            Rules = new List<DivergenceRuleDto> { new() { Type = "similarity_percentage", Threshold = 0.95 } }
        });

        var createdGate = await createResponse.Content.ReadFromJsonAsync<RegressionGateResponseDto>();
        createdGate!.IsActive.Should().BeTrue();

        // Act - Deactivate
        var toggleResponse = await _client.PatchAsync(
            $"/api/regression-gates/{createdGate.Id}/toggle",
            JsonContent.Create(new ToggleRegressionGateRequestDto { IsActive = false }));

        // Assert
        toggleResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var toggledGate = await toggleResponse.Content.ReadFromJsonAsync<RegressionGateResponseDto>();
        toggledGate.Should().NotBeNull();
        toggledGate!.IsActive.Should().BeFalse();

        // Act - Reactivate
        var reactivateResponse = await _client.PatchAsync(
            $"/api/regression-gates/{createdGate.Id}/toggle",
            JsonContent.Create(new ToggleRegressionGateRequestDto { IsActive = true }));

        // Assert
        reactivateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var reactivatedGate = await reactivateResponse.Content.ReadFromJsonAsync<RegressionGateResponseDto>();
        reactivatedGate!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task MarkExecutionAsGolden_WithValidExecution_ReturnsOk()
    {
        // Arrange
        var authToken = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Add("Cookie", authToken);

        var workflowId = await CreateTestWorkflowAsync();
        var executionId = await ExecuteWorkflowAsync(workflowId);

        var request = new MarkGoldenRequestDto { Label = "Baseline Execution" };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/executions/{executionId}/mark-golden", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task UnmarkExecutionAsGolden_WithGoldenExecution_ReturnsNoContent()
    {
        // Arrange
        var authToken = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Add("Cookie", authToken);

        var workflowId = await CreateTestWorkflowAsync();
        var executionId = await ExecuteWorkflowAsync(workflowId);
        await MarkExecutionAsGoldenAsync(executionId, "Baseline");

        // Act
        var response = await _client.DeleteAsync($"/api/executions/{executionId}/mark-golden");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task UnmarkExecutionAsGolden_WithNonGoldenExecution_ReturnsNotFound()
    {
        // Arrange
        var authToken = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Add("Cookie", authToken);

        var workflowId = await CreateTestWorkflowAsync();
        var executionId = await ExecuteWorkflowAsync(workflowId);
        // Don't mark as golden

        // Act
        var response = await _client.DeleteAsync($"/api/executions/{executionId}/mark-golden");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ListGoldenExecutions_ReturnsUserGoldenExecutions()
    {
        // Arrange
        var authToken = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Add("Cookie", authToken);

        var workflowId = await CreateTestWorkflowAsync();
        var executionId1 = await ExecuteWorkflowAsync(workflowId);

        await MarkExecutionAsGoldenAsync(executionId1, "Baseline 1");

        // Act
        var response = await _client.GetAsync("/api/executions/golden");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var goldenExecutions = await response.Content.ReadFromJsonAsync<List<GoldenExecutionMetadataDto>>();
        goldenExecutions.Should().NotBeNull();
        goldenExecutions!.Should().HaveCountGreaterOrEqualTo(1);
        goldenExecutions.Should().Contain(g => g.ExecutionId == executionId1 && g.Label == "Baseline 1");
    }

    [Fact]
    public async Task RunGateTest_WithMatchingExecution_ReturnsPassed()
    {
        // Arrange
        var authToken = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Add("Cookie", authToken);

        var workflowId = await CreateTestWorkflowAsync();
        var goldenExecutionId = await ExecuteWorkflowAsync(workflowId);
        await MarkExecutionAsGoldenAsync(goldenExecutionId, "Baseline");

        var createGateResponse = await _client.PostAsJsonAsync("/api/regression-gates", new CreateRegressionGateRequestDto
        {
            Name = $"Test Gate {Guid.NewGuid()}",
            GoldenExecutionId = goldenExecutionId,
            WorkflowId = workflowId,
            Rules = new List<DivergenceRuleDto>
            {
                new() { Type = "similarity_percentage", Threshold = 1.0 }
            }
        });

        var gate = await createGateResponse.Content.ReadFromJsonAsync<RegressionGateResponseDto>();

        // Execute workflow again (should produce identical deterministic results)
        var candidateExecutionId = await ExecuteWorkflowAsync(workflowId);

        var testRequest = new RunGateTestRequestDto { CandidateExecutionId = candidateExecutionId };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/regression-gates/{gate!.Id}/test", testRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<RegressionTestResultDto>();
        result.Should().NotBeNull();
        result!.GateId.Should().Be(gate.Id);
        result.CandidateExecutionId.Should().Be(candidateExecutionId);
        result.GoldenExecutionId.Should().Be(goldenExecutionId);
        result.Passed.Should().BeTrue("deterministic execution should match golden baseline");
        result.RuleResults.Should().NotBeEmpty();
        result.DivergenceSummary.Should().NotBeNull();
    }

    [Fact]
    public async Task RunGateTest_WithNonExistentCandidate_ReturnsNotFound()
    {
        // Arrange
        var authToken = await AuthenticateAsync();
        _client.DefaultRequestHeaders.Add("Cookie", authToken);

        var workflowId = await CreateTestWorkflowAsync();
        var executionId = await ExecuteWorkflowAsync(workflowId);
        await MarkExecutionAsGoldenAsync(executionId, "Baseline");

        var createGateResponse = await _client.PostAsJsonAsync("/api/regression-gates", new CreateRegressionGateRequestDto
        {
            Name = $"Test Gate {Guid.NewGuid()}",
            GoldenExecutionId = executionId,
            WorkflowId = workflowId,
            Rules = new List<DivergenceRuleDto> { new() { Type = "similarity_percentage", Threshold = 0.95 } }
        });

        var gate = await createGateResponse.Content.ReadFromJsonAsync<RegressionGateResponseDto>();

        var testRequest = new RunGateTestRequestDto { CandidateExecutionId = "nonexistent-execution-id" };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/regression-gates/{gate!.Id}/test", testRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // Helper methods
    private async Task<string> AuthenticateAsync()
    {
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            username = $"testuser_{Guid.NewGuid()}",
            email = $"test_{Guid.NewGuid()}@example.com",
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
            Name = $"Test Workflow {Guid.NewGuid()}",
            Description = "For regression gate testing",
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

    private async Task<string> ExecuteWorkflowAsync(string workflowId)
    {
        var request = new { input = new Dictionary<string, string>() };
        var response = await _client.PostAsJsonAsync($"/api/workflows/{workflowId}/execute", request);
        var result = await response.Content.ReadFromJsonAsync<WorkflowExecutionResponseDto>();
        return result!.ExecutionId;
    }

    private async Task MarkExecutionAsGoldenAsync(string executionId, string label)
    {
        var request = new MarkGoldenRequestDto { Label = label };
        await _client.PostAsJsonAsync($"/api/executions/{executionId}/mark-golden", request);
    }
}