using CreditScore.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace CreditScore.Api.Data;

public class CreditScoreDbContext : DbContext
{
    public CreditScoreDbContext(DbContextOptions<CreditScoreDbContext> options)
        : base(options) { }

    public DbSet<CreditHistory> CreditHistories => Set<CreditHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CreditHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.Property(e => e.TotalOriginalAmount).HasPrecision(18, 2);
            entity.Property(e => e.TotalNegotiatedAmount).HasPrecision(18, 2);
        });
    }
}