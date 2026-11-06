using Microsoft.EntityFrameworkCore;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Models.Entities;
using SecondHandMarketplace.Web.Models.ViewModels;
using SecondHandMarketplace.Web.Models.ViewModels.Products;

namespace SecondHandMarketplace.Web.Services.Favorites;

public class FavoriteService : IFavoriteService
{
    private readonly AppDbContext _db;

    public FavoriteService(AppDbContext db) => _db = db;

    public async Task<(bool ThanhCong, bool LaYeuThich)> ToggleAsync(int maNguoiDung, int maSanPham, CancellationToken cancellationToken = default)
    {
        var coSanPham = await _db.SanPhams.AsNoTracking()
            .AnyAsync(p => p.MaSanPham == maSanPham && !p.DaXoa, cancellationToken);
        if (!coSanPham) return (false, false);

        var existing = await _db.YeuThichs
            .FirstOrDefaultAsync(f => f.MaNguoiDung == maNguoiDung && f.MaSanPham == maSanPham, cancellationToken);
        if (existing is not null)
        {
            _db.YeuThichs.Remove(existing);
            await _db.SaveChangesAsync(cancellationToken);
            return (true, false);
        }

        _db.YeuThichs.Add(new YeuThich { MaNguoiDung = maNguoiDung, MaSanPham = maSanPham, NgayTao = DateTime.UtcNow });
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Hai request bật cùng lúc: UNIQUE(MaNguoiDung, MaSanPham) chặn request sau — coi như đã yêu thích.
            foreach (var entry in _db.ChangeTracker.Entries<YeuThich>().Where(e => e.State == EntityState.Added).ToList())
                entry.State = EntityState.Detached;
            return (true, true);
        }
        return (true, true);
    }

    public async Task<PagedResult<ProductCardVm>> LayDanhSachAsync(int maNguoiDung, int page = 1, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        const int pageSize = 24;

        var query = _db.YeuThichs
            .AsNoTracking()
            .Where(f => f.MaNguoiDung == maNguoiDung
                        && !f.SanPham.DaXoa
                        && (f.SanPham.TrangThai == "ACTIVE" || f.SanPham.TrangThai == "RESERVED" || f.SanPham.TrangThai == "SOLD")
                        && f.SanPham.CuaHang.TrangThai == "ACTIVE"
                        && f.SanPham.CuaHang.NguoiDung.TrangThai == "ACTIVE");

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(f => f.NgayTao)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(f => new ProductCardVm
            {
                Id = f.SanPham.MaSanPham,
                Name = f.SanPham.Ten,
                Price = f.SanPham.Gia ?? 0m,
                ImageUrl = f.SanPham.Anh.Where(a => a.LaAnhChinh).Select(a => a.DuongDan).FirstOrDefault()
                             ?? f.SanPham.Anh.OrderBy(a => a.ThuTu).Select(a => a.DuongDan).FirstOrDefault(),
                ShopName = f.SanPham.CuaHang.TenShop,
                ConditionName = f.SanPham.TinhTrang != null ? f.SanPham.TinhTrang.Ten : null,
                CategoryName = f.SanPham.DanhMuc != null ? f.SanPham.DanhMuc.Ten : null
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<ProductCardVm> { Items = items, Page = page, PageSize = pageSize, TotalItems = total };
    }

    public async Task<HashSet<int>> LayIdsDangYeuThichAsync(int maNguoiDung, CancellationToken cancellationToken = default)
    {
        var ids = await _db.YeuThichs.AsNoTracking()
            .Where(f => f.MaNguoiDung == maNguoiDung)
            .Select(f => f.MaSanPham)
            .ToListAsync(cancellationToken);
        return ids.ToHashSet();
    }
}
