using Microsoft.EntityFrameworkCore;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Models.Entities;
using SecondHandMarketplace.Web.Models.ViewModels.Cart;
using SecondHandMarketplace.Web.Models.ViewModels.Orders;
using SecondHandMarketplace.Web.Services.Carts;
using SecondHandMarketplace.Web.Services.Orders;
using Xunit;

namespace SecondHandMarketplace.Tests;

public class FakeCartService : ICartService
{
    public CartIndexViewModel GioHangReturn { get; set; } = new();

    public Task<CartIndexViewModel> LayGioHangAsync(int maNguoiDung) => Task.FromResult(GioHangReturn);
    public Task<(bool ThanhCong, string? ThongBao)> ThemVaoGioAsync(int maNguoiDung, int maSanPham) => Task.FromResult((true, (string?)"OK"));
    public Task<bool> CapNhatTrangThaiChonAsync(int maNguoiDung, int maGioHang, bool daChon) => Task.FromResult(true);
    public Task<bool> CapNhatChonTatCaTheoShopAsync(int maNguoiDung, int maCuaHang, bool daChon) => Task.FromResult(true);
    public Task<bool> XoaKhoiGioAsync(int maNguoiDung, int maGioHang) => Task.FromResult(true);
    public Task<int> DemSoLuongMonAsync(int maNguoiDung) => Task.FromResult(0);
}

public class OrderServiceTests
{
    private AppDbContext GetInMemoryDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task ChuanBiCheckoutTuGioAsync_LayDungThongTinNguoiNhanVaTienHang()
    {
        using var db = GetInMemoryDbContext("OrderTest_CheckoutTuGio");
        var fakeCart = new FakeCartService();

        db.NguoiDungs.Add(new NguoiDung
        {
            MaNguoiDung = 1,
            HoTen = "Nguyen Van A",
            SoDienThoai = "0987654321",
            DiaChiMacDinh = "123 Le Loi, Q1"
        });
        await db.SaveChangesAsync();

        fakeCart.GioHangReturn = new CartIndexViewModel
        {
            DanhSachNhomShop = new List<CartShopGroupViewModel>
            {
                new()
                {
                    MaCuaHang = 10,
                    TenShop = "Shop A",
                    Items = new List<CartItemViewModel>
                    {
                        new() { MaSanPham = 100, TenSanPham = "Laptop Cu", Gia = 5000000, DaChon = true, CoTheMua = true }
                    }
                }
            }
        };

        var service = new OrderService(db, fakeCart);
        var result = await service.ChuanBiCheckoutTuGioAsync(1);

        Assert.Equal("Nguyen Van A", result.TenNguoiNhan);
        Assert.Equal("0987654321", result.SoDienThoaiNhan);
        Assert.Equal("123 Le Loi, Q1", result.DiaChiNhan);
        Assert.Equal(5000000, result.TienHang);
    }

    [Fact]
    public async Task ChuanBiCheckoutMuaNgayAsync_KhongChoPhepMuaSanPhamCuaChinhShopMinh()
    {
        using var db = GetInMemoryDbContext("OrderTest_MuaNgayCuaMinh");
        var fakeCart = new FakeCartService();

        var shop = new CuaHang { MaCuaHang = 1, MaNguoiDung = 10, TenShop = "Shop Cua Toi", TrangThai = "ACTIVE" };
        var sp = new SanPham
        {
            MaSanPham = 200,
            MaCuaHang = 1,
            Ten = "May Doc Sach",
            Gia = 1500000,
            TrangThai = "ACTIVE",
            DaXoa = false,
            CuaHang = shop
        };
        db.CuaHangs.Add(shop);
        db.SanPhams.Add(sp);
        await db.SaveChangesAsync();

        var service = new OrderService(db, fakeCart);
        var result = await service.ChuanBiCheckoutMuaNgayAsync(10, 200);

        Assert.Null(result); // Trả về null vì không được mua sản phẩm của shop mình
    }

    [Fact]
    public async Task HuyDonMuaAsync_LyDoRong_TraVeLoi()
    {
        using var db = GetInMemoryDbContext("OrderTest_HuyDonRong");
        var fakeCart = new FakeCartService();

        var service = new OrderService(db, fakeCart);
        var (thanhCong, thongBao) = await service.HuyDonMuaAsync(1, 10, "   ");

        Assert.False(thanhCong);
        Assert.Equal("Vui lòng nhập lý do hủy đơn.", thongBao);
    }

