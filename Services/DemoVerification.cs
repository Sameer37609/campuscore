using CampusCore.Models;
namespace CampusCore.Services;

public static class DemoVerification
{
    public static void Run(IAcademicData d)
    {
        var count = 0;
        void Check(bool ok, string name) { if (!ok) throw new InvalidOperationException("FAIL: " + name); Console.WriteLine("PASS: " + name); count++; }
        Check(d.Students.Count == 48 && d.Courses.Count == 12, "Seed counts: 48 students and 12 courses");
        Check(d.Students.Select(s => s.Id).Distinct().Count() == d.Students.Count && d.Students.Select(s => s.Email).Distinct().Count() == d.Students.Count, "Unique student IDs and emails");
        Check(d.Courses.Select(c => c.Id).Distinct().Count() == d.Courses.Count && d.Courses.Select(c => c.Code).Distinct().Count() == d.Courses.Count, "Unique course IDs and codes");
        Check(d.Enrollments.Select(e => e.Id).Distinct().Count() == d.Enrollments.Count, "Unique enrollment IDs");
        Check(d.Enrollments.All(e => d.Students.Any(s => s.Id == e.StudentId) && d.Courses.Any(c => c.Id == e.CourseId)), "Enrollment foreign keys");
        Check(d.Enrollments.GroupBy(e => new { e.StudentId, e.CourseId, e.Term, e.AcademicYear }).All(g => g.Count() == 1), "Unique student/course/term/year");
        Check(d.Courses.All(c => d.Occupied(c.Id) <= c.Capacity), "No course over capacity");
        Check(d.Enrollments.Where(e => e.Status == "Enrolled").All(e => d.Students.Single(s => s.Id == e.StudentId).Status == "Active" && d.Courses.Single(c => c.Id == e.CourseId).Status == "Active"), "Only active students and courses enrolled");
        Check(d.Enrollments.All(e => (e.Status == "Completed") == (e.Grade is not null)), "Grade/status consistency");
        Check(d.Enrollments.GroupBy(e => e.EnrolledOn.Month).Sum(g => g.Count()) == d.Enrollments.Count, "Trend total reconciles");
        Check(d.Students.GroupBy(s => s.Department).Sum(g => g.Count()) == 48, "Department total reconciles");
        Check(d.Courses.Sum(c => d.Occupied(c.Id)) == d.Enrollments.Count(e => e.Term == d.CurrentTerm && e.AcademicYear == d.CurrentYear && e.Status == "Enrolled"), "Capacity total reconciles");
        DirectoryPage Q(string kind, string? q = null, string? dep = null, string? status = null, string? year = null, string sort = "name", int page = 1) => d.Directory(new DirectoryPage { Kind = kind, Q = q, Department = dep, Status = status, Year = year, Sort = sort, Page = page });
        Check(Q("Students", "mIrA").Rows.Single().Title == "Mira Ashford", "Case insensitive student search");
        Check(Q("Students", dep: "Business", status: "Active", year: "Year 2").Rows.All(r => r.Department == "Business" && r.Status == "Active" && r.Year == "Year 2"), "Combined student filters");
        Check(Q("Students", sort: "name-desc").Rows.First().Title == d.Students.MaxBy(s => s.Name)!.Name, "Descending sort");
        Check(Q("Students", page: 2).Rows.Count == 10 && !Q("Students").Rows.Select(r => r.Id).Intersect(Q("Students", page: 2).Rows.Select(r => r.Id)).Any(), "Pagination has distinct records");
        Check(Q("Students", page: -50).Page == 1 && Q("Students", page: int.MaxValue).Page == 5, "Invalid pages are clamped");
        Check(Q("Students", "zzzz-no-match").Total == 0, "Empty search results");
        Check(Q("Courses", "CS201").Rows.Single().Title == "Data Structures", "Course code search");
        Check(Q("Courses", status: "Archived").Total == 1, "Archived course filter");
        Check(Q("Enrollments", status: "Completed", year: "Spring 2026").Total == 48, "Term/year/status filters");
        Check(Q("Enrollments", "Mira").Rows.All(r => r.Title == "Mira Ashford") && Q("Enrollments", "CS201").Total > 0, "Enrollment student and course search");
        Console.WriteLine($"VERIFIED {count} checks. Students={d.Students.Count}; ActiveCourses={d.Courses.Count(c => c.Status == "Active")}; Enrollments={d.Enrollments.Count}; Occupied={d.Courses.Sum(c => d.Occupied(c.Id))}; Seats={d.Courses.Where(c => c.Status == "Active").Sum(c => c.Capacity)}");
    }
}
