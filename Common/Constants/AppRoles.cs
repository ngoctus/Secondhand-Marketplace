namespace SecondHandMarketplace.Web.Common.Constants;

/// <summary>
/// Chỉ có 2 role, viết hoa đúng như cột NguoiDung.VaiTro trong DB. Luôn dùng hằng số này, không gõ chuỗi.
/// Không có role Buyer/Seller: ai cũng là USER, vừa mua vừa bán được.
/// </summary>
public static class AppRoles
{
    public const string User = "USER";
    public const string Admin = "ADMIN";
}
