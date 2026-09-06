using Mdsweep.Application.Common.Abstractions;
using Mdsweep.Application.TripImports.Import;
using Mdsweep.Infrastructure.Persistence;
using Wolverine.ErrorHandling;
using Wolverine.FluentValidation;

namespace Mdsweep.Api.Configuration;

public static class MessagingExtensions
{
    public static WebApplicationBuilder AddMessaging(this WebApplicationBuilder builder)
    {
        builder.Host.UseWolverine(options =>
        {
            // Discovery is deliberately explicit. Moving this extension must not silently
            // remove API endpoints or Application handlers from Wolverine's graph.
            options.ApplicationAssembly = typeof(Program).Assembly;
            options.Discovery.IncludeAssembly(typeof(IRequest<>).Assembly);
            options.UseFluentValidation();
            options.AddPersistence(builder.Configuration);
            options.LocalQueue("trip-import-processing").UseDurableInbox();
            options.PublishMessage<PopulateImportedTripPickupTime>().ToLocalQueue("trip-import-processing");
            options.OnException<HttpRequestException>().ScheduleRetry(
                TimeSpan.FromSeconds(10),
                TimeSpan.FromMinutes(1),
                TimeSpan.FromMinutes(5)
            );
            options.OnException<TaskCanceledException>().ScheduleRetry(
                TimeSpan.FromSeconds(10),
                TimeSpan.FromMinutes(1),
                TimeSpan.FromMinutes(5)
            );
        });

        builder.Services.AddWolverineHttp();

        return builder;
    }
}
