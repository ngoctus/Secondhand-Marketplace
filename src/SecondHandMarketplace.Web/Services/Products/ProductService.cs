using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SecondHandMarketplace.Web.Common;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Models.Entities;
using SecondHandMarketplace.Web.Models.ViewModels;
using SecondHandMarketplace.Web.Models.ViewModels.Products;

namespace SecondHandMarketplace.Web.Services.Products;

public class ProductService : IProductService
{
    private const int SoAnhToiDa = 10;

    private readonly AppDbContext _db;

    public ProductService(AppDbContext db) => _db = db;

    public async Task<PagedResult<ProductCardVm>> SearchAsync(ProductSearchVm f)
    {
        // Giống view vw_SanPhamCongKhai: chỉ sản phẩm đã duyệt (ACTIVE), chưa xóa mềm,
        // cửa hàng và chủ cửa hàng đều đang hoạt động.
        var query = _db.SanPhams
            .AsNoTracking()
            .Include(p => p.CuaHang)
            .ThenInclude(ch => ch.NguoiDung)
            .Include(p => p.Anh)
            .Include(p => p.TinhTrang)
            .Where(p => p.TrangThai == "ACTIVE"
                        && !p.DaXoa
                        && p.CuaHang.TrangThai == "ACTIVE"
                        && p.CuaHang.NguoiDung.TrangThai == "ACTIVE");

        if (!string.IsNullOrWhiteSpace(f.Keyword))
        {
            var tuKhoa = f.Keyword.Trim();
            var tuKhoaChuanHoa = TextHelper.ChuanHoa(tuKhoa);
            query = query.Where(p => p.ChuoiTimKiem.Contains(tuKhoaChuanHoa)
                                     || p.CuaHang.TenShop.Contains(tuKhoa)
                                     || (p.DanhMuc != null && p.DanhMuc.Ten.Contains(tuKhoa)));
        }

        // Lọc 1 danh mục (đúng tên cột trong DB)
        if (f.CategoryId != null)
            query = query.Where(p => p.MaDanhMuc == f.CategoryId);

        // Lọc danh mục từ checkbox sidebar (chấp nhận cả slug "thoi-trang", ID số, hoặc tên tiếng Việt "Thời trang")
        if (f.Categories is { Length: > 0 })
        {
            var catInputs = f.Categories.Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c.Trim()).ToList();
            if (catInputs.Count > 0)
            {
                var mappedNames = new List<string>();
                var mappedIds = new List<int>();

                foreach (var input in catInputs)
                {
                    if (int.TryParse(input, out var cid))
                    {
                        mappedIds.Add(cid);
                    }
                    else
                    {
                        mappedNames.Add(input);
                        var slug = input.ToLowerInvariant();
                        if (slug == "thoi-trang") { mappedNames.Add("Thời trang"); mappedIds.Add(1); }
                        else if (slug == "dien-tu") { mappedNames.Add("Đồ điện tử"); mappedIds.Add(2); }
                        else if (slug == "do-gia-dung") { mappedNames.Add("Đồ gia dụng"); mappedIds.Add(3); }
                        else if (slug == "sach") { mappedNames.Add("Sách & văn phòng phẩm"); mappedIds.Add(4); }
                        else if (slug == "phu-kien" || slug == "xe-co") { mappedNames.Add("Xe cộ & phụ kiện"); mappedIds.Add(5); }
                        else if (slug == "do-choi") { mappedNames.Add("Đồ chơi & mẹ bé"); mappedIds.Add(6); }
                        else if (slug == "the-thao") { mappedNames.Add("Thể thao & dã ngoại"); mappedIds.Add(7); }
                        else if (slug == "noi-that") { mappedNames.Add("Nội thất"); mappedIds.Add(8); }
                        else if (slug == "khac") { mappedNames.Add("Khác"); mappedIds.Add(9); }
                    }
                }

                query = query.Where(p =>
                    (p.MaDanhMuc != null && mappedIds.Contains(p.MaDanhMuc.Value))
                    || (p.DanhMuc != null && mappedNames.Contains(p.DanhMuc.Ten)));
            }
        }

