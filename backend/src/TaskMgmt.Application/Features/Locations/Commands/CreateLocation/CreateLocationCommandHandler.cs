using MediatR;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Application.Features.Locations.Common;
using TaskMgmt.Domain.Entities;

namespace TaskMgmt.Application.Features.Locations.Commands.CreateLocation;

public class CreateLocationCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    : IRequestHandler<CreateLocationCommand, LocationDto>
{
    public async Task<LocationDto> Handle(CreateLocationCommand request, CancellationToken cancellationToken)
    {
        var location = new Location
        {
            Name = request.Name,
            Address = request.Address,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            ParentLocationId = request.ParentLocationId,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedByUserId = currentUser.UserId,
        };

        context.Locations.Add(location);
        await context.SaveChangesAsync(cancellationToken);

        return LocationDto.FromEntity(location);
    }
}
