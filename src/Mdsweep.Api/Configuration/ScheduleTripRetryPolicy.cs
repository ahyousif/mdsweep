using JasperFx;
using JasperFx.CodeGeneration;
using Mdsweep.Application.Trips.Scheduling;
using Wolverine.Configuration;
using Wolverine.ErrorHandling;
using Wolverine.Runtime.Handlers;

namespace Mdsweep.Api.Configuration;

public sealed class ScheduleTripRetryPolicy : IHandlerPolicy
{
    public void Apply(IReadOnlyList<HandlerChain> chains, GenerationRules rules, IServiceContainer container)
    {
        foreach (var chain in chains.Where(chain => AppliesTo(chain.MessageType)))
        {
            chain
                .OnException<HttpRequestException>()
                .ScheduleRetry(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(10));
            chain
                .OnException<TaskCanceledException>(exception => !exception.CancellationToken.IsCancellationRequested)
                .ScheduleRetry(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(10));
        }
    }

    public static bool AppliesTo(Type messageType) => messageType == typeof(ScheduleTripCommand);
}
