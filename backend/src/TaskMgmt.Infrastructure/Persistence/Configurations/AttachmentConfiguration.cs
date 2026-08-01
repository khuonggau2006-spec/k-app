using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskMgmt.Domain.Entities;

namespace TaskMgmt.Infrastructure.Persistence.Configurations;

public class AttachmentConfiguration : IEntityTypeConfiguration<Attachment>
{
    public void Configure(EntityTypeBuilder<Attachment> builder)
    {
        builder.ToTable("Attachments");

        builder.Property(a => a.FileName).HasMaxLength(255).IsRequired();
        builder.Property(a => a.StorageKey).HasMaxLength(512).IsRequired();
        builder.Property(a => a.ContentType).HasMaxLength(150).IsRequired();

        builder.HasIndex(a => a.WorkTaskId);
        builder.HasIndex(a => a.StorageKey).IsUnique();

        builder.HasOne(a => a.WorkTask)
            .WithMany(t => t.Attachments)
            .HasForeignKey(a => a.WorkTaskId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Uploader)
            .WithMany()
            .HasForeignKey(a => a.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Domain.Entities.User>()
            .WithMany()
            .HasForeignKey(a => a.UpdatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
