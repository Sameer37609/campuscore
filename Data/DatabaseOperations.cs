using CampusCore.Models;
using CampusCore.Services;
using Microsoft.EntityFrameworkCore;
using System.Data;
namespace CampusCore.Data;

public static class AcademicLock
{
    public static Task AcquireAsync(AcademicDbContext db, bool exclusive = true) => db.Database.ExecuteSqlRawAsync(exclusive ?
        "DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource=N'CampusCoreAcademicWrite', @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=15000; IF @r<0 THROW 51000, 'Academic data is busy. Try again.', 1;" :
        "DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource=N'CampusCoreAcademicWrite', @LockMode='Shared', @LockOwner='Transaction', @LockTimeout=15000; IF @r<0 THROW 51000, 'Academic data is busy. Try again.', 1;");
}
public static class DevelopmentSeed
{
    public static async Task<bool> ApplyAsync(AcademicDbContext db)
    {
        DatabaseSafety.RequireLocal(db.Database.GetConnectionString()!);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        await AcademicLock.AcquireAsync(db);
        if (await db.Students.AnyAsync() || await db.Courses.AnyAsync() || await db.Enrollments.AnyAsync())
        {
            await tx.CommitAsync(); Console.WriteLine("Seed skipped: existing academic records preserved."); return false;
        }
        var source = new DemoAcademicData();
        var students = source.Students.Select(s => new Student(0, s.Number, s.Name, s.Email, s.Department, s.Year, s.Status)).ToArray();
        var courses = source.Courses.Select(c => new Course(0, c.Code, c.Title, c.Department, c.Credits, c.Capacity, c.Status)).ToArray();
        db.AddRange(students); db.AddRange(courses); await db.SaveChangesAsync();
        var studentIds = source.Students.ToDictionary(s => s.Id, s => students.Single(x => x.Number == s.Number).Id);
        var courseIds = source.Courses.ToDictionary(c => c.Id, c => courses.Single(x => x.Code == c.Code).Id);
        db.AddRange(source.Enrollments.Select(e => new Enrollment(0, studentIds[e.StudentId], courseIds[e.CourseId], e.Term, e.AcademicYear, e.Status, e.Grade, e.EnrolledOn)));
        await db.SaveChangesAsync(); await tx.CommitAsync();
        Console.WriteLine("Fictional seed inserted: 48 students, 12 courses, 158 enrollments."); return true;
    }
}
public sealed class DatabaseAcademicData(AcademicDbContext db, IConfiguration config) : IAcademicData
{
    private Student[]? students;
    private Course[]? courses;
    private Enrollment[]? enrollments;
    public string CurrentTerm => config["Academic:CurrentTerm"] ?? "Fall";
    public int CurrentYear => config.GetValue("Academic:CurrentYear", 2026);
    public IReadOnlyList<Student> Students { get { Load(); return students!; } }
    public IReadOnlyList<Course> Courses { get { Load(); return courses!; } }
    public IReadOnlyList<Enrollment> Enrollments { get { Load(); return enrollments!; } }
    private void Load()
    {
        if (students is not null) return;
        // One consistent request snapshot. Shared transaction lock coordinates with
        // every write, so the three related queries cannot straddle a mutation.
        using var tx = db.Database.BeginTransaction(IsolationLevel.Serializable);
        AcademicLock.AcquireAsync(db, false).GetAwaiter().GetResult();
        var s = db.Students.AsNoTracking().OrderBy(x => x.Id).ToArray();
        var c = db.Courses.AsNoTracking().OrderBy(x => x.Id).ToArray();
        var e = db.Enrollments.AsNoTracking().OrderBy(x => x.Id).ToArray();
        tx.Commit(); students = s; courses = c; enrollments = e;
    }
}
