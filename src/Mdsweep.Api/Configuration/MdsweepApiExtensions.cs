using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Mdsweep.Api.Features.Identity;
using Mdsweep.Infrastructure.Identity;

namespace Mdsweep.Api.Configuration;

public static class MdsweepApiExtensions
{
    public static WebApplicationBuilder AddMdsweepApi(this WebApplicationBuilder builder)
    {
        builder.Services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();
            options.ForwardLimit = 1;
        });

        builder.Services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie(options =>
            {
                options.Cookie.Name = AuthenticationConventions.CookieName;
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
            .AddOpenIdConnect();

        builder.Services
            .AddOptions<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme)
            .Configure<IOptions<KeycloakAuthenticationOptions>, IHostEnvironment>(
                (oidc, keycloak, environment) =>
                {
                    var configuration = keycloak.Value;
                    oidc.MapInboundClaims = false;
                    oidc.Authority = configuration.Authority;
                    oidc.ClientId = configuration.ClientId;
                    oidc.ClientSecret = configuration.ClientSecret;
                    oidc.ResponseType = OpenIdConnectResponseType.Code;
                    oidc.SaveTokens = false;
                    oidc.RequireHttpsMetadata = !environment.IsDevelopment();
                    oidc.TokenValidationParameters.NameClaimType = "sub";
                    oidc.TokenValidationParameters.RoleClaimType = "roles";
                }
            );

        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy(
                TenantAuthorizationPolicies.Dispatcher,
                policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.AddRequirements(new TenantRoleRequirement(TenantRoles.Dispatcher));
                }
            );
        });
        builder.Services.AddScoped<IAuthorizationHandler, TenantRoleAuthorizationHandler>();

        builder.Services.AddAntiforgery(options =>
        {
            options.HeaderName = AuthenticationConventions.AntiforgeryHeaderName;
            options.Cookie.Name = AuthenticationConventions.AntiforgeryCookieName;
            options.Cookie.HttpOnly = true;
        });

        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
            options.SerializerOptions.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
        });

        return builder;
    }
}
