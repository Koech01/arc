using NSubstitute;
using FluentAssertions;
using Arc.Api.Middleware;
using Arc.Application.Admin;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
namespace Arc.UnitTests.Middleware;
using Microsoft.Extensions.Logging.Abstractions;


public sealed class GlobalExceptionMiddlewareTests
{
    private readonly GlobalExceptionMiddleware _middleware;
    private readonly RequestDelegate _next;
    private readonly DefaultHttpContext _context;

    public GlobalExceptionMiddlewareTests()
    {
        _next = Substitute.For<RequestDelegate>();
        _middleware = new GlobalExceptionMiddleware(_next);
        _context = new DefaultHttpContext();
        _context.Response.Body = new MemoryStream();
    }

    [Fact]
    public async Task InvokeAsync_WhenNoException_ShouldCallNext()
    {
        await _middleware.InvokeAsync(_context);

        await _next.Received(1).Invoke(_context);
    }

    [Fact]
    public async Task InvokeAsync_WhenExceptionThrown_ShouldReturn500()
    {
        _next.When(x => x.Invoke(Arg.Any<HttpContext>()))
             .Do(_ => throw new Exception("Test exception"));

        await _middleware.InvokeAsync(_context);

        _context.Response.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task InvokeAsync_WhenExceptionThrown_ShouldReturnJsonResponse()
    {
        _next.When(x => x.Invoke(Arg.Any<HttpContext>()))
             .Do(_ => throw new Exception("Test exception"));

        await _middleware.InvokeAsync(_context);

        _context.Response.ContentType.Should().Be("application/json");
        _context.Response.Body.Position = 0;
        var reader = new StreamReader(_context.Response.Body);
        var body = await reader.ReadToEndAsync();
        body.Should().Contain("error");
    }
}

public sealed class MaintenanceModeMiddlewareTests
{
    private readonly MaintenanceModeMiddleware _middleware;
    private readonly RequestDelegate _next;
    private readonly DefaultHttpContext _context;
    private readonly IMaintenanceModeService _maintenanceMode;

    public MaintenanceModeMiddlewareTests()
    {
        _next = Substitute.For<RequestDelegate>();
        _maintenanceMode = Substitute.For<IMaintenanceModeService>();
        _middleware = new MaintenanceModeMiddleware(_next, NullLogger<MaintenanceModeMiddleware>.Instance);
        _context = new DefaultHttpContext();
        _context.Response.Body = new MemoryStream();
    }

    [Fact]
    public async Task InvokeAsync_WhenMaintenanceModeDisabled_ShouldAllowRequest()
    {
        _maintenanceMode.IsEnabled.Returns(false);
        _context.Request.Path = "/api/execute";

        await _middleware.InvokeAsync(_context, _maintenanceMode);

        await _next.Received(1).Invoke(_context);
        _context.Response.StatusCode.Should().Be(200);
    }

    [Theory]
    [InlineData("/api/auth/login")]
    [InlineData("/api/admin/users")]
    [InlineData("/health")]
    [InlineData("/swagger")]
    public async Task InvokeAsync_WithAlwaysAllowedPaths_ShouldAllowRequest(string path)
    {
        _maintenanceMode.IsEnabled.Returns(true);
        _context.Request.Path = path;

        await _middleware.InvokeAsync(_context, _maintenanceMode);

        await _next.Received(1).Invoke(_context);
    }

    [Fact]
    public async Task InvokeAsync_WhenUserIsAdmin_ShouldAllowRequest()
    {
        _maintenanceMode.IsEnabled.Returns(true);
        _context.Request.Path = "/api/execute";
        _context.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Role, "Admin")
        }, "test"));

        await _middleware.InvokeAsync(_context, _maintenanceMode);

        await _next.Received(1).Invoke(_context);
    }

    [Fact]
    public async Task InvokeAsync_WhenMaintenanceModeEnabledAndNotAdmin_ShouldReturn503()
    {
        _maintenanceMode.IsEnabled.Returns(true);
        _maintenanceMode.GetStatus().Returns(new MaintenanceModeStatus(true, null, DateTime.UtcNow, "System upgrade"));
        _context.Request.Path = "/api/execute";
        _context.User = new ClaimsPrincipal(new ClaimsIdentity());

        await _middleware.InvokeAsync(_context, _maintenanceMode);

        await _next.DidNotReceive().Invoke(_context);
        _context.Response.StatusCode.Should().Be(503);
    }

    [Fact]
    public async Task InvokeAsync_WhenBlocked_ShouldReturnJsonWithMessage()
    {
        _maintenanceMode.IsEnabled.Returns(true);
        _maintenanceMode.GetStatus().Returns(new MaintenanceModeStatus(true, null, DateTime.UtcNow, "System upgrade"));
        _context.Request.Path = "/api/execute";
        _context.User = new ClaimsPrincipal(new ClaimsIdentity());

        await _middleware.InvokeAsync(_context, _maintenanceMode);

        _context.Response.ContentType.Should().Be("application/json");
        _context.Response.Headers["Retry-After"].ToString().Should().Be("300");
        
        _context.Response.Body.Position = 0;
        var reader = new StreamReader(_context.Response.Body);
        var body = await reader.ReadToEndAsync();
        body.Should().Contain("maintenance");
        body.Should().Contain("System upgrade");
    }
}