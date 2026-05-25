using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Debts.Domain.Entities;

namespace Debts.Infrastructure.Configurations;

public class DebtConfiguration : IEntityTypeConfiguration<Debt>
{
    public void Configure(EntityTypeBuilder<Debt> builder)
    {
        builder.ToTable("Debts");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.OriginalAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.NegotiatedAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.HasOne(x => x.User)
            .WithMany(x => x.Debts)
            .HasForeignKey(x => x.UserId);
    }
}