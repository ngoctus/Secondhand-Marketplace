using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SecondHandMarketplace.Web.Common;
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
            PhuongThucThanhToan = "COD",
            HinhThucNhan = "GIAO_HANG"
        };
    }

    public async Task<CheckoutViewModel?> ChuanBiCheckoutMuaNgayAsync(int maNguoiDung, int maSanPham)
    {
        var user = await _db.NguoiDungs.FindAsync(maNguoiDung);
        var sp = await _db.SanPhams
            .Include(x => x.CuaHang)
                .ThenInclude(c => c.NguoiDung)
            .Include(x => x.Anh)
            .FirstOrDefaultAsync(x => x.MaSanPham == maSanPham && !x.DaXoa);

        if (sp == null || sp.TrangThai != "ACTIVE" || sp.CuaHang.TrangThai != "ACTIVE" || (sp.CuaHang.NguoiDung != null && sp.CuaHang.NguoiDung.TrangThai != "ACTIVE"))
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
            PhuongThucThanhToan = "COD",
            HinhThucNhan = "GIAO_HANG"
        };
    }

    public async Task<DatHangResultViewModel> DatHangTuGioAsync(int maNguoiDung, CheckoutViewModel model)
    {
        var danhSachMaDon = new List<string>();

        try
        {
            // Fix #4: Tự động bỏ chọn các sản phẩm trong giỏ không còn khả dụng
            var itemsKhongKhaDung = await _db.GioHangs
                .Include(g => g.SanPham)
                .ThenInclude(s => s.CuaHang)
                .ThenInclude(c => c.NguoiDung)
                .Where(g => g.MaNguoiDung == maNguoiDung && g.DaChon &&
                            (g.SanPham.TrangThai != "ACTIVE" || g.SanPham.DaXoa ||
                             g.SanPham.CuaHang.TrangThai != "ACTIVE" || g.SanPham.CuaHang.NguoiDung.TrangThai != "ACTIVE"))
                .ToListAsync();

            if (itemsKhongKhaDung.Any())
            {
                foreach (var item in itemsKhongKhaDung)
                {
                    item.DaChon = false;
                }
                await _db.SaveChangesAsync();
            }

            var coSanPhamChon = await _db.GioHangs.AnyAsync(g => g.MaNguoiDung == maNguoiDung && g.DaChon);
            if (!coSanPhamChon)
            {
                return new DatHangResultViewModel
                {
                    ThanhCong = false,
                    ThongBao = "Không có sản phẩm khả dụng nào được chọn trong giỏ hàng."
                };
            }
            // Kiến trúc: Stored procedure sp_DatHangTuGio trong database/06_procedures.sql đã tự động
            // tạo thông báo (NotificationTypes.Order) cho chủ cửa hàng (người bán), đồng thời ghi nhận bản ghi ThanhToan với trạng thái tương ứng, đảm bảo tính toàn vẹn (ACID).
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
                cmd.Parameters.Add(new SqlParameter("@HinhThucNhan", model.HinhThucNhan ?? "GIAO_HANG"));

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
                // Fallback phục vụ test môi trường In-Memory: mô phỏng trung thực sp_DatHangTuGio
                var cartItems = await _db.GioHangs
                    .Include(g => g.SanPham)
                        .ThenInclude(s => s.CuaHang)
                    .Where(g => g.MaNguoiDung == maNguoiDung && g.DaChon)
                    .ToListAsync();

                if (!cartItems.Any())
                {
                    return new DatHangResultViewModel
                    {
                        ThanhCong = false,
                        ThongBao = "Không có sản phẩm nào được chọn trong giỏ hàng."
                    };
                }

                if (cartItems.Any(g => g.SanPham.CuaHang.MaNguoiDung == maNguoiDung))
                {
                    return new DatHangResultViewModel
                    {
                        ThanhCong = false,
                        ThongBao = "Bạn không thể đặt mua sản phẩm của chính cửa hàng mình."
                    };
                }

                if (cartItems.Any(g => g.SanPham.TrangThai != "ACTIVE" || g.SanPham.DaXoa || g.SanPham.CuaHang.TrangThai != "ACTIVE"))
                {
                    return new DatHangResultViewModel
                    {
                        ThanhCong = false,
                        ThongBao = "Có sản phẩm không còn bán hoặc không thuộc cửa hàng này."
                    };
                }

                var shopGroups = cartItems.GroupBy(g => g.SanPham.MaCuaHang).ToList();
                var phiMoiDon = (model.HinhThucNhan == "GAP_TRUC_TIEP") ? 0 : model.PhiVanChuyen;
                var counter = 0;

                foreach (var group in shopGroups)
                {
                    var shopId = group.Key;
                    var itemsInShop = group.ToList();
                    var tienHangShop = itemsInShop.Sum(i => i.SanPham.Gia ?? 0);
                    var tongTienShop = tienHangShop + phiMoiDon;
                    var maDonCode = $"DH{DateTime.UtcNow.Ticks.ToString()[^6..]}_{shopId}_{++counter}";

                    var don = new DonHang
                    {
                        MaDon = maDonCode,
                        MaNguoiDung = maNguoiDung,
                        MaCuaHang = shopId,
                        TienHang = tienHangShop,
                        PhiVanChuyen = phiMoiDon,
                        TongTien = tongTienShop,
                        TrangThai = "PENDING",
                        TrangThaiThanhToan = model.PhuongThucThanhToan == "ONLINE" ? "PAID" : "UNPAID",
                        PhuongThucThanhToan = model.PhuongThucThanhToan,
                        HinhThucNhan = model.HinhThucNhan ?? "GIAO_HANG",
                        TenNguoiNhan = model.TenNguoiNhan.Trim(),
                        SoDienThoaiNhan = model.SoDienThoaiNhan.Trim(),
                        DiaChiNhan = model.DiaChiNhan.Trim(),
                        NgayTao = DateTime.UtcNow
                    };
                    _db.DonHangs.Add(don);
                    await _db.SaveChangesAsync();

                    foreach (var item in itemsInShop)
                    {
                        _db.ChiTietDonHangs.Add(new ChiTietDonHang
                        {
                            MaDonHang = don.MaDonHang,
                            MaSanPham = item.MaSanPham,
                            TenSanPham = item.SanPham.Ten,
                            Gia = item.SanPham.Gia ?? 0,
                            DangGiuCho = true
                        });

                        item.SanPham.TrangThai = "RESERVED";
                        item.SanPham.NgayCapNhat = DateTime.UtcNow;
                    }

                    var tt = new ThanhToan
                    {
                        MaDonHang = don.MaDonHang,
                        MaGiaoDich = "TXN_" + don.MaDon,
                        PhuongThucThanhToan = model.PhuongThucThanhToan,
                        SoTien = don.TongTien,
                        TrangThaiThanhToan = model.PhuongThucThanhToan == "ONLINE" ? "SUCCESS" : "PENDING",
                        ThoiGianThanhToan = model.PhuongThucThanhToan == "ONLINE" ? DateTime.UtcNow : null,
                        NgayTao = DateTime.UtcNow
                    };
                    _db.ThanhToans.Add(tt);

                    danhSachMaDon.Add(don.MaDon);
                }

                _db.GioHangs.RemoveRange(cartItems);
                await _db.SaveChangesAsync();
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
                cmd.Parameters.Add(new SqlParameter("@HinhThucNhan", model.HinhThucNhan ?? "GIAO_HANG"));

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
                sp.NgayCapNhat = DateTime.UtcNow;
                var phiVc = (model.HinhThucNhan == "GAP_TRUC_TIEP") ? 0 : model.PhiVanChuyen;
                var don = new DonHang
                {
                    MaDon = "DH" + DateTime.UtcNow.Ticks.ToString()[^8..],
                    MaNguoiDung = maNguoiDung,
                    MaCuaHang = sp.MaCuaHang,
                    TienHang = sp.Gia ?? 0,
                    TongTien = (sp.Gia ?? 0) + phiVc,
                    PhiVanChuyen = phiVc,
                    TrangThai = "PENDING",
                    TrangThaiThanhToan = model.PhuongThucThanhToan == "ONLINE" ? "PAID" : "UNPAID",
                    PhuongThucThanhToan = model.PhuongThucThanhToan,
                    HinhThucNhan = model.HinhThucNhan ?? "GIAO_HANG",
                    TenNguoiNhan = model.TenNguoiNhan.Trim(),
                    SoDienThoaiNhan = model.SoDienThoaiNhan.Trim(),
                    DiaChiNhan = model.DiaChiNhan.Trim(),
                    NgayTao = DateTime.UtcNow
                };
                _db.DonHangs.Add(don);
                await _db.SaveChangesAsync();

                _db.ChiTietDonHangs.Add(new ChiTietDonHang
                {
                    MaDonHang = don.MaDonHang,
                    MaSanPham = sp.MaSanPham,
                    TenSanPham = sp.Ten,
                    Gia = sp.Gia ?? 0,
                    DangGiuCho = true
                });

                var tt = new ThanhToan
                {
                    MaDonHang = don.MaDonHang,
                    MaGiaoDich = "TXN_" + don.MaDon,
                    PhuongThucThanhToan = model.PhuongThucThanhToan,
                    SoTien = don.TongTien,
                    TrangThaiThanhToan = model.PhuongThucThanhToan == "ONLINE" ? "SUCCESS" : "PENDING",
                    ThoiGianThanhToan = model.PhuongThucThanhToan == "ONLINE" ? DateTime.UtcNow : null,
                    NgayTao = DateTime.UtcNow
                };
                _db.ThanhToans.Add(tt);
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
        // Kiến trúc: Sử dụng EF Core LINQ trên _db.DonHangs (tương đương logic vw_DonHangTongQuan)
        // để hỗ trợ cả truy vấn quan hệ và môi trường kiểm thử tự động.
        var query = _db.DonHangs
            .Include(d => d.CuaHang)
            .Include(d => d.ChiTiet)
            .Where(d => d.MaNguoiDung == maNguoiDung);

        var ttChuan = trangThai?.Trim().ToUpperInvariant();
        if (ttChuan is "PENDING" or "CONFIRMED" or "SHIPPING" or "DELIVERED" or "COMPLETED" or "CANCELLED")
        {
            query = query.Where(d => d.TrangThai == ttChuan);
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
            HinhThucNhan = d.HinhThucNhan,
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
        // Kiến trúc: Sử dụng EF Core LINQ trên _db.DonHangs tương đương vw_ChiTietDonHang.
        var d = await _db.DonHangs
            .Include(x => x.CuaHang)
            .Include(x => x.ChiTiet)
            .Include(x => x.LichSu)
            .FirstOrDefaultAsync(x => x.MaDonHang == maDonHang && x.MaNguoiDung == maNguoiDung);

        if (d == null) return null;

        var maTranhChap = await _db.TranhChaps
            .Where(t => t.MaDonHang == maDonHang)
            .OrderByDescending(t => t.NgayTao)
            .Select(t => (int?)t.MaTranhChap)
            .FirstOrDefaultAsync();

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
            }).ToList(),
            MaTranhChap = maTranhChap
        };
    }

    public async Task<(bool ThanhCong, string? ThongBao)> HuyDonMuaAsync(int maNguoiDung, int maDonHang, string lyDo)
    {
        if (string.IsNullOrWhiteSpace(lyDo))
        {
            return (false, "Vui lòng nhập lý do hủy đơn.");
        }

        // Fix #2: Kiểm tra trạng thái đơn hàng phía Buyer trước khi xử lý
        var donHang = await _db.DonHangs.FirstOrDefaultAsync(d => d.MaDonHang == maDonHang && d.MaNguoiDung == maNguoiDung);
        if (donHang == null) return (false, "Đơn hàng không tồn tại hoặc không thuộc quyền sở hữu của bạn.");
        if (donHang.TrangThai != "PENDING" && donHang.TrangThai != "CONFIRMED")
        {
            return (false, "Chỉ có thể hủy đơn hàng khi đơn ở trạng thái chờ duyệt hoặc đã xác nhận (shop chưa giao hàng).");
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
                donHang.TrangThai = "CANCELLED";
                donHang.LyDoHuy = lyDo.Trim();
                donHang.NguoiHuy = "BUYER";
                donHang.ThoiGianHuy = DateTime.UtcNow;
                if (donHang.TrangThaiThanhToan == "PAID")
                {
                    donHang.TrangThaiThanhToan = "REFUNDED";
                    var tt = await _db.ThanhToans.FirstOrDefaultAsync(t => t.MaDonHang == maDonHang);
                    if (tt != null)
                    {
                        tt.TrangThaiThanhToan = "REFUNDED";
                    }
                }

                var chiTiets = await _db.ChiTietDonHangs
                    .Include(c => c.SanPham)
                    .Where(c => c.MaDonHang == maDonHang).ToListAsync();
                foreach (var ct in chiTiets)
                {
                    ct.DangGiuCho = false;
                    if (ct.SanPham != null && ct.SanPham.TrangThai == "RESERVED")
                    {
                        ct.SanPham.TrangThai = "ACTIVE";
                    }
                }

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

    public async Task<List<DonHangItemViewModel>> LayDanhSachDonTheoMaAsync(int maNguoiDung, List<string> danhSachMaDon)
    {
        if (danhSachMaDon == null || danhSachMaDon.Count == 0) return new();

        var query = _db.DonHangs.AsNoTracking()
            .Include(d => d.CuaHang)
            .Include(d => d.ChiTiet)
            .Where(d => d.MaNguoiDung == maNguoiDung && danhSachMaDon.Contains(d.MaDon));

        var list = await query.ToListAsync();
        return list.Select(d => new DonHangItemViewModel
        {
            MaDonHang = d.MaDonHang,
            MaDon = d.MaDon,
            MaCuaHang = d.MaCuaHang,
            TenShop = d.CuaHang?.TenShop ?? "",
            TienHang = d.TienHang,
            PhiVanChuyen = d.PhiVanChuyen,
            TongTien = d.TongTien,
            TrangThai = d.TrangThai,
            TrangThaiThanhToan = d.TrangThaiThanhToan,
            PhuongThucThanhToan = d.PhuongThucThanhToan,
            HinhThucNhan = d.HinhThucNhan,
            NgayTao = d.NgayTao,
            ChiTiet = d.ChiTiet.Select(c => new ChiTietDonHangItemViewModel
            {
                MaChiTietDonHang = c.MaChiTietDonHang,
                MaSanPham = c.MaSanPham,
                TenSanPham = c.TenSanPham,
                AnhDaiDien = c.AnhDaiDien,
                Gia = c.Gia
            }).ToList()
        }).ToList();
    }

    public async Task<(bool ThanhCong, string? ThongBao)> XacNhanThanhToanChuyenKhoanAsync(int maNguoiDung, int maDonHang)
    {
        var don = await _db.DonHangs.FirstOrDefaultAsync(d => d.MaDonHang == maDonHang && d.MaNguoiDung == maNguoiDung);
        if (don == null) return (false, "Đơn hàng không tồn tại.");
        if (don.TrangThaiThanhToan == "PAID") return (true, "Đơn hàng đã được thanh toán trước đó.");

        don.TrangThaiThanhToan = "PAID";
        don.NgayCapNhat = DateTime.UtcNow;

        var tt = await _db.ThanhToans.FirstOrDefaultAsync(t => t.MaDonHang == maDonHang);
        if (tt != null)
        {
            tt.TrangThaiThanhToan = "SUCCESS";
            tt.ThoiGianThanhToan = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return (true, "Xác nhận chuyển khoản thành công! Cảm ơn bạn.");
    }

    public async Task<(bool ThanhCong, string? ThongBao)> XacNhanThanhToanTatCaChuyenKhoanAsync(int maNguoiDung, IEnumerable<int> maDonHangs)
    {
        var idList = maDonHangs.ToList();
        if (idList.Count == 0) return (false, "Không có đơn hàng nào.");

        var dons = await _db.DonHangs.Where(d => idList.Contains(d.MaDonHang) && d.MaNguoiDung == maNguoiDung).ToListAsync();
        if (dons.Count == 0) return (false, "Đơn hàng không tồn tại.");

        foreach (var don in dons)
        {
            don.TrangThaiThanhToan = "PAID";
            don.NgayCapNhat = DateTime.UtcNow;
        }

        var tts = await _db.ThanhToans.Where(t => idList.Contains(t.MaDonHang)).ToListAsync();
        foreach (var tt in tts)
        {
            tt.TrangThaiThanhToan = "SUCCESS";
            tt.ThoiGianThanhToan = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return (true, "Xác nhận chuyển khoản thành công cho các đơn hàng! Cảm ơn bạn.");
    }

    public async Task<(bool ThanhCong, string? ThongBao)> XacNhanDaNhanTienShopAsync(int chuShopId, int maDonHang)
    {
        var don = await _db.DonHangs
            .Include(d => d.CuaHang)
            .FirstOrDefaultAsync(d => d.MaDonHang == maDonHang && d.CuaHang.MaNguoiDung == chuShopId);

        if (don == null) return (false, "Đơn hàng không tồn tại hoặc không thuộc cửa hàng của bạn.");

        don.TrangThaiThanhToan = "PAID";
        don.NgayCapNhat = DateTime.UtcNow;

        var tt = await _db.ThanhToans.FirstOrDefaultAsync(t => t.MaDonHang == maDonHang);
        if (tt != null)
        {
            tt.TrangThaiThanhToan = "SUCCESS";
            tt.ThoiGianThanhToan = DateTime.UtcNow;
        }
        else
        {
            _db.ThanhToans.Add(new ThanhToan
            {
                MaDonHang = don.MaDonHang,
                MaGiaoDich = "TXN_" + don.MaDon,
                PhuongThucThanhToan = don.PhuongThucThanhToan,
                SoTien = don.TongTien,
                TrangThaiThanhToan = "SUCCESS",
                ThoiGianThanhToan = DateTime.UtcNow,
                NgayTao = DateTime.UtcNow
            });
        }

        _db.LichSuTrangThaiDonHangs.Add(new LichSuTrangThaiDonHang
        {
            MaDonHang = don.MaDonHang,
            TrangThaiCu = don.TrangThai,
            TrangThaiMoi = don.TrangThai,
            LoaiNguoiThucHien = "SELLER",
            NguoiThucHienId = chuShopId,
            LyDo = "Người bán xác nhận đã nhận đủ tiền thanh toán.",
            ThoiGian = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        return (true, "Đã xác nhận nhận tiền thành công!");
    }

    private static string LayThongBaoLoi(Exception ex) => SqlLoi.ThongBao(ex);
}
