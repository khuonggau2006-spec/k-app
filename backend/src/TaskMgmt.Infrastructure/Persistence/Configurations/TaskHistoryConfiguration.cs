using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskMgmt.Domain.Entities;

namespace TaskMgmt.Infrastructure.Persistence.Configurations;

public class TaskHistoryConfiguration : IEntityTypeConfiguration<TaskHistory>
{
    public void Configure(EntityTypeBuilder<TaskHistory> builder)
    {
        builder.ToTable("TaskHistories");

        builder.Property(h => h.ActionType).HasConversion<int>();
        builder.Property(h => h.Description).HasMaxLength(1000).IsRequired();
        builder.Property(h => h.FieldName).HasMaxLength(100);
        builder.Property(h => h.OldValue).HasMaxLength(1000);
        builder.Property(h => h.NewValue).HasMaxLength(1000);

        builder.HasIndex(h => h.WorkTaskId);
        builder.HasIndex(h => h.ActionType);
        builder.HasIndex(h => h.ActorUserId);

        builder.HasOne(h => h.WorkTask)
            .WithMany(t => t.Histories)
            .HasForeignKey(h => h.WorkTaskId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(h => h.Actor)
            .WithMany()
            .HasForeignKey(h => h.ActorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(h => h.Target)
            .WithMany()
            .HasForeignKey(h => h.TargetUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
