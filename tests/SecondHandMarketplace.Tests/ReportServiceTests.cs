using Microsoft.EntityFrameworkCore;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Models.Entities;
using SecondHandMarketplace.Web.Models.ViewModels.Reports;
using SecondHandMarketplace.Web.Services.Reports;
using Xunit;

namespace SecondHandMarketplace.Tests;

public class ReportServiceTests
{
    private AppDbContext GetInMemoryDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task TaoBaoCaoAsync_ChongGuiTrungKhiDangPending()
    {
        using var db = GetInMemoryDbContext("ReportTest_ChongTrung");
        var service = new ReportService(db);

        db.BaoCaos.Add(new BaoCao
        {
            MaBaoCao = 1,
            MaNguoiBaoCao = 5,
            LoaiDoiTuong = "PRODUCT",
            MaDoiTuong = 100,
            LyDo = "Lừa đảo",
            TrangThai = "PENDING"
        });
        await db.SaveChangesAsync();

        var (thanhCong, thongBao) = await service.TaoBaoCaoAsync(5, new TaoBaoCaoViewModel
        {
            LoaiDoiTuong = "PRODUCT",
            MaDoiTuong = 100,
            LyDo = "Lừa đảo lần 2"
        });

        Assert.False(thanhCong);
        Assert.Contains("đang chờ", thongBao);
    }

    [Fact]
    public async Task XuLyBaoCaoAdminAsync_DismissThanhCong()
    {
        using var db = GetInMemoryDbContext("ReportTest_Dismiss");
        var service = new ReportService(db);

        db.BaoCaos.Add(new BaoCao
        {
            MaBaoCao = 1,
            MaNguoiBaoCao = 5,
            LoaiDoiTuong = "PRODUCT",
            MaDoiTuong = 100,
            LyDo = "Spam",
            TrangThai = "PENDING"
        });
        await db.SaveChangesAsync();

        var (thanhCong, _) = await service.XuLyBaoCaoAdminAsync(1, 1, "DISMISS", "Không vi phạm");
        Assert.True(thanhCong);

        var bc = await db.BaoCaos.FindAsync(1);
        Assert.Equal("DISMISSED", bc!.TrangThai);
    }
}
