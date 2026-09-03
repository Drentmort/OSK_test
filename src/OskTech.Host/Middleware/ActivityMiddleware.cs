using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using OskTech.Application.Interfaces.Services;
using OskTech.Host.Auth;

namespace OskTech.Host.Middleware;

public sealed class ActivityMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IActivityService activity)
    {
        if (context.User.Identity?.IsAuthenticated == true &&
            context.Request.Path.StartsWithSegments("/cabinet", StringComparison.OrdinalIgnoreCase))
        {
            var userId = AuthExtensions.GetUserId(context.User);
            if (userId is null)
            {
                context.Response.Redirect("/login");
                return;
            }

            try
            {
                await activity.EnsureActiveAsync(userId.Value, context.RequestAborted);
            }
            catch (InvalidOperationException)
            {
                await Auth.AuthExtensions.SignOutAsync(context);
                context.Response.Redirect("/login");
                return;
            }
        }

        await next(context);
    }
}
