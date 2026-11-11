using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecondHandMarketplace.Web.Common.Extensions;
using SecondHandMarketplace.Web.Models.DTOs.Categories;
using SecondHandMarketplace.Web.Services.Categories;

namespace SecondHandMarketplace.Web.Controllers.Api.v1;

/// <summary>
/// Quản lý danh mục và tình trạng sản phẩm.
/// </summary>
[ApiController]
[Route("api/v1/categories")]
[Produces("application/json")]
public class CategoriesController(ICategoryService categoryService) : ControllerBase
{
    /// <summary>
    /// Lấy danh sách toàn bộ danh mục sản phẩm đang hoạt động.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ServiceResult<IReadOnlyList<CategoryResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategories(CancellationToken cancellationToken)
    {
        var categories = await categoryService.GetCategoriesAsync(cancellationToken);
        var dtos = categories.Select(c => new CategoryResponse
        {
            Id = c.MaDanhMuc,
            Name = c.Ten,
            Description = null,
            Icon = null,
            DisplayOrder = c.ThuTuHienThi,
            IsActive = c.DangHienThi
        }).ToList();

        return ServiceResult<IReadOnlyList<CategoryResponse>>.Ok(dtos).ToActionResult(this);
    }

    /// <summary>
    /// Lấy danh sách các tình trạng sản phẩm (Mới, Đã sử dụng còn mới, v.v.).
    /// </summary>
    [HttpGet("conditions")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ServiceResult<IReadOnlyList<object>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConditions(CancellationToken cancellationToken)
    {
        var conditions = await categoryService.GetConditionsAsync(cancellationToken);
        var dtos = conditions.Select(c => new
        {
            Id = c.MaTinhTrang,
            Code = c.Ma,
            Name = c.Ten,
            Description = c.MoTa,
            DisplayOrder = c.ThuTuHienThi,
            IsActive = c.DangSuDung
        }).ToList();

        return ServiceResult<IReadOnlyList<object>>.Ok(dtos).ToActionResult(this);
    }
}
