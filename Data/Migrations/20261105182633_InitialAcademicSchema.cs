using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CampusCore.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialAcademicSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Courses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Department = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Credits = table.Column<int>(type: "int", nullable: false),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Courses", x => x.Id);
                    table.CheckConstraint("CK_Course_Capacity", "[Capacity] BETWEEN 1 AND 500");
                    table.CheckConstraint("CK_Course_Credits", "[Credits] BETWEEN 1 AND 6");
                    table.CheckConstraint("CK_Course_Department", "[Department] IN ('Business','Computer Science','Design','Engineering')");
                    table.CheckConstraint("CK_Course_Status", "[Status] IN ('Active','Archived')");
                    table.CheckConstraint("CK_Course_Text", "LEN(LTRIM(RTRIM([Code]))) > 0 AND LEN(LTRIM(RTRIM([Title]))) > 0");
                });

            migrationBuilder.CreateTable(
                name: "Students",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Number = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    Department = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Students", x => x.Id);
                    table.CheckConstraint("CK_Student_Department", "[Department] IN ('Business','Computer Science','Design','Engineering')");
                    table.CheckConstraint("CK_Student_Status", "[Status] IN ('Active','On leave','Graduated')");
                    table.CheckConstraint("CK_Student_Text", "LEN(LTRIM(RTRIM([Number]))) > 0 AND LEN(LTRIM(RTRIM([Name]))) > 0 AND LEN(LTRIM(RTRIM([Email]))) > 0");
                    table.CheckConstraint("CK_Student_Year", "[Year] BETWEEN 1 AND 4");
                });

            migrationBuilder.CreateTable(
                name: "Enrollments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    Term = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                    AcademicYear = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Grade = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    EnrolledOn = table.Column<DateOnly>(type: "date", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Enrollments", x => x.Id);
                    table.CheckConstraint("CK_Enrollment_Grade", "([Status] = 'Completed' AND [Grade] IS NOT NULL AND [Grade] IN ('A','A-','B+','B','B-','C+','C','C-','D','F')) OR ([Status] <> 'Completed' AND [Grade] IS NULL)");
                    table.CheckConstraint("CK_Enrollment_Status", "[Status] IN ('Enrolled','Completed','Withdrawn')");
                    table.CheckConstraint("CK_Enrollment_Term", "[Term] IN ('Spring','Summer','Fall','Winter')");
                    table.CheckConstraint("CK_Enrollment_Year", "[AcademicYear] BETWEEN 2000 AND 2100 AND YEAR([EnrolledOn]) = [AcademicYear]");
                    table.ForeignKey(
                        name: "FK_Enrollments_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Enrollments_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Courses_Code",
                table: "Courses",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_CourseId_Term_AcademicYear_Status",
                table: "Enrollments",
                columns: new[] { "CourseId", "Term", "AcademicYear", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_EnrolledOn",
                table: "Enrollments",
                column: "EnrolledOn");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_StudentId_CourseId_Term_AcademicYear",
                table: "Enrollments",
                columns: new[] { "StudentId", "CourseId", "Term", "AcademicYear" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Students_Department",
                table: "Students",
                column: "Department");

            migrationBuilder.CreateIndex(
                name: "IX_Students_Email",
                table: "Students",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Students_Number",
                table: "Students",
                column: "Number",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Enrollments");

            migrationBuilder.DropTable(
                name: "Courses");

            migrationBuilder.DropTable(
                name: "Students");
        }
    }
}
