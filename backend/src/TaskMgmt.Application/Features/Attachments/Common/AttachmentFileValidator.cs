namespace TaskMgmt.Application.Features.Attachments.Common;

// Whitelist định dạng file được phép upload + kiểm tra magic bytes để chặn file đổi đuôi
// giả mạo (ví dụ file thực thi đổi tên thành .jpg). Cố tình KHÔNG cho phép .svg/.html dù
// trông giống ảnh/tài liệu vì có thể chứa script (nguy cơ stored XSS).
public static class AttachmentFileValidator
{
    private static readonly Dictionary<string, string> AllowedExtensionToContentType = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".gif"] = "image/gif",
        [".webp"] = "image/webp",
        [".pdf"] = "application/pdf",
        [".doc"] = "application/msword",
        [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        [".xls"] = "application/vnd.ms-excel",
        [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        [".ppt"] = "application/vnd.ms-powerpoint",
        [".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        [".txt"] = "text/plain",
        [".csv"] = "text/csv",
    };

    // Chữ ký nhị phân (magic bytes) đã biết cho từng content-type. Loại không có trong danh
    // sách này (txt/csv - dữ liệu văn bản thuần, không có chữ ký cố định) sẽ bỏ qua bước này.
    private static readonly Dictionary<string, byte[][]> SignaturesByContentType = new()
    {
        ["image/jpeg"] = [[0xFF, 0xD8, 0xFF]],
        ["image/png"] = [[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]],
        ["image/gif"] = [[0x47, 0x49, 0x46, 0x38]],
        ["application/pdf"] = [[0x25, 0x50, 0x44, 0x46]],
        // Legacy OLE format dùng chung cho .doc/.xls/.ppt cũ.
        ["application/msword"] = [[0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1]],
        ["application/vnd.ms-excel"] = [[0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1]],
        ["application/vnd.ms-powerpoint"] = [[0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1]],
        // OOXML (.docx/.xlsx/.pptx) đều là file ZIP nên chung chữ ký PK.
        ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"] = [[0x50, 0x4B, 0x03, 0x04]],
        ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"] = [[0x50, 0x4B, 0x03, 0x04]],
        ["application/vnd.openxmlformats-officedocument.presentationml.presentation"] = [[0x50, 0x4B, 0x03, 0x04]],
    };

    public static bool TryGetAllowedContentType(string fileName, out string contentType)
    {
        var extension = Path.GetExtension(fileName);
        return AllowedExtensionToContentType.TryGetValue(extension, out contentType!);
    }

    // WebP thực chất là RIFF container ("RIFF"....."WEBP"), chữ ký không nằm liền ở đầu file
    // nên xử lý riêng thay vì đưa vào SignaturesByContentType.
    public static bool MatchesSignature(string contentType, ReadOnlySpan<byte> header)
    {
        if (contentType == "image/webp")
        {
            return header.Length >= 12
                && header[..4].SequenceEqual("RIFF"u8)
                && header.Slice(8, 4).SequenceEqual("WEBP"u8);
        }

        if (!SignaturesByContentType.TryGetValue(contentType, out var signatures))
        {
            // Không có chữ ký tham chiếu (vd. text/plain, text/csv) - bỏ qua kiểm tra nhị phân.
            return true;
        }

        foreach (var signature in signatures)
        {
            if (header.Length >= signature.Length && header[..signature.Length].SequenceEqual(signature))
            {
                return true;
            }
        }

        return false;
    }
}
