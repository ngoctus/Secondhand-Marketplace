using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using SecondHandMarketplace.Web.Common;
using SecondHandMarketplace.Web.Common.Constants;
using SecondHandMarketplace.Web.Common.Extensions;
using SecondHandMarketplace.Web.Models.ViewModels.Ai;
using SecondHandMarketplace.Web.Services.Ai;

namespace SecondHandMarketplace.Web.Controllers.Api;

/// <summary>
/// API cho chatbox AI:
///   POST /api/ai/chat    { "message": "..." }  ->  { success, message, data: { reply } }
///   GET  /api/ai/status                         ->  { success, message, data: { enabled } }
/// Xác thực: JWT hoặc Cookie (xem AiChatAuthorizeAttribute; Cookie cần thêm Antiforgery token). Người chưa đăng nhập bị 401.
/// Chat bị giới hạn theo tài khoản (AiRequestThrottle): vượt giới hạn thì chờ tối đa vài chục giây, nếu phải chờ lâu hơn trả 429 + Retry-After.
/// Mọi phản hồi dùng định dạng KetQua { success, message, data } và không chứa khóa API hay chi tiết lỗi nội bộ.
/// </summary>
[ApiController]
[Route("api/ai")]
[AiChatAuthorize]
public class AiChatController : ControllerBase
{
    private readonly IAiChatService _ai;
    private readonly IAiRequestThrottle _throttle;
    private readonly ILogger<AiChatController> _logger;

    public AiChatController(IAiChatService ai, IAiRequestThrottle throttle, ILogger<AiChatController> logger)
    {
        _ai = ai;
        _throttle = throttle;
        _logger = logger;
    }

    [HttpPost("chat")]
    [RequestSizeLimit(16 * 1024)]
    public async Task<IActionResult> Chat([FromBody] AiChatRequest? request, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";

        if (request is null || string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(KetQua.ThatBai("Vui lòng nhập nội dung tin nhắn."));

        // Role và User ID lấy từ claim đã được xác thực (cookie đã mã hóa / JWT đã ký), không bao giờ từ nội dung request.
        var role = User.IsInRole(AppRoles.Admin) ? AppRoles.Admin
                 : User.IsInRole(AppRoles.User) ? AppRoles.User
                 : string.Empty;
        var userId = User.GetUserId();
        if (role.Length == 0 || string.IsNullOrWhiteSpace(userId))
            return StatusCode(StatusCodes.Status403Forbidden, KetQua.ThatBai("Bạn không có quyền sử dụng trợ lý AI."));

        try
        {
            var gate = await _throttle.AcquireAsync(userId, role, cancellationToken);
            if (!gate.Acquired) return TooManyRequests(gate);

            // using: slot đồng thời được trả lại khi xong, khi có exception và cả khi bị hủy.
            using var lease = gate.Lease;
            var result = await _ai.AskAsync(userId, role, request.Message, cancellationToken);
            return ToActionResult(result);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Client đã ngắt kết nối: không còn ai nhận phản hồi.
            return StatusCode(StatusCodes.Status499ClientClosedRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi không mong đợi khi xử lý /api/ai/chat.");
            return StatusCode(StatusCodes.Status500InternalServerError,
                KetQua.ThatBai("Đã xảy ra lỗi. Vui lòng thử lại sau."));
        }
    }

    /// <summary>Widget gọi để biết có nên hiển thị Chatbox hay không (Admin có thể tắt).</summary>
    [HttpGet("status")]
    public async Task<IActionResult> Status(CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        try
        {
            var enabled = await _ai.IsEnabledAsync(cancellationToken);
            return Ok(KetQua.ThanhCong("OK", new { enabled }));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return StatusCode(StatusCodes.Status499ClientClosedRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi không mong đợi khi xử lý /api/ai/status.");
            return StatusCode(StatusCodes.Status500InternalServerError,
                KetQua.ThatBai("Đã xảy ra lỗi. Vui lòng thử lại sau."));
        }
    }

    private IActionResult TooManyRequests(AiThrottleAcquireResult gate)
    {
        var seconds = (int)Math.Ceiling(gate.RetryAfter.TotalSeconds);
        Response.Headers.RetryAfter = seconds.ToString(CultureInfo.InvariantCulture);

        var message = gate.Rejection == AiThrottleRejection.Busy
            ? "Bạn đang có quá nhiều yêu cầu đang xử lý. Vui lòng đợi vài giây rồi thử lại."
            : $"Bạn đã gửi quá nhiều tin nhắn. Vui lòng thử lại sau {FormatWait(seconds)}.";

        return StatusCode(StatusCodes.Status429TooManyRequests, new KetQua(false, message, new { retryAfterSeconds = seconds }));
    }

    private static string FormatWait(int seconds) =>
        seconds < 60 ? $"{seconds} giây" : $"{(int)Math.Ceiling(seconds / 60.0)} phút";

    private IActionResult ToActionResult(AiChatResult result)
    {
        var message = result.Error ?? "Đã xảy ra lỗi. Vui lòng thử lại sau.";
        return result.Status switch
        {
            AiChatStatus.Success => Ok(KetQua.ThanhCong("OK", new { reply = result.Reply })),
            AiChatStatus.InvalidInput => BadRequest(KetQua.ThatBai(message)),
            AiChatStatus.Forbidden => StatusCode(StatusCodes.Status403Forbidden, KetQua.ThatBai(message)),
            // Chatbox bị Admin tắt: báo rõ để widget khóa ô nhập.
            AiChatStatus.Disabled => StatusCode(StatusCodes.Status503ServiceUnavailable,
                new KetQua(false, message, new { disabled = true })),
            AiChatStatus.NotConfigured => StatusCode(StatusCodes.Status503ServiceUnavailable, KetQua.ThatBai(message)),
            AiChatStatus.Timeout => StatusCode(StatusCodes.Status504GatewayTimeout, KetQua.ThatBai(message)),
            AiChatStatus.ProviderError or AiChatStatus.InvalidResponse =>
                StatusCode(StatusCodes.Status502BadGateway, KetQua.ThatBai(message)),
            _ => StatusCode(StatusCodes.Status500InternalServerError, KetQua.ThatBai(message))
        };
    }
}
