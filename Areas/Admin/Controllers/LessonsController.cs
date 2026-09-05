using CPE_Review_Hub.Data;
using CPE_Review_Hub.Models;
using CPE_Review_Hub.Security;
using CPE_Review_Hub.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CPE_Review_Hub.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Policy = PortalSecurity.Admin), Route("admin/lessons")]
public class LessonsController(ApplicationDbContext db) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(int? subjectId)
    {
        ViewData["SubjectId"] = subjectId;
        var query = db.Lessons.AsNoTracking().AsQueryable();
        if (subjectId.HasValue) query = query.Where(x => x.SubjectId == subjectId);
        return View(await query.OrderBy(x => x.Subject.Code).ThenBy(x => x.SortOrder).ThenBy(x => x.Title)
            .Select(x => new LessonAdminRow(x.Id, x.Title, x.Subject.Code, x.SortOrder,
                x.ReviewMaterials.Count, x.IsPublished)).ToListAsync());
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create(int? subjectId) => View("Edit", await PopulateAsync(
        new LessonFormViewModel { SubjectId = subjectId ?? 0 }));

    [HttpGet("{id:int}/edit")]
    public async Task<IActionResult> Edit(int id)
    {
        var lesson = await db.Lessons.AsNoTracking().Include(x => x.Activity).SingleOrDefaultAsync(x => x.Id == id);
        if (lesson is null) return NotFound();
        return View(await PopulateAsync(new LessonFormViewModel
        {
            Id = lesson.Id, SubjectId = lesson.SubjectId, Title = lesson.Title,
            Summary = lesson.Summary, SortOrder = lesson.SortOrder, IsPublished = lesson.IsPublished,
            ActivityTitle = lesson.Activity?.Title ?? "", ActivityInstructions = lesson.Activity?.Instructions ?? "",
            ActivityExpectedOutput = lesson.Activity?.ExpectedOutput ?? "",
            ActivityIsPublished = lesson.Activity?.IsPublished ?? false
        }));
    }

    [HttpPost("save")]
    public async Task<IActionResult> Save(LessonFormViewModel model)
    {
        if (!await db.Subjects.AnyAsync(x => x.Id == model.SubjectId))
            ModelState.AddModelError(nameof(model.SubjectId), "Select a valid subject.");
        if (!ModelState.IsValid) return View("Edit", await PopulateAsync(model));

        Lesson lesson;
        if (model.Id == 0)
        {
            lesson = new Lesson { CreatedAtUtc = DateTime.UtcNow };
            db.Lessons.Add(lesson);
        }
        else
        {
            lesson = await db.Lessons.Include(x => x.Activity).SingleOrDefaultAsync(x => x.Id == model.Id) ?? null!;
            if (lesson is null) return NotFound();
        }
        lesson.SubjectId = model.SubjectId;
        lesson.Title = model.Title.Trim();
        lesson.Summary = model.Summary.Trim();
        lesson.SortOrder = model.SortOrder;
        lesson.IsPublished = model.IsPublished;
        lesson.UpdatedAtUtc = DateTime.UtcNow;
        if (lesson.Activity is not null || model.ActivityIsPublished || !string.IsNullOrWhiteSpace(model.ActivityTitle) ||
            !string.IsNullOrWhiteSpace(model.ActivityInstructions) || !string.IsNullOrWhiteSpace(model.ActivityExpectedOutput))
        {
            if (string.IsNullOrWhiteSpace(model.ActivityTitle) || string.IsNullOrWhiteSpace(model.ActivityInstructions) ||
                string.IsNullOrWhiteSpace(model.ActivityExpectedOutput))
            {
                ModelState.AddModelError("", "Complete the activity title, instructions, and expected output together.");
                return View("Edit", await PopulateAsync(model));
            }
            lesson.Activity ??= new LessonActivity();
            lesson.Activity.Title = model.ActivityTitle.Trim();
            lesson.Activity.Instructions = model.ActivityInstructions.Trim();
            lesson.Activity.ExpectedOutput = model.ActivityExpectedOutput.Trim();
            lesson.Activity.IsPublished = model.ActivityIsPublished;
        }
        await db.SaveChangesAsync();
        TempData["Success"] = model.Id == 0 ? "Lesson created." : "Lesson updated.";
        return RedirectToAction(nameof(Index), new { subjectId = model.SubjectId });
    }

    [HttpGet("{id:int}/delete")]
    public async Task<IActionResult> Delete(int id)
    {
        var lesson = await db.Lessons.AsNoTracking().Include(x => x.Subject)
            .Include(x => x.ReviewMaterials).SingleOrDefaultAsync(x => x.Id == id);
        return lesson is null ? NotFound() : View(lesson);
    }

    [HttpPost("{id:int}/delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var lesson = await db.Lessons.SingleOrDefaultAsync(x => x.Id == id);
        if (lesson is null) return NotFound();
        var subjectId = lesson.SubjectId;
        db.Lessons.Remove(lesson);
        await db.SaveChangesAsync();
        TempData["Success"] = "Lesson and its review materials were deleted.";
        return RedirectToAction(nameof(Index), new { subjectId });
    }

    private async Task<LessonFormViewModel> PopulateAsync(LessonFormViewModel model)
    {
        model.Subjects = await db.Subjects.AsNoTracking().OrderBy(x => x.AcademicLevelId).ThenBy(x => x.Code)
            .Select(x => new SelectionOption(x.Id, $"{x.AcademicLevel.Name} · {x.Code} — {x.Title}"))
            .ToListAsync();
        return model;
    }
}
