using MediatR;
using TaskMgmt.Application.Features.Auth.Common;

namespace TaskMgmt.Application.Features.Users.Queries.GetUsers;

public record GetUsersQuery : IRequest<List<UserDto>>;
