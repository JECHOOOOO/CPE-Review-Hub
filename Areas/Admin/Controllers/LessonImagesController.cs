using CPE_Review_Hub.Data;
using CPE_Review_Hub.Models;
using CPE_Review_Hub.Security;
using CPE_Review_Hub.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CPE_Review_Hub.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Policy = PortalSecurity.Admin), Route("admin/lesson-images")]
public class LessonImagesController(ApplicationDbContext db) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(int? lessonId)
    {
        ViewData["LessonId"] = lessonId;
        var query = db.LessonImages.AsNoTracking().AsQueryable();
        if (lessonId.HasValue) query = query.Where(x => x.LessonId == lessonId);
        return View(await query.OrderBy(x => x.Lesson.Subject.Code).ThenBy(x => x.Lesson.SortOrder)
            .ThenBy(x => x.DisplayOrder).Select(x => new LessonImageAdminRow(x.Id, x.ImagePath, x.AltText,
                x.Lesson.Title, x.Lesson.Subject.Code, x.DisplayOrder, x.IsPublished)).ToListAsync());
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create(int? lessonId) => View("Edit", await PopulateAsync(
        new LessonImageFormViewModel { LessonId = lessonId ?? 0, IsPublished = true }));

    [HttpGet("{id:int}/edit")]
    public async Task<IActionResult> Edit(int id)
    {
        var image = await db.LessonImages.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
        return image is null ? NotFound() : View(await PopulateAsync(new LessonImageFormViewModel
        {
            Id = image.Id, LessonId = image.LessonId, ImagePath = image.ImagePath, AltText = image.AltText,
            SourceUrl = image.SourceUrl, SourceName = image.SourceName, License = image.License,
            Author = image.Author, Caption = image.Caption, DisplayOrder = image.DisplayOrder,
            IsPublished = image.IsPublished
        }));
    }

    [HttpPost("save")]
    public async Task<IActionResult> Save(LessonImageFormViewModel model, CancellationToken cancellationToken)
    {
        if (!await db.Lessons.AnyAsync(x => x.Id == model.LessonId, cancellationToken))
            ModelState.AddModelError(nameof(model.LessonId), "Select a valid lesson.");
        var imagePath = model.ImagePath.Trim();
        var sourceUrl = model.SourceUrl.Trim();
        if (!LessonImageUrlPolicy.IsAllowedDirectImage(imagePath))
            ModelState.AddModelError(nameof(model.ImagePath),
                "Use a direct HTTPS image URL from upload.wikimedia.org (SVG, PNG, JPG, JPEG, or WEBP). ");
        if (!LessonImageUrlPolicy.IsAllowedSourcePage(sourceUrl))
            ModelState.AddModelError(nameof(model.SourceUrl),
                "Use the matching HTTPS Wikimedia Commons File page.");
        if (!ModelState.IsValid) return View("Edit", await PopulateAsync(model));

        var image = model.Id == 0 ? new LessonImage() :
            await db.LessonImages.SingleOrDefaultAsync(x => x.Id == model.Id, cancellationToken);
        if (image is null) return NotFound();
        if (model.Id == 0) db.LessonImages.Add(image);
        image.LessonId = model.LessonId; image.ImagePath = imagePath; image.AltText = model.AltText.Trim();
        image.SourceUrl = sourceUrl; image.SourceName = model.SourceName.Trim();
        image.License = model.License.Trim(); image.Author = model.Author.Trim();
        image.Caption = model.Caption.Trim(); image.DisplayOrder = model.DisplayOrder; image.IsPublished = model.IsPublished;
        image.ImageData = null; image.ContentType = null;
        await db.SaveChangesAsync(cancellationToken);
        TempData["Success"] = model.Id == 0 ? "Lesson image added." : "Lesson image updated.";
        return RedirectToAction(nameof(Index), new { lessonId = model.LessonId });
    }

    [HttpGet("{id:int}/delete")]
    public async Task<IActionResult> Delete(int id)
    {
        var image = await db.LessonImages.AsNoTracking().Include(x => x.Lesson).ThenInclude(x => x.Subject)
            .SingleOrDefaultAsync(x => x.Id == id);
        return image is null ? NotFound() : View(image);
    }

    [HttpPost("{id:int}/delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var image = await db.LessonImages.SingleOrDefaultAsync(x => x.Id == id);
        if (image is null) return NotFound();
        var lessonId = image.LessonId; db.LessonImages.Remove(image); await db.SaveChangesAsync();
        TempData["Success"] = "Lesson image removed.";
        return RedirectToAction(nameof(Index), new { lessonId });
    }

    private async Task<LessonImageFormViewModel> PopulateAsync(LessonImageFormViewModel model)
    {
        model.Lessons = await db.Lessons.AsNoTracking().OrderBy(x => x.Subject.Code).ThenBy(x => x.SortOrder)
            .Select(x => new SelectionOption(x.Id, $"{x.Subject.Code} · {x.Title}")).ToListAsync();
        return model;
    }
}
