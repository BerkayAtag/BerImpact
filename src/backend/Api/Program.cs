using System.Security.Claims;
using System.Threading.RateLimiting;
using System.Text.Json.Serialization;
using BerImpact.Api;
using BerImpact.Infrastructure;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseWindowsService(o => o.ServiceName = "BerImpact API");
builder.Services.AddPlatform(builder.Configuration);
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();
builder.Services.AddIdentity<PlatformUser, IdentityRole>(o => {
    o.Password.RequiredLength = 14; o.Password.RequireNonAlphanumeric = true;
    o.Lockout.MaxFailedAccessAttempts = 5; o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    o.User.RequireUniqueEmail = false;
}).AddEntityFrameworkStores<PlatformDb>().AddDefaultTokenProviders();
builder.Services.ConfigureApplicationCookie(o => {
    o.Cookie.Name = "BerImpact.Session"; o.Cookie.HttpOnly = true; o.Cookie.SameSite = SameSiteMode.Strict;
    o.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    o.ExpireTimeSpan = TimeSpan.FromMinutes(30); o.SlidingExpiration = false;
    o.Events.OnRedirectToLogin = c => { c.Response.StatusCode = 401; return Task.CompletedTask; };
    o.Events.OnRedirectToAccessDenied = c => { c.Response.StatusCode = 403; return Task.CompletedTask; };
});
builder.Services.Configure<SecurityStampValidatorOptions>(o => o.ValidationInterval = TimeSpan.FromMinutes(1));
var protection = builder.Services.AddDataProtection().SetApplicationName("BerImpact");
if (builder.Configuration["DataProtectionPath"] is { Length: > 0 } keyPath) {
    protection.PersistKeysToFileSystem(new DirectoryInfo(keyPath));
    if (OperatingSystem.IsWindows()) protection.ProtectKeysWithDpapi(protectToLocalMachine: true);
    else if (!builder.Environment.IsDevelopment()) {
        var cert = builder.Configuration["DataProtectionCertificate"] ?? throw new InvalidOperationException("DataProtectionCertificate is required on Linux production.");
        var passwordFile = builder.Configuration["DataProtectionCertificatePasswordFile"];
        var password = passwordFile == null ? builder.Configuration["DataProtectionCertificatePassword"] : File.ReadAllText(passwordFile).Trim();
        protection.ProtectKeysWithCertificate(System.Security.Cryptography.X509Certificates.X509CertificateLoader.LoadPkcs12FromFile(cert, password));
    }
} else if (!builder.Environment.IsDevelopment() && !args.Contains("--migrate") && !args.Contains("--create-user")) throw new InvalidOperationException("A protected persistent DataProtectionPath is required.");
builder.Services.AddAntiforgery(o => {
    o.HeaderName = "X-CSRF-TOKEN"; o.Cookie.Name = "BerImpact.Csrf"; o.Cookie.SameSite = SameSiteMode.Strict;
    o.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
});
builder.Services.AddAuthorization(o => {
    o.AddPolicy("InventoryWrite", p => p.RequireRole(Roles.Admin, Roles.Integration));
    o.AddPolicy("ChangeWrite", p => p.RequireRole(Roles.Admin, Roles.Manager));
    o.AddPolicy("Analyze", p => p.RequireRole(Roles.Admin, Roles.Manager, Roles.Analyst));
    o.AddPolicy("Approve", p => p.RequireRole(Roles.Approver));
    o.AddPolicy("Audit", p => p.RequireRole(Roles.Admin, Roles.Auditor));
});
builder.Services.AddRateLimiter(o => {
    o.RejectionStatusCode = 429;
    o.AddPolicy("login", c => RateLimitPartition.GetFixedWindowLimiter(c.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    o.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(c => RateLimitPartition.GetFixedWindowLimiter(c.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? c.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions { PermitLimit = 300, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.Configure<ForwardedHeadersOptions>(o => {
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.ForwardLimit = 1;
    if (builder.Configuration["TrustedProxy"] is { Length: > 0 } address)
        o.KnownProxies.Add(System.Net.IPAddress.Parse(address));
});
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = 4 * 1024 * 1024);
if (builder.Configuration.GetValue<bool>("RunWorker")) builder.Services.AddHostedService<AnalysisWorker>();
var app = builder.Build();

if (args.Contains("--migrate") || args.Contains("--create-user")) {
    using var scope = app.Services.CreateScope();
    if (args.Contains("--migrate")) await scope.ServiceProvider.GetRequiredService<PlatformDb>().Database.MigrateAsync();
    if (args.Contains("--create-user")) {
        var name = builder.Configuration["username"] ?? throw new ArgumentException("--username is required");
        var roles = (builder.Configuration["roles"] ?? Roles.Reader).Split(',');
        if (roles.Any(r => !Roles.All.Contains(r))) throw new ArgumentException("Unknown role");
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in Roles.All) if (!await roleManager.RoleExistsAsync(role)) {
            var created = await roleManager.CreateAsync(new IdentityRole(role));
            if (!created.Succeeded) throw new InvalidOperationException("Role creation failed.");
        }
        // Read from redirected stdin, never a command-line argument or a logged value.
        if (!Console.IsInputRedirected) throw new InvalidOperationException("Pipe a secret password to stdin.");
        var password = await Console.In.ReadLineAsync() ?? throw new ArgumentException("Password is required on stdin");
        var users = scope.ServiceProvider.GetRequiredService<UserManager<PlatformUser>>();
        if (await users.FindByNameAsync(name) != null) throw new InvalidOperationException("User already exists; no credentials changed.");
        var user = new PlatformUser { UserName = name };
        var result = await users.CreateAsync(user, password);
        if (!result.Succeeded) throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Code)));
        var assigned = await users.AddToRolesAsync(user, roles);
        if (!assigned.Succeeded) throw new InvalidOperationException("Role assignment failed.");
        var db = scope.ServiceProvider.GetRequiredService<PlatformDb>();
        db.AddAudit("local-operator", "UserProvisioned", user.Id, string.Join(",", roles)); await db.SaveChangesAsync();
        Console.WriteLine("Account provisioned.");
    }
    return;
}

app.UseForwardedHeaders();
app.UseExceptionHandler();
app.Use(async (ctx, next) => {
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    ctx.Response.Headers["X-Frame-Options"] = "DENY";
    ctx.Response.Headers["Referrer-Policy"] = "no-referrer";
    ctx.Response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; connect-src 'self'; font-src 'self'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'";
    ctx.Response.Headers["X-Correlation-ID"] = ctx.TraceIdentifier;
    if (ctx.Request.Path.StartsWithSegments("/api")) ctx.Response.Headers.CacheControl = "no-store";
    if (!app.Environment.IsDevelopment()) ctx.Response.Headers.StrictTransportSecurity = "max-age=31536000";
    try { await next(ctx); }
    catch (AntiforgeryValidationException) { await Results.Problem("Invalid CSRF token.", statusCode: 400).ExecuteAsync(ctx); }
    catch (DbUpdateConcurrencyException) { await Results.Problem("Record changed. Refresh and retry.", statusCode: 409).ExecuteAsync(ctx); }
    catch (DbUpdateException e) when (e.InnerException is Npgsql.PostgresException { SqlState: "23505" }) { await Results.Problem("Duplicate or concurrent request. Refresh and retry with the same idempotency key.", statusCode: 409).ExecuteAsync(ctx); }
    catch (ArgumentException e) { await Results.Problem(e.Message, statusCode: 400).ExecuteAsync(ctx); }
    catch (System.Text.Json.JsonException) { await Results.Problem("Invalid JSON input.", statusCode: 400).ExecuteAsync(ctx); }
});
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.Use(async (ctx, next) => {
    if (ctx.Request.Path.StartsWithSegments("/api") && ctx.Request.Method is not ("GET" or "HEAD" or "OPTIONS")) {
        // Authentication/authorization runs first, preventing anonymous mutation probes from leaking data.
        await ctx.RequestServices.GetRequiredService<IAntiforgery>().ValidateRequestAsync(ctx);
    }
    await next(ctx);
});
app.MapGet("/api/v1/health", () => Results.Ok(new { status = "alive" }));
app.MapGet("/api/v1/health/ready", async (PlatformDb db, CancellationToken ct) => {
    try { return await db.Database.CanConnectAsync(ct) && !(await db.Database.GetPendingMigrationsAsync(ct)).Any() ? Results.Ok(new { status = "ready" }) : Results.StatusCode(503); }
    catch { return Results.StatusCode(503); }
});
app.MapAuth();
app.MapInventory();
app.MapChanges();
app.MapReports();
app.MapOpenApi().RequireAuthorization();
app.Map("/api/{**path}", () => Results.NotFound());
app.UseDefaultFiles(); app.UseStaticFiles();
app.MapFallbackToFile("index.html");
await app.RunAsync();

public partial class Program { }