    [Fact]
    public async Task MuaNgayAsync_HaiBuyerDuaKhoCungMotSanPham_BuyerSauBiChan()
    {
        using var db = GetInMemoryDbContext("OrderTest_DuaKho");
        var fakeCart = new FakeCartService();

        var shop = new CuaHang { MaCuaHang = 1, MaNguoiDung = 99, TenShop = "Tech Store", TrangThai = "ACTIVE" };
        var sp = new SanPham
        {
            MaSanPham = 500,
            MaCuaHang = 1,
            Ten = "iPhone 13 Pro Max",
            Gia = 15000000,
            TrangThai = "ACTIVE",
            DaXoa = false,
            CuaHang = shop
        };
        db.CuaHangs.Add(shop);
        db.SanPhams.Add(sp);
        await db.SaveChangesAsync();

        var service = new OrderService(db, fakeCart);

        var checkoutVm = new CheckoutViewModel
        {
            TenNguoiNhan = "Buyer Test",
            SoDienThoaiNhan = "0901234567",
            DiaChiNhan = "123 Nguyen Hue",
            PhuongThucThanhToan = "COD"
        };

        // Buyer 1 (id 1) đặt mua trước
        var res1 = await service.MuaNgayAsync(1, 500, checkoutVm);
        Assert.True(res1.ThanhCong);
        Assert.NotEmpty(res1.DanhSachMaDon);

        // Kiểm tra sản phẩm đã chuyển trạng thái sang SOLD
        var spSauKhiMua = await db.SanPhams.FindAsync(500);
        Assert.Equal("RESERVED", spSauKhiMua!.TrangThai);

        // Buyer 2 (id 2) cũng cố mua ngay sản phẩm 500
        var res2 = await service.MuaNgayAsync(2, 500, checkoutVm);
        Assert.False(res2.ThanhCong);
        Assert.Empty(res2.DanhSachMaDon);
        Assert.Contains("không tồn tại hoặc đã được bán", res2.ThongBao);
    }

    [Fact]
    public async Task HuyDonMuaAsync_DonDaHoanTat_KhongChoHuy()
    {
        using var db = GetInMemoryDbContext("OrderTest_HuyDonSaiTrangThai");
        var fakeCart = new FakeCartService();

        var don = new DonHang
        {
            MaDonHang = 10,
            MaNguoiDung = 1,
            MaCuaHang = 2,
            TrangThai = "COMPLETED",
            TrangThaiThanhToan = "PAID",
            TongTien = 200000,
            NgayTao = DateTime.UtcNow
        };
        db.DonHangs.Add(don);
        await db.SaveChangesAsync();

        var service = new OrderService(db, fakeCart);
        var (ok, msg) = await service.HuyDonMuaAsync(1, 10, "Đổi ý muốn hủy");

        Assert.False(ok);
        Assert.Contains("Chỉ có thể hủy đơn hàng khi đơn ở trạng thái chờ duyệt hoặc đã xác nhận", msg);
    }

    [Fact]
    public async Task XacNhanDaNhanHangAsync_DonChuaGiao_KhongChoXacNhan()
    {
        using var db = GetInMemoryDbContext("OrderTest_NhanHangSaiTrangThai");
        var fakeCart = new FakeCartService();

        var don = new DonHang
        {
            MaDonHang = 20,
            MaNguoiDung = 1,
            MaCuaHang = 2,
            TrangThai = "SHIPPING", // Chưa DELIVERED
            TrangThaiThanhToan = "PAID",
            TongTien = 500000,
            NgayTao = DateTime.UtcNow
        };
        db.DonHangs.Add(don);
        await db.SaveChangesAsync();

        var service = new OrderService(db, fakeCart);
        var (ok, msg) = await service.XacNhanDaNhanHangAsync(1, 20);

        Assert.False(ok);
        Assert.Contains("Chỉ có thể xác nhận khi đơn hàng đã được giao đến bạn", msg);
    }

