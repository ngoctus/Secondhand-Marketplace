using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Models.Entities;
using SecondHandMarketplace.Web.Services.Orders;
using Xunit;

namespace SecondHandMarketplace.Tests;

public class OrderCleanupWorkerTests
{
    private (AppDbContext db, OrderCleanupBackgroundService worker) CreateWorker(string dbName)
    {
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(opt => opt.UseInMemoryDatabase(dbName));
        var sp = services.BuildServiceProvider();

        var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
        var db = sp.GetRequiredService<AppDbContext>();

        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var logger = NullLogger<OrderCleanupBackgroundService>.Instance;

        var worker = new OrderCleanupBackgroundService(scopeFactory, config, logger);
        return (db, worker);
    }

    [Fact]
    public async Task QuetDonHang_HuyDonVietQrQuaHan24h_TraSanPhamVeActiveVaMoGiuCho()
    {
        var (db, worker) = CreateWorker("Cleanup_HuyDonVietQr");

        var buyer = new NguoiDung { MaNguoiDung = 1, HoTen = "Buyer", SoDienThoai = "0900000001" };
        var seller = new NguoiDung { MaNguoiDung = 2, HoTen = "Seller", SoDienThoai = "0900000002" };
        var shop = new CuaHang { MaCuaHang = 10, MaNguoiDung = 2, TenShop = "Shop 10", TrangThai = "ACTIVE" };
        var sp = new SanPham { MaSanPham = 100, Ten = "Vot tennis cu", TrangThai = "RESERVED", Gia = 500000 };

        var don = new DonHang
        {
            MaDonHang = 50,
            MaDon = "ORD-CLEAN-01",
            MaNguoiDung = 1,
            MaCuaHang = 10,
            TrangThai = "PENDING",
            TrangThaiThanhToan = "UNPAID",
            PhuongThucThanhToan = "BANK_TRANSFER",
            TongTien = 500000,
            NgayTao = DateTime.UtcNow.AddHours(-26),
            NgayCapNhat = DateTime.UtcNow.AddHours(-26),
            CuaHang = shop,
            NguoiDung = buyer
        };

        var chiTiet = new ChiTietDonHang
        {
            MaChiTietDonHang = 501,
            MaDonHang = 50,
            MaSanPham = 100,
            TenSanPham = "Vot tennis cu",
            Gia = 500000,
            DangGiuCho = true,
            DonHang = don,
            SanPham = sp
        };

        db.NguoiDungs.AddRange(buyer, seller);
        db.CuaHangs.Add(shop);
        db.SanPhams.Add(sp);
        db.DonHangs.Add(don);
        db.ChiTietDonHangs.Add(chiTiet);
        await db.SaveChangesAsync();

        await worker.QuetDonHangAsync(CancellationToken.None);

        var donSauQuet = await db.DonHangs.AsNoTracking().FirstOrDefaultAsync(d => d.MaDonHang == 50);
        Assert.NotNull(donSauQuet);
        Assert.Equal("CANCELLED", donSauQuet!.TrangThai);
        Assert.Equal("SYSTEM", donSauQuet.NguoiHuy);
        Assert.NotNull(donSauQuet.ThoiGianHuy);

        var ctSauQuet = await db.ChiTietDonHangs.AsNoTracking().FirstOrDefaultAsync(c => c.MaChiTietDonHang == 501);
        Assert.False(ctSauQuet!.DangGiuCho);

        var spSauQuet = await db.SanPhams.AsNoTracking().FirstOrDefaultAsync(s => s.MaSanPham == 100);
        Assert.Equal("ACTIVE", spSauQuet!.TrangThai);

        var lichSuSp = await db.LichSuTrangThaiSanPhams.FirstOrDefaultAsync(l => l.MaSanPham == 100);
        Assert.NotNull(lichSuSp);
        Assert.Equal("ACTIVE", lichSuSp!.TrangThaiMoi);

        var thongBaoBuyer = await db.ThongBaos.FirstOrDefaultAsync(t => t.MaNguoiDung == 1 && t.MaThamChieu == 50);
        Assert.NotNull(thongBaoBuyer);
    }

    [Fact]
    public async Task QuetDonHang_KhongHuyDonChuaQuaHan24h()
    {
        var (db, worker) = CreateWorker("Cleanup_KhongHuyDonMoi");

        var buyer = new NguoiDung { MaNguoiDung = 3, HoTen = "Buyer 3", SoDienThoai = "0900000003" };
        var seller = new NguoiDung { MaNguoiDung = 4, HoTen = "Seller 4", SoDienThoai = "0900000004" };
        var shop = new CuaHang { MaCuaHang = 11, MaNguoiDung = 4, TenShop = "Shop 11", TrangThai = "ACTIVE" };
        var sp = new SanPham { MaSanPham = 101, Ten = "Ban phim cu", TrangThai = "RESERVED", Gia = 300000 };

        var don = new DonHang
        {
            MaDonHang = 51,
            MaDon = "ORD-CLEAN-02",
            MaNguoiDung = 3,
            MaCuaHang = 11,
            TrangThai = "PENDING",
            TrangThaiThanhToan = "UNPAID",
            PhuongThucThanhToan = "BANK_TRANSFER",
            TongTien = 300000,
            NgayTao = DateTime.UtcNow.AddHours(-10), // Chưa đủ 24h
            NgayCapNhat = DateTime.UtcNow.AddHours(-10),
            CuaHang = shop,
            NguoiDung = buyer
        };

        var chiTiet = new ChiTietDonHang
        {
            MaChiTietDonHang = 502,
            MaDonHang = 51,
            MaSanPham = 101,
            TenSanPham = "Ban phim cu",
            Gia = 300000,
            DangGiuCho = true,
            DonHang = don,
            SanPham = sp
        };

        db.NguoiDungs.AddRange(buyer, seller);
        db.CuaHangs.Add(shop);
        db.SanPhams.Add(sp);
        db.DonHangs.Add(don);
        db.ChiTietDonHangs.Add(chiTiet);
        await db.SaveChangesAsync();

        await worker.QuetDonHangAsync(CancellationToken.None);

        var donSauQuet = await db.DonHangs.FindAsync(51);
        Assert.Equal("PENDING", donSauQuet!.TrangThai);

        var spSauQuet = await db.SanPhams.FindAsync(101);
        Assert.Equal("RESERVED", spSauQuet!.TrangThai);
    }

