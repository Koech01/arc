using NSubstitute;
using Arc.Api.Filters;
using FluentAssertions;
using Arc.Domain.Models;
using Arc.Application.Admin;
using Microsoft.AspNetCore.Mvc;
using Arc.Application.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging.Abstractions;


namespace Arc.UnitTests.Filters;
public sealed class AdminActionLoggingFilterTests
{
    private readonly AdminActionLoggingFilter _filter;
    private readonly IAdminAuditLogger _auditLogger;
    private readonly IUserContext _userContext;
    private readonly ActionExecutingContext _context;
    private readonly ActionExecutionDelegate _next;
    private readonly UserId _adminUserId;

    public AdminActionLoggingFilterTests()
    {
        _auditLogger = Substitute.For<IAdminAuditLogger>();
        _userContext = Substitute.For<IUserContext>();
        _adminUserId = new UserId(Guid.NewGuid());
        _userContext.CurrentUserId.Returns(_adminUserId);

        _filter = new AdminActionLoggingFilter(
            _auditLogger,
            _userContext,
            NullLogger<AdminActionLoggingFilter>.Instance);

        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(httpContext, new(), new(), new());
        _context = new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            null!);

        _next = Substitute.For<ActionExecutionDelegate>();
        _next.Invoke().Returns(new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), null!));
    }

    [Fact]
    public async Task OnActionExecutionAsync_ShouldCallNext()
    {
        _context.HttpContext.Request.Path = "/api/admin/stats";
        _context.HttpContext.Request.Method = "GET";

        await _filter.OnActionExecutionAsync(_context, _next);

        await _next.Received(1).Invoke();
    }

    [Theory]
    [InlineData("GET", "/api/admin/stats", AdminAuditAction.ViewedStats)]
    [InlineData("GET", "/api/admin/users", AdminAuditAction.ViewedUserList)]
    [InlineData("GET", "/api/admin/login-history", AdminAuditAction.ViewedLoginHistory)]
    [InlineData("PATCH", "/api/admin/users/123/status", AdminAuditAction.ActivatedUser)]
    [InlineData("PATCH", "/api/admin/users/123/role", AdminAuditAction.ChangedUserRole)]
    [InlineData("POST", "/api/admin/users/123/reset-password", AdminAuditAction.ResetUserPassword)]
    [InlineData("DELETE", "/api/admin/users/123", AdminAuditAction.DeletedUser)]
    [InlineData("POST", "/api/admin/maintenance", AdminAuditAction.EnabledMaintenanceMode)]
    [InlineData("DELETE", "/api/admin/maintenance", AdminAuditAction.DisabledMaintenanceMode)]
    [InlineData("GET", "/api/admin/system", AdminAuditAction.ViewedSystemConfig)]
    public async Task OnActionExecutionAsync_ShouldLogCorrectAction(string method, string path, AdminAuditAction expectedAction)
    {
        _context.HttpContext.Request.Path = path;
        _context.HttpContext.Request.Method = method;

        await _filter.OnActionExecutionAsync(_context, _next);

        await _auditLogger.Received(1).LogAsync(
            Arg.Is<AdminAuditEvent>(e => 
                e.AdminUserId == _adminUserId.Value && 
                e.Action == expectedAction),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OnActionExecutionAsync_WithUnknownPath_ShouldNotLog()
    {
        _context.HttpContext.Request.Path = "/api/unknown";
        _context.HttpContext.Request.Method = "GET";

        await _filter.OnActionExecutionAsync(_context, _next);

        await _auditLogger.DidNotReceive().LogAsync(
            Arg.Any<AdminAuditEvent>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OnActionExecutionAsync_ShouldCaptureIpAddress()
    {
        _context.HttpContext.Request.Path = "/api/admin/stats";
        _context.HttpContext.Request.Method = "GET";
        _context.HttpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.1");

        await _filter.OnActionExecutionAsync(_context, _next);

        await _auditLogger.Received(1).LogAsync(
            Arg.Is<AdminAuditEvent>(e => e.IpAddress == "192.168.1.1"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OnActionExecutionAsync_ShouldCaptureUserAgent()
    {
        _context.HttpContext.Request.Path = "/api/admin/stats";
        _context.HttpContext.Request.Method = "GET";
        _context.HttpContext.Request.Headers.UserAgent = "Test-Browser/1.0";

        await _filter.OnActionExecutionAsync(_context, _next);

        await _auditLogger.Received(1).LogAsync(
            Arg.Is<AdminAuditEvent>(e => e.UserAgent == "Test-Browser/1.0"),
            Arg.Any<CancellationToken>());
    }
}