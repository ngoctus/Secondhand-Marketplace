using Microsoft.AspNetCore.Mvc;
using SecondHandMarketplace.Web.Common;
using SecondHandMarketplace.Web.Common.Constants;
using SecondHandMarketplace.Web.Common.Extensions;
using Xunit;

namespace SecondHandMarketplace.Tests;

public class ApiContractTests
{
    private class DummyController : ControllerBase { }

    [Fact]
    public void ServiceResult_Ok_ShouldHaveCorrectProperties()
    {
        var result = ServiceResult<string>.Ok("TestData", "Thành công");

        Assert.True(result.Success);
        Assert.Equal("SUCCESS", result.Code);
        Assert.Equal("Thành công", result.Message);
        Assert.Equal("TestData", result.Data);
        Assert.Null(result.Errors);
    }

    [Fact]
    public void ServiceResult_Fail_ShouldHaveCorrectProperties()
    {
        var errors = new[] { "Lỗi 1", "Lỗi 2" };
        var result = ServiceResult<string>.Fail(ErrorCodes.ValidationError, "Dữ liệu không hợp lệ", errors);

        Assert.False(result.Success);
        Assert.Equal(ErrorCodes.ValidationError, result.Code);
        Assert.Equal("Dữ liệu không hợp lệ", result.Message);
        Assert.Null(result.Data);
        Assert.NotNull(result.Errors);
        Assert.Equal(2, result.Errors!.Count);
    }

    [Theory]
    [InlineData(10, 10, 1)]
    [InlineData(11, 10, 2)]
    [InlineData(0, 10, 0)]
    [InlineData(25, 5, 5)]
    public void PagedResult_TotalPages_ShouldCalculateCorrectly(int totalItems, int pageSize, int expectedPages)
    {
        var items = Enumerable.Range(1, totalItems).ToList();
        var paged = new PagedResult<int>(items, totalItems, 1, pageSize);

        Assert.Equal(expectedPages, paged.TotalPages);
    }

    [Fact]
    public void PagedResult_NavigationProperties_ShouldBeAccurate()
    {
        var pagedFirstPage = new PagedResult<int>(new List<int> { 1, 2 }, 30, 1, 10);
        Assert.False(pagedFirstPage.HasPreviousPage);
        Assert.True(pagedFirstPage.HasNextPage);

        var pagedMiddlePage = new PagedResult<int>(new List<int> { 11, 12 }, 30, 2, 10);
        Assert.True(pagedMiddlePage.HasPreviousPage);
        Assert.True(pagedMiddlePage.HasNextPage);

        var pagedLastPage = new PagedResult<int>(new List<int> { 21, 22 }, 30, 3, 10);
        Assert.True(pagedLastPage.HasPreviousPage);
        Assert.False(pagedLastPage.HasNextPage);
    }

    [Fact]
    public void ToActionResult_Ok_Returns200Ok()
    {
        var controller = new DummyController();
        var result = ServiceResult<int>.Ok(123);
        var actionResult = result.ToActionResult(controller);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal(200, okResult.StatusCode);
    }

    [Fact]
    public void ToActionResult_NotFound_Returns404NotFound()
    {
        var controller = new DummyController();
        var result = ServiceResult<int>.Fail(ErrorCodes.ProductNotFound, "Không tìm thấy");
        var actionResult = result.ToActionResult(controller);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(actionResult);
        Assert.Equal(404, notFoundResult.StatusCode);
    }

    [Fact]
    public void ToActionResult_Unauthorized_Returns401Unauthorized()
    {
        var controller = new DummyController();
        var result = ServiceResult<int>.Fail(ErrorCodes.Unauthorized, "Chưa đăng nhập");
        var actionResult = result.ToActionResult(controller);

        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(actionResult);
        Assert.Equal(401, unauthorizedResult.StatusCode);
    }

    [Fact]
    public void ToActionResult_Forbidden_Returns403Forbidden()
    {
        var controller = new DummyController();
        var result = ServiceResult<int>.Fail(ErrorCodes.Forbidden, "Không có quyền");
        var actionResult = result.ToActionResult(controller);

        var statusResult = Assert.IsType<ObjectResult>(actionResult);
        Assert.Equal(403, statusResult.StatusCode);
    }

    [Fact]
    public void ToActionResult_Conflict_Returns409Conflict()
    {
        var controller = new DummyController();
        var result = ServiceResult<int>.Fail(ErrorCodes.EmailExists, "Email đã tồn tại");
        var actionResult = result.ToActionResult(controller);

        var conflictResult = Assert.IsType<ConflictObjectResult>(actionResult);
        Assert.Equal(409, conflictResult.StatusCode);
    }

    [Fact]
    public void ToActionResult_BadRequest_Returns400BadRequest()
    {
        var controller = new DummyController();
        var result = ServiceResult<int>.Fail(ErrorCodes.ValidationError, "Dữ liệu sai");
        var actionResult = result.ToActionResult(controller);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(actionResult);
        Assert.Equal(400, badRequestResult.StatusCode);
    }
}
