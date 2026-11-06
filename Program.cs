using CampusCore.Configuration;
using CampusCore.Data;
using CampusCore.Security;
using CampusCore.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var mode = CampusCoreMode.Resolve(builder.Configuration, builder.Environment);
builder.Services.AddSingleton(mode);
builder.Services.AddControllersWithViews(options => options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddAntiforgery(options => { options.Cookie.SameSite = SameSiteMode.Strict; options.Cookie.HttpOnly = true; });
builder.Services.AddSingleton<LocalWriteAccess>();
if (mode.IsPublicDemo)
{
    // No connection string is read, no DbContext/write service is registered,
    // and each request receives a new, fixed fictional dataset.
    builder.Services.AddScoped<IAcademicData, DemoAcademicData>();
}
else
{
    var connection = builder.Configuration.GetConnectionString("CampusCore") ?? throw new InvalidOperationException("LocalSql mode requires a local development database connection.");
    DatabaseSafety.RequireLocal(connection);
    builder.Services.AddDbContext<AcademicDbContext>(options => options.UseSqlServer(connection));
    builder.Services.AddScoped<IAcademicData, DatabaseAcademicData>();
    builder.Services.AddScoped<AcademicWrites>();
}
var app = builder.Build();
var commands = new[] { "--migrate-dev", "--seed-dev", "--verify-data", "--verify-database", "--verify-demo" };
if (args.Any(commands.Contains))
{
    using var scope = app.Services.CreateScope();
    if (args.Contains("--verify-demo"))
    {
        if (!mode.IsPublicDemo || args.Any(a => commands.Contains(a) && a != "--verify-demo")) throw new InvalidOperationException("Run --verify-demo alone in PublicDemo mode.");
        DemoModeVerification.Run(scope.ServiceProvider, mode, app.Configuration);
        return;
    }
    if (args.Length == 1 && args.Contains("--verify-data"))
    {
        DemoVerification.Run(scope.ServiceProvider.GetRequiredService<IAcademicData>());
        return;
    }
    if (mode.IsPublicDemo || !app.Environment.IsDevelopment() || !bool.TryParse(app.Configuration["Development:EnableWrites"], out var enabled) || !enabled)
        throw new InvalidOperationException("Database commands require LocalSql mode and explicitly enabled Development writes.");
    var db = scope.ServiceProvider.GetRequiredService<AcademicDbContext>();
    var connection = db.Database.GetConnectionString()!;
    DatabaseSafety.RequireLocal(connection);
    if (args.Contains("--migrate-dev")) { await db.Database.MigrateAsync(); Console.WriteLine("Local development migrations applied."); }
    if (args.Contains("--seed-dev")) { if ((await db.Database.GetPendingMigrationsAsync()).Any()) throw new InvalidOperationException("Apply migrations before seeding."); await DevelopmentSeed.ApplyAsync(db); }
    if (args.Contains("--verify-data")) DemoVerification.Run(scope.ServiceProvider.GetRequiredService<IAcademicData>());
    if (args.Contains("--verify-database")) await DatabaseVerification.RunAsync(connection);
    return;
}
// Headers cover errors and rejected requests as well as successful MVC pages.
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "same-origin";
    context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; style-src 'self'; script-src 'self'; img-src 'self' data:; frame-ancestors 'none'; base-uri 'self'; form-action 'self'";
    context.Response.Headers["Cache-Control"] = "no-cache, no-store";
    context.Response.Headers["Pragma"] = "no-cache";
    await next();
});
// LocalSql may never expose even read-only local records to a remote/proxied
// request. PublicDemo has no SQL services and rejects every unsafe HTTP method.
app.Use(async (context, next) =>
{
    var safeMethod = HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method) || HttpMethods.IsOptions(context.Request.Method);
    if ((!mode.IsPublicDemo && !LocalWriteAccess.IsLocalRequest(context)) ||
        (!safeMethod && !context.RequestServices.GetRequiredService<LocalWriteAccess>().Allows(context)))
    {
        context.Response.StatusCode = 403;
        context.Response.ContentType = "text/plain; charset=utf-8";
        await context.Response.WriteAsync("This request is not permitted in the current workspace.");
        return;
    }
    await next();
});
app.UseExceptionHandler("/Home/Error");
app.UseStatusCodePagesWithReExecute("/Home/Status", "?code={0}");
app.UseStaticFiles();
app.UseRouting();
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.Run();
