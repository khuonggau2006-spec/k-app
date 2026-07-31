using MediatR;
using TaskMgmt.Application.Features.Locations.Common;

namespace TaskMgmt.Application.Features.Locations.Queries.GetLocationById;

public record GetLocationByIdQuery(Guid Id) : IRequest<LocationDto>;
