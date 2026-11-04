using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Models.ViewModels.Cart;
using SecondHandMarketplace.Web.Models.ViewModels.Orders;
using SecondHandMarketplace.Web.Services.Carts;

namespace SecondHandMarketplace.Web.Services.Orders;

public class OrderService : IOrderService
{
    private readonly AppDbContext _db;
    private readonly ICartService _cartService;

    public OrderService(AppDbContext db, ICartService cartService)
    {
        _db = db;
        _cartService = cartService;
    }

    public async Task<CheckoutViewModel> ChuanBiCheckoutTuGioAsync(int maNguoiDung)
    {
        var user = await _db.NguoiDungs.FindAsync(maNguoiDung);
        var cartVm = await _cartService.LayGioHangAsync(maNguoiDung);

        // Chỉ lấy các món được chọn và có thể mua
        cartVm.DanhSachNhomShop = cartVm.DanhSachNhomShop
            .Where(g => g.Items.Any(i => i.DaChon && i.CoTheMua))
            .Select(g => new CartShopGroupViewModel
            {
                MaCuaHang = g.MaCuaHang,
                TenShop = g.TenShop,
                Items = g.Items.Where(i => i.DaChon && i.CoTheMua).ToList()
            })
            .ToList();

        var tienHang = cartVm.TongTienTamTinh;

        return new CheckoutViewModel
        {
            TenNguoiNhan = user?.HoTen ?? string.Empty,
            SoDienThoaiNhan = user?.SoDienThoai ?? string.Empty,
            DiaChiNhan = user?.DiaChiMacDinh ?? string.Empty,
            GioHang = cartVm,
            TienHang = tienHang,
            PhiVanChuyen = 0,
            PhuongThucThanhToan = "COD"
        };
    }

    public async Task<CheckoutViewModel?> ChuanBiCheckoutMuaNgayAsync(int maNguoiDung, int maSanPham)
    {
        var user = await _db.NguoiDungs.FindAsync(maNguoiDung);
        var sp = await _db.SanPhams
            .Include(x => x.CuaHang)
            .Include(x => x.Anh)
            .FirstOrDefaultAsync(x => x.MaSanPham == maSanPham && !x.DaXoa);

        if (sp == null || sp.TrangThai != "ACTIVE" || sp.CuaHang.TrangThai != "ACTIVE")
        {
            return null;
        }

        if (sp.CuaHang.MaNguoiDung == maNguoiDung)
        {
            return null; // Không thể mua sản phẩm của shop mình
        }

        var itemVm = new CartItemViewModel
        {
            MaGioHang = 0,
            MaSanPham = sp.MaSanPham,
            TenSanPham = sp.Ten,
            Gia = sp.Gia ?? 0,
            DaChon = true,
            CoTheMua = true,
            TrangThaiSanPham = sp.TrangThai,
            MaCuaHang = sp.MaCuaHang,
            TenShop = sp.CuaHang.TenShop,
            AnhDaiDien = sp.Anh.Where(a => a.LaAnhChinh).Select(a => a.DuongDan).FirstOrDefault()
                         ?? sp.Anh.Select(a => a.DuongDan).FirstOrDefault()
        };

        return new CheckoutViewModel
        {
            TenNguoiNhan = user?.HoTen ?? string.Empty,
            SoDienThoaiNhan = user?.SoDienThoai ?? string.Empty,
            DiaChiNhan = user?.DiaChiMacDinh ?? string.Empty,
            MaSanPhamMuaNgay = maSanPham,
            SanPhamMuaNgay = itemVm,
            TienHang = itemVm.Gia,
            PhiVanChuyen = 0,
            PhuongThucThanhToan = "COD"
        };
    }

