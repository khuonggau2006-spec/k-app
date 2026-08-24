using TaskMgmt.Domain.Entities;

namespace TaskMgmt.Application.Features.Locations.Common;

public record LocationDto(
    Guid Id,
    string Name,
    string? Address,
    double Latitude,
    double Longitude,
    double CheckInRadiusMeters,
    bool IsActive,
    Guid? ParentLocationId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc)
{
    public static LocationDto FromEntity(Location location) => new(
        location.Id,
        location.Name,
        location.Address,
        location.Latitude,
        location.Longitude,
        location.CheckInRadiusMeters,
        location.IsActive,
        location.ParentLocationId,
        location.CreatedAtUtc,
        location.UpdatedAtUtc);
}
