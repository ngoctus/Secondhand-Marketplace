using Microsoft.EntityFrameworkCore;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Models.Entities;
using SecondHandMarketplace.Web.Models.ViewModels.Reviews;
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

    // =========================================================
    // C3: Giao diện người mua — LayDanhGiaCuaNguoiMua & LayDanhGiaTheoDonHang
    // =========================================================

    [Fact]
    public async Task LayDanhGiaCuaNguoiMua_ChiTraDanhGiaCuaMinh()
    {
        using var db = GetDb("review_c3_nguoi_mua");
        SeedCoSo(db, "COMPLETED");

        // Tạo thêm user 2 mua đơn 2
        var buyer2 = new NguoiDung { MaNguoiDung = 3, HoTen = "User 2", Email = "u2@test.local", MatKhauBam = "x" };
        var don2 = new DonHang
        {
            MaDonHang = 2, MaDon = "DH002", MaNguoiDung = 3, MaCuaHang = 1,
            TenNguoiNhan = "User 2", SoDienThoaiNhan = "088", DiaChiNhan = "XYZ",
            TrangThai = "COMPLETED", NgayTao = DateTime.UtcNow, NgayCapNhat = DateTime.UtcNow
        };
        db.NguoiDungs.Add(buyer2);
        db.DonHangs.Add(don2);
        await db.SaveChangesAsync();

        var svc = new ReviewService(db);
        await svc.TaoDanhGiaAsync(1, 1, 5, "Của user 1");
        await svc.TaoDanhGiaAsync(2, 3, 4, "Của user 2");

        var resultUser1 = await svc.LayDanhGiaCuaNguoiMuaAsync(1, page: 1, pageSize: 10);
        var resultUser2 = await svc.LayDanhGiaCuaNguoiMuaAsync(3, page: 1, pageSize: 10);

        Assert.Equal(1, resultUser1.TotalItems);
        Assert.Equal("Của user 1", resultUser1.Items[0].NoiDung);
        Assert.Equal("DH001", resultUser1.Items[0].MaDon);
        Assert.Equal("Shop Test", resultUser1.Items[0].TenShop);

        Assert.Equal(1, resultUser2.TotalItems);
        Assert.Equal("Của user 2", resultUser2.Items[0].NoiDung);
        Assert.Equal("DH002", resultUser2.Items[0].MaDon);
    }

    [Fact]
    public async Task LayDanhGiaTheoDonHang_DaDanhGia_TraChiTietDanhGia()
    {
        using var db = GetDb("review_c3_theo_don_hang");
        SeedCoSo(db, "COMPLETED");
        var svc = new ReviewService(db);
        await svc.TaoDanhGiaAsync(1, 1, 5, "Tuyệt vời!");

        var dg = await svc.LayDanhGiaTheoDonHangAsync(1);

        Assert.NotNull(dg);
        Assert.Equal(1, dg.MaDonHang);
        Assert.Equal((byte)5, dg.SoSao);
        Assert.Equal("Tuyệt vời!", dg.NoiDung);
    }

    [Fact]
    public async Task LayDanhGiaTheoDonHang_ChuaDanhGia_TraNull()
    {
        using var db = GetDb("review_c3_don_chua_danh_gia");
        SeedCoSo(db, "COMPLETED");
        var svc = new ReviewService(db);

        var dg = await svc.LayDanhGiaTheoDonHangAsync(1);

        Assert.Null(dg);
    }

    // =========================================================
    // C4: Admin rà soát shop bị đánh giá thấp
    // =========================================================

    [Fact]
    public async Task LayDanhSachShopCanRaSoat_TabCanRaSoat_ChiTraShopDuoi3SaoVaDu5DanhGia()
    {
        using var db = GetDb("review_c4_can_ra_soat");
        // Shop 1: Bad Shop (5 đánh giá 1 sao => TB 1.00 < 3.0, SoDanhGia = 5)
        var seller1 = new NguoiDung { MaNguoiDung = 10, HoTen = "Chu Shop 1", Email = "seller1@test.local", MatKhauBam = "x" };
        var shop1 = new CuaHang { MaCuaHang = 10, MaNguoiDung = 10, TenShop = "Shop Diem Thap", SoDienThoaiLienHe = "091", TrangThai = "ACTIVE" };

        // Shop 2: Good Shop (5 đánh giá 5 sao => TB 5.00 >= 3.0, SoDanhGia = 5)
        var seller2 = new NguoiDung { MaNguoiDung = 20, HoTen = "Chu Shop 2", Email = "seller2@test.local", MatKhauBam = "x" };
        var shop2 = new CuaHang { MaCuaHang = 20, MaNguoiDung = 20, TenShop = "Shop Diem Cao", SoDienThoaiLienHe = "092", TrangThai = "ACTIVE" };

        // Shop 3: Few reviews (2 đánh giá 1 sao => TB 1.00 < 3.0 nhưng SoDanhGia = 2 < 5)
        var seller3 = new NguoiDung { MaNguoiDung = 30, HoTen = "Chu Shop 3", Email = "seller3@test.local", MatKhauBam = "x" };
        var shop3 = new CuaHang { MaCuaHang = 30, MaNguoiDung = 30, TenShop = "Shop It Danh Gia", SoDienThoaiLienHe = "093", TrangThai = "ACTIVE" };

        var buyer = new NguoiDung { MaNguoiDung = 1, HoTen = "Nguoi Mua", Email = "buyer@test.local", MatKhauBam = "x" };

        db.NguoiDungs.AddRange(seller1, seller2, seller3, buyer);
        db.CuaHangs.AddRange(shop1, shop2, shop3);

        // Tạo 5 đơn và 5 đánh giá 1 sao cho Shop 1
        for (int i = 1; i <= 5; i++)
        {
            db.DonHangs.Add(new DonHang
            {
                MaDonHang = 100 + i, MaDon = $"DH10{i}", MaNguoiDung = 1, MaCuaHang = 10,
                TenNguoiNhan = "A", SoDienThoaiNhan = "09", DiaChiNhan = "B",
                TrangThai = "COMPLETED", NgayTao = DateTime.UtcNow, NgayCapNhat = DateTime.UtcNow
            });
            db.DanhGias.Add(new DanhGia
            {
                MaDanhGia = 100 + i, MaDonHang = 100 + i, MaNguoiMua = 1, MaCuaHang = 10,
                SoSao = 1, TrangThai = "VISIBLE", NgayTao = DateTime.UtcNow
            });
        }

        // Tạo 5 đơn và 5 đánh giá 5 sao cho Shop 2
        for (int i = 1; i <= 5; i++)
        {
            db.DonHangs.Add(new DonHang
            {
                MaDonHang = 200 + i, MaDon = $"DH20{i}", MaNguoiDung = 1, MaCuaHang = 20,
                TenNguoiNhan = "A", SoDienThoaiNhan = "09", DiaChiNhan = "B",
                TrangThai = "COMPLETED", NgayTao = DateTime.UtcNow, NgayCapNhat = DateTime.UtcNow
            });
            db.DanhGias.Add(new DanhGia
            {
                MaDanhGia = 200 + i, MaDonHang = 200 + i, MaNguoiMua = 1, MaCuaHang = 20,
                SoSao = 5, TrangThai = "VISIBLE", NgayTao = DateTime.UtcNow
            });
        }

        // Tạo 2 đơn và 2 đánh giá 1 sao cho Shop 3
        for (int i = 1; i <= 2; i++)
        {
            db.DonHangs.Add(new DonHang
            {
                MaDonHang = 300 + i, MaDon = $"DH30{i}", MaNguoiDung = 1, MaCuaHang = 30,
                TenNguoiNhan = "A", SoDienThoaiNhan = "09", DiaChiNhan = "B",
                TrangThai = "COMPLETED", NgayTao = DateTime.UtcNow, NgayCapNhat = DateTime.UtcNow
            });
            db.DanhGias.Add(new DanhGia
            {
                MaDanhGia = 300 + i, MaDonHang = 300 + i, MaNguoiMua = 1, MaCuaHang = 30,
                SoSao = 1, TrangThai = "VISIBLE", NgayTao = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync();

        var svc = new ReviewService(db);
        var result = await svc.LayDanhSachShopCanRaSoatAsync(new ShopCanRaSoatFilterVm { Tab = "can_ra_soat" });

        // Chỉ Shop 1 thỏa: điểm 1.00 < 3.0 và số đánh giá 5 >= 5
        Assert.Equal(1, result.TotalItems);
        Assert.Single(result.Items);
        Assert.Equal("Shop Diem Thap", result.Items[0].TenShop);
        Assert.Equal(1.00m, result.Items[0].DiemTrungBinh);
        Assert.Equal(5, result.Items[0].SoDanhGia);
        Assert.True(result.Items[0].LaShopCanRaSoat);
        Assert.Equal(1, result.TongShopCanRaSoat);
    }

    [Fact]
    public async Task LayDanhSachShopCanRaSoat_TabAll_TraTatCaShopCoDanhGia()
    {
        using var db = GetDb("review_c4_tab_all");
        var seller1 = new NguoiDung { MaNguoiDung = 10, HoTen = "Chu Shop 1", Email = "seller1@test.local", MatKhauBam = "x" };
        var shop1 = new CuaHang { MaCuaHang = 10, MaNguoiDung = 10, TenShop = "Shop 1", SoDienThoaiLienHe = "091", TrangThai = "ACTIVE" };
        var seller2 = new NguoiDung { MaNguoiDung = 20, HoTen = "Chu Shop 2", Email = "seller2@test.local", MatKhauBam = "x" };
        var shop2 = new CuaHang { MaCuaHang = 20, MaNguoiDung = 20, TenShop = "Shop 2", SoDienThoaiLienHe = "092", TrangThai = "ACTIVE" };
        var buyer = new NguoiDung { MaNguoiDung = 1, HoTen = "Buyer", Email = "buyer@test.local", MatKhauBam = "x" };

        db.NguoiDungs.AddRange(seller1, seller2, buyer);
        db.CuaHangs.AddRange(shop1, shop2);

        db.DonHangs.Add(new DonHang { MaDonHang = 1, MaDon = "DH1", MaNguoiDung = 1, MaCuaHang = 10, TenNguoiNhan = "A", SoDienThoaiNhan = "09", DiaChiNhan = "B", TrangThai = "COMPLETED", NgayTao = DateTime.UtcNow, NgayCapNhat = DateTime.UtcNow });
        db.DanhGias.Add(new DanhGia { MaDanhGia = 1, MaDonHang = 1, MaNguoiMua = 1, MaCuaHang = 10, SoSao = 2, TrangThai = "VISIBLE", NgayTao = DateTime.UtcNow });

        db.DonHangs.Add(new DonHang { MaDonHang = 2, MaDon = "DH2", MaNguoiDung = 1, MaCuaHang = 20, TenNguoiNhan = "A", SoDienThoaiNhan = "09", DiaChiNhan = "B", TrangThai = "COMPLETED", NgayTao = DateTime.UtcNow, NgayCapNhat = DateTime.UtcNow });
        db.DanhGias.Add(new DanhGia { MaDanhGia = 2, MaDonHang = 2, MaNguoiMua = 1, MaCuaHang = 20, SoSao = 5, TrangThai = "VISIBLE", NgayTao = DateTime.UtcNow });

        await db.SaveChangesAsync();

        var svc = new ReviewService(db);
        var result = await svc.LayDanhSachShopCanRaSoatAsync(new ShopCanRaSoatFilterVm { Tab = "all" });

        Assert.Equal(2, result.TotalItems);
        // Sắp xếp: Shop 1 (2 sao) trước Shop 2 (5 sao)
        Assert.Equal("Shop 1", result.Items[0].TenShop);
        Assert.Equal("Shop 2", result.Items[1].TenShop);
    }

    [Fact]
    public async Task LayDanhSachShopCanRaSoat_Search_LocTheoTenVaEmail()
    {
        using var db = GetDb("review_c4_search");
        var seller1 = new NguoiDung { MaNguoiDung = 10, HoTen = "Nguyen Van A", Email = "nguyenvana@test.local", MatKhauBam = "x" };
        var shop1 = new CuaHang { MaCuaHang = 10, MaNguoiDung = 10, TenShop = "Alpha Store", SoDienThoaiLienHe = "091", TrangThai = "ACTIVE" };
        var seller2 = new NguoiDung { MaNguoiDung = 20, HoTen = "Tran Van B", Email = "tranvanb@test.local", MatKhauBam = "x" };
        var shop2 = new CuaHang { MaCuaHang = 20, MaNguoiDung = 20, TenShop = "Beta Shop", SoDienThoaiLienHe = "092", TrangThai = "ACTIVE" };
        var buyer = new NguoiDung { MaNguoiDung = 1, HoTen = "Buyer", Email = "buyer@test.local", MatKhauBam = "x" };

        db.NguoiDungs.AddRange(seller1, seller2, buyer);
        db.CuaHangs.AddRange(shop1, shop2);

        db.DonHangs.Add(new DonHang { MaDonHang = 1, MaDon = "DH1", MaNguoiDung = 1, MaCuaHang = 10, TenNguoiNhan = "A", SoDienThoaiNhan = "09", DiaChiNhan = "B", TrangThai = "COMPLETED", NgayTao = DateTime.UtcNow, NgayCapNhat = DateTime.UtcNow });
        db.DanhGias.Add(new DanhGia { MaDanhGia = 1, MaDonHang = 1, MaNguoiMua = 1, MaCuaHang = 10, SoSao = 4, TrangThai = "VISIBLE", NgayTao = DateTime.UtcNow });

        db.DonHangs.Add(new DonHang { MaDonHang = 2, MaDon = "DH2", MaNguoiDung = 1, MaCuaHang = 20, TenNguoiNhan = "A", SoDienThoaiNhan = "09", DiaChiNhan = "B", TrangThai = "COMPLETED", NgayTao = DateTime.UtcNow, NgayCapNhat = DateTime.UtcNow });
        db.DanhGias.Add(new DanhGia { MaDanhGia = 2, MaDonHang = 2, MaNguoiMua = 1, MaCuaHang = 20, SoSao = 5, TrangThai = "VISIBLE", NgayTao = DateTime.UtcNow });

        await db.SaveChangesAsync();

        var svc = new ReviewService(db);
        var result = await svc.LayDanhSachShopCanRaSoatAsync(new ShopCanRaSoatFilterVm { Tab = "all", Search = "Alpha" });

        Assert.Equal(1, result.TotalItems);
        Assert.Equal("Alpha Store", result.Items[0].TenShop);
    }
}
