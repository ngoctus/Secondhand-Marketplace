using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Models.Entities;
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
            // Kiến trúc: Stored procedure sp_DatHangTuGio trong database/06_procedures.sql đã tự động
            // tạo thông báo (NotificationTypes.Order) cho người bán và người mua trong cùng transaction,
            // đảm bảo tính toàn vẹn và nguyên tử (ACID).
            if (_db.Database.IsRelational())
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
            }
            else
            {
                // Fallback phục vụ test môi trường In-Memory
                var don = new DonHang
                {
                    MaDon = "DH" + DateTime.UtcNow.Ticks.ToString()[^8..],
                    MaNguoiDung = maNguoiDung,
                    MaCuaHang = 1,
                    TienHang = model.TienHang,
                    PhiVanChuyen = model.PhiVanChuyen,
                    TongTien = model.TongThanhToan,
                    TrangThai = "PENDING",
                    TrangThaiThanhToan = "UNPAID",
                    PhuongThucThanhToan = model.PhuongThucThanhToan,
                    TenNguoiNhan = model.TenNguoiNhan.Trim(),
                    SoDienThoaiNhan = model.SoDienThoaiNhan.Trim(),
                    DiaChiNhan = model.DiaChiNhan.Trim(),
                    NgayTao = DateTime.UtcNow
                };
                _db.DonHangs.Add(don);
                await _db.SaveChangesAsync();
                danhSachMaDon.Add(don.MaDon);
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
                ThongBao = LayThongBaoLoi(ex)
            };
        }
    }

    public async Task<DatHangResultViewModel> MuaNgayAsync(int maNguoiDung, int maSanPham, CheckoutViewModel model)
    {
        try
        {
            if (_db.Database.IsRelational())
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
            else
            {
                // In-Memory fallback phục vụ Unit Test logic và concurrency/race condition
                var sp = await _db.SanPhams.Include(s => s.CuaHang).FirstOrDefaultAsync(s => s.MaSanPham == maSanPham && !s.DaXoa);
                if (sp == null || sp.TrangThai != "ACTIVE" || sp.CuaHang?.TrangThai != "ACTIVE")
                {
                    return new DatHangResultViewModel
                    {
                        ThanhCong = false,
                        ThongBao = "Sản phẩm không tồn tại hoặc đã được bán."
                    };
                }
                if (sp.CuaHang.MaNguoiDung == maNguoiDung)
                {
                    return new DatHangResultViewModel
                    {
                        ThanhCong = false,
                        ThongBao = "Bạn không thể đặt mua sản phẩm của chính cửa hàng mình."
                    };
                }

                sp.TrangThai = "SOLD";
                var don = new DonHang
                {
                    MaDon = "DH" + DateTime.UtcNow.Ticks.ToString()[^8..],
                    MaNguoiDung = maNguoiDung,
                    MaCuaHang = sp.MaCuaHang,
                    TienHang = sp.Gia ?? 0,
                    TongTien = (sp.Gia ?? 0) + model.PhiVanChuyen,
                    PhiVanChuyen = model.PhiVanChuyen,
                    TrangThai = "PENDING",
                    TrangThaiThanhToan = "UNPAID",
                    PhuongThucThanhToan = model.PhuongThucThanhToan,
                    TenNguoiNhan = model.TenNguoiNhan.Trim(),
                    SoDienThoaiNhan = model.SoDienThoaiNhan.Trim(),
                    DiaChiNhan = model.DiaChiNhan.Trim(),
                    NgayTao = DateTime.UtcNow
                };
                _db.DonHangs.Add(don);
                await _db.SaveChangesAsync();

                return new DatHangResultViewModel
                {
                    ThanhCong = true,
                    ThongBao = "Đặt mua sản phẩm thành công!",
                    DanhSachMaDon = new List<string> { don.MaDon }
                };
            }
        }
        catch (Exception ex)
        {
            return new DatHangResultViewModel
            {
                ThanhCong = false,
                ThongBao = LayThongBaoLoi(ex)
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
            if (_db.Database.IsRelational())
            {
                await _db.Database.ExecuteSqlInterpolatedAsync(
                    $"EXEC sp_CapNhatTrangThaiDon @MaDonHang={maDonHang}, @TrangThaiMoi={"CANCELLED"}, @MaNguoiDung={maNguoiDung}, @LyDo={lyDo.Trim()}");
            }
            else
            {
                var donHang = await _db.DonHangs.FirstOrDefaultAsync(d => d.MaDonHang == maDonHang && d.MaNguoiDung == maNguoiDung);
                if (donHang == null) return (false, "Đơn hàng không tồn tại hoặc không thuộc quyền sở hữu của bạn.");
                if (donHang.TrangThai != "PENDING" && donHang.TrangThai != "CONFIRMED")
                {
                    return (false, "Chỉ có thể hủy đơn hàng khi đơn ở trạng thái chờ duyệt hoặc đã xác nhận.");
                }
                donHang.TrangThai = "CANCELLED";
                donHang.LyDoHuy = lyDo.Trim();
                donHang.NguoiHuy = "BUYER";
                donHang.ThoiGianHuy = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }

            return (true, "Hủy đơn hàng thành công.");
        }
        catch (Exception ex)
        {
            return (false, LayThongBaoLoi(ex));
        }
    }

    public async Task<(bool ThanhCong, string? ThongBao)> XacNhanDaNhanHangAsync(int maNguoiDung, int maDonHang)
    {
        try
        {
            if (_db.Database.IsRelational())
            {
                await _db.Database.ExecuteSqlInterpolatedAsync(
                    $"EXEC sp_CapNhatTrangThaiDon @MaDonHang={maDonHang}, @TrangThaiMoi={"COMPLETED"}, @MaNguoiDung={maNguoiDung}, @LyDo={"Người mua đã nhận được hàng"}");
            }
            else
            {
                var donHang = await _db.DonHangs.FirstOrDefaultAsync(d => d.MaDonHang == maDonHang && d.MaNguoiDung == maNguoiDung);
                if (donHang == null) return (false, "Đơn hàng không tồn tại hoặc không thuộc quyền sở hữu của bạn.");
                if (donHang.TrangThai != "DELIVERED")
                {
                    return (false, "Chỉ có thể xác nhận khi đơn hàng đã được giao đến bạn.");
                }
                donHang.TrangThai = "COMPLETED";
                donHang.ThoiGianHoanTat = DateTime.UtcNow;
                if (donHang.PhuongThucThanhToan == "COD") donHang.TrangThaiThanhToan = "PAID";
                await _db.SaveChangesAsync();
            }

            return (true, "Đã xác nhận nhận hàng và hoàn tất đơn thành công.");
        }
        catch (Exception ex)
        {
            return (false, LayThongBaoLoi(ex));
        }
    }

    private static string LayThongBaoLoi(Exception ex)
    {
        if (ex is SqlException sqlEx)
        {
            return sqlEx.Number switch
            {
                51101 => "Tài khoản người mua không hợp lệ hoặc đã bị khóa.",
                51102 => "Không có sản phẩm nào được chọn trong giỏ hàng.",
                51103 => "Bạn không thể đặt mua sản phẩm của chính cửa hàng mình.",
                51104 => "Sản phẩm không còn bán hoặc đã có người khác nhanh tay đặt mua trước.",
                51105 => "Giá sản phẩm đã được người bán cập nhật thay đổi. Vui lòng kiểm tra lại giỏ hàng.",
                51107 => "Cửa hàng bán sản phẩm này hiện đang tạm ngừng hoạt động.",
                51201 => "Đơn hàng không tồn tại trên hệ thống.",
                51203 => "Không thể chuyển trạng thái đơn hàng này.",
                51204 => "Bạn không có quyền thao tác trên đơn hàng này.",
                51205 => "Chỉ chủ cửa hàng mới có quyền xác nhận giao hàng.",
                51206 => "Chỉ người mua mới có quyền xác nhận hoàn tất đơn hàng.",
                51207 => "Hủy đơn hàng bắt buộc phải cung cấp lý do cụ thể.",
                >= 50000 and <= 53999 => sqlEx.Message,
                2601 or 2627 => "Dữ liệu bị trùng lặp hoặc đã có người khác thao tác trước. Vui lòng tải lại trang.",
                1205 => "Hệ thống đang bận xử lý giao dịch khác, vui lòng thử lại sau giây lát.",
                _ => "Đã xảy ra lỗi khi xử lý cơ sở dữ liệu. Vui lòng thử lại sau."
            };
        }

        return !string.IsNullOrWhiteSpace(ex.Message) && !ex.Message.Contains("Microsoft.") && !ex.Message.Contains("System.")
            ? ex.Message
            : "Đã xảy ra lỗi không mong muốn trong quá trình xử lý đơn hàng. Vui lòng thử lại sau.";
    }
}
