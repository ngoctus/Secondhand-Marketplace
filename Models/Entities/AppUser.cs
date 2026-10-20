namespace SecondHandMarketplace.Web.Models.Entities;

/// <summary>Người dùng (tương ứng bảng NguoiDung). Không dùng Identity, đăng nhập bằng cookie.</summary>
public class AppUser
{
    public string Id { get; set; } = string.Empty;   // TODO: đổi sang int khi chốt DB (DangNhapHelper đang dùng int)
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "USER";
    public string Status { get; set; } = "ACTIVE";
    public string? AvatarUrl { get; set; }
    public string? DefaultAddress { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
