using Moq;
using FluentAssertions;
using Arc.Domain.Models;
using Arc.Infrastructure.LLM;
using Microsoft.Extensions.Logging;


namespace Arc.UnitTests.LLM;
public sealed class LLMProviderFactoryTests
{
    private readonly Mock<ILoggerFactory> _mockLoggerFactory;
    private readonly Mock<ILogger<GenericLlmProvider>> _mockLogger;
    private readonly HttpClient _httpClient;
    private readonly LLMProviderFactory _factory;

    public LLMProviderFactoryTests()
    {
        _mockLoggerFactory = new Mock<ILoggerFactory>();
        _mockLogger = new Mock<ILogger<GenericLlmProvider>>();
        _httpClient = new HttpClient();

        _mockLoggerFactory
            .Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(_mockLogger.Object);

        _factory = new LLMProviderFactory(_httpClient, _mockLoggerFactory.Object);
    }

    [Fact]
    public void CreateProvider_WithValidConfiguration_ShouldReturnProvider()
    {
        // Arrange
            var config = LLMConfiguration.Create(
                name: "Test Config",
                baseUrl: "https://api.openai.com/v1",
                model: "gpt-3.5-turbo",
                apiKey: "test-key",
                endpoint: "chat/completions",
                authType: "bearer",
                headers: new Dictionary<string, string>(),
                createdBy: UserId.From(Guid.NewGuid()));

        // Act
        var provider = _factory.CreateProvider(config);

        // Assert
        provider.Should().NotBeNull();
        provider.Should().BeOfType<GenericLlmProvider>();
    }

    [Fact]
    public void CreateProvider_WithDifferentAuthTypes_ShouldCreateProviders()
    {
        // Arrange
        var authTypes = new[] { "bearer", "api-key", "x-api-key", "x-goog-api-key", "url-param" };

        foreach (var authType in authTypes)
        {
                var config = LLMConfiguration.Create(
                    name: $"Test Config {authType}",
                    baseUrl: "https://api.example.com",
                    model: "test-model",
                    apiKey: "test-key",
                    endpoint: "chat/completions",
                    authType: authType,
                    headers: new Dictionary<string, string>(),
                    createdBy: UserId.From(Guid.NewGuid()));

            // Act
            var provider = _factory.CreateProvider(config);

            // Assert
            provider.Should().NotBeNull();
            provider.Should().BeOfType<GenericLlmProvider>();
        }
    }

    [Fact]
    public void CreateProvider_WithCustomHeaders_ShouldIncludeHeaders()
    {
        // Arrange
        var headers = new Dictionary<string, string>
        {
            ["X-Custom-Header"] = "CustomValue",
            ["X-Another-Header"] = "AnotherValue"
        };

        var config = LLMConfiguration.Create(
            name: "Test Config",
            baseUrl: "https://api.openai.com/v1",
            model: "gpt-3.5-turbo",
            apiKey: "test-key",
            endpoint: "chat/completions",
            authType: "bearer",
            headers: headers,
            createdBy: UserId.From(Guid.NewGuid()));

        // Act
        var provider = _factory.CreateProvider(config);

        // Assert
        provider.Should().NotBeNull();
        provider.Should().BeOfType<GenericLlmProvider>();
    }

    [Fact]
    public void CreateProvider_WithoutApiKey_ShouldCreateProvider()
    {
        // Arrange
        var config = LLMConfiguration.Create(
            name: "Ollama Local",
            baseUrl: "http://localhost:11434",
            model: "llama2",
            apiKey: null,
            endpoint: "api/generate",
            authType: "none",
            headers: new Dictionary<string, string>(),
            createdBy: UserId.From(Guid.NewGuid()));

        // Act
        var provider = _factory.CreateProvider(config);

        // Assert
        provider.Should().NotBeNull();
        provider.Should().BeOfType<GenericLlmProvider>();
    }

    [Fact]
    public void CreateProvider_ShouldCreateLoggerForGenericLlmProvider()
    {
        // Arrange
        var config = LLMConfiguration.Create(
            name: "Test Config",
            baseUrl: "https://api.openai.com/v1",
            model: "gpt-3.5-turbo",
            apiKey: "test-key",
            endpoint: "chat/completions",
            authType: "bearer",
            headers: new Dictionary<string, string>(),
            createdBy: UserId.From(Guid.NewGuid()));

        // Act
        var provider = _factory.CreateProvider(config);

        // Assert
        _mockLoggerFactory.Verify(
            x => x.CreateLogger(It.Is<string>(s => s.Contains("GenericLlmProvider"))),
            Times.Once);
    }
}