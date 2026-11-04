using Microsoft.EntityFrameworkCore;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Models.Entities;
using SecondHandMarketplace.Web.Models.ViewModels.Reports;

namespace SecondHandMarketplace.Web.Services.Reports;

public class ReportService : IReportService
{
    private readonly AppDbContext _db;

    public ReportService(AppDbContext db) => _db = db;

    public async Task<(bool ThanhCong, string? ThongBao)> TaoBaoCaoAsync(int maNguoiBaoCao, TaoBaoCaoViewModel vm)
    {
        // 1. Chống gửi trùng khi đang có báo cáo PENDING
        var daTonTai = await _db.BaoCaos.AnyAsync(x =>
            x.MaNguoiBaoCao == maNguoiBaoCao &&
            x.LoaiDoiTuong == vm.LoaiDoiTuong &&
            x.MaDoiTuong == vm.MaDoiTuong &&
            x.TrangThai == "PENDING");

        if (daTonTai)
        {
            return (false, "Bạn đã gửi báo cáo cho đối tượng này và đang chờ ban quản trị xử lý.");
        }

        // 2. Kiểm tra đối tượng báo cáo tồn tại
        switch (vm.LoaiDoiTuong.ToUpper())
        {
            case "PRODUCT":
                var sp = await _db.SanPhams.FindAsync(vm.MaDoiTuong);
                if (sp == null || sp.DaXoa) return (false, "Sản phẩm không tồn tại.");
                break;
            case "USER":
                var nd = await _db.NguoiDungs.FindAsync(vm.MaDoiTuong);
                if (nd == null) return (false, "Người dùng không tồn tại.");
                if (nd.MaNguoiDung == maNguoiBaoCao) return (false, "Không thể tự báo cáo chính mình.");
                break;
            case "SHOP":
                var ch = await _db.CuaHangs.FindAsync(vm.MaDoiTuong);
                if (ch == null) return (false, "Cửa hàng không tồn tại.");
                if (ch.MaNguoiDung == maNguoiBaoCao) return (false, "Không thể tự báo cáo cửa hàng của mình.");
                break;
            default:
                return (false, "Loại đối tượng báo cáo không hợp lệ.");
        }

        // 3. Tạo bản ghi báo cáo
        var baoCao = new BaoCao
        {
            LoaiDoiTuong = vm.LoaiDoiTuong.ToUpper(),
            MaDoiTuong = vm.MaDoiTuong,
            MaNguoiBaoCao = maNguoiBaoCao,
            LyDo = vm.LyDo.Trim(),
            MoTa = vm.MoTa?.Trim(),
            TrangThai = "PENDING",
            NgayTao = DateTime.UtcNow
        };

        _db.BaoCaos.Add(baoCao);
        await _db.SaveChangesAsync();

        return (true, "Đã gửi báo cáo vi phạm thành công. Ban quản trị sẽ sớm xem xét.");
    }

    public async Task<List<BaoCaoItemViewModel>> LayLichSuBaoCaoCuaToiAsync(int maNguoiBaoCao)
    {
        var rawList = await _db.BaoCaos
            .Include(x => x.NguoiBaoCao)
            .Include(x => x.NguoiXuLy)
            .Where(x => x.MaNguoiBaoCao == maNguoiBaoCao)
            .OrderByDescending(x => x.NgayTao)
            .ToListAsync();

        return await NapTenDoiTuongVaMapVm(rawList);
    }

    public async Task<AdminReportListViewModel> LayDanhSachBaoCaoAdminAsync(string? trangThai, string? loaiDoiTuong)
    {
        var query = _db.BaoCaos
            .Include(x => x.NguoiBaoCao)
            .Include(x => x.NguoiXuLy)
            .AsQueryable();

        if (!string.IsNullOrEmpty(trangThai))
        {
            query = query.Where(x => x.TrangThai == trangThai);
        }

        if (!string.IsNullOrEmpty(loaiDoiTuong))
        {
            query = query.Where(x => x.LoaiDoiTuong == loaiDoiTuong);
        }

        var rawList = await query.OrderByDescending(x => x.NgayTao).ToListAsync();
        var items = await NapTenDoiTuongVaMapVm(rawList);
        var tongChuaXuLy = await _db.BaoCaos.CountAsync(x => x.TrangThai == "PENDING");

        return new AdminReportListViewModel
        {
            TrangThaiFilter = trangThai,
            LoaiFilter = loaiDoiTuong,
            Items = items,
            TongChuaXuLy = tongChuaXuLy
        };
    }

