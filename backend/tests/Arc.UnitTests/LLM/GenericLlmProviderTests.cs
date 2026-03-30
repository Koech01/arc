using Moq;
using System.Net;
using Moq.Protected;
using System.Text.Json;
using FluentAssertions;
namespace Arc.UnitTests.LLM;
using Arc.Infrastructure.LLM;
using Microsoft.Extensions.Logging;


public sealed class GenericLlmProviderTests
{
    private readonly Mock<ILogger<GenericLlmProvider>> _mockLogger;
    private readonly Mock<HttpMessageHandler> _mockHttpHandler;
    private readonly HttpClient _httpClient;

    public GenericLlmProviderTests()
    {
        _mockLogger = new Mock<ILogger<GenericLlmProvider>>();
        _mockHttpHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpHandler.Object);
    }

    [Fact]
    public async Task GenerateTextAsync_WithOpenAIFormat_ShouldReturnText()
    {
        // Arrange
        var responseJson = JsonSerializer.Serialize(new
        {
            choices = new[]
            {
                new
                {
                    message = new { content = "Generated response" }
                }
            }
        });

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseJson)
            });

        var provider = new GenericLlmProvider(
            _httpClient,
            "https://api.openai.com/v1",
            "gpt-3.5-turbo",
            "test-key",
            "chat/completions",
            "bearer",
            new Dictionary<string, string>(),
            _mockLogger.Object);

        // Act
        var result = await provider.GenerateTextAsync("Test prompt");

        // Assert
        result.Should().Be("Generated response");
    }

    [Fact]
    public async Task GenerateTextAsync_WithGeminiFormat_ShouldReturnText()
    {
        // Arrange
        var responseJson = JsonSerializer.Serialize(new
        {
            candidates = new[]
            {
                new
                {
                    content = new
                    {
                        parts = new[] { new { text = "Gemini response" } }
                    }
                }
            }
        });

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseJson)
            });

        var provider = new GenericLlmProvider(
            _httpClient,
            "https://generativelanguage.googleapis.com/v1",
            "gemini-pro",
            "test-key",
            "models/gemini-pro:generateContent",
            "x-goog-api-key",
            new Dictionary<string, string>(),
            _mockLogger.Object);

        // Act
        var result = await provider.GenerateTextAsync("Test prompt");

        // Assert
        result.Should().Be("Gemini response");
    }

    [Fact]
    public async Task GenerateTextAsync_WithOllamaFormat_ShouldReturnText()
    {
        // Arrange
        var responseJson = JsonSerializer.Serialize(new
        {
            response = "Ollama response"
        });

        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseJson)
            });

        var provider = new GenericLlmProvider(
            _httpClient,
            "http://localhost:11434",
            "llama2",
            null,
            "api/generate",
            "bearer",
            new Dictionary<string, string>(),
            _mockLogger.Object);

        // Act
        var result = await provider.GenerateTextAsync("Test prompt");

        // Assert
        result.Should().Be("Ollama response");
    }

    [Fact]
    public async Task GenerateTextAsync_WithNullPrompt_ShouldThrowArgumentNullException()
    {
        // Arrange
        var provider = new GenericLlmProvider(
            _httpClient,
            "https://api.openai.com/v1",
            "gpt-3.5-turbo",
            "test-key",
            "chat/completions",
            "bearer",
            new Dictionary<string, string>(),
            _mockLogger.Object);

        // Act & Assert
        var act = async () => await provider.GenerateTextAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GenerateTextAsync_WithHttpError_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("Error occurred")
            });

        var provider = new GenericLlmProvider(
            _httpClient,
            "https://api.openai.com/v1",
            "gpt-3.5-turbo",
            "test-key",
            "chat/completions",
            "bearer",
            new Dictionary<string, string>(),
            _mockLogger.Object);

        // Act & Assert
        var act = async () => await provider.GenerateTextAsync("Test prompt");
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GenerateTextAsync_WithCustomHeaders_ShouldIncludeHeaders()
    {
        // Arrange
        var responseJson = JsonSerializer.Serialize(new
        {
            choices = new[]
            {
                new { message = new { content = "Response" } }
            }
        });

        HttpRequestMessage? capturedRequest = null;
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, ct) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseJson)
            });

        var customHeaders = new Dictionary<string, string>
        {
            ["X-Custom-Header"] = "CustomValue"
        };

        var provider = new GenericLlmProvider(
            _httpClient,
            "https://api.openai.com/v1",
            "gpt-3.5-turbo",
            "test-key",
            "chat/completions",
            "bearer",
            customHeaders,
            _mockLogger.Object);

        // Act
        await provider.GenerateTextAsync("Test prompt");

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Headers.Should().Contain(h => h.Key == "X-Custom-Header");
    }

    [Fact]
    public async Task GenerateTextAsync_WithBearerAuth_ShouldIncludeAuthorizationHeader()
    {
        // Arrange
        var responseJson = JsonSerializer.Serialize(new
        {
            choices = new[]
            {
                new { message = new { content = "Response" } }
            }
        });

        HttpRequestMessage? capturedRequest = null;
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, ct) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseJson)
            });

        var provider = new GenericLlmProvider(
            _httpClient,
            "https://api.openai.com/v1",
            "gpt-3.5-turbo",
            "test-key",
            "chat/completions",
            "bearer",
            new Dictionary<string, string>(),
            _mockLogger.Object);

        // Act
        await provider.GenerateTextAsync("Test prompt");

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Headers.Authorization.Should().NotBeNull();
        capturedRequest.Headers.Authorization!.Scheme.Should().Be("Bearer");
        capturedRequest.Headers.Authorization.Parameter.Should().Be("test-key");
    }

    [Fact]
    public async Task GenerateTextAsync_WithApiKeyAuth_ShouldIncludeApiKeyHeader()
    {
        // Arrange
        var responseJson = JsonSerializer.Serialize(new
        {
            choices = new[]
            {
                new { message = new { content = "Response" } }
            }
        });

        HttpRequestMessage? capturedRequest = null;
        _mockHttpHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, ct) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseJson)
            });

        var provider = new GenericLlmProvider(
            _httpClient,
            "https://api.azure.com",
            "gpt-4",
            "test-key",
            "chat/completions",
            "api-key",
            new Dictionary<string, string>(),
            _mockLogger.Object);

        // Act
        await provider.GenerateTextAsync("Test prompt");

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Headers.Should().Contain(h => h.Key == "api-key");
    }

    [Fact]
    public void Constructor_WithNullHttpClient_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new GenericLlmProvider(
            null!,
            "https://api.openai.com",
            "gpt-3.5-turbo",
            "test-key",
            "chat/completions",
            "bearer",
            new Dictionary<string, string>(),
            _mockLogger.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("httpClient");
    }

    [Fact]
    public void Constructor_WithNullBaseUrl_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new GenericLlmProvider(
            _httpClient,
            null!,
            "gpt-3.5-turbo",
            "test-key",
            "chat/completions",
            "bearer",
            new Dictionary<string, string>(),
            _mockLogger.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("baseUrl");
    }

    [Fact]
    public void Constructor_WithNullModel_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new GenericLlmProvider(
            _httpClient,
            "https://api.openai.com",
            null!,
            "test-key",
            "chat/completions",
            "bearer",
            new Dictionary<string, string>(),
            _mockLogger.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("model");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new GenericLlmProvider(
            _httpClient,
            "https://api.openai.com",
            "gpt-3.5-turbo",
            "test-key",
            "chat/completions",
            "bearer",
            new Dictionary<string, string>(),
            null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }
}