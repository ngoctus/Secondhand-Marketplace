using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SecondHandMarketplace.Web.Common;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Models.Entities;
using SecondHandMarketplace.Web.Models.ViewModels.Products;
using SecondHandMarketplace.Web.Models.ViewModels.Shop;
using SecondHandMarketplace.Web.Services.AuditLogs;

namespace SecondHandMarketplace.Web.Services.Shops;

public sealed class ShopService(AppDbContext db, IAuditLogService auditLog) : IShopService
{
    public Task<CuaHang?> GetByOwnerAsync(int userId, CancellationToken cancellationToken = default) =>
        db.CuaHangs.AsNoTracking().SingleOrDefaultAsync(x => x.MaNguoiDung == userId, cancellationToken);

    public async Task<(bool Success, string? Error)> SaveAsync(int userId, ShopSaveInput input, CancellationToken cancellationToken = default)
    {
        var name = input.Name.Trim();
        var phone = input.Phone.Trim();
        var description = Clean(input.Description);
        var logo = Clean(input.Logo);
        if (!string.IsNullOrWhiteSpace(logo) && !UrlAnToan.HopLe(logo))
            return (false, "Ảnh logo phải là URL http/https hoặc đường dẫn hợp lệ.");
        var addressDetail = Clean(input.DiaChiChiTiet);
        var areaLabel = Clean(input.KhuVucHienThi);

        // 1. Địa chỉ: tỉnh và xã bắt buộc, xã phải thuộc đúng tỉnh, địa chỉ chi tiết bắt buộc.
        var locationError = await ValidateLocationAsync(input.MaTinhThanh, input.MaPhuongXa, addressDetail, areaLabel, cancellationToken);
        if (locationError is not null) return (false, locationError);

        // 2. Ngân hàng: tùy chọn, nhưng đã nhập thì phải đủ cả ba.
        var (bank, bankError) = ValidateBank(input.MaNganHang, input.SoTaiKhoan, input.TenChuTaiKhoan);
        if (bankError is not null) return (false, bankError);

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

            // Procedure cũ chưa biết các cột mới nên ghi tiếp địa chỉ/ngân hàng cho shop vừa tạo.
            shop = await db.CuaHangs.SingleAsync(x => x.MaCuaHang == (int)shopId.Value, cancellationToken);
            ApplyExtras(shop, input, addressDetail, areaLabel, bank);
            try { await db.SaveChangesAsync(cancellationToken); }
            catch (DbUpdateException) { return (false, "Đã tạo cửa hàng nhưng chưa lưu được địa chỉ, vui lòng cập nhật lại."); }
            return (true, null);
        }

