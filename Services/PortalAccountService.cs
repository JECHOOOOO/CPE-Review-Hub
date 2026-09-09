using System.Security.Claims;
using CPE_Review_Hub.Models;
using CPE_Review_Hub.Security;
using CPE_Review_Hub.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

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

    public async Task<(IdentityResult Result, ApplicationUser? User)> RegisterStudentAsync(RegisterViewModel model)
    {
        var user = new ApplicationUser
        {
            UserName = model.Email.Trim(), Email = model.Email.Trim(), FullName = model.FullName.Trim(),
            University = model.University?.Trim() ?? "", YearLevel = model.YearLevel?.Trim() ?? "",
            AccountType = PortalSecurity.Student, LockoutEnabled = true, EmailConfirmed = true
        };
        IdentityResult result;
        try
        {
            result = await users.CreateAsync(user, model.Password);
        }
        catch (DbUpdateException exception) when (exception.InnerException is MySql.Data.MySqlClient.MySqlException { Number: 1062 })
        {
            // A rapid double-submit can pass Identity's uniqueness check twice before
            // MySQL accepts the first insert. Treat the unique-index race as a normal
            // registration failure instead of showing the database outage page.
            result = IdentityResult.Failed(new IdentityError
            {
                Code = "DuplicateEmail",
                Description = "An account with this email already exists."
            });
        }
        return (result, result.Succeeded ? user : null);
    }

    public Task<IdentityResult> DeleteAsync(ApplicationUser user) => users.DeleteAsync(user);

    public async Task LogoutAsync(HttpContext context)
    {
        await context.SignOutAsync(PortalSecurity.Student);
        await context.SignOutAsync(PortalSecurity.Admin);
    }
}
