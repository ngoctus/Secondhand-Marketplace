using SecondHandMarketplace.Web.Models.Entities;

namespace SecondHandMarketplace.Web.Services.Categories;

public interface ICategoryService
{
    Task<IReadOnlyList<DanhMuc>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TinhTrangSanPham>> GetConditionsAsync(CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error, int Id)> SaveCategoryAsync(int? id, string name, int order, bool visible, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> DeleteCategoryAsync(int id, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error, int Id)> SaveConditionAsync(int? id, string code, string name, string? description, int order, bool active, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> DeleteConditionAsync(int id, CancellationToken cancellationToken = default);
}