    public async Task<DatHangResultViewModel> DatHangTuGioAsync(int maNguoiDung, CheckoutViewModel model)
    {
        var danhSachMaDon = new List<string>();

        try
        {
            var conn = _db.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "sp_DatHangTuGio";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add(new SqlParameter("@MaNguoiDung", maNguoiDung));
            cmd.Parameters.Add(new SqlParameter("@TenNguoiNhan", model.TenNguoiNhan.Trim()));
            cmd.Parameters.Add(new SqlParameter("@SoDienThoaiNhan", model.SoDienThoaiNhan.Trim()));
            cmd.Parameters.Add(new SqlParameter("@DiaChiNhan", model.DiaChiNhan.Trim()));
            cmd.Parameters.Add(new SqlParameter("@PhiVanChuyenMoiDon", model.PhiVanChuyen));
            cmd.Parameters.Add(new SqlParameter("@PhuongThucThanhToan", model.PhuongThucThanhToan));

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var maDon = reader["MaDon"]?.ToString();
                if (!string.IsNullOrEmpty(maDon))
                {
                    danhSachMaDon.Add(maDon);
                }
            }

            return new DatHangResultViewModel
            {
                ThanhCong = true,
                ThongBao = "Đặt hàng thành công!",
                DanhSachMaDon = danhSachMaDon
            };
        }
        catch (Exception ex)
        {
            return new DatHangResultViewModel
            {
                ThanhCong = false,
                ThongBao = ex.Message
            };
        }
    }

    public async Task<DatHangResultViewModel> MuaNgayAsync(int maNguoiDung, int maSanPham, CheckoutViewModel model)
    {
        try
        {
            var conn = _db.Database.GetDbConnection();
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "sp_TaoDonHang";
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add(new SqlParameter("@MaNguoiDung", maNguoiDung));
            cmd.Parameters.Add(new SqlParameter("@MaSanPham", maSanPham));
            cmd.Parameters.Add(new SqlParameter("@TenNguoiNhan", model.TenNguoiNhan.Trim()));
            cmd.Parameters.Add(new SqlParameter("@SoDienThoaiNhan", model.SoDienThoaiNhan.Trim()));
            cmd.Parameters.Add(new SqlParameter("@DiaChiNhan", model.DiaChiNhan.Trim()));
            cmd.Parameters.Add(new SqlParameter("@PhiVanChuyen", model.PhiVanChuyen));
            cmd.Parameters.Add(new SqlParameter("@PhuongThucThanhToan", model.PhuongThucThanhToan));

            var outParam = new SqlParameter("@MaDonHangMoi", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };
            cmd.Parameters.Add(outParam);

            await cmd.ExecuteNonQueryAsync();

            var maDonHangMoi = (int)outParam.Value;
            var don = await _db.DonHangs.FindAsync(maDonHangMoi);

            return new DatHangResultViewModel
            {
                ThanhCong = true,
                ThongBao = "Đặt mua sản phẩm thành công!",
                DanhSachMaDon = don != null ? new List<string> { don.MaDon } : new()
            };
        }
        catch (Exception ex)
        {
            return new DatHangResultViewModel
            {
                ThanhCong = false,
                ThongBao = ex.Message
            };
        }
    }

    public async Task<List<DonHangItemViewModel>> LayDanhSachDonMuaAsync(int maNguoiDung, string? trangThai)
    {
        var query = _db.DonHangs
            .Include(d => d.CuaHang)
            .Include(d => d.ChiTiet)
            .Where(d => d.MaNguoiDung == maNguoiDung);

        if (!string.IsNullOrEmpty(trangThai))
        {
            query = query.Where(d => d.TrangThai == trangThai);
        }

        var list = await query.OrderByDescending(d => d.NgayTao).ToListAsync();

        return list.Select(d => new DonHangItemViewModel
        {
            MaDonHang = d.MaDonHang,
            MaDon = d.MaDon,
            MaCuaHang = d.MaCuaHang,
            TenShop = d.CuaHang.TenShop,
            TienHang = d.TienHang,
            PhiVanChuyen = d.PhiVanChuyen,
            TongTien = d.TongTien,
            TrangThai = d.TrangThai,
            TrangThaiThanhToan = d.TrangThaiThanhToan,
            PhuongThucThanhToan = d.PhuongThucThanhToan,
            NgayTao = d.NgayTao,
            NguoiHuy = d.NguoiHuy,
            LyDoHuy = d.LyDoHuy,
            ChiTiet = d.ChiTiet.Select(ct => new ChiTietDonHangItemViewModel
            {
                MaChiTietDonHang = ct.MaChiTietDonHang,
                MaSanPham = ct.MaSanPham,
                TenSanPham = ct.TenSanPham,
                AnhDaiDien = ct.AnhDaiDien,
                Gia = ct.Gia
            }).ToList()
        }).ToList();
    }

    public async Task<DonHangChiTietViewModel?> LayChiTietDonMuaAsync(int maNguoiDung, int maDonHang)
    {
        var d = await _db.DonHangs
            .Include(x => x.CuaHang)
            .Include(x => x.ChiTiet)
            .Include(x => x.LichSu)
            .FirstOrDefaultAsync(x => x.MaDonHang == maDonHang && x.MaNguoiDung == maNguoiDung);

        if (d == null) return null;

        return new DonHangChiTietViewModel
        {
            MaDonHang = d.MaDonHang,
            MaDon = d.MaDon,
            MaCuaHang = d.MaCuaHang,
            TenShop = d.CuaHang.TenShop,
            TenNguoiNhan = d.TenNguoiNhan,
            SoDienThoaiNhan = d.SoDienThoaiNhan,
            DiaChiNhan = d.DiaChiNhan,
            TienHang = d.TienHang,
            PhiVanChuyen = d.PhiVanChuyen,
            TongTien = d.TongTien,
            PhuongThucThanhToan = d.PhuongThucThanhToan,
            TrangThaiThanhToan = d.TrangThaiThanhToan,
            TrangThai = d.TrangThai,
            NgayTao = d.NgayTao,
            ThoiGianXacNhan = d.ThoiGianXacNhan,
            ThoiGianBatDauGiao = d.ThoiGianBatDauGiao,
            ThoiGianDaGiao = d.ThoiGianDaGiao,
            ThoiGianHoanTat = d.ThoiGianHoanTat,
            NguoiHuy = d.NguoiHuy,
            LyDoHuy = d.LyDoHuy,
            ThoiGianHuy = d.ThoiGianHuy,
            DanhSachSanPham = d.ChiTiet.Select(ct => new ChiTietDonHangItemViewModel
            {
                MaChiTietDonHang = ct.MaChiTietDonHang,
                MaSanPham = ct.MaSanPham,
                TenSanPham = ct.TenSanPham,
                AnhDaiDien = ct.AnhDaiDien,
                Gia = ct.Gia
            }).ToList(),
            LichSuTrangThai = d.LichSu.OrderBy(ls => ls.ThoiGian).Select(ls => new LichSuTrangThaiDonViewModel
            {
                TrangThaiCu = ls.TrangThaiCu,
                TrangThaiMoi = ls.TrangThaiMoi,
                LoaiNguoiThucHien = ls.LoaiNguoiThucHien,
                LyDo = ls.LyDo,
                ThoiGian = ls.ThoiGian
            }).ToList()
        };
    }

    public async Task<(bool ThanhCong, string? ThongBao)> HuyDonMuaAsync(int maNguoiDung, int maDonHang, string lyDo)
    {
        if (string.IsNullOrWhiteSpace(lyDo))
        {
            return (false, "Vui lòng nhập lý do hủy đơn.");
        }

        try
        {
            await _db.Database.ExecuteSqlInterpolatedAsync(
                $"EXEC sp_CapNhatTrangThaiDon @MaDonHang={maDonHang}, @TrangThaiMoi={"CANCELLED"}, @MaNguoiDung={maNguoiDung}, @LyDo={lyDo.Trim()}");

            return (true, "Hủy đơn hàng thành công.");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(bool ThanhCong, string? ThongBao)> XacNhanDaNhanHangAsync(int maNguoiDung, int maDonHang)
    {
        try
        {
            await _db.Database.ExecuteSqlInterpolatedAsync(
                $"EXEC sp_CapNhatTrangThaiDon @MaDonHang={maDonHang}, @TrangThaiMoi={"COMPLETED"}, @MaNguoiDung={maNguoiDung}, @LyDo={"Người mua đã nhận được hàng"}");

            return (true, "Đã xác nhận nhận hàng và hoàn tất đơn thành công.");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }
}
