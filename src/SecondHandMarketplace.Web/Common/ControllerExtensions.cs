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

    public static void ThongBaoCanhBao(this Controller controller, string noiDung) =>
        controller.TempData["CanhBao"] = noiDung;

    /// <summary>
    /// Hiển thị Modal Dialog trung tâm cho các lỗi hoặc cảnh báo nghiêm trọng (Task C-01).
    /// </summary>
    public static void ThongBaoModalLoi(this Controller controller, string noiDung, string tieuDe = "Cảnh báo nghiêm trọng")
    {
        controller.TempData["ModalLoi"] = noiDung;
        controller.TempData["ModalTieuDe"] = tieuDe;
    }
}
