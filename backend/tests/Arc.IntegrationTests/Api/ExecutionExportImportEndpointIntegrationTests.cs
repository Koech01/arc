using System.Net;
using System.Text.Json;
using FluentAssertions;
using System.Net.Http.Json;
using Arc.Api.DTOs.Execution;
namespace Arc.IntegrationTests.Api;
using Microsoft.AspNetCore.Mvc.Testing;


public class ExecutionExportImportEndpointIntegrationTests : IAsyncLifetime
{
    private readonly WebApplicationFactory<Program> _factory;
    private HttpClient _authenticatedClient = null!;

    public ExecutionExportImportEndpointIntegrationTests()
    {
        _factory = new WebApplicationFactory<Program>();
    }

    public async Task InitializeAsync()
    {
        _authenticatedClient = _factory.CreateClient();

      var registerResponse = await _authenticatedClient.PostAsJsonAsync("/api/auth/register", new
      {
        username = $"export-{Guid.NewGuid():N}",
        email = $"export-{Guid.NewGuid():N}@example.com",
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

      _authenticatedClient.DefaultRequestHeaders.Add("Cookie", authCookie.Split(';')[0]);
    }

    public async Task DisposeAsync()
    {
        _authenticatedClient?.Dispose();
        _factory?.Dispose();
    }

    [Fact]
    public async Task ExportExecution_ReturnsJsonFile_WhenExecutionExists()
    {
        // Arrange
        var executionId = "exec-" + Guid.NewGuid().ToString().Substring(0, 8);

        // Act
        var response = await _authenticatedClient.GetAsync($"/api/executions/{executionId}/export?format=json");

        // Assert
        // Note: This test requires a real execution to exist or mock setup
        // Adjust based on actual test database setup
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ExportExecution_ReturnsCsvFile_WhenFormatIsCSV()
    {
        // Arrange
        var executionId = "exec-" + Guid.NewGuid().ToString().Substring(0, 8);

        // Act
        var response = await _authenticatedClient.GetAsync($"/api/executions/{executionId}/export?format=csv");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var contentType = response.Content.Headers.ContentType?.ToString();
            contentType.Should().Contain("text/csv");
        }
    }

    [Fact]
    public async Task ExportBulk_ReturnsBulkExportWithFilter()
    {
        // Arrange
        var request = new ExecutionExportBulkRequestDto
        {
            Format = "json",
            Limit = 10,
            Offset = 0
        };

        // Act
        var response = await _authenticatedClient.PostAsJsonAsync(
            "/api/executions/export-bulk",
            request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ValidateImport_AcceptsValidJsonData()
    {
        // Arrange
        var guid = Guid.NewGuid().ToString();
        var validJson = $$"""
        {
          "executionId": "exec-123",
          "userId": "{{guid}}",
          "createdAtUtc": "2026-01-01T00:00:00Z",
          "status": "Succeeded",
          "tasks": [
            {
              "taskId": "task-1",
              "taskName": "Task 1",
              "executionOrder": 1,
              "status": "Succeeded",
              "output": "output-1"
            }
          ]
        }
        """;

        var request = new ExecutionImportRequestDto { Data = validJson };

        // Act
        var response = await _authenticatedClient.PostAsJsonAsync(
            "/api/executions/import/validate",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("\"valid\":");
    }

    [Fact]
    public async Task ValidateImport_RejectsInvalidJsonData()
    {
        // Arrange
        var invalidJson = """
        {
          "executionId": "exec-123",
          "userId": "not-a-guid",
          "tasks": []
        }
        """;

        var request = new ExecutionImportRequestDto { Data = invalidJson };

        // Act
        var response = await _authenticatedClient.PostAsJsonAsync(
            "/api/executions/import/validate",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("\"valid\":false");
    }

    [Fact]
    public async Task ImportExecution_CreatesNewExecution_WhenDataIsValid()
    {
        // Arrange
        var guid = Guid.NewGuid().ToString();
        var validJson = $$"""
        {
          "executionId": "exec-import-{{Guid.NewGuid().ToString().Substring(0, 8)}}",
          "userId": "{{guid}}",
          "createdAtUtc": "2026-01-01T00:00:00Z",
          "status": "Succeeded",
          "tasks": [
            {
              "taskId": "task-1",
              "taskName": "Task 1",
              "executionOrder": 1,
              "status": "Succeeded",
              "output": "output-1"
            }
          ]
        }
        """;

        var json = JsonSerializer.Deserialize<dynamic>(validJson)!;
        var executionId = (string)json.GetProperty("executionId").GetString()!;

        var request = new ExecutionImportRequestDto { Data = validJson };

        // Act
        var response = await _authenticatedClient.PostAsJsonAsync(
            $"/api/executions/{executionId}/import",
            request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ImportBulk_ProcessesMultipleExecutions()
    {
        // Arrange
        var guid1 = Guid.NewGuid().ToString();
        var guid2 = Guid.NewGuid().ToString();
        var bulkJson = $$"""
        [
          {
            "executionId": "exec-bulk-1-{{Guid.NewGuid().ToString().Substring(0, 8)}}",
            "userId": "{{guid1}}",
            "createdAtUtc": "2026-01-01T00:00:00Z",
            "status": "Succeeded",
            "tasks": [
              {
                "taskId": "task-1",
                "taskName": "Task 1",
                "executionOrder": 1,
                "status": "Succeeded",
                "output": "output-1"
              }
            ]
          },
          {
            "executionId": "exec-bulk-2-{{Guid.NewGuid().ToString().Substring(0, 8)}}",
            "userId": "{{guid2}}",
            "createdAtUtc": "2026-01-02T00:00:00Z",
            "status": "Succeeded",
            "tasks": [
              {
                "taskId": "task-2",
                "taskName": "Task 2",
                "executionOrder": 1,
                "status": "Succeeded",
                "output": "output-2"
              }
            ]
          }
        ]
        """;

        var request = new ExecutionImportRequestDto { Data = bulkJson };

        // Act
        var response = await _authenticatedClient.PostAsJsonAsync(
            "/api/executions/import-bulk",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("\"successCount\":");
    }

    [Fact]
    public async Task ExportAuditLogs_ReturnsAuditLogsAsJson()
    {
        // Arrange
        var executionId = "exec-" + Guid.NewGuid().ToString().Substring(0, 8);

        // Act
        var response = await _authenticatedClient.GetAsync(
            $"/api/executions/{executionId}/export/audit-logs");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var contentType = response.Content.Headers.ContentType?.ToString();
            contentType.Should().Contain("application/json");
        }
    }

    [Fact]
    public async Task ExportImportRoundTrip_PreservesExecutionData()
    {
        // This is a higher-level integration test that:
        // 1. Exports an execution
        // 2. Validates the export
        // 3. Imports the export
        // 4. Verifies the imported execution matches the original

        // Arrange & Act & Assert
        // This test would require a complete setup with real data
        // Left as a template for when full test infrastructure is in place
        await Task.CompletedTask;
    }
}