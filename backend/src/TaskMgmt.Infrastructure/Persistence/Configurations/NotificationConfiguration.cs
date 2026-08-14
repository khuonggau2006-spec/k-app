using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskMgmt.Domain.Entities;

namespace TaskMgmt.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");

        builder.Property(n => n.Title).HasMaxLength(200).IsRequired();
        builder.Property(n => n.Body).HasMaxLength(1000).IsRequired();
        builder.Property(n => n.Type).HasMaxLength(50).IsRequired();

        // Thêm CreatedAtUtc vào cuối để phủ luôn ORDER BY CreatedAtUtc DESC trong GetNotificationsQuery,
        // tránh bước sort riêng sau khi lọc theo UserId/IsRead.
        builder.HasIndex(n => new { n.UserId, n.IsRead, n.CreatedAtUtc });
        builder.HasIndex(n => n.WorkTaskId);

        builder.HasOne(n => n.User)
            .WithMany(u => u.Notifications)
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(n => n.WorkTask)
            .WithMany()
            .HasForeignKey(n => n.WorkTaskId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
