using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecondHandMarketplace.Web.Common.Constants;
using SecondHandMarketplace.Web.Common.Extensions;
using SecondHandMarketplace.Web.Models.ViewModels.Ai;
using SecondHandMarketplace.Web.Services.Ai;

namespace SecondHandMarketplace.Web.Controllers.Api;

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
        var role = User.IsInRole(AppRoles.Admin) ? AppRoles.Admin : AppRoles.User;

        var reply = await _ai.AskAsync(User.GetUserId(), role, request.Message);
        return Ok(new { reply });
    }
}
