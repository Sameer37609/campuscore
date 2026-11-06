using CampusCore.Configuration;
using CampusCore.Data;
using CampusCore.Security;
using System.Net;
namespace CampusCore.Services;

public static class DemoModeVerification
{
    public static void Run(IServiceProvider services, CampusCoreMode mode, IConfiguration configuration)
    {
        var checks = 0;
        void Check(bool pass, string name) { if (!pass) throw new InvalidOperationException("FAIL: " + name); Console.WriteLine("PASS: " + name); checks++; }
        Check(mode.IsPublicDemo, "Explicit PublicDemo mode");
        Check(services.GetService<AcademicDbContext>() is null, "No database context registered");
        Check(services.GetService<AcademicWrites>() is null, "No write service registered");
        var data = services.GetRequiredService<IAcademicData>();
        Check(data is DemoAcademicData, "Only centralized fictional records supplied");
        DemoVerification.Run(data);
        var context = new DefaultHttpContext(); context.Request.Scheme = "http"; context.Request.Host = new("localhost", 5085); context.Connection.LocalIpAddress = IPAddress.Loopback; context.Connection.RemoteIpAddress = IPAddress.Loopback;
        Check(!services.GetRequiredService<LocalWriteAccess>().Allows(context), "Demo denies even local writes regardless of write/environment flags");
        Check(data.Students.All(s => s.Email.EndsWith("@students.example.edu", StringComparison.Ordinal)), "Every demo email comes from the fictional fixture");
        var original = data.Students[0].Name; data.Students[0].Name = "Verification-only change";
        using (var scope = services.CreateScope()) Check(scope.ServiceProvider.GetRequiredService<IAcademicData>().Students[0].Name == original, "Demo state cannot carry between request scopes");
        data.Students[0].Name = original;
        Console.WriteLine($"VERIFIED {checks} demo isolation checks plus 22 dataset/query checks.");
    }
}
