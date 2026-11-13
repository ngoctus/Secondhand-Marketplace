using Microsoft.AspNetCore.Http;

namespace SecondHandMarketplace.Web.Services.Areas;

/// <summary>Một tỉnh/thành để đổ vào dropdown.</summary>
public sealed record TinhThanhItem(int Ma, string Ten);

/// <summary>Một xã/phường để đổ vào dropdown.</summary>
public sealed record PhuongXaItem(int Ma, string Ten);

/// <summary>Danh mục khu vực (tỉnh/thành → xã/phường) và khu vực người dùng đã chọn (lưu cookie).</summary>
public interface IKhuVucService
{
    Task<IReadOnlyList<TinhThanhItem>> GetTinhThanhAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PhuongXaItem>> GetPhuongXaTheoTinhAsync(int maTinh, CancellationToken cancellationToken = default);

    /// <summary>Xã có thuộc đúng tỉnh hay không (dùng chung cho form shop và cookie).</summary>
    Task<bool> PhuongXaThuocTinhAsync(int maTinh, int maXa, CancellationToken cancellationToken = default);

    /// <summary>
    /// Đọc khu vực đã chọn từ cookie. Cookie rác, tỉnh không tồn tại hoặc xã không thuộc tỉnh thì trả (null, null).
    /// Xã có thể null khi người dùng chỉ chọn tỉnh ("Tất cả" xã).
    /// </summary>
    Task<(int? MaTinh, int? MaXa)> GetKhuVucDaChonAsync(HttpContext httpContext, CancellationToken cancellationToken = default);

    /// <summary>Kiểm tra rồi ghi cookie. Trả false (không ghi gì) nếu tỉnh/xã không hợp lệ. maTinh null = xóa lựa chọn.</summary>
    Task<bool> LuuKhuVucDaChonAsync(HttpContext httpContext, int? maTinh, int? maXa, CancellationToken cancellationToken = default);
}