    [Fact]
    public async Task HuyDonMuaAsync_DonDangGiao_KhongChoHuy()
    {
        using var db = GetInMemoryDbContext("OrderTest_HuyDonDangGiao");
        var fakeCart = new FakeCartService();

        var don = new DonHang
        {
            MaDonHang = 21,
            MaNguoiDung = 1,
            MaCuaHang = 2,
            TrangThai = "SHIPPING",
            TrangThaiThanhToan = "PAID",
            TongTien = 200000,
            NgayTao = DateTime.UtcNow
        };
        db.DonHangs.Add(don);
        await db.SaveChangesAsync();

        var service = new OrderService(db, fakeCart);
        var (ok, msg) = await service.HuyDonMuaAsync(1, 21, "Muốn hủy khi đang giao");

        Assert.False(ok);
        Assert.Contains("Chỉ có thể hủy đơn hàng khi đơn ở trạng thái chờ duyệt hoặc đã xác nhận", msg);
    }

    [Fact]
    public async Task HuyDonMuaAsync_DonDaGiao_KhongChoHuy()
    {
        using var db = GetInMemoryDbContext("OrderTest_HuyDonDaGiao");
        var fakeCart = new FakeCartService();

        var don = new DonHang
        {
            MaDonHang = 22,
            MaNguoiDung = 1,
            MaCuaHang = 2,
            TrangThai = "DELIVERED",
            TrangThaiThanhToan = "PAID",
            TongTien = 350000,
            NgayTao = DateTime.UtcNow
        };
        db.DonHangs.Add(don);
        await db.SaveChangesAsync();

        var service = new OrderService(db, fakeCart);
        var (ok, msg) = await service.HuyDonMuaAsync(1, 22, "Muốn hủy khi đã giao");

        Assert.False(ok);
        Assert.Contains("Chỉ có thể hủy đơn hàng khi đơn ở trạng thái chờ duyệt hoặc đã xác nhận", msg);
    }

    [Fact]
    public async Task HuyDonMuaAsync_DonPending_ThanhCongVaGiaiPhongGiuCho()
    {
        using var db = GetInMemoryDbContext("OrderTest_HuyPendingGiaiPhong");
        var fakeCart = new FakeCartService();

        var sp = new SanPham
        {
            MaSanPham = 50,
            MaCuaHang = 2,
            Ten = "Laptop Test",
            Gia = 1000000,
            TrangThai = "RESERVED"
        };
        db.SanPhams.Add(sp);

        var don = new DonHang
        {
            MaDonHang = 23,
            MaNguoiDung = 1,
            MaCuaHang = 2,
            TrangThai = "PENDING",
            TrangThaiThanhToan = "PAID",
            TongTien = 1000000,
            NgayTao = DateTime.UtcNow
        };
        db.DonHangs.Add(don);

        db.ChiTietDonHangs.Add(new ChiTietDonHang
        {
            MaDonHang = 23,
            MaSanPham = 50,
            Gia = 1000000,
                        DangGiuCho = true
        });

        db.ThanhToans.Add(new ThanhToan
        {
            MaDonHang = 23,
            SoTien = 1000000,
            PhuongThucThanhToan = "ONLINE",
            TrangThaiThanhToan = "SUCCESS"
        });

        await db.SaveChangesAsync();

        var service = new OrderService(db, fakeCart);
        var (ok, msg) = await service.HuyDonMuaAsync(1, 23, "Khách đổi ý");

        Assert.True(ok);
        var donDb = await db.DonHangs.FindAsync(23);
        Assert.Equal("CANCELLED", donDb!.TrangThai);
        Assert.Equal("REFUNDED", donDb.TrangThaiThanhToan);

        var ctDb = await db.ChiTietDonHangs.FirstAsync(c => c.MaDonHang == 23);
        Assert.False(ctDb.DangGiuCho);

        var spDb = await db.SanPhams.FindAsync(50);
        Assert.Equal("ACTIVE", spDb!.TrangThai);

        var ttDb = await db.ThanhToans.FirstAsync(t => t.MaDonHang == 23);
        Assert.Equal("REFUNDED", ttDb.TrangThaiThanhToan);
    }

