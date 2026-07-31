using MediatR;
using TaskMgmt.Application.Features.Locations.Common;

namespace TaskMgmt.Application.Features.Locations.Commands.CreateLocation;

public record CreateLocationCommand(
    string Name,
    string? Address,
    double Latitude,
    double Longitude,
    Guid? ParentLocationId) : IRequest<LocationDto>;
