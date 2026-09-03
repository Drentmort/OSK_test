using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using OskTech.Application.Interfaces.Services;

namespace OskTech.Host.Auth;

public static class AuthExtensions
{
    public const string DeviceIdCookie = "device_id";

    public static string GetOrCreateDeviceId(HttpContext context)
    {
        if (context.Request.Cookies.TryGetValue(DeviceIdCookie, out var deviceId) && !string.IsNullOrWhiteSpace(deviceId))
            return deviceId;

        deviceId = Guid.NewGuid().ToString("N");
        context.Response.Cookies.Append(DeviceIdCookie, deviceId, new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            MaxAge = TimeSpan.FromDays(365),
            SameSite = SameSiteMode.Lax
        });
        return deviceId;
    }

    public static Task SignInAsync(HttpContext context, AuthResult result, string deviceId)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, result.UserId.ToString()),
            new(ClaimTypes.Name, result.Login),
            new("device_id", deviceId)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        return context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
    }

    public static Task SignOutAsync(HttpContext context) =>
        context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

    public static Guid? GetUserId(ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? id : null;
    }

    public static string? GetDeviceId(ClaimsPrincipal user) => user.FindFirstValue("device_id");
}
