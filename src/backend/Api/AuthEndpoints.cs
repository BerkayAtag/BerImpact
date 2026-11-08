using System.Security.Claims;
using BerImpact.Infrastructure;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;

namespace BerImpact.Api;

public sealed record LoginRequest(string Username, string Password);

public static class AuthEndpoints
{
    public static void MapAuth(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/auth");
        group.MapGet("/csrf", (HttpContext c, IAntiforgery anti) => Results.Ok(new { token = anti.GetAndStoreTokens(c).RequestToken }));
        group.MapPost("/login", async (LoginRequest input, SignInManager<PlatformUser> signIn, PlatformDb db, HttpContext ctx, CancellationToken ct) => {
            if (string.IsNullOrWhiteSpace(input.Username) || input.Username.Length > 256 || string.IsNullOrEmpty(input.Password) || input.Password.Length > 1024) return Results.BadRequest();
            var result = await signIn.PasswordSignInAsync(input.Username, input.Password, false, lockoutOnFailure: true);
            var actor = result.Succeeded ? (await signIn.UserManager.FindByNameAsync(input.Username))!.Id : "anonymous";
            db.AddAudit(actor, result.Succeeded ? "LoginSucceeded" : "LoginFailed", "session", "", ctx.TraceIdentifier);
            await db.SaveChangesAsync(ct);
            return result.Succeeded ? Results.Ok(new { authenticated = true }) : Results.Problem("Invalid credentials or account unavailable.", statusCode: 401);
        }).RequireRateLimiting("login");
        group.MapGet("/me", async (HttpContext ctx, UserManager<PlatformUser> users) => {
            var user = await users.GetUserAsync(ctx.User);
            return user == null ? Results.Unauthorized() : Results.Ok(new { user.Id, username = user.UserName, roles = await users.GetRolesAsync(user) });
        }).RequireAuthorization();
        group.MapPost("/logout", async (SignInManager<PlatformUser> signIn, HttpContext ctx, PlatformDb db, CancellationToken ct) => {
            db.AddAudit(ctx.User.FindFirstValue(ClaimTypes.NameIdentifier)!, "Logout", "session", "", ctx.TraceIdentifier);
            await db.SaveChangesAsync(ct); await signIn.SignOutAsync(); return Results.NoContent();
        }).RequireAuthorization();
    }

    public static string Actor(this HttpContext context) => context.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    public static string Key(this HttpContext context) {
        var key = context.Request.Headers["Idempotency-Key"].ToString();
        if (key.Length is < 8 or > 100) throw new ArgumentException("Idempotency-Key (8..100 characters) is required.");
        return key;
    }
}
