using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace CampusCore.Data;

public static class DatabaseSafety
{
    public const string Connection = "Server=(localdb)\\MSSQLLocalDB;Database=CampusCoreDevelopment;Integrated Security=true;Encrypt=false";
    public static void RequireLocal(string connection)
    {
        var c = new SqlConnectionStringBuilder(connection);
        if (!c.DataSource.Equals("(localdb)\\MSSQLLocalDB", StringComparison.OrdinalIgnoreCase) || !c.IntegratedSecurity ||
            (c.InitialCatalog != "CampusCoreDevelopment" && !c.InitialCatalog.StartsWith("CampusCore_Verify_", StringComparison.Ordinal)))
            throw new InvalidOperationException("Database operations are restricted to the named CampusCore LocalDB development or verification databases.");
    }
}
public class AcademicDesignFactory : IDesignTimeDbContextFactory<AcademicDbContext>
{
    public AcademicDbContext CreateDbContext(string[] args)
    {
        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") != "Development")
            throw new InvalidOperationException("Run EF tooling with ASPNETCORE_ENVIRONMENT=Development. Hosted migrations are not supported.");
        var config = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json").AddJsonFile("appsettings.Development.json", true).AddEnvironmentVariables().Build();
        var connection = config.GetConnectionString("CampusCore") ?? throw new InvalidOperationException("Missing CampusCore connection.");
        if (config["CampusCore:Mode"] != "LocalSql") throw new InvalidOperationException("EF tooling is disabled outside explicit LocalSql mode.");
        DatabaseSafety.RequireLocal(connection);
        return new(new DbContextOptionsBuilder<AcademicDbContext>().UseSqlServer(connection).Options);
    }
}
