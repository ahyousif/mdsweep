using Mdsweep.Api.Common.Authorization;
using Mdsweep.Api.Common.Extensions;
using Mdsweep.Application.Common.Extensions;
using Mdsweep.Application.TripImports.Get;
using Mdsweep.Application.TripImports.Preview;

namespace Mdsweep.Api.Features.TripImports.Preview;

public sealed class PreviewTripImportEndpoint
{
    [Tags(TripImportConstants.Tag)]
    [Authorize(Policy = AuthorizationPolicies.TripsImport)]
    [WolverinePost(TripImportConstants.Route)]
    public static async Task<IResult> Post(IFormFile file, IMessageBus bus, CancellationToken ct)
    {
        await using var content = new MemoryStream();

        await file.CopyToAsync(content, ct);

        var result = await bus.SendAsync(
            new PreviewTripImportCommand(file.FileName, file.ContentType, content.ToArray()),
            ct
        );

        return await result.ToEndpointResultAsync(importId => GetResponse(importId, bus, ct, created: true));
    }

    private static async Task<IResult> GetResponse(Guid importId, IMessageBus bus, CancellationToken ct, bool created)
    {
        var result = await bus.SendAsync(new GetTripImportQuery(importId), ct);
        return result.ToEndpointResult(model =>
            created
                ? Results.Created($"{TripImportConstants.Route}/{model.Id}", TripImportResponse.FromModel(model))
                : Results.Ok(TripImportResponse.FromModel(model))
        );
    }
}
