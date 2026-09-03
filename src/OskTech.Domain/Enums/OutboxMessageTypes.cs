namespace OskTech.Domain.Enums;

public static class OutboxMessageTypes
{
    public const string UserTextUpdated = "UserTextUpdated";
    public const string SessionsRevoked = "SessionsRevoked";
    public const string ActivityUpdated = "ActivityUpdated";
}
