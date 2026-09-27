using CPE_Review_Hub.Security;
using CPE_Review_Hub.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.DataProtection;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
// Restricted Windows accounts may not write to the machine Event Log. Console
// logging remains available locally and is what the Vercel container captures.
if (OperatingSystem.IsWindows())
    builder.Logging.AddFilter<Microsoft.Extensions.Logging.EventLog.EventLogLoggerProvider>((_, _) => false);
var dataProtectionPath = builder.Configuration["DataProtection:KeysPath"];
if (builder.Configuration.GetValue<bool>("DataProtection:PersistToDatabase"))
{
    var certificate = builder.Configuration["DataProtection:CertificateBase64"]
        ?? throw new InvalidOperationException("Production key encryption certificate is required.");
    builder.Services.AddDataProtection().SetApplicationName("COLMS")
        .PersistKeysToDbContext<CPE_Review_Hub.Data.ApplicationDbContext>()
        .ProtectKeysWithCertificate(System.Security.Cryptography.X509Certificates.X509CertificateLoader.LoadPkcs12(
            Convert.FromBase64String(certificate), null,
            System.Security.Cryptography.X509Certificates.X509KeyStorageFlags.EphemeralKeySet));
}
else if (!string.IsNullOrWhiteSpace(dataProtectionPath))
    builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath));
builder.Services.AddControllersWithViews(options =>
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddPortalSecurity(builder.Configuration, builder.Environment.IsDevelopment());
builder.Services.AddScoped<StudentProgressService>();
builder.Services.AddScoped<ActivityAttachmentService>();
builder.Services.AddScoped<AssessmentScoringService>();
builder.Services.AddScoped<PortalPasswordResetService>();
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Email"));
builder.Services.AddScoped<IEmailDeliveryService, EmailDeliveryService>();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("account", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0
        }));
});
var app = builder.Build();
if (await DatabaseCommands.TryRunAsync(app, args)) return;
await FreshmanCatalogDeployment.ApplyAsync(app);
await OnlineLessonImageDeployment.ApplyAsync(app);
// Only explicitly configured reverse-proxy deployments may supply the external
// HTTPS scheme. Vercel terminates TLS before forwarding to the container.
if (builder.Configuration.GetValue<bool>("Hosting:LocalProxy") ||
    builder.Configuration.GetValue<bool>("Hosting:VercelProxy"))
{
    var forwardedOptions = new Microsoft.AspNetCore.Builder.ForwardedHeadersOptions
    {
        ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto,
        ForwardLimit = 1
    };
    if (builder.Configuration.GetValue<bool>("Hosting:VercelProxy"))
    {
        // The container is reachable through Vercel's managed gateway. Its
        // address is dynamic, so a static KnownProxies allowlist is unavailable.
        forwardedOptions.KnownIPNetworks.Clear();
        forwardedOptions.KnownProxies.Clear();
    }
    app.UseForwardedHeaders(forwardedOptions);
}
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    // Dynamic authenticated pages must not be stored by shared caches. Static
    // assets are safe to cache and should not be downloaded again on every
    // lesson navigation, especially from a serverless deployment.
    var path = context.Request.Path;
    var isStaticAsset = path.StartsWithSegments("/css") || path.StartsWithSegments("/js") ||
        path.StartsWithSegments("/lib") || path.StartsWithSegments("/images") ||
        path.StartsWithSegments("/fonts") || Path.HasExtension(path.Value);
    if (!isStaticAsset)
    {
        context.Response.Headers.CacheControl = "no-cache, no-store";
        context.Response.Headers.Pragma = "no-cache";
    }
    await next();
});
app.UseRouting();
app.UseRateLimiter();
app.UseMiddleware<DatabaseErrorMiddleware>();
app.UseMiddleware<RequestMonitoringMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.UseStatusCodePagesWithReExecute("/Home/HttpStatus", "?code={0}");
app.MapStaticAssets();
app.MapControllerRoute("areas", "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}").WithStaticAssets();
app.Run();
