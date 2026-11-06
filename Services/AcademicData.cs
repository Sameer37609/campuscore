using CampusCore.Models;
namespace CampusCore.Services;

public interface IAcademicData
{
    IReadOnlyList<Student> Students { get; }
    IReadOnlyList<Course> Courses { get; }
    IReadOnlyList<Enrollment> Enrollments { get; }
    string CurrentTerm { get; }
    int CurrentYear { get; }
}
// Deterministic fictional fixture used only for explicit SQL seeding and PublicDemo request scopes.
public sealed class DemoAcademicData : IAcademicData
{
    public string CurrentTerm => "Fall";
    public int CurrentYear => 2026;
    public IReadOnlyList<Student> Students { get; }
    public IReadOnlyList<Course> Courses { get; }
    public IReadOnlyList<Enrollment> Enrollments { get; }
    public DemoAcademicData()
    {
        string[] names = ["Avery Linden", "Mira Ashford", "Noah Vale", "Sana Westbrook", "Elias Rowan", "Lena Hartwell", "Theo Marlow", "Zoya Ellery", "Arlo Bennett", "Isla Wren", "Rayan Oakley", "Nora Sterling", "Leo Fenwick", "Amara Finch", "Finn Calder", "Esme Hollis", "Idris Bellamy", "Maya Alden", "Oscar Emery", "Lila Winslow", "Adam Kestrel", "Freya Monroe", "Omar Langley", "Eva Roswell", "Jude Briar", "Anya Whitmore", "Sami Norwood", "Iris Halden", "Ezra Lockwood", "Rhea Fairchild", "Dani Mercer", "Ines Corbin", "Kian Elwood", "Alma Voss", "Remy Hawthorne", "Tara Lennox", "Hugo Alder", "Nila Arden", "Otis Waverly", "Yara Selwyn", "Luca Huxley", "Asha Ridley", "Zain Beaumont", "Cleo Ainsley", "Milo Carver", "Thea Roslyn", "Ilan Sorrell", "Ayla Everly"];
        string[] departments = ["Computer Science", "Business", "Engineering", "Design"];
        Students = Array.AsReadOnly(names.Select((n, i) => new Student(i + 1, $"ST-{2026 - (i / 4) % 4}-{i + 1:000}", n, n.ToLowerInvariant().Replace(' ', '.') + "@students.example.edu", departments[i % 4], (i / 4) % 4 + 1, i % 13 == 0 ? "On leave" : i % 11 == 0 ? "Graduated" : "Active")).ToArray());
        string[] titles = ["Data Structures", "Principles of Marketing", "Applied Mechanics", "Visual Communication", "Database Systems", "Financial Accounting", "Circuit Analysis", "Interaction Design", "Software Engineering", "Business Analytics", "Sustainable Engineering", "Design Research"];
        Courses = Array.AsReadOnly(titles.Select((n, i) => new Course(i + 1, new[] { "CS", "BUS", "ENG", "DES" }[i % 4] + (201 + i / 4 * 100), n, departments[i % 4], i % 3 == 0 ? 4 : 3, 14 + i % 3 * 4, i == 11 ? "Archived" : "Active")).ToArray());
        var records = new List<Enrollment>();
        foreach (var s in Students)
        {
            var own = Courses.Where(c => c.Department == s.Department).ToArray();
            // Completed historical term: also supplies the first three months of the trend.
            records.Add(new(records.Count + 1, s.Id, own[0].Id, "Spring", 2026, "Completed", new[] { "A", "B+", "B", "A-" }[s.Id % 4], new DateOnly(2026, 1 + (s.Id % 3), 2 + s.Id % 24)));
            if (s.Status != "Active") continue;
            foreach (var c in own.Where(c => c.Status == "Active"))
                records.Add(new(records.Count + 1, s.Id, c.Id, "Fall", 2026, s.Id % 17 == 0 ? "Withdrawn" : "Enrolled", null, new DateOnly(2026, 8 + s.Id % 3, 1 + (s.Id + c.Id) % 27)));
        }
        Enrollments = records.AsReadOnly();
    }
}
public static class AcademicQueries
{
    public static int Occupied(this IAcademicData d, int courseId) => d.Enrollments.Count(e => e.CourseId == courseId && e.Term == d.CurrentTerm && e.AcademicYear == d.CurrentYear && e.Status == "Enrolled");
    public static DirectoryRow StudentRow(this IAcademicData d, Student s) => new(s.Number, s.Name, s.Email, s.Department, $"Year {s.Year}", s.Status, $"{d.Enrollments.Count(e => e.StudentId == s.Id && e.Status == "Enrolled")} active courses", $"/Students/Details/{s.Id}", s.Initials);
    public static DirectoryRow CourseRow(this IAcademicData d, Course c) => new(c.Code, c.Title, $"{c.Credits} credit hours", c.Department, $"{d.CurrentTerm} {d.CurrentYear}", c.Status, $"{d.Occupied(c.Id)} / {c.Capacity} seats", $"/Courses/Details/{c.Id}", c.Code[..Math.Min(2, c.Code.Length)], d.Occupied(c.Id), c.Capacity);
    public static DirectoryRow EnrollmentRow(this IAcademicData d, Enrollment e)
    {
        var s = d.Students.Single(s => s.Id == e.StudentId); var c = d.Courses.Single(c => c.Id == e.CourseId);
        return new($"EN-{e.Id:0000}", s.Name, $"{c.Code} · {c.Title}", c.Department, $"{e.Term} {e.AcademicYear}", e.Status, e.Grade is null ? (e.Status == "Withdrawn" ? "Not applicable" : "Grade pending") : $"Grade {e.Grade}", $"/Enrollments/Details/{e.Id}", s.Initials);
    }
    public static DirectoryPage Directory(this IAcademicData d, DirectoryPage p)
    {
        var all = (p.Kind switch { "Students" => d.Students.Select(d.StudentRow), "Courses" => d.Courses.Select(d.CourseRow), _ => d.Enrollments.Select(d.EnrollmentRow) }).ToArray();
        p.AllCount = all.Length;
        p.Departments = all.Select(r => r.Department).Distinct().Order().ToArray();
        p.Statuses = all.Select(r => r.Status).Distinct().Order().ToArray();
        p.Years = all.Select(r => r.Year).Distinct().Order().ToArray();
        IEnumerable<DirectoryRow> rows = all;
        if (!string.IsNullOrWhiteSpace(p.Q)) rows = rows.Where(r => $"{r.Id} {r.Title} {r.Subtitle}".Contains(p.Q.Trim(), StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrEmpty(p.Department)) rows = rows.Where(r => r.Department == p.Department);
        if (!string.IsNullOrEmpty(p.Status)) rows = rows.Where(r => r.Status == p.Status);
        if (!string.IsNullOrEmpty(p.Year)) rows = rows.Where(r => r.Year == p.Year);
        rows = p.Sort switch { "name-desc" => rows.OrderByDescending(r => r.Title).ThenBy(r => r.Id), "id" => rows.OrderBy(r => r.Id), "department" => rows.OrderBy(r => r.Department).ThenBy(r => r.Title).ThenBy(r => r.Id), _ => rows.OrderBy(r => r.Title).ThenBy(r => r.Id) };
        p.Total = rows.Count(); p.Page = Math.Clamp(p.Page, 1, p.Pages); p.Rows = rows.Skip((p.Page - 1) * 10).Take(10).ToArray(); return p;
    }
}
