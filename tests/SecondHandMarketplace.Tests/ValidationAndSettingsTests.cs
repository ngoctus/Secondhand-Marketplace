using System.ComponentModel.DataAnnotations;
using SecondHandMarketplace.Web.Models.ViewModels.Account;
using SecondHandMarketplace.Web.Models.ViewModels.Orders;
using SecondHandMarketplace.Web.Models.ViewModels.Products;
using SecondHandMarketplace.Web.Models.ViewModels.Shop;
using Xunit;

namespace SecondHandMarketplace.Tests;

public class ValidationAndSettingsTests
{
    private static IList<ValidationResult> ValidateModel(object model)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(model, serviceProvider: null, items: null);
        Validator.TryValidateObject(model, context, results, validateAllProperties: true);
        return results;
    }

    [Theory]
    [InlineData("0901234567", true)]
    [InlineData("0387431269", true)]
    [InlineData("0771234567", true)]
    [InlineData("0521234567", true)]
    [InlineData("0891234567", true)]
    [InlineData("0123456789", false)] // Không bắt đầu bằng 03, 05, 07, 08, 09
    [InlineData("090123456", false)]   // 9 số (thiếu 1 số)
    [InlineData("09012345678", false)] // 11 số (thừa)
    [InlineData("090123456a", false)] // Có chữ cái
    [InlineData("", false)]            // Trống
    public void RegisterVm_PhoneValidation_ShouldValidateCorrectly(string phone, bool isValid)
    {
        var vm = new RegisterVm
        {
            FullName = "Nguyễn Văn A",
            Email = "test@example.com",
            Phone = phone,
            Password = "Password@123",
            ConfirmPassword = "Password@123"
        };

        var errors = ValidateModel(vm);
        var hasPhoneError = errors.Any(e => e.MemberNames.Contains(nameof(RegisterVm.Phone)));

        Assert.Equal(isValid, !hasPhoneError);
    }

    [Theory]
    [InlineData("Password@123", true)]
    [InlineData("Admin#2026", true)]
    [InlineData("Abcd@123", true)]
    [InlineData("Abc@12", false)]      // < 8 ký tự
    [InlineData("password@123", false)] // Không có chữ hoa
    [InlineData("PASSWORD@123", false)] // Không có chữ thường
    [InlineData("Password@@@", false)]  // Không có số
    [InlineData("Password123", false)]  // Không có ký tự đặc biệt
    public void RegisterVm_PasswordValidation_ShouldValidateCorrectly(string password, bool isValid)
    {
        var vm = new RegisterVm
        {
            FullName = "Nguyễn Văn A",
            Email = "test@example.com",
            Phone = "0901234567",
            Password = password,
            ConfirmPassword = password
        };

        var errors = ValidateModel(vm);
        var hasPasswordError = errors.Any(e => e.MemberNames.Contains(nameof(RegisterVm.Password)));

        Assert.Equal(isValid, !hasPasswordError);
    }

    [Theory]
    [InlineData("NewPass@2026", true)]
    [InlineData("short1!", false)]
    [InlineData("NoSpecial123", false)]
    public void ResetPasswordOtpVm_PasswordValidation_ShouldValidateCorrectly(string newPassword, bool isValid)
    {
        var vm = new ResetPasswordOtpVm
        {
            Email = "test@example.com",
            Code = "123456",
            NewPassword = newPassword,
            ConfirmPassword = newPassword
        };

        var errors = ValidateModel(vm);
        var hasPasswordError = errors.Any(e => e.MemberNames.Contains(nameof(ResetPasswordOtpVm.NewPassword)));

        Assert.Equal(isValid, !hasPasswordError);
    }

    [Theory]
    [InlineData("NewPass@2026", true)]
    [InlineData("short1!", false)]
    [InlineData("NoSpecial123", false)]
    public void ChangePasswordVm_PasswordValidation_ShouldValidateCorrectly(string newPassword, bool isValid)
    {
        var vm = new ChangePasswordVm
        {
            CurrentPassword = "OldPassword@123",
            NewPassword = newPassword,
            ConfirmPassword = newPassword
        };

        var errors = ValidateModel(vm);
        var hasPasswordError = errors.Any(e => e.MemberNames.Contains(nameof(ChangePasswordVm.NewPassword)));

        Assert.Equal(isValid, !hasPasswordError);
    }

    [Fact]
    public void ProductSearchVm_DefaultPageSize_ShouldBe9()
    {
        Assert.Equal(9, ProductSearchVm.DefaultPageSize);
        var vm = new ProductSearchVm();
        Assert.Equal(9, vm.PageSize);
    }
}
