using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Application.Features.Auth.Common;

namespace TaskMgmt.Application.Features.Users.Queries.GetUsers;

public class GetUsersQueryHandler(IApplicationDbContext context) : IRequestHandler<GetUsersQuery, List<UserDto>>
{
    public async Task<List<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        return await context.Users
            .Where(u => u.IsActive)
            .OrderBy(u => u.FullName)
            .Select(u => new UserDto(u.Id, u.Email, u.FullName, u.SystemRole))
            .ToListAsync(cancellationToken);
    }
}
