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
