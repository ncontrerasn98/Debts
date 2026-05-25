using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Debts.Domain.Entities;

namespace Debts.Infrastructure.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasMany(x => x.Debts)
            .WithOne(x => x.User)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasData(
            new User { Id = Guid.Parse("00000000-0000-0000-0000-000000000004"), Name = "admin", PasswordHash = "$2a$12$hB8bm0BhdNJL5hDUyZogGuRNhPbB62K45D2zTFo1w9gTazJe55rDC" }//password: pass
        );
    }
}