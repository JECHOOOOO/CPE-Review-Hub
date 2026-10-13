using CPE_Review_Hub.Data;
using CPE_Review_Hub.Models;
using CPE_Review_Hub.Security;
using CPE_Review_Hub.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CPE_Review_Hub.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Policy = PortalSecurity.Admin), Route("admin/materials")]
public class MaterialsController(ApplicationDbContext db) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(int? lessonId)
    {
        ViewData["LessonId"] = lessonId;
        var query = db.ReviewMaterials.AsNoTracking().AsQueryable();
        if (lessonId.HasValue) query = query.Where(x => x.LessonId == lessonId);
        return View(await query.OrderBy(x => x.Lesson.Subject.Code).ThenBy(x => x.Lesson.SortOrder)
            .ThenBy(x => x.SortOrder).Select(x => new MaterialAdminRow(x.Id, x.Title,
                x.Lesson.Title, x.Lesson.Subject.Code, x.SortOrder, x.IsPublished)).ToListAsync());
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create(int? lessonId) => View("Edit", await PopulateAsync(
        new MaterialFormViewModel { LessonId = lessonId ?? 0 }));

    [HttpGet("{id:int}/edit")]
    public async Task<IActionResult> Edit(int id)
    {
        var material = await db.ReviewMaterials.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
        if (material is null) return NotFound();
        return View(await PopulateAsync(new MaterialFormViewModel
        {
            Id = material.Id, LessonId = material.LessonId, Title = material.Title,
            Content = material.Content, ResourceUrl = material.ResourceUrl,
            SortOrder = material.SortOrder, IsPublished = material.IsPublished
        }));
    }

    [HttpPost("save")]
    public async Task<IActionResult> Save(MaterialFormViewModel model)
    {
        if (!await db.Lessons.AnyAsync(x => x.Id == model.LessonId))
            ModelState.AddModelError(nameof(model.LessonId), "Select a valid lesson.");
        if (!ModelState.IsValid) return View("Edit", await PopulateAsync(model));

        ReviewMaterial material;
        if (model.Id == 0)
        {
            material = new ReviewMaterial { CreatedAtUtc = DateTime.UtcNow };
            db.ReviewMaterials.Add(material);
        }
        else
        {
            material = await db.ReviewMaterials.SingleOrDefaultAsync(x => x.Id == model.Id) ?? null!;
            if (material is null) return NotFound();
        }
        material.LessonId = model.LessonId;
        material.Title = model.Title.Trim();
        material.Content = model.Content.Trim();
        material.ResourceUrl = model.ResourceUrl?.Trim() ?? "";
        material.SortOrder = model.SortOrder;
        material.IsPublished = model.IsPublished;
        material.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        TempData["Success"] = model.Id == 0 ? "Review material created." : "Review material updated.";
        return RedirectToAction(nameof(Index), new { lessonId = model.LessonId });
    }

    [HttpGet("{id:int}/delete")]
    public async Task<IActionResult> Delete(int id)
    {
        var material = await db.ReviewMaterials.AsNoTracking().Include(x => x.Lesson)
            .ThenInclude(x => x.Subject).SingleOrDefaultAsync(x => x.Id == id);
        return material is null ? NotFound() : View(material);
    }

    [HttpPost("{id:int}/delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var material = await db.ReviewMaterials.SingleOrDefaultAsync(x => x.Id == id);
        if (material is null) return NotFound();
        var lessonId = material.LessonId;
        db.ReviewMaterials.Remove(material);
        await db.SaveChangesAsync();
        TempData["Success"] = "Review material deleted.";
        return RedirectToAction(nameof(Index), new { lessonId });
    }

    private async Task<MaterialFormViewModel> PopulateAsync(MaterialFormViewModel model)
    {
        model.Lessons = await db.Lessons.AsNoTracking().OrderBy(x => x.Subject.Code).ThenBy(x => x.SortOrder)
            .Select(x => new SelectionOption(x.Id, $"{x.Subject.Code} · {x.Title}"))
            .ToListAsync();
        return model;
    }
}
