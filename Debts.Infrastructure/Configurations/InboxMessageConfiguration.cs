using Debts.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Debts.Infrastructure.Configurations;

public class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable("InboxMessages");

        builder.HasKey(x =>
            new
            {
                x.MessageId,
                x.Consumer
            });

        builder.Property(x => x.Consumer)
            .HasMaxLength(200);

        builder.Property(x => x.ProcessedOnUtc)
            .IsRequired();
    }
}