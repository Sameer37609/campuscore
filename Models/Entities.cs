namespace CampusCore.Models;

public class Student
{
    public Student() { }
    public Student(int id, string number, string name, string email, string department, int year, string status)
    {
        Id = id; Number = number; Name = name; Email = email; Department = department; Year = year; Status = status;
    }
    public int Id { get; set; }
    public string Number { get; set; } = "";
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Department { get; set; } = "";
    public int Year { get; set; }
    public string Status { get; set; } = "Active";
    public byte[] RowVersion { get; set; } = [];
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public string Initials => string.Concat(Name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(n => n[0]).Take(2));
}
public class Course
{
    public Course() { }
    public Course(int id, string code, string title, string department, int credits, int capacity, string status)
    {
        Id = id; Code = code; Title = title; Department = department; Credits = credits; Capacity = capacity; Status = status;
    }
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Title { get; set; } = "";
    public string Department { get; set; } = "";
    public int Credits { get; set; }
    public int Capacity { get; set; }
    public string Status { get; set; } = "Active";
    public byte[] RowVersion { get; set; } = [];
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
}
public class Enrollment
{
    public Enrollment() { }
    public Enrollment(int id, int studentId, int courseId, string term, int academicYear, string status, string? grade, DateOnly enrolledOn)
    {
        Id = id; StudentId = studentId; CourseId = courseId; Term = term; AcademicYear = academicYear; Status = status; Grade = grade; EnrolledOn = enrolledOn;
    }
    public int Id { get; set; }
    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public string Term { get; set; } = "Fall";
    public int AcademicYear { get; set; }
    public string Status { get; set; } = "Enrolled";
    public string? Grade { get; set; }
    public DateOnly EnrolledOn { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
