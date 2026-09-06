using System.Security.Claims;
using Mdsweep.Api.Common.Authentication;
using Mdsweep.Api.Common.Authorization;
using Mdsweep.Infrastructure.Identity;

namespace Mdsweep.Api.Configuration;

public static class ApiExtensions
{
    public static WebApplicationBuilder AddApi(this WebApplicationBuilder builder)
    {
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

        builder
            .Services.AddOptions<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme)
            .Configure<IOptions<KeycloakAuthenticationOptions>, IHostEnvironment>(
                (oidc, keycloak, environment) =>
                {
                    var configuration = keycloak.Value;
                    oidc.MapInboundClaims = false;
                    oidc.Authority = configuration.Authority;
                    oidc.ClientId = configuration.ClientId;
                    oidc.ClientSecret = configuration.ClientSecret;
                    oidc.ResponseType = OpenIdConnectResponseType.Code;
                    // The OIDC handler reads this ID token as id_token_hint during RP-initiated logout.
                    oidc.SaveTokens = true;
                    oidc.RequireHttpsMetadata = !environment.IsDevelopment();
                    oidc.TokenValidationParameters.NameClaimType = "sub";
                    oidc.Events.OnTokenValidated = async context =>
                    {
                        var subject = context.Principal?.FindFirstValue("sub");
                        if (string.IsNullOrWhiteSpace(subject))
                        {
                            context.Fail("The identity provider did not supply a subject.");
                            return;
                        }

                        var tenantAccess = context.HttpContext.RequestServices.GetRequiredService<ITenantAccess>();
                        var memberships = await tenantAccess.GetMembershipsAsync(
                            subject,
                            context.HttpContext.RequestAborted
                        );
                        var tenantIds = memberships
                            .Select(membership => membership.TenantId)
                            .Distinct(StringComparer.Ordinal)
                            .ToList();

                        if (tenantIds.Count == 1 && context.Principal?.Identity is ClaimsIdentity identity)
                        {
                            identity.AddClaim(new Claim(CustomClaimTypes.ActiveTenantId, tenantIds[0]));
                        }
                    };
                }
            );

        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy(
                AuthorizationPolicies.TripsViewAll,
                policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.AddRequirements(
                        new TenantRoleRequirement(TenantRoles.Administrator, TenantRoles.Dispatcher)
                    );
                }
            );
            options.AddPolicy(
                AuthorizationPolicies.TripsManage,
                policy =>
                    policy.AddRequirements(
                        new TenantRoleRequirement(TenantRoles.Administrator, TenantRoles.Dispatcher)
                    )
            );
            options.AddPolicy(
                AuthorizationPolicies.TripsImport,
                policy =>
                    policy.AddRequirements(
                        new TenantRoleRequirement(TenantRoles.Administrator, TenantRoles.Dispatcher)
                    )
            );
            options.AddPolicy(
                AuthorizationPolicies.PassengersManage,
                policy =>
                    policy.AddRequirements(
                        new TenantRoleRequirement(TenantRoles.Administrator, TenantRoles.Dispatcher)
                    )
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
