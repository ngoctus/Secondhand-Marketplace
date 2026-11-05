namespace SecondHandMarketplace.Web.Common;

/// <summary>Luật chuyển trạng thái đơn ở phía C#, khớp fn_ChuyenTrangThaiDonHangHopLe trong DB (DB vẫn là nơi chốt cuối).</summary>
public static class OrderRules
{
    private static readonly Dictionary<string, string[]> Next = new()
    {
        ["PENDING"] = new[] { "CONFIRMED", "CANCELLED" },
        ["CONFIRMED"] = new[] { "SHIPPING", "CANCELLED" },
        ["SHIPPING"] = new[] { "DELIVERED", "COMPLETED", "CANCELLED" },
        ["DELIVERED"] = new[] { "COMPLETED", "CANCELLED" },
        ["COMPLETED"] = new[] { "CANCELLED" }
    };

    public static bool HopLe(string cu, string moi) => Next.TryGetValue(cu, out var ds) && ds.Contains(moi);

    /// <summary>Bước tiếp theo người bán được làm (không gồm hủy).</summary>
    public static string[] TiepTheoChoNguoiBan(string cu) => cu switch
    {
        "PENDING" => new[] { "CONFIRMED" },
        "CONFIRMED" => new[] { "SHIPPING" },
        "SHIPPING" => new[] { "DELIVERED" },
        _ => Array.Empty<string>()
    };

    public static bool NguoiBanDuocHuy(string cu) => cu is "PENDING" or "CONFIRMED";

    public static bool NguoiMuaDuocHuy(string cu) => cu is "PENDING" or "CONFIRMED";

    public static string[] TiepTheoChoAdmin(string cu) => Next.TryGetValue(cu, out var ds) ? ds : Array.Empty<string>();

    public static string NhanHanhDong(string moi) => moi switch
    {
        "CONFIRMED" => "Xác nhận đơn",
        "SHIPPING" => "Đóng gói và giao hàng",
        "DELIVERED" => "Giao thành công",
        "COMPLETED" => "Hoàn tất đơn",
        "CANCELLED" => "Hủy đơn",
        _ => moi
    };
}
