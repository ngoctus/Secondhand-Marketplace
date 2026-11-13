using System.ComponentModel.DataAnnotations;

namespace SecondHandMarketplace.Web.Models.DTOs.Shops;

public class CreateShopRequest
{
    [Required(ErrorMessage = "Tên shop không được để trống.")]
    [StringLength(100, ErrorMessage = "Tên shop không quá 100 ký tự.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [StringLength(500)]
    public string? Logo { get; set; }

    [Required(ErrorMessage = "Số điện thoại liên hệ không được để trống.")]
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
    [StringLength(15)]
    public string ContactPhone { get; set; } = string.Empty;

    [StringLength(255)]
    public string? ContactAddress { get; set; }

    /// <summary>Mã tỉnh/thành (bắt buộc khi lưu shop; xem GET /KhuVuc/PhuongXa để lấy danh sách xã).</summary>
    public int? ProvinceId { get; set; }

    /// <summary>Mã phường/xã, phải thuộc tỉnh <see cref="ProvinceId"/>.</summary>
    public int? WardId { get; set; }

    [StringLength(300)]
    public string? AddressDetail { get; set; }

    [StringLength(100)]
    public string? AreaLabel { get; set; }

    /// <summary>Mã BIN VietQR của ngân hàng nhận chuyển khoản (tùy chọn, nhưng nhập thì phải đủ 3 trường ngân hàng).</summary>
    [StringLength(10)]
    public string? BankBin { get; set; }

    [StringLength(30)]
    public string? BankAccountNumber { get; set; }

    [StringLength(100)]
    public string? BankAccountHolder { get; set; }
}

public class UpdateShopRequest
{
    [Required(ErrorMessage = "Tên shop không được để trống.")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [StringLength(500)]
    public string? Logo { get; set; }

    [Required(ErrorMessage = "Số điện thoại liên hệ không được để trống.")]
    [Phone]
    [StringLength(15)]
    public string ContactPhone { get; set; } = string.Empty;

    [StringLength(255)]
    public string? ContactAddress { get; set; }
}

public class ShopResponse
{
    public int Id { get; set; }
    public int OwnerId { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Logo { get; set; }
    public string ContactPhone { get; set; } = string.Empty;
    public string? ContactAddress { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int TotalProducts { get; set; }
    public double RatingAverage { get; set; }
}
