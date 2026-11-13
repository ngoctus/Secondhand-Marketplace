using Microsoft.EntityFrameworkCore;
using SecondHandMarketplace.Web.Common;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Models.Entities;
using SecondHandMarketplace.Web.Models.ViewModels.Products;
using SecondHandMarketplace.Web.Services.Products;
using SecondHandMarketplace.Web.Services.Reviews;
using Xunit;

namespace SecondHandMarketplace.Tests;

/// <summary>
/// Test các luồng truy vấn công khai của ProductService (kế hoạch mục B9):
/// lọc khu vực, lọc giá theo phân khúc cố định, cắt mô tả ngắn trên thẻ sản phẩm.
/// </summary>
public class ProductSearchServiceTests
{
    private static AppDbContext NewDb([System.Runtime.CompilerServices.CallerMemberName] string name = "")
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"ProductSearch_{Guid.NewGuid()}_{name}")
            .Options;
        return new AppDbContext(options);
    }

    private static NguoiDung User(int id, string trangThai = "ACTIVE") => new()
    {
        MaNguoiDung = id,
        HoTen = $"User {id}",
        Email = $"u{id}@test.local",
        TrangThai = trangThai
    };

    private static CuaHang Shop(int id, int ownerId, int? maTinh = null, int? maXa = null, string trangThai = "ACTIVE") => new()
    {
        MaCuaHang = id,
        MaNguoiDung = ownerId,
        TenShop = $"Shop {id}",
        SoDienThoaiLienHe = "0100000000",
        TrangThai = trangThai,
        MaTinhThanh = maTinh,
        MaPhuongXa = maXa,
        KhuVucHienThi = maTinh is null ? null : $"Phường {maXa}, Tỉnh {maTinh}",
        NgayTao = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
    };

    private static SanPham Product(
        int id, int shopId, decimal gia, string ten = "Món đồ", string? moTa = null,
        string trangThai = "ACTIVE", bool daXoa = false, int? ngay = 1) => new()
    {
        MaSanPham = id,
        MaCuaHang = shopId,
        Ten = ten,
        // Seed và service đều ghi chuỗi chuẩn hóa không dấu của tên vào cột này.
        ChuoiTimKiem = TextHelper.ChuanHoa(ten),
        MoTa = moTa,
        Gia = gia,
        TrangThai = trangThai,
        DaXoa = daXoa,
        NgayTao = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(ngay ?? 0),
        NgayCapNhat = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(ngay ?? 0)
    };

    private static async Task<(AppDbContext db, ProductService service)> DaoTaoCoSoDungTimKiemAsync()
    {
        var db = NewDb();
        db.NguoiDungs.AddRange(User(1), User(2), User(3), User(4, "LOCKED"));
        // Shop 1: tỉnh 1, phường 11. Shop 2: tỉnh 1, phường 12. Shop 3: tỉnh 2. Shop 4: chủ bị khóa.
        db.CuaHangs.AddRange(
            Shop(1, 1, maTinh: 1, maXa: 11),
            Shop(2, 2, maTinh: 1, maXa: 12),
            Shop(3, 3, maTinh: 2, maXa: 21),
            Shop(4, 4, maTinh: 1, maXa: 11));
        db.SanPhams.AddRange(
            Product(101, 1, 50_000m, "Áo khoác jeans", ngay: 1),
            Product(102, 1, 200_000m, "Balô cũ", ngay: 2),
            Product(103, 2, 400_000m, "Bàn phím cơ", ngay: 3),
            Product(104, 3, 9_000_000m, "Máy ảnh", ngay: 4),
            // Ba tin sau không được xuất hiện trên trang công khai.
            Product(105, 1, 300_000m, "Tin đang nháp", trangThai: "DRAFT", ngay: 5),
            Product(106, 1, 300_000m, "Tin đã xóa mềm", trangThai: "ACTIVE", daXoa: true, ngay: 6),
            Product(107, 4, 300_000m, "Tin của shop bị khóa", ngay: 7));
        await db.SaveChangesAsync();
        return (db, new ProductService(db, new ReviewService(db)));
    }

    [Fact]
    public async Task SearchAsync_LocTheoMaTinhThanh()
    {
        var (db, service) = await DaoTaoCoSoDungTimKiemAsync();
        using (db)
        {
            var ketQua = await service.SearchAsync(new ProductSearchVm { MaTinhThanh = 1 });

            Assert.Equal(new[] { 101, 102, 103 }, ketQua.Items.Select(i => i.Id).OrderBy(id => id));
            Assert.Equal(3, ketQua.TotalItems);
        }
    }

    [Fact]
    public async Task SearchAsync_LocTheoMaPhuongXa_ThuHepHonMaTinhThanh()
    {
        var (db, service) = await DaoTaoCoSoDungTimKiemAsync();
        using (db)
        {
            var caTinh = await service.SearchAsync(new ProductSearchVm { MaTinhThanh = 1 });
            var caPhuong = await service.SearchAsync(new ProductSearchVm { MaTinhThanh = 1, MaPhuongXa = 12 });

            Assert.Equal(3, caTinh.TotalItems);
            Assert.Equal(new[] { 103 }, caPhuong.Items.Select(i => i.Id));
            Assert.Equal("Phường 12, Tỉnh 1", caPhuong.Items.Single().KhuVuc);
        }
    }

    [Fact]
    public async Task SearchAsync_TinhThanhKhongCoTinTraVeDanhSachRong()
    {
        var (db, service) = await DaoTaoCoSoDungTimKiemAsync();
        using (db)
        {
            var ketQua = await service.SearchAsync(new ProductSearchVm { MaTinhThanh = 99 });

            Assert.Empty(ketQua.Items);
            Assert.Equal(0, ketQua.TotalItems);
        }
    }

    [Fact]
    public async Task SearchAsync_PhanKhucGiaBaoGiamHaiDau()
    {
        var (db, service) = await DaoTaoCoSoDungTimKiemAsync();
        using (db)
        {
            // Phân khúc 2 = 100.000đ - 350.000đ: 200.000 vào, 50.000 và 400.000 ở ngoài.
            var giua = await service.SearchAsync(new ProductSearchVm { PriceRange = 2 });
            Assert.Equal(new[] { 102 }, giua.Items.Select(i => i.Id));

            // Phân khúc 5 = trên 5.000.000đ (không có biên trên).
            var cao = await service.SearchAsync(new ProductSearchVm { PriceRange = 5 });
            Assert.Equal(new[] { 104 }, cao.Items.Select(i => i.Id));

            // Boundary: đúng 50.000đ thuộc phân khúc 1 (0 - 100.000đ).
            var thap = await service.SearchAsync(new ProductSearchVm { PriceRange = 1 });
            Assert.Equal(new[] { 101 }, thap.Items.Select(i => i.Id));
        }
    }

    [Fact]
    public async Task SearchAsync_LocGiaTheoMinMaxNhapTay()
    {
        var (db, service) = await DaoTaoCoSoDungTimKiemAsync();
        using (db)
        {
            var ketQua = await service.SearchAsync(new ProductSearchVm { PriceRange = 0, MinPrice = 190_000m, MaxPrice = 400_000m });

            Assert.Equal(new[] { 102, 103 }, ketQua.Items.Select(i => i.Id).OrderBy(id => id));
        }
    }

    [Fact]
    public async Task SearchAsync_CatMoTaNganTheoDoDaiChuanVaKhongCatTinNgan()
    {
        var (db, service) = await DaoTaoCoSoDungTimKiemAsync();
        using (db)
        {
            var dai = new string('A', 130);
            db.SanPhams.Add(Product(108, 1, 60_000m, "Tin mô tả dài", dai, ngay: 8));
            db.SanPhams.Add(Product(109, 1, 60_000m, "Tin mô tả vừa", "Còn mới 90%, mua 2024.", ngay: 9));
            db.SanPhams.Add(Product(110, 1, 60_000m, "Tin không mô tả", "   ", ngay: 10));
            await db.SaveChangesAsync();

            var ketQua = await service.SearchAsync(new ProductSearchVm { PageSize = 100 });

            var daiKetQua = ketQua.Items.Single(i => i.Id == 108).MoTaNgan;
            Assert.NotNull(daiKetQua);
            Assert.Equal(new string('A', ProductCardVm.DoDaiMoTaNgan) + "…", daiKetQua);

            Assert.Equal("Còn mới 90%, mua 2024.", ketQua.Items.Single(i => i.Id == 109).MoTaNgan);
            Assert.Null(ketQua.Items.Single(i => i.Id == 110).MoTaNgan);
        }
    }

    [Fact]
    public async Task SearchAsync_ChatCacTinKhongCongKhai()
    {
        var (db, service) = await DaoTaoCoSoDungTimKiemAsync();
        using (db)
        {
            var ketQua = await service.SearchAsync(new ProductSearchVm { PageSize = 100 });

            Assert.Equal(new[] { 101, 102, 103, 104 }, ketQua.Items.Select(i => i.Id).OrderBy(id => id).ToArray());
            Assert.All(ketQua.Items, i => Assert.Equal("ACTIVE", i.Status));
        }
    }

    [Fact]
    public async Task SearchAsync_TimKhongDauVanKhopVaSapXepTheoGia()
    {
        var (db, service) = await DaoTaoCoSoDungTimKiemAsync();
        using (db)
        {
            var ketQua = await service.SearchAsync(new ProductSearchVm { Keyword = "ao khoac", Sort = "price_asc" });

            Assert.Equal(new[] { 101 }, ketQua.Items.Select(i => i.Id));

            var giaTangDan = await service.SearchAsync(new ProductSearchVm { Sort = "price_asc" });
            Assert.Equal(new[] { 101, 102, 103, 104 }, giaTangDan.Items.Select(i => i.Id).ToArray());

            var giaGiamDan = await service.SearchAsync(new ProductSearchVm { Sort = "price_desc" });
            Assert.Equal(new[] { 104, 103, 102, 101 }, giaGiamDan.Items.Select(i => i.Id).ToArray());
        }
    }

    [Fact]
    public async Task SearchAsync_PhanTrangGiuDungTongSoSanPham()
    {
        var (db, service) = await DaoTaoCoSoDungTimKiemAsync();
        using (db)
        {
            // Mặc định sắp xếp newest: 104, 103, 102, 101.
            var trang1 = await service.SearchAsync(new ProductSearchVm { Page = 1, PageSize = 2 });
            var trang2 = await service.SearchAsync(new ProductSearchVm { Page = 2, PageSize = 2 });

            Assert.Equal(new[] { 104, 103 }, trang1.Items.Select(i => i.Id));
            Assert.Equal(new[] { 102, 101 }, trang2.Items.Select(i => i.Id));
            Assert.Equal(4, trang1.TotalItems);
            Assert.Equal(2, trang1.TotalPages);
            Assert.Equal(4, trang2.TotalItems);
        }
    }
}
