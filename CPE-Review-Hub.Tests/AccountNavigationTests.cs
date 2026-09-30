using System.Reflection;
using System.Security.Claims;
using CPE_Review_Hub.Controllers;
using CPE_Review_Hub.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using AdminAccountController = CPE_Review_Hub.Areas.Admin.Controllers.AccountController;
using StudentAccountController = CPE_Review_Hub.Areas.Student.Controllers.AccountController;

namespace CPE_Review_Hub.Tests;

public sealed class AccountNavigationTests
{
    [Fact]
    public void Authenticated_student_is_redirected_from_student_login_and_registration()
    {
        var controller = new StudentAccountController();
        SetUser(controller, PortalSecurity.Student);

        var login = Assert.IsType<RedirectToActionResult>(controller.Login());
        var register = Assert.IsType<RedirectToActionResult>(controller.Register());

        Assert.Equal("Dashboard", login.ControllerName);
        Assert.Equal("Index", login.ActionName);
        Assert.Equal("Student", login.RouteValues!["area"]);
        Assert.Equal("Dashboard", register.ControllerName);
        Assert.Equal("Student", register.RouteValues!["area"]);
    }

    [Fact]
    public void Authenticated_administrator_is_redirected_from_admin_login()
    {
        var controller = new AdminAccountController();
        SetUser(controller, PortalSecurity.Admin);

        var login = Assert.IsType<RedirectToActionResult>(controller.Login());

        Assert.Equal("Dashboard", login.ControllerName);
        Assert.Equal("Index", login.ActionName);
        Assert.Equal("Admin", login.RouteValues!["area"]);
    }

    [Fact]
    public void An_account_from_the_other_portal_does_not_gain_a_portal_redirect()
    {
        var studentAtAdminLogin = new AdminAccountController();
        SetUser(studentAtAdminLogin, PortalSecurity.Student);
        var adminResult = Assert.IsType<ViewResult>(studentAtAdminLogin.Login());

        var adminAtStudentLogin = new StudentAccountController();
        SetUser(adminAtStudentLogin, PortalSecurity.Admin);
        var studentResult = Assert.IsType<ViewResult>(adminAtStudentLogin.Login());

        Assert.NotNull(adminResult);
        Assert.NotNull(studentResult);
    }

    [Fact]
    public void Public_about_action_allows_anonymous_visitors()
    {
        var action = typeof(HomeController).GetMethod(nameof(HomeController.About));

        Assert.NotNull(action);
        Assert.NotNull(action!.GetCustomAttribute<AllowAnonymousAttribute>());
    }

    private static void SetUser(Controller controller, string role)
    {
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Role, role)], "test");
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }
}
