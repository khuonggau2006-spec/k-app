using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Application.Features.Locations.Common;

namespace TaskMgmt.Application.Features.Locations.Queries.GetLocations;

public class GetLocationsQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetLocationsQuery, List<LocationDto>>
{
    public async Task<List<LocationDto>> Handle(GetLocationsQuery request, CancellationToken cancellationToken)
    {
        return await context.Locations
            .Where(l => l.IsActive)
            .OrderBy(l => l.Name)
            .Select(l => new LocationDto(
                l.Id,
                l.Name,
                l.Address,
                l.Latitude,
                l.Longitude,
                l.IsActive,
                l.ParentLocationId,
                l.CreatedAtUtc,
                l.UpdatedAtUtc))
            .ToListAsync(cancellationToken);
    }
}
