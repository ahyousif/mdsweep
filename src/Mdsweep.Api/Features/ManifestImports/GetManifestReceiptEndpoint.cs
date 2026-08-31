using System.Security.Claims;
using Mdsweep.Api.Features.Identity;
using Mdsweep.Application.ManifestImports;

namespace Mdsweep.Api.Features.ManifestImports;

public static class GetManifestReceiptEndpoint
{
    [WolverineGet("/api/manifest-receipts/{receiptId:guid}")]
    public static async Task<IResult> Get(
        Guid receiptId,
        ClaimsPrincipal user,
        ITenantAccess tenantAccess,
        IMessageBus bus,
        CancellationToken cancellationToken
    )
    {
        var context = await TenantContextResolver.ResolveActive(user, tenantAccess, cancellationToken);
        if (context is null || !TenantContextResolver.HasRole(context, "Dispatcher"))
        {
            return Results.Forbid();
        }

        var response = await bus.InvokeAsync<GetManifestReceiptResult>(
            new GetManifestReceipt(receiptId),
            cancellationToken
        );

        return response.Found ? Results.Ok(response.Receipt) : Results.NotFound();
    }
}
