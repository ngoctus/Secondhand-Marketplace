using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SecondHandMarketplace.Web.Data;

namespace SecondHandMarketplace.Web.Services.Areas;

public sealed class KhuVucService(AppDbContext db) : IKhuVucService
{
    public const string CookieName = "khuvuc";
    public static readonly TimeSpan CookieLifetime = TimeSpan.FromDays(180);

    public async Task<IReadOnlyList<TinhThanhItem>> GetTinhThanhAsync(CancellationToken cancellationToken = default) =>
        await db.TinhThanhs.AsNoTracking()
            .OrderBy(x => x.TenTinhThanh)
            .Select(x => new TinhThanhItem(x.MaTinhThanh, x.TenTinhThanh))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PhuongXaItem>> GetPhuongXaTheoTinhAsync(int maTinh, CancellationToken cancellationToken = default) =>
        await db.PhuongXas.AsNoTracking()
            .Where(x => x.MaTinhThanh == maTinh)
            .OrderBy(x => x.TenPhuongXa)
            .Select(x => new PhuongXaItem(x.MaPhuongXa, x.TenPhuongXa))
            .ToListAsync(cancellationToken);

    public Task<bool> PhuongXaThuocTinhAsync(int maTinh, int maXa, CancellationToken cancellationToken = default) =>
        db.PhuongXas.AsNoTracking().AnyAsync(x => x.MaPhuongXa == maXa && x.MaTinhThanh == maTinh, cancellationToken);

    public async Task<(int? MaTinh, int? MaXa)> GetKhuVucDaChonAsync(HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        if (!httpContext.Request.Cookies.TryGetValue(CookieName, out var raw) || !TryParse(raw, out var maTinh, out var maXa))
            return (null, null);

        if (!await db.TinhThanhs.AsNoTracking().AnyAsync(x => x.MaTinhThanh == maTinh, cancellationToken))
            return (null, null);
        if (maXa is not null && !await PhuongXaThuocTinhAsync(maTinh, maXa.Value, cancellationToken))
            return (null, null);
        return (maTinh, maXa);
    }

    public async Task<bool> LuuKhuVucDaChonAsync(HttpContext httpContext, int? maTinh, int? maXa, CancellationToken cancellationToken = default)
    {
        if (maTinh is null)
        {
            httpContext.Response.Cookies.Delete(CookieName);
            return true;
        }

        if (!await db.TinhThanhs.AsNoTracking().AnyAsync(x => x.MaTinhThanh == maTinh.Value, cancellationToken)) return false;
        if (maXa is not null && !await PhuongXaThuocTinhAsync(maTinh.Value, maXa.Value, cancellationToken)) return false;

        httpContext.Response.Cookies.Append(CookieName, $"{maTinh.Value}:{maXa?.ToString() ?? string.Empty}", new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.Add(CookieLifetime),
            MaxAge = CookieLifetime,
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Secure = httpContext.Request.IsHttps,
            IsEssential = true,
            Path = "/"
        });
        return true;
    }

    /// <summary>Định dạng "maTinh:maXa" (maXa có thể để trống). Mọi giá trị khác đều là rác.</summary>
    internal static bool TryParse(string? raw, out int maTinh, out int? maXa)
    {
        maTinh = 0;
        maXa = null;
        if (string.IsNullOrWhiteSpace(raw) || raw.Length > 25) return false;

        var parts = raw.Split(':');
        if (parts.Length != 2) return false;
        if (!int.TryParse(parts[0], System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out maTinh) || maTinh <= 0)
            return false;
        if (parts[1].Length == 0) return true;
        if (!int.TryParse(parts[1], System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var xa) || xa <= 0)
            return false;
        maXa = xa;
        return true;
    }
}
