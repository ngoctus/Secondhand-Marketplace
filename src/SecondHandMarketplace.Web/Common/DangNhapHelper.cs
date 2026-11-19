using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace SecondHandMarketplace.Web.Common;

/// <summary>
/// Tạo và đọc thông tin phiên đăng nhập (cookie). VaiTro là USER hoặc ADMIN,
/// nên [Authorize(Roles = "ADMIN")] hoạt động bình thường.
/// Dùng khi đăng nhập: await HttpContext.SignInAsync(DangNhapHelper.TaoPrincipal(...));
/// </summary>
public static class DangNhapHelper
{
    public static ClaimsPrincipal TaoPrincipal(int nguoiDungId, string email, string hoTen, string vaiTro, string? anhDaiDien = null, string? securityStamp = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, nguoiDungId.ToString()),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Name, hoTen),
            new(ClaimTypes.Role, vaiTro)
        };

        if (!string.IsNullOrWhiteSpace(anhDaiDien))
        {
            claims.Add(new("avatar", anhDaiDien));
        }

        if (!string.IsNullOrWhiteSpace(securityStamp))
        {
            claims.Add(new("sec_stamp", securityStamp));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }

    public static string LaySecurityStamp(string? matKhauBam) =>
        string.IsNullOrEmpty(matKhauBam) ? string.Empty : (matKhauBam.Length >= 12 ? matKhauBam[^12..] : matKhauBam);

    // Mã người dùng luôn lấy từ phiên đăng nhập, không tin dữ liệu client gửi lên.
    public static int? LayNguoiDungId(this ClaimsPrincipal user)
    {
        var giaTri = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(giaTri, out var id) ? id : null;
    }

    public static string? LayVaiTro(this ClaimsPrincipal user) =>
        user.FindFirst(ClaimTypes.Role)?.Value;

    public static bool LaAdmin(this ClaimsPrincipal user) =>
        user.IsInRole("ADMIN");
}
