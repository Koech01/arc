using System.Net;
using Arc.Api.DTOs.Admin;
using Arc.Api.Controllers;
using System.Net.Http.Json;
namespace Arc.IntegrationTests.Api;
using Microsoft.AspNetCore.Mvc.Testing;


public sealed class AdminEndpointIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AdminEndpointIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetStats_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/admin/stats");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetUsers_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/admin/users");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetStats_WithAdminAuth_ReturnsStats()
    {
        var client = _factory.CreateClient();

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new
        {
            username = $"admin-{Guid.NewGuid():N}",
            email = $"admin-{Guid.NewGuid():N}@example.com",
            password = "Admin123!",
            role = "Admin"
        });

        Assert.True(registerResponse.IsSuccessStatusCode);

        var cookies = registerResponse.Headers.GetValues("Set-Cookie");
        var authCookie = cookies.FirstOrDefault(c => c.StartsWith("auth_token="));
        Assert.NotNull(authCookie);

        client.DefaultRequestHeaders.Add("Cookie", authCookie);

        // Get stats
        var statsResponse = await client.GetAsync("/api/admin/stats");
        Assert.True(statsResponse.IsSuccessStatusCode);

        var stats = await statsResponse.Content.ReadFromJsonAsync<AdminStatsResponseDto>();
        Assert.NotNull(stats);
        Assert.True(stats.TotalUsers >= 0);
        Assert.True(stats.ActiveUsers >= 0);
        Assert.True(stats.TotalExecutions >= 0);
    }

    [Fact]
    public async Task GetUsers_WithAdminAuth_ReturnsUserList()
    {
        var client = _factory.CreateClient();

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new
        {
            username = $"admin-{Guid.NewGuid():N}",
            email = $"admin-{Guid.NewGuid():N}@example.com",
            password = "Admin123!",
            role = "Admin"
        });

        Assert.True(registerResponse.IsSuccessStatusCode);

        var cookies = registerResponse.Headers.GetValues("Set-Cookie");
        var authCookie = cookies.FirstOrDefault(c => c.StartsWith("auth_token="));
        Assert.NotNull(authCookie);

        client.DefaultRequestHeaders.Add("Cookie", authCookie);

        // Get users
        var usersResponse = await client.GetAsync("/api/admin/users");
        Assert.True(usersResponse.IsSuccessStatusCode);

        var users = await usersResponse.Content.ReadFromJsonAsync<List<AdminUserDto>>();
        Assert.NotNull(users);
        Assert.NotEmpty(users);
        Assert.Contains(users, u => u.Email.Contains("admin-", StringComparison.Ordinal));
    }
}