using Mdsweep.Api.Configuration;
using Mdsweep.Api.Features.Identity;
using Mdsweep.Infrastructure;
using Mdsweep.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddMdsweepInfrastructure(builder.Configuration);
builder.AddMdsweepApi();
builder.AddMdsweepMessaging();

var app = builder.Build();

await app.InitializeMdsweepInfrastructureAsync();

// Must run before authentication so OIDC generates HTTPS callback URLs
// correctly when running behind Azure Container Apps ingress.
app.UseForwardedHeaders();

//
// Angular is served by its own Vite/ng dev server during `aspire run`.
// During publish/deploy Aspire copies the Angular build into API/wwwroot.
//
if (!app.Environment.IsDevelopment())
{
    app.UseDefaultFiles();
    app.UseStaticFiles();
}

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapWolverineEndpoints(options =>
{
    options.RequireAuthorizeOnAll();
    options.AutoAntiforgeryOnFormEndpoints();
    options.TenantId.IsClaimTypeNamed(TenantClaimTypes.ActiveTenantId);
    options.TenantId.AssertExists();
});
app.MapIdentity();

app.MapDefaultEndpoints();

//
// Must be last. In production, anything that wasn't an API/auth/health
// endpoint falls back to Angular's index.html for client-side routing.
//
if (!app.Environment.IsDevelopment())
{
    app.Map("/api/{**path}", () => Results.NotFound());
    app.MapFallbackToFile("index.html");
}
await app.RunAsync();

public partial class Program;
