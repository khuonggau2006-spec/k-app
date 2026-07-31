using TaskMgmt.Domain.Enums;

namespace TaskMgmt.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    SystemRole? Role { get; }
}
