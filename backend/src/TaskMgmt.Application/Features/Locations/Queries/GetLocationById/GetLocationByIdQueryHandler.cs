using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common.Exceptions;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Application.Features.Locations.Common;
using TaskMgmt.Domain.Entities;

namespace TaskMgmt.Application.Features.Locations.Queries.GetLocationById;

public class GetLocationByIdQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetLocationByIdQuery, LocationDto>
{
    public async Task<LocationDto> Handle(GetLocationByIdQuery request, CancellationToken cancellationToken)
    {
        var location = await context.Locations
            .FirstOrDefaultAsync(l => l.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Location), request.Id);

        return LocationDto.FromEntity(location);
    }
}
