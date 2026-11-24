using Microsoft.AspNetCore.Http;

namespace SecondHandMarketplace.Web.Common;

/// <summary>
/// Tiện ích upload và kiểm tra tính hợp lệ của file ảnh (Magic Bytes, đuôi mở rộng, kích thước) an toàn (Task 3.8).
/// </summary>
public static class UploadAnhAnToan
{
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    public const long DefaultMaxFileSize = 5 * 1024 * 1024; // 5 MB

    /// <summary>
    /// Kiểm tra định dạng đuôi, dung lượng và Magic Bytes của file ảnh.
    /// </summary>
    public static async Task<(bool HopLe, string? Loi, string? Extension)> KiemTraAnhHopLeAsync(
        IFormFile? file,
        long maxBytes = DefaultMaxFileSize,
        CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            return (false, "Vui lòng chọn tệp hình ảnh.", null);
        }

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
        {
            return (false, "Chỉ chấp nhận tệp ảnh JPG, JPEG, PNG hoặc WEBP.", null);
        }

        if (file.Length > maxBytes)
        {
            return (false, $"Kích thước ảnh không được vượt quá {maxBytes / (1024 * 1024)}MB.", null);
        }

        using var readStream = file.OpenReadStream();
        var header = new byte[12];
        var readBytes = await readStream.ReadAsync(header, 0, 12, cancellationToken);

        bool isImage = false;
        if (readBytes >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
        {
            isImage = true; // JPEG/JPG
        }
        else if (readBytes >= 4 && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47)
        {
            isImage = true; // PNG
        }
        else if (readBytes >= 12 && header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46
                                && header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50)
        {
            isImage = true; // WEBP (RIFF....WEBP)
        }

        if (!isImage)
        {
            return (false, "Nội dung tệp không phải là định dạng hình ảnh hợp lệ.", null);
        }

        return (true, null, ext);
    }

    /// <summary>
    /// Lưu file ảnh đã kiểm tra vào thư mục wwwroot/uploads/{subFolder} với tên file GUID ngẫu nhiên.
    /// Trả về đường dẫn tương đối (để lưu DB) và đường dẫn tuyệt đối (để quản lý/dọn dẹp).
    /// </summary>
    public static async Task<(string RelativePath, string FullPath)> LuuAnhAsync(
        IFormFile file,
        string webRootPath,
        string subFolder,
        string extension,
        CancellationToken cancellationToken = default)
    {
        var folder = Path.Combine(webRootPath, "uploads", subFolder);
        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(folder, fileName);

        using (var readStream = file.OpenReadStream())
        using (var writeStream = new FileStream(fullPath, FileMode.Create))
        {
            await readStream.CopyToAsync(writeStream, cancellationToken);
        }

        var relativePath = $"/uploads/{subFolder}/{fileName}";
        return (relativePath, fullPath);
    }

    /// <summary>
    /// Xóa an toàn file ảnh trên đĩa (bỏ qua nếu không tồn tại hoặc có lỗi).
    /// </summary>
    public static void XoaAnh(string? fullPath)
    {
        if (string.IsNullOrWhiteSpace(fullPath)) return;
        try
        {
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }
        catch
        {
            // Bỏ qua lỗi xóa file rác (best-effort)
        }
    }
}
