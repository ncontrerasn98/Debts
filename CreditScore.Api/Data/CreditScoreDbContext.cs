using CreditScore.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace CreditScore.Api.Data;

public class CreditScoreDbContext : DbContext
{
    public CreditScoreDbContext(DbContextOptions<CreditScoreDbContext> options)
        : base(options) { }

    public DbSet<CreditHistory> CreditHistories => Set<CreditHistory>();
    public DbSet<CreditHistoryEvent> CreditHistoryEvents => Set<CreditHistoryEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CreditHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.Property(e => e.TotalOriginalAmount).HasPrecision(18, 2);
            entity.Property(e => e.TotalNegotiatedAmount).HasPrecision(18, 2);

            entity.Navigation(e => e.Events)
                .HasField("_events")
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        });
        
        modelBuilder.Entity<CreditHistoryEvent>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.EventType)
                .HasMaxLength(50)
                .IsRequired();

            entity.HasIndex(e => new { e.CreditHistoryId, e.DebtId, e.EventType })
                .IsUnique();  // garantía de unicidad a nivel DB

            entity.HasOne<CreditHistory>()
                .WithMany(h => h.Events)
                .HasForeignKey(e => e.CreditHistoryId);
        });
    }
}