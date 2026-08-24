using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common.Caching;
using TaskMgmt.Application.Common.Extensions;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Application.Features.Locations.Common;

namespace TaskMgmt.Application.Features.Locations.Queries.GetLocations;

public class GetLocationsQueryHandler(IApplicationDbContext context, ICacheService cache)
    : IRequestHandler<GetLocationsQuery, List<LocationDto>>
{
    public Task<List<LocationDto>> Handle(GetLocationsQuery request, CancellationToken cancellationToken) =>
        cache.GetOrSetAsync(CacheKeys.LocationListKey, CacheKeys.LocationListExpiration, () => context.Locations
            .Where(l => l.IsActive)
            .OrderBy(l => l.Name)
            .Select(l => new LocationDto(
                l.Id,
                l.Name,
                l.Address,
                l.Latitude,
                l.Longitude,
                l.CheckInRadiusMeters,
                l.IsActive,
                l.ParentLocationId,
                l.CreatedAtUtc,
                l.UpdatedAtUtc))
            .ToListAsync(cancellationToken), cancellationToken);
}
