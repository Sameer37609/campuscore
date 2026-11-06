using CampusCore.Data;
using CampusCore.Models;
using CampusCore.Security;
using CampusCore.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace CampusCore.Controllers;

[LocalWrite]
[Route("{kind:regex(^(Students|Courses|Enrollments)$)}")]
public sealed class ManagementController(AcademicDbContext db, AcademicWrites writes, IAcademicData data) : Controller
{
    private static string Version(byte[] value) => Convert.ToBase64String(value);
    private async Task Choices()
    {
        ViewBag.Students = await db.Students.AsNoTracking().OrderBy(s => s.Name).ToArrayAsync();
        ViewBag.Courses = await db.Courses.AsNoTracking().OrderBy(c => c.Code).ToArrayAsync();
    }
    private async Task<IActionResult> Form(string kind, AcademicForm model) { ViewData["Kind"] = kind; if (kind == "Enrollments") await Choices(); return View(kind == "Students" ? "StudentForm" : kind == "Courses" ? "CourseForm" : "EnrollmentForm", model); }
    [HttpGet("Create")]
    public async Task<IActionResult> Create(string kind) => await Form(kind, kind == "Students" ? new StudentForm() : kind == "Courses" ? new CourseForm() : new EnrollmentForm { Term = data.CurrentTerm, AcademicYear = data.CurrentYear });
    [HttpGet("Edit/{id:int}")]
    public async Task<IActionResult> Edit(string kind, int id)
    {
        if (kind == "Students") { var s = await db.Students.AsNoTracking().SingleOrDefaultAsync(s => s.Id == id); if (s is null) return NotFound(); return await Form(kind, new StudentForm { Id = id, Number = s.Number, Name = s.Name, Email = s.Email, Department = s.Department, Year = s.Year, Status = s.Status, Version = Version(s.RowVersion) }); }
        if (kind == "Courses") { var c = await db.Courses.AsNoTracking().SingleOrDefaultAsync(c => c.Id == id); if (c is null) return NotFound(); return await Form(kind, new CourseForm { Id = id, Code = c.Code, Title = c.Title, Department = c.Department, Credits = c.Credits, Capacity = c.Capacity, Status = c.Status, Version = Version(c.RowVersion) }); }
        var e = await db.Enrollments.AsNoTracking().SingleOrDefaultAsync(e => e.Id == id); if (e is null) return NotFound();
        return await Form(kind, new EnrollmentForm { Id = id, StudentId = e.StudentId, CourseId = e.CourseId, Term = e.Term, AcademicYear = e.AcademicYear, Status = e.Status, Grade = e.Grade, EnrolledOn = e.EnrolledOn, Version = Version(e.RowVersion) });
    }
    [HttpPost("SaveStudent")]
    public async Task<IActionResult> SaveStudent(string kind, StudentForm model)
    {
        if (kind != "Students") return BadRequest(); if (!ModelState.IsValid) return await Invalid(kind, model);
        return await Result(kind, model, await writes.SaveStudent(model));
    }
    [HttpPost("SaveCourse")]
    public async Task<IActionResult> SaveCourse(string kind, CourseForm model)
    {
        if (kind != "Courses") return BadRequest(); if (!ModelState.IsValid) return await Invalid(kind, model);
        return await Result(kind, model, await writes.SaveCourse(model));
    }
    [HttpPost("SaveEnrollment")]
    public async Task<IActionResult> SaveEnrollment(string kind, EnrollmentForm model)
    {
        if (kind != "Enrollments") return BadRequest(); if (!ModelState.IsValid) return await Invalid(kind, model);
        return await Result(kind, model, await writes.SaveEnrollment(model));
    }
    private async Task<IActionResult> Invalid(string kind, AcademicForm model) { Response.StatusCode = 422; return await Form(kind, model); }
    private async Task<IActionResult> Result(string kind, AcademicForm model, WriteResult result)
    {
        if (result.Missing) return NotFound();
        if (!result.Success) { ModelState.AddModelError("", result.Error!); return await Invalid(kind, model); }
        TempData["Success"] = "Record saved successfully."; return Redirect($"/{kind}/Details/{result.Id}");
    }
    [HttpGet("Delete/{id:int}")]
    public async Task<IActionResult> Delete(string kind, int id)
    {
        DeleteForm form;
        if (kind == "Students") { var s = await db.Students.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id); if (s is null) return NotFound(); form = new() { Id = id, Title = s.Name, Version = Version(s.RowVersion), Restriction = await db.Enrollments.AnyAsync(e => e.StudentId == id) ? "Enrollment history exists. This student cannot be deleted." : null }; }
        else if (kind == "Courses") { var c = await db.Courses.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id); if (c is null) return NotFound(); form = new() { Id = id, Title = c.Title, Version = Version(c.RowVersion), Restriction = await db.Enrollments.AnyAsync(e => e.CourseId == id) ? "Enrollment history exists. This course cannot be deleted." : null }; }
        else { var e = await db.Enrollments.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id); if (e is null) return NotFound(); form = new() { Id = id, Title = $"EN-{id:0000}", Version = Version(e.RowVersion), Restriction = e.Status != "Withdrawn" ? "Only withdrawn enrollments can be removed. Completed history is retained; withdraw an active enrollment first." : null }; }
        form.Kind = kind; return View(form);
    }
    [HttpPost("Delete/{id:int}")]
    public async Task<IActionResult> DeleteConfirmed(string kind, int id, DeleteForm form)
    {
        if (id != form.Id) return BadRequest();
        var result = await writes.Delete(kind, id, form.Version);
        if (result.Missing) return NotFound();
        if (!result.Success) { Response.StatusCode = 422; form.Kind = kind; form.Title = $"Record {id}"; form.Restriction = result.Error; return View("Delete", form); }
        TempData["Success"] = "Record removed successfully."; return Redirect($"/{kind}");
    }
}
