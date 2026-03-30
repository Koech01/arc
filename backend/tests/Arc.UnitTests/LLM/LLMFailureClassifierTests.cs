using System.Net;
using FluentAssertions;
namespace Arc.UnitTests.LLM;
using Arc.Infrastructure.LLM;


public sealed class LLMFailureClassifierTests
{
    [Theory]
    [InlineData(HttpStatusCode.ServiceUnavailable, true)]
    [InlineData(HttpStatusCode.TooManyRequests, true)]
    [InlineData(HttpStatusCode.GatewayTimeout, true)]
    [InlineData(HttpStatusCode.RequestTimeout, true)]
    [InlineData(HttpStatusCode.InternalServerError, true)]
    [InlineData(HttpStatusCode.BadGateway, true)]
    [InlineData(HttpStatusCode.HttpVersionNotSupported, true)]
    public void IsTransientFailure_WithTransientStatusCodes_ShouldReturnTrue(HttpStatusCode statusCode, bool expected)
    {
        // Arrange
        var response = new HttpResponseMessage(statusCode);

        // Act
        var result = LLMFailureClassifier.IsTransientFailure(response);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, false)]
    [InlineData(HttpStatusCode.Unauthorized, false)]
    [InlineData(HttpStatusCode.Forbidden, false)]
    [InlineData(HttpStatusCode.NotFound, false)]
    [InlineData(HttpStatusCode.MethodNotAllowed, false)]
    [InlineData(HttpStatusCode.Conflict, false)]
    [InlineData(HttpStatusCode.UnprocessableEntity, false)]
    public void IsTransientFailure_WithNonTransientStatusCodes_ShouldReturnFalse(HttpStatusCode statusCode, bool expected)
    {
        // Arrange
        var response = new HttpResponseMessage(statusCode);

        // Act
        var result = LLMFailureClassifier.IsTransientFailure(response);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void IsTransientFailure_WithOKStatus_ShouldReturnFalse()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK);

        // Act
        var result = LLMFailureClassifier.IsTransientFailure(response);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsTransientFailure_WithTimeoutException_ShouldReturnTrue()
    {
        // Arrange
        var exception = new TimeoutException();

        // Act
        var result = LLMFailureClassifier.IsTransientFailure(exception);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsTransientFailure_WithOperationCanceledException_ShouldReturnTrue()
    {
        // Arrange
        var exception = new OperationCanceledException();

        // Act
        var result = LLMFailureClassifier.IsTransientFailure(exception);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsTransientFailure_WithHttpRequestExceptionWithTimeoutInner_ShouldReturnTrue()
    {
        // Arrange
        var innerException = new TimeoutException();
        var exception = new HttpRequestException("Request timeout", innerException);

        // Act
        var result = LLMFailureClassifier.IsTransientFailure(exception);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsTransientFailure_WithArgumentException_ShouldReturnFalse()
    {
        // Arrange
        var exception = new ArgumentException("Invalid argument");

        // Act
        var result = LLMFailureClassifier.IsTransientFailure(exception);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsTransientFailure_WithInvalidOperationException_ShouldReturnFalse()
    {
        // Arrange
        var exception = new InvalidOperationException("Invalid operation");

        // Act
        var result = LLMFailureClassifier.IsTransientFailure(exception);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsTransientFailure_WithHttpRequestExceptionWithoutTimeoutInner_ShouldReturnFalse()
    {
        // Arrange
        var exception = new HttpRequestException("Request failed");

        // Act
        var result = LLMFailureClassifier.IsTransientFailure(exception);

        // Assert
        result.Should().BeFalse();
    }
}