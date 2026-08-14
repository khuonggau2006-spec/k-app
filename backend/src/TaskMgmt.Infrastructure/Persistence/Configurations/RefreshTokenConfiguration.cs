using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskMgmt.Domain.Entities;

namespace TaskMgmt.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.Property(t => t.Token).HasMaxLength(200).IsRequired();
        builder.HasIndex(t => t.Token).IsUnique();

        // CleanupExpiredTokensJob quét theo ExpiresAtUtc mỗi ngày - bảng này tăng dần theo mỗi lần
        // login/refresh nên cần index để job không phải seq scan toàn bảng.
        builder.HasIndex(t => t.ExpiresAtUtc);

        builder.Ignore(t => t.IsActive);

        builder.HasOne(t => t.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
