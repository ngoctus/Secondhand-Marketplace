namespace SecondHandMarketplace.Web.Models.ViewModels.Cart;

/// <summary>Thông tin một món hàng trong giỏ.</summary>
public class CartItemViewModel
{
    public int MaGioHang { get; set; }
    public int MaSanPham { get; set; }
    public string TenSanPham { get; set; } = string.Empty;
    public decimal Gia { get; set; }
    public string? AnhDaiDien { get; set; }
    public bool DaChon { get; set; }
    public bool CoTheMua { get; set; }
    public string TrangThaiSanPham { get; set; } = string.Empty;
    public int MaCuaHang { get; set; }
    public string TenShop { get; set; } = string.Empty;
}

/// <summary>Nhóm các món hàng theo từng Cửa hàng.</summary>
public class CartShopGroupViewModel
{
    public int MaCuaHang { get; set; }
    public string TenShop { get; set; } = string.Empty;
    public List<CartItemViewModel> Items { get; set; } = new();
    public decimal TongTienShop => Items.Where(x => x.DaChon && x.CoTheMua).Sum(x => x.Gia);
}

/// <summary>Mô hình dữ liệu toàn bộ trang Giỏ hàng.</summary>
public class CartIndexViewModel
{
    public List<CartShopGroupViewModel> DanhSachNhomShop { get; set; } = new();
    public decimal TongTienTamTinh => DanhSachNhomShop.Sum(g => g.TongTienShop);
    public int TongSoMonChon => DanhSachNhomShop.SelectMany(g => g.Items).Count(x => x.DaChon && x.CoTheMua);
}
