using MediatR;
using TaskMgmt.Application.Features.Locations.Common;

namespace TaskMgmt.Application.Features.Locations.Commands.UpdateLocation;

public record UpdateLocationCommand(
    Guid Id,
    string Name,
    string? Address,
    double Latitude,
    double Longitude,
    bool IsActive,
    Guid? ParentLocationId) : IRequest<LocationDto>;
