using CPE_Review_Hub.Data;
using CPE_Review_Hub.Models;
using CPE_Review_Hub.Security;
using CPE_Review_Hub.Services;
using CPE_Review_Hub.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CPE_Review_Hub.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Policy = PortalSecurity.Admin), Route("admin/subjects")]
public class SubjectsController(ApplicationDbContext db, ILogger<SubjectsController> logger) : Controller
{
    [HttpGet(""), HttpGet("/admin/courses")]
    public async Task<IActionResult> Index()
    {
        var data = await db.Subjects.AsNoTracking()
        .OrderBy(x => x.AcademicLevelId).ThenBy(x => x.Code)
        .Select(x => new { x.Id, x.Code, x.Title, x.AcademicLevelId, LevelName = x.AcademicLevel.Name,
            LessonCount = x.Lessons.Count, x.IsPublished }).ToListAsync();
        return View(data.Select(x =>
        {
            var metadata = FreshmanCourseMetadata.ForCode(x.Code);
            return new SubjectAdminRow(x.Id, x.Code, x.Title,
                $"{x.LevelName} — {x.AcademicLevelId} Level",
                metadata.Units, metadata.Semester, x.LessonCount, x.IsPublished);
        }).ToList());
    }

    [HttpPost("seed-first-year")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SeedFirstYear()
    {
        try
        {
            var result = await CourseCatalogSeeder.SeedFirstYearAsync(db);
            TempData["Success"] = $"First-year curriculum is ready: {result.LessonsAdded} lessons, " +
                $"{result.MaterialsAdded} study guides, {result.ActivitiesAdded} activities, " +
                $"{result.AssessmentsAdded} assessments, and {result.QuestionsAdded} questions added. " +
                "Existing course content and student history were not overwritten.";
        }
        catch (InvalidOperationException exception)
        {
            TempData["Error"] = exception.Message;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to seed the first-year COLMS curriculum.");
            TempData["Error"] = "Could not initialize first-year content. No account or progress data was reset; check application logs and try again.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create() => View("Edit", await PopulateAsync(new SubjectFormViewModel()));

    [HttpGet("{id:int}/edit")]
    public async Task<IActionResult> Edit(int id)
    {
        var subject = await db.Subjects.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
        if (subject is null) return NotFound();
        return View(await PopulateAsync(new SubjectFormViewModel
        {
            Id = subject.Id, Code = subject.Code, Title = subject.Title,
            Description = subject.Description, AcademicLevelId = subject.AcademicLevelId,
            Units = FreshmanCourseMetadata.ForCode(subject.Code).Units,
            Semester = FreshmanCourseMetadata.ForCode(subject.Code).Semester,
            IsPublished = subject.IsPublished
        }));
    }

    [HttpPost("save")]
    public async Task<IActionResult> Save(SubjectFormViewModel model)
    {
        var code = model.Code.Trim().ToUpperInvariant();
        if (!await db.AcademicLevels.AnyAsync(x => x.Id == model.AcademicLevelId))
            ModelState.AddModelError(nameof(model.AcademicLevelId), "Select a valid academic year.");
        if (await db.Subjects.AnyAsync(x => x.Code == code && x.Id != model.Id))
            ModelState.AddModelError(nameof(model.Code), "This subject code already exists.");
        if (!ModelState.IsValid) return View("Edit", await PopulateAsync(model));

        Subject subject;
        if (model.Id == 0)
        {
            subject = new Subject { CreatedAtUtc = DateTime.UtcNow };
            db.Subjects.Add(subject);
        }
        else
        {
            subject = await db.Subjects.SingleOrDefaultAsync(x => x.Id == model.Id) ?? null!;
            if (subject is null) return NotFound();
        }
        subject.Code = code;
        subject.Title = model.Title.Trim();
        subject.Description = model.Description.Trim();
        subject.AcademicLevelId = model.AcademicLevelId;
        subject.Units = model.Units;
        subject.Semester = model.Semester;
        subject.IsPublished = model.IsPublished;
        subject.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        TempData["Success"] = model.Id == 0 ? "Subject created." : "Subject updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("{id:int}/delete")]
    public async Task<IActionResult> Delete(int id)
    {
        var subject = await db.Subjects.AsNoTracking().Include(x => x.Lessons)
            .SingleOrDefaultAsync(x => x.Id == id);
        return subject is null ? NotFound() : View(subject);
    }

    [HttpPost("{id:int}/delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var subject = await db.Subjects.SingleOrDefaultAsync(x => x.Id == id);
        if (subject is null) return NotFound();
        db.Subjects.Remove(subject);
        await db.SaveChangesAsync();
        TempData["Success"] = "Subject and its lessons/materials were deleted.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<SubjectFormViewModel> PopulateAsync(SubjectFormViewModel model)
    {
        model.Levels = await db.AcademicLevels.AsNoTracking().OrderBy(x => x.Id)
            .Select(x => new SelectionOption(x.Id, $"{x.Name} — {x.Id} Level")).ToListAsync();
        return model;
    }
}
