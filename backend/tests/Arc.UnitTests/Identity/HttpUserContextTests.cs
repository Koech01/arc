using NSubstitute;
using FluentAssertions;
using Arc.Domain.Models;
using Arc.Application.Identity;
using Microsoft.AspNetCore.Http;
namespace Arc.UnitTests.Identity;
using Arc.Infrastructure.Identity;

public sealed class HttpUserContextTests
{
    private readonly IJwtTokenService _jwtTokenService;

    public HttpUserContextTests()
    {
        _jwtTokenService = Substitute.For<IJwtTokenService>();
    }

    [Fact]
    public void CurrentUserId_WithValidJwtToken_ShouldReturnParsedUserId()
    {
        // Arrange
        var expectedUserId = UserId.From(Guid.NewGuid());
        var token = "valid_jwt_token";
        var httpContextAccessor = CreateHttpContextAccessorWithBearerToken(token);
        var userContext = new HttpUserContext(httpContextAccessor, _jwtTokenService);
        
        _jwtTokenService.ValidateToken(token).Returns(expectedUserId);

        // Act
        var result = userContext.CurrentUserId;

        // Assert
        result.Should().Be(expectedUserId);
        _jwtTokenService.Received(1).ValidateToken(token);
    }

    [Fact]
    public void CurrentUserId_WithInvalidJwtToken_ShouldFallbackToHeaderAndReturnAnonymous()
    {
        // Arrange
        var token = "invalid_jwt_token";
        var httpContextAccessor = CreateHttpContextAccessorWithBearerToken(token);
        var userContext = new HttpUserContext(httpContextAccessor, _jwtTokenService);
        
        _jwtTokenService.ValidateToken(token).Returns((UserId?)null);

        // Act
        var result = userContext.CurrentUserId;

        // Assert
        result.Should().Be(UserId.Anonymous);
        _jwtTokenService.Received(1).ValidateToken(token);
    }

    [Fact]
    public void CurrentUserId_WithValidUserIdHeader_ShouldReturnParsedUserId()
    {
        // Arrange
        var expectedGuid = Guid.NewGuid();
        var httpContextAccessor = CreateHttpContextAccessor(expectedGuid.ToString());
        var userContext = new HttpUserContext(httpContextAccessor, _jwtTokenService);

        // Act
        var result = userContext.CurrentUserId;

        // Assert
        result.Value.Should().Be(expectedGuid);
    }

    [Fact]
    public void CurrentUserId_WithInvalidUserIdHeader_ShouldReturnAnonymous()
    {
        // Arrange
        var httpContextAccessor = CreateHttpContextAccessor("invalid-guid");
        var userContext = new HttpUserContext(httpContextAccessor, _jwtTokenService);

        // Act
        var result = userContext.CurrentUserId;

        // Assert
        result.Should().Be(UserId.Anonymous);
    }

    [Fact]
    public void CurrentUserId_WithEmptyUserIdHeader_ShouldReturnAnonymous()
    {
        // Arrange
        var httpContextAccessor = CreateHttpContextAccessor("");
        var userContext = new HttpUserContext(httpContextAccessor, _jwtTokenService);

        // Act
        var result = userContext.CurrentUserId;

        // Assert
        result.Should().Be(UserId.Anonymous);
    }

    [Fact]
    public void CurrentUserId_WithMissingUserIdHeader_ShouldReturnAnonymous()
    {
        // Arrange
        var httpContextAccessor = CreateHttpContextAccessor(null);
        var userContext = new HttpUserContext(httpContextAccessor, _jwtTokenService);

        // Act
        var result = userContext.CurrentUserId;

        // Assert
        result.Should().Be(UserId.Anonymous);
    }

    [Fact]
    public void CurrentUserId_WithNoHttpContext_ShouldReturnAnonymous()
    {
        // Arrange
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns((HttpContext?)null);
        var userContext = new HttpUserContext(httpContextAccessor, _jwtTokenService);

        // Act
        var result = userContext.CurrentUserId;

        // Assert
        result.Should().Be(UserId.Anonymous);
    }

    [Fact]
    public void CurrentUserId_ShouldBeDeterministic()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var httpContextAccessor = CreateHttpContextAccessor(guid.ToString());
        var userContext = new HttpUserContext(httpContextAccessor, _jwtTokenService);

        // Act
        var result1 = userContext.CurrentUserId;
        var result2 = userContext.CurrentUserId;

        // Assert
        result1.Should().Be(result2);
        result1.Value.Should().Be(guid);
    }

    [Fact]
    public void Constructor_WithNullHttpContextAccessor_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var act = () => new HttpUserContext(null!, _jwtTokenService);
        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("httpContextAccessor");
    }

    [Fact]
    public void Constructor_WithNullJwtTokenService_ShouldThrowArgumentNullException()
    {
        // Arrange
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();

        // Act & Assert
        var act = () => new HttpUserContext(httpContextAccessor, null!);
        act.Should().Throw<ArgumentNullException>()
           .WithParameterName("jwtTokenService");
    }

    [Fact]
    public void CurrentUserId_WithJwtTokenTakesPrecedenceOverHeader()
    {
        // Arrange
        var jwtUserId = UserId.From(Guid.NewGuid());
        var headerUserId = Guid.NewGuid();
        var token = "valid_jwt_token";
        
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Authorization"] = $"Bearer {token}";
        httpContext.Request.Headers["X-User-Id"] = headerUserId.ToString();
        
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(httpContext);
        
        var userContext = new HttpUserContext(httpContextAccessor, _jwtTokenService);
        _jwtTokenService.ValidateToken(token).Returns(jwtUserId);

        // Act
        var result = userContext.CurrentUserId;

        // Assert
        result.Should().Be(jwtUserId);
        result.Should().NotBe(UserId.From(headerUserId));
    }

    private static IHttpContextAccessor CreateHttpContextAccessor(string? userIdHeaderValue)
    {
        var httpContext = new DefaultHttpContext();
        
        if (userIdHeaderValue != null)
        {
            httpContext.Request.Headers["X-User-Id"] = userIdHeaderValue;
        }

        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(httpContext);
        
        return httpContextAccessor;
    }

    private static IHttpContextAccessor CreateHttpContextAccessorWithBearerToken(string token)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Authorization"] = $"Bearer {token}";

        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(httpContext);
        
        return httpContextAccessor;
    }
}