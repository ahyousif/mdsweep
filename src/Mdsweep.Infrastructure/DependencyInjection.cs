using Mdsweep.Application.Common.Abstractions;
using Mdsweep.Application.Common.Authorization;
using Mdsweep.Application.Common.Email;
using Mdsweep.Application.Common.Security;
using Mdsweep.Application.Trips.Import.Manifest;
using Mdsweep.Application.Trips.Scheduling;
using Mdsweep.Domain.Users;
using Mdsweep.Infrastructure.Email;
using Mdsweep.Infrastructure.Identity;
using Mdsweep.Infrastructure.Manifests;
using Mdsweep.Infrastructure.Persistence;
using Mdsweep.Infrastructure.Persistence.Repositories;
using Mdsweep.Infrastructure.Routing;

namespace Mdsweep.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<KeycloakAuthenticationOptions>()
            .Bind(configuration.GetSection(KeycloakAuthenticationOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.Authority), "Authentication authority is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.ClientId), "Authentication client id is required.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.ClientSecret),
                "Authentication client secret is required."
            )
            .ValidateOnStart();

        services.AddSingleton<IClock>(SystemClock.Instance);
        services.AddSingleton<ITokenService, SecureTokenService>();
        services.AddScoped<IRepository>(serviceProvider => serviceProvider.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IInvitationRepository, InvitationRepository>();
        services.AddScoped<ITenantAccess, TenantAccess>();
        services.AddScoped<IMtmManifestReader, MtmManifestReader>();

        services.AddGoogleMaps(configuration);
        services.AddEmailSender(configuration);

        return services;
    }

    private static void AddEmailSender(this IServiceCollection services, IConfiguration configuration)
    {
        // TODO: consider using options monitoring across the board for all options
        services.AddOptions<EmailOptions>().Bind(configuration.GetSection(EmailOptions.SectionName));

        services.PostConfigure<EmailOptions>(options =>
        {
            // Aspire provides SMTP networking through a connection string; MailKit needs host/port.
            var connectionStringName = Guard.Against.NullOrWhiteSpace(
                options.ConnectionStringName,
                "Email:ConnectionStringName"
            );
            var connectionString = Guard.Against.NullOrWhiteSpace(
                configuration.GetConnectionString(connectionStringName),
                $"ConnectionStrings:{connectionStringName}"
            );

            var endpoint = ParseSmtpEndpoint(connectionString, connectionStringName);

            options.Host = endpoint.Host;
            options.Port = endpoint.Port;
        });

        services.AddScoped<IEmailSender, EmailSender>();
    }

    private static (string Host, int Port) ParseSmtpEndpoint(string connectionString, string connectionStringName)
    {
        var builder = new DbConnectionStringBuilder { ConnectionString = connectionString };
        var endpointValue = Guard.Against.Null(
            builder.TryGetValue("endpoint", out var value) ? value : null,
            $"ConnectionStrings:{connectionStringName}:endpoint"
        );
        var endpoint = Guard.Against.Null(
            Uri.TryCreate(endpointValue.ToString(), UriKind.Absolute, out var uri) ? uri : null,
            $"ConnectionStrings:{connectionStringName}:endpoint"
        );

        return (endpoint.Host, endpoint.Port);
    }

    private static void AddGoogleMaps(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<GoogleRoutesOptions>()
            .Bind(configuration.GetSection(GoogleRoutesOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.ApiKey), "Google Routes API key is required.")
            .ValidateOnStart();

        services.AddHttpClient(
            GoogleRouteEstimateProvider.HttpClientName,
            client =>
            {
                client.BaseAddress = new Uri("https://routes.googleapis.com/");
                client.Timeout = TimeSpan.FromSeconds(10);
            }
        );

        services.AddScoped<IRouteEstimateProvider, GoogleRouteEstimateProvider>();
    }
}