    [Fact]
    public async Task DatHang_VoiHinhThucNhan_GapTrucTiep_LuuDungVaoDonHang()
    {
        using var db = GetInMemoryDbContext("OrderTest_HinhThucNhanGapTrucTiep");
        var fakeCart = new FakeCartService();

        var shop = new CuaHang { MaCuaHang = 1, MaNguoiDung = 10, TenShop = "Shop Test", TrangThai = "ACTIVE" };
        var sp = new SanPham { MaSanPham = 101, MaCuaHang = 1, Ten = "Sach Cu", Gia = 50000, TrangThai = "ACTIVE" };
        db.CuaHangs.Add(shop);
        db.SanPhams.Add(sp);
        await db.SaveChangesAsync();

        var model = new CheckoutViewModel
        {
            TenNguoiNhan = "Nguoi Mua A",
            SoDienThoaiNhan = "0988888888",
            DiaChiNhan = "Cong truong UTC2",
            HinhThucNhan = "GAP_TRUC_TIEP",
            PhuongThucThanhToan = "COD",
            PhiVanChuyen = 0
        };

        var service = new OrderService(db, fakeCart);
        var res = await service.MuaNgayAsync(2, 101, model);

        Assert.True(res.ThanhCong);
        var don = await db.DonHangs.FirstOrDefaultAsync();
        Assert.NotNull(don);
        Assert.Equal("GAP_TRUC_TIEP", don.HinhThucNhan);
        Assert.Equal("Cong truong UTC2", don.DiaChiNhan);
    }

    [Fact]
    public async Task LayChiTietDonMuaAsync_NguoiDungKhacKhongXemDuocDon_BaoVeIDOR()
    {
        using var db = GetInMemoryDbContext("OrderTest_BaoVeIDOR");
        var fakeCart = new FakeCartService();

        var don = new DonHang
        {
            MaDonHang = 99,
            MaNguoiDung = 5,
            MaCuaHang = 1,
            MaDon = "DH99",
            TrangThai = "PENDING"
        };
        db.DonHangs.Add(don);
        await db.SaveChangesAsync();

        var service = new OrderService(db, fakeCart);
        var detail = await service.LayChiTietDonMuaAsync(999, 99);

        Assert.Null(detail);
    }

    [Fact]
    public async Task XacNhanDaNhanTienShopAsync_ChuShopXacNhanThanhToan_ChuyenTrangThaiPaid()
    {
        using var db = GetInMemoryDbContext("OrderTest_XacNhanNhanTienShop");
        var fakeCart = new FakeCartService();

        var shop = new CuaHang { MaCuaHang = 5, MaNguoiDung = 88, TenShop = "Shop Chu 88", TrangThai = "ACTIVE" };
        var don = new DonHang
        {
            MaDonHang = 55,
            MaNguoiDung = 2,
            MaCuaHang = 5,
            MaDon = "DH55",
            TongTien = 300000,
            PhuongThucThanhToan = "BANK_TRANSFER",
            TrangThaiThanhToan = "UNPAID",
            TrangThai = "CONFIRMED"
        };
        db.CuaHangs.Add(shop);
        db.DonHangs.Add(don);
        await db.SaveChangesAsync();

        var service = new OrderService(db, fakeCart);

        var (okLa, _) = await service.XacNhanDaNhanTienShopAsync(999, 55);
        Assert.False(okLa);

        var (ok, _) = await service.XacNhanDaNhanTienShopAsync(88, 55);
        Assert.True(ok);

        var donDb = await db.DonHangs.FindAsync(55);
        Assert.Equal("PAID", donDb!.TrangThaiThanhToan);

        var ttDb = await db.ThanhToans.FirstOrDefaultAsync(t => t.MaDonHang == 55);
        Assert.NotNull(ttDb);
        Assert.Equal("SUCCESS", ttDb.TrangThaiThanhToan);
    }

