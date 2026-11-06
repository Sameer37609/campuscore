using CampusCore.Models;
using Microsoft.EntityFrameworkCore;
namespace CampusCore.Data;

public sealed class AcademicDbContext(DbContextOptions<AcademicDbContext> options) : DbContext(options)
{
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Student>(b =>
        {
            b.ToTable("Students", t =>
            {
                t.HasCheckConstraint("CK_Student_Year", "[Year] BETWEEN 1 AND 4");
                t.HasCheckConstraint("CK_Student_Status", "[Status] IN ('Active','On leave','Graduated')");
                t.HasCheckConstraint("CK_Student_Text", "LEN(LTRIM(RTRIM([Number]))) > 0 AND LEN(LTRIM(RTRIM([Name]))) > 0 AND LEN(LTRIM(RTRIM([Email]))) > 0");
                t.HasCheckConstraint("CK_Student_Department", "[Department] IN ('Business','Computer Science','Design','Engineering')");
            });
            b.HasKey(x => x.Id);
            b.Property(x => x.Number).HasMaxLength(30).IsRequired(); b.HasIndex(x => x.Number).IsUnique();
            b.Property(x => x.Name).HasMaxLength(100).IsRequired();
            b.Property(x => x.Email).HasMaxLength(254).IsRequired(); b.HasIndex(x => x.Email).IsUnique();
            b.Property(x => x.Department).HasMaxLength(60).IsRequired(); b.HasIndex(x => x.Department);
            b.Property(x => x.Status).HasMaxLength(20).IsRequired();
            b.Property(x => x.RowVersion).IsRowVersion(); b.Ignore(x => x.Initials);
        });
        model.Entity<Course>(b =>
        {
            b.ToTable("Courses", t =>
            {
                t.HasCheckConstraint("CK_Course_Credits", "[Credits] BETWEEN 1 AND 6");
                t.HasCheckConstraint("CK_Course_Capacity", "[Capacity] BETWEEN 1 AND 500");
                t.HasCheckConstraint("CK_Course_Status", "[Status] IN ('Active','Archived')");
                t.HasCheckConstraint("CK_Course_Text", "LEN(LTRIM(RTRIM([Code]))) > 0 AND LEN(LTRIM(RTRIM([Title]))) > 0");
                t.HasCheckConstraint("CK_Course_Department", "[Department] IN ('Business','Computer Science','Design','Engineering')");
            });
            b.HasKey(x => x.Id); b.Property(x => x.Code).HasMaxLength(20).IsRequired(); b.HasIndex(x => x.Code).IsUnique();
            b.Property(x => x.Title).HasMaxLength(120).IsRequired();
            b.Property(x => x.Department).HasMaxLength(60).IsRequired(); b.Property(x => x.Status).HasMaxLength(20).IsRequired();
            b.Property(x => x.RowVersion).IsRowVersion();
        });
        model.Entity<Enrollment>(b =>
        {
            b.ToTable("Enrollments", t =>
            {
                t.HasCheckConstraint("CK_Enrollment_Year", "[AcademicYear] BETWEEN 2000 AND 2100 AND YEAR([EnrolledOn]) = [AcademicYear]");
                t.HasCheckConstraint("CK_Enrollment_Term", "[Term] IN ('Spring','Summer','Fall','Winter')");
                t.HasCheckConstraint("CK_Enrollment_Status", "[Status] IN ('Enrolled','Completed','Withdrawn')");
                t.HasCheckConstraint("CK_Enrollment_Grade", "([Status] = 'Completed' AND [Grade] IS NOT NULL AND [Grade] IN ('A','A-','B+','B','B-','C+','C','C-','D','F')) OR ([Status] <> 'Completed' AND [Grade] IS NULL)");
            });
            b.HasKey(x => x.Id);
            b.HasOne(x => x.Student).WithMany(x => x.Enrollments).HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(x => x.Course).WithMany(x => x.Enrollments).HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.Restrict);
            b.Property(x => x.Term).HasMaxLength(12).IsRequired(); b.Property(x => x.Status).HasMaxLength(20).IsRequired();
            b.Property(x => x.Grade).HasMaxLength(2); b.Property(x => x.RowVersion).IsRowVersion();
            b.HasIndex(x => new { x.StudentId, x.CourseId, x.Term, x.AcademicYear }).IsUnique();
            b.HasIndex(x => new { x.CourseId, x.Term, x.AcademicYear, x.Status });
            b.HasIndex(x => x.EnrolledOn);
        });
    }
}
