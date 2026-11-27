using SecondHandMarketplace.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;
using SecondHandMarketplace.Web.Common;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Models.ViewModels.Cart;

namespace SecondHandMarketplace.Web.Services.Carts;

public class CartService : ICartService
{
    private readonly AppDbContext _db;

    public CartService(AppDbContext db) => _db = db;

    public async Task<CartIndexViewModel> LayGioHangAsync(int maNguoiDung)
    {
        // Kiến trúc: Sử dụng EF Core LINQ với Include/ThenInclude tương đương logic view vw_GioHangChiTiet,
        // đồng thời tương thích hoàn toàn trên cả SQL Server và môi trường test In-Memory.
        var rawItems = await _db.GioHangs
            .AsNoTracking()
            .Include(x => x.SanPham)
                .ThenInclude(s => s.CuaHang)
                    .ThenInclude(c => c.NguoiDung)
            .Include(x => x.SanPham)
                .ThenInclude(s => s.Anh)
            .Where(x => x.MaNguoiDung == maNguoiDung)
            .OrderByDescending(x => x.ThoiGianThem)
            .ToListAsync();

        var cartItems = rawItems.Select(gh =>
        {
            var sp = gh.SanPham;
            var ch = sp?.CuaHang;
            var tonKho = sp?.SoLuong ?? 0;
            var coTheMua = sp != null && !sp.DaXoa && sp.TrangThai == "ACTIVE" && tonKho > 0
                           && ch != null && ch.TrangThai == "ACTIVE"
                           && ch.NguoiDung != null && ch.NguoiDung.TrangThai == "ACTIVE"
                           && ch.MaNguoiDung != maNguoiDung;

            return new CartItemViewModel
            {
                MaGioHang = gh.MaGioHang,
                MaSanPham = gh.MaSanPham,
                TenSanPham = sp?.Ten ?? "Sản phẩm không tồn tại",
                Gia = sp?.Gia ?? 0,
                SoLuong = tonKho > 0 ? Math.Clamp(gh.SoLuong, 1, tonKho) : Math.Max(1, gh.SoLuong),
                SoLuongTon = tonKho,
                DaChon = gh.DaChon,
                CoTheMua = coTheMua,
                TrangThaiSanPham = sp?.TrangThai ?? "UNKNOWN",
                MaCuaHang = sp?.MaCuaHang ?? 0,
                TenShop = ch?.TenShop ?? "Cửa hàng không xác định",
                AnhDaiDien = sp?.Anh.Where(a => a.LaAnhChinh).Select(a => a.DuongDan).FirstOrDefault()
                             ?? sp?.Anh.Select(a => a.DuongDan).FirstOrDefault()
            };
        }).ToList();

        var shopGroups = cartItems
            .GroupBy(x => new { x.MaCuaHang, x.TenShop })
            .Select(g => new CartShopGroupViewModel
            {
                MaCuaHang = g.Key.MaCuaHang,
                TenShop = g.Key.TenShop,
                Items = g.ToList()
            }).ToList();

        return new CartIndexViewModel
        {
            DanhSachNhomShop = shopGroups
        };
    }

    public async Task<(bool ThanhCong, string? ThongBao)> ThemVaoGioAsync(int maNguoiDung, int maSanPham, int soLuong = 1)
    {
        try
        {
            soLuong = Math.Clamp(soLuong, 1, 9999);

            if (_db.Database.IsRelational())
            {
                // Gọi stored procedure sp_ThemVaoGio (tự cộng dồn số lượng nếu đã có trong giỏ và chặn vượt tồn kho)
                await _db.Database.ExecuteSqlInterpolatedAsync(
                    $"EXEC sp_ThemVaoGio @MaNguoiDung={maNguoiDung}, @MaSanPham={maSanPham}, @SoLuong={soLuong}");
            }
            else
            {
                // In-Memory fallback cho Unit Test
                var sp = await _db.SanPhams
                    .Include(s => s.CuaHang)
                    .FirstOrDefaultAsync(s => s.MaSanPham == maSanPham && !s.DaXoa);

                var existing = await _db.GioHangs.FirstOrDefaultAsync(g => g.MaNguoiDung == maNguoiDung && g.MaSanPham == maSanPham);
                if (existing != null)
                {
                    if (sp == null || existing.SoLuong >= sp.SoLuong)
                    {
                        return (true, "Sản phẩm này đã có sẵn trong giỏ hàng của bạn.");
                    }
                    existing.SoLuong = Math.Min(sp.SoLuong, existing.SoLuong + soLuong);
                    existing.DaChon = true;
                    await _db.SaveChangesAsync();
                    return (true, "Đã cập nhật số lượng sản phẩm trong giỏ hàng.");
                }

                if (sp == null || sp.TrangThai != "ACTIVE" || sp.SoLuong <= 0 || sp.CuaHang?.TrangThai != "ACTIVE")
                {
                    return (false, "Sản phẩm đã hết hàng hoặc không còn bán.");
                }

                if (sp.CuaHang.MaNguoiDung == maNguoiDung)
                {
                    return (false, "Không thể thêm sản phẩm của chính mình vào giỏ hàng.");
                }

                _db.GioHangs.Add(new GioHang
                {
                    MaNguoiDung = maNguoiDung,
                    MaSanPham = maSanPham,
                    SoLuong = Math.Min(sp.SoLuong, soLuong),
                    DaChon = true,
                    ThoiGianThem = DateTime.UtcNow
                });
                await _db.SaveChangesAsync();
            }

            return (true, "Đã thêm sản phẩm vào giỏ hàng thành công.");
        }
        catch (Exception ex)
        {
            return (false, SqlLoi.ThongBao(ex));
        }
    }

