using Microsoft.EntityFrameworkCore;
using TaskMgmt.Application.Common.Interfaces;

namespace TaskMgmt.Application.Features.WorkTasks.Common;

internal static class WorkTaskHierarchy
{
    public const int MaxLevel = 3;
    private const int MaxWalkSteps = MaxLevel + 5;

    public static async Task<int> GetLevelAsync(IApplicationDbContext context, Guid taskId, CancellationToken cancellationToken)
    {
        var level = 1;
        Guid? currentId = taskId;

        for (var i = 0; i < MaxWalkSteps; i++)
        {
            var parentId = await context.WorkTasks
                .Where(t => t.Id == currentId)
                .Select(t => t.ParentTaskId)
                .FirstOrDefaultAsync(cancellationToken);

            if (parentId is null)
            {
                break;
            }

            level++;
            currentId = parentId;
        }

        return level;
    }

    public static bool ExceedsMaxDepth(int parentLevel) => parentLevel + 1 > MaxLevel;

    // Walks up from newParentId; if it reaches taskId, assigning newParentId as
    // taskId's parent would create a cycle (newParentId is currently a descendant of taskId).
    public static async Task<bool> WouldCreateCycleAsync(
        IApplicationDbContext context,
        Guid taskId,
        Guid newParentId,
        CancellationToken cancellationToken)
    {
        Guid? currentId = newParentId;

        for (var i = 0; i < MaxWalkSteps && currentId is not null; i++)
        {
            if (currentId == taskId)
            {
                return true;
            }

            currentId = await context.WorkTasks
                .Where(t => t.Id == currentId)
                .Select(t => t.ParentTaskId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return false;
    }
}
