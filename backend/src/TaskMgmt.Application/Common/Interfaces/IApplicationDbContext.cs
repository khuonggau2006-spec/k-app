using Microsoft.EntityFrameworkCore;
using TaskMgmt.Domain.Entities;

namespace TaskMgmt.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Location> Locations { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<WorkTask> WorkTasks { get; }
    DbSet<TaskAssignee> TaskAssignees { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
