using FluentAssertions;
using Arc.Domain.Models;
namespace Arc.UnitTests.Domain;


public sealed class UserIdTests
{
    [Fact]
    public void UserId_FromGuid_ShouldCreateCorrectUserId()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var userId = UserId.From(guid);

        // Assert
        userId.Value.Should().Be(guid);
    }

    [Fact]
    public void UserId_FromValidGuidString_ShouldCreateCorrectUserId()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var guidString = guid.ToString();

        // Act
        var userId = UserId.From(guidString);

        // Assert
        userId.Value.Should().Be(guid);
    }

    [Fact]
    public void UserId_FromInvalidGuidString_ShouldThrowArgumentException()
    {
        // Arrange
        var invalidGuidString = "not-a-guid";

        // Act & Assert
        var act = () => UserId.From(invalidGuidString);
        act.Should().Throw<ArgumentException>()
           .WithMessage("Invalid GUID format: not-a-guid*");
    }

    [Fact]
    public void UserId_ToString_ShouldReturnGuidString()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var userId = UserId.From(guid);

        // Act
        var result = userId.ToString();

        // Assert
        result.Should().Be(guid.ToString());
    }

    [Fact]
    public void UserId_Anonymous_ShouldHaveFixedValue()
    {
        // Arrange & Act
        var anonymous1 = UserId.Anonymous;
        var anonymous2 = UserId.Anonymous;

        // Assert
        anonymous1.Should().Be(anonymous2);
        anonymous1.Value.Should().Be(new Guid("00000000-0000-0000-0000-000000000001"));
    }

    [Fact]
    public void UserId_Equality_ShouldWorkCorrectly()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var userId1 = UserId.From(guid);
        var userId2 = UserId.From(guid);
        var userId3 = UserId.From(Guid.NewGuid());

        // Act & Assert
        userId1.Should().Be(userId2);
        userId1.Should().NotBe(userId3);
        (userId1 == userId2).Should().BeTrue();
        (userId1 != userId3).Should().BeTrue();
    }

    [Fact]
    public void UserId_GetHashCode_ShouldBeConsistent()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var userId1 = UserId.From(guid);
        var userId2 = UserId.From(guid);

        // Act & Assert
        userId1.GetHashCode().Should().Be(userId2.GetHashCode());
    }
}