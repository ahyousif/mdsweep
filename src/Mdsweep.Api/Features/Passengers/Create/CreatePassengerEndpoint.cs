using Mdsweep.Application.Common.Extensions;

namespace Mdsweep.Api.Features.Passengers.Create;

public sealed class CreatePassengerEndpoint
{
    [Tags(PassengerConstants.Tag)]
    [WolverinePost(PassengerConstants.Route)]
    public static async Task Post(CreatePassengerRequest req, IMessageBus bus, CancellationToken ct)
    {
        var result = await bus.SendAsync(req.ToCommand(), ct);
    }
}
