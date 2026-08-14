using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskMgmt.Domain.Entities;

namespace TaskMgmt.Infrastructure.Persistence.Configurations;

public class WorkTaskConfiguration : IEntityTypeConfiguration<WorkTask>
{
    public void Configure(EntityTypeBuilder<WorkTask> builder)
    {
        builder.ToTable("WorkTasks");

        builder.Property(t => t.Title).HasMaxLength(200).IsRequired();
        builder.Property(t => t.Status).HasConversion<int>();

        // Mọi query đọc WorkTask đều mở đầu bằng Where(IsActive) - đặt IsActive làm cột dẫn đầu
        // để Postgres dùng được index cho cả 2 kiểu lọc phổ biến: theo Status/Location (danh sách,
        // dashboard) và theo khoảng DueDateUtc (dashboard quá hạn/sắp hạn, job nhắc hạn).
        builder.HasIndex(t => new { t.IsActive, t.Status, t.LocationId });
        builder.HasIndex(t => new { t.IsActive, t.DueDateUtc, t.Status });

        builder.HasOne(t => t.ParentTask)
            .WithMany(t => t.SubTasks)
            .HasForeignKey(t => t.ParentTaskId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Location)
            .WithMany(l => l.WorkTasks)
            .HasForeignKey(t => t.LocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Domain.Entities.User>()
            .WithMany()
            .HasForeignKey(t => t.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Domain.Entities.User>()
            .WithMany()
            .HasForeignKey(t => t.UpdatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
