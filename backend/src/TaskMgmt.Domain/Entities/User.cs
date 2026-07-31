using TaskMgmt.Domain.Common;
using TaskMgmt.Domain.Enums;

namespace TaskMgmt.Domain.Entities;

public class User : AuditableEntity
{
    public required string Email { get; set; }
    public required string FullName { get; set; }
    public required string PasswordHash { get; set; }
    public SystemRole SystemRole { get; set; } = SystemRole.Member;
    public bool IsActive { get; set; } = true;

    public ICollection<TaskAssignee> TaskAssignments { get; set; } = [];
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}
