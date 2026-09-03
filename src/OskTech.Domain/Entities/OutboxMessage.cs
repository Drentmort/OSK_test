namespace OskTech.Domain.Entities;

public class OutboxMessage
{
    public Guid Id { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public int RetryCount { get; private set; }

    private OutboxMessage()
    {
    }

    public static OutboxMessage Create(string type, string payload, DateTime now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        ArgumentNullException.ThrowIfNull(payload);

        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = type,
            Payload = payload,
            CreatedAt = now
        };
    }

    public void MarkProcessed(DateTime now) => ProcessedAt = now;

    public void IncrementRetry() => RetryCount++;
}
