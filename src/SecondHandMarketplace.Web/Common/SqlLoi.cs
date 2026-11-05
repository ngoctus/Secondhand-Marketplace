using Microsoft.Data.SqlClient;

namespace SecondHandMarketplace.Web.Common;

/// <summary>Đổi SqlException hoặc Exception thành câu thông báo tiếng Việt thân thiện.</summary>
public static class SqlLoi
{
    public static string ThongBao(SqlException ex)
    {
        return ex.Number switch
        {
            51101 => "Tài khoản người mua không hợp lệ hoặc đã bị khóa.",
            51102 => "Không có sản phẩm nào được chọn trong giỏ hàng.",
            51103 => "Bạn không thể đặt mua sản phẩm của chính cửa hàng mình.",
            51104 => "Sản phẩm không còn bán hoặc đã có người khác nhanh tay đặt mua trước.",
            51105 => "Giá sản phẩm đã được người bán cập nhật thay đổi. Vui lòng kiểm tra lại giỏ hàng.",
            51106 => "Có sản phẩm trong giỏ không còn khả dụng hoặc không thuộc cửa hàng này.",
            51107 => "Cửa hàng bán sản phẩm này hiện đang tạm ngừng hoạt động.",
            51108 => "Chủ cửa hàng đang bị khóa tài khoản, không thể tạo đơn mới.",
            51120 => "Chưa chọn sản phẩm nào trong giỏ hàng.",
            51201 => "Đơn hàng không tồn tại trên hệ thống.",
            51202 => "Tài khoản không hợp lệ hoặc đã bị khóa.",
            51203 => "Không thể chuyển trạng thái đơn hàng này.",
            51204 => "Bạn không có quyền thao tác trên đơn hàng này.",
            51205 => "Chỉ chủ cửa hàng mới có quyền xác nhận và giao hàng.",
            51206 => "Chỉ người mua mới có quyền xác nhận hoàn tất đơn hàng.",
            51207 => "Thao tác trên đơn hàng bắt buộc phải nhập lý do cụ thể.",
            51208 => "Người mua chỉ được hủy đơn khi shop chưa giao hàng.",
            51209 => "Người bán chỉ được hủy đơn khi chưa giao hàng.",
            >= 50000 and <= 53999 => ex.Message,
            2601 or 2627 => "Dữ liệu đã tồn tại hoặc đã có người khác thao tác trước bạn. Vui lòng tải lại trang.",
            1205 => "Hệ thống đang bận xử lý giao dịch khác, vui lòng thử lại sau giây lát.",
            _ => "Có lỗi khi xử lý dữ liệu cơ sở dữ liệu. Vui lòng thử lại sau."
        };
    }

    public static string ThongBao(Exception ex)
    {
        if (ex is SqlException sqlEx) return ThongBao(sqlEx);
        return !string.IsNullOrWhiteSpace(ex.Message) && !ex.Message.Contains("Microsoft.") && !ex.Message.Contains("System.")
            ? ex.Message
            : "Đã xảy ra lỗi trong quá trình xử lý dữ liệu. Vui lòng thử lại sau.";
    }
}
