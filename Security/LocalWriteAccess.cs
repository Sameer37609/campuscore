using System.Net;
using CampusCore.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
namespace CampusCore.Security;

public sealed class LocalWriteAccess(IHostEnvironment environment, IConfiguration configuration, CampusCoreMode mode)
{
    public bool Allows(HttpContext context)
    {
        // Demo mode wins over every environment/write flag and never reads SQL configuration.
        if (mode.IsPublicDemo || !bool.TryParse(configuration["Development:EnableWrites"], out var enabled) || !IsAllowed(environment.IsDevelopment(), enabled, context)) return false;
        try { CampusCore.Data.DatabaseSafety.RequireLocal(configuration.GetConnectionString("CampusCore") ?? ""); return true; }
        catch (Exception e) when (e is ArgumentException or InvalidOperationException) { return false; }
    }
    public static bool IsLocalRequest(HttpContext context)
    {
        var request = context.Request;
        bool Loopback(IPAddress? ip) => ip is not null && IPAddress.IsLoopback(ip.IsIPv4MappedToIPv6 ? ip.MapToIPv4() : ip);
        var host = request.Host.Host.Trim('[', ']');
        var localHost = host.Equals("localhost", StringComparison.OrdinalIgnoreCase) || (IPAddress.TryParse(host, out var parsed) && Loopback(parsed));
        if (!localHost || !Loopback(context.Connection.RemoteIpAddress) || !Loopback(context.Connection.LocalIpAddress)) return false;
        if (request.Headers.ContainsKey("Forwarded") || request.Headers.Keys.Any(k => k.StartsWith("X-Forwarded-", StringComparison.OrdinalIgnoreCase) || k.StartsWith("X-Original-", StringComparison.OrdinalIgnoreCase))) return false;
        if (request.Headers.TryGetValue("Origin", out var origin) &&
            (!Uri.TryCreate(origin.ToString(), UriKind.Absolute, out var uri) || uri.GetLeftPart(UriPartial.Authority) != request.Scheme + "://" + request.Host.Value)) return false;
        return true;
    }
    public static bool IsAllowed(bool development, bool enabled, HttpContext context) => development && enabled && IsLocalRequest(context);
}
public sealed class LocalWriteFilter(LocalWriteAccess access) : IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context) { if (!access.Allows(context.HttpContext)) context.Result = new StatusCodeResult(403); }
}
public sealed class LocalWriteAttribute() : TypeFilterAttribute(typeof(LocalWriteFilter));
