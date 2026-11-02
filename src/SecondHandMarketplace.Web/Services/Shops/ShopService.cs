using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Models.Entities;
using SecondHandMarketplace.Web.Services.AuditLogs;

namespace SecondHandMarketplace.Web.Services.Shops;

public sealed class ShopService(AppDbContext db, IAuditLogService auditLog) : IShopService
{
    public Task<CuaHang?> GetByOwnerAsync(int userId, CancellationToken cancellationToken = default) =>
        db.CuaHangs.AsNoTracking().SingleOrDefaultAsync(x => x.MaNguoiDung == userId, cancellationToken);

    public async Task<(bool Success, string? Error)> SaveAsync(int userId, string name, string phone, string? description, string? logo, CancellationToken cancellationToken = default)
    {
        name = name.Trim();
        phone = phone.Trim();
        description = Clean(description);
        logo = Clean(logo);

        var shop = await db.CuaHangs.SingleOrDefaultAsync(x => x.MaNguoiDung == userId, cancellationToken);
        if (shop is null)
        {
            // sp_TaoCuaHang tự kiểm tra tài khoản, giới hạn 1 shop và tự ghi nhật ký.
            var shopId = new SqlParameter("@CuaHangId", SqlDbType.Int) { Direction = ParameterDirection.Output };
            try
            {
                await db.Database.ExecuteSqlRawAsync(
                    "EXEC dbo.sp_TaoCuaHang @MaNguoiDung, @TenShop, @SoDienThoaiLienHe, @MoTa, @Logo, @CuaHangId OUTPUT",
                    new object[]
                    {
                        new SqlParameter("@MaNguoiDung", SqlDbType.Int) { Value = userId },
                        new SqlParameter("@TenShop", SqlDbType.NVarChar, 100) { Value = name },
                        new SqlParameter("@SoDienThoaiLienHe", SqlDbType.VarChar, 15) { Value = phone },
                        new SqlParameter("@MoTa", SqlDbType.NVarChar, 1000) { Value = (object?)description ?? DBNull.Value },
                        new SqlParameter("@Logo", SqlDbType.VarChar, 500) { Value = (object?)logo ?? DBNull.Value },
                        shopId
                    }, cancellationToken);
            }
            catch (SqlException ex) when (ex.Number is 50001 or 50002)
            {
                return (false, ex.Message);
            }
            catch (SqlException)
            {
                return (false, "Không thể tạo cửa hàng, vui lòng kiểm tra lại thông tin.");
            }
            return (true, null);
        }

        if (shop.TrangThai == "LOCKED") return (false, "Cửa hàng đang bị khóa, không thể chỉnh sửa.");
        shop.TenShop = name;
        shop.SoDienThoaiLienHe = phone;
        shop.MoTa = description;
        shop.Logo = logo;
        shop.NgayCapNhat = DateTime.UtcNow;
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException) { return (false, "Thông tin cửa hàng không hợp lệ."); }
        await auditLog.LogAsync(userId, "UPDATE_SHOP", "CuaHang", shop.MaCuaHang);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> LockShopAsync(int? actorId, int shopId, string? reason, CancellationToken cancellationToken = default)
    {
        reason = reason?.Trim();
        if (string.IsNullOrEmpty(reason) || reason.Length > 500)
            return (false, "Lý do khóa bắt buộc và tối đa 500 ký tự.");
        var shop = await db.CuaHangs.SingleOrDefaultAsync(x => x.MaCuaHang == shopId, cancellationToken);
        if (shop is null) return (false, "Không tìm thấy cửa hàng.");
        if (shop.TrangThai == "LOCKED") return (true, null);

        shop.TrangThai = "LOCKED";
        shop.LyDoKhoa = reason;
        shop.ThoiGianKhoa = DateTime.UtcNow;
        shop.NgayCapNhat = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await auditLog.LogAsync(actorId, "LOCK_SHOP", "CuaHang", shopId, "SUCCESS", reason);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UnlockShopAsync(int? actorId, int shopId, CancellationToken cancellationToken = default)
    {
        var shop = await db.CuaHangs.SingleOrDefaultAsync(x => x.MaCuaHang == shopId, cancellationToken);
        if (shop is null) return (false, "Không tìm thấy cửa hàng.");
        if (shop.TrangThai != "LOCKED") return (true, null);

        shop.TrangThai = "ACTIVE";
        shop.LyDoKhoa = null;
        shop.ThoiGianKhoa = null;
        shop.NgayCapNhat = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        await auditLog.LogAsync(actorId, "UNLOCK_SHOP", "CuaHang", shopId);
        return (true, null);
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}