using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecondHandMarketplace.Web.Services.Chat;

namespace SecondHandMarketplace.Web.Controllers;

/// <summary>Endpoint JSON cho widget chat: danh sách hội thoại, lịch sử tin nhắn, bắt đầu chat, số chưa đọc.</summary>
[Authorize(Roles = AppRoles.User)]
[Route("chat")]
public sealed class ChatController(IChatService chat) : Controller
{
    [HttpGet("conversations")]
    public async Task<IActionResult> Conversations(CancellationToken ct) =>
        Ok(KetQua.ThanhCong("OK", await chat.GetConversationsAsync(User.LayNguoiDungId()!.Value, ct)));

    [HttpGet("conversations/{id:int}/messages")]
    public async Task<IActionResult> Messages(int id, long? before, int take = 30, CancellationToken ct = default)
    {
        var items = await chat.GetMessagesAsync(User.LayNguoiDungId()!.Value, id, before, take, ct);
        return items is null
            ? NotFound(KetQua.ThatBai("Không tìm thấy cuộc trò chuyện."))
            : Ok(KetQua.ThanhCong("OK", items));
    }

    [HttpPost("start")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Start([FromForm] int productId, CancellationToken ct)
    {
        var (ok, error, conv) = await chat.StartByProductAsync(User.LayNguoiDungId()!.Value, productId, ct);
        return ok ? Ok(KetQua.ThanhCong("OK", conv)) : BadRequest(KetQua.ThatBai(error ?? "Không mở được cuộc trò chuyện."));
    }

    [HttpGet("unread")]
    public async Task<IActionResult> Unread(CancellationToken ct) =>
        Ok(KetQua.ThanhCong("OK", new { count = await chat.CountUnreadAsync(User.LayNguoiDungId()!.Value, ct) }));
}