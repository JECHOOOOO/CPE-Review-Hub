using CPE_Review_Hub.Data;
using CPE_Review_Hub.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CPE_Review_Hub.Controllers;

[Authorize(AuthenticationSchemes = "Student,Admin", Roles = "Student,Admin")]
public class LessonMediaController(ApplicationDbContext db) : Controller
{
    [HttpGet("/images/lessons/stored/{id:int}")]
    public async Task<IActionResult> Image(int id)
    {
        var image = await db.LessonImages.AsNoTracking().Where(x => x.Id == id &&
            (User.IsInRole(PortalSecurity.Admin) || (x.IsPublished && x.Lesson.IsPublished)))
            .Select(x => new { x.ImageData, x.ContentType }).SingleOrDefaultAsync();
        if (image?.ImageData is null || image.ContentType is null) return NotFound();
        return File(image.ImageData, image.ContentType);
    }
}
