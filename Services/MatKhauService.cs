using BCrypt.Net;

namespace SecondHandMarketplace.Web.Services;

public sealed class MatKhauService : IMatKhauService
{
    public string Bam(string matKhau) =>
        BCrypt.Net.BCrypt.HashPassword(matKhau);

    public bool KiemTra(string matKhau, string matKhauBam)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(matKhau, matKhauBam);
        }
        catch (SaltParseException)
        {
            // Chuỗi trong DB không phải hash BCrypt hợp lệ -> coi như sai mật khẩu
            return false;
        }
    }
}
