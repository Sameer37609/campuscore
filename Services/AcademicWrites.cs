using System.ComponentModel.DataAnnotations;
using System.Data;
using CampusCore.Data;
using CampusCore.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
namespace CampusCore.Services;

public record WriteResult(bool Success, int Id = 0, string? Error = null, bool Missing = false);
public sealed class AcademicWrites(AcademicDbContext db, ILogger<AcademicWrites> logger)
{
    private sealed class RuleException(string message) : Exception(message);
    private static void Require(bool condition, string message) { if (!condition) throw new RuleException(message); }
    private static void Validate(object model)
    {
        var errors = new List<ValidationResult>();
        Require(Validator.TryValidateObject(model, new ValidationContext(model), errors, true), string.Join(" ", errors.Select(e => e.ErrorMessage)));
    }
    private static void Version(byte[] current, string expected)
    {
        Require(!string.IsNullOrEmpty(expected), "This form is missing its record version. Reload the record.");
        Require(Convert.TryFromBase64String(expected, new Span<byte>(new byte[32]), out var count) && count == current.Length, "Invalid record version. Reload the record.");
        Require(current.SequenceEqual(Convert.FromBase64String(expected)), "This record changed while the form was open. Reload it before trying again.");
    }
    private async Task<WriteResult> Execute(Func<Task<int>> action)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            await AcademicLock.AcquireAsync(db);
            var id = await action(); await db.SaveChangesAsync(); await tx.CommitAsync(); return new(true, id);
        }
        catch (RuleException e) { return new(false, Error: e.Message); }
        catch (KeyNotFoundException) { return new(false, Error: "This record no longer exists.", Missing: true); }
        catch (DbUpdateConcurrencyException) { return new(false, Error: "This record changed. Reload it before trying again."); }
        catch (DbUpdateException e) { logger.LogWarning("Academic write rejected: {Type}", e.InnerException?.GetType().Name); return new(false, Error: "The change conflicts with an existing record or relationship. Check the values and reload the record."); }
        catch (SqlException e) { logger.LogWarning("Academic write failed with SQL code {Code}", e.Number); return new(false, Error: "The database is busy or unavailable. Please try again shortly."); }
        finally { db.ChangeTracker.Clear(); }
    }
    public Task<WriteResult> SaveStudent(StudentForm f) => Execute(async () =>
    {
        Validate(f); var number = f.Number.Trim().ToUpperInvariant(); var email = f.Email.Trim().ToLowerInvariant();
        Require(!await db.Students.AnyAsync(s => s.Id != f.Id && (s.Number == number || s.Email == email)), "Student ID and email must both be unique.");
        var s = f.Id == 0 ? new Student() : await db.Students.FindAsync(f.Id) ?? throw new KeyNotFoundException();
        if (f.Id != 0) { Version(s.RowVersion, f.Version); Require(f.Status == "Active" || !await db.Enrollments.AnyAsync(e => e.StudentId == f.Id && e.Status == "Enrolled"), "Withdraw or complete this student's active enrollments before changing their status."); }
        s.Number = number; s.Name = f.Name.Trim(); s.Email = email; s.Department = f.Department; s.Year = f.Year; s.Status = f.Status;
        if (f.Id == 0) db.Students.Add(s); await db.SaveChangesAsync(); return s.Id;
    });
    public Task<WriteResult> SaveCourse(CourseForm f) => Execute(async () =>
    {
        Validate(f); var code = f.Code.Trim().ToUpperInvariant();
        Require(!await db.Courses.AnyAsync(c => c.Id != f.Id && c.Code == code), "Course code must be unique.");
        var c = f.Id == 0 ? new Course() : await db.Courses.FindAsync(f.Id) ?? throw new KeyNotFoundException();
        if (f.Id != 0)
        {
            Version(c.RowVersion, f.Version);
            var loads = await db.Enrollments.Where(e => e.CourseId == f.Id && e.Status == "Enrolled").GroupBy(e => new { e.Term, e.AcademicYear }).Select(g => g.Count()).ToArrayAsync();
            Require(loads.All(n => n <= f.Capacity), "Capacity cannot be lower than the enrolled count in any term.");
            Require(f.Status == "Active" || loads.Length == 0, "Withdraw or complete active enrollments before archiving this course.");
        }
        c.Code = code; c.Title = f.Title.Trim(); c.Department = f.Department; c.Credits = f.Credits; c.Capacity = f.Capacity; c.Status = f.Status;
        if (f.Id == 0) db.Courses.Add(c); await db.SaveChangesAsync(); return c.Id;
    });
    public Task<WriteResult> SaveEnrollment(EnrollmentForm f) => Execute(async () =>
    {
        Validate(f);
        var s = await db.Students.FindAsync(f.StudentId) ?? throw new RuleException("Choose an existing student.");
        var c = await db.Courses.FindAsync(f.CourseId) ?? throw new RuleException("Choose an existing course.");
        Require(!await db.Enrollments.AnyAsync(e => e.Id != f.Id && e.StudentId == f.StudentId && e.CourseId == f.CourseId && e.Term == f.Term && e.AcademicYear == f.AcademicYear), "This student already has an enrollment for that course and term. Update the existing record.");
        var e = f.Id == 0 ? new Enrollment() : await db.Enrollments.FindAsync(f.Id) ?? throw new KeyNotFoundException();
        if (f.Id != 0) { Version(e.RowVersion, f.Version); Require(e.StudentId == f.StudentId && e.CourseId == f.CourseId && e.Term == f.Term && e.AcademicYear == f.AcademicYear, "Student, course and academic term cannot be changed on an existing enrollment."); }
        Require(e.Id == 0 || e.Status != "Completed" || f.Status == "Completed", "Completed enrollment history is retained. You can correct its grade, but cannot reopen or withdraw it.");
        if (f.Status == "Enrolled")
        {
            Require(s.Status == "Active" && c.Status == "Active", "Only active students can enroll in active courses.");
            var count = await db.Enrollments.CountAsync(x => x.Id != f.Id && x.CourseId == f.CourseId && x.Term == f.Term && x.AcademicYear == f.AcademicYear && x.Status == "Enrolled");
            Require(count < c.Capacity, "This course has reached capacity for the selected term.");
        }
        e.StudentId = f.StudentId; e.CourseId = f.CourseId; e.Term = f.Term; e.AcademicYear = f.AcademicYear; e.EnrolledOn = f.EnrolledOn; e.Status = f.Status; e.Grade = string.IsNullOrEmpty(f.Grade) ? null : f.Grade;
        if (f.Id == 0) db.Enrollments.Add(e); await db.SaveChangesAsync(); return e.Id;
    });
    public Task<WriteResult> Delete(string kind, int id, string version) => Execute(async () =>
    {
        if (kind == "Students") { var s = await db.Students.FindAsync(id) ?? throw new KeyNotFoundException(); Version(s.RowVersion, version); Require(!await db.Enrollments.AnyAsync(e => e.StudentId == id), "This student has enrollment history and cannot be deleted."); db.Remove(s); }
        else if (kind == "Courses") { var c = await db.Courses.FindAsync(id) ?? throw new KeyNotFoundException(); Version(c.RowVersion, version); Require(!await db.Enrollments.AnyAsync(e => e.CourseId == id), "This course has enrollment history and cannot be deleted."); db.Remove(c); }
        else if (kind == "Enrollments") { var e = await db.Enrollments.FindAsync(id) ?? throw new KeyNotFoundException(); Version(e.RowVersion, version); Require(e.Status == "Withdrawn", "Only withdrawn enrollments can be removed. Completed records are retained; withdraw an active enrollment first."); db.Remove(e); }
        else throw new RuleException("Unknown record type.");
        return id;
    });
}
