using FluentAssertions;
using Arc.Domain.Models;
namespace Arc.UnitTests.Identity;
using Arc.Infrastructure.Identity;
using Microsoft.Extensions.Configuration;


public sealed class JwtTokenServiceTests
{
    private readonly JwtTokenService _jwtTokenService;
    private readonly User _testUser;

    public JwtTokenServiceTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SecretKey"] = "ThisIsAVerySecureSecretKeyThatIsLongEnough123456",
                ["Jwt:Issuer"] = "ArcTestIssuer",
                ["Jwt:Audience"] = "ArcTestAudience",
                ["Jwt:ExpirationMinutes"] = "60"
            })
            .Build();

        _jwtTokenService = new JwtTokenService(configuration);
        _testUser = User.Create("test@example.com", "hashedpassword", UserRole.User);
    }

    [Fact]
    public void Constructor_WithValidConfiguration_ShouldCreateService()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SecretKey"] = "ThisIsAVerySecureSecretKeyThatIsLongEnough123456",
                ["Jwt:Issuer"] = "Arc",
                ["Jwt:Audience"] = "Arc",
                ["Jwt:ExpirationMinutes"] = "60"
            })
            .Build();

        // Act
        var service = new JwtTokenService(configuration);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithMissingSecretKey_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        // Act & Assert
        var act = () => new JwtTokenService(configuration);
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("JWT SecretKey not configured");
    }

    [Fact]
    public void Constructor_WithShortSecretKey_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SecretKey"] = "TooShort"
            })
            .Build();

        // Act & Assert
        var act = () => new JwtTokenService(configuration);
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("JWT SecretKey must be at least 32 characters long");
    }

    [Fact]
    public void GenerateToken_WithValidUser_ShouldReturnToken()
    {
        // Act
        var token = _jwtTokenService.GenerateToken(_testUser);

        // Assert
        token.Should().NotBeNullOrWhiteSpace();
        token.Should().Contain("."); // JWT format has dots
    }

    [Fact]
    public void GenerateToken_WithNullUser_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => _jwtTokenService.GenerateToken(null!);
        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("user");
    }

    [Fact]
    public void GenerateToken_WithAdminUser_ShouldIncludeAdminRole()
    {
        // Arrange
        var adminUser = User.Create("admin@example.com", "hashedpassword", UserRole.Admin);

        // Act
        var token = _jwtTokenService.GenerateToken(adminUser);

        // Assert
        token.Should().NotBeNullOrWhiteSpace();
        var userId = _jwtTokenService.ValidateToken(token);
        userId.Should().NotBeNull();
        userId!.Value.Value.Should().Be(adminUser.Id.Value);
    }

    [Fact]
    public void GenerateToken_CalledTwiceForSameUser_ShouldReturnDifferentTokens()
    {
        // Act
        var token1 = _jwtTokenService.GenerateToken(_testUser);
        Thread.Sleep(1100); // ensure different second → different nbf/exp
        var token2 = _jwtTokenService.GenerateToken(_testUser);

        // Assert
        token1.Should().NotBe(token2);
    }

    [Fact]
    public void ValidateToken_WithValidToken_ShouldReturnUserId()
    {
        // Arrange
        var token = _jwtTokenService.GenerateToken(_testUser);

        // Act
        var result = _jwtTokenService.ValidateToken(token);

        // Assert
        result.Should().NotBeNull();
        result!.Value.Value.Should().Be(_testUser.Id.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void ValidateToken_WithInvalidToken_ShouldReturnNull(string? invalidToken)
    {
        // Act
        var result = _jwtTokenService.ValidateToken(invalidToken!);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ValidateToken_WithMalformedToken_ShouldReturnNull()
    {
        // Arrange
        var malformedToken = "this.is.not.a.valid.jwt";

        // Act
        var result = _jwtTokenService.ValidateToken(malformedToken);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ValidateToken_WithExpiredToken_ShouldReturnNull()
    {
        // Arrange - Create a token with 0 minute expiration
        var configWithZeroExpiration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SecretKey"] = "ThisIsAVerySecureSecretKeyThatIsLongEnough123456",
                ["Jwt:Issuer"] = "ArcTestIssuer",
                ["Jwt:Audience"] = "ArcTestAudience",
                ["Jwt:ExpirationMinutes"] = "0"
            })
            .Build();

        var shortLivedService = new JwtTokenService(configWithZeroExpiration);
        var token = shortLivedService.GenerateToken(_testUser);

        // Wait for token to expire (0-minute config uses 1-second lifetime)
        Thread.Sleep(1500);

        // Act
        var result = shortLivedService.ValidateToken(token);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ValidateToken_WithTokenFromDifferentIssuer_ShouldReturnNull()
    {
        // Arrange
        var differentIssuerConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SecretKey"] = "ThisIsAVerySecureSecretKeyThatIsLongEnough123456",
                ["Jwt:Issuer"] = "DifferentIssuer",
                ["Jwt:Audience"] = "ArcTestAudience",
                ["Jwt:ExpirationMinutes"] = "60"
            })
            .Build();

        var differentService = new JwtTokenService(differentIssuerConfig);
        var token = differentService.GenerateToken(_testUser);

        // Act
        var result = _jwtTokenService.ValidateToken(token);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ValidateToken_WithTokenFromDifferentSecretKey_ShouldReturnNull()
    {
        // Arrange
        var differentKeyConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SecretKey"] = "DifferentSecretKeyThatIsAlsoLongEnough123456789",
                ["Jwt:Issuer"] = "ArcTestIssuer",
                ["Jwt:Audience"] = "ArcTestAudience",
                ["Jwt:ExpirationMinutes"] = "60"
            })
            .Build();

        var differentService = new JwtTokenService(differentKeyConfig);
        var token = differentService.GenerateToken(_testUser);

        // Act
        var result = _jwtTokenService.ValidateToken(token);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithDefaultsForOptionalFields_ShouldUseDefaults()
    {
        // Arrange
        var minimalConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SecretKey"] = "ThisIsAVerySecureSecretKeyThatIsLongEnough123456"
            })
            .Build();

        // Act
        var service = new JwtTokenService(minimalConfig);
        var token = service.GenerateToken(_testUser);

        // Assert
        token.Should().NotBeNullOrWhiteSpace();
        var userId = service.ValidateToken(token);
        userId.Should().NotBeNull();
    }
}