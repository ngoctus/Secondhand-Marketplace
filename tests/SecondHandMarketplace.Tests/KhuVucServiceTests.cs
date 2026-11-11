using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Models.Entities;
using SecondHandMarketplace.Web.Services.Areas;

namespace SecondHandMarketplace.Tests;

public class KhuVucServiceTests
{
    private static KhuVucService NewService(string name)
    {
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(name).Options);
        db.TinhThanhs.AddRange(
            new TinhThanh { MaTinhThanh = 79, TenTinhThanh = "Thành phố Hồ Chí Minh", MaHanhChinh = "79" },
            new TinhThanh { MaTinhThanh = 1, TenTinhThanh = "Thành phố Hà Nội", MaHanhChinh = "01" });
        db.PhuongXas.AddRange(
            new PhuongXa { MaPhuongXa = 26734, MaTinhThanh = 79, TenPhuongXa = "Phường Sài Gòn", MaHanhChinh = "26734" },
            new PhuongXa { MaPhuongXa = 26737, MaTinhThanh = 79, TenPhuongXa = "Phường Tân Định", MaHanhChinh = "26737" },
            new PhuongXa { MaPhuongXa = 4, MaTinhThanh = 1, TenPhuongXa = "Phường Ba Đình", MaHanhChinh = "00004" });
        db.SaveChanges();
        return new KhuVucService(db);
    }

    /// <summary>Gắn cookie mà response vừa Set-Cookie vào một request mới (giống trình duyệt gửi lại).</summary>
    private static DefaultHttpContext RequestVoiCookieTu(HttpContext response)
    {
        var request = new DefaultHttpContext();
        var cookies = response.Response.Headers.SetCookie
            .Select(h => SetCookieHeaderValue.Parse(h!))
            .Select(c => $"{c.Name}={c.Value}");
        request.Request.Headers.Cookie = string.Join("; ", cookies);
        return request;
    }

    private static DefaultHttpContext RequestVoiCookie(string raw)
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers.Cookie = $"{KhuVucService.CookieName}={Uri.EscapeDataString(raw)}";
        return ctx;
    }

    [Fact]
    public async Task GetTinhThanh_TraVeDaySapXepTheoTen()
    {
        var svc = NewService(nameof(GetTinhThanh_TraVeDaySapXepTheoTen));
        var list = await svc.GetTinhThanhAsync();

        Assert.Equal(2, list.Count);
        Assert.Equal("Thành phố Hà Nội", list[0].Ten);
    }

    [Fact]
    public async Task GetPhuongXaTheoTinh_ChiTraXaCuaTinhDo()
    {
        var svc = NewService(nameof(GetPhuongXaTheoTinh_ChiTraXaCuaTinhDo));
        var list = await svc.GetPhuongXaTheoTinhAsync(79);

        Assert.Equal(2, list.Count);
        Assert.DoesNotContain(list, x => x.Ma == 4);
        Assert.Empty(await svc.GetPhuongXaTheoTinhAsync(12345));
    }

    [Fact]
    public async Task LuuRoiDoc_TraVeDungKhuVuc()
    {
        var svc = NewService(nameof(LuuRoiDoc_TraVeDungKhuVuc));
        var response = new DefaultHttpContext();

        Assert.True(await svc.LuuKhuVucDaChonAsync(response, 79, 26734));
        var request = RequestVoiCookieTu(response);
        var (tinh, xa) = await svc.GetKhuVucDaChonAsync(request);

        Assert.Equal(79, tinh);
        Assert.Equal(26734, xa);
    }

    [Fact]
    public async Task LuuChiTinh_DocLaiXaLaNull()
    {
        var svc = NewService(nameof(LuuChiTinh_DocLaiXaLaNull));
        var response = new DefaultHttpContext();

        Assert.True(await svc.LuuKhuVucDaChonAsync(response, 79, null));
        var (tinh, xa) = await svc.GetKhuVucDaChonAsync(RequestVoiCookieTu(response));

        Assert.Equal(79, tinh);
        Assert.Null(xa);
    }

    [Fact]
    public async Task Luu_CookieHttpOnlySameSiteLaxVaHan180Ngay()
    {
        var svc = NewService(nameof(Luu_CookieHttpOnlySameSiteLaxVaHan180Ngay));
        var response = new DefaultHttpContext();
        await svc.LuuKhuVucDaChonAsync(response, 79, 26734);

        var cookie = SetCookieHeaderValue.Parse(response.Response.Headers.SetCookie.Single()!);
        Assert.Equal("khuvuc", cookie.Name.Value);
        Assert.True(cookie.HttpOnly);
        Assert.Equal(Microsoft.Net.Http.Headers.SameSiteMode.Lax, cookie.SameSite);
        Assert.Equal(TimeSpan.FromDays(180), cookie.MaxAge);
    }

    [Fact]
    public async Task Luu_XaKhongThuocTinh_BiTuChoiVaKhongGhiCookie()
    {
        var svc = NewService(nameof(Luu_XaKhongThuocTinh_BiTuChoiVaKhongGhiCookie));
        var response = new DefaultHttpContext();

        Assert.False(await svc.LuuKhuVucDaChonAsync(response, 79, 4));
        Assert.False(response.Response.Headers.ContainsKey("Set-Cookie"));
    }

    [Theory]
    [InlineData("rac")]
    [InlineData("")]
    [InlineData("79")]
    [InlineData("79:26734:1")]
    [InlineData("abc:def")]
    [InlineData("-5:7")]
    [InlineData("79:-1")]
    [InlineData("99999:1")]            // tỉnh không tồn tại
    [InlineData("79:4")]               // xã thuộc tỉnh khác
    [InlineData("79:99999")]           // xã không tồn tại
    [InlineData("79:26734'; DROP TABLE x;--")]
    public async Task CookieRacHoacSai_TraVeNull(string raw)
    {
        var svc = NewService("CookieRac_" + Guid.NewGuid());
        var (tinh, xa) = await svc.GetKhuVucDaChonAsync(RequestVoiCookie(raw));

        Assert.Null(tinh);
        Assert.Null(xa);
    }

    [Fact]
    public async Task KhongCoCookie_TraVeNull()
    {
        var svc = NewService(nameof(KhongCoCookie_TraVeNull));
        var (tinh, xa) = await svc.GetKhuVucDaChonAsync(new DefaultHttpContext());

        Assert.Null(tinh);
        Assert.Null(xa);
    }

    [Fact]
    public async Task Luu_TinhNull_XoaLuaChon()
    {
        var svc = NewService(nameof(Luu_TinhNull_XoaLuaChon));
        var response = new DefaultHttpContext();

        Assert.True(await svc.LuuKhuVucDaChonAsync(response, null, null));
        var cookie = SetCookieHeaderValue.Parse(response.Response.Headers.SetCookie.Single()!);
        Assert.True(cookie.Expires < DateTimeOffset.UtcNow);
    }
}
