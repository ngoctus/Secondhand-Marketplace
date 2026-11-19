using Microsoft.EntityFrameworkCore;
using SecondHandMarketplace.Web.Common;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Models.Entities;
using SecondHandMarketplace.Web.Services.Shops;

namespace SecondHandMarketplace.Tests;

/// <summary>
/// EF InMemory không kiểm tra FK/CHECK và không chạy được stored procedure, nên các luật địa chỉ/ngân hàng
/// phải nằm trong ShopService mới test được. Mọi ca ở đây dùng shop ĐÃ CÓ (nhánh cập nhật).
/// </summary>
public class ShopServiceTests
{
    private const int UserId = 7;

    private static AppDbContext NewDb(string name)
    {
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(name).Options);
        db.TinhThanhs.AddRange(
            new TinhThanh { MaTinhThanh = 79, TenTinhThanh = "Thành phố Hồ Chí Minh", MaHanhChinh = "79" },
            new TinhThanh { MaTinhThanh = 1, TenTinhThanh = "Thành phố Hà Nội", MaHanhChinh = "01" });
        db.PhuongXas.AddRange(
            new PhuongXa { MaPhuongXa = 26734, MaTinhThanh = 79, TenPhuongXa = "Phường Sài Gòn", MaHanhChinh = "26734" },
            new PhuongXa { MaPhuongXa = 4, MaTinhThanh = 1, TenPhuongXa = "Phường Ba Đình", MaHanhChinh = "00004" });
        // Shop cũ: các cột địa chỉ/ngân hàng đều NULL.
        db.CuaHangs.Add(new CuaHang { MaCuaHang = 1, MaNguoiDung = UserId, TenShop = "Shop cũ", SoDienThoaiLienHe = "0900000001", TrangThai = "ACTIVE" });
        db.SaveChanges();
        return db;
    }

    private static ShopSaveInput Input(int? tinh = 79, int? xa = 26734, string? diaChi = "12 Lê Lợi",
        string? bin = null, string? stk = null, string? chuTk = null) =>
        new("Shop mới tên", "0900000009", "Mô tả", null, tinh, xa, diaChi, "Gần chợ", bin, stk, chuTk);

    private static ShopService NewService(AppDbContext db, FakeAuditLogService? audit = null) => new(db, audit ?? new FakeAuditLogService());

    [Fact]
    public async Task SaveAsync_ThieuTinh_BiTuChoi()
    {
        using var db = NewDb(nameof(SaveAsync_ThieuTinh_BiTuChoi));
        var (ok, err) = await NewService(db).SaveAsync(UserId, Input(tinh: null));

        Assert.False(ok);
        Assert.Contains("tỉnh", err);
        Assert.Null((await db.CuaHangs.SingleAsync()).MaTinhThanh);
    }

    [Fact]
    public async Task SaveAsync_ThieuXa_BiTuChoi()
    {
        using var db = NewDb(nameof(SaveAsync_ThieuXa_BiTuChoi));
        var (ok, err) = await NewService(db).SaveAsync(UserId, Input(xa: null));

        Assert.False(ok);
        Assert.Contains("phường/xã", err);
    }

    [Fact]
    public async Task SaveAsync_ThieuDiaChiChiTiet_BiTuChoi()
    {
        using var db = NewDb(nameof(SaveAsync_ThieuDiaChiChiTiet_BiTuChoi));
        var (ok, err) = await NewService(db).SaveAsync(UserId, Input(diaChi: "   "));

        Assert.False(ok);
        Assert.Contains("địa chỉ chi tiết", err);
    }

    [Fact]
    public async Task SaveAsync_XaKhongThuocTinh_BiTuChoi()
    {
        using var db = NewDb(nameof(SaveAsync_XaKhongThuocTinh_BiTuChoi));
        // Phường Ba Đình (Hà Nội) nhưng chọn TP.HCM.
        var (ok, err) = await NewService(db).SaveAsync(UserId, Input(tinh: 79, xa: 4));

        Assert.False(ok);
        Assert.Contains("không thuộc", err);
        Assert.Null((await db.CuaHangs.SingleAsync()).MaPhuongXa);
    }

    [Fact]
    public async Task SaveAsync_TinhKhongTonTai_BiTuChoi()
    {
        using var db = NewDb(nameof(SaveAsync_TinhKhongTonTai_BiTuChoi));
        var (ok, _) = await NewService(db).SaveAsync(UserId, Input(tinh: 9999, xa: 26734));

        Assert.False(ok);
    }

    [Fact]
    public async Task SaveAsync_ShopCuThieuDiaChi_BoSungDayDuThiLuuDuocVaGhiNhatKy()
    {
        using var db = NewDb(nameof(SaveAsync_ShopCuThieuDiaChi_BoSungDayDuThiLuuDuocVaGhiNhatKy));
        var audit = new FakeAuditLogService();
        var (ok, err) = await NewService(db, audit).SaveAsync(UserId, Input());

        Assert.True(ok, err);
        var shop = await db.CuaHangs.SingleAsync();
        Assert.Equal(79, shop.MaTinhThanh);
        Assert.Equal(26734, shop.MaPhuongXa);
        Assert.Equal("12 Lê Lợi", shop.DiaChiChiTiet);
        Assert.Equal("Gần chợ", shop.KhuVucHienThi);
        Assert.Null(shop.MaNganHang);   // ngân hàng là tùy chọn
        Assert.Contains(audit.Logs, l => l.Action == "UPDATE_SHOP");
    }

    [Fact]
    public async Task SaveAsync_NganHangThieuSoTaiKhoan_BiTuChoi()
    {
        using var db = NewDb(nameof(SaveAsync_NganHangThieuSoTaiKhoan_BiTuChoi));
        var (ok, err) = await NewService(db).SaveAsync(UserId, Input(bin: "970436", chuTk: "NGUYEN VAN A"));

        Assert.False(ok);
        Assert.Contains("số tài khoản", err);
    }

    [Fact]
    public async Task SaveAsync_ChiNhapSoTaiKhoanKhongChonNganHang_BiTuChoi()
    {
        using var db = NewDb(nameof(SaveAsync_ChiNhapSoTaiKhoanKhongChonNganHang_BiTuChoi));
        var (ok, err) = await NewService(db).SaveAsync(UserId, Input(stk: "0123456789", chuTk: "NGUYEN VAN A"));

        Assert.False(ok);
        Assert.Contains("ngân hàng", err);
    }

    [Fact]
    public async Task SaveAsync_SoTaiKhoanCoChu_BiTuChoi()
    {
        using var db = NewDb(nameof(SaveAsync_SoTaiKhoanCoChu_BiTuChoi));
        var (ok, err) = await NewService(db).SaveAsync(UserId, Input(bin: "970436", stk: "01234abc", chuTk: "NGUYEN VAN A"));

        Assert.False(ok);
        Assert.Contains("chữ số", err);
    }

    [Fact]
    public async Task SaveAsync_BinNganHangLa_BiTuChoi()
    {
        using var db = NewDb(nameof(SaveAsync_BinNganHangLa_BiTuChoi));
        var (ok, _) = await NewService(db).SaveAsync(UserId, Input(bin: "123456", stk: "0123456789", chuTk: "NGUYEN VAN A"));

        Assert.False(ok);
    }

    [Fact]
    public async Task SaveAsync_NganHangDayDu_LuuTenNganHangTheoBin()
    {
        using var db = NewDb(nameof(SaveAsync_NganHangDayDu_LuuTenNganHangTheoBin));
        var (ok, err) = await NewService(db).SaveAsync(UserId, Input(bin: "970436", stk: "0123456789", chuTk: "NGUYEN VAN A"));

        Assert.True(ok, err);
        var shop = await db.CuaHangs.SingleAsync();
        Assert.Equal("970436", shop.MaNganHang);
        Assert.Equal("Vietcombank", shop.NganHang);
        Assert.Equal("0123456789", shop.SoTaiKhoan);
        Assert.Equal("NGUYEN VAN A", shop.TenChuTaiKhoan);
    }

    [Fact]
    public async Task SaveAsync_ShopDaCo_CapNhatChinhShopDoKhongTaoShopThuHai()
    {
        using var db = NewDb(nameof(SaveAsync_ShopDaCo_CapNhatChinhShopDoKhongTaoShopThuHai));
        var (ok, _) = await NewService(db).SaveAsync(UserId, Input());

        Assert.True(ok);
        var shop = await db.CuaHangs.SingleAsync();   // vẫn đúng một shop
        Assert.Equal(1, shop.MaCuaHang);
        Assert.Equal("Shop mới tên", shop.TenShop);
    }

    [Fact]
    public async Task SaveAsync_ShopBiKhoa_KhongChoSua()
    {
        using var db = NewDb(nameof(SaveAsync_ShopBiKhoa_KhongChoSua));
        (await db.CuaHangs.SingleAsync()).TrangThai = "LOCKED";
        await db.SaveChangesAsync();

        var (ok, err) = await NewService(db).SaveAsync(UserId, Input());

        Assert.False(ok);
        Assert.Contains("khóa", err);
    }

    [Fact]
    public void DanhSachNganHang_BinHopLeVaKhongTrung()
    {
        Assert.All(VietQrBanks.All, b => Assert.Matches("^[0-9]{6}$", b.Bin));
        Assert.Equal(VietQrBanks.All.Count, VietQrBanks.All.Select(b => b.Bin).Distinct().Count());
        Assert.Equal("970415", VietQrBanks.TimTheoBin("970415")!.Bin);   // VietinBank (UTC2), khớp VietQrViewModel mặc định
        Assert.Equal("970422", VietQrBanks.TimTheoBin("970422")!.Bin);   // MB Bank
        Assert.Null(VietQrBanks.TimTheoBin("000000"));
    }
}
