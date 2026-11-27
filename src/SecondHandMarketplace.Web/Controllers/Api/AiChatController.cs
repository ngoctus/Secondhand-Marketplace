using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using SecondHandMarketplace.Web.Common;
using SecondHandMarketplace.Web.Common.Constants;
using SecondHandMarketplace.Web.Common.Extensions;
using SecondHandMarketplace.Web.Models.ViewModels.Ai;
using SecondHandMarketplace.Web.Services.Ai;

namespace SecondHandMarketplace.Web.Controllers.Api;

/// <summary>
/// 3 Endpoint AI tương tác với trình duyệt (cùng dùng Khóa API nhóm 1: Ai:ApiKey + FallbackApiKey):
///   1. POST /api/ai/chat               { "message": "...", "language": "vi|en" }     ->  { success, message, data: { reply } }
///   2. POST /api/ai/suggest-price      { "name": "...", "categoryId": ... }          ->  { success, message, data: { minPriceVnd, maxPriceVnd, ... } }
///   3. POST /api/ai/draft-description  { "name": "...", "conditionId": ..., ... }    ->  { success, message, data: { draft, reply } }
///   +  GET  /api/ai/status                                                           ->  { success, message, data: { enabled, chatEnabled, sellerAssistantEnabled } }
/// (AI Kiểm duyệt sản phẩm chạy nội bộ trên server qua AiModerationAnalyzer bằng Khóa API nhóm 2: Ai:ModerationApiKey.)
/// Xác thực: JWT hoặc Cookie (xem AiChatAuthorizeAttribute; Cookie cần thêm Antiforgery token).
/// Giới hạn theo tài khoản (AiRequestThrottle); mọi thông báo lỗi tự động khớp ngôn ngữ VI | EN.
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
        var lang = AiLanguage.Normalize(request?.Language);

        if (request is null || string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(KetQua.ThatBai(AiLanguage.Choose(lang,
                "Vui lòng nhập nội dung tin nhắn.",
                "Please enter a message.")));

        var (userId, role) = ReadIdentity();
        if (role.Length == 0 || string.IsNullOrWhiteSpace(userId))
            return StatusCode(StatusCodes.Status403Forbidden, KetQua.ThatBai(AiLanguage.Choose(lang,
                "Bạn không có quyền sử dụng trợ lý AI.",
                "You do not have permission to use the AI assistant.")));

        try
        {
            var gate = await _throttle.AcquireAsync(userId, role, cancellationToken);
            if (!gate.Acquired) return TooManyRequests(gate, lang);

            using var lease = gate.Lease;
            var result = await _ai.AskAsync(userId, role, request.Message, lang, cancellationToken);
            return ToActionResult(result, lang);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return StatusCode(StatusCodes.Status499ClientClosedRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi không mong đợi khi xử lý /api/ai/chat.");
            return StatusCode(StatusCodes.Status500InternalServerError,
                KetQua.ThatBai(GenericError(lang)));
        }
    }

    [HttpPost("draft-description")]
    [RequestSizeLimit(16 * 1024)]
    public async Task<IActionResult> DraftDescription([FromBody] AiDescriptionDraftRequest? request, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        var lang = AiLanguage.Normalize(request?.Language);

        if (request is null || string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(KetQua.ThatBai(AiLanguage.Choose(lang,
                "Vui lòng nhập tên sản phẩm trước khi nhờ AI viết mô tả.",
                "Please enter a product name before requesting an AI description.")));

        var (userId, role) = ReadIdentity();
        if (role.Length == 0 || string.IsNullOrWhiteSpace(userId))
            return StatusCode(StatusCodes.Status403Forbidden, KetQua.ThatBai(AiLanguage.Choose(lang,
                "Bạn không có quyền sử dụng trợ lý AI.",
                "You do not have permission to use the AI assistant.")));

        try
        {
            var gate = await _throttle.AcquireAsync(userId, role, cancellationToken);
            if (!gate.Acquired) return TooManyRequests(gate, lang);

            using var lease = gate.Lease;
            var input = new AiDescriptionDraftInput(
                request.Name,
                request.CategoryId,
                request.ConditionId,
                request.CategoryName,
                request.ConditionName,
                request.Price,
                request.CurrentDescription,
                lang);
            var result = await _ai.DraftDescriptionAsync(userId, role, input, cancellationToken);
            return ToDraftActionResult(result, lang);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return StatusCode(StatusCodes.Status499ClientClosedRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi không mong đợi khi xử lý /api/ai/draft-description.");
            return StatusCode(StatusCodes.Status500InternalServerError,
                KetQua.ThatBai(GenericError(lang)));
        }
    }

    [HttpPost("suggest-price")]
    [RequestSizeLimit(16 * 1024)]
    public async Task<IActionResult> SuggestPrice([FromBody] AiPriceSuggestRequest? request, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        var lang = AiLanguage.Normalize(request?.Language);

        if (request is null || string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(KetQua.ThatBai(AiLanguage.Choose(lang,
                "Vui lòng nhập tên sản phẩm trước khi nhờ AI tham khảo giá.",
                "Please enter a product name before requesting a price suggestion.")));

        var (userId, role) = ReadIdentity();
        if (role.Length == 0 || string.IsNullOrWhiteSpace(userId))
            return StatusCode(StatusCodes.Status403Forbidden, KetQua.ThatBai(AiLanguage.Choose(lang,
                "Bạn không có quyền sử dụng trợ lý AI.",
                "You do not have permission to use the AI assistant.")));

        try
        {
            var gate = await _throttle.AcquireAsync(userId, role, cancellationToken);
            if (!gate.Acquired) return TooManyRequests(gate, lang);

            using var lease = gate.Lease;
            var input = new AiPriceSuggestInput(
                request.Name,
                request.CategoryId,
                request.ConditionId,
                request.CategoryName,
                request.ConditionName,
                request.Description,
                lang);
            var result = await _ai.SuggestPriceAsync(userId, role, input, cancellationToken);
            return ToPriceActionResult(result, lang);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return StatusCode(StatusCodes.Status499ClientClosedRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi không mong đợi khi xử lý /api/ai/suggest-price.");
            return StatusCode(StatusCodes.Status500InternalServerError,
                KetQua.ThatBai(GenericError(lang)));
        }
    }

    /// <summary>Widget &amp; form đăng tin gọi để biết Chatbox hoặc Trợ lý bán hàng đang bật hay tắt.</summary>
    [HttpGet("status")]
    public async Task<IActionResult> Status(CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        var lang = AiLanguage.Normalize(null);
        try
        {
            var status = await _ai.GetFeatureStatusAsync(cancellationToken);
            return Ok(KetQua.ThanhCong("OK", new
            {
                enabled = status.ChatEnabled,
                chatEnabled = status.ChatEnabled,
                sellerAssistantEnabled = status.SellerAssistantEnabled
            }));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return StatusCode(StatusCodes.Status499ClientClosedRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi không mong đợi khi xử lý /api/ai/status.");
            return StatusCode(StatusCodes.Status500InternalServerError,
                KetQua.ThatBai(GenericError(lang)));
        }
    }

    private (string UserId, string Role) ReadIdentity()
    {
        var role = User.IsInRole(AppRoles.Admin) ? AppRoles.Admin
                 : User.IsInRole(AppRoles.User) ? AppRoles.User
                 : string.Empty;
        var userId = User.LayNguoiDungId()?.ToString() ?? string.Empty;
        return (userId, role);
    }

    private IActionResult TooManyRequests(AiThrottleAcquireResult gate, string lang)
    {
        var seconds = (int)Math.Ceiling(gate.RetryAfter.TotalSeconds);
        Response.Headers.RetryAfter = seconds.ToString(CultureInfo.InvariantCulture);

        var message = gate.Rejection == AiThrottleRejection.Busy
            ? AiLanguage.Choose(lang,
                "Bạn đang có quá nhiều yêu cầu đang xử lý. Vui lòng đợi vài giây rồi thử lại.",
                "You have too many requests in progress. Please wait a few seconds and try again.")
            : AiLanguage.Choose(lang,
                $"Bạn đã gửi quá nhiều yêu cầu. Vui lòng thử lại sau {FormatWait(seconds, lang)}.",
                $"You have sent too many requests. Please try again in {FormatWait(seconds, lang)}.");

        return StatusCode(StatusCodes.Status429TooManyRequests, new KetQua(false, message, new { retryAfterSeconds = seconds }));
    }

    private static string FormatWait(int seconds, string lang)
    {
        if (seconds < 60)
            return AiLanguage.Choose(lang, $"{seconds} giây", $"{seconds} seconds");
        var mins = (int)Math.Ceiling(seconds / 60.0);
        return AiLanguage.Choose(lang, $"{mins} phút", mins == 1 ? "1 minute" : $"{mins} minutes");
    }

    private static string GenericError(string lang) => AiLanguage.Choose(lang,
        "Đã xảy ra lỗi. Vui lòng thử lại sau.",
        "Something went wrong. Please try again later.");

    private IActionResult ToActionResult(AiChatResult result, string lang)
    {
        var message = result.Error ?? GenericError(lang);
        return result.Status switch
        {
            AiChatStatus.Success => Ok(KetQua.ThanhCong("OK", new { reply = result.Reply })),
            AiChatStatus.InvalidInput => BadRequest(KetQua.ThatBai(message)),
            AiChatStatus.Forbidden => StatusCode(StatusCodes.Status403Forbidden, KetQua.ThatBai(message)),
            AiChatStatus.Disabled => StatusCode(StatusCodes.Status503ServiceUnavailable,
                new KetQua(false, message, new { disabled = true })),
            AiChatStatus.NotConfigured => StatusCode(StatusCodes.Status503ServiceUnavailable, KetQua.ThatBai(message)),
            AiChatStatus.Timeout => StatusCode(StatusCodes.Status504GatewayTimeout, KetQua.ThatBai(message)),
            AiChatStatus.ProviderError or AiChatStatus.InvalidResponse =>
                StatusCode(StatusCodes.Status502BadGateway, KetQua.ThatBai(message)),
            _ => StatusCode(StatusCodes.Status500InternalServerError, KetQua.ThatBai(message))
        };
    }

    private IActionResult ToDraftActionResult(AiChatResult result, string lang)
    {
        var message = result.Error ?? GenericError(lang);
        return result.Status switch
        {
            AiChatStatus.Success => Ok(KetQua.ThanhCong("OK", new { draft = result.Reply, reply = result.Reply })),
            AiChatStatus.InvalidInput => BadRequest(KetQua.ThatBai(message)),
            AiChatStatus.Forbidden => StatusCode(StatusCodes.Status403Forbidden, KetQua.ThatBai(message)),
            AiChatStatus.Disabled => StatusCode(StatusCodes.Status503ServiceUnavailable,
                new KetQua(false, message, new { disabled = true })),
            AiChatStatus.NotConfigured => StatusCode(StatusCodes.Status503ServiceUnavailable, KetQua.ThatBai(message)),
            AiChatStatus.Timeout => StatusCode(StatusCodes.Status504GatewayTimeout, KetQua.ThatBai(message)),
            AiChatStatus.ProviderError or AiChatStatus.InvalidResponse =>
                StatusCode(StatusCodes.Status502BadGateway, KetQua.ThatBai(message)),
            _ => StatusCode(StatusCodes.Status500InternalServerError, KetQua.ThatBai(message))
        };
    }

    private IActionResult ToPriceActionResult(AiPriceSuggestionResult result, string lang)
    {
        var message = result.Error ?? GenericError(lang);
        return result.Status switch
        {
            AiChatStatus.Success => Ok(KetQua.ThanhCong("OK", result.Data)),
            AiChatStatus.InvalidInput => BadRequest(KetQua.ThatBai(message)),
            AiChatStatus.Forbidden => StatusCode(StatusCodes.Status403Forbidden, KetQua.ThatBai(message)),
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
