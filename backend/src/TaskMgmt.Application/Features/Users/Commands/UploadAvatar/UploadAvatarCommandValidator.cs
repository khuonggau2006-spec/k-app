using FluentValidation;
using TaskMgmt.Application.Features.Attachments.Common;

namespace TaskMgmt.Application.Features.Users.Commands.UploadAvatar;

public class UploadAvatarCommandValidator : AbstractValidator<UploadAvatarCommand>
{
    private const long MaxSizeBytes = 5 * 1024 * 1024;
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];

    public UploadAvatarCommandValidator()
    {
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(255);

        RuleFor(x => x.FileName)
            .Must(fileName => AllowedExtensions.Contains(Path.GetExtension(fileName), StringComparer.OrdinalIgnoreCase))
            .WithMessage("Chỉ nhận ảnh JPG/PNG/WEBP.");

        RuleFor(x => x.SizeBytes)
            .GreaterThan(0)
            .WithMessage("Ảnh rỗng.")
            .LessThanOrEqualTo(MaxSizeBytes)
            .WithMessage("Dung lượng ảnh vượt quá 5MB.");

        // Đối chiếu magic bytes với định dạng khai báo qua đuôi file để chặn file giả mạo đuôi.
        RuleFor(x => x)
            .Must(x =>
            {
                if (!AllowedExtensions.Contains(Path.GetExtension(x.FileName), StringComparer.OrdinalIgnoreCase)
                    || !AttachmentFileValidator.TryGetAllowedContentType(x.FileName, out var contentType))
                {
                    return true; // đã báo lỗi ở rule FileName phía trên, tránh trùng lỗi.
                }

                Span<byte> header = stackalloc byte[16];
                var bytesRead = x.Content.Read(header);
                x.Content.Position = 0;
                return AttachmentFileValidator.MatchesSignature(contentType, header[..bytesRead]);
            })
            .WithMessage("Nội dung ảnh không khớp với định dạng khai báo.")
            .WithName(nameof(UploadAvatarCommand.FileName));
    }
}
