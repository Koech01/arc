using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Arc.Api.DTOs.Auth;
namespace Arc.IntegrationTests.Api;
using Microsoft.AspNetCore.Mvc.Testing;


public sealed class AuthEndpointIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AuthEndpointIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Register_WithValidData_ShouldReturnSuccessAndToken()
    {
        // Arrange
        var email = $"test-{Guid.NewGuid():N}@example.com";
        var request = new RegisterRequestDto
        {
            Username = "testuser",
            Email = email,
            Password = "Password123",
            Role = "User"
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/auth/register", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var authResponse = JsonSerializer.Deserialize<AuthResponseDto>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        authResponse.Should().NotBeNull();
        authResponse!.User.Should().NotBeNull();
        authResponse.User.Email.Should().Be(email);
        authResponse.User.Role.Should().Be("User");
        authResponse.User.IsActive.Should().BeTrue();
        
        // Verify auth cookie is set
        response.Headers.TryGetValues("Set-Cookie", out var cookies);
        cookies.Should().NotBeNull();
        cookies!.Any(c => c.Contains("auth_token")).Should().BeTrue();
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new RegisterRequestDto
        {
            Username = "duplicate",
            Email = "duplicate@example.com",
            Password = "Password123",
            Role = "User"
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Register first user
        await _client.PostAsync("/api/auth/register", content);

        // Act - Try to register again with same email
        var response = await _client.PostAsync("/api/auth/register", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithInvalidEmail_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new RegisterRequestDto
        {
            Username = "testuser",
            Email = "invalid-email",
            Password = "Password123",
            Role = "User"
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/auth/register", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithWeakPassword_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new RegisterRequestDto
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "weak",
            Role = "User"
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/auth/register", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnSuccessAndToken()
    {
        // Arrange - First register a user
        var registerRequest = new RegisterRequestDto
        {
            Username = "loginuser",
            Email = "login@example.com",
            Password = "Password123",
            Role = "User"
        };

        var registerJson = JsonSerializer.Serialize(registerRequest);
        var registerContent = new StringContent(registerJson, Encoding.UTF8, "application/json");
        await _client.PostAsync("/api/auth/register", registerContent);

        // Arrange - Login request
        var loginRequest = new LoginRequestDto
        {
            Email = "login@example.com",
            Password = "Password123"
        };

        var loginJson = JsonSerializer.Serialize(loginRequest);
        var loginContent = new StringContent(loginJson, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/auth/login", loginContent);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var authResponse = JsonSerializer.Deserialize<AuthResponseDto>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        authResponse.Should().NotBeNull();
        authResponse!.User.Should().NotBeNull();
        authResponse.User.Email.Should().Be("login@example.com");
        
        // Verify auth cookie is set
        response.Headers.TryGetValues("Set-Cookie", out var cookies);
        cookies.Should().NotBeNull();
        cookies!.Any(c => c.Contains("auth_token")).Should().BeTrue();
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ShouldReturnUnauthorized()
    {
        // Arrange
        var loginRequest = new LoginRequestDto
        {
            Email = "nonexistent@example.com",
            Password = "WrongPassword"
        };

        var json = JsonSerializer.Serialize(loginRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/auth/login", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCurrentUser_WithValidToken_ShouldReturnUserInfo()
    {
        // Arrange - Register and login to get token
        var uniqueEmail = $"me-{Guid.NewGuid():N}@example.com";
        var registerRequest = new RegisterRequestDto
        {
            Username = $"meuser-{Guid.NewGuid():N}",
            Email = uniqueEmail,
            Password = "Password123",
            Role = "Admin"
        };

        var registerJson = JsonSerializer.Serialize(registerRequest);
        var registerContent = new StringContent(registerJson, Encoding.UTF8, "application/json");
        var registerResponse = await _client.PostAsync("/api/auth/register", registerContent);

        var registerResponseContent = await registerResponse.Content.ReadAsStringAsync();
        var authResponse = JsonSerializer.Deserialize<AuthResponseDto>(registerResponseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Extract cookie from response
        registerResponse.Headers.TryGetValues("Set-Cookie", out var cookies);
        var authCookie = cookies?.FirstOrDefault(c => c.Contains("auth_token"));
        authCookie.Should().NotBeNull();

        _client.DefaultRequestHeaders.Remove("Cookie");
        _client.DefaultRequestHeaders.Add("Cookie", authCookie!.Split(';')[0]);

        // Act
        var response = await _client.GetAsync("/api/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var userDto = JsonSerializer.Deserialize<UserDto>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        userDto.Should().NotBeNull();
        userDto!.Email.Should().Be(uniqueEmail);
        userDto.Role.Should().Be("Admin");
        userDto.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetCurrentUser_WithoutToken_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCurrentUser_WithInvalidToken_ShouldReturnUnauthorized()
    {
        // Arrange
        _client.DefaultRequestHeaders.Remove("Cookie");
        _client.DefaultRequestHeaders.Add("Cookie", "auth_token=invalid_token");

        // Act
        var response = await _client.GetAsync("/api/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AuthenticationFlow_ShouldBeDeterministic()
    {
        // Arrange
        var deterministicEmail = $"deterministic-{Guid.NewGuid():N}@example.com";
        var request = new RegisterRequestDto
        {
            Username = "detuser",
            Email = deterministicEmail,
            Password = "Password123",
            Role = "User"
        };

        var json = JsonSerializer.Serialize(request);
        var content1 = new StringContent(json, Encoding.UTF8, "application/json");
        var content2 = new StringContent(json, Encoding.UTF8, "application/json");

        // Act - Register same user twice (should fail on second attempt)
        var response1 = await _client.PostAsync("/api/auth/register", content1);
        var response2 = await _client.PostAsync("/api/auth/register", content2);

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Verify login works consistently
        var loginRequest = new LoginRequestDto
        {
            Email = deterministicEmail,
            Password = "Password123"
        };

        var loginJson = JsonSerializer.Serialize(loginRequest);
        var loginContent1 = new StringContent(loginJson, Encoding.UTF8, "application/json");
        var loginContent2 = new StringContent(loginJson, Encoding.UTF8, "application/json");

        var loginResponse1 = await _client.PostAsync("/api/auth/login", loginContent1);
        var loginResponse2 = await _client.PostAsync("/api/auth/login", loginContent2);

        loginResponse1.StatusCode.Should().Be(HttpStatusCode.OK);
        loginResponse2.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}