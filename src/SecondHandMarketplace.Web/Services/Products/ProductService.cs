using System.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SecondHandMarketplace.Web.Common;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Models.Entities;
using SecondHandMarketplace.Web.Models.ViewModels;
using SecondHandMarketplace.Web.Models.ViewModels.Products;
using SecondHandMarketplace.Web.Services.Reviews;

namespace SecondHandMarketplace.Web.Services.Products;

public class ProductService : IProductService
{
    private const int SoAnhToiDa = 10;

    private readonly AppDbContext _db;
    private readonly IReviewService _danhGia;
    private readonly IWebHostEnvironment? _environment;

    public ProductService(AppDbContext db, IReviewService danhGia, IWebHostEnvironment? environment = null)
    {
        _db = db;
        _danhGia = danhGia;
        _environment = environment;
    }

    public async Task<PagedResult<ProductCardVm>> SearchAsync(ProductSearchVm f)
    {
        f.GiaiPhanGia();
        var matchedCategoryIds = await TimDanhMucKhopTuKhoaAsync(f.Keyword);
        var query = TaoTruyVanTimKiem(f, matchedCategoryIds);
        var page = Math.Clamp(f.Page, 1, 10000);
        var pageSize = Math.Clamp(f.PageSize, 1, 100);
        var total = await query.CountAsync();

        var duLieuTho = await SapXepTruyVan(query, f.Sort)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProductCardSource(
                p.MaSanPham,
                p.Ten,
                p.Gia,
                p.MoTa,
                p.TrangThai,
                p.MaCuaHang,
                p.CuaHang.TenShop,
                p.CuaHang.Logo,
                p.CuaHang.KhuVucHienThi,
                p.Anh.Where(a => a.LaAnhChinh).Select(a => a.DuongDan).FirstOrDefault(),
                p.TinhTrang != null ? p.TinhTrang.Ten : null,
                p.DanhMuc != null ? p.DanhMuc.Ten : null,
                p.MaDanhMuc))
            .ToListAsync();

        var items = await ProductCardMapper.TaoDanhSachAsync(duLieuTho, _danhGia);
        if (_environment is not null)
        {
            foreach (var item in items)
                item.ImageUrl = ThumbnailIfPresent(item.ImageUrl);
        }

