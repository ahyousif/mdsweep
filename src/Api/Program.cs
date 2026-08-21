using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Mdsweep.Api.Features.Dispatch;
using Mdsweep.Api.Features.ManifestImports;
using Mdsweep.Api.Infrastructure;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("mdsweep")));
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.Cookie.Name = ".Mdsweep.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Events.OnRedirectToLogin = context => { context.Response.StatusCode = 401; return Task.CompletedTask; };
        options.Events.OnRedirectToAccessDenied = context => { context.Response.StatusCode = 403; return Task.CompletedTask; };
    })
    .AddOpenIdConnect(options =>
    {
        options.Authority = builder.Configuration["Authentication:Authority"];
        options.ClientId = builder.Configuration["Authentication:ClientId"];
        options.ClientSecret = builder.Configuration["Authentication:ClientSecret"];
        options.ResponseType = "code";
        options.SaveTokens = false;
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.TokenValidationParameters.NameClaimType = "sub";
        options.TokenValidationParameters.RoleClaimType = "roles";
    });
builder.Services.AddAuthorization();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.EnsureCreatedAsync();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapManifestImports();
app.MapDispatch();
app.MapGet("/api/auth/login", () => Results.Challenge(new AuthenticationProperties { RedirectUri = "/" }, [OpenIdConnectDefaults.AuthenticationScheme]));
app.MapGet("/api/auth/me", (ClaimsPrincipal user) => Results.Ok(new { subject = user.FindFirstValue("sub"), roles = user.FindAll("roles").Select(x => x.Value) })).RequireAuthorization();
app.MapPost("/api/auth/logout", () => Results.SignOut([CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme])).RequireAuthorization();
app.MapDefaultEndpoints();
app.Run();

public partial class Program;
