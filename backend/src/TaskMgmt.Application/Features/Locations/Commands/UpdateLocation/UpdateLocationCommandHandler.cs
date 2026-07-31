using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common.Exceptions;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Application.Features.Locations.Common;
using TaskMgmt.Domain.Entities;

namespace TaskMgmt.Application.Features.Locations.Commands.UpdateLocation;

public class UpdateLocationCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    : IRequestHandler<UpdateLocationCommand, LocationDto>
{
    public async Task<LocationDto> Handle(UpdateLocationCommand request, CancellationToken cancellationToken)
    {
        var location = await context.Locations
            .FirstOrDefaultAsync(l => l.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Location), request.Id);

        location.Name = request.Name;
        location.Address = request.Address;
        location.Latitude = request.Latitude;
        location.Longitude = request.Longitude;
        location.IsActive = request.IsActive;
        location.ParentLocationId = request.ParentLocationId;
        location.UpdatedAtUtc = DateTimeOffset.UtcNow;
        location.UpdatedByUserId = currentUser.UserId;

        await context.SaveChangesAsync(cancellationToken);

        return LocationDto.FromEntity(location);
    }
}
