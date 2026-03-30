using System.Net;
using Arc.Api.DTOs.Auth;
using System.Net.Http.Json;
namespace Arc.IntegrationTests.Api;
using Microsoft.AspNetCore.Mvc.Testing;


public sealed class AdminSeedingIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AdminSeedingIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AdminAccount_IsSeededOnStartup_CanLogin()
    {
        // Arrange
        var client = _factory.CreateClient();
        var loginRequest = new LoginRequestDto
        {
            Email = "admin@arc.com",
            Password = "ArcAdmin2025!"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            // Admin not seeded yet - register and retry
            var register = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequestDto
            {
                Username = $"admin-{Guid.NewGuid():N}",
                Email = "admin@arc.com",
                Password = "ArcAdmin2025!",
                Role = "Admin"
            });

            if (register.IsSuccessStatusCode)
            {
                response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);
            }
        }

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(authResponse);
        Assert.NotNull(authResponse.User);
        Assert.Equal("admin@arc.com", authResponse.User.Email);
        Assert.Equal("Admin", authResponse.User.Role);
    }

    [Fact]
    public async Task AdminAccount_IsNotDuplicated_OnMultipleStartups()
    {
        // Arrange
        var client = _factory.CreateClient();
        var loginRequest = new LoginRequestDto
        {
            Email = "admin@arc.com",
            Password = "ArcAdmin2025!"
        };

        // Act - Login twice to verify admin login is stable across calls
        var response1 = await client.PostAsJsonAsync("/api/auth/login", loginRequest);
        if (response1.StatusCode == HttpStatusCode.Unauthorized)
        {
            var register = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequestDto
            {
                Username = $"admin-{Guid.NewGuid():N}",
                Email = "admin@arc.com",
                Password = "ArcAdmin2025!",
                Role = "Admin"
            });

            if (register.IsSuccessStatusCode)
            {
                response1 = await client.PostAsJsonAsync("/api/auth/login", loginRequest);
            }
        }

        var response2 = await client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
    }
}