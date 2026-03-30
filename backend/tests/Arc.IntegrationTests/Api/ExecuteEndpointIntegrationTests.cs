using System.Net;
using Arc.Api.DTOs;
using FluentAssertions;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;


namespace Arc.IntegrationTests.Api;
public sealed class ExecuteEndpointIntegrationTests
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ExecuteEndpointIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Execute_WithLinearTasks_ReturnsDeterministicExecutionOrder()
    {
        await AuthenticateAsync();

        var request = new ExecuteRequestDto("""
            Task A
            Task B
            Task C
            """);

        var response = await _client.PostAsJsonAsync("/api/execute", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await response.Content.ReadFromJsonAsync<ExecuteResponseDto>();
        payload.Should().NotBeNull();

        payload!.Tasks.Select(t => t.ExecutionOrder)
            .Should()
            .Equal(1, 2, 3);
    }

    [Fact]
    public async Task Execute_WithBranchingGraph_IsDeterministicAcrossRuns()
    {
        await AuthenticateAsync();

        var request = new ExecuteRequestDto("""
            Root
            Child 1 depends on Root
            Child 2 depends on Root
            """);

        var first = await _client.PostAsJsonAsync("/api/execute", request);
        var second = await _client.PostAsJsonAsync("/api/execute", request);

        var firstResult = await first.Content.ReadFromJsonAsync<ExecuteResponseDto>();
        var secondResult = await second.Content.ReadFromJsonAsync<ExecuteResponseDto>();

        firstResult!.Tasks.Should()
            .BeEquivalentTo(secondResult!.Tasks, opts => opts.WithStrictOrdering());
    }

    private async Task AuthenticateAsync()
    {
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            username = $"execute-{Guid.NewGuid():N}",
            email = $"execute-{Guid.NewGuid():N}@example.com",
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