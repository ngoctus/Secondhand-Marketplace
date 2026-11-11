using SecondHandMarketplace.Web.Services.Areas;

namespace SecondHandMarketplace.Web.Models.ViewModels.Areas;

/// <summary>Model cho partial Views/Shared/_ChonKhuVuc.cshtml (dùng ở form shop và bộ lọc).</summary>
public sealed class ChonKhuVucVm
{
    /// <summary>Tên field gửi lên cho tỉnh/thành (khớp thuộc tính của form cha để model binding và validation chạy).</summary>
    public string TenFieldTinh { get; set; } = "MaTinhThanh";

    /// <summary>Tên field gửi lên cho xã/phường.</summary>
    public string TenFieldXa { get; set; } = "MaPhuongXa";

    public int? MaTinh { get; set; }
    public int? MaXa { get; set; }

    public IReadOnlyList<TinhThanhItem> TinhThanhs { get; set; } = Array.Empty<TinhThanhItem>();

    /// <summary>Danh sách xã của tỉnh đang chọn (nạp sẵn để hiển thị lại sau khi post lỗi).</summary>
    public IReadOnlyList<PhuongXaItem> PhuongXas { get; set; } = Array.Empty<PhuongXaItem>();

    /// <summary>true: có tùy chọn "Tất cả" (bộ lọc); false: bắt buộc chọn (form shop).</summary>
    public bool ChoPhepTatCa { get; set; }

    public string NhanTinh { get; set; } = "Tỉnh/Thành phố";
    public string NhanXa { get; set; } = "Phường/Xã";
}
