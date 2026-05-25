using Debts.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Debts.Infrastructure.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(50);
        builder.HasIndex(x => x.Name).IsUnique();
        
        builder.HasData(
            new Role { Id = Guid.Parse("00000000-0000-0000-0000-000000000001"), Name = "Admin" },
            new Role { Id = Guid.Parse("00000000-0000-0000-0000-000000000002"), Name = "UserAdmin" },
            new Role { Id = Guid.Parse("00000000-0000-0000-0000-000000000003"), Name = "DebtAdmin" }
        );
    }
}