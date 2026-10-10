
using Microsoft.AspNetCore.Mvc;
using SecondHandMarketplace.Web.Services;

namespace SecondHandMarketplace.Web.Controllers;

[ApiController]
[Route("api/images")]
public sealed class ImageUploadController : ControllerBase
{
    private readonly IImageUploadService _imageUploadService;

    public ImageUploadController(IImageUploadService imageUploadService)
    {
        _imageUploadService = imageUploadService;
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<IActionResult> Upload(
        [FromForm] IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (file is null)
        {
            return BadRequest(new
            {
                message = "Thiếu file ảnh. Hãy gửi file với tên trường 'file'."
            });
        }

        var result = await _imageUploadService.UploadAsync(
            file, cancellationToken);

        if (!result.Success)
        {
            return BadRequest(new { message = result.Error });
        }

        return Ok(new
        {
            message = "Upload ảnh thành công.",
            fileName = result.FileName,
            url = result.Url
        });
    }

    [HttpDelete("{fileName}")]
    [RequestSizeLimit(1024)]
    public async Task<IActionResult> Delete(string fileName)
    {
        bool deleted = await _imageUploadService.DeleteAsync(fileName);

        if (!deleted)
        {
            return NotFound(new
            {
                message = "Không tìm thấy ảnh hoặc tên file không hợp lệ."
            });
        }

        return Ok(new { message = "Xóa ảnh thành công." });
    }
}
