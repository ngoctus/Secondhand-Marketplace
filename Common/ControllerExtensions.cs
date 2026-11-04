using Microsoft.AspNetCore.Mvc;

namespace SecondHandMarketplace.Web.Common;

/// <summary>
/// Đặt thông báo hiển thị ở trang kế tiếp (qua TempData, hiện bởi _ThongBao.cshtml).
/// Dùng trong Controller: this.ThongBaoThanhCong("Đăng ký thành công");
/// </summary>
public static class ControllerExtensions
{
    public static void ThongBaoThanhCong(this Controller controller, string noiDung) =>
        controller.TempData["ThanhCong"] = noiDung;

    public static void ThongBaoLoi(this Controller controller, string noiDung) =>
        controller.TempData["Loi"] = noiDung;
}
