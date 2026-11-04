namespace SecondHandMarketplace.Web.Common;

/// <summary>
/// Kiểu trả về JSON chuẩn cho endpoint gọi bằng fetch: { success, message, data }.
/// Ví dụ: return Ok(KetQua.ThanhCong("Đã thêm vào giỏ", new { soLuong = 3 }));
///        return BadRequest(KetQua.ThatBai("Sản phẩm không còn khả dụng"));
/// </summary>
public sealed record KetQua(bool Success, string Message, object? Data = null)
{
    public static KetQua ThanhCong(string message = "Thành công.", object? data = null) =>
        new(true, message, data);

    public static KetQua ThatBai(string message) =>
        new(false, message);
}
