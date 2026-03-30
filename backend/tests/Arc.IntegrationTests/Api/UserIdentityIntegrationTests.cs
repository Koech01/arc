using System.Net;
using Arc.Api.DTOs;
using FluentAssertions;
using System.Net.Http.Json;
namespace Arc.IntegrationTests.Api;
using Microsoft.AspNetCore.Mvc.Testing;


public sealed class UserIdentityIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public UserIdentityIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Execute_WithUserIdHeader_ShouldGenerateUserSpecificExecutionId()
    {
        var client = _factory.CreateClient();
        await AuthenticateAsync(client);

        var response = await client.PostAsJsonAsync("/api/execute", new ExecuteRequestDto("Task A\nTask B"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ExecuteResponseDto>();
        result.Should().NotBeNull();
        result!.ExecutionId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Execute_WithoutUserIdHeader_ShouldUseAnonymousUser()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/execute", new ExecuteRequestDto("Task A\nTask B"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Execute_SameInputDifferentUsers_ShouldGenerateDifferentExecutionIds()
    {
        var request = new ExecuteRequestDto("Task A\nTask B");

        var user1Client = _factory.CreateClient();
        await AuthenticateAsync(user1Client);
        var response1 = await user1Client.PostAsJsonAsync("/api/execute", request);
        var result1 = await response1.Content.ReadFromJsonAsync<ExecuteResponseDto>();

        var user2Client = _factory.CreateClient();
        await AuthenticateAsync(user2Client);
        var response2 = await user2Client.PostAsJsonAsync("/api/execute", request);
        var result2 = await response2.Content.ReadFromJsonAsync<ExecuteResponseDto>();

        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
        result1!.ExecutionId.Should().NotBe(result2!.ExecutionId);
    }

    [Fact]
    public async Task Execute_SameInputSameUser_ShouldGenerateSameExecutionId()
    {
        var client = _factory.CreateClient();
        await AuthenticateAsync(client);

        var request = new ExecuteRequestDto("Task A\nTask B");
        var response1 = await client.PostAsJsonAsync("/api/execute", request);
        var response2 = await client.PostAsJsonAsync("/api/execute", request);
        var result1 = await response1.Content.ReadFromJsonAsync<ExecuteResponseDto>();
        var result2 = await response2.Content.ReadFromJsonAsync<ExecuteResponseDto>();

        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
        result1!.ExecutionId.Should().Be(result2!.ExecutionId);
    }

    [Fact]
    public async Task Execute_WithInvalidUserIdHeader_ShouldUseAnonymousUser()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-User-Id", "invalid-guid");

        var response = await client.PostAsJsonAsync("/api/execute", new ExecuteRequestDto("Task A\nTask B"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetExecution_ShouldReturnStoredExecutionWithUserId()
    {
        var client = _factory.CreateClient();
        await AuthenticateAsync(client);

        var createResponse = await client.PostAsJsonAsync("/api/execute", new ExecuteRequestDto("Task A\nTask B"));
        var createResult = await createResponse.Content.ReadFromJsonAsync<ExecuteResponseDto>();

        var getResponse = await client.GetAsync($"/api/execute/{createResult!.ExecutionId}");
        var getResult = await getResponse.Content.ReadFromJsonAsync<ExecuteResponseDto>();

        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        getResult!.ExecutionId.Should().Be(createResult.ExecutionId);
    }

    private static async Task AuthenticateAsync(HttpClient client)
    {
        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new
        {
            username = $"identity-{Guid.NewGuid():N}",
            email = $"identity-{Guid.NewGuid():N}@example.com",
            password = "Password123!"
        });

        registerResponse.EnsureSuccessStatusCode();
        var authCookie = registerResponse.Headers.GetValues("Set-Cookie")
            .FirstOrDefault(c => c.StartsWith("auth_token="));

        if (string.IsNullOrWhiteSpace(authCookie))
        {
            throw new InvalidOperationException("Authentication cookie was not issued.");
        }

        client.DefaultRequestHeaders.Remove("Cookie");
        client.DefaultRequestHeaders.Add("Cookie", authCookie.Split(';')[0]);
    }
}