namespace SecondHandMarketplace.Web.Models.ViewModels.Dashboard;

public sealed class TopProductVm
{
    public int ProductId { get; set; }
    public string Name { get; set; } = "";
    public decimal Revenue { get; set; }
}

public sealed class ChartDataVm
{
    public List<string> Labels { get; set; } = new();
    public List<decimal> Revenue { get; set; } = new();
    public List<int> Orders { get; set; } = new();
}

public sealed class SellerDashboardVm
{
    public string ShopName { get; set; } = "";
    public int TongSanPham { get; set; }
    public int SanPhamDangBan { get; set; }
    public int SanPhamDaBan { get; set; }
    public int TongDon { get; set; }
    public int DonHoanTat { get; set; }
    public decimal DoanhThu { get; set; }
    public int DonChoXuLy { get; set; }
    public int PhienDangMo { get; set; }
    public Dictionary<string, int> DonTheoTrangThai { get; set; } = new();
    public List<TopProductVm> SanPhamBanChay { get; set; } = new();
}

public sealed class AdminDashboardVm
{
    public int TongNguoiDung { get; set; }
    public int TongCuaHang { get; set; }
    public int TongSanPham { get; set; }
    public int SanPhamChoDuyet { get; set; }
    public int TongDonHang { get; set; }
    public int DonHoanTat { get; set; }
    public decimal DoanhThu { get; set; }
    public int? BaoCaoChuaXuLy { get; set; }      // null = chưa có bảng báo cáo (D chưa làm)
    public int PhienDangMo { get; set; }
    public Dictionary<string, int> DonTheoTrangThai { get; set; } = new();
    public List<Orders.OrderRowVm> DonMoiNhat { get; set; } = new();
}