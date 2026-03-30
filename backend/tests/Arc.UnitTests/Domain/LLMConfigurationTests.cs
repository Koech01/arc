using FluentAssertions;
using Arc.Domain.Models;
namespace Arc.UnitTests.Domain;


public sealed class LLMConfigurationTests
{
    private readonly UserId _testUserId = new(Guid.NewGuid());

    [Fact]
    public void Create_WithValidParameters_ShouldCreateConfiguration()
    {
        var config = LLMConfiguration.Create(
            "OpenAI GPT-4",
            "https://api.openai.com/v1",
            "gpt-4",
            "sk-test-key",
            null,
            null,
            null,
            _testUserId);

        config.Id.Should().NotBeNullOrEmpty();
        config.Name.Should().Be("OpenAI GPT-4");
        config.BaseUrl.Should().Be("https://api.openai.com/v1");
        config.Model.Should().Be("gpt-4");
        config.ApiKey.Should().Be("sk-test-key");
        config.Endpoint.Should().Be("chat/completions");
        config.AuthType.Should().Be("bearer");
        config.Headers.Should().NotBeNull().And.BeEmpty();
        config.CreatedBy.Should().Be(_testUserId);
        config.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_WithCustomEndpointAndAuthType_ShouldUseProvidedValues()
    {
        var headers = new Dictionary<string, string> { { "X-Custom-Header", "value" } };

        var config = LLMConfiguration.Create(
            "Custom LLM",
            "https://custom-llm.com",
            "model-name",
            "api-key",
            "v1/completions",
            "apikey",
            headers,
            _testUserId);

        config.Endpoint.Should().Be("v1/completions");
        config.AuthType.Should().Be("apikey");
        config.Headers.Should().ContainKey("X-Custom-Header");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WithEmptyName_ShouldThrowException(string name)
    {
        var act = () => LLMConfiguration.Create(
            name,
            "https://api.openai.com",
            "gpt-4",
            "api-key",
            null,
            null,
            null,
            _testUserId);

        act.Should().Throw<ArgumentException>()
           .WithParameterName("name");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WithEmptyBaseUrl_ShouldThrowException(string baseUrl)
    {
        var act = () => LLMConfiguration.Create(
            "OpenAI GPT-4",
            baseUrl,
            "gpt-4",
            "api-key",
            null,
            null,
            null,
            _testUserId);

        act.Should().Throw<ArgumentException>()
           .WithParameterName("baseUrl");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WithEmptyModel_ShouldThrowException(string model)
    {
        var act = () => LLMConfiguration.Create(
            "OpenAI GPT-4",
            "https://api.openai.com",
            model,
            "api-key",
            null,
            null,
            null,
            _testUserId);

        act.Should().Throw<ArgumentException>()
           .WithParameterName("model");
    }

    [Fact]
    public void Create_WithNullApiKey_ShouldSucceed()
    {
        var config = LLMConfiguration.Create(
            "Custom LLM",
            "https://custom-llm.com",
            "model-name",
            null,
            null,
            null,
            null,
            _testUserId);

        config.ApiKey.Should().BeNull();
    }

    [Fact]
    public void Deactivate_ShouldReturnNewConfigurationMarkedInactive()
    {
        var config = LLMConfiguration.Create(
            "OpenAI GPT-4",
            "https://api.openai.com/v1",
            "gpt-4",
            "sk-test-key",
            null,
            null,
            null,
            _testUserId);

        var deactivated = config.Deactivate();

        deactivated.IsActive.Should().BeFalse();
        deactivated.Id.Should().Be(config.Id);
        deactivated.Name.Should().Be(config.Name);
        deactivated.BaseUrl.Should().Be(config.BaseUrl);
        deactivated.Model.Should().Be(config.Model);
        deactivated.ApiKey.Should().Be(config.ApiKey);
        deactivated.CreatedBy.Should().Be(config.CreatedBy);
        deactivated.CreatedAt.Should().Be(config.CreatedAt);
    }

    [Fact]
    public void WithUpdates_WithNewName_ShouldUpdateName()
    {
        var config = LLMConfiguration.Create(
            "OpenAI GPT-4",
            "https://api.openai.com/v1",
            "gpt-4",
            "sk-test-key",
            null,
            null,
            null,
            _testUserId);

        var updated = config.WithUpdates(name: "OpenAI GPT-4 Turbo");

        updated.Name.Should().Be("OpenAI GPT-4 Turbo");
        updated.Id.Should().Be(config.Id);
        updated.BaseUrl.Should().Be(config.BaseUrl);
        updated.Model.Should().Be(config.Model);
    }

    [Fact]
    public void WithUpdates_WithNewApiKey_ShouldUpdateApiKey()
    {
        var config = LLMConfiguration.Create(
            "OpenAI GPT-4",
            "https://api.openai.com/v1",
            "gpt-4",
            "sk-old-key",
            null,
            null,
            null,
            _testUserId);

        var updated = config.WithUpdates(apiKey: "sk-new-key");

        updated.ApiKey.Should().Be("sk-new-key");
    }

    [Fact]
    public void WithUpdates_WithNullApiKey_ShouldPreserveExistingApiKey()
    {
        var config = LLMConfiguration.Create(
            "OpenAI GPT-4",
            "https://api.openai.com/v1",
            "gpt-4",
            "sk-existing-key",
            null,
            null,
            null,
            _testUserId);

        var updated = config.WithUpdates(apiKey: null);

        updated.ApiKey.Should().Be("sk-existing-key");
    }

    [Fact]
    public void WithUpdates_WithEmptyApiKey_ShouldPreserveExistingApiKey()
    {
        var config = LLMConfiguration.Create(
            "OpenAI GPT-4",
            "https://api.openai.com/v1",
            "gpt-4",
            "sk-existing-key",
            null,
            null,
            null,
            _testUserId);

        var updated = config.WithUpdates(apiKey: "");

        updated.ApiKey.Should().Be("sk-existing-key");
    }

    [Fact]
    public void WithUpdates_WithMultipleUpdates_ShouldUpdateAllProvidedFields()
    {
        var config = LLMConfiguration.Create(
            "OpenAI GPT-4",
            "https://api.openai.com/v1",
            "gpt-4",
            "sk-test-key",
            null,
            null,
            null,
            _testUserId);

        var newHeaders = new Dictionary<string, string> { { "X-Custom", "value" } };

        var updated = config.WithUpdates(
            name: "Updated Name",
            baseUrl: "https://new-url.com",
            model: "gpt-4-turbo",
            endpoint: "v2/chat",
            authType: "apikey",
            headers: newHeaders);

        updated.Name.Should().Be("Updated Name");
        updated.BaseUrl.Should().Be("https://new-url.com");
        updated.Model.Should().Be("gpt-4-turbo");
        updated.Endpoint.Should().Be("v2/chat");
        updated.AuthType.Should().Be("apikey");
        updated.Headers.Should().ContainKey("X-Custom");
        updated.Id.Should().Be(config.Id);
        updated.CreatedBy.Should().Be(config.CreatedBy);
        updated.CreatedAt.Should().Be(config.CreatedAt);
    }

    [Fact]
    public void WithUpdates_WithNoUpdates_ShouldPreserveAllFields()
    {
        var config = LLMConfiguration.Create(
            "OpenAI GPT-4",
            "https://api.openai.com/v1",
            "gpt-4",
            "sk-test-key",
            null,
            null,
            null,
            _testUserId);

        var updated = config.WithUpdates();

        updated.Name.Should().Be(config.Name);
        updated.BaseUrl.Should().Be(config.BaseUrl);
        updated.Model.Should().Be(config.Model);
        updated.ApiKey.Should().Be(config.ApiKey);
        updated.Endpoint.Should().Be(config.Endpoint);
        updated.AuthType.Should().Be(config.AuthType);
    }
}