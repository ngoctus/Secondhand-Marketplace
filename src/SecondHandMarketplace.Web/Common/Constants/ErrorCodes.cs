namespace SecondHandMarketplace.Web.Common.Constants;

/// <summary>
/// Danh mục mã lỗi chuẩn hệ thống (UPPERCASE_SNAKE_CASE).
/// Tuân thủ quy ước thiết kế API Contract cho Core.
/// </summary>
public static class ErrorCodes
{
    // === CHUNG ===
    public const string Success = "SUCCESS";
    public const string ValidationError = "VALIDATION_ERROR";
    public const string NotFound = "NOT_FOUND";
    public const string Unauthorized = "UNAUTHORIZED";
    public const string Forbidden = "FORBIDDEN";
    public const string Conflict = "CONFLICT";
    public const string InternalError = "INTERNAL_ERROR";
    public const string BadRequest = "BAD_REQUEST";

    // === EPIC 1: TÀI KHOẢN / NGƯỜI DÙNG ===
    public const string UserNotFound = "USER_NOT_FOUND";
    public const string EmailExists = "EMAIL_ALREADY_EXISTS";
    public const string PhoneExists = "PHONE_ALREADY_EXISTS";
    public const string InvalidCredentials = "INVALID_CREDENTIALS";
    public const string InvalidOldPassword = "INVALID_OLD_PASSWORD";
    public const string UserInactive = "USER_INACTIVE";
    public const string UserLocked = "USER_LOCKED";

    // === EPIC 2: CỬA HÀNG & SẢN PHẨM ===
    public const string ShopNotFound = "SHOP_NOT_FOUND";
    public const string ShopExists = "SHOP_ALREADY_EXISTS";
    public const string ProductNotFound = "PRODUCT_NOT_FOUND";
    public const string CategoryNotFound = "CATEGORY_NOT_FOUND";
    public const string InvalidPrice = "INVALID_PRICE";
    public const string MissingProductImage = "MISSING_PRODUCT_IMAGE";
    public const string ImageUploadFailed = "IMAGE_UPLOAD_FAILED";

    // === EPIC 3: KIỂM DUYỆT SẢN PHẨM ===
    public const string ForbiddenKeywordDetected = "FORBIDDEN_KEYWORD_DETECTED";
    public const string ModerationRuleViolation = "MODERATION_RULE_VIOLATION";
    public const string ProductNotPending = "PRODUCT_NOT_PENDING";
    public const string ProductAlreadyModerated = "PRODUCT_ALREADY_MODERATED";
    public const string RejectionReasonRequired = "REJECTION_REASON_REQUIRED";
    public const string InvalidStatusTransition = "INVALID_STATUS_TRANSITION";

    // === EPIC 4: TÌM KIẾM & BỘ LỌC ===
    public const string InvalidFilter = "INVALID_FILTER";

    // === EPIC 5 & 6: GIỎ HÀNG & ĐƠN HÀNG ===
    public const string CartItemNotFound = "CART_ITEM_NOT_FOUND";
    public const string ProductUnavailable = "PRODUCT_UNAVAILABLE";
    public const string CannotBuyOwnProduct = "CANNOT_BUY_OWN_PRODUCT";
    public const string EmptyCart = "EMPTY_CART";
    public const string OrderNotFound = "ORDER_NOT_FOUND";
    public const string OrderCannotCancel = "ORDER_CANNOT_CANCEL";
    public const string OrderCannotUpdateStatus = "ORDER_CANNOT_UPDATE_STATUS";

    // === MỞ RỘNG: ĐẤU GIÁ (AUCTIONS) ===
    public const string AuctionNotFound = "AUCTION_NOT_FOUND";
    public const string AuctionNotRunning = "AUCTION_NOT_RUNNING";
    public const string BidTooLow = "BID_TOO_LOW";
    public const string CannotBidOwnAuction = "CANNOT_BID_OWN_AUCTION";
    public const string AlreadyLeadingBidder = "ALREADY_LEADING_BIDDER";

    // === ĐÁNH GIÁ, TRANH CHẤP & BÁO CÁO ===
    public const string ReviewNotAllowed = "REVIEW_NOT_ALLOWED";
    public const string ReportAlreadySubmitted = "REPORT_ALREADY_SUBMITTED";
    public const string DisputeNotFound = "DISPUTE_NOT_FOUND";
}
