using Microsoft.Data.SqlClient;

namespace SecondHandMarketplace.Web.Common;

/// <summary>Đổi SqlException hoặc Exception thành câu thông báo tiếng Việt thân thiện.</summary>
public static class SqlLoi
{
    public static string ThongBao(SqlException ex)
    {
        return ex.Number switch
        {
            50030 => "Chỉ tài khoản USER đang hoạt động mới có giỏ hàng.",
            50031 => "Không thể thêm sản phẩm của chính mình vào giỏ hàng.",
            50032 => "Sản phẩm không còn bán hoặc không tồn tại.",
            51101 => "Tài khoản người mua không hợp lệ hoặc đã bị khóa.",
            51102 => "Sản phẩm không tồn tại hoặc đã bị gỡ.",
            51103 => "Không có sản phẩm hợp lệ nào được chọn trong giỏ hàng.",
            51104 => "Cửa hàng hoặc sản phẩm không tồn tại.",
            51105 => "Bạn không thể đặt mua sản phẩm của chính cửa hàng mình.",
            51106 => "Có sản phẩm không còn bán (đã bán hoặc đã được người khác đặt trước) hoặc không thuộc cửa hàng này.",
            51107 => "Cửa hàng bán sản phẩm này hiện đang tạm ngừng hoạt động hoặc bị khóa.",
            51108 => "Chủ cửa hàng đang bị khóa tài khoản, không thể tạo đơn mới.",
            51109 => "Phí vận chuyển không hợp lệ.",
            51110 => "Phương thức thanh toán không hợp lệ.",
            51111 => "Chưa nhập tên người nhận hàng.",
            51112 => "Số điện thoại người nhận không hợp lệ (phải từ 9 đến 15 chữ số).",
            51113 => "Chưa nhập địa chỉ giao hàng hoặc điểm hẹn giao dịch.",
            51114 => "Đơn hàng phải có ít nhất 1 sản phẩm.",
            51115 => "Hình thức nhận hàng không hợp lệ (chỉ nhận GIAO_HANG hoặc GAP_TRUC_TIEP).",
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
