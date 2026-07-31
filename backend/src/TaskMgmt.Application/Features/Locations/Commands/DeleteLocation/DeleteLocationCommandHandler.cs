using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common.Exceptions;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Domain.Entities;

namespace TaskMgmt.Application.Features.Locations.Commands.DeleteLocation;

public class DeleteLocationCommandHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    : IRequestHandler<DeleteLocationCommand>
{
    public async Task Handle(DeleteLocationCommand request, CancellationToken cancellationToken)
    {
        var location = await context.Locations
            .FirstOrDefaultAsync(l => l.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Location), request.Id);

        location.IsActive = false;
        location.UpdatedAtUtc = DateTimeOffset.UtcNow;
        location.UpdatedByUserId = currentUser.UserId;

        await context.SaveChangesAsync(cancellationToken);
    }
}