    [Fact]
    public async Task QuetDonHang_HoanTatDonDeliveredQua5Ngay_KhongCoTranhChap()
    {
        var (db, worker) = CreateWorker("Cleanup_HoanTatDelivered");

        var buyer = new NguoiDung { MaNguoiDung = 5, HoTen = "Buyer 5", SoDienThoai = "0900000005" };
        var seller = new NguoiDung { MaNguoiDung = 6, HoTen = "Seller 6", SoDienThoai = "0900000006" };
        var shop = new CuaHang { MaCuaHang = 12, MaNguoiDung = 6, TenShop = "Shop 12", TrangThai = "ACTIVE" };
        var sp = new SanPham { MaSanPham = 102, Ten = "Ao khoac cu", TrangThai = "RESERVED", Gia = 150000 };

        var don = new DonHang
        {
            MaDonHang = 52,
            MaDon = "ORD-CLEAN-03",
            MaNguoiDung = 5,
            MaCuaHang = 12,
            TrangThai = "DELIVERED",
            TrangThaiThanhToan = "UNPAID",
            PhuongThucThanhToan = "COD",
            TongTien = 150000,
            ThoiGianDaGiao = DateTime.UtcNow.AddDays(-6), // Quá 5 ngày
            NgayTao = DateTime.UtcNow.AddDays(-8),
            NgayCapNhat = DateTime.UtcNow.AddDays(-6),
            CuaHang = shop,
            NguoiDung = buyer
        };

        var chiTiet = new ChiTietDonHang
        {
            MaChiTietDonHang = 503,
            MaDonHang = 52,
            MaSanPham = 102,
            TenSanPham = "Ao khoac cu",
            Gia = 150000,
            DangGiuCho = true,
            DonHang = don,
            SanPham = sp
        };

        db.NguoiDungs.AddRange(buyer, seller);
        db.CuaHangs.Add(shop);
        db.SanPhams.Add(sp);
        db.DonHangs.Add(don);
        db.ChiTietDonHangs.Add(chiTiet);
        await db.SaveChangesAsync();

        await worker.QuetDonHangAsync(CancellationToken.None);

        var donSauQuet = await db.DonHangs.AsNoTracking().FirstOrDefaultAsync(d => d.MaDonHang == 52);
        Assert.Equal("COMPLETED", donSauQuet!.TrangThai);
        Assert.Equal("PAID", donSauQuet.TrangThaiThanhToan);
        Assert.NotNull(donSauQuet.ThoiGianHoanTat);

        var spSauQuet = await db.SanPhams.AsNoTracking().FirstOrDefaultAsync(s => s.MaSanPham == 102);
        Assert.Equal("SOLD", spSauQuet!.TrangThai);
    }

    [Fact]
    public async Task QuetDonHang_KhongHoanTatDonDeliveredNeuDangCoTranhChap()
    {
        var (db, worker) = CreateWorker("Cleanup_KhongHoanTatKhiTranhChap");

        var buyer = new NguoiDung { MaNguoiDung = 7, HoTen = "Buyer 7", SoDienThoai = "0900000007" };
        var seller = new NguoiDung { MaNguoiDung = 8, HoTen = "Seller 8", SoDienThoai = "0900000008" };
        var shop = new CuaHang { MaCuaHang = 13, MaNguoiDung = 8, TenShop = "Shop 13", TrangThai = "ACTIVE" };
        var sp = new SanPham { MaSanPham = 103, Ten = "Dong ho cu", TrangThai = "RESERVED", Gia = 800000 };

        var don = new DonHang
        {
            MaDonHang = 53,
            MaDon = "ORD-CLEAN-04",
            MaNguoiDung = 7,
            MaCuaHang = 13,
            TrangThai = "DELIVERED",
            TrangThaiThanhToan = "PAID",
            PhuongThucThanhToan = "BANK_TRANSFER",
            TongTien = 800000,
            ThoiGianDaGiao = DateTime.UtcNow.AddDays(-7),
            NgayTao = DateTime.UtcNow.AddDays(-10),
            NgayCapNhat = DateTime.UtcNow.AddDays(-7),
            CuaHang = shop,
            NguoiDung = buyer
        };

        var tranhChap = new TranhChap
        {
            MaTranhChap = 1,
            MaDonHang = 53,
            MaNguoiTao = 7,
            LyDo = "Hàng bị vỡ mặt kính",
            TrangThai = "OPEN", // Đang mở khiếu nại
            NgayTao = DateTime.UtcNow.AddDays(-2),
            NgayCapNhat = DateTime.UtcNow.AddDays(-2)
        };

        db.NguoiDungs.AddRange(buyer, seller);
        db.CuaHangs.Add(shop);
        db.SanPhams.Add(sp);
        db.DonHangs.Add(don);
        db.TranhChaps.Add(tranhChap);
        await db.SaveChangesAsync();

        await worker.QuetDonHangAsync(CancellationToken.None);

        var donSauQuet = await db.DonHangs.FindAsync(53);
        // Không được hoàn tất vì đang có tranh chấp
        Assert.Equal("DELIVERED", donSauQuet!.TrangThai);
    }
}
