using Microsoft.AspNetCore.Mvc;
using SecondHandMarketplace.Web.Common;
using SecondHandMarketplace.Web.Common.Extensions;
using SecondHandMarketplace.Web.Models.ViewModels.Admin;
using SecondHandMarketplace.Web.Services.Accounts;

namespace SecondHandMarketplace.Web.Areas.Admin.Controllers;

/// <summary>
/// Quản trị đơn Khiếu nại khóa nhầm / Cam kết mở khóa tài khoản & cấu hình Cooldown / VietQR (Task KN-03).
/// </summary>
public sealed class AccountAppealsController(IAccountAppealAdminService appeals) : AdminBaseController
{
    [HttpGet]
    public async Task<IActionResult> Index(
        string? trangThai = null,
        string? loaiYeuCau = null,
        string? tuKhoa = null,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        var filter = new AdminAccountAppealFilter(trangThai, loaiYeuCau, tuKhoa, page);
        var model = await appeals.GetAdminListAsync(filter, cancellationToken);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0) return NotFound();
        var detail = await appeals.GetAdminDetailAsync(id, cancellationToken);
        if (detail is null) return NotFound();
        return View(detail);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id, string? ghiChuAdmin, CancellationToken cancellationToken = default)
    {
        var adminId = ParseCurrentUserId();
        if (adminId is null) return Forbid();

        var result = await appeals.ApproveAsync(adminId.Value, id, ghiChuAdmin, cancellationToken);
        if (result.Success)
            this.ThongBaoThanhCong(result.Message ?? "Đã chấp thuận đơn và mở khóa tài khoản.");
        else
            this.ThongBaoLoi(result.Error ?? "Không thể duyệt đơn khiếu nại.");

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, string? ghiChuAdmin, CancellationToken cancellationToken = default)
    {
        var adminId = ParseCurrentUserId();
        if (adminId is null) return Forbid();

        var result = await appeals.RejectAsync(adminId.Value, id, ghiChuAdmin, cancellationToken);
        if (result.Success)
            this.ThongBaoThanhCong(result.Message ?? "Đã từ chối đơn khiếu nại.");
        else
            this.ThongBaoLoi(result.Error ?? "Không thể từ chối đơn khiếu nại.");

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Settings(CancellationToken cancellationToken = default)
    {
        var current = await appeals.GetSettingsAsync(cancellationToken);
        var vm = new AccountAppealSettingsVm
        {
            SoLanGuiMoiChuKy = current.SoLanGuiMoiChuKy,
            CooldownNgay = current.CooldownNgay,
            CooldownGio = current.CooldownGio,
            CooldownPhutLe = current.CooldownPhutLe,
            PhiMoKhoaLan2 = current.PhiMoKhoaLan2,
            TenNganHang = current.TenNganHang,
            MaNganHang = current.MaNganHang,
            SoTaiKhoan = current.SoTaiKhoan,
            ChuTaiKhoan = current.ChuTaiKhoan,
            CooldownPhutHienTai = current.CooldownPhut,
            NgayCapNhat = current.NgayCapNhat,
            TenNguoiCapNhat = current.TenNguoiCapNhat
        };
        PopulateQrPreview(vm);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Settings(AccountAppealSettingsVm vm, CancellationToken cancellationToken = default)
    {
        var adminId = ParseCurrentUserId();
        if (adminId is null) return Forbid();

        var request = new UpdateAccountAppealSettingsRequest
        {
            SoLanGuiMoiChuKy = vm.SoLanGuiMoiChuKy,
            CooldownNgay = vm.CooldownNgay,
            CooldownGio = vm.CooldownGio,
            CooldownPhutLe = vm.CooldownPhutLe,
            PhiMoKhoaLan2 = vm.PhiMoKhoaLan2,
            TenNganHang = vm.TenNganHang,
            MaNganHang = vm.MaNganHang,
            SoTaiKhoan = vm.SoTaiKhoan,
            ChuTaiKhoan = vm.ChuTaiKhoan
        };

        var result = await appeals.UpdateSettingsAsync(adminId.Value, request, cancellationToken);
        if (result.Success)
        {
            this.ThongBaoThanhCong(result.Message ?? "Đã lưu cấu hình khiếu nại & mở khóa tài khoản.");
            return RedirectToAction(nameof(Settings));
        }

        ModelState.AddModelError(string.Empty, result.Error ?? "Không thể lưu cấu hình.");
        var current = await appeals.GetSettingsAsync(cancellationToken);
        vm.CooldownPhutHienTai = current.CooldownPhut;
        vm.NgayCapNhat = current.NgayCapNhat;
        vm.TenNguoiCapNhat = current.TenNguoiCapNhat;
        PopulateQrPreview(vm);
        return View(vm);
    }

    private static void PopulateQrPreview(AccountAppealSettingsVm vm)
    {
        var transferContent = AccountAppealRules.BuildTransferContent(1, AccountAppealRules.FeeLockTier);
        vm.PreviewTransferContent = transferContent;
        var bankCode = string.IsNullOrWhiteSpace(vm.MaNganHang) ? AccountAppealRules.DefaultBankCode : vm.MaNganHang.Trim();
        var accountNo = string.IsNullOrWhiteSpace(vm.SoTaiKhoan) ? AccountAppealRules.DefaultBankAccount : vm.SoTaiKhoan.Trim();
        var amount = Math.Max(0m, vm.PhiMoKhoaLan2);
        vm.PreviewQrDataUrl = VietQrGenerator.GenerateMockQrDataUrl(bankCode, accountNo, amount, transferContent);
    }

    private int? ParseCurrentUserId() => int.TryParse(User.GetUserId(), out var id) ? id : null;
}
