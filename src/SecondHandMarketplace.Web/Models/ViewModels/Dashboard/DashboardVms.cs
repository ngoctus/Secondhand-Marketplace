namespace SecondHandMarketplace.Web.Models.ViewModels.Dashboard;

public sealed class TopProductVm
{
    public int ProductId { get; set; }
    public string Name { get; set; } = "";
    public string ShopName { get; set; } = "";
    public int QuantitySold { get; set; } = 1;
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

public sealed class CategoryCountVm
{
    public string Name { get; set; } = "";
    public int Count { get; set; }
}

public sealed class AdminDashboardVm
{
    public int TongNguoiDung { get; set; }
    public int NguoiDungBiKhoa { get; set; }
    public int TongCuaHang { get; set; }
    public int TongSanPham { get; set; }
    public int SanPhamDangBan { get; set; }
    public int SanPhamDaBan { get; set; }
    public int SanPhamChoDuyet { get; set; }
    public int TongDonHang { get; set; }
    public int DonHoanTat { get; set; }
    public decimal DoanhThu { get; set; }
    public string ThangHienTai { get; set; } = "";
    public decimal DoanhThuThang { get; set; }
    public int DonHoanTatThang { get; set; }
    public decimal LoiNhuanThang { get; set; }
    public decimal TongLoiNhuan { get; set; }
    public decimal TyLeLoiNhuan { get; set; } = 5m;
    public TopProductVm? SanPhamBanChayNhat { get; set; }
    public List<TopProductVm> SanPhamBanChay { get; set; } = new();
    public int? BaoCaoChuaXuLy { get; set; }      // null = chưa có bảng báo cáo (D chưa làm)
    public int TranhChapDangMo { get; set; }
    public int KhieuNaiChoXuLy { get; set; }
    public int PhienDangMo { get; set; }
    public Dictionary<string, int> DonTheoTrangThai { get; set; } = new();
    public List<CategoryCountVm> TopDanhMuc { get; set; } = new();
    public List<Orders.OrderRowVm> DonMoiNhat { get; set; } = new();
}