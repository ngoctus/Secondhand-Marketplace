using System.ComponentModel.DataAnnotations;
using SecondHandMarketplace.Web.Models.ViewModels.Products;
using Xunit;

namespace SecondHandMarketplace.Tests;

/// <summary>
/// Test ràng buộc biểu mẫu đăng bán (kế hoạch mục B5): mô tả bắt buộc và đủ dài ngay ở phía máy chủ,
/// không chỉ dựa vào bộ đếm ký tự trên trình duyệt.
/// </summary>
public class ProductFormValidationTests
{
    private const int SoKyTuToiThieu = 20;

    private static List<ValidationResult> Valid(ProductFormVm form)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(form, new ValidationContext(form), results, validateAllProperties: true);
        return results;
    }

    private static ProductFormVm Form(string? moTa) => new()
    {
        Ten = "Áo khoác denim nam size L",
        MoTa = moTa,
        Gia = 250000,
        MaDanhMuc = 1,
        MaTinhTrang = 1
    };

    private static string? Loi(string thuocTinh, List<ValidationResult> results) =>
        results.FirstOrDefault(r => r.MemberNames.Contains(thuocTinh))?.ErrorMessage;

    [Fact]
    public void MoTaRong_BiTuChoi()
    {
        var loi = Loi("MoTa", Valid(Form(null)));

        Assert.NotNull(loi);
        Assert.Contains("bắt buộc", loi);
    }

    [Fact]
    public void MoTaNganHonChuan_BiTuChoi()
    {
        var ngan = new string('a', SoKyTuToiThieu - 1);

        var loi = Loi("MoTa", Valid(Form(ngan)));

        Assert.NotNull(loi);
        Assert.Contains(SoKyTuToiThieu.ToString(), loi);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void MoTaChiCoKhoangTrang_BiTuChoi(string moTa)
    {
        Assert.NotNull(Loi("MoTa", Valid(Form(moTa))));
    }

    [Fact]
    public void MoTaDuChuan_FormHopLe()
    {
        var form = Form("Áo khoác hai lớp, còn đẹp, không rách, mua thêm ảnh thật khi cần.");

        Assert.Empty(Valid(form));
    }

    [Fact]
    public void MoTaVuot4000KyTu_BiTuChoi()
    {
        var loi = Loi("MoTa", Valid(Form(new string('a', 4001))));

        Assert.NotNull(loi);
        Assert.Contains("4000", loi);
    }
}
