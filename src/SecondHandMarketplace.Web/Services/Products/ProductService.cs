using Microsoft.EntityFrameworkCore;
using SecondHandMarketplace.Web.Common;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Models.ViewModels.Products;

namespace SecondHandMarketplace.Web.Services.Products;

public class ProductService : IProductService
{
    private readonly AppDbContext _db;

    public ProductService(AppDbContext db) => _db = db;

    public async Task<List<ProductCardVm>> SearchAsync(ProductSearchVm f)
    {
        // Giống view vw_SanPhamCongKhai: chỉ sản phẩm đã duyệt (ACTIVE), chưa xóa mềm,
        // cửa hàng và chủ cửa hàng đều đang hoạt động.
        var query = _db.SanPhams
            .AsNoTracking()
            .Where(p => p.TrangThai == "ACTIVE"
                        && !p.DaXoa
                        && p.CuaHang.TrangThai == "ACTIVE"
                        && p.CuaHang.NguoiDung.TrangThai == "ACTIVE");

        if (!string.IsNullOrWhiteSpace(f.Keyword))
        {
            var tuKhoa = f.Keyword.Trim();
            var tuKhoaChuanHoa = TextHelper.ChuanHoa(tuKhoa);
            query = query.Where(p => p.ChuoiTimKiem.Contains(tuKhoaChuanHoa)
                                     || p.CuaHang.TenShop.Contains(tuKhoa));
        }
        if (f.CategoryId != null)
            query = query.Where(p => p.MaDanhMuc == f.CategoryId);
        if (f.ConditionId != null)
            query = query.Where(p => p.MaTinhTrang == f.ConditionId);
        if (f.MinPrice != null)
            query = query.Where(p => p.Gia >= f.MinPrice);
        if (f.MaxPrice != null)
            query = query.Where(p => p.Gia <= f.MaxPrice);

        return await query
            .OrderByDescending(p => p.NgayTao)
            .ThenByDescending(p => p.MaSanPham)
            .Take(200)
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
            .ToListAsync();
    }

    public async Task<ProductDetailVm?> GetDetailAsync(int id)
    {
        // Giống view vw_SanPhamChiTietCongKhai: xem được ACTIVE, RESERVED, SOLD.
        return await _db.SanPhams
            .AsNoTracking()
            .Where(p => p.MaSanPham == id
                        && !p.DaXoa
                        && (p.TrangThai == "ACTIVE" || p.TrangThai == "RESERVED" || p.TrangThai == "SOLD")
                        && p.CuaHang.TrangThai == "ACTIVE"
                        && p.CuaHang.NguoiDung.TrangThai == "ACTIVE")
            .Select(p => new ProductDetailVm
            {
                Id = p.MaSanPham,
                Name = p.Ten,
                Description = p.MoTa,
                Price = p.Gia ?? 0m,
                Status = p.TrangThai,
                CanBuy = p.TrangThai == "ACTIVE",
                CategoryName = p.DanhMuc != null ? p.DanhMuc.Ten : null,
                ConditionName = p.TinhTrang != null ? p.TinhTrang.Ten : null,
                ShopName = p.CuaHang.TenShop,
                ShopDescription = p.CuaHang.MoTa,
                CreatedAt = p.NgayTao,
                Images = p.Anh.OrderBy(a => a.ThuTu).Select(a => a.DuongDan).ToList()
            })
            .FirstOrDefaultAsync();
    }
}