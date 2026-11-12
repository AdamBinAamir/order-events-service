using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Orders.Infrastructure.Inbox;

/// <summary>A message id the worker has already handled. The primary key makes repeats impossible.</summary>
public sealed class ProcessedMessage
{
    public Guid MessageId { get; set; }
    public string Type { get; set; } = null!;
    public DateTime ProcessedOnUtc { get; set; }
}

internal sealed class ProcessedMessageConfiguration : IEntityTypeConfiguration<ProcessedMessage>
{
    public void Configure(EntityTypeBuilder<ProcessedMessage> builder)
    {
        builder.ToTable("ProcessedMessages");
        builder.HasKey(m => m.MessageId);
        builder.Property(m => m.MessageId).ValueGeneratedNever();
        builder.Property(m => m.Type).HasMaxLength(200).IsRequired();
    }
}
