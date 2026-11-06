using SecondHandMarketplace.Web.Services.Moderation;

namespace SecondHandMarketplace.Web.Services.ProductReview;

public static class ProductReviewTabs
{
    public const string Pending = "pending";
    public const string Active = "active";
}

public static class ProductReviewRules
{
    public const int PageSize = 15;
    /// <summary>Khớp sp_QuyetDinhKiemDuyet: lý do từ chối tối thiểu 10 ký tự; cột LyDo nvarchar(500).</summary>
    public const int MinReasonLength = 10;
    public const int MaxReasonLength = 500;
}

public sealed record ProductListItem(
    int MaSanPham, int? MaLuotKiemDuyet, string Ten, decimal? Gia, string? TenDanhMuc, string? AnhDaiDien,
    string TrangThai, int MaCuaHang, string TenShop, int MaNguoiBan, string TenNguoiBan, string EmailNguoiBan,
    DateTime Moc,                       // chờ duyệt: thời điểm gửi; đang bán: ngày cập nhật
    string? AnalysisStatus, string? Level);

public sealed record ProductListPage(string Tab, IReadOnlyList<ProductListItem> Items, int Page, int TotalItems, int TotalPages);

public sealed record ReviewTurnRow(
    int MaLuotKiemDuyet, DateTime ThoiGianGui, string? QuyetDinh, string? LyDo, int? MaQuanTriVien, string? TenQuanTriVien,
    DateTime? ThoiGianQuyetDinh, bool QuyetDinhTuDong, string AnalysisStatus, string? Level, string? GiaiThich,
    string? LoiXuLy, bool ViolationRecorded, IReadOnlyList<KeywordMatch> Matches);

public sealed record StatusHistoryRow(
    string? TrangThaiCu, string TrangThaiMoi, string LoaiNguoiThucHien, string? TenNguoiThucHien, string? LyDo, DateTime ThoiGian);

public sealed class ProductReviewDetail
{
    public int MaSanPham { get; init; }
    public string Ten { get; init; } = "";
    public string? MoTa { get; init; }
    public decimal? Gia { get; init; }
    public string TrangThai { get; init; } = "";
    public string? LyDoGo { get; init; }
    public string? TenDanhMuc { get; init; }
    public string? TenTinhTrang { get; init; }
    public DateTime NgayTao { get; init; }
    public DateTime NgayCapNhat { get; init; }
    public int MaCuaHang { get; init; }
    public string TenShop { get; init; } = "";
    public string TrangThaiShop { get; init; } = "";
    public int MaNguoiBan { get; init; }
    public string TenNguoiBan { get; init; } = "";
    public string EmailNguoiBan { get; init; } = "";
    public string TrangThaiNguoiBan { get; init; } = "";
    public int SoViPhamNguoiBan { get; init; }
    public int NguongKhoa { get; init; }
    public IReadOnlyList<string> Anh { get; init; } = Array.Empty<string>();
    /// <summary>Kết quả C2 của lượt đang chờ (null nếu không có lượt đang chờ).</summary>
    public ModerationReview? Review { get; init; }
    public IReadOnlyList<ReviewTurnRow> Turns { get; init; } = Array.Empty<ReviewTurnRow>();
    public IReadOnlyList<StatusHistoryRow> StatusHistory { get; init; } = Array.Empty<StatusHistoryRow>();

    public bool IsPending => TrangThai == "PENDING_REVIEW" && Review is { TurnIsOpen: true };
    public bool CanApprove => IsPending && Review!.CanApprove;
    /// <summary>Từ chối thường luôn được phép khi đang chờ duyệt, trừ HIGH (đã do hệ thống xử lý).</summary>
    public bool CanReject => IsPending && Review!.Level != ModerationPolicy.LevelHigh;
    public string? ApproveBlockedReason => IsPending && !Review!.CanApprove ? Review.ApproveBlockedReason : null;
    public bool CanRemove => TrangThai == "ACTIVE";
}

public sealed record ProductDecisionResult(
    bool Success, string? Error, bool AlreadyDecided, string? NewStatus, AdminDecisionFollowUp? FollowUp, string? Warning)
{
    public static ProductDecisionResult Fail(string error, bool alreadyDecided = false) =>
        new(false, error, alreadyDecided, null, null, null);
}

public sealed record ViolationRow(
    int MaViPham, int MaNguoiBan, string TenNguoiBan, string EmailNguoiBan, string TrangThaiNguoiBan, string VaiTroNguoiBan,
    int TongViPham, int MaSanPham, string TenSanPham, int MaLuotKiemDuyet, string MucDo, string NguonGhiNhan,
    DateTime ThoiGian, int? MaQuanTriVien, string? TenQuanTriVien, IReadOnlyList<KeywordMatch> Matches);

public sealed record ViolationPage(int? SellerId, string? SellerName, IReadOnlyList<ViolationRow> Items, int Page, int TotalItems, int TotalPages, int Threshold);

/// <summary>
/// C1: kiểm duyệt sản phẩm và quản lý vi phạm (Admin). Chỉ gọi stored procedure có sẵn (sp_DuyetSanPham, sp_TuChoiSanPham,
/// sp_ChuyenTrangThaiSanPham) và IModerationService của C2; KHÔNG tự ghi vi phạm (HIGH do C2, LOW do trigger SQL khi Admin từ chối).
/// Hợp đồng context giống INotificationService: gọi sau khi mọi thay đổi chưa lưu của nơi gọi đã được SaveChanges.
/// </summary>
public interface IProductReviewService
{
    /// <summary>tab: "pending" (chờ duyệt, mặc định) hoặc "active" (đang bán, để gỡ sản phẩm vi phạm).</summary>
    Task<ProductListPage> GetListAsync(string? tab, int page, CancellationToken cancellationToken = default);

    Task<ProductReviewDetail?> GetDetailAsync(int maSanPham, CancellationToken cancellationToken = default);

    /// <summary>Duyệt: PENDING_REVIEW -> ACTIVE, không vi phạm. Chặn khi QUEUED/FAILED/chưa quét từ khóa/HIGH.</summary>
    Task<ProductDecisionResult> ApproveAsync(int adminId, int maSanPham, CancellationToken cancellationToken = default);

    /// <summary>Từ chối: PENDING_REVIEW -> REJECTED, bắt buộc lý do (10-500 ký tự). Vi phạm LOW (nếu có) do trigger SQL ghi cùng quyết định.</summary>
    Task<ProductDecisionResult> RejectAsync(int adminId, int maSanPham, string? reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gỡ sản phẩm vi phạm: ACTIVE -> REMOVED qua sp_ChuyenTrangThaiSanPham (lý do bắt buộc). Không ghi vi phạm.
    /// D5 (xử lý báo cáo) gọi hàm này với adminId của Admin đang xử lý. Có nhật ký + thông báo người bán.
    /// </summary>
    Task<ProductDecisionResult> RemoveProductAsync(int adminId, int maSanPham, string? reason, CancellationToken cancellationToken = default);

    /// <summary>Lịch sử vi phạm (mới nhất trước); sellerId = null: tất cả người bán.</summary>
    Task<ViolationPage> GetViolationsAsync(int? sellerId, int page, CancellationToken cancellationToken = default);
}
