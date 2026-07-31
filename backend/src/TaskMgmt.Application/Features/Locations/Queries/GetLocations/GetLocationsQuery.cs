using MediatR;
using TaskMgmt.Application.Features.Locations.Common;

namespace TaskMgmt.Application.Features.Locations.Queries.GetLocations;

public record GetLocationsQuery : IRequest<List<LocationDto>>;
