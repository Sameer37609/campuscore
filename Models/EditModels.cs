using System.ComponentModel.DataAnnotations;
namespace CampusCore.Models;

public static class AcademicOptions
{
    public static readonly string[] Departments = ["Business", "Computer Science", "Design", "Engineering"];
    public static readonly string[] StudentStatuses = ["Active", "On leave", "Graduated"];
    public static readonly string[] CourseStatuses = ["Active", "Archived"];
    public static readonly string[] EnrollmentStatuses = ["Enrolled", "Completed", "Withdrawn"];
    public static readonly string[] Terms = ["Spring", "Summer", "Fall", "Winter"];
    public static readonly string[] Grades = ["A", "A-", "B+", "B", "B-", "C+", "C", "C-", "D", "F"];
}
public abstract class AcademicForm
{
    public int Id { get; set; }
    [DisplayFormat(ConvertEmptyStringToNull = false)] public string Version { get; set; } = "";
}
public sealed class StudentForm : AcademicForm, IValidatableObject
{
    [Required, StringLength(30), RegularExpression(@"[A-Za-z0-9][A-Za-z0-9-]*", ErrorMessage = "Use letters, numbers and hyphens for the student ID."), Display(Name = "Student ID")]
    public string Number { get; set; } = "";
    [Required, StringLength(100), Display(Name = "Full name")] public string Name { get; set; } = "";
    [Required, StringLength(254), EmailAddress] public string Email { get; set; } = "";
    [Required] public string Department { get; set; } = "Computer Science";
    [Range(1, 4), Display(Name = "Academic year")] public int Year { get; set; } = 1;
    [Required] public string Status { get; set; } = "Active";
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (!AcademicOptions.Departments.Contains(Department)) yield return new("Choose a listed department.", [nameof(Department)]);
        if (!AcademicOptions.StudentStatuses.Contains(Status)) yield return new("Choose a listed status.", [nameof(Status)]);
    }
}
public sealed class CourseForm : AcademicForm, IValidatableObject
{
    [Required, StringLength(20), RegularExpression(@"[A-Za-z0-9][A-Za-z0-9-]*", ErrorMessage = "Use letters, numbers and hyphens for the course code."), Display(Name = "Course code")] public string Code { get; set; } = "";
    [Required, StringLength(120), Display(Name = "Course title")] public string Title { get; set; } = "";
    [Required] public string Department { get; set; } = "Computer Science";
    [Range(1, 6), Display(Name = "Credit hours")] public int Credits { get; set; } = 3;
    [Range(1, 500)] public int Capacity { get; set; } = 25;
    [Required] public string Status { get; set; } = "Active";
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (!AcademicOptions.Departments.Contains(Department)) yield return new("Choose a listed department.", [nameof(Department)]);
        if (!AcademicOptions.CourseStatuses.Contains(Status)) yield return new("Choose a listed status.", [nameof(Status)]);
    }
}
public sealed class EnrollmentForm : AcademicForm, IValidatableObject
{
    [Range(1, int.MaxValue), Display(Name = "Student")] public int StudentId { get; set; }
    [Range(1, int.MaxValue), Display(Name = "Course")] public int CourseId { get; set; }
    [Required, Display(Name = "Academic term")] public string Term { get; set; } = "Fall";
    [Range(2000, 2100), Display(Name = "Academic year")] public int AcademicYear { get; set; } = 2026;
    [Required] public string Status { get; set; } = "Enrolled";
    public string? Grade { get; set; }
    [Required, DataType(DataType.Date), Display(Name = "Enrollment date")] public DateOnly EnrolledOn { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (!AcademicOptions.Terms.Contains(Term)) yield return new("Choose a listed term.", [nameof(Term)]);
        if (!AcademicOptions.EnrollmentStatuses.Contains(Status)) yield return new("Choose a listed status.", [nameof(Status)]);
        if (Status == "Completed" && !AcademicOptions.Grades.Contains(Grade)) yield return new("Completed enrollments require a listed grade.", [nameof(Grade)]);
        if (Status != "Completed" && !string.IsNullOrEmpty(Grade)) yield return new("Only completed enrollments can have a grade. Clear the grade first.", [nameof(Grade)]);
        if (EnrolledOn.Year != AcademicYear) yield return new("Enrollment date must be within the academic year.", [nameof(EnrolledOn)]);
    }
}
public sealed class DeleteForm : AcademicForm
{
    public string Kind { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Restriction { get; set; }
}