        // Lọc 1 tình trạng theo mã (LIKE_NEW, GOOD, ...)
        if (f.ConditionId != null)
            query = query.Where(p => p.MaTinhTrang == f.ConditionId);

        // Lọc tình trạng từ radio sidebar (new / like-new / good / fair / worn) -> tra MaTinhTrang theo Ma
        if (!string.IsNullOrWhiteSpace(f.Condition))
        {
            var condCode = f.Condition.Trim().ToUpperInvariant();
            query = query.Where(p => p.TinhTrang != null && p.TinhTrang.Ma == condCode);
        }

        if (f.MinPrice != null)
            query = query.Where(p => p.Gia >= f.MinPrice);
        if (f.MaxPrice != null)
            query = query.Where(p => p.Gia <= f.MaxPrice);

        // Sắp xếp
        query = f.Sort switch
        {
            "price_asc" => query.OrderBy(p => p.Gia).ThenByDescending(p => p.NgayTao),
            "price_desc" => query.OrderByDescending(p => p.Gia).ThenByDescending(p => p.NgayTao),
            _ => query.OrderByDescending(p => p.NgayTao).ThenByDescending(p => p.MaSanPham)
        };

        var page = Math.Max(1, f.Page);
        var pageSize = Math.Clamp(f.PageSize, 1, 100);

        var totalItems = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
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

        return new PagedResult<ProductCardVm>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems
        };
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

    // =====================================================================
    // B1 – Người bán: đăng / sửa / ẩn / gửi duyệt / ảnh
    // =====================================================================

    private async Task<CuaHang?> TimShopCuaToiAsync(int maNguoiDung, CancellationToken ct)
        => await _db.CuaHangs.AsNoTracking().FirstOrDefaultAsync(c => c.MaNguoiDung == maNguoiDung, ct);

    public async Task<PagedResult<MyProductVm>> GetMyAsync(int maNguoiDung, int page = 1, CancellationToken cancellationToken = default)
    {
        var shop = await TimShopCuaToiAsync(maNguoiDung, cancellationToken);
        if (shop is null) return new PagedResult<MyProductVm>();

        page = Math.Max(1, page);
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

        sp.MaDanhMuc = form.MaDanhMuc;
        sp.MaTinhTrang = form.MaTinhTrang;
        sp.Ten = form.Ten.Trim();
        sp.ChuoiTimKiem = LamChuoiTimKiem(form.Ten);
        sp.MoTa = string.IsNullOrWhiteSpace(form.MoTa) ? null : form.MoTa.Trim();
        sp.Gia = form.Gia;
        sp.NgayCapNhat = DateTime.UtcNow;

        var thuTu = sp.Anh.Count;
        if (thuTu < SoAnhToiDa)
        {
            foreach (var a in anhMoi.Take(SoAnhToiDa - thuTu))
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

    public async Task<(bool Success, string? Error)> GuiDuyetAsync(int maNguoiDung, int maSanPham, CancellationToken cancellationToken = default)
    {
        var (loi, _) = await KiemTraTruDangBanAsync(maNguoiDung, cancellationToken);
        if (loi is not null) return (false, loi);

        if (!_db.Database.IsRelational())
        {
            // Fallback In-Memory (chỉ phục vụ test): mô phỏng đúng kết quả của sp_GuiDuyetSanPham.
            var sp = await _db.SanPhams.FirstOrDefaultAsync(x => x.MaSanPham == maSanPham && !x.DaXoa, cancellationToken);
            if (sp is null) return (false, "Sản phẩm không tồn tại.");
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

    public async Task<(bool Success, string? Error)> XoaAnhAsync(int maNguoiDung, int maSanPham, int maAnh, CancellationToken cancellationToken = default)
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

        var wasMain = anh.LaAnhChinh;
        _db.AnhSanPhams.Remove(anh);
        await _db.SaveChangesAsync(cancellationToken);

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
            }
        }
        return (true, null);
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