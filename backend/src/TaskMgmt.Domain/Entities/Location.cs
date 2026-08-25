using TaskMgmt.Domain.Common;

namespace TaskMgmt.Domain.Entities;

public class Location : AuditableEntity
{
    public required string Name { get; set; }
    public string? Address { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public bool IsActive { get; set; } = true;
    public double CheckInRadiusMeters { get; set; } = 100;

    public Guid? ParentLocationId { get; set; }
    public Location? ParentLocation { get; set; }
    public ICollection<Location> ChildLocations { get; set; } = [];

    public ICollection<WorkTask> WorkTasks { get; set; } = [];
}
