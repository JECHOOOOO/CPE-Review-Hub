using System.Security.Claims;
using CPE_Review_Hub.Models;
using CPE_Review_Hub.Security;
using CPE_Review_Hub.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;

namespace CPE_Review_Hub.Services;

public class PortalAccountService(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn)
{
    public async Task<bool> LoginAsync(HttpContext context, LoginViewModel model, string portal)
    {
        var user = await users.FindByEmailAsync(model.Email.Trim());
        if (user is null || !user.IsActive || user.AccountType != portal) return false;
        var result = await signIn.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: true);
        if (!result.Succeeded) return false;

        await LogoutAsync(context);
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id), new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Role, user.AccountType),
            new Claim(PortalSecurity.StampClaim, user.SecurityStamp ?? "")
        }, portal);
        await context.SignInAsync(portal, new ClaimsPrincipal(identity), new AuthenticationProperties
        {
            IsPersistent = false, AllowRefresh = false
        });
        return true;
    }

    public Task<IdentityResult> RegisterStudentAsync(RegisterViewModel model) => users.CreateAsync(new ApplicationUser
    {
        UserName = model.Email.Trim(), Email = model.Email.Trim(), FullName = model.FullName.Trim(),
        University = model.University?.Trim() ?? "", YearLevel = model.YearLevel?.Trim() ?? "",
        AccountType = PortalSecurity.Student, LockoutEnabled = true
    }, model.Password);

    public async Task LogoutAsync(HttpContext context)
    {
        await context.SignOutAsync(PortalSecurity.Student);
        await context.SignOutAsync(PortalSecurity.Admin);
    }
}
