
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

namespace SecondHandMarketplace.Web.Services;

public sealed class ImageUploadService : IImageUploadService
{
    private const long MaxFileSize = 5 * 1024 * 1024; // 5 MB

    private readonly string _uploadDirectory;

    public ImageUploadService(IWebHostEnvironment environment)
    {
        _uploadDirectory = Path.Combine(
            environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot"),
            "uploads");

        Directory.CreateDirectory(_uploadDirectory);
    }

    public async Task<ImageUploadResult> UploadAsync(
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0)
        {
            return Fail("Vui lòng chọn một file ảnh không rỗng.");
        }

        if (file.Length > MaxFileSize)
        {
            return Fail("Dung lượng ảnh tối đa là 5 MB.");
        }

        // Đọc tối đa 12 byte để nhận diện chữ ký file.
        byte[] header = new byte[12];

        await using (var input = file.OpenReadStream())
        {
            int bytesRead = await input.ReadAsync(
                header.AsMemory(0, header.Length), cancellationToken);

            if (bytesRead < 3)
            {
                return Fail("File không phải ảnh hợp lệ.");
            }
        }

        string? extension = DetectImageExtension(header);

        if (extension is null)
        {
            return Fail("Chỉ chấp nhận ảnh JPEG, PNG hoặc WebP hợp lệ.");
        }

        // Tên file do server tạo, không sử dụng tên từ người dùng.
        string fileName = $"{Guid.NewGuid():N}{extension}";
        string fullPath = Path.Combine(_uploadDirectory, fileName);

        try
        {
            await using var input = file.OpenReadStream();
            await using var output = new FileStream(
                fullPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                useAsync: true);

            await input.CopyToAsync(output, cancellationToken);

            return new ImageUploadResult(
                true,
                fileName,
                $"/uploads/{fileName}",
                null);
        }
        catch (OperationCanceledException)
        {
            TryDelete(fullPath);
            throw;
        }
        catch (IOException)
        {
            TryDelete(fullPath);
            return Fail("Không thể lưu ảnh. Vui lòng thử lại.");
        }
        catch
        {
            TryDelete(fullPath);
            throw;
        }
    }

    public Task<bool> DeleteAsync(string fileName)
    {
        // Chỉ chấp nhận tên GUID được dịch vụ tạo ra.
        if (string.IsNullOrWhiteSpace(fileName) ||
            !Regex.IsMatch(
                fileName,
                @"\A[a-fA-F0-9]{32}\.(jpg|png|webp)\z",
                RegexOptions.IgnoreCase))
        {
            return Task.FromResult(false);
        }

        string fullPath = Path.Combine(_uploadDirectory, fileName);

        if (!File.Exists(fullPath))
        {
            return Task.FromResult(false);
        }

        File.Delete(fullPath);
        return Task.FromResult(true);
    }

    private static string? DetectImageExtension(byte[] bytes)
    {
        // JPEG: FF D8 FF
        if (bytes.Length >= 3 &&
            bytes[0] == 0xFF &&
            bytes[1] == 0xD8 &&
            bytes[2] == 0xFF)
        {
            return ".jpg";
        }

        // PNG: 89 50 4E 47 0D 0A 1A 0A
        if (bytes.Length >= 8 &&
            bytes[0] == 0x89 &&
            bytes[1] == 0x50 &&
            bytes[2] == 0x4E &&
            bytes[3] == 0x47 &&
            bytes[4] == 0x0D &&
            bytes[5] == 0x0A &&
            bytes[6] == 0x1A &&
            bytes[7] == 0x0A)
        {
            return ".png";
        }

        // WebP: "RIFF" ở đầu, "WEBP" tại byte 8.
        if (bytes.Length >= 12 &&
            bytes[0] == (byte)'R' &&
            bytes[1] == (byte)'I' &&
            bytes[2] == (byte)'F' &&
            bytes[3] == (byte)'F' &&
            bytes[8] == (byte)'W' &&
            bytes[9] == (byte)'E' &&
            bytes[10] == (byte)'B' &&
            bytes[11] == (byte)'P')
        {
            return ".webp";
        }

        return null;
    }

    private static ImageUploadResult Fail(string message) =>
        new(false, null, null, message);

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
            // Không che mất lỗi ban đầu.
        }
        catch (UnauthorizedAccessException)
        {
            // Không che mất lỗi ban đầu.
        }
    }
}
