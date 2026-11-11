using Microsoft.EntityFrameworkCore;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Models.Entities;
using SecondHandMarketplace.Web.Services.Reviews;
using Xunit;

namespace SecondHandMarketplace.Tests;

/// <summary>
/// Unit test cho ReviewService (C2).
/// Chạy trên EF Core InMemory nên sẽ kích hoạt fallback logic (không gọi sp_TaoDanhGia thật).
/// Logic trong fallback được viết tương đương với sp_TaoDanhGia (kiểm tra COMPLETED, đúng người mua, chưa đánh giá).
///
/// Phạm vi: TaoDanhGiaAsync, LayDanhGiaTheoShopAsync, DoiTrangThaiHienThiAsync, KiemTraDonDaDanhGiaAsync, LayDiemCuaHangAsync.
/// </summary>
public class ReviewServiceTests
{
    private static AppDbContext GetDb(string name)
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(name)
            .Options;
        return new AppDbContext(opts);
    }

    // ─── Seed helpers ────────────────────────────────────────
    private static (NguoiDung buyer, NguoiDung seller, CuaHang shop, DonHang don)
        SeedCoSo(AppDbContext db, string donTrangThai = "COMPLETED")
    {
        var buyer = new NguoiDung { MaNguoiDung = 1, HoTen = "Nguoi Mua", Email = "buyer@test.local", MatKhauBam = "x" };
        var seller = new NguoiDung { MaNguoiDung = 2, HoTen = "Nguoi Ban", Email = "seller@test.local", MatKhauBam = "x" };
        var shop = new CuaHang { MaCuaHang = 1, MaNguoiDung = 2, TenShop = "Shop Test", SoDienThoaiLienHe = "099", TrangThai = "ACTIVE" };
        var don = new DonHang
        {
            MaDonHang = 1, MaDon = "DH001", MaNguoiDung = 1, MaCuaHang = 1,
            TenNguoiNhan = "Nguoi Mua", SoDienThoaiNhan = "099", DiaChiNhan = "123",
            TrangThai = donTrangThai, NgayTao = DateTime.UtcNow, NgayCapNhat = DateTime.UtcNow
        };
        db.NguoiDungs.AddRange(buyer, seller);
        db.CuaHangs.Add(shop);
        db.DonHangs.Add(don);
        db.SaveChanges();
        return (buyer, seller, shop, don);
    }

    // =========================================================
    // TaoDanhGiaAsync — nghiệp vụ tạo đánh giá (InMemory fallback)
    // =========================================================

    [Fact]
    public async Task TaoDanhGia_DonHoanTat_DungNguoiMua_TraThanhCong()
    {
        using var db = GetDb("review_tao_ok");
        SeedCoSo(db, "COMPLETED");
        var svc = new ReviewService(db);

        var (ok, err, id) = await svc.TaoDanhGiaAsync(1, 1, 5, "Rất tốt!");

        Assert.True(ok);
        Assert.Null(err);
        Assert.NotNull(id);
        Assert.True(id > 0);
        Assert.True(await db.DanhGias.AnyAsync(d => d.MaDonHang == 1 && d.SoSao == 5));
    }

    [Fact]
    public async Task TaoDanhGia_DonChuaHoanTat_TraLoi()
    {
        using var db = GetDb("review_don_chua_hoan_tat");
        SeedCoSo(db, "SHIPPING");   // chưa COMPLETED
        var svc = new ReviewService(db);

        var (ok, err, id) = await svc.TaoDanhGiaAsync(1, 1, 4, null);

        Assert.False(ok);
        Assert.NotNull(err);
        Assert.Null(id);
        Assert.Contains("hoàn tất", err, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TaoDanhGia_KhongPhaiNguoiMua_TraLoi()
    {
        using var db = GetDb("review_nguoi_khac");
        SeedCoSo(db, "COMPLETED");
        var svc = new ReviewService(db);

        // maNguoiMua = 99 không phải người mua của đơn (đơn thuộc người dùng 1)
        var (ok, err, id) = await svc.TaoDanhGiaAsync(1, 99, 3, null);

        Assert.False(ok);
        Assert.NotNull(err);
        Assert.Null(id);
        Assert.Contains("người mua", err, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TaoDanhGia_DaDanhGiaRoi_KhongChoPhepLanHai()
    {
        using var db = GetDb("review_lan_hai");
        SeedCoSo(db, "COMPLETED");
        var svc = new ReviewService(db);

        // Lần đầu
        var (ok1, _, _) = await svc.TaoDanhGiaAsync(1, 1, 5, "Tốt");
        Assert.True(ok1);

        // Lần hai — phải bị từ chối
        var (ok2, err2, _) = await svc.TaoDanhGiaAsync(1, 1, 3, "Lại đánh giá");
        Assert.False(ok2);
        Assert.Contains("đã được đánh giá", err2, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TaoDanhGia_SoSaoNgoaiKhoang_TraLoi()
    {
        using var db = GetDb("review_sao_sai");
        SeedCoSo(db, "COMPLETED");
        var svc = new ReviewService(db);

        var (ok, err, id) = await svc.TaoDanhGiaAsync(1, 1, 0, null);

        Assert.False(ok);
        Assert.Null(id);
        Assert.Contains("1 đến 5", err, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TaoDanhGia_NoiDungKhongCo_LuuNullThanhCong()
    {
        using var db = GetDb("review_noi_dung_null");
        SeedCoSo(db, "COMPLETED");
        var svc = new ReviewService(db);

        var (ok, err, id) = await svc.TaoDanhGiaAsync(1, 1, 4, "   ");  // khoảng trắng → chuẩn hóa thành null

        Assert.True(ok);
        Assert.Null(err);
        var dg = await db.DanhGias.FirstAsync(d => d.MaDonHang == 1);
        Assert.Null(dg.NoiDung);
    }

    // =========================================================
    // KiemTraDonDaDanhGiaAsync
    // =========================================================

    [Fact]
    public async Task KiemTra_DonChuaDanhGia_TraFalse()
    {
        using var db = GetDb("review_chua_dg");
        SeedCoSo(db, "COMPLETED");
        var svc = new ReviewService(db);

        Assert.False(await svc.KiemTraDonDaDanhGiaAsync(1));
    }

    [Fact]
    public async Task KiemTra_DonDaDanhGia_TraTrue()
    {
        using var db = GetDb("review_da_dg");
        SeedCoSo(db, "COMPLETED");
        var svc = new ReviewService(db);

        await svc.TaoDanhGiaAsync(1, 1, 5, null);

        Assert.True(await svc.KiemTraDonDaDanhGiaAsync(1));
    }

    // =========================================================
    // DoiTrangThaiHienThiAsync — admin ẩn/hiện đánh giá
    // =========================================================

    [Fact]
    public async Task DoiTrangThai_AnDanhGia_CapNhatThanhCong()
    {
        using var db = GetDb("review_an");
        SeedCoSo(db, "COMPLETED");
        var svc = new ReviewService(db);
        await svc.TaoDanhGiaAsync(1, 1, 5, "OK");

        var danhGia = await db.DanhGias.FirstAsync();
        var (ok, err) = await svc.DoiTrangThaiHienThiAsync(danhGia.MaDanhGia, "HIDDEN", adminId: 1);

        Assert.True(ok);
        Assert.Null(err);
        var updated = await db.DanhGias.FindAsync(danhGia.MaDanhGia);
        Assert.Equal("HIDDEN", updated!.TrangThai);
    }

    [Fact]
    public async Task DoiTrangThai_TrangThaiKhongHopLe_TraLoi()
    {
        using var db = GetDb("review_trang_thai_sai");
        SeedCoSo(db, "COMPLETED");
        var svc = new ReviewService(db);
        await svc.TaoDanhGiaAsync(1, 1, 5, null);

        var danhGia = await db.DanhGias.FirstAsync();
        var (ok, err) = await svc.DoiTrangThaiHienThiAsync(danhGia.MaDanhGia, "DELETED", adminId: 1);

        Assert.False(ok);
        Assert.NotNull(err);
    }

    [Fact]
    public async Task DoiTrangThai_KhongTimThayDanhGia_TraLoi()
    {
        using var db = GetDb("review_khong_tim_thay");
        var svc = new ReviewService(db);

        var (ok, err) = await svc.DoiTrangThaiHienThiAsync(999, "HIDDEN", adminId: 1);

        Assert.False(ok);
        Assert.Contains("Không tìm thấy", err, StringComparison.OrdinalIgnoreCase);
    }

    // =========================================================
    // LayDanhGiaTheoShopAsync — phân trang và bảo vệ riêng tư
    // =========================================================

    [Fact]
    public async Task LayDanhGia_ChiLayVisible_KhongTraHiddenVeMacDinh()
    {
        using var db = GetDb("review_phan_trang_visible");
        SeedCoSo(db, "COMPLETED");
        var svc = new ReviewService(db);

        // Tạo đánh giá VISIBLE
        await svc.TaoDanhGiaAsync(1, 1, 5, "Tốt");
        var dg = await db.DanhGias.FirstAsync();

        // Ẩn đánh giá
        await svc.DoiTrangThaiHienThiAsync(dg.MaDanhGia, "HIDDEN", adminId: 1);

        // Mặc định chỉ lấy VISIBLE → không có kết quả
        var result = await svc.LayDanhGiaTheoShopAsync(1, chiLayTatCa: false);
        Assert.Equal(0, result.TotalItems);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task LayDanhGia_AdminLayTatCa_TraCaHidden()
    {
        using var db = GetDb("review_admin_all");
        SeedCoSo(db, "COMPLETED");
        var svc = new ReviewService(db);

        await svc.TaoDanhGiaAsync(1, 1, 3, "Trung bình");
        var dg = await db.DanhGias.FirstAsync();
        await svc.DoiTrangThaiHienThiAsync(dg.MaDanhGia, "HIDDEN", adminId: 1);

        var result = await svc.LayDanhGiaTheoShopAsync(1, chiLayTatCa: true);
        Assert.Equal(1, result.TotalItems);
        Assert.Single(result.Items);
        Assert.Equal("HIDDEN", result.Items[0].TrangThai);
    }

    [Fact]
    public async Task LayDanhGia_KhongLoEmail_KhongLoSoDienThoai()
    {
        using var db = GetDb("review_privacy");
        SeedCoSo(db, "COMPLETED");
        var svc = new ReviewService(db);
        await svc.TaoDanhGiaAsync(1, 1, 4, "OK");

        var result = await svc.LayDanhGiaTheoShopAsync(1);

        Assert.Single(result.Items);
        // DanhGiaItemVm không có thuộc tính Email hay SoDienThoai
        var item = result.Items[0];
        Assert.Equal(1, item.MaNguoiMua);
        Assert.False(string.IsNullOrEmpty(item.TenNguoiMua)); // chỉ tên, không lộ email
    }

    // =========================================================
    // LayDiemCuaHangAsync — đọc điểm trung bình
    // =========================================================

    [Fact]
    public async Task LayDiem_ChuaCoDanhGia_TraZero()
    {
        using var db = GetDb("review_diem_zero");
        SeedCoSo(db, "COMPLETED");
        var svc = new ReviewService(db);

        var diem = await svc.LayDiemCuaHangAsync(1);

        // Shop tồn tại nhưng chưa có đánh giá nào
        Assert.NotNull(diem);
        Assert.Equal(0m, diem.DiemTrungBinh);
        Assert.Equal(0, diem.SoDanhGia);
    }

    [Fact]
    public async Task LayDiem_CoDanhGia_TinhDungDiemTrungBinh()
    {
        using var db = GetDb("review_diem_avg");
        SeedCoSo(db, "COMPLETED");

        // Thêm đơn thứ 2 để tạo đánh giá thứ 2
        db.DonHangs.Add(new DonHang
        {
            MaDonHang = 2, MaDon = "DH002", MaNguoiDung = 1, MaCuaHang = 1,
            TenNguoiNhan = "Nguoi Mua", SoDienThoaiNhan = "099", DiaChiNhan = "123",
            TrangThai = "COMPLETED", NgayTao = DateTime.UtcNow, NgayCapNhat = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var svc = new ReviewService(db);
        await svc.TaoDanhGiaAsync(1, 1, 5, null);  // 5 sao
        await svc.TaoDanhGiaAsync(2, 1, 3, null);  // 3 sao

        var diem = await svc.LayDiemCuaHangAsync(1);

        Assert.NotNull(diem);
        Assert.Equal(2, diem.SoDanhGia);
        Assert.Equal(4.00m, diem.DiemTrungBinh); // (5+3)/2 = 4.00
    }

    [Fact]
    public async Task LayDiem_DanhGiaHidden_KhongTinhVaoDiemTrungBinh()
    {
        using var db = GetDb("review_diem_hidden");
        SeedCoSo(db, "COMPLETED");
        var svc = new ReviewService(db);
        await svc.TaoDanhGiaAsync(1, 1, 1, null);  // 1 sao

        var dg = await db.DanhGias.FirstAsync();
        await svc.DoiTrangThaiHienThiAsync(dg.MaDanhGia, "HIDDEN", adminId: 1);

        var diem = await svc.LayDiemCuaHangAsync(1);

        // Không có đánh giá VISIBLE → điểm = 0
        Assert.NotNull(diem);
        Assert.Equal(0m, diem.DiemTrungBinh);
        Assert.Equal(0, diem.SoDanhGia);
    }
}
