namespace OskTech.Application.Options;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public int RefreshTokenDays { get; set; } = 7;
    public TimeSpan InactivityTimeout { get; set; } = TimeSpan.FromHours(24);
}

public sealed class RateLimitOptions
{
    public const string SectionName = "RateLimit";

    public int LoginPerMinute { get; set; } = 10;
    public int RegisterPerMinute { get; set; } = 5;
}
