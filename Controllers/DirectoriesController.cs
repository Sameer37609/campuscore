using CampusCore.Models;
using CampusCore.Services;
using Microsoft.AspNetCore.Mvc;
namespace CampusCore.Controllers;

public abstract class AcademicController(IAcademicData data) : Controller
{
    protected IAcademicData Data => data;
    protected IActionResult Listing(string kind, string? q, string? department, string? status, string? year, string sort, int page) => View("~/Views/Academic/Directory.cshtml", data.Directory(new DirectoryPage { Kind = kind, Q = q, Department = department, Status = status, Year = year, Sort = sort, Page = page }));
    protected IActionResult Detail(DetailPage model) => View("~/Views/Academic/Details.cshtml", model);
    protected static KeyValuePair<string, string> Fact(string name, object value) => new(name, value.ToString() ?? "—");
}
public class StudentsController(IAcademicData data) : AcademicController(data)
{
    [HttpGet] public IActionResult Index(string? q, string? department, string? status, string? year, string sort = "name", int page = 1) => Listing("Students", q, department, status, year, sort, page);
    [HttpGet]
    public IActionResult Details(int id)
    {
        var s = Data.Students.SingleOrDefault(s => s.Id == id); if (s is null) return NotFound();
        return Detail(new("Student profile", s.Name, s.Number, s.Status, [Fact("Email", s.Email), Fact("Department", s.Department), Fact("Academic year", $"Year {s.Year}"), Fact("Active courses", Data.Enrollments.Count(e => e.StudentId == id && e.Status == "Enrolled"))], Data.Enrollments.Where(e => e.StudentId == id).Select(Data.EnrollmentRow).ToArray(), "Enrollment history", "/Students"));
    }
}
public class CoursesController(IAcademicData data) : AcademicController(data)
{
    [HttpGet] public IActionResult Index(string? q, string? department, string? status, string? year, string sort = "name", int page = 1) => Listing("Courses", q, department, status, year, sort, page);
    [HttpGet]
    public IActionResult Details(int id)
    {
        var c = Data.Courses.SingleOrDefault(c => c.Id == id); if (c is null) return NotFound();
        return Detail(new("Course overview", c.Title, c.Code, c.Status, [Fact("Department", c.Department), Fact("Credit hours", c.Credits), Fact("Current term", $"{Data.CurrentTerm} {Data.CurrentYear}"), Fact("Current capacity", $"{Data.Occupied(id)} of {c.Capacity} seats")], Data.Enrollments.Where(e => e.CourseId == id).Select(Data.EnrollmentRow).ToArray(), "Enrollment history · all terms", "/Courses"));
    }
}
public class EnrollmentsController(IAcademicData data) : AcademicController(data)
{
    [HttpGet] public IActionResult Index(string? q, string? department, string? status, string? year, string sort = "id", int page = 1) => Listing("Enrollments", q, department, status, year, sort, page);
    [HttpGet]
    public IActionResult Details(int id)
    {
        var e = Data.Enrollments.SingleOrDefault(e => e.Id == id); if (e is null) return NotFound();
        var s = Data.Students.Single(s => s.Id == e.StudentId); var c = Data.Courses.Single(c => c.Id == e.CourseId);
        return Detail(new("Enrollment record", $"EN-{e.Id:0000}", $"{s.Name} · {c.Code}", e.Status, [Fact("Academic term", e.Term), Fact("Academic year", e.AcademicYear), Fact("Enrolled on", e.EnrolledOn.ToString("dd MMM yyyy")), Fact("Recorded grade", e.Grade ?? (e.Status == "Withdrawn" ? "Not applicable" : "Not yet recorded"))], [Data.StudentRow(s), Data.CourseRow(c)], "Linked academic records", "/Enrollments"));
    }
}
