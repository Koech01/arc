using System.Text;
using Arc.Domain.Models;
using System.Security.Claims;
using Arc.Application.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
namespace Arc.Infrastructure.Identity;
using Microsoft.Extensions.Configuration;


/// <summary>
/// JWT token service that generates and validates JWT tokens for authentication.
/// Provides deterministic token generation using configured secret key.
/// </summary>
public sealed class JwtTokenService : IJwtTokenService
{
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expirationMinutes;
    private readonly SigningCredentials _signingCredentials;

    public JwtTokenService(IConfiguration configuration)
    {
        _secretKey = configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");
        _issuer = configuration["Jwt:Issuer"] ?? "Arc";
        _audience = configuration["Jwt:Audience"] ?? "Arc";
        _expirationMinutes = int.Parse(configuration["Jwt:ExpirationMinutes"] ?? "60");

        if (_secretKey.Length < 32)
            throw new InvalidOperationException("JWT SecretKey must be at least 32 characters long");

        var key = Encoding.UTF8.GetBytes(_secretKey);
        _signingCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);
    }

    public string GenerateToken(User user)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.Value.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToRoleString()),
            new Claim("user_id", user.Id.Value.ToString()),
            new Claim("is_active", user.IsActive.ToString().ToLowerInvariant())
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = _expirationMinutes > 0
                ? DateTime.UtcNow.AddMinutes(_expirationMinutes)
                : DateTime.UtcNow.AddSeconds(1),
            NotBefore = DateTime.UtcNow,
            Issuer = _issuer,
            Audience = _audience,
            SigningCredentials = _signingCredentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public UserId? ValidateToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_secretKey);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
            var userIdClaim = principal.FindFirst("user_id")?.Value;

            if (userIdClaim != null && Guid.TryParse(userIdClaim, out var userId))
            {
                return UserId.From(userId);
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    public UserId? ExtractUserId(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jsonToken = tokenHandler.ReadJwtToken(token);
            var userIdClaim = jsonToken.Claims.FirstOrDefault(c => c.Type == "user_id")?.Value;

            if (userIdClaim != null && Guid.TryParse(userIdClaim, out var userId))
            {
                return UserId.From(userId);
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}