    [Fact]
    public async Task DatHangTuGioAsync_NhieuShop_TachDonRiengBietVaDoiTrangThaiReserved()
    {
        using var db = GetInMemoryDbContext("OrderTest_TachDonNhieuShop");
        var fakeCart = new FakeCartService();
        var service = new OrderService(db, fakeCart);

        var shop1 = new CuaHang { MaCuaHang = 10, MaNguoiDung = 100, TenShop = "Shop A", TrangThai = "ACTIVE" };
        var shop2 = new CuaHang { MaCuaHang = 20, MaNguoiDung = 200, TenShop = "Shop B", TrangThai = "ACTIVE" };
        db.CuaHangs.AddRange(shop1, shop2);

        var sp1 = new SanPham { MaSanPham = 1, MaCuaHang = 10, Ten = "Sản phẩm A1", Gia = 100000, TrangThai = "ACTIVE", CuaHang = shop1 };
        var sp2 = new SanPham { MaSanPham = 2, MaCuaHang = 20, Ten = "Sản phẩm B1", Gia = 200000, TrangThai = "ACTIVE", CuaHang = shop2 };
        db.SanPhams.AddRange(sp1, sp2);

        db.GioHangs.Add(new GioHang { MaGioHang = 1, MaNguoiDung = 5, MaSanPham = 1, DaChon = true, SanPham = sp1 });
        db.GioHangs.Add(new GioHang { MaGioHang = 2, MaNguoiDung = 5, MaSanPham = 2, DaChon = true, SanPham = sp2 });
        await db.SaveChangesAsync();

        var model = new SecondHandMarketplace.Web.Models.ViewModels.Orders.CheckoutViewModel
        {
            TenNguoiNhan = "Nguyễn Văn Test",
            SoDienThoaiNhan = "0987654321",
            DiaChiNhan = "123 Đường Test, Quận 1",
            PhiVanChuyen = 30000,
            PhuongThucThanhToan = "COD",
            HinhThucNhan = "GIAO_HANG"
        };

        var result = await service.DatHangTuGioAsync(5, model);

        Assert.True(result.ThanhCong);
        Assert.Equal(2, result.DanhSachMaDon.Count);

        // Kiểm tra database có 2 đơn hàng thuộc 2 shop khác nhau
        var donHangs = await db.DonHangs.Where(d => d.MaNguoiDung == 5).ToListAsync();
        Assert.Equal(2, donHangs.Count);
        Assert.Contains(donHangs, d => d.MaCuaHang == 10);
        Assert.Contains(donHangs, d => d.MaCuaHang == 20);

        // Kiểm tra sản phẩm đã đổi sang RESERVED
        var p1 = await db.SanPhams.FindAsync(1);
        var p2 = await db.SanPhams.FindAsync(2);
        Assert.Equal("RESERVED", p1!.TrangThai);
        Assert.Equal("RESERVED", p2!.TrangThai);

        // Kiểm tra giỏ hàng đã được xóa sạch
        var conTrongGio = await db.GioHangs.Where(g => g.MaNguoiDung == 5).CountAsync();
        Assert.Equal(0, conTrongGio);

        // Kiểm tra chi tiết đơn hàng
        var chiTiets = await db.ChiTietDonHangs.ToListAsync();
        Assert.Equal(2, chiTiets.Count);
        Assert.All(chiTiets, ct => Assert.True(ct.DangGiuCho));
    }

    [Fact]
    public async Task DatHangTuGioAsync_GapTrucTiep_EpPhiVanChuyenVeKhong_VaGuiThongBaoChoShop()
    {
        using var db = GetInMemoryDbContext("OrderTest_EpPhiShip0VaThongBaoShop");
        var fakeCart = new FakeCartService();
        var service = new OrderService(db, fakeCart);

        var shop = new CuaHang { MaCuaHang = 30, MaNguoiDung = 300, TenShop = "Shop C", TrangThai = "ACTIVE" };
        db.CuaHangs.Add(shop);

        var sp = new SanPham { MaSanPham = 301, MaCuaHang = 30, Ten = "Sản phẩm C1", Gia = 150000, TrangThai = "ACTIVE", CuaHang = shop };
        db.SanPhams.Add(sp);

        db.GioHangs.Add(new GioHang { MaGioHang = 30, MaNguoiDung = 6, MaSanPham = 301, DaChon = true, SanPham = sp });
        await db.SaveChangesAsync();

        var model = new SecondHandMarketplace.Web.Models.ViewModels.Orders.CheckoutViewModel
        {
            TenNguoiNhan = "Trần Thị B",
            SoDienThoaiNhan = "0912345678",
            DiaChiNhan = "Quán trà sữa cổng UTC2",
            PhiVanChuyen = 50000, // Cố tình truyền 50,000đ khi gặp trực tiếp
            PhuongThucThanhToan = "COD",
            HinhThucNhan = "GAP_TRUC_TIEP"
        };

        var result = await service.DatHangTuGioAsync(6, model);

        Assert.True(result.ThanhCong);
        var don = await db.DonHangs.FirstOrDefaultAsync(d => d.MaNguoiDung == 6);
        Assert.NotNull(don);
        Assert.Equal(0, don.PhiVanChuyen);
        Assert.Equal(150000, don.TongTien);

        // Kiểm tra có thông báo cho người bán
        var thongBao = await db.ThongBaos.FirstOrDefaultAsync(tb => tb.MaNguoiDung == 300);
        Assert.NotNull(thongBao);
        Assert.Equal("ORDER", thongBao.Loai);
    }

