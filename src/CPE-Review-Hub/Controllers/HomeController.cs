using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using CPE_Review_Hub.Models;
using CPE_Review_Hub.Security;

namespace CPE_Review_Hub.Controllers;

public class HomeController : Controller
{
    [AllowAnonymous]
    public async Task<IActionResult> Index()
    {
        await SetJourneyNavigationAsync();
        return View();
    }

    [AllowAnonymous, HttpGet("/about")]
    public async Task<IActionResult> About()
    {
        await SetJourneyNavigationAsync();
        return View();
    }

    private async Task SetJourneyNavigationAsync()
    {
        var student = await HttpContext.AuthenticateAsync(PortalSecurity.Student);
        var admin = await HttpContext.AuthenticateAsync(PortalSecurity.Admin);
        if (student.Succeeded && student.Principal?.IsInRole(PortalSecurity.Student) == true)
        {
            ViewData["JourneyUrl"] = "/student/dashboard";
            ViewData["JourneyLabel"] = "Continue to student dashboard";
            ViewData["StudentLoginLabel"] = "Student dashboard";
            ViewData["StudentLoginUrl"] = "/student/dashboard";
        }
        else if (admin.Succeeded && admin.Principal?.IsInRole(PortalSecurity.Admin) == true)
        {
            ViewData["JourneyUrl"] = "/admin/dashboard";
            ViewData["JourneyLabel"] = "Continue to admin dashboard";
            ViewData["StudentLoginLabel"] = "Student login";
            ViewData["StudentLoginUrl"] = "/student.html";
        }
        else
        {
            ViewData["JourneyUrl"] = "/student/register";
            ViewData["JourneyLabel"] = "Begin your journey";
            ViewData["StudentLoginLabel"] = "Student login";
            ViewData["StudentLoginUrl"] = "/student.html";
        }
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult AccessDenied()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult HttpStatus(int code)
    {
        if (code != StatusCodes.Status404NotFound) return RedirectToAction(nameof(Index));
        Response.StatusCode = code;
        return View("NotFound");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
