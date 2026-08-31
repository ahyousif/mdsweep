using Mdsweep.Application.Dispatch;
using Mdsweep.Infrastructure.Persistence;

namespace Mdsweep.Infrastructure.Dispatch;

public static class GetServiceDayHandler
{
    public static Task<List<ServiceDayTripResponse>> Handle(
        GetServiceDay query,
        ApplicationDbContext db,
        CancellationToken cancellationToken
    ) =>
        db
            .Trips.Where(x =>
                x.ProviderId == query.ProviderId && x.AppointmentDate == query.ServiceDate
            )
            .OrderBy(x => x.AppointmentTime)
            .Select(x => new ServiceDayTripResponse(
                x.TripNumber,
                x.JourneyKey,
                x.MemberFirstName + " " + x.MemberLastName,
                x.PickupAddress,
                x.PickupCity,
                x.DeliveryAddress,
                x.DeliveryCity,
                x.PassengerType,
                x.VehicleType,
                x.BrokerStatus,
                x.AppointmentTime,
                db.TripSchedules.Where(schedule => schedule.TripId == x.Id)
                    .Select(schedule => (TimeOnly?)schedule.ScheduledPickupTime)
                    .SingleOrDefault(),
                x.IsWillCall,
                x.IsActive
            ))
            .ToListAsync(cancellationToken);
}
