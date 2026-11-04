namespace SecondHandMarketplace.Web.Models
{
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        public int MaLoi { get; set; } = 500;

        public string TieuDe { get; set; } = "Đã xảy ra lỗi";

        public string ThongDiep { get; set; } = "Hệ thống gặp sự cố. Vui lòng thử lại sau.";
    }
}
