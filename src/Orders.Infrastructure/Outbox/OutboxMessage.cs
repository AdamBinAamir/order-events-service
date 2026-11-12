namespace Orders.Infrastructure.Outbox;

/// <summary>A domain event stored in the same transaction as the aggregate that raised it.</summary>
public sealed class OutboxMessage
{
    /// <summary>Same as the domain event id, so consumers can de-duplicate on it.</summary>
    public Guid Id { get; set; }
    public string Type { get; set; } = null!;
    public string Payload { get; set; } = null!;
    public DateTime OccurredOnUtc { get; set; }
    public DateTime? ProcessedOnUtc { get; set; }
}
