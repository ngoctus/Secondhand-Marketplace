namespace SecondHandMarketplace.Web.Services.Auctions;

public static class AuctionRules
{
    /// <summary>Lượt đầu: từ giá khởi điểm. Các lượt sau: giá hiện tại + bước giá.</summary>
    public static decimal GiaToiThieu(int soLuotDat, decimal giaKhoiDiem, decimal giaHienTai, decimal buocGia) =>
        soLuotDat == 0 ? giaKhoiDiem : giaHienTai + buocGia;

    /// <summary>Trạng thái hiển thị: thêm ENDING = đã hết giờ nhưng service nền chưa chốt kết quả.</summary>
    public static string TrangThaiHienThi(string trangThai, DateTime batDau, DateTime ketThuc, DateTime nowUtc)
    {
        if (trangThai is "ENDED" or "CANCELLED") return trangThai;
        if (nowUtc >= ketThuc) return "ENDING";
        return nowUtc >= batDau ? "RUNNING" : "SCHEDULED";
    }
}