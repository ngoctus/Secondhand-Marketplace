using Microsoft.Data.SqlClient;

namespace SecondHandMarketplace.Web.Common;

/// <summary>Đổi SqlException thành câu thông báo tiếng Việt. Lỗi THROW 5xxxx từ stored procedure đã là câu tiếng Việt sẵn.</summary>
public static class SqlLoi
{
    public static string ThongBao(SqlException ex)
    {
        if (ex.Number is >= 50000 and <= 53999) return ex.Message;
        if (ex.Number is 2601 or 2627) return "Dữ liệu đã bị thay đổi hoặc đã có người thao tác trước bạn, vui lòng tải lại trang.";
        if (ex.Number == 1205) return "Hệ thống đang bận, vui lòng thử lại sau giây lát.";
        return "Có lỗi khi xử lý dữ liệu, vui lòng thử lại.";
    }
}