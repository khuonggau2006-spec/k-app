using TaskMgmt.Domain.Common;

namespace TaskMgmt.Domain.Entities;

public class RefreshToken : BaseEntity
{
    public required Guid UserId { get; set; }
    public User? User { get; set; }

    public required string Token { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public DateTimeOffset? RevokedAtUtc { get; set; }
    public string? ReplacedByToken { get; set; }

    public bool IsActive => RevokedAtUtc is null && ExpiresAtUtc > DateTimeOffset.UtcNow;
}
