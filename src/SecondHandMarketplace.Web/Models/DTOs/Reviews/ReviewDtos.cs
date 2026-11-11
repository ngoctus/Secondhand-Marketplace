using System.ComponentModel.DataAnnotations;

namespace SecondHandMarketplace.Web.Models.DTOs.Reviews;

public class CreateReviewRequest
{
    [Required]
    public int OrderId { get; set; }

    [Required(ErrorMessage = "Số sao đánh giá từ 1 đến 5 sao.")]
    [Range(1, 5, ErrorMessage = "Đánh giá từ 1 đến 5 sao.")]
    public int Rating { get; set; }

    [StringLength(1000, ErrorMessage = "Nhận xét không vượt quá 1000 ký tự.")]
    public string? Comment { get; set; }
}

public class ReviewResponse
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ReviewerId { get; set; }
    public string ReviewerName { get; set; } = string.Empty;
    public string? ReviewerAvatar { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}