    public async Task<(bool ThanhCong, string? ThongBao, int SoLuongMoi)> CapNhatSoLuongAsync(int maNguoiDung, int maGioHang, int soLuong)
    {
        var item = await _db.GioHangs
            .Include(x => x.SanPham)
            .FirstOrDefaultAsync(x => x.MaGioHang == maGioHang && x.MaNguoiDung == maNguoiDung);

        if (item == null) return (false, "Không tìm thấy sản phẩm trong giỏ hàng.", 0);
        if (item.SanPham == null || item.SanPham.DaXoa || item.SanPham.TrangThai != "ACTIVE" || item.SanPham.SoLuong <= 0)
            return (false, "Sản phẩm đã hết hàng hoặc không còn bán.", item.SoLuong);

        var clamped = Math.Clamp(soLuong, 1, item.SanPham.SoLuong);
        item.SoLuong = clamped;
        await _db.SaveChangesAsync();

        var warning = soLuong > item.SanPham.SoLuong
            ? $"Chỉ còn {item.SanPham.SoLuong} sản phẩm trong kho."
            : null;
        return (true, warning, clamped);
    }

    public async Task<bool> CapNhatTrangThaiChonAsync(int maNguoiDung, int maGioHang, bool daChon)
    {
        var item = await _db.GioHangs
            .FirstOrDefaultAsync(x => x.MaGioHang == maGioHang && x.MaNguoiDung == maNguoiDung);

        if (item == null) return false;

        item.DaChon = daChon;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CapNhatChonTatCaTheoShopAsync(int maNguoiDung, int maCuaHang, bool daChon)
    {
        var items = await _db.GioHangs
            .Include(x => x.SanPham)
            .Where(x => x.MaNguoiDung == maNguoiDung && x.SanPham.MaCuaHang == maCuaHang)
            .ToListAsync();

        if (!items.Any()) return false;

        foreach (var item in items)
        {
            item.DaChon = daChon;
        }

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> XoaKhoiGioAsync(int maNguoiDung, int maGioHang)
    {
        var item = await _db.GioHangs
            .FirstOrDefaultAsync(x => x.MaGioHang == maGioHang && x.MaNguoiDung == maNguoiDung);

        if (item == null) return false;

        _db.GioHangs.Remove(item);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<int> XoaMonKhongKhaDungAsync(int maNguoiDung)
    {
        var items = await _db.GioHangs
            .Include(g => g.SanPham).ThenInclude(s => s.CuaHang).ThenInclude(c => c.NguoiDung)
            .Where(g => g.MaNguoiDung == maNguoiDung &&
                (g.SanPham == null || g.SanPham.DaXoa || g.SanPham.TrangThai != "ACTIVE" ||
                 g.SanPham.CuaHang == null || g.SanPham.CuaHang.TrangThai != "ACTIVE" ||
                 g.SanPham.CuaHang.NguoiDung == null || g.SanPham.CuaHang.NguoiDung.TrangThai != "ACTIVE"))
            .ToListAsync();
        if (items.Count == 0) return 0;
        _db.GioHangs.RemoveRange(items);
        await _db.SaveChangesAsync();
        return items.Count;
    }

    public async Task<int> DemSoLuongMonAsync(int maNguoiDung)
    {
        return await _db.GioHangs.CountAsync(x => x.MaNguoiDung == maNguoiDung);
    }
}
