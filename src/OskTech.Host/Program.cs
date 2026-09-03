using Microsoft.AspNetCore.Authentication.Cookies;
using OskTech.Host.Middleware;
using OskTech.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxConcurrentConnections = 10_000;
    options.Limits.MaxConcurrentUpgradedConnections = 10_000;
});

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddHttpContextAccessor();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
    app.UseHsts();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseMiddleware<ActivityMiddleware>();
app.UseAuthorization();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();

public partial class Program;