        if (shop.TrangThai == "LOCKED") return (false, "Cửa hàng đang bị khóa, không thể chỉnh sửa.");
        shop.TenShop = name;
        shop.SoDienThoaiLienHe = phone;
        shop.MoTa = description;
        shop.Logo = logo;
        ApplyExtras(shop, input, addressDetail, areaLabel, bank);
        shop.NgayCapNhat = DateTime.UtcNow;
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException) { return (false, "Thông tin cửa hàng không hợp lệ."); }
        await auditLog.LogAsync(userId, "UPDATE_SHOP", "CuaHang", shop.MaCuaHang);
        return (true, null);
    }

    public async Task<ShopPublicVm?> GetPublicShopAsync(int id, int page = 1, int pageSize = 12, CancellationToken cancellationToken = default)
    {
        page = Math.Clamp(page, 1, 10000);
        pageSize = Math.Clamp(pageSize, 1, 48);

        // Chỉ chọn đúng các cột công khai: SĐT, địa chỉ chi tiết, ngân hàng không bao giờ được nạp lên.
        var shop = await db.CuaHangs.AsNoTracking()
            .Where(x => x.MaCuaHang == id && x.TrangThai == "ACTIVE" && x.NguoiDung.TrangThai == "ACTIVE")
            .Select(x => new ShopPublicVm
            {
                Id = x.MaCuaHang,
                Name = x.TenShop,
                Logo = x.Logo,
                Description = x.MoTa,
                JoinedAtUtc = x.NgayTao,
                WardName = x.PhuongXa != null ? x.PhuongXa.TenPhuongXa : null,
                ProvinceName = x.TinhThanh != null ? x.TinhThanh.TenTinhThanh : null
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (shop is null) return null;

        var products = db.SanPhams.AsNoTracking().Where(p => p.MaCuaHang == id && p.TrangThai == "ACTIVE" && !p.DaXoa);
        var total = await products.CountAsync(cancellationToken);
        var items = await products
            .OrderByDescending(p => p.NgayTao).ThenByDescending(p => p.MaSanPham)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(p => new ProductCardVm
            {
                Id = p.MaSanPham,
                Name = p.Ten,
                Price = p.Gia ?? 0m,
                ImageUrl = p.Anh.Where(a => a.LaAnhChinh).Select(a => a.DuongDan).FirstOrDefault(),
                ShopName = p.CuaHang.TenShop,
                ConditionName = p.TinhTrang != null ? p.TinhTrang.Ten : null,
                CategoryName = p.DanhMuc != null ? p.DanhMuc.Ten : null
            })
            .ToListAsync(cancellationToken);
        shop.Products = new PagedResult<ProductCardVm>(items, total, page, pageSize);

        // Điểm và số đơn đọc từ vw_DiemCuaHang (module C). Chưa có view/đánh giá thì để null, view hiện "Chưa có đánh giá".
        try
        {
            var diem = (await db.Database
                .SqlQuery<DiemShopRow>($"SELECT DiemTrungBinh, SoDanhGia, SoDonThanhCong FROM dbo.vw_DiemCuaHang WHERE MaCuaHang = {id}")
                .ToListAsync(cancellationToken)).FirstOrDefault();
            if (diem is not null)
            {
                shop.CompletedOrders = diem.SoDonThanhCong;
                if (diem.SoDanhGia > 0)
                {
                    shop.AverageRating = diem.DiemTrungBinh;
                    shop.ReviewCount = diem.SoDanhGia;
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // view chưa tồn tại (chưa chạy 11_danhgia.sql) hoặc provider không hỗ trợ SQL thô: bỏ qua điểm.
        }

        return shop;
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

    private async Task<string?> ValidateLocationAsync(int? maTinh, int? maXa, string? addressDetail, string? areaLabel, CancellationToken ct)
    {
        if (maTinh is null or <= 0) return "Vui lòng chọn tỉnh/thành phố.";
        if (maXa is null or <= 0) return "Vui lòng chọn phường/xã.";
        if (addressDetail is null) return "Vui lòng nhập địa chỉ chi tiết.";
        if (addressDetail.Length > 300) return "Địa chỉ chi tiết tối đa 300 ký tự.";
        if (areaLabel is { Length: > 100 }) return "Tên khu vực tối đa 100 ký tự.";

        if (!await db.TinhThanhs.AsNoTracking().AnyAsync(x => x.MaTinhThanh == maTinh, ct))
            return "Tỉnh/thành phố không tồn tại.";
        // Xã phải thuộc đúng tỉnh đã chọn, nếu không thì từ chối (chặn dữ liệu giả mạo từ form/API).
        if (!await db.PhuongXas.AsNoTracking().AnyAsync(x => x.MaPhuongXa == maXa && x.MaTinhThanh == maTinh, ct))
            return "Phường/xã không thuộc tỉnh/thành phố đã chọn.";
        return null;
    }

    /// <summary>Không nhập gì = bỏ qua (shop không có thông tin nhận chuyển khoản). Có nhập thì phải đủ ngân hàng, số tài khoản, tên chủ tài khoản.</summary>
    private static (BankInfo? Bank, string? Error) ValidateBank(string? bin, string? account, string? holder)
    {
        bin = Clean(bin);
        account = Clean(account);
        holder = Clean(holder);
        if (bin is null && account is null && holder is null) return (null, null);

        if (bin is null) return (null, "Vui lòng chọn ngân hàng nhận chuyển khoản.");
        if (account is null) return (null, "Vui lòng nhập số tài khoản ngân hàng.");
        if (holder is null) return (null, "Vui lòng nhập tên chủ tài khoản.");

        var nganHang = VietQrBanks.TimTheoBin(bin);
        if (nganHang is null) return (null, "Ngân hàng không nằm trong danh sách hỗ trợ VietQR.");
        if (account.Length > 30 || !account.All(char.IsAsciiDigit)) return (null, "Số tài khoản chỉ gồm chữ số (tối đa 30 số).");
        if (holder.Length > 100) return (null, "Tên chủ tài khoản tối đa 100 ký tự.");
        return (new BankInfo(nganHang.Bin, nganHang.TenNgan, account, holder), null);
    }

    private static void ApplyExtras(CuaHang shop, ShopSaveInput input, string? addressDetail, string? areaLabel, BankInfo? bank)
    {
        shop.MaTinhThanh = input.MaTinhThanh;
        shop.MaPhuongXa = input.MaPhuongXa;
        shop.DiaChiChiTiet = addressDetail;
        shop.KhuVucHienThi = areaLabel;
        shop.MaNganHang = bank?.Bin;
        shop.NganHang = bank?.Name;
        shop.SoTaiKhoan = bank?.Account;
        shop.TenChuTaiKhoan = bank?.Holder;
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed record BankInfo(string Bin, string Name, string Account, string Holder);
}

/// <summary>Một dòng của vw_DiemCuaHang (EF cần kiểu không lồng trong lớp private để dựng đối tượng).</summary>
internal sealed class DiemShopRow
{
    public decimal DiemTrungBinh { get; set; }
    public int SoDanhGia { get; set; }
    public int SoDonThanhCong { get; set; }
}
