namespace CampusCore.Configuration;

// Explicit mode selection is the composition boundary. An invalid/missing mode
// never falls back to a writable or database-backed hosted application.
public sealed record CampusCoreMode(bool IsPublicDemo)
{
    public static CampusCoreMode Resolve(IConfiguration configuration, IHostEnvironment environment) => configuration["CampusCore:Mode"] switch
    {
        "PublicDemo" => new(true),
        "LocalSql" when environment.IsDevelopment() => new(false),
        "LocalSql" => throw new InvalidOperationException("LocalSql mode is restricted to the Development environment."),
        _ => throw new InvalidOperationException("Set CampusCore:Mode explicitly to PublicDemo or LocalSql. No mode was selected safely.")
    };
}
