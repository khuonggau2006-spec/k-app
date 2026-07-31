using TaskMgmt.Application.Common.Interfaces;
using TaskMgmt.Domain.Enums;

namespace TaskMgmt.Application.UnitTests.Common;

internal class FakeCurrentUserService(Guid? userId, SystemRole? role) : ICurrentUserService
{
    public Guid? UserId { get; } = userId;
    public SystemRole? Role { get; } = role;
}
