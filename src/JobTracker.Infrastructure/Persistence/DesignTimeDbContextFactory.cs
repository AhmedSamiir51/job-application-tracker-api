using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace JobTracker.Infrastructure.Persistence;

/// <summary>Lets `dotnet ef` create the context without booting the web host.</summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=jobtracker;Username=postgres;Password=postgres")
            .Options;
        return new AppDbContext(options);
    }
}
