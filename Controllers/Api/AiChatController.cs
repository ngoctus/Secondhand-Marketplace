using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web2Hand.Common.Constants;
using Web2Hand.Common.Extensions;
using Web2Hand.Models.ViewModels.Ai;
using Web2Hand.Services.Ai;

namespace Web2Hand.Controllers.Api;

/// <summary>API cho chatbox AI: POST /api/ai/chat  { "message": "..." }</summary>
[ApiController]
[Route("api/ai")]
[Authorize]
public class AiChatController : ControllerBase
{
    private readonly IAiChatService _ai;

    public AiChatController(IAiChatService ai) => _ai = ai;

    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] AiChatRequest request)
    {
        var role = User.IsInRole(AppRoles.Admin) ? AppRoles.Admin
                 : User.IsInRole(AppRoles.Seller) ? AppRoles.Seller
                 : AppRoles.Buyer;

        var reply = await _ai.AskAsync(User.GetUserId(), role, request.Message);
        return Ok(new { reply });
    }
}
