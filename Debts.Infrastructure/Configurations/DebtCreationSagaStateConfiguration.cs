using Debts.Application.Sagas.CreateDebt;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Debts.Infrastructure.Configurations;

public class DebtCreationSagaStateConfiguration : IEntityTypeConfiguration<DebtCreationSagaState>
{
    public void Configure(EntityTypeBuilder<DebtCreationSagaState> builder)
    {
        builder.ToTable("DebtCreationSagaStates");

        builder.HasKey(x => x.CorrelationId);

        builder.Property(x => x.CurrentState)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.FailureReason)
            .HasMaxLength(500);

        builder.Property(x => x.Amount)
            .HasPrecision(18, 2);
    }
}