using Microsoft.EntityFrameworkCore;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Models.ViewModels.Cart;

namespace SecondHandMarketplace.Web.Services.Carts;

public class CartService : ICartService
{
    private readonly AppDbContext _db;

    public CartService(AppDbContext db) => _db = db;

    public async Task<CartIndexViewModel> LayGioHangAsync(int maNguoiDung)
    {
        var rawItems = await _db.GioHangs
            .Where(x => x.MaNguoiDung == maNguoiDung)
            .OrderByDescending(x => x.ThoiGianThem)
            .Select(x => new CartItemViewModel
            {
                MaGioHang = x.MaGioHang,
                MaSanPham = x.MaSanPham,
                TenSanPham = x.SanPham.Ten,
                Gia = x.SanPham.Gia ?? 0,
                DaChon = x.DaChon,
                MaCuaHang = x.SanPham.MaCuaHang,
                TenShop = x.SanPham.CuaHang.TenShop,
                AnhDaiDien = x.SanPham.Anh
                    .Where(a => a.LaAnhChinh)
                    .Select(a => a.DuongDan)
                    .FirstOrDefault()
                    ?? x.SanPham.Anh.Select(a => a.DuongDan).FirstOrDefault(),
                TrangThaiSanPham = x.SanPham.TrangThai,
                CoTheMua = x.SanPham.TrangThai == "ACTIVE" && !x.SanPham.DaXoa && x.SanPham.CuaHang.TrangThai == "ACTIVE"
            })
            .ToListAsync();

        var groups = rawItems
            .GroupBy(x => new { x.MaCuaHang, x.TenShop })
            .Select(g => new CartShopGroupViewModel
            {
                MaCuaHang = g.Key.MaCuaHang,
                TenShop = g.Key.TenShop,
                Items = g.ToList()
            })
            .ToList();

        return new CartIndexViewModel
        {
            DanhSachNhomShop = groups
        };
    }

    public async Task<(bool ThanhCong, string? ThongBao)> ThemVaoGioAsync(int maNguoiDung, int maSanPham)
    {
        try
        {
            // Gọi stored procedure sp_ThemVaoGio
            await _db.Database.ExecuteSqlInterpolatedAsync(
                $"EXEC sp_ThemVaoGio @MaNguoiDung={maNguoiDung}, @MaSanPham={maSanPham}");

            return (true, "Đã thêm sản phẩm vào giỏ hàng thành công.");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
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
            .Where(x => x.MaNguoiDung == maNguoiDung && x.SanPham.MaCuaHang == maCuaHang)
            .ToListAsync();

        if (items.Count == 0) return false;

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

    public async Task<int> DemSoLuongMonAsync(int maNguoiDung)
    {
        return await _db.GioHangs.CountAsync(x => x.MaNguoiDung == maNguoiDung);
    }
}
