using Mdsweep.Api.Common.Authorization;
using Mdsweep.Api.Common.Extensions;
using Mdsweep.Application.Common.Extensions;
using Mdsweep.Application.Passengers.Get;

namespace Mdsweep.Api.Features.Passengers.Create;

public sealed class CreatePassengerEndpoint
{
    [Tags(PassengerConstants.Tag)]
    [Authorize(Policy = AuthorizationPolicies.PassengersManage)]
    [WolverinePost(PassengerConstants.Route)]
    public static async Task<IResult> Post(
        CreatePassengerRequest request,
        IMessageBus bus,
        IAntiforgery antiforgery,
        HttpContext httpContext,
        CancellationToken ct
    )
    {
        await antiforgery.ValidateRequestAsync(httpContext);
        var result = await bus.SendAsync(request.ToCommand(), ct);

        return await result.ToEndpointResultAsync(passengerId => GetPassengerResponse(passengerId, bus, ct));
    }

    private static async Task<IResult> GetPassengerResponse(Guid passengerId, IMessageBus bus, CancellationToken ct)
    {
        var result = await bus.SendAsync(new GetPassengerQuery(passengerId), ct);

        return result.ToEndpointResult(model =>
            Results.Created($"{PassengerConstants.Route}/{model.Id}", PassengerResponse.FromModel(model))
        );
    }
}
