using Mdsweep.Api.Common.Extensions;
using Mdsweep.Application.Vehicles;

namespace Mdsweep.Api.Features.Vehicles;

internal static class VehicleWriteResult
{
    public static async Task<IResult> Execute(Func<Task<IResult>> write)
    {
        try
        {
            return await write();
        }
        catch (VehicleVinConflictException)
        {
            return Result.Invalid(VehicleErrors.DuplicateVin()).ToEndpointResult();
        }
    }
}