    [Fact]
    public async Task XacNhanThanhToanTatCaChuyenKhoanAsync_ChiCapNhatDonCuaChinhMinh_BaoVeIDOR()
    {
        using var db = GetInMemoryDbContext("OrderTest_BatchPaymentIDOR");
        var fakeCart = new FakeCartService();
        var service = new OrderService(db, fakeCart);

        // Đơn của user 10
        var don10 = new DonHang { MaDonHang = 1001, MaNguoiDung = 10, MaCuaHang = 1, MaDon = "DH1001", PhuongThucThanhToan = "BANK_TRANSFER" };
        var tt10 = new ThanhToan { MaThanhToan = 1001, MaDonHang = 1001, PhuongThucThanhToan = "BANK_TRANSFER", TrangThaiThanhToan = "PENDING" };

        // Đơn của victim user 20
        var don20 = new DonHang { MaDonHang = 2001, MaNguoiDung = 20, MaCuaHang = 2, MaDon = "DH2001", PhuongThucThanhToan = "BANK_TRANSFER" };
        var tt20 = new ThanhToan { MaThanhToan = 2001, MaDonHang = 2001, PhuongThucThanhToan = "BANK_TRANSFER", TrangThaiThanhToan = "PENDING" };

        db.DonHangs.AddRange(don10, don20);
        db.ThanhToans.AddRange(tt10, tt20);
        await db.SaveChangesAsync();

        // User 10 cố gắng POST cả đơn của mình (1001) lẫn đơn của user khác (2001)
        var (ok, _) = await service.XacNhanThanhToanTatCaChuyenKhoanAsync(10, new List<int> { 1001, 2001 });

        Assert.True(ok);

        // Đơn 1001 của user 10 phải được cập nhật SUCCESS
        var tt10Db = await db.ThanhToans.FindAsync(1001);
        Assert.Equal("SUCCESS", tt10Db!.TrangThaiThanhToan);

        // Đơn 2001 của victim KHÔNG được bị thay đổi, vẫn là PENDING
        var tt20Db = await db.ThanhToans.FindAsync(2001);
        Assert.Equal("PENDING", tt20Db!.TrangThaiThanhToan);
    }

    [Fact]
    public async Task XacNhanThanhToanChuyenKhoanAsync_DonDaHuyHoacHoanTien_KhongChoXacNhan()
    {
        using var db = GetInMemoryDbContext("OrderTest_PaymentCancelledCheck");
        var fakeCart = new FakeCartService();
        var service = new OrderService(db, fakeCart);

        var donHuy = new DonHang
        {
            MaDonHang = 301,
            MaNguoiDung = 5,
            MaCuaHang = 1,
            MaDon = "DH301",
            PhuongThucThanhToan = "BANK_TRANSFER",
            TrangThai = "CANCELLED"
        };
        var donHoanTien = new DonHang
        {
            MaDonHang = 302,
            MaNguoiDung = 5,
            MaCuaHang = 1,
            MaDon = "DH302",
            PhuongThucThanhToan = "BANK_TRANSFER",
            TrangThai = "CONFIRMED",
            TrangThaiThanhToan = "REFUNDED"
        };
        db.DonHangs.AddRange(donHuy, donHoanTien);
        await db.SaveChangesAsync();

        var (ok1, msg1) = await service.XacNhanThanhToanChuyenKhoanAsync(5, 301);
        Assert.False(ok1);
        Assert.Contains("đã hủy", msg1);

        var (ok2, msg2) = await service.XacNhanThanhToanChuyenKhoanAsync(5, 302);
        Assert.False(ok2);
        Assert.Contains("hoàn tiền", msg2);
    }

