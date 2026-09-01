using Mdsweep.Application.Common.Pagination;
using Mdsweep.Application.Trips.List;
using Microsoft.AspNetCore.Mvc;
using NodaTime.Text;

namespace Mdsweep.Api.Features.Trips.List;

public sealed class ListTripsRequest
{
    private string? serviceDate;
    private bool serviceDateParsed;
    private bool serviceDateIsValid;
    private LocalDate? parsedServiceDate;

    [FromQuery]
    public string? ServiceDate
    {
        get => serviceDate;
        set
        {
            serviceDate = value;
            serviceDateParsed = false;
            parsedServiceDate = null;
        }
    }

    [FromQuery]
    public string? BrokerStatus { get; set; }

    [FromQuery]
    public bool? IsWillCall { get; set; }

    [FromQuery]
    public int Page { get; set; } = 1;

    [FromQuery]
    public int PageSize { get; set; } = 50;

    [FromQuery]
    public TripSortBy SortBy { get; set; } = TripSortBy.AppointmentTime;

    [FromQuery]
    public SortDirection SortDirection { get; set; } = SortDirection.Ascending;

    public bool HasValidServiceDate()
    {
        ParseServiceDate();
        return serviceDateIsValid;
    }

    public ListTripsQuery ToQuery()
    {
        ParseServiceDate();
        return new ListTripsQuery(parsedServiceDate, BrokerStatus, IsWillCall, Page, PageSize, SortBy, SortDirection);
    }

    private void ParseServiceDate()
    {
        if (serviceDateParsed)
        {
            return;
        }

        serviceDateParsed = true;
        serviceDateIsValid = serviceDate is null;
        if (serviceDate is null)
        {
            return;
        }

        var parsed = LocalDatePattern.Iso.Parse(serviceDate);
        serviceDateIsValid = parsed.Success;
        if (parsed.Success)
        {
            parsedServiceDate = parsed.Value;
        }
    }
}
