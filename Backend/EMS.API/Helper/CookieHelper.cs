namespace EMS.API.Helper;

public static class CookieHelper
{
    public const string RefreshTokenCookieName = "refreshToken";

    // XSS Safe (Can't read Refresh token with JS)
    private const string RefreshCookiePath = "/api/auth";

    public static void SetRefreshTokenCookie(HttpContext context, string RefreshToken)
    {
        var isProduction = context.RequestServices
            .GetRequiredService<IWebHostEnvironment>()
            .IsProduction();

        var options = new CookieOptions
        {
            HttpOnly = true,
            Secure = context.Request.IsHttps,
            SameSite = isProduction ? SameSiteMode.Strict : SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddHours(24),
            Path = RefreshCookiePath,
            IsEssential = true
        };

        context.Response.Cookies.Append(RefreshTokenCookieName, RefreshToken, options);
    }

    // Clear Refresh token (logout / session expiry)
    public static void ClearRefreshTokenCookie(HttpContext context)
    {
        var isProduction = context.RequestServices
            .GetRequiredService<IWebHostEnvironment>()
            .IsProduction();

        context.Response.Cookies.Delete(RefreshTokenCookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = context.Request.IsHttps,
            SameSite = isProduction ? SameSiteMode.Strict : SameSiteMode.Lax,
            Path = RefreshCookiePath
        });
    }

    // Read refresh token from cookie, set null if don't get.
    public static string? GetRefreshTokenFromCookie(HttpContext context)
    {
        return context.Request.Cookies[RefreshTokenCookieName];
    }
}