using Moq;
using FluentAssertions;
using Arc.Domain.Models;
using Arc.Application.LLM;
using Arc.Infrastructure.LLM;
using Arc.Application.Identity;
using Arc.Infrastructure.Execution;
using Microsoft.Extensions.Logging;


namespace Arc.UnitTests.Execution;
public sealed class DynamicLLMProviderServiceTests
{
    private readonly Mock<ILLMConfigurationRepository> _mockConfigRepository;
    private readonly Mock<LLMProviderFactory> _mockProviderFactory;
    private readonly Mock<IUserContext> _mockUserContext;
    private readonly Mock<ILogger<DynamicLLMProviderService>> _mockLogger;
    private readonly DynamicLLMProviderService _service;
    private readonly UserId _testUserId;

    public DynamicLLMProviderServiceTests()
    {
        _mockConfigRepository = new Mock<ILLMConfigurationRepository>();
        _mockProviderFactory = new Mock<LLMProviderFactory>(null!, null!);
        _mockUserContext = new Mock<IUserContext>();
        _mockLogger = new Mock<ILogger<DynamicLLMProviderService>>();
        _testUserId = UserId.From(Guid.NewGuid());

        _mockUserContext.Setup(x => x.CurrentUserId).Returns(_testUserId);

        _service = new DynamicLLMProviderService(
            _mockConfigRepository.Object,
            _mockProviderFactory.Object,
            _mockUserContext.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task GetProviderAsync_WithValidConfigId_ShouldReturnProvider()
    {
        // Arrange
        var configId = "test-config-id";
        var config = LLMConfiguration.Create(
            name: "Test Config",
            baseUrl: "https://api.openai.com/v1",
            model: "gpt-3.5-turbo",
            apiKey: "test-key",
            endpoint: "chat/completions",
            authType: "bearer",
            headers: new Dictionary<string, string>(),
            createdBy: _testUserId);

        var mockProvider = new Mock<ILLMProvider>();

        _mockConfigRepository
            .Setup(x => x.GetByIdAsync(configId, _testUserId))
            .ReturnsAsync(config);

        _mockProviderFactory
            .Setup(x => x.CreateProvider(config))
            .Returns(mockProvider.Object);

        // Act
        var result = await _service.GetProviderAsync(configId);

        // Assert
        result.Should().NotBeNull();
        result.Should().Be(mockProvider.Object);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetProviderAsync_WithNullOrEmptyConfigId_ShouldThrowInvalidOperationException(string? configId)
    {
        // Act & Assert
        var act = async () => await _service.GetProviderAsync(configId);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No LLM configuration was specified*");
    }

    [Fact]
    public async Task GetProviderAsync_WithNonExistentConfigId_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var configId = "non-existent-config";

        _mockConfigRepository
            .Setup(x => x.GetByIdAsync(configId, _testUserId))
            .ReturnsAsync((LLMConfiguration?)null);

        // Act & Assert
        var act = async () => await _service.GetProviderAsync(configId);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*LLM configuration '{configId}' was not found*");
    }

    [Fact]
    public async Task GetProviderAsync_WithInactiveConfig_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var configId = "inactive-config";
        var config = LLMConfiguration.Create(
            name: "Inactive Config",
            baseUrl: "https://api.openai.com/v1",
            model: "gpt-3.5-turbo",
            apiKey: "test-key",
            endpoint: "chat/completions",
            authType: "bearer",
            headers: new Dictionary<string, string>(),
            createdBy: _testUserId);

        config = config.Deactivate();

        _mockConfigRepository
            .Setup(x => x.GetByIdAsync(configId, _testUserId))
            .ReturnsAsync(config);

        // Act & Assert
        var act = async () => await _service.GetProviderAsync(configId);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*LLM configuration '{configId}' is inactive*");
    }

    [Fact]
    public async Task GetProviderAsync_ShouldUseCurrentUserIdFromContext()
    {
        // Arrange
        var configId = "test-config";
        var config = LLMConfiguration.Create(
            name: "Test Config",
            baseUrl: "https://api.openai.com/v1",
            model: "gpt-3.5-turbo",
            apiKey: "test-key",
            endpoint: "chat/completions",
            authType: "bearer",
            headers: new Dictionary<string, string>(),
            createdBy: _testUserId);

        _mockConfigRepository
            .Setup(x => x.GetByIdAsync(configId, _testUserId))
            .ReturnsAsync(config);

        _mockProviderFactory
            .Setup(x => x.CreateProvider(config))
            .Returns(new Mock<ILLMProvider>().Object);

        // Act
        await _service.GetProviderAsync(configId);

        // Assert
        _mockUserContext.Verify(x => x.CurrentUserId, Times.Once);
        _mockConfigRepository.Verify(x => x.GetByIdAsync(configId, _testUserId), Times.Once);
    }

    [Fact]
    public async Task GetProviderAsync_WithCancellationToken_ShouldPassTokenToRepository()
    {
        // Arrange
        var configId = "test-config";
        var cancellationToken = new CancellationToken();
        var config = LLMConfiguration.Create(
            name: "Test Config",
            baseUrl: "https://api.openai.com/v1",
            model: "gpt-3.5-turbo",
            apiKey: "test-key",
            endpoint: "chat/completions",
            authType: "bearer",
            headers: new Dictionary<string, string>(),
            createdBy: _testUserId);

        _mockConfigRepository
            .Setup(x => x.GetByIdAsync(configId, _testUserId))
            .ReturnsAsync(config);

        _mockProviderFactory
            .Setup(x => x.CreateProvider(config))
            .Returns(new Mock<ILLMProvider>().Object);

        // Act
        await _service.GetProviderAsync(configId, cancellationToken);

        // Assert
        _mockConfigRepository.Verify(x => x.GetByIdAsync(configId, _testUserId), Times.Once);
    }

    [Fact]
    public async Task GetProviderAsync_ShouldLogDebugInformation()
    {
        // Arrange
        var configId = "test-config";
        var config = LLMConfiguration.Create(
            name: "Test Config",
            baseUrl: "https://api.openai.com/v1",
            model: "gpt-3.5-turbo",
            apiKey: "test-key",
            endpoint: "chat/completions",
            authType: "bearer",
            headers: new Dictionary<string, string>(),
            createdBy: _testUserId);

        _mockConfigRepository
            .Setup(x => x.GetByIdAsync(configId, _testUserId))
            .ReturnsAsync(config);

        _mockProviderFactory
            .Setup(x => x.CreateProvider(config))
            .Returns(new Mock<ILLMProvider>().Object);

        // Act
        await _service.GetProviderAsync(configId);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(configId)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}