    public async Task<(bool ThanhCong, string? ThongBao)> XuLyBaoCaoAdminAsync(int maBaoCao, int adminId, string hanhDong, string? ghiChu)
    {
        var bc = await _db.BaoCaos.FindAsync(maBaoCao);
        if (bc == null) return (false, "Không tìm thấy báo cáo.");
        if (bc.TrangThai != "PENDING") return (false, "Báo cáo này đã được xử lý trước đó.");

        var now = DateTime.UtcNow;
        bc.NguoiXuLyId = adminId;
        bc.NgayXuLy = now;

        if (hanhDong == "DISMISS")
        {
            bc.TrangThai = "DISMISSED";
            bc.GhiChuXuLy = string.IsNullOrWhiteSpace(ghiChu) ? "Bỏ qua báo cáo (không vi phạm)." : ghiChu.Trim();
        }
        else if (hanhDong == "RESOLVE")
        {
            bc.TrangThai = "RESOLVED";
            bc.GhiChuXuLy = string.IsNullOrWhiteSpace(ghiChu) ? "Đã xử lý đối tượng vi phạm." : ghiChu.Trim();

            // Thực thi hành động tương ứng đối tượng
            if (bc.LoaiDoiTuong == "PRODUCT")
            {
                var sp = await _db.SanPhams.FindAsync(bc.MaDoiTuong);
                if (sp != null)
                {
                    sp.TrangThai = "REMOVED";
                    sp.LyDoGo = bc.GhiChuXuLy;
                    sp.NgayCapNhat = now;
                }
            }
            else if (bc.LoaiDoiTuong == "USER")
            {
                var nd = await _db.NguoiDungs.FindAsync(bc.MaDoiTuong);
                if (nd != null && nd.VaiTro != "ADMIN")
                {
                    nd.TrangThai = "LOCKED";
                    nd.LyDoKhoa = bc.GhiChuXuLy;
                    nd.ThoiGianKhoa = now;
                }
            }
            else if (bc.LoaiDoiTuong == "SHOP")
            {
                var ch = await _db.CuaHangs.FindAsync(bc.MaDoiTuong);
                if (ch != null)
                {
                    ch.TrangThai = "LOCKED";
                    ch.LyDoKhoa = bc.GhiChuXuLy;
                    ch.ThoiGianKhoa = now;
                }
            }
        }
        else
        {
            return (false, "Hành động xử lý không hợp lệ.");
        }

        await _db.SaveChangesAsync();
        return (true, "Đã cập nhật xử lý báo cáo thành công.");
    }

    private async Task<List<BaoCaoItemViewModel>> NapTenDoiTuongVaMapVm(List<BaoCao> list)
    {
        var productIds = list.Where(x => x.LoaiDoiTuong == "PRODUCT").Select(x => x.MaDoiTuong).Distinct().ToList();
        var userIds = list.Where(x => x.LoaiDoiTuong == "USER").Select(x => x.MaDoiTuong).Distinct().ToList();
        var shopIds = list.Where(x => x.LoaiDoiTuong == "SHOP").Select(x => x.MaDoiTuong).Distinct().ToList();

        var products = await _db.SanPhams.Where(p => productIds.Contains(p.MaSanPham)).ToDictionaryAsync(p => p.MaSanPham, p => p.Ten);
        var users = await _db.NguoiDungs.Where(u => userIds.Contains(u.MaNguoiDung)).ToDictionaryAsync(u => u.MaNguoiDung, u => u.HoTen);
        var shops = await _db.CuaHangs.Where(s => shopIds.Contains(s.MaCuaHang)).ToDictionaryAsync(s => s.MaCuaHang, s => s.TenShop);

        return list.Select(x =>
        {
            var tenDoiTuong = x.LoaiDoiTuong switch
            {
                "PRODUCT" => products.GetValueOrDefault(x.MaDoiTuong, $"Sản phẩm #{x.MaDoiTuong}"),
                "USER" => users.GetValueOrDefault(x.MaDoiTuong, $"Người dùng #{x.MaDoiTuong}"),
                "SHOP" => shops.GetValueOrDefault(x.MaDoiTuong, $"Cửa hàng #{x.MaDoiTuong}"),
                _ => $"#{x.MaDoiTuong}"
            };

            return new BaoCaoItemViewModel
            {
                MaBaoCao = x.MaBaoCao,
                LoaiDoiTuong = x.LoaiDoiTuong,
                MaDoiTuong = x.MaDoiTuong,
                TenDoiTuong = tenDoiTuong,
                MaNguoiBaoCao = x.MaNguoiBaoCao,
                TenNguoiBaoCao = x.NguoiBaoCao?.HoTen ?? "Người dùng",
                LyDo = x.LyDo,
                MoTa = x.MoTa,
                TrangThai = x.TrangThai,
                NguoiXuLyId = x.NguoiXuLyId,
                TenNguoiXuLy = x.NguoiXuLy?.HoTen,
                GhiChuXuLy = x.GhiChuXuLy,
                NgayTao = x.NgayTao,
                NgayXuLy = x.NgayXuLy
            };
        }).ToList();
    }
}
