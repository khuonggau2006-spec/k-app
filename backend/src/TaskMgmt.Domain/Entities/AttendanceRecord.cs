using TaskMgmt.Domain.Common;

namespace TaskMgmt.Domain.Entities;

public class AttendanceRecord : AuditableEntity
{
    public required Guid UserId { get; set; }
    public User? User { get; set; }
    public required DateOnly WorkDate { get; set; }
    public DateTimeOffset? CheckInAtUtc { get; set; }
    public double? CheckInLatitude { get; set; }
    public double? CheckInLongitude { get; set; }
    public Guid? CheckInLocationId { get; set; }
    public Location? CheckInLocation { get; set; }
    public DateTimeOffset? CheckOutAtUtc { get; set; }
    public double? CheckOutLatitude { get; set; }
    public double? CheckOutLongitude { get; set; }
    public Guid? CheckOutLocationId { get; set; }
    public Location? CheckOutLocation { get; set; }
}
