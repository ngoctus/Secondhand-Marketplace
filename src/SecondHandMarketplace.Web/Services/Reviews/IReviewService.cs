using SecondHandMarketplace.Web.Models.ViewModels.Reviews;

namespace SecondHandMarketplace.Web.Services.Reviews;

/// <summary>
/// Nghiệp vụ đánh giá người bán sau khi đơn hoàn tất.
/// Nguồn điểm sao duy nhất trên toàn hệ thống là view vw_DiemCuaHang — service này KHÔNG tự tính lại.
///
/// Ba nhiệm vụ chính (C2):
///   1. TaoDanhGiaAsync  – người mua gửi đánh giá (gọi sp_TaoDanhGia trên SQL Server, fallback EF trên InMemory).
///   2. LayDanhGiaTheoShopAsync – lấy danh sách đánh giá có phân trang, kèm thống kê điểm shop.
///   3. DoiTrangThaiHienThiAsync – admin ẩn/hiện đánh giá vi phạm (không tự động, không khóa shop).
///
/// Hàm phụ trợ (dùng bởi C3 – controller giao diện người mua):
///   4. KiemTraDonDaDanhGiaAsync – kiểm tra đơn đã được đánh giá chưa.
///   5. LayDiemCuaHangAsync      – đọc vw_DiemCuaHang cho một hoặc nhiều shop.
/// </summary>
public interface IReviewService
{
    /// <summary>
    /// Tạo đánh giá mới cho đơn hàng COMPLETED.
    /// Trả (true, null, MaDanhGia) khi thành công; (false, thông báo lỗi, null) khi thất bại.
    /// Mã lỗi từ sp_TaoDanhGia: 50001 đơn không tồn tại, 50002 không phải người mua,
    ///   50003 đơn chưa hoàn tất, 50004 đã đánh giá, 50005 số sao ngoài khoảng.
    /// </summary>
    Task<(bool Success, string? Error, int? MaDanhGia)> TaoDanhGiaAsync(
        int maDonHang,
        int maNguoiMua,
        byte soSao,
        string? noiDung,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy danh sách đánh giá theo shop, có phân trang và thống kê điểm từ vw_DiemCuaHang.
    /// Mặc định chỉ trả đánh giá VISIBLE (phù hợp trang shop công khai và thẻ sản phẩm).
    /// Admin dùng chiLayTatCa = true để xem cả HIDDEN.
    /// Không lộ Email hay SoDienThoai của người mua.
    /// </summary>
    Task<DanhSachDanhGiaTheoShopVm> LayDanhGiaTheoShopAsync(
        int maCuaHang,
        int page = 1,
        int pageSize = 5,
        bool chiLayTatCa = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ẩn hoặc hiện một đánh giá (chỉ Admin).
    /// TrangThaiMoi phải là "VISIBLE" hoặc "HIDDEN".
    /// Khi đánh giá bị HIDDEN, vw_DiemCuaHang tự động loại nó khỏi điểm trung bình.
    /// Trả (false, lỗi) nếu đánh giá không tồn tại hoặc trạng thái không hợp lệ.
    /// </summary>
    Task<(bool Success, string? Error)> DoiTrangThaiHienThiAsync(
        int maDanhGia,
        string trangThaiMoi,
        int adminId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Kiểm tra đơn hàng đã được đánh giá chưa.
    /// Dùng bởi C3 (controller người mua) để quyết định ẩn/hiện nút Đánh giá.
    /// </summary>
    Task<bool> KiemTraDonDaDanhGiaAsync(int maDonHang, CancellationToken cancellationToken = default);

    /// <summary>
    /// Đọc điểm trung bình và số đánh giá từ vw_DiemCuaHang cho một shop.
    /// Trả null nếu shop không tồn tại.
    /// Đây là nguồn dữ liệu cho thẻ sản phẩm (B), trang shop công khai (A5), trang rà soát (C4).
    /// </summary>
    Task<DiemCuaHangVm?> LayDiemCuaHangAsync(int maCuaHang, CancellationToken cancellationToken = default);

    /// <summary>
    /// Đọc điểm trung bình cho nhiều shop cùng lúc (tối ưu truy vấn khi hiển thị danh sách thẻ sản phẩm).
    /// Key: MaCuaHang, Value: DiemCuaHangVm.
    /// </summary>
    Task<Dictionary<int, DiemCuaHangVm>> LayDiemNhieuShopAsync(
        IEnumerable<int> maCuaHangs,
        CancellationToken cancellationToken = default);
}
