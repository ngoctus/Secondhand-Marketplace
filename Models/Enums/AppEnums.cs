namespace SecondHandMarketplace.Web.Models.Enums;

// Tất cả enum của hệ thống nằm trong file này để dễ tìm.

public enum ProductCondition { New, LikeNew, Good, Fair, Poor }

public enum SaleType { BuyNow, Auction }

public enum DeliveryMethod { Direct, Shipping, Both }

/// <summary>PendingReview: chờ Admin duyệt. Reserved: đã có đơn. Sold: đơn COMPLETED.</summary>
public enum ProductStatus { PendingReview, Active, Reserved, Sold, Hidden, Rejected }

/// <summary>Luồng: Pending -> Confirmed -> Shipping -> Delivered -> Completed. Nhánh phụ: Cancelled.</summary>
public enum OrderStatus { Pending, Confirmed, Shipping, Delivered, Completed, Cancelled }

public enum PaymentMethod { Cod, BankTransfer }

public enum AuctionStatus { Scheduled, Running, Ended, Cancelled }

public enum ReportTargetType { Product, User, Behavior }

public enum ReportStatus { New, Investigating, Resolved, Rejected }

public enum DisputeStatus { Open, Resolved, Rejected }

public enum NotificationType { Outbid, AuctionWon, AuctionLost, AuctionEnding, Order, Chat, FavoriteAuction, Report, System }
