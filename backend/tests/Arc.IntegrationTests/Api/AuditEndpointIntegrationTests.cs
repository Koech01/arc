using System.Net;
using Arc.Api.DTOs; 
using FluentAssertions;
using Arc.Api.DTOs.Audit;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Arc.Application.Telemetry;


namespace Arc.IntegrationTests.Api
{
    public class AuditEndpointIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public AuditEndpointIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetLogs_ShouldReturnAuditLogsAfterExecution()
        {
            await AuthenticateAsync();

            // Arrange: Execute a deterministic workflow
            var request = new ExecuteRequestDto("TaskA\nTaskB");
            var response = await _client.PostAsJsonAsync("/api/execute", request);
            response.EnsureSuccessStatusCode();

            var executeResult = await response.Content.ReadFromJsonAsync<ExecuteResponseDto>();
            executeResult.Should().NotBeNull();
            var executionId = executeResult!.ExecutionId;

            // Act: Retrieve audit logs
            var logsResponse = await _client.GetAsync($"/api/audit/{executionId}");
            logsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var logs = await logsResponse.Content.ReadFromJsonAsync<AuditLogEntryDto[]>();
            logs.Should().NotBeNull();
            logs!.Select(l => l.EventType).Should().Contain(new[]
            {
                Arc.Application.Telemetry.AuditEventType.OrchestratorStarted,
                Arc.Application.Telemetry.AuditEventType.TaskStarted,
                Arc.Application.Telemetry.AuditEventType.TaskFinished,
                Arc.Application.Telemetry.AuditEventType.OrchestratorFinished
            });
        }

        [Fact]
        public async Task CanFilterAuditLogsViaQueryParameters()
        {
            await AuthenticateAsync();

            var request = new ExecuteRequestDto("TaskA\nTaskB");
            var response = await _client.PostAsJsonAsync("/api/execute", request);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ExecuteResponseDto>();
            var executionId = result!.ExecutionId;

            var filteredResponse = await _client.GetAsync(
                $"/api/audit/{executionId}?eventType=TaskStarted"
            );

            filteredResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var logs = await filteredResponse.Content
                .ReadFromJsonAsync<AuditLogEntryDto[]>();

            logs.Should().NotBeEmpty();
            logs!.All(l => l.EventType == AuditEventType.TaskStarted)
                .Should().BeTrue();
        }

        private async Task AuthenticateAsync()
        {
            var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new
            {
                username = $"audit-{Guid.NewGuid():N}",
                email = $"audit-{Guid.NewGuid():N}@example.com",
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
}