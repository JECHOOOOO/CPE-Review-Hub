using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CPE_Review_Hub.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddUserSecrets<DesignTimeDbContextFactory>().AddEnvironmentVariables().Build();
        // Generating a migration requires a provider, but does not connect to a server.
        var connection = configuration.GetConnectionString("ReviewHub")
            ?? "Server=127.0.0.1;Database=cpe_review_hub;SslMode=Required";
        return new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseMySQL(connection).Options);
    }
}
