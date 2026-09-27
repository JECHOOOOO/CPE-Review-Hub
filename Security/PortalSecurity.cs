using System.Security.Claims;
using CPE_Review_Hub.Data;
using CPE_Review_Hub.Models;
using CPE_Review_Hub.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace CPE_Review_Hub.Security;

public static class PortalSecurity
{
    public const string Student = "Student";
    public const string Admin = "Admin";
    public const string StampClaim = "cpe:security-stamp";

    public static IServiceCollection AddPortalSecurity(this IServiceCollection services,
        IConfiguration configuration, bool development)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            var connection = configuration.GetConnectionString("ReviewHub");
            if (string.IsNullOrWhiteSpace(connection)) throw new DatabaseSetupException();
            options.UseMySQL(connection);
        });
        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.User.RequireUniqueEmail = true;
            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        }).AddEntityFrameworkStores<ApplicationDbContext>().AddSignInManager().AddDefaultTokenProviders();
        services.Configure<DataProtectionTokenProviderOptions>(options =>
            options.TokenLifespan = TimeSpan.FromHours(1));

        services.AddAuthentication(options =>
        {
            options.DefaultScheme = "Portal";
            options.DefaultChallengeScheme = "Portal";
        }).AddPolicyScheme("Portal", "Portal", options =>
        {
            options.ForwardDefaultSelector = context =>
                context.Request.Path.StartsWithSegments("/Admin") ||
                context.Request.Path.Equals("/admin.html", StringComparison.OrdinalIgnoreCase) ? Admin : Student;
        })
        .AddCookie(Student, options => ConfigureCookie(options, Student, development))
        .AddCookie(Admin, options => ConfigureCookie(options, Admin, development));

        services.AddAuthorization(options =>
        {
            options.AddPolicy(Student, policy => policy.AddAuthenticationSchemes(Student)
                .RequireAuthenticatedUser().RequireRole(Student));
            options.AddPolicy(Admin, policy => policy.AddAuthenticationSchemes(Admin)
                .RequireAuthenticatedUser().RequireRole(Admin));
        });
        services.AddScoped<PortalAccountService>();
        return services;
    }

    private static void ConfigureCookie(CookieAuthenticationOptions options, string portal, bool development)
    {
        options.Cookie.Name = $"CpeReviewHub.{portal}";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = development ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
        options.LoginPath = portal == Admin ? "/admin/login" : "/student/login";
        options.AccessDeniedPath = "/Home/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(2);
        options.SlidingExpiration = false;
        options.Events.OnValidatePrincipal = async context =>
        {
            var id = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            var stamp = context.Principal?.FindFirstValue(StampClaim);
            var cache = context.HttpContext.RequestServices.GetRequiredService<IMemoryCache>();
            var cacheKey = $"portal-principal:{portal}:{id}:{stamp}";
            if (id is not null && stamp is not null &&
                cache.TryGetValue(cacheKey, out bool isRecentlyValid) && isRecentlyValid)
                return;

            var users = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
            var user = id is null ? null : await users.FindByIdAsync(id);
            // Revoked roles, disabled accounts, password changes and lockouts invalidate old cookies.
            if (user is null || !user.IsActive || user.AccountType != portal ||
                user.SecurityStamp != context.Principal?.FindFirstValue(StampClaim) || await users.IsLockedOutAsync(user))
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(portal);
            }
            else
                cache.Set(cacheKey, true, TimeSpan.FromMinutes(1));
        };
    }
}
