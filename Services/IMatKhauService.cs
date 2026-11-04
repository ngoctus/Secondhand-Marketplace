namespace SecondHandMarketplace.Web.Services;

public interface IMatKhauService
{
    /// <summary>Băm mật khẩu (lưu vào cột NguoiDung.MatKhauBam).</summary>
    string Bam(string matKhau);

    /// <summary>Kiểm tra mật khẩu người dùng nhập với chuỗi đã băm trong DB.</summary>
    bool KiemTra(string matKhau, string matKhauBam);
}
