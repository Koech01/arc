using System.Net;
using FluentAssertions;
using System.Net.Http.Json;
using Arc.Api.DTOs.Webhooks; 
namespace Arc.IntegrationTests.Api;
using Microsoft.AspNetCore.Mvc.Testing;


public sealed class WebhookEndpointIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public WebhookEndpointIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task RegisterWebhook_WithValidInput_ReturnsCreatedResponse()
    {
        // Arrange
        var token = await GetAuthToken();
        _client.DefaultRequestHeaders.Remove("Cookie");
        _client.DefaultRequestHeaders.Add("Cookie", token);

        var request = new CreateWebhookRequestDto
        {
            Url = "https://example.com/webhook",
            Events = new List<string> { "execution.completed", "execution.failed" },
            Secret = "this-is-a-super-secret-webhook-key-of-sufficient-length"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/webhooks", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = await response.Content.ReadFromJsonAsync<WebhookResponseDto>();
        content.Should().NotBeNull();
        content!.Url.Should().Be(request.Url);
        content.Events.Should().ContainInOrder(request.Events);
        content.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task RegisterWebhook_WithInvalidUrl_ReturnsBadRequest()
    {
        // Arrange
        var token = await GetAuthToken();
        _client.DefaultRequestHeaders.Remove("Cookie");
        _client.DefaultRequestHeaders.Add("Cookie", token);

        var request = new CreateWebhookRequestDto
        {
            Url = "not-a-valid-url",
            Events = new List<string> { "execution.completed" },
            Secret = "this-is-a-super-secret-webhook-key-of-sufficient-length"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/webhooks", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RegisterWebhook_WithShortSecret_ReturnsBadRequest()
    {
        // Arrange
        var token = await GetAuthToken();
        _client.DefaultRequestHeaders.Remove("Cookie");
        _client.DefaultRequestHeaders.Add("Cookie", token);

        var request = new CreateWebhookRequestDto
        {
            Url = "https://example.com/webhook",
            Events = new List<string> { "execution.completed" },
            Secret = "short" // Too short
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/webhooks", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RegisterWebhook_WithNoEvents_ReturnsBadRequest()
    {
        // Arrange
        var token = await GetAuthToken();
        _client.DefaultRequestHeaders.Remove("Cookie");
        _client.DefaultRequestHeaders.Add("Cookie", token);

        var request = new CreateWebhookRequestDto
        {
            Url = "https://example.com/webhook",
            Events = new List<string>(), // Empty
            Secret = "this-is-a-super-secret-webhook-key-of-sufficient-length"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/webhooks", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RegisterWebhook_WithInvalidEventType_ReturnsBadRequest()
    {
        // Arrange
        var token = await GetAuthToken();
        _client.DefaultRequestHeaders.Remove("Cookie");
        _client.DefaultRequestHeaders.Add("Cookie", token);

        var request = new CreateWebhookRequestDto
        {
            Url = "https://example.com/webhook",
            Events = new List<string> { "invalid.event" },
            Secret = "this-is-a-super-secret-webhook-key-of-sufficient-length"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/webhooks", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ListWebhooks_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/webhooks");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListWebhooks_WithAuth_ReturnsOkWithEmptyList()
    {
        // Arrange
        var token = await GetAuthToken();
        _client.DefaultRequestHeaders.Remove("Cookie");
        _client.DefaultRequestHeaders.Add("Cookie", token);

        // Act
        var response = await _client.GetAsync("/api/webhooks");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<List<WebhookListItemDto>>();
        content.Should().BeEmpty();
    }

    [Fact]
    public async Task ListWebhooks_AfterCreation_ReturnsCreatedWebhook()
    {
        // Arrange
        var token = await GetAuthToken();
        _client.DefaultRequestHeaders.Remove("Cookie");
        _client.DefaultRequestHeaders.Add("Cookie", token);

        var createRequest = new CreateWebhookRequestDto
        {
            Url = "https://example.com/webhook",
            Events = new List<string> { "execution.completed" },
            Secret = "this-is-a-super-secret-webhook-key-of-sufficient-length"
        };

        await _client.PostAsJsonAsync("/api/webhooks", createRequest);

        // Act
        var response = await _client.GetAsync("/api/webhooks");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<List<WebhookListItemDto>>();
        content.Should().HaveCount(1);
        content![0].Url.Should().Be(createRequest.Url);
    }

    [Fact]
    public async Task DeleteWebhook_WithValidId_ReturnsNoContent()
    {
        // Arrange
        var token = await GetAuthToken();
        _client.DefaultRequestHeaders.Remove("Cookie");
        _client.DefaultRequestHeaders.Add("Cookie", token);

        var createRequest = new CreateWebhookRequestDto
        {
            Url = "https://example.com/webhook",
            Events = new List<string> { "execution.completed" },
            Secret = "this-is-a-super-secret-webhook-key-of-sufficient-length"
        };

        var createResponse = await _client.PostAsJsonAsync("/api/webhooks", createRequest);
        var createdWebhook = await createResponse.Content.ReadFromJsonAsync<WebhookResponseDto>();
        var webhookId = createdWebhook!.Id;

        // Act
        var response = await _client.DeleteAsync($"/api/webhooks/{webhookId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteWebhook_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var token = await GetAuthToken();
        _client.DefaultRequestHeaders.Remove("Cookie");
        _client.DefaultRequestHeaders.Add("Cookie", token);

        // Act
        var response = await _client.DeleteAsync($"/api/webhooks/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetWebhook_WithValidId_ReturnsWebhook()
    {
        // Arrange
        var token = await GetAuthToken();
        _client.DefaultRequestHeaders.Remove("Cookie");
        _client.DefaultRequestHeaders.Add("Cookie", token);

        var createRequest = new CreateWebhookRequestDto
        {
            Url = "https://example.com/webhook",
            Events = new List<string> { "execution.completed", "execution.failed" },
            Secret = "this-is-a-super-secret-webhook-key-of-sufficient-length"
        };

        var createResponse = await _client.PostAsJsonAsync("/api/webhooks", createRequest);
        var createdWebhook = await createResponse.Content.ReadFromJsonAsync<WebhookResponseDto>();
        var webhookId = createdWebhook!.Id;

        // Act
        var response = await _client.GetAsync($"/api/webhooks/{webhookId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<WebhookResponseDto>();
        content.Should().NotBeNull();
        content!.Id.Should().Be(webhookId);
        content.Url.Should().Be(createRequest.Url);
    }

    [Fact]
    public async Task WebhookEndpoints_WhenNotAuthenticated_ReturnUnauthorized()
    {
        // Act
        var getResponse = await _client.GetAsync("/api/webhooks");
        var postResponse = await _client.PostAsJsonAsync("/api/webhooks", new CreateWebhookRequestDto());
        var deleteResponse = await _client.DeleteAsync($"/api/webhooks/{Guid.NewGuid()}");

        // Assert
        getResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        postResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetWebhook_WithInvalidGuid_ReturnsBadRequest()
    {
        // Arrange
        var token = await GetAuthToken();
        _client.DefaultRequestHeaders.Remove("Cookie");
        _client.DefaultRequestHeaders.Add("Cookie", token);

        // Act
        var response = await _client.GetAsync("/api/webhooks/not-a-guid");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task<string> GetAuthToken()
    {
        // Register a user
        var registerRequest = new
        {
            username = $"webhook-{Guid.NewGuid():N}",
            email = $"webhook-test-{Guid.NewGuid()}@example.com",
            password = "SecurePassword123!"
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        
        if (!registerResponse.IsSuccessStatusCode)
        {
            throw new Exception("Failed to register user for webhook tests");
        }

        // Login to get token
        var loginRequest = new
        {
            email = registerRequest.email,
            password = registerRequest.password
        };

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        
        if (!loginResponse.IsSuccessStatusCode)
        {
            throw new Exception("Failed to login for webhook tests");
        }

        var authCookie = loginResponse.Headers
            .GetValues("Set-Cookie")
            .FirstOrDefault(c => c.StartsWith("auth_token="));

        if (string.IsNullOrWhiteSpace(authCookie))
        {
            throw new InvalidOperationException("Authentication cookie was not issued.");
        }

        return authCookie.Split(';')[0];
    }
}