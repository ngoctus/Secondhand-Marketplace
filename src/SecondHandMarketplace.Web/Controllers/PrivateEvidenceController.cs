using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecondHandMarketplace.Web.Common.Extensions;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Services.Storage;

namespace SecondHandMarketplace.Web.Controllers;

[Authorize]
[Route("private-evidence")]
public sealed class PrivateEvidenceController(
    AppDbContext db,
    IUploadStorage uploadStorage) : Controller
{
    [HttpGet("disputes/{id:int}/{kind}")]
    public async Task<IActionResult> Dispute(int id, string kind, CancellationToken cancellationToken)
    {
        if (kind is not ("buyer" or "seller")) return NotFound();
        var dispute = await db.TranhChaps.AsNoTracking()
            .Include(x => x.DonHang).ThenInclude(x => x.CuaHang)
            .FirstOrDefaultAsync(x => x.MaTranhChap == id, cancellationToken);
        if (dispute is null) return NotFound();

        var userId = User.LayNguoiDungId();
        var isAdmin = User.IsInRole("ADMIN");
        var isBuyer = userId == dispute.DonHang.MaNguoiDung;
        var isSeller = userId == dispute.DonHang.CuaHang.MaNguoiDung;
        if (!isAdmin && !isBuyer && !isSeller) return Forbid();

        var path = kind == "seller" ? dispute.AnhPhanHoi : dispute.BangChungAnh;
        return await ServeEvidenceAsync(path, cancellationToken);
    }

    [HttpGet("account-appeals/{id:int}")]
    public async Task<IActionResult> AccountAppeal(int id, CancellationToken cancellationToken)
    {
        var appeal = await db.KhieuNaiTaiKhoans.AsNoTracking()
            .FirstOrDefaultAsync(x => x.MaKhieuNai == id, cancellationToken);
        if (appeal is null) return NotFound();
        if (!User.IsInRole("ADMIN") && User.LayNguoiDungId() != appeal.MaNguoiDung) return Forbid();
        return await ServeEvidenceAsync(appeal.BangChungAnh, cancellationToken);
    }

    private async Task<IActionResult> ServeEvidenceAsync(string? storedPath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(storedPath)) return NotFound();
        var bytes = await uploadStorage.ReadAsync(storedPath, isPrivate: true, cancellationToken);
        // Existing installations may still have evidence under wwwroot/uploads; only this authorized endpoint serves it.
        if (bytes is null)
            bytes = await uploadStorage.ReadAsync(storedPath, isPrivate: false, cancellationToken);
        if (bytes is null) return NotFound();

        Response.Headers.CacheControl = "private, no-store";
        return File(bytes, Path.GetExtension(storedPath).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        });
    }
}