    [Fact]
    public async Task XacNhanDaNhanTienShopAsync_DonPendingHoacDaHuy_KhongChoXacNhan()
    {
        using var db = GetInMemoryDbContext("OrderTest_ShopPaymentPendingCheck");
        var fakeCart = new FakeCartService();
        var service = new OrderService(db, fakeCart);

        var shop = new CuaHang { MaCuaHang = 7, MaNguoiDung = 77, TenShop = "Shop 77", TrangThai = "ACTIVE" };
        var donPending = new DonHang
        {
            MaDonHang = 401,
            MaNguoiDung = 2,
            MaCuaHang = 7,
            MaDon = "DH401",
            TrangThai = "PENDING",
            PhuongThucThanhToan = "BANK_TRANSFER",
            TrangThaiThanhToan = "UNPAID"
        };
        var donHuy = new DonHang
        {
            MaDonHang = 402,
            MaNguoiDung = 2,
            MaCuaHang = 7,
            MaDon = "DH402",
            TrangThai = "CANCELLED",
            PhuongThucThanhToan = "BANK_TRANSFER",
            TrangThaiThanhToan = "UNPAID"
        };
        db.CuaHangs.Add(shop);
        db.DonHangs.AddRange(donPending, donHuy);
        await db.SaveChangesAsync();

        var (okPending, msgPending) = await service.XacNhanDaNhanTienShopAsync(77, 401);
        Assert.False(okPending);
        Assert.Contains("đã được xác nhận hoặc đang thực hiện", msgPending);

        var (okHuy, msgHuy) = await service.XacNhanDaNhanTienShopAsync(77, 402);
        Assert.False(okHuy);
        Assert.Contains("đã hủy", msgHuy);
    }

    [Fact]
    public async Task XacNhanDaNhanHangAsync_GiaoHangDangShipping_ChuaTheHoanTat()
    {
        using var db = GetInMemoryDbContext("OrderTest_GiaoHangShippingCheck");
        var fakeCart = new FakeCartService();
        var service = new OrderService(db, fakeCart);

        var don = new DonHang
        {
            MaDonHang = 501,
            MaNguoiDung = 3,
            MaCuaHang = 1,
            MaDon = "DH501",
            HinhThucNhan = "GIAO_HANG",
            TrangThai = "SHIPPING",
            PhuongThucThanhToan = "COD"
        };
        db.DonHangs.Add(don);
        await db.SaveChangesAsync();

        var (ok, msg) = await service.XacNhanDaNhanHangAsync(3, 501);
        Assert.False(ok);
        Assert.Contains("giao đến bạn", msg);
    }

    [Fact]
    public async Task XacNhanDaNhanHangAsync_GapTrucTiepDangShipping_ChoPhepHoanTat()
    {
        using var db = GetInMemoryDbContext("OrderTest_GapTrucTiepShippingCheck");
        var fakeCart = new FakeCartService();
        var service = new OrderService(db, fakeCart);

        var don = new DonHang
        {
            MaDonHang = 502,
            MaNguoiDung = 3,
            MaCuaHang = 1,
            MaDon = "DH502",
            HinhThucNhan = "GAP_TRUC_TIEP",
            TrangThai = "SHIPPING",
            PhuongThucThanhToan = "COD"
        };
        db.DonHangs.Add(don);
        await db.SaveChangesAsync();

        var (ok, msg) = await service.XacNhanDaNhanHangAsync(3, 502);
        Assert.True(ok);
        Assert.Contains("hoàn tất đơn thành công", msg);

        var donDb = await db.DonHangs.FindAsync(502);
        Assert.Equal("COMPLETED", donDb!.TrangThai);
    }

    [Fact]
    public async Task LayChiTietDonMuaAsync_GapTrucTiep_MapDungHinhThucNhanVaCoTheXacNhanNhanHang()
    {
        using var db = GetInMemoryDbContext("OrderTest_LayChiTietGapTrucTiep");
        var fakeCart = new FakeCartService();
        var service = new OrderService(db, fakeCart);

        var shop = new CuaHang { MaCuaHang = 10, MaNguoiDung = 99, TenShop = "Shop Test Direct" };
        db.CuaHangs.Add(shop);

        var don = new DonHang
        {
            MaDonHang = 503,
            MaNguoiDung = 5,
            MaCuaHang = 10,
            MaDon = "DH503",
            HinhThucNhan = "GAP_TRUC_TIEP",
            TrangThai = "SHIPPING",
            PhuongThucThanhToan = "COD",
            TongTien = 150000,
            CuaHang = shop
        };
        db.DonHangs.Add(don);
        await db.SaveChangesAsync();

        var detail = await service.LayChiTietDonMuaAsync(5, 503);
        Assert.NotNull(detail);
        Assert.Equal("GAP_TRUC_TIEP", detail!.HinhThucNhan);
        Assert.True(detail.CoTheXacNhanNhanHang);
    }
}
