using Mdsweep.Application.Common.Abstractions;
using Mdsweep.Infrastructure.Persistence;

namespace Mdsweep.Api.Configuration;

public static class MdsweepMessagingExtensions
{
    public static WebApplicationBuilder AddMdsweepMessaging(this WebApplicationBuilder builder)
    {
        builder.Host.UseWolverine(options =>
        {
            // Discovery is deliberately explicit. Moving this extension must not silently
            // remove API endpoints or Application handlers from Wolverine's graph.
            options.ApplicationAssembly = typeof(Program).Assembly;
            options.Discovery.IncludeAssembly(typeof(IRequest<>).Assembly);
            options.AddMdsweepPersistence(builder.Configuration);
        });
        builder.Services.AddWolverineHttp();

        return builder;
    }
}
