using CampusCore.Data;
using CampusCore.Models;
using CampusCore.Security;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net;
namespace CampusCore.Services;

public static class DatabaseVerification
{
    public static async Task RunAsync(string connection)
    {
        DatabaseSafety.RequireLocal(connection);
        var builder = new SqlConnectionStringBuilder(connection) { InitialCatalog = "CampusCore_Verify_" + Guid.NewGuid().ToString("N") };
        var options = new DbContextOptionsBuilder<AcademicDbContext>().UseSqlServer(builder.ConnectionString).Options;
        AcademicDbContext Open() => new(options);
        var checks = 0;
        void Check(bool condition, string name) { if (!condition) throw new InvalidOperationException("FAIL: " + name); Console.WriteLine("PASS: " + name); checks++; }
        async Task<WriteResult> Write(Func<AcademicWrites, Task<WriteResult>> operation) { await using var context = Open(); return await operation(new(context, NullLogger<AcademicWrites>.Instance)); }
        async Task<StudentForm> StudentEdit(int id) { await using var context = Open(); var s = await context.Students.SingleAsync(x => x.Id == id); return new() { Id = id, Number = s.Number, Name = s.Name, Email = s.Email, Department = s.Department, Year = s.Year, Status = s.Status, Version = Convert.ToBase64String(s.RowVersion) }; }
        async Task<CourseForm> CourseEdit(int id) { await using var context = Open(); var c = await context.Courses.SingleAsync(x => x.Id == id); return new() { Id = id, Code = c.Code, Title = c.Title, Department = c.Department, Credits = c.Credits, Capacity = c.Capacity, Status = c.Status, Version = Convert.ToBase64String(c.RowVersion) }; }
        async Task<EnrollmentForm> EnrollmentEdit(int id) { await using var context = Open(); var e = await context.Enrollments.SingleAsync(x => x.Id == id); return new() { Id = id, StudentId = e.StudentId, CourseId = e.CourseId, Term = e.Term, AcademicYear = e.AcademicYear, Status = e.Status, Grade = e.Grade, EnrolledOn = e.EnrolledOn, Version = Convert.ToBase64String(e.RowVersion) }; }
        async Task<bool> Constraint(Func<AcademicDbContext, Task> operation) { await using var context = Open(); try { await operation(context); return false; } catch (DbUpdateException) { return true; } }
        var config = new ConfigurationBuilder().Build();
        Console.WriteLine("Verification uses an isolated disposable LocalDB database: " + builder.InitialCatalog);
        await using (var checkConnection = new SqlConnection(new SqlConnectionStringBuilder(connection) { InitialCatalog = "master" }.ConnectionString))
        {
            await checkConnection.OpenAsync(); await using var command = checkConnection.CreateCommand(); command.CommandText = "SELECT DB_ID(@name)"; command.Parameters.AddWithValue("@name", builder.InitialCatalog);
            if (await command.ExecuteScalarAsync() is not DBNull) throw new InvalidOperationException("Verification database already exists; refusing to reuse it.");
        }
        try
        {
            await using (var db = Open())
            {
                await db.Database.MigrateAsync(); Check(!(await db.Database.GetPendingMigrationsAsync()).Any(), "SQL migrations applied to isolated database");
                Check(await DevelopmentSeed.ApplyAsync(db), "Fictional seed inserts into an empty database");
                Check(!await DevelopmentSeed.ApplyAsync(db), "Seed is idempotent and preserves existing records");
            }
            await using (var db = Open()) { var data = new DatabaseAcademicData(db, config); DemoVerification.Run(data); Check(data.Students.Count == 48 && data.Enrollments.Count == 158, "Actual SQL seed metrics reconcile"); }
            Check(!(await Write(w => w.SaveStudent(new() { Number = "INVALID", Name = "", Email = "bad", Department = "Other", Year = 9 }))).Success, "Server validation rejects invalid student");
            var s1 = await Write(w => w.SaveStudent(new() { Number = "QA-ONE", Name = "QA One", Email = "qa.one@example.test", Department = "Business", Year = 1 }));
            var s2 = await Write(w => w.SaveStudent(new() { Number = "QA-TWO", Name = "QA Two", Email = "qa.two@example.test", Department = "Business", Year = 2 }));
            Check(s1.Success && s2.Success, "Student create persists to SQL");
            Check(!(await Write(w => w.SaveStudent(new() { Number = "qa-one", Name = "Duplicate", Email = "unique@example.test" }))).Success, "Duplicate student ID rejected case insensitively");
            var student = await StudentEdit(s1.Id); var stale = await StudentEdit(s1.Id); student.Name = "QA One Updated";
            Check((await Write(w => w.SaveStudent(student))).Success, "Student update persists");
            Check(!(await Write(w => w.SaveStudent(stale))).Success, "Stale student edit rejected");
            await using (var fresh = Open()) Check((await fresh.Students.SingleAsync(x => x.Id == s1.Id)).Name == "QA One Updated", "Persistence survives new DbContext / connection");
            Check(!(await Write(w => w.SaveCourse(new() { Code = "BAD", Title = "Invalid capacity", Capacity = 0 }))).Success, "Course capacity validation");
            var course = await Write(w => w.SaveCourse(new() { Code = "QA101", Title = "Concurrency Lab", Department = "Business", Capacity = 1, Credits = 3 }));
            Check(course.Success, "Course create persists"); var shortCode = await Write(w => w.SaveCourse(new() { Code = "Q", Title = "Single character code", Capacity = 1 })); await using (var shortDb = Open()) { var projection = new DatabaseAcademicData(shortDb, config); Check(shortCode.Success && projection.CourseRow(projection.Courses.Single(x => x.Id == shortCode.Id)).Initials == "Q", "Valid single-character course code renders safely"); }
            var shortForm = await CourseEdit(shortCode.Id); Check((await Write(w => w.Delete("Courses", shortCode.Id, shortForm.Version))).Success, "Single-character test course removed");
            Check(!(await Write(w => w.SaveCourse(new() { Code = "qa101", Title = "Duplicate code" }))).Success, "Duplicate course code rejected");
            var c = await CourseEdit(course.Id); c.Title = "Concurrency Lab Updated";
            Check((await Write(w => w.SaveCourse(c))).Success, "Course update persists");
            EnrollmentForm Enrollment(int studentId) => new() { StudentId = studentId, CourseId = course.Id, Term = "Fall", AcademicYear = 2026, EnrolledOn = new(2026, 10, 1) };
            var race = await Task.WhenAll(Write(w => w.SaveEnrollment(Enrollment(s1.Id))), Write(w => w.SaveEnrollment(Enrollment(s2.Id))));
            Check(race.Count(r => r.Success) == 1, "Concurrent enrollment attempts cannot exceed one remaining seat");
            var winner = race.Single(r => r.Success); var enrollment = await EnrollmentEdit(winner.Id);
            Check(!(await Write(w => w.SaveEnrollment(Enrollment(enrollment.StudentId)))).Success, "Duplicate student/course/term enrollment rejected");
            var inactive = await StudentEdit(enrollment.StudentId); inactive.Status = "On leave";
            Check(!(await Write(w => w.SaveStudent(inactive))).Success, "Active enrollments block inactive student status");
            c = await CourseEdit(course.Id); c.Status = "Archived";
            Check(!(await Write(w => w.SaveCourse(c))).Success, "Active enrollments block course archive");
            Check(!(await Write(w => w.Delete("Students", inactive.Id, inactive.Version))).Success, "Student with dependent enrollment cannot be deleted");
            Check(!(await Write(w => w.Delete("Courses", c.Id, c.Version))).Success, "Course with dependent enrollment cannot be deleted");
            Check(!(await Write(w => w.Delete("Enrollments", enrollment.Id, enrollment.Version))).Success, "Enrolled record must be withdrawn before removal");
            enrollment.Status = "Completed";
            Check(!(await Write(w => w.SaveEnrollment(enrollment))).Success, "Completed status requires grade");
            int historicalId; await using (var db = Open()) historicalId = await db.Enrollments.Where(e => e.Status == "Enrolled" && e.CourseId != course.Id).Select(e => e.Id).FirstAsync();
            var history = await EnrollmentEdit(historicalId); history.Status = "Completed"; history.Grade = "A";
            Check((await Write(w => w.SaveEnrollment(history))).Success, "Completion and grade recording persist");
            history = await EnrollmentEdit(historicalId); history.Grade = "B+";
            Check((await Write(w => w.SaveEnrollment(history))).Success, "Grade update persists");
            history = await EnrollmentEdit(historicalId);
            Check(!(await Write(w => w.Delete("Enrollments", history.Id, history.Version))).Success, "Completed enrollment cannot be removed");
            history.Status = "Withdrawn"; history.Grade = null;
            Check(!(await Write(w => w.SaveEnrollment(history))).Success, "Completed history cannot be withdrawn to bypass retention");
            enrollment = await EnrollmentEdit(winner.Id); enrollment.Status = "Withdrawn"; enrollment.Grade = null;
            Check((await Write(w => w.SaveEnrollment(enrollment))).Success, "Withdrawal persists and releases capacity");
            await using (var db = Open()) { var data = new DatabaseAcademicData(db, config); Check(data.Occupied(course.Id) == 0, "Dashboard occupancy reflects withdrawal"); }
            var other = enrollment.StudentId == s1.Id ? s2.Id : s1.Id;
            var second = await Write(w => w.SaveEnrollment(Enrollment(other)));
            Check(second.Success, "Released seat can be occupied");
            var withdrawn = await EnrollmentEdit(winner.Id);
            Check(!(await Write(w => w.SaveEnrollment(new() { Id = withdrawn.Id, StudentId = withdrawn.StudentId, CourseId = withdrawn.CourseId, Term = withdrawn.Term, AcademicYear = withdrawn.AcademicYear, Status = "Enrolled", EnrolledOn = withdrawn.EnrolledOn, Version = withdrawn.Version }))).Success, "Reactivation checks capacity again");
            Check(await Constraint(async db => { db.Students.Add(new(0, "QA-ONE", "Constraint Test", "constraint@example.test", "Business", 1, "Active")); await db.SaveChangesAsync(); }), "SQL unique index blocks duplicate student number");
            Check(await Constraint(async db => { db.Courses.Add(new(0, "QA101", "Constraint Test", "Business", 3, 1, "Active")); await db.SaveChangesAsync(); }), "SQL unique index blocks duplicate course code");
            Check(await Constraint(async db => { db.Enrollments.Add(new(0, other, course.Id, "Fall", 2026, "Enrolled", null, new(2026, 10, 1))); await db.SaveChangesAsync(); }), "SQL unique index blocks duplicate enrollment");
            Check(await Constraint(async db => { db.Enrollments.Add(new(0, int.MaxValue, course.Id, "Spring", 2026, "Enrolled", null, new(2026, 2, 1))); await db.SaveChangesAsync(); }), "SQL foreign key rejects missing student");
            Check(await Constraint(async db => { db.Courses.Add(new(0, "QA-BAD", "Constraint Test", "Business", 3, 501, "Active")); await db.SaveChangesAsync(); }), "SQL check constraint enforces capacity range");
            Check(await Constraint(async db => { var parent = await db.Courses.FindAsync(course.Id); db.Remove(parent!); await db.SaveChangesAsync(); }), "SQL restricted foreign key blocks parent deletion");
            await using (var a = Open()) await using (var b = Open())
            {
                var first = await a.Students.SingleAsync(s => s.Id == s1.Id); var secondCopy = await b.Students.SingleAsync(s => s.Id == s1.Id);
                first.Name = "Concurrency A"; await a.SaveChangesAsync(); secondCopy.Name = "Concurrency B"; var conflict = false;
                try { await b.SaveChangesAsync(); } catch (DbUpdateConcurrencyException) { conflict = true; }
                Check(conflict, "SQL rowversion enforces optimistic concurrency");
            }
            var secondEdit = await EnrollmentEdit(second.Id); secondEdit.Status = "Withdrawn";
            Check((await Write(w => w.SaveEnrollment(secondEdit))).Success, "Second withdrawal persists");
            foreach (var id in new[] { winner.Id, second.Id }) { var e = await EnrollmentEdit(id); Check((await Write(w => w.Delete("Enrollments", id, e.Version))).Success, "Withdrawn enrollment deletion persists"); }
            foreach (var id in new[] { s1.Id, s2.Id }) { var s = await StudentEdit(id); Check((await Write(w => w.Delete("Students", id, s.Version))).Success, "Unreferenced student deletion persists"); }
            c = await CourseEdit(course.Id); Check((await Write(w => w.Delete("Courses", course.Id, c.Version))).Success, "Unreferenced course deletion persists");
            var partial = await Write(w => w.SaveStudent(new() { Number = "QA-PRESERVE", Name = "Preserve Record", Email = "preserve@example.test" }));
            await using (var db = Open()) { Check(!await DevelopmentSeed.ApplyAsync(db) && await db.Students.AnyAsync(s => s.Id == partial.Id), "Seed never overwrites existing academic data"); }
            var ctx = new DefaultHttpContext(); ctx.Request.Scheme = "http"; ctx.Request.Host = new("localhost", 5080); ctx.Connection.LocalIpAddress = IPAddress.Loopback; ctx.Connection.RemoteIpAddress = IPAddress.Loopback;
            Check(LocalWriteAccess.IsAllowed(true, true, ctx), "Explicit loopback development write permitted");
            Check(!LocalWriteAccess.IsAllowed(false, true, ctx), "Production writes fail closed even if enable flag is true");
            Check(!LocalWriteAccess.IsAllowed(true, false, ctx), "Disabled development writes rejected");
            ctx.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.4"); Check(!LocalWriteAccess.IsAllowed(true, true, ctx), "Non-loopback remote client rejected"); ctx.Connection.RemoteIpAddress = IPAddress.Loopback;
            ctx.Request.Host = new("public.example.test", 5080); Check(!LocalWriteAccess.IsAllowed(true, true, ctx), "Public Host rejected"); ctx.Request.Host = new("localhost", 5080);
            ctx.Request.Headers["X-Forwarded-For"] = "127.0.0.1"; Check(!LocalWriteAccess.IsAllowed(true, true, ctx), "Forwarded client headers rejected"); ctx.Request.Headers.Clear();
            ctx.Request.Headers.Origin = "https://external.example.test"; Check(!LocalWriteAccess.IsAllowed(true, true, ctx), "Cross-origin write rejected");
            Console.WriteLine($"VERIFIED {checks} SQL integration/security checks, plus 22 dataset/query checks.");
        }
        finally
        {
            // Only this run's freshly generated verification database is disposable.
            if (!builder.InitialCatalog.StartsWith("CampusCore_Verify_", StringComparison.Ordinal) || builder.InitialCatalog.Length != "CampusCore_Verify_".Length + 32) throw new InvalidOperationException("Unsafe verification cleanup rejected.");
            DatabaseSafety.RequireLocal(builder.ConnectionString);
            await using var cleanup = Open(); await cleanup.Database.EnsureDeletedAsync();
            Console.WriteLine("Removed isolated verification database; CampusCoreDevelopment was not changed by these integration tests.");
        }
    }
}
