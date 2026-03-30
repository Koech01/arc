using NSubstitute;
using FluentAssertions;
namespace Arc.UnitTests.Identity;
using Arc.Infrastructure.Identity;
using Microsoft.Extensions.Logging;


public sealed class ConsoleEmailServiceTests
{
    private readonly ILogger<ConsoleEmailService> _mockLogger;
    private readonly ConsoleEmailService _emailService;

    public ConsoleEmailServiceTests()
    {
        _mockLogger = Substitute.For<ILogger<ConsoleEmailService>>();
        _emailService = new ConsoleEmailService(_mockLogger);
    }

    [Fact]
    public async Task SendPasswordResetEmailAsync_WithValidParameters_ShouldLogEmail()
    {
        // Arrange
        var email = "test@example.com";
        var resetLink = "https://example.com/reset?token=abc123";

        // Act
        await _emailService.SendPasswordResetEmailAsync(email, resetLink);

        // Assert
        _mockLogger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(v => v.ToString()!.Contains(email) && v.ToString()!.Contains(resetLink)),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task SendPasswordResetEmailAsync_ShouldCompleteSuccessfully()
    {
        // Arrange
        var email = "test@example.com";
        var resetLink = "https://example.com/reset?token=abc123";

        // Act
        var act = async () => await _emailService.SendPasswordResetEmailAsync(email, resetLink);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendPasswordResetEmailAsync_WithEmptyEmail_ShouldStillLog()
    {
        // Arrange
        var email = "";
        var resetLink = "https://example.com/reset?token=abc123";

        // Act
        await _emailService.SendPasswordResetEmailAsync(email, resetLink);

        // Assert
        _mockLogger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task SendPasswordResetEmailAsync_WithMultipleCalls_ShouldLogEachTime()
    {
        // Arrange
        var email1 = "user1@example.com";
        var email2 = "user2@example.com";
        var resetLink1 = "https://example.com/reset?token=abc123";
        var resetLink2 = "https://example.com/reset?token=def456";

        // Act
        await _emailService.SendPasswordResetEmailAsync(email1, resetLink1);
        await _emailService.SendPasswordResetEmailAsync(email2, resetLink2);

              // Assert
        _mockLogger.Received(2).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }
}