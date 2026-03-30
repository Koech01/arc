using FluentAssertions;
using Arc.Api.DTOs.Webhooks;
using Arc.Api.Validators.Webhooks;
namespace Arc.UnitTests.Validators;


public sealed class CreateWebhookRequestDtoValidatorTests
{
    private readonly CreateWebhookRequestDtoValidator _validator = new();

    [Fact]
    public void Validate_WithValidData_ShouldPass()
    {
        var request = new CreateWebhookRequestDto
        {
            Url = "https://example.com/webhook",
            Events = new List<string> { "execution.started", "execution.completed" },
            Secret = "this-is-a-very-long-secret-key-for-webhook"
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_WithEmptyUrl_ShouldFail(string url)
    {
        var request = new CreateWebhookRequestDto
        {
            Url = url,
            Events = new List<string> { "execution.completed" },
            Secret = "this-is-a-very-long-secret-key-for-webhook"
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Url" && 
                                            e.ErrorMessage == "Webhook URL is required");
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("ftp://example.com")]
    [InlineData("invalid://url")]
    public void Validate_WithInvalidUrl_ShouldFail(string url)
    {
        var request = new CreateWebhookRequestDto
        {
            Url = url,
            Events = new List<string> { "execution.completed" },
            Secret = "this-is-a-very-long-secret-key-for-webhook"
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Url" && 
                                            e.ErrorMessage == "Webhook URL must be a valid HTTP/HTTPS URL");
    }

    [Theory]
    [InlineData("http://example.com/webhook")]
    [InlineData("https://example.com/webhook")]
    public void Validate_WithValidHttpUrls_ShouldPass(string url)
    {
        var request = new CreateWebhookRequestDto
        {
            Url = url,
            Events = new List<string> { "execution.completed" },
            Secret = "this-is-a-very-long-secret-key-for-webhook"
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyEvents_ShouldFail()
    {
        var request = new CreateWebhookRequestDto
        {
            Url = "https://example.com/webhook",
            Events = new List<string>(),
            Secret = "this-is-a-very-long-secret-key-for-webhook"
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Events" && 
                                            e.ErrorMessage == "At least one event type must be selected");
    }

    [Fact]
    public void Validate_WithInvalidEventType_ShouldFail()
    {
        var request = new CreateWebhookRequestDto
        {
            Url = "https://example.com/webhook",
            Events = new List<string> { "execution.completed", "invalid.event" },
            Secret = "this-is-a-very-long-secret-key-for-webhook"
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Events" && 
                                            e.ErrorMessage == "All event types must be one of: execution.started, execution.completed, execution.failed");
    }

    [Fact]
    public void Validate_WithValidEventTypes_ShouldPass()
    {
        var request = new CreateWebhookRequestDto
        {
            Url = "https://example.com/webhook",
            Events = new List<string> { "execution.started", "execution.completed", "execution.failed" },
            Secret = "this-is-a-very-long-secret-key-for-webhook"
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_WithEmptySecret_ShouldFail(string secret)
    {
        var request = new CreateWebhookRequestDto
        {
            Url = "https://example.com/webhook",
            Events = new List<string> { "execution.completed" },
            Secret = secret
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Secret" && 
                                            e.ErrorMessage == "Webhook secret is required");
    }

    [Fact]
    public void Validate_WithSecretTooShort_ShouldFail()
    {
        var request = new CreateWebhookRequestDto
        {
            Url = "https://example.com/webhook",
            Events = new List<string> { "execution.completed" },
            Secret = "short"
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Secret" && 
                                            e.ErrorMessage == "Webhook secret must be at least 20 characters");
    }
}

public sealed class UpdateWebhookRequestDtoValidatorTests
{
    private readonly UpdateWebhookRequestDtoValidator _validator = new();

    [Fact]
    public void Validate_WithValidData_ShouldPass()
    {
        var request = new UpdateWebhookRequestDto
        {
            Url = "https://example.com/webhook-updated",
            Events = new List<string> { "execution.completed" },
            Secret = null
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithValidSecretUpdate_ShouldPass()
    {
        var request = new UpdateWebhookRequestDto
        {
            Url = "https://example.com/webhook-updated",
            Events = new List<string> { "execution.completed" },
            Secret = "new-very-long-secret-key-for-webhook"
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_WithEmptyUrl_ShouldFail(string url)
    {
        var request = new UpdateWebhookRequestDto
        {
            Url = url,
            Events = new List<string> { "execution.completed" },
            Secret = null
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Url" && 
                                            e.ErrorMessage == "Webhook URL is required");
    }

    [Fact]
    public void Validate_WithInvalidUrl_ShouldFail()
    {
        var request = new UpdateWebhookRequestDto
        {
            Url = "not-a-valid-url",
            Events = new List<string> { "execution.completed" },
            Secret = null
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Url" && 
                                            e.ErrorMessage == "Webhook URL must be a valid HTTP/HTTPS URL");
    }

    [Fact]
    public void Validate_WithEmptyEvents_ShouldFail()
    {
        var request = new UpdateWebhookRequestDto
        {
            Url = "https://example.com/webhook",
            Events = new List<string>(),
            Secret = null
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Events" && 
                                            e.ErrorMessage == "At least one event type must be selected");
    }

    [Fact]
    public void Validate_WithInvalidEventType_ShouldFail()
    {
        var request = new UpdateWebhookRequestDto
        {
            Url = "https://example.com/webhook",
            Events = new List<string> { "invalid.event" },
            Secret = null
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Events");
    }

    [Fact]
    public void Validate_WithSecretTooShort_ShouldFail()
    {
        var request = new UpdateWebhookRequestDto
        {
            Url = "https://example.com/webhook",
            Events = new List<string> { "execution.completed" },
            Secret = "short"
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Secret" && 
                                            e.ErrorMessage == "Webhook secret must be at least 20 characters");
    }

    [Fact]
    public void Validate_WithNullSecret_ShouldPass()
    {
        var request = new UpdateWebhookRequestDto
        {
            Url = "https://example.com/webhook",
            Events = new List<string> { "execution.completed" },
            Secret = null
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyStringSecret_ShouldPass()
    {
        var request = new UpdateWebhookRequestDto
        {
            Url = "https://example.com/webhook",
            Events = new List<string> { "execution.completed" },
            Secret = ""
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }
}