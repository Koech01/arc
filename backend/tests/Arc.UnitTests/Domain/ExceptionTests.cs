using FluentAssertions;
using Arc.Domain.Exceptions;
namespace Arc.UnitTests.Domain;


public sealed class DomainExceptionTests
{
    [Fact]
    public void Constructor_WithMessage_ShouldCreateException()
    {
        var exception = new TestDomainException("Test error message");

        exception.Message.Should().Be("Test error message");
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithMessageAndInnerException_ShouldCreateException()
    {
        var innerException = new InvalidOperationException("Inner error");
        var exception = new TestDomainException("Test error message", innerException);

        exception.Message.Should().Be("Test error message");
        exception.InnerException.Should().Be(innerException);
    }

    [Fact]
    public void DomainException_ShouldBeOfTypeException()
    {
        var exception = new TestDomainException("Test error");

        exception.Should().BeAssignableTo<Exception>();
    }

    private sealed class TestDomainException : DomainException
    {
        public TestDomainException(string message) : base(message) { }
        public TestDomainException(string message, Exception innerException) : base(message, innerException) { }
    }
}

public sealed class AuthenticationExceptionTests
{
    [Fact]
    public void Constructor_WithMessage_ShouldCreateException()
    {
        var exception = new AuthenticationException("Authentication failed");

        exception.Message.Should().Be("Authentication failed");
        exception.Should().BeAssignableTo<DomainException>();
    }

    [Fact]
    public void Constructor_WithMessageAndInnerException_ShouldCreateException()
    {
        var innerException = new InvalidOperationException("Inner error");
        var exception = new AuthenticationException("Authentication failed", innerException);

        exception.Message.Should().Be("Authentication failed");
        exception.InnerException.Should().Be(innerException);
    }

    [Fact]
    public void InvalidCredentials_ShouldCreateExceptionWithStandardMessage()
    {
        var exception = AuthenticationException.InvalidCredentials();

        exception.Message.Should().Be("Invalid email or password");
    }

    [Fact]
    public void InactiveAccount_ShouldCreateExceptionWithStandardMessage()
    {
        var exception = AuthenticationException.InactiveAccount();

        exception.Message.Should().Be("User account is inactive");
    }

    [Fact]
    public void DuplicateEmail_ShouldCreateExceptionWithEmailInMessage()
    {
        var exception = AuthenticationException.DuplicateEmail("test@example.com");

        exception.Message.Should().Contain("test@example.com");
        exception.Message.Should().Contain("already exists");
    }

    [Fact]
    public void UserNotFound_ShouldCreateExceptionWithStandardMessage()
    {
        var exception = AuthenticationException.UserNotFound();

        exception.Message.Should().Be("User not found");
    }

    [Fact]
    public void AccountLocked_ShouldCreateExceptionWithLockoutMessage()
    {
        var lockedUntil = DateTime.UtcNow.AddMinutes(15);
        var exception = AuthenticationException.AccountLocked(lockedUntil);

        exception.Message.Should().Contain("locked");
        exception.Message.Should().Contain("minute");
    }

    [Fact]
    public void AccountDeleted_ShouldCreateExceptionWithDeletedMessage()
    {
        var exception = AuthenticationException.AccountDeleted();

        exception.Message.Should().Contain("removed");
    }
}

public sealed class WorkflowExceptionTests
{
    [Fact]
    public void Constructor_WithMessage_ShouldCreateException()
    {
        var exception = new WorkflowException("Workflow validation failed");

        exception.Message.Should().Be("Workflow validation failed");
        exception.Should().BeAssignableTo<DomainException>();
    }
}

public sealed class WebhookExceptionTests
{
    [Fact]
    public void Constructor_WithMessage_ShouldCreateException()
    {
        var exception = new WebhookException("Webhook delivery failed");

        exception.Message.Should().Be("Webhook delivery failed");
        exception.Should().BeAssignableTo<DomainException>();
    }
}