        return new PagedResult<ProductCardVm>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = total
        };
    }

    /// <summary>
    /// Hai đầu thanh trượt giá (B-04): biên thấp/cao nhất của tập tin đang hiển thị công khai.
    /// Cố ý không áp các bộ lọc khác để slider không đổi thang mỗi lần người mua đổi danh mục hay khu vực.
    /// </summary>
    public async Task<(decimal Min, decimal Max)> LayBienGiaAsync(CancellationToken cancellationToken = default)
    {
        var bien = await _db.SanPhams.AsNoTracking()
            .Where(p => p.TrangThai == "ACTIVE" && !p.DaXoa
                        && p.CuaHang.TrangThai == "ACTIVE"
                        && p.CuaHang.NguoiDung.TrangThai == "ACTIVE"
                        && p.Gia != null)
            .GroupBy(p => 1)
            .Select(g => new { Min = g.Min(p => p.Gia), Max = g.Max(p => p.Gia) })
            .FirstOrDefaultAsync(cancellationToken);

        return bien is null ? (0m, 0m) : (bien.Min ?? 0m, bien.Max ?? 0m);
    }

    /// <summary>
    /// Tìm danh mục khớp từ khóa theo chuẩn không dấu (ví dụ "thoi trang" hoặc "thời trang" đều khớp "Thời trang"),
    /// đồng thời bắt buộc khớp ranh giới từ đối với token ngắn (≤ 3 ký tự) để "ao" không khớp nhầm vào "Thể thao" (thao).
    /// </summary>
    private async Task<List<int>> TimDanhMucKhopTuKhoaAsync(string? keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword)) return [];

        var tuKhoa = keyword.Trim();
        if (tuKhoa.Length > 200) tuKhoa = tuKhoa[..200];
        var chuanHoa = TextHelper.ChuanHoa(tuKhoa);
        var tokens = chuanHoa.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0) return [];

        var categories = await _db.DanhMucs.AsNoTracking()
            .Select(d => new { d.MaDanhMuc, d.Ten })
            .ToListAsync();

        var matched = new List<int>();
        foreach (var c in categories)
        {
            var catNorm = TextHelper.ChuanHoa(c.Ten);
            var catPadded = " " + catNorm + " ";
            var allMatch = true;
            foreach (var token in tokens)
            {
                if (token.Length <= 3)
                {
                    if (!catPadded.Contains(" " + token + " ", StringComparison.Ordinal))
                    {
                        allMatch = false;
                        break;
                    }
                }
                else
                {
                    if (!catNorm.Contains(token, StringComparison.Ordinal))
                    {
                        allMatch = false;
                        break;
                    }
                }
            }
            if (allMatch) matched.Add(c.MaDanhMuc);
        }
        return matched;
    }

    // Giống view vw_SanPhamCongKhai: chỉ sản phẩm đã duyệt (ACTIVE), chưa xóa mềm,
    // cửa hàng và chủ cửa hàng đều đang hoạt động.
    private IQueryable<SanPham> TaoTruyVanTimKiem(ProductSearchVm f, IReadOnlyCollection<int> matchedCategoryIds)
    {
        var query = _db.SanPhams
            .AsNoTracking()
            .Include(p => p.CuaHang)
            .ThenInclude(ch => ch.NguoiDung)
            .Include(p => p.Anh)
            .Include(p => p.TinhTrang)
            .Include(p => p.DanhMuc)
            .Where(p => p.TrangThai == "ACTIVE"
                        && !p.DaXoa
                        && p.CuaHang.TrangThai == "ACTIVE"
                        && p.CuaHang.NguoiDung.TrangThai == "ACTIVE");

        if (!string.IsNullOrWhiteSpace(f.Keyword))
        {
            var tuKhoa = f.Keyword.Trim();
            if (tuKhoa.Length > 200)
            {
                tuKhoa = tuKhoa[..200];
            }
            var tuKhoaChuanHoa = TextHelper.ChuanHoa(tuKhoa);
            var tokens = tuKhoaChuanHoa.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var hasMatchedCategories = matchedCategoryIds.Count > 0;

            if (tokens.Length <= 1)
            {
                var token = tokens.Length == 1 ? tokens[0] : tuKhoaChuanHoa;
                if (token.Length <= 3)
                {
                    // Đối với từ ngắn (<= 3 ký tự: "ao", "xe", "mu", "tu", "ban"...):
                    // Bắt buộc khớp ranh giới từ (whole-word boundary) trong ChuoiTimKiem hoặc Ten:
                    // để "áo" (ao) KHÔNG bị khớp nhầm vào "mũ bảo hiểm" (bao), "lưng cao" (cao), "thể thao" (thao), "giáo trình" (giao)...
                    var boundaryToken = " " + token + " ";
                    var boundaryTuKhoa = " " + tuKhoa + " ";
                    query = query.Where(p => (" " + p.ChuoiTimKiem + " ").Contains(boundaryToken)
                                             || (" " + p.Ten + " ").Contains(boundaryTuKhoa)
                                             || (hasMatchedCategories && p.MaDanhMuc != null && matchedCategoryIds.Contains(p.MaDanhMuc.Value)));
                }
                else
                {
                    query = query.Where(p => p.ChuoiTimKiem.Contains(token)
                                             || p.Ten.Contains(tuKhoa)
                                             || (p.MoTa != null && p.MoTa.Contains(tuKhoa))
                                             || p.CuaHang.TenShop.Contains(tuKhoa)
                                             || (hasMatchedCategories && p.MaDanhMuc != null && matchedCategoryIds.Contains(p.MaDanhMuc.Value)));
                }
            }
            else
            {
                // Tìm kiếm đa từ: mỗi từ khóa phải có mặt trong sản phẩm (hoặc sản phẩm thuộc danh mục khớp đủ từ khóa)
                foreach (var t in tokens)
                {
                    var token = t;
                    if (token.Length <= 3)
                    {
                        var boundaryToken = " " + token + " ";
                        query = query.Where(p => (" " + p.ChuoiTimKiem + " ").Contains(boundaryToken)
                                                 || (" " + p.Ten + " ").Contains(boundaryToken)
                                                 || (hasMatchedCategories && p.MaDanhMuc != null && matchedCategoryIds.Contains(p.MaDanhMuc.Value)));
                    }
                    else
                    {
                        query = query.Where(p => p.ChuoiTimKiem.Contains(token)
                                                 || p.Ten.Contains(token)
                                                 || (p.MoTa != null && p.MoTa.Contains(token))
                                                 || p.CuaHang.TenShop.Contains(token)
                                                 || (hasMatchedCategories && p.MaDanhMuc != null && matchedCategoryIds.Contains(p.MaDanhMuc.Value)));
                    }
                }
            }
        }

        if (f.CategoryId != null)
            query = query.Where(p => p.MaDanhMuc == f.CategoryId);
        if (f.ConditionId != null)
            query = query.Where(p => p.MaTinhTrang == f.ConditionId);
        if (f.MinPrice != null)
            query = query.Where(p => p.Gia >= f.MinPrice);
        if (f.MaxPrice != null)
            query = query.Where(p => p.Gia <= f.MaxPrice);

        // Khu vực sinh sống (mục A4): lọc theo mã tỉnh/xã của cửa hàng, không tính khoảng cách.
        if (f.MaTinhThanh != null)
            query = query.Where(p => p.CuaHang.MaTinhThanh == f.MaTinhThanh);
        if (f.MaPhuongXa != null)
            query = query.Where(p => p.CuaHang.MaPhuongXa == f.MaPhuongXa);

        return query;
    }

    private static IQueryable<SanPham> SapXepTruyVan(IQueryable<SanPham> query, string? sort) => sort switch
    {
        "price_asc" => query.OrderBy(p => p.Gia).ThenByDescending(p => p.MaSanPham),
        "price_desc" => query.OrderByDescending(p => p.Gia).ThenByDescending(p => p.MaSanPham),
        _ => query.OrderByDescending(p => p.NgayTao).ThenByDescending(p => p.MaSanPham)
    };

    // Thẻ sản phẩm dùng bản thumbnail 640px nếu controller upload đã tạo ra, nếu không thì dùng ảnh gốc.
    private string? ThumbnailIfPresent(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl) || _environment?.WebRootPath is null) return imageUrl;
        var cleanUrl = imageUrl.Split('?', '#')[0];
        if (!cleanUrl.StartsWith("/uploads/products/", StringComparison.OrdinalIgnoreCase)) return imageUrl;
        var relative = cleanUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var directory = Path.GetDirectoryName(relative) ?? string.Empty;
        var thumbName = Path.GetFileNameWithoutExtension(relative) + "-thumb.webp";
        var thumbPath = Path.Combine(_environment.WebRootPath, directory, thumbName);
        return System.IO.File.Exists(thumbPath)
            ? "/" + Path.Combine(directory, thumbName).Replace(Path.DirectorySeparatorChar, '/')
            : imageUrl;
    }

    // Xóa file ảnh + thumbnail trên đĩa sau khi bản ghi đã bị xóa (best effort).
    // Chỉ đụng tới thư mục upload của người dùng, ảnh seed nằm ngoài nên không bị xóa nhầm.
    private void XoaFileAnh(string? duongDan)
    {
        if (_environment?.WebRootPath is null || string.IsNullOrWhiteSpace(duongDan)) return;

        var goc = Path.GetFullPath(Path.Combine(_environment.WebRootPath, "uploads", "products"));
        var tuongDoi = duongDan.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var duongDanDia = Path.GetFullPath(Path.Combine(_environment.WebRootPath, tuongDoi));
        if (!duongDanDia.StartsWith(goc + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) return;

        var thumbPath = Path.Combine(Path.GetDirectoryName(duongDanDia)!,
            Path.GetFileNameWithoutExtension(duongDanDia) + "-thumb.webp");
        try
        {
            if (System.IO.File.Exists(duongDanDia)) System.IO.File.Delete(duongDanDia);
            if (System.IO.File.Exists(thumbPath)) System.IO.File.Delete(thumbPath);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    public async Task<ProductDetailVm?> GetDetailAsync(int id)
    {
        // Giống view vw_SanPhamChiTietCongKhai: xem được ACTIVE, RESERVED, SOLD.
        var duLieuTho = await _db.SanPhams
            .AsNoTracking()
            .Where(p => p.MaSanPham == id
                        && !p.DaXoa
                        && (p.TrangThai == "ACTIVE" || p.TrangThai == "RESERVED" || p.TrangThai == "SOLD")
                        && p.CuaHang.TrangThai == "ACTIVE"
                        && p.CuaHang.NguoiDung.TrangThai == "ACTIVE")
            .Select(p => new
            {
                p.MaSanPham,
                p.MaCuaHang,
                SellerUserId = p.CuaHang.MaNguoiDung,
                p.MaDanhMuc,
                p.Ten,
                p.MoTa,
                p.Gia,
                p.TrangThai,
                p.NgayTao,
                CategoryName = p.DanhMuc != null ? p.DanhMuc.Ten : null,
                ConditionName = p.TinhTrang != null ? p.TinhTrang.Ten : null,
                ShopName = p.CuaHang.TenShop,
                ShopDescription = p.CuaHang.MoTa,
                ShopLogo = p.CuaHang.Logo,
                ShopKhuVuc = p.CuaHang.KhuVucHienThi,
                ImageUrls = p.Anh.OrderByDescending(a => a.LaAnhChinh).ThenBy(a => a.ThuTu).Select(a => a.DuongDan).ToList()
            })
            .FirstOrDefaultAsync();

        if (duLieuTho is null) return null;

        // Điểm sao chỉ lấy từ vw_DiemCuaHang qua IReviewService (C). View trả 0 khi chưa có đánh giá
        // nên phải kiểm tra SoDanhGia > 0 mới coi là có điểm.
        var diem = await _danhGia.LayDiemCuaHangAsync(duLieuTho.MaCuaHang);

        return new ProductDetailVm
        {
            Id = duLieuTho.MaSanPham,
            Name = duLieuTho.Ten,
            Description = duLieuTho.MoTa,
            Price = duLieuTho.Gia ?? 0m,
            Status = duLieuTho.TrangThai,
            CanBuy = duLieuTho.TrangThai == "ACTIVE",
            CategoryId = duLieuTho.MaDanhMuc,
            CategoryName = duLieuTho.CategoryName,
            ConditionName = duLieuTho.ConditionName,
            CreatedAt = duLieuTho.NgayTao,
            Images = duLieuTho.ImageUrls,
            ShopId = duLieuTho.MaCuaHang,
            SellerUserId = duLieuTho.SellerUserId,
            ShopName = duLieuTho.ShopName,
            ShopDescription = duLieuTho.ShopDescription,
            ShopLogoUrl = duLieuTho.ShopLogo,
            DiemTrungBinh = diem is not null && diem.SoDanhGia > 0 ? diem.DiemTrungBinh : null,
            SoDanhGia = diem?.SoDanhGia ?? 0,
            SoDonThanhCong = diem?.SoDonThanhCong ?? 0,
            KhuVuc = duLieuTho.ShopKhuVuc
        };
    }

    // =====================================================================
    // B1 – Người bán: đăng / sửa / ẩn / gửi duyệt / ảnh
    // =====================================================================

    private async Task<CuaHang?> TimShopCuaToiAsync(int maNguoiDung, CancellationToken ct)
        => await _db.CuaHangs.AsNoTracking().FirstOrDefaultAsync(c => c.MaNguoiDung == maNguoiDung, ct);

    public async Task<PagedResult<MyProductVm>> GetMyAsync(int maNguoiDung, int page = 1, CancellationToken cancellationToken = default)
    {
        var shop = await TimShopCuaToiAsync(maNguoiDung, cancellationToken);
        if (shop is null) return new PagedResult<MyProductVm>();

        page = Math.Clamp(page, 1, 10000);
        const int pageSize = 12;

        var query = _db.SanPhams
            .AsNoTracking()
            .Where(p => p.MaCuaHang == shop.MaCuaHang && !p.DaXoa);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(p => p.NgayCapNhat).ThenByDescending(p => p.MaSanPham)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(p => new MyProductVm
            {
                Id = p.MaSanPham,
                Ten = p.Ten,
                Gia = p.Gia,
                TrangThai = p.TrangThai,
                NgayTao = p.NgayTao,
                NgayCapNhat = p.NgayCapNhat,
                AnhDaiDien = p.Anh.Where(a => a.LaAnhChinh).Select(a => a.DuongDan).FirstOrDefault()
                                 ?? p.Anh.OrderBy(a => a.ThuTu).Select(a => a.DuongDan).FirstOrDefault(),
                SoLuongAnh = p.Anh.Count,
                LyDoTuChoiGanNhat = _db.LuotKiemDuyets
                    .Where(l => l.MaSanPham == p.MaSanPham && l.QuyetDinh == "REJECTED")
                    .OrderByDescending(l => l.ThoiGianQuyetDinh)
                    .Select(l => l.LyDo).FirstOrDefault(),
                PhanTichThatBai = _db.LuotKiemDuyets.Any(l => l.MaSanPham == p.MaSanPham
                    && l.QuyetDinh == null && l.PhanTich != null && l.PhanTich.TrangThai == "FAILED")
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<MyProductVm> { Items = items, Page = page, PageSize = pageSize, TotalItems = total };
    }

    public async Task<ProductFormVm?> GetFormAsync(int maNguoiDung, int maSanPham, CancellationToken cancellationToken = default)
    {
        var shop = await TimShopCuaToiAsync(maNguoiDung, cancellationToken);
        if (shop is null) return null;

        return await _db.SanPhams
            .AsNoTracking()
            .Where(p => p.MaSanPham == maSanPham && p.MaCuaHang == shop.MaCuaHang && !p.DaXoa)
            .Select(p => new ProductFormVm
            {
                Id = p.MaSanPham,
                Ten = p.Ten,
                MoTa = p.MoTa,
                Gia = p.Gia,
                MaDanhMuc = p.MaDanhMuc,
                MaTinhTrang = p.MaTinhTrang,
                TrangThai = p.TrangThai,
                AnhHienTai = p.Anh.OrderBy(a => a.ThuTu)
                    .Select(a => new ProductImageVm { MaAnh = a.MaAnhSP, DuongDan = a.DuongDan, LaAnhChinh = a.LaAnhChinh })
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<(bool Success, string? Error, int Id)> CreateAsync(
        int maNguoiDung, ProductFormVm form, IReadOnlyList<ProductImageInput> anh,
        CancellationToken cancellationToken = default)
    {
        var (loi, shop) = await KiemTraTruDangBanAsync(maNguoiDung, cancellationToken);
        if (loi is not null) return (false, loi, 0);

        var (hopLe, thongBao) = await KiemTraDanhMucTinhTrangAsync(form, cancellationToken);
        if (!hopLe) return (false, thongBao, 0);

        var sp = new SanPham
        {
            MaCuaHang = shop!.MaCuaHang,
            MaDanhMuc = form.MaDanhMuc,
            MaTinhTrang = form.MaTinhTrang,
            Ten = form.Ten.Trim(),
            ChuoiTimKiem = LamChuoiTimKiem(form.Ten),
            MoTa = string.IsNullOrWhiteSpace(form.MoTa) ? null : form.MoTa.Trim(),
            Gia = form.Gia,
            TrangThai = "DRAFT",
            NgayTao = DateTime.UtcNow,
            NgayCapNhat = DateTime.UtcNow
        };

        var soAnh = Math.Min(anh.Count, SoAnhToiDa);
        for (var i = 0; i < soAnh; i++)
        {
            sp.Anh.Add(new AnhSanPham
            {
                DuongDan = anh[i].DuongDan,
                ThuTu = i + 1,
                LaAnhChinh = i == 0,
                DungLuong = anh[i].DungLuong,
                NgayTao = DateTime.UtcNow
            });
        }

        _db.SanPhams.Add(sp);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            return (false, SqlLoi.ThongBao(ex), 0);
        }
        return (true, null, sp.MaSanPham);
    }

    public async Task<(bool Success, string? Error)> UpdateAsync(
        int maNguoiDung, int maSanPham, ProductFormVm form, IReadOnlyList<ProductImageInput> anhMoi,
        CancellationToken cancellationToken = default)
    {
        var (loi, shop) = await KiemTraTruDangBanAsync(maNguoiDung, cancellationToken);
        if (loi is not null) return (false, loi);

        var sp = await _db.SanPhams
            .Include(x => x.Anh)
            .FirstOrDefaultAsync(x => x.MaSanPham == maSanPham && x.MaCuaHang == shop!.MaCuaHang && !x.DaXoa, cancellationToken);
        if (sp is null) return (false, "Không tìm thấy sản phẩm hoặc sản phẩm không thuộc cửa hàng của bạn.");
        if (sp.TrangThai == "PENDING_REVIEW") return (false, "Sản phẩm đang chờ duyệt, không thể chỉnh sửa.");
        if (sp.TrangThai is "RESERVED" or "SOLD" or "REMOVED") return (false, "Sản phẩm đã có đơn đặt/đã bán hoặc đã bị gỡ, không thể chỉnh sửa.");

        var (hopLe, thongBao) = await KiemTraDanhMucTinhTrangAsync(form, cancellationToken);
        if (!hopLe) return (false, thongBao);

        var wasActive = sp.TrangThai == "ACTIVE";
        sp.MaDanhMuc = form.MaDanhMuc;
        sp.MaTinhTrang = form.MaTinhTrang;
        sp.Ten = form.Ten.Trim();
        sp.ChuoiTimKiem = LamChuoiTimKiem(form.Ten);
        sp.MoTa = string.IsNullOrWhiteSpace(form.MoTa) ? null : form.MoTa.Trim();
        sp.Gia = form.Gia;
        sp.NgayCapNhat = DateTime.UtcNow;
        if (wasActive)
        {
            sp.TrangThai = "DRAFT";
        }

        // Đếm theo số ảnh để giới hạn 10, nhưng thứ tự mới phải nối sau ThuTu lớn nhất:
        // xóa một ảnh ở giữa để lại khoảng trống (2,3,4) nên Count+1 sẽ trùng unique key.
        var soAnhHienCo = sp.Anh.Count;
        var thuTu = soAnhHienCo == 0 ? 0 : sp.Anh.Max(a => a.ThuTu);
        if (soAnhHienCo < SoAnhToiDa)
        {
            foreach (var a in anhMoi.Take(SoAnhToiDa - soAnhHienCo))
            {
                thuTu++;
                sp.Anh.Add(new AnhSanPham
                {
                    DuongDan = a.DuongDan,
                    ThuTu = thuTu,
                    LaAnhChinh = false,
                    DungLuong = a.DungLuong,
                    NgayTao = DateTime.UtcNow
                });
            }
            // Sản phẩm chưa có ảnh chính (đã xóa hết) thì thăng ảnh cũ nhất còn lại lên làm chính.
            if (!sp.Anh.Any(x => x.LaAnhChinh))
            {
                var dau = sp.Anh.OrderBy(x => x.ThuTu).FirstOrDefault();
                if (dau is not null) dau.LaAnhChinh = true;
            }
        }

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            return (false, SqlLoi.ThongBao(ex));
        }
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> HideAsync(int maNguoiDung, int maSanPham, CancellationToken cancellationToken = default)
    {
        var (loi, shop) = await KiemTraTruDangBanAsync(maNguoiDung, cancellationToken);
        if (loi is not null) return (false, loi);

        var cuaHangId = shop!.MaCuaHang;
        var thuocSo = await _db.SanPhams.AsNoTracking()
            .AnyAsync(x => x.MaSanPham == maSanPham && x.MaCuaHang == cuaHangId && !x.DaXoa, cancellationToken);
        if (!thuocSo) return (false, "Không tìm thấy sản phẩm hoặc sản phẩm không thuộc cửa hàng của bạn.");

        try
        {
            await _db.Database.ExecuteSqlInterpolatedAsync(
                $@"EXEC dbo.sp_ChuyenTrangThaiSanPham @MaSanPham={maSanPham}, @TrangThaiMoi='HIDDEN', @LoaiNguoiThucHien='SELLER', @NguoiThucHienId={maNguoiDung}",
                cancellationToken);
        }
        catch (Exception ex)
        {
            // sp kiểm tra tính hợp lệ của bước chuyển (50011): chỉ ACTIVE mới ẩn được.
            return (false, SqlLoi.ThongBao(ex));
        }
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(int maNguoiDung, int maSanPham, CancellationToken cancellationToken = default)
    {
        var (loi, shop) = await KiemTraTruDangBanAsync(maNguoiDung, cancellationToken);
        if (loi is not null) return (false, loi);

        var cuaHangId = shop!.MaCuaHang;
        var thuocSo = await _db.SanPhams.AsNoTracking()
            .AnyAsync(x => x.MaSanPham == maSanPham && x.MaCuaHang == cuaHangId && !x.DaXoa, cancellationToken);
        if (!thuocSo) return (false, "Không tìm thấy sản phẩm hoặc sản phẩm không thuộc cửa hàng của bạn.");

        if (!_db.Database.IsRelational())
        {
            var sp = await _db.SanPhams.FirstOrDefaultAsync(x => x.MaSanPham == maSanPham && !x.DaXoa, cancellationToken);
            if (sp is null) return (false, "Sản phẩm không tồn tại.");
            if (sp.TrangThai is "SOLD" or "RESERVED")
                return (false, "Sản phẩm đã có đơn hàng hoặc đã bán, không thể xóa.");

            sp.DaXoa = true;
            sp.ThoiGianXoa = DateTime.UtcNow;
            sp.NgayCapNhat = DateTime.UtcNow;

            var cartItems = await _db.GioHangs.Where(g => g.MaSanPham == maSanPham).ToListAsync(cancellationToken);
            _db.GioHangs.RemoveRange(cartItems);

            await _db.SaveChangesAsync(cancellationToken);
            return (true, null);
        }

        try
        {
            await _db.Database.ExecuteSqlInterpolatedAsync(
                $@"EXEC dbo.sp_XoaMemSanPham @MaSanPham={maSanPham}, @MaNguoiDung={maNguoiDung}",
                cancellationToken);
        }
        catch (Exception ex)
        {
            return (false, SqlLoi.ThongBao(ex));
        }
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> GuiDuyetAsync(int maNguoiDung, int maSanPham, CancellationToken cancellationToken = default)
    {
        var (loi, _) = await KiemTraTruDangBanAsync(maNguoiDung, cancellationToken);
        if (loi is not null) return (false, loi);

        if (!_db.Database.IsRelational())
        {
            // Fallback In-Memory (chỉ phục vụ test): mô phỏng đúng kết quả của sp_GuiDuyetSanPham.
            var sp = await _db.SanPhams
                .Include(x => x.CuaHang)
                .FirstOrDefaultAsync(x => x.MaSanPham == maSanPham && !x.DaXoa, cancellationToken);
            if (sp is null) return (false, "Sản phẩm không tồn tại.");
            if (sp.CuaHang?.MaNguoiDung != maNguoiDung) return (false, "Bạn không có quyền thao tác trên sản phẩm này.");
            sp.TrangThai = "PENDING_REVIEW";
            sp.NgayCapNhat = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            return (true, null);
        }

        try
        {
            var conn = _db.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync(cancellationToken);

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "sp_GuiDuyetSanPham";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add(new SqlParameter("@MaSanPham", maSanPham));
            cmd.Parameters.Add(new SqlParameter("@MaNguoiDung", maNguoiDung));
            cmd.Parameters.Add(new SqlParameter("@TaoPhanTichAI", true));

            var loiRule = new List<string>();
            var thanhCong = false;
            await using (var reader = await cmd.ExecuteReaderAsync(cancellationToken))
            {
                do
                {
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        thanhCong = reader.GetBoolean(0);
                        if (!thanhCong && !reader.IsDBNull(3))
                            loiRule.Add(reader.GetString(3));
                    }
                } while (await reader.NextResultAsync(cancellationToken));
            }

            if (!thanhCong)
                return (false, loiRule.Count > 0 ? string.Join(" ", loiRule.Distinct()) : "Sản phẩm chưa đủ điều kiện gửi duyệt.");
            return (true, null);
        }
        catch (SqlException ex)
        {
            return (false, SqlLoi.ThongBao(ex));
        }
    }

    public async Task<(bool Success, string? Error)> DatAnhChinhAsync(int maNguoiDung, int maSanPham, int maAnh, CancellationToken cancellationToken = default)
    {
        var (loi, shop) = await KiemTraTruDangBanAsync(maNguoiDung, cancellationToken);
        if (loi is not null) return (false, loi);

        var anh = await _db.AnhSanPhams
            .Include(a => a.SanPham)
            .FirstOrDefaultAsync(a => a.MaAnhSP == maAnh && a.MaSanPham == maSanPham
                                      && a.SanPham.MaCuaHang == shop!.MaCuaHang && !a.SanPham.DaXoa, cancellationToken);
        if (anh is null) return (false, "Không tìm thấy ảnh.");
        var loiTrangThai = LoiTrangThaiKhongChoSuaAnh(anh.SanPham.TrangThai);
        if (loiTrangThai is not null) return (false, loiTrangThai);
        if (anh.LaAnhChinh) return (true, null);

        // Index UNIQUE filtered (MaSanPham WHERE LaAnhChinh=1): phải bỏ ảnh chính cũ TRƯỚC khi bật ảnh mới.
        await _db.AnhSanPhams
            .Where(a => a.MaSanPham == maSanPham && a.LaAnhChinh)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.LaAnhChinh, false), cancellationToken);
        anh.LaAnhChinh = true;
        await _db.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<(bool Success, string? Error, int? MaAnhChinhMoi)> XoaAnhAsync(int maNguoiDung, int maSanPham, int maAnh, CancellationToken cancellationToken = default)
    {
        var (loi, shop) = await KiemTraTruDangBanAsync(maNguoiDung, cancellationToken);
        if (loi is not null) return (false, loi, null);

        var anh = await _db.AnhSanPhams
            .Include(a => a.SanPham)
            .FirstOrDefaultAsync(a => a.MaAnhSP == maAnh && a.MaSanPham == maSanPham
                                      && a.SanPham.MaCuaHang == shop!.MaCuaHang && !a.SanPham.DaXoa, cancellationToken);
        if (anh is null) return (false, "Không tìm thấy ảnh.", null);
        var loiTrangThai = LoiTrangThaiKhongChoSuaAnh(anh.SanPham.TrangThai);
        if (loiTrangThai is not null) return (false, loiTrangThai, null);

        var wasMain = anh.LaAnhChinh;
        var duongDanAnh = anh.DuongDan;
        _db.AnhSanPhams.Remove(anh);
        await _db.SaveChangesAsync(cancellationToken);
        XoaFileAnh(duongDanAnh);

        int? maAnhChinhMoi = null;
        if (wasMain)
        {
            var keo = await _db.AnhSanPhams
                .Where(a => a.MaSanPham == maSanPham)
                .OrderBy(a => a.ThuTu)
                .FirstOrDefaultAsync(cancellationToken);
            if (keo is not null)
            {
                keo.LaAnhChinh = true;
                await _db.SaveChangesAsync(cancellationToken);
                maAnhChinhMoi = keo.MaAnhSP;
            }
        }
        return (true, null, maAnhChinhMoi);
    }

    private static string? LoiTrangThaiKhongChoSuaAnh(string trangThai) => trangThai switch
    {
        "PENDING_REVIEW" => "Sản phẩm đang chờ duyệt, không thể chỉnh sửa ảnh.",
        "RESERVED" or "SOLD" => "Sản phẩm đã có đơn đặt/đã bán, không thể chỉnh sửa ảnh.",
        "REMOVED" => "Sản phẩm đã bị gỡ, không thể chỉnh sửa ảnh.",
        "ACTIVE" => "Sản phẩm đang bán, hãy ẩn sản phẩm trước khi chỉnh sửa ảnh.",
        _ => null
    };

    private async Task<(string? Error, CuaHang? Shop)> KiemTraTruDangBanAsync(int maNguoiDung, CancellationToken ct)
    {
        var shop = await TimShopCuaToiAsync(maNguoiDung, ct);
        if (shop is null)
            return ("Bạn chưa có cửa hàng. Hãy tạo cửa hàng trước khi đăng bán.", null);
        if (shop.TrangThai != "ACTIVE")
            return ("Cửa hàng đang bị khóa, không thể thao tác với sản phẩm.", null);
        return (null, shop);
    }

    private async Task<(bool HopLe, string? ThongBao)> KiemTraDanhMucTinhTrangAsync(ProductFormVm form, CancellationToken ct)
    {
        if (form.MaDanhMuc is null || !await _db.DanhMucs.AnyAsync(d => d.MaDanhMuc == form.MaDanhMuc && d.DangHienThi, ct))
            return (false, "Danh mục không hợp lệ hoặc đang bị ẩn.");
        if (form.MaTinhTrang is null || !await _db.TinhTrangSanPhams.AnyAsync(t => t.MaTinhTrang == form.MaTinhTrang && t.DangSuDung, ct))
            return (false, "Tình trạng sản phẩm không hợp lệ.");
        return (true, null);
    }

    // Cột ChuoiTimKiem là nvarchar(200); seed ghi fn_ChuanHoaChuoi(Ten) nên C# làm tương tự.
    private static string LamChuoiTimKiem(string ten)
    {
        var chuoi = TextHelper.ChuanHoa(ten);
        return chuoi.Length <= 200 ? chuoi : chuoi[..200];
    }
}
