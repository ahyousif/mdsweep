using Microsoft.AspNetCore.Identity;
using Mdsweep.Api.Infrastructure;

namespace Mdsweep.Api.Features.Identity;

public static class IdentityEndpoints
{
    public static IEndpointRouteBuilder MapIdentity(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/auth/login", Login);
        endpoints.MapPost("/api/auth/logout", async (SignInManager<ApplicationUser> signInManager) =>
        {
            await signInManager.SignOutAsync();
            return Results.NoContent();
        }).RequireAuthorization();
        return endpoints;
    }

    private static async Task<IResult> Login(
        LoginRequest request,
        SignInManager<ApplicationUser> signInManager)
    {
        var result = await signInManager.PasswordSignInAsync(
            request.Email.Trim(), request.Password, isPersistent: false, lockoutOnFailure: true);
        return result.Succeeded
            ? Results.Ok(new { role = "Dispatcher" })
            : Results.Json(new { message = "The email or password is incorrect." }, statusCode: StatusCodes.Status401Unauthorized);
    }

    private sealed record LoginRequest(string Email, string Password);
}
