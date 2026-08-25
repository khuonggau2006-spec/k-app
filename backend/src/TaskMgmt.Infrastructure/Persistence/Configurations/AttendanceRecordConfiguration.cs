using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskMgmt.Domain.Entities;

namespace TaskMgmt.Infrastructure.Persistence.Configurations;

public class AttendanceRecordConfiguration : IEntityTypeConfiguration<AttendanceRecord>
{
    public void Configure(EntityTypeBuilder<AttendanceRecord> builder)
    {
        builder.ToTable("AttendanceRecords");

        builder.HasIndex(a => new { a.UserId, a.WorkDate }).IsUnique();

        builder.HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.CheckInLocation)
            .WithMany()
            .HasForeignKey(a => a.CheckInLocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.CheckOutLocation)
            .WithMany()
            .HasForeignKey(a => a.CheckOutLocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Domain.Entities.User>()
            .WithMany()
            .HasForeignKey(a => a.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Domain.Entities.User>()
            .WithMany()
            .HasForeignKey(a => a.UpdatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
