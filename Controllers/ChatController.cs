using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Web2Hand.Controllers;

/// <summary>Trang chat Buyer - Seller. Gửi / nhận tin realtime nằm ở Hubs/ChatHub.cs.</summary>
[Authorize]
public class ChatController : Controller
{
    // TODO: Index (danh sách hội thoại), Conversation(userId) (lịch sử tin nhắn)
    public IActionResult Index() => View();
}
