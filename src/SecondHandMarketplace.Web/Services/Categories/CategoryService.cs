using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Models.Entities;

namespace SecondHandMarketplace.Web.Services.Categories;

public sealed class CategoryService(AppDbContext db, IMemoryCache cache) : ICategoryService
{
    private static readonly TimeSpan DanhMucCacheTime = TimeSpan.FromMinutes(5);

    public async Task<IReadOnlyList<DanhMuc>> GetCategoriesAsync(CancellationToken cancellationToken = default) =>
        await cache.GetOrCreateAsync("catalog:categories", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = DanhMucCacheTime;
            return await db.DanhMucs.AsNoTracking().OrderBy(x => x.ThuTuHienThi).ThenBy(x => x.Ten).ToListAsync(cancellationToken);
        }) ?? [];

    public async Task<IReadOnlyList<TinhTrangSanPham>> GetConditionsAsync(CancellationToken cancellationToken = default) =>
        await cache.GetOrCreateAsync("catalog:conditions", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = DanhMucCacheTime;
            return await db.TinhTrangSanPhams.AsNoTracking().OrderBy(x => x.ThuTuHienThi).ThenBy(x => x.Ten).ToListAsync(cancellationToken);
        }) ?? [];

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
        cache.Remove("catalog:categories");
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
        cache.Remove("catalog:categories");
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
        cache.Remove("catalog:conditions");
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
        cache.Remove("catalog:conditions");
        return (true, inUse ? "Tình trạng đang được sản phẩm sử dụng nên đã được ẩn." : "Đã xóa tình trạng.");
    }
}
