namespace SecondHandMarketplace.Web.Models.ViewModels.Admin;

public sealed class AdminUsersVm
{
    public IReadOnlyList<AdminUserRowVm> Items { get; set; } = [];
    public string? Search { get; set; }
    public string? Status { get; set; }
    public int Page { get; set; }
    public int TotalPages { get; set; }
    public int TotalItems { get; set; }
}

public sealed class AdminUserRowVm
{
    public int Id { get; set; }
    public string HoTen { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? SoDienThoai { get; set; }
    public string VaiTro { get; set; } = string.Empty;
    public string TrangThai { get; set; } = string.Empty;
    public string? LyDoKhoa { get; set; }
    public DateTime NgayTao { get; set; }
}
