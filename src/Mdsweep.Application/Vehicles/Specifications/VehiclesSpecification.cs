using Mdsweep.Application.Common.Specifications;
using Mdsweep.Domain.Vehicles;

namespace Mdsweep.Application.Vehicles.Specifications;

public sealed class VehiclesSpecification : SpecificationBuilder<VehicleAggregate, Guid, VehiclesSpecification>
{
    public VehiclesSpecification WithVin(string vin)
    {
        Spec.Add(query => query.Where(vehicle => vehicle.Vin == vin));
        return this;
    }
}
