using Arc.Domain.Models;
using Arc.Application.Identity;
using Microsoft.AspNetCore.Http;
namespace Arc.Infrastructure.Identity;


/// <summary>
/// Resolves user identity from HTTP request context.
/// Implements deterministic user resolution strategy:
/// 1. Try to resolve from JWT Bearer token
/// 2. Try to resolve from X-User-Id header (legacy support)
/// 3. Fallback to anonymous user (deterministic default)
/// </summary>
public sealed class HttpUserContext : IUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IJwtTokenService _jwtTokenService;

    public HttpUserContext(IHttpContextAccessor httpContextAccessor, IJwtTokenService jwtTokenService)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _jwtTokenService = jwtTokenService ?? throw new ArgumentNullException(nameof(jwtTokenService));
    }

    public UserId CurrentUserId
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
                return UserId.Anonymous;

            // Try to resolve from cookie first (frontend sends token in cookie)
            if (httpContext.Request.Cookies.TryGetValue("auth_token", out var cookieToken))
            {
                if (!string.IsNullOrWhiteSpace(cookieToken))
                {
                    var userId = _jwtTokenService.ValidateToken(cookieToken);
                    if (userId.HasValue)
                    {
                        return userId.Value;
                    }
                }
            }

            // Try to resolve from JWT Bearer token (Authorization header)
            if (httpContext.Request.Headers.TryGetValue("Authorization", out var authHeaderValues))
            {
                var authHeader = authHeaderValues.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    var token = authHeader["Bearer ".Length..].Trim();
                    var userId = _jwtTokenService.ValidateToken(token);
                    if (userId.HasValue)
                    {
                        return userId.Value;
                    }
                }
            }

            // Fallback: Try to resolve from X-User-Id header (legacy support)
            if (httpContext.Request.Headers.TryGetValue("X-User-Id", out var userIdHeader))
            {
                var userIdString = userIdHeader.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(userIdString) && Guid.TryParse(userIdString, out var userId))
                {
                    return UserId.From(userId);
                }
            }

            // Fallback to deterministic anonymous user
            return UserId.Anonymous;
        }
    }
}