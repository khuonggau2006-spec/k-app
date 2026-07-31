using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskMgmt.Domain.Entities;
using TaskMgmt.Domain.Enums;

namespace TaskMgmt.Infrastructure.Persistence.Configurations;

public class TaskAssigneeConfiguration : IEntityTypeConfiguration<TaskAssignee>
{
    public void Configure(EntityTypeBuilder<TaskAssignee> builder)
    {
        builder.ToTable("TaskAssignees");

        builder.Property(a => a.Role).HasConversion<int>();

        // One role per user per task.
        builder.HasIndex(a => new { a.WorkTaskId, a.UserId }).IsUnique();

        // At most one Owner per task.
        builder.HasIndex(a => a.WorkTaskId)
            .IsUnique()
            .HasFilter($"\"{nameof(TaskAssignee.Role)}\" = {(int)TaskAssigneeRole.Owner}");

        builder.HasOne(a => a.WorkTask)
            .WithMany(t => t.Assignees)
            .HasForeignKey(a => a.WorkTaskId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.User)
            .WithMany(u => u.TaskAssignments)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
