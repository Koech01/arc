namespace Arc.Api.Common;

public static class CookieSettings
{
    public const string CookieName = "auth_token";
    
    public static CookieOptions GetCookieOptions(bool isProduction)
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = isProduction,
            SameSite = isProduction ? SameSiteMode.Strict : SameSiteMode.Lax,
            MaxAge = TimeSpan.FromHours(8),
            Path = "/",
            IsEssential = true
        };
    }
}