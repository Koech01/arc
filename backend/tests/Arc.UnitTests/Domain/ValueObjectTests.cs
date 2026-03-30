using FluentAssertions;
using Arc.Domain.Models;
namespace Arc.UnitTests.Domain;


public sealed class WebhookIdTests
{
    [Fact]
    public void Constructor_WithValidGuid_ShouldCreateId()
    {
        var guid = Guid.NewGuid();

        var id = new WebhookId(guid);

        id.Value.Should().Be(guid);
    }

    [Fact]
    public void Constructor_WithEmptyGuid_ShouldThrowException()
    {
        var act = () => new WebhookId(Guid.Empty);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void TwoIds_WithSameGuid_ShouldBeEqual()
    {
        var guid = Guid.NewGuid();
        var id1 = new WebhookId(guid);
        var id2 = new WebhookId(guid);

        id1.Should().Be(id2);
        (id1 == id2).Should().BeTrue();
        id1.GetHashCode().Should().Be(id2.GetHashCode());
    }

    [Fact]
    public void TwoIds_WithDifferentGuids_ShouldNotBeEqual()
    {
        var id1 = new WebhookId(Guid.NewGuid());
        var id2 = new WebhookId(Guid.NewGuid());

        id1.Should().NotBe(id2);
        (id1 != id2).Should().BeTrue();
    }

    [Fact]
    public void Create_ShouldGenerateNewId()
    {
        var id = WebhookId.Create();

        id.Should().NotBeNull();
        id.Value.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_ShouldGenerateUniqueIds()
    {
        var id1 = WebhookId.Create();
        var id2 = WebhookId.Create();

        id1.Should().NotBe(id2);
    }
}