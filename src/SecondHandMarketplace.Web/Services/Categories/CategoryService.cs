using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using SecondHandMarketplace.Web.Common;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Models.Entities;

namespace SecondHandMarketplace.Web.Services.Categories;

public sealed class CategoryService(AppDbContext db, IDistributedCache cache) : ICategoryService
{
    private static readonly TimeSpan DanhMucCacheTime = TimeSpan.FromMinutes(5);

    public Task<IReadOnlyList<DanhMuc>> GetCategoriesAsync(CancellationToken cancellationToken = default) =>
        GetCachedListAsync("catalog:categories", () => db.DanhMucs.AsNoTracking()
            .OrderBy(x => x.ThuTuHienThi).ThenBy(x => x.Ten).ToListAsync(cancellationToken), cancellationToken);

    public Task<IReadOnlyList<TinhTrangSanPham>> GetConditionsAsync(CancellationToken cancellationToken = default) =>
        GetCachedListAsync("catalog:conditions", () => db.TinhTrangSanPhams.AsNoTracking()
            .OrderBy(x => x.ThuTuHienThi).ThenBy(x => x.Ten).ToListAsync(cancellationToken), cancellationToken);

    private async Task<IReadOnlyList<T>> GetCachedListAsync<T>(
        string key,
        Func<Task<List<T>>> load,
        CancellationToken cancellationToken)
    {
        var json = await cache.GetStringAsync(key, cancellationToken);
        if (!string.IsNullOrWhiteSpace(json))
        {
            try
            {
                var cached = JsonSerializer.Deserialize<List<T>>(json);
                if (cached is not null) return cached;
            }
            catch (JsonException) { }
        }

        var value = await load();
        await cache.SetStringAsync(key, JsonSerializer.Serialize(value),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = DanhMucCacheTime }, cancellationToken);
        return value;
    }

    public async Task<(bool Success, string? Error, int Id)> SaveCategoryAsync(int? id, string name, int order, bool visible, CancellationToken cancellationToken = default)
    {
        name = name.Trim();
        if (name.Length == 0 || name.Length > 100) return (false, "Tên danh mục phải từ 1 đến 100 ký tự.", 0);
        if (await db.DanhMucs.AnyAsync(x => x.Ten == name && x.MaDanhMuc != (id ?? 0), cancellationToken)) return (false, "Tên danh mục đã tồn tại.", 0);
        var entity = id.HasValue ? await db.DanhMucs.SingleOrDefaultAsync(x => x.MaDanhMuc == id.Value, cancellationToken) : new DanhMuc();
        if (entity is null) return (false, "Không tìm thấy danh mục.", 0);
        entity.Ten = name; entity.ThuTuHienThi = order; entity.DangHienThi = visible;
        if (!id.HasValue) db.DanhMucs.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        await cache.RemoveAsync("catalog:categories", cancellationToken);
        await HomeCacheHelper.XoaCacheTrangChuAsync(cache, cancellationToken);
        return (true, null, entity.MaDanhMuc);
    }

    public async Task<(bool Success, string? Error)> DeleteCategoryAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await db.DanhMucs.SingleOrDefaultAsync(x => x.MaDanhMuc == id, cancellationToken);
        if (entity is null) return (false, "Không tìm thấy danh mục.");
        var inUse = await db.SanPhams.AnyAsync(x => x.MaDanhMuc == id, cancellationToken);
        if (inUse) entity.DangHienThi = false;
        else db.DanhMucs.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
        await cache.RemoveAsync("catalog:categories", cancellationToken);
        await HomeCacheHelper.XoaCacheTrangChuAsync(cache, cancellationToken);
        return (true, inUse ? "Danh mục đang có sản phẩm nên đã được ẩn thay vì xóa." : "Đã xóa danh mục.");
    }

    public async Task<(bool Success, string? Error, int Id)> SaveConditionAsync(int? id, string code, string name, string? description, int order, bool active, CancellationToken cancellationToken = default)
    {
        code = code.Trim().ToUpperInvariant(); name = name.Trim();
        if (code.Length == 0 || code.Length > 30 || name.Length == 0 || name.Length > 100) return (false, "Mã và tên tình trạng không hợp lệ.", 0);
        if (await db.TinhTrangSanPhams.AnyAsync(x => (x.Ma == code || x.Ten == name) && x.MaTinhTrang != (id ?? 0), cancellationToken)) return (false, "Mã hoặc tên tình trạng đã tồn tại.", 0);
        var entity = id.HasValue ? await db.TinhTrangSanPhams.SingleOrDefaultAsync(x => x.MaTinhTrang == id.Value, cancellationToken) : new TinhTrangSanPham();
        if (entity is null) return (false, "Không tìm thấy tình trạng sản phẩm.", 0);
        entity.Ma = code; entity.Ten = name; entity.MoTa = description?.Trim(); entity.ThuTuHienThi = order; entity.DangSuDung = active;
        if (!id.HasValue) db.TinhTrangSanPhams.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        await cache.RemoveAsync("catalog:conditions", cancellationToken);
        await HomeCacheHelper.XoaCacheTrangChuAsync(cache, cancellationToken);
        return (true, null, entity.MaTinhTrang);
    }

    public async Task<(bool Success, string? Error)> DeleteConditionAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await db.TinhTrangSanPhams.SingleOrDefaultAsync(x => x.MaTinhTrang == id, cancellationToken);
        if (entity is null) return (false, "Không tìm thấy tình trạng sản phẩm.");
        var inUse = await db.SanPhams.AnyAsync(x => x.MaTinhTrang == id, cancellationToken);
        if (inUse) entity.DangSuDung = false;
        else db.TinhTrangSanPhams.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
        await cache.RemoveAsync("catalog:conditions", cancellationToken);
        await HomeCacheHelper.XoaCacheTrangChuAsync(cache, cancellationToken);
        return (true, inUse ? "Tình trạng đang được sản phẩm sử dụng nên đã được ẩn." : "Đã xóa tình trạng.");
    }
}
