using System.Text.Json.Serialization;
using Mdsweep.Api.Features.Dispatch;
using Mdsweep.Api.Features.Identity;
using Mdsweep.Application.ManifestImports;
using Mdsweep.Infrastructure;
using Mdsweep.Infrastructure.Identity;
using Mdsweep.Infrastructure.ManifestImports;
using Mdsweep.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.Http;
using Wolverine.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddMdsweepInfrastructure(builder.Configuration);

builder.Host.UseWolverine(options =>
{
    options.Discovery.IncludeAssembly(typeof(PreviewManifest).Assembly);
    options.Discovery.IncludeAssembly(typeof(PreviewManifestHandler).Assembly);
    // Lightweight mode keeps EF Core as the unit of work without introducing
    // Wolverine durable message storage, inboxes, or outboxes.
    options.UseEntityFrameworkCoreTransactions(TransactionMiddlewareMode.Lightweight);
});
builder.Services.AddWolverineHttp();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
    options.ForwardLimit = 1;
});

builder
    .Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.Cookie.Name = ".Mdsweep.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;

        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };

        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    })
    .AddOpenIdConnect(options =>
    {
        options.MapInboundClaims = false;

        options.Authority =
            builder.Configuration["Authentication:Authority"]
            ?? "https://keycloak.invalid/realms/mdsweep";

        options.ClientId = builder.Configuration["Authentication:ClientId"] ?? "mdsweep-server";

        options.ClientSecret = builder.Configuration["Authentication:ClientSecret"];

        options.ResponseType = "code";
        options.SaveTokens = false;

        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();

        options.TokenValidationParameters.NameClaimType = "sub";
        options.TokenValidationParameters.RoleClaimType = "roles";
    });

builder.Services.AddAuthorization();

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-XSRF-TOKEN";
    options.Cookie.Name = ".Mdsweep.Antiforgery";
    options.Cookie.HttpOnly = true;
});

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter())
);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    await db.Database.MigrateAsync();

    if (app.Environment.IsDevelopment())
    {
        await DevelopmentIdentitySeeder.SeedAsync(db);
    }
}

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
