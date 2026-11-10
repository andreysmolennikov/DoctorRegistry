using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DoctorRegistry.Infrastructure.Persistence.Messaging;

public sealed class ProcessedMessageConfiguration : IEntityTypeConfiguration<ProcessedMessage>
{
    public void Configure(EntityTypeBuilder<ProcessedMessage> builder)
    {
        builder.ToTable("processed_messages", "messaging");
        builder.HasKey(x => x.MessageId);
        builder.Property(x => x.ProcessedAt).IsRequired();
    }
}
