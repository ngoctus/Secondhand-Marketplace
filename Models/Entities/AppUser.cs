using Microsoft.AspNetCore.Identity;

namespace Web2Hand.Models.Entities;

/// <summary>Người dùng chung cho cả 3 vai trò (Buyer / Seller / Admin phân biệt bằng Role).</summary>
public class AppUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? Description { get; set; }          // mô tả shop (Seller)
    public bool IsActive { get; set; } = true;        // Admin khóa / mở tài khoản
    public bool CanSell { get; set; } = true;         // Admin khóa quyền bán
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
