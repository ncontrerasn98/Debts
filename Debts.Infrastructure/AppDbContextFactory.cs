using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Debts.Infrastructure;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

        optionsBuilder.UseMySql(
            "server=127.0.0.1;port=3306;database=debtdb;user=root;password=root",
            ServerVersion.AutoDetect("server=127.0.0.1;port=3306;database=debtdb;user=root;password=root")
        );

        return new AppDbContext(optionsBuilder.Options);
    }
}