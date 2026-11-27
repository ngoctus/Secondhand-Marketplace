using Microsoft.EntityFrameworkCore;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Models.Entities;
using SecondHandMarketplace.Web.Services.Accounts;
using SecondHandMarketplace.Web.Services.Areas;
using Xunit;

namespace SecondHandMarketplace.Tests;

public class UserAddressServiceTests
{
    private static AppDbContext NewDb([System.Runtime.CompilerServices.CallerMemberName] string name = "")
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"UserAddr_{Guid.NewGuid()}_{name}")
            .Options;
        return new AppDbContext(options);
    }

    private static async Task<(AppDbContext db, UserAddressService service)> SeedAsync()
    {
        var db = NewDb();
        db.NguoiDungs.Add(new NguoiDung
        {
            MaNguoiDung = 1,
            HoTen = "Nguyễn Văn An",
            Email = "an@test.local",
            SoDienThoai = "0901000001",
            TrangThai = "ACTIVE"
        });
        db.TinhThanhs.Add(new TinhThanh { MaTinhThanh = 79, TenTinhThanh = "TP. Hồ Chí Minh", MaHanhChinh = "79" });
        db.PhuongXas.AddRange(
            new PhuongXa { MaPhuongXa = 1001, MaTinhThanh = 79, TenPhuongXa = "Phường Bến Nghé", MaHanhChinh = "26734" },
            new PhuongXa { MaPhuongXa = 1002, MaTinhThanh = 79, TenPhuongXa = "Phường Thảo Điền", MaHanhChinh = "27088" });
        await db.SaveChangesAsync();

        var khuVuc = new KhuVucService(db);
        return (db, new UserAddressService(db, khuVuc));
    }

    [Fact]
    public async Task AddSetDefaultAndDelete_DongBoDiaChiMacDinhVoiNguoiDung()
    {
        var (db, service) = await SeedAsync();
        using (db)
        {
            // Thêm địa chỉ đầu tiên -> tự động là mặc định và đồng bộ NguoiDung.DiaChiMacDinh
            var (ok1, err1, addr1) = await service.AddAsync(1, new UserAddressInput(
                "Nhà riêng", "Nguyễn Văn An", "0901000001", 79, 1001, "25 Lê Lợi", false));
            Assert.True(ok1, err1);
            Assert.NotNull(addr1);
            Assert.True(addr1!.LaMacDinh);
            Assert.Contains("25 Lê Lợi", (await db.NguoiDungs.AsNoTracking().FirstAsync(u => u.MaNguoiDung == 1)).DiaChiMacDinh);

            // Thêm địa chỉ dự phòng thứ hai -> không ghi đè mặc định
            var (ok2, err2, addr2) = await service.AddAsync(1, new UserAddressInput(
                "Văn phòng", "Nguyễn Văn An", "0901000001", 79, 1002, "88 Xuân Thủy", false));
            Assert.True(ok2, err2);
            Assert.False(addr2!.LaMacDinh);

            // Đặt địa chỉ dự phòng thành mặc định -> đồng bộ lại NguoiDung.DiaChiMacDinh
            var (okDef, errDef) = await service.SetDefaultAsync(1, addr2.MaDiaChi);
            Assert.True(okDef, errDef);
            var list = await service.GetAllAsync(1);
            Assert.Equal(2, list.Count);
            Assert.Equal(addr2.MaDiaChi, list[0].MaDiaChi);
            Assert.True(list[0].LaMacDinh);
            Assert.Contains("88 Xuân Thủy", (await db.NguoiDungs.AsNoTracking().FirstAsync(u => u.MaNguoiDung == 1)).DiaChiMacDinh);

            // Xóa địa chỉ mặc định -> tự thăng địa chỉ còn lại thành mặc định
            var (okDel, errDel) = await service.DeleteAsync(1, addr2.MaDiaChi);
            Assert.True(okDel, errDel);
            var remaining = Assert.Single(await service.GetAllAsync(1));
            Assert.Equal(addr1.MaDiaChi, remaining.MaDiaChi);
            Assert.True(remaining.LaMacDinh);
            Assert.Contains("25 Lê Lợi", (await db.NguoiDungs.AsNoTracking().FirstAsync(u => u.MaNguoiDung == 1)).DiaChiMacDinh);
        }
    }
}
