using CPE_Review_Hub.Data;
using CPE_Review_Hub.Models;
using CPE_Review_Hub.Security;
using CPE_Review_Hub.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CPE_Review_Hub.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Policy = PortalSecurity.Admin), Route("admin/lesson-images")]
public class LessonImagesController(ApplicationDbContext db, IWebHostEnvironment environment) : Controller
{
    private static readonly HashSet<string> UploadExtensions = [".png", ".jpg", ".jpeg", ".webp"];

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
            Caption = image.Caption, DisplayOrder = image.DisplayOrder, IsPublished = image.IsPublished
        }));
    }

    [HttpPost("save"), RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Save(LessonImageFormViewModel model, CancellationToken cancellationToken)
    {
        if (!await db.Lessons.AnyAsync(x => x.Id == model.LessonId, cancellationToken))
            ModelState.AddModelError(nameof(model.LessonId), "Select a valid lesson.");
        var imagePath = model.ImagePath.Trim();
        if (model.ImageFile is { Length: > 0 } upload)
        {
            var extension = Path.GetExtension(upload.FileName).ToLowerInvariant();
            if (!UploadExtensions.Contains(extension) || upload.Length > 5 * 1024 * 1024)
                ModelState.AddModelError(nameof(model.ImageFile), "Upload a PNG, JPG, or WEBP image no larger than 5 MB.");
            else
            {
                var folder = Path.Combine(environment.WebRootPath, "images", "lessons", "uploads");
                Directory.CreateDirectory(folder);
                var fileName = $"{Guid.NewGuid():N}{extension}";
                await using var stream = System.IO.File.Create(Path.Combine(folder, fileName));
                await upload.CopyToAsync(stream, cancellationToken);
                imagePath = $"/images/lessons/uploads/{fileName}";
            }
        }
        if (!IsExistingLocalImage(imagePath))
            ModelState.AddModelError(nameof(model.ImagePath), "Choose an upload or enter an existing /images/... path.");
        if (!ModelState.IsValid) return View("Edit", await PopulateAsync(model));

        var image = model.Id == 0 ? new LessonImage() :
            await db.LessonImages.SingleOrDefaultAsync(x => x.Id == model.Id, cancellationToken);
        if (image is null) return NotFound();
        if (model.Id == 0) db.LessonImages.Add(image);
        image.LessonId = model.LessonId; image.ImagePath = imagePath; image.AltText = model.AltText.Trim();
        image.Caption = model.Caption.Trim(); image.DisplayOrder = model.DisplayOrder; image.IsPublished = model.IsPublished;
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

    private bool IsExistingLocalImage(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !path.StartsWith("/images/", StringComparison.OrdinalIgnoreCase) || path.Contains("..")) return false;
        var relative = path.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var root = Path.GetFullPath(environment.WebRootPath) + Path.DirectorySeparatorChar;
        var fullPath = Path.GetFullPath(Path.Combine(environment.WebRootPath, relative));
        return fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase) && System.IO.File.Exists(fullPath);
    }

    private async Task<LessonImageFormViewModel> PopulateAsync(LessonImageFormViewModel model)
    {
        model.Lessons = await db.Lessons.AsNoTracking().OrderBy(x => x.Subject.Code).ThenBy(x => x.SortOrder)
            .Select(x => new SelectionOption(x.Id, $"{x.Subject.Code} · {x.Title}")).ToListAsync();
        return model;
    }
}
