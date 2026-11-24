using Microsoft.Extensions.Configuration;

namespace SecondHandMarketplace.Web.Services.Ai;

/// <summary>
/// Cấu hình kỹ thuật của Chatbox AI, đọc từ section "Ai" (appsettings / User Secrets / biến môi trường).
/// Khóa API (Ai:ApiKey) CHỈ được đặt trong User Secrets hoặc biến môi trường Ai__ApiKey, không bao giờ commit.
/// Đọc trực tiếp từ IConfiguration (luôn có sẵn trong DI) nên không cần đăng ký thêm.
/// Các giá trị Bật/tắt, Model, System Prompt do Admin chỉnh nằm trong database (xem <see cref="IAiSettingsService"/>);
/// giá trị ở đây chỉ là mặc định khi database chưa có cấu hình.
/// </summary>
public sealed class AiOptions
{
    public const string SectionName = "Ai";
    public const string DefaultEndpoint = "https://generativelanguage.googleapis.com/v1beta/models";

    public static readonly IReadOnlyList<string> DefaultAllowedModels =
        new[] { "gemini-3.1-flash-lite", "gemini-3.8-flash", "gemini-3.6-flash", "gemini-3.5-flash" };

    /// <summary>System Prompt mặc định khi database chưa có cấu hình; giữ đồng bộ với database/20_cauhinh_ai.sql.</summary>
    public const string DefaultSystemPromptText =
        "Bạn là trợ lý AI của Re:market - sàn mua bán đồ đã qua sử dụng theo khu vực, với ba giá trị cốt lõi: Gần bạn, Đáng tin cậy và Dễ giao dịch. " +
        "Hãy trả lời bằng tiếng Việt, thân thiện, ngắn gọn và trung thực. " +
        "Khi tư vấn đồ cũ, luôn nhắc người mua hỏi kỹ phụ kiện đi kèm, thời gian đã sử dụng và tình trạng xước xát, hư hỏng thực tế; không cam kết thay cho người bán. " +
        "AN TOÀN VÀ RIÊNG TƯ: Tuyệt đối không tiết lộ, không yêu cầu và không suy đoán số điện thoại, địa chỉ nhà riêng hay số tài khoản ngân hàng của bất kỳ ai. " +
        "Không khuyên người dùng chuyển tiền trước cho người lạ; với giao dịch trực tiếp, hãy khuyên hẹn ở nơi công cộng đông người và kiểm tra kỹ hàng trước khi thanh toán. " +
        "QUY TẮC CÔNG CỤ: Bắt buộc gọi search_products khi người dùng muốn tìm đồ; gọi recommend_products khi cần gợi ý hoặc so sánh theo nhu cầu, ngân sách; " +
        "gọi draft_product_description khi người bán nhờ soạn mô tả đăng bán. " +
        "Chỉ dùng thông tin thật do công cụ trả về: không tự bịa ra sản phẩm, giá, thông số, tình trạng hay cửa hàng. Nếu không có kết quả, nói rõ là hiện chưa có. " +
        "Khi giới thiệu sản phẩm, nêu đúng giá VND và kèm liên kết dạng [Tên sản phẩm](/Products/Details/{id}). " +
        "Chỉ trao đổi các nội dung liên quan đến mua bán trên Re:market.";

    /// <summary>Khóa API chính của Chatbox AI (và dùng cho kiểm duyệt nếu không đặt ModerationApiKey). Rỗng nghĩa là chưa cấu hình.</summary>
    public string ApiKey { get; init; } = string.Empty;

    /// <summary>Khóa API riêng cho AI kiểm duyệt sản phẩm (C-01), tách quota khỏi Chatbox AI.</summary>
    public string ModerationApiKey { get; init; } = string.Empty;

    /// <summary>Khóa API dự phòng chung (auto-failover khi key chính gặp 429 hoặc lỗi xác thực).</summary>
    public string FallbackApiKey { get; init; } = string.Empty;

    public string Endpoint { get; init; } = DefaultEndpoint;


    /// <summary>Model dùng khi database chưa có cấu hình hoặc model trong database không còn hợp lệ.</summary>
    public string DefaultModel { get; init; } = DefaultAllowedModels[0];

    /// <summary>Danh sách model Admin được phép chọn (cũng là danh sách duy nhất được gửi lên nhà cung cấp).</summary>
    public IReadOnlyList<string> AllowedModels { get; init; } = DefaultAllowedModels;

    public bool DefaultEnabled { get; init; } = true;
    public string DefaultSystemPrompt { get; init; } = DefaultSystemPromptText;

    public int MaxTokens { get; init; } = 1024;

    /// <summary>Tổng thời gian tối đa cho một lượt chat (gồm mọi lần gọi AI và truy vấn dữ liệu).</summary>
    public int TimeoutSeconds { get; init; } = 50;

    /// <summary>Số vòng gọi công cụ tối đa trong một lượt chat (chống vòng lặp vô hạn).</summary>
    public int MaxToolRounds { get; init; } = 3;

    // --- Cấu hình riêng cho AI kiểm duyệt sản phẩm (C-01, độc lập với cờ bật/tắt Chatbox) ---

    /// <summary>Bật/tắt Tầng 2 LLM cho kiểm duyệt sản phẩm. Độc lập với CauHinhAI.DangBat của Chatbox.</summary>
    public bool ModerationEnabled { get; init; } = true;

    /// <summary>Model mặc định dùng cho Tầng 2 kiểm duyệt sản phẩm.</summary>
    public string ModerationModel { get; init; } = DefaultAllowedModels[0];

    /// <summary>Thời gian chờ tối đa (giây) cho một lượt gọi AI kiểm duyệt trước khi fallback về keyword-v1.</summary>
    public int ModerationTimeoutSeconds { get; init; } = 15;

    /// <summary>Số ảnh đại diện tối đa gửi kèm LLM mỗi lượt kiểm duyệt (0 = chỉ Text).</summary>
    public int ModerationMaxImages { get; init; } = 2;

    /// <summary>
    /// Danh sách key theo thứ tự ưu tiên cho AI kiểm duyệt:
    /// Ưu tiên ModerationApiKey (nếu có, nếu trống thì dùng ApiKey), sau đó auto-failover sang FallbackApiKey.
    /// </summary>
    public IReadOnlyList<string> GetModerationApiKeys()
    {
        var keys = new List<string>(2);
        var primary = !string.IsNullOrWhiteSpace(ModerationApiKey) ? ModerationApiKey : ApiKey;
        if (!string.IsNullOrWhiteSpace(primary))
            keys.Add(primary.Trim());
        if (!string.IsNullOrWhiteSpace(FallbackApiKey))
        {
            var fallback = FallbackApiKey.Trim();
            if (!keys.Any(k => string.Equals(k, fallback, StringComparison.Ordinal)))
                keys.Add(fallback);
        }
        return keys;
    }

    public static AiOptions FromConfiguration(IConfiguration configuration)
    {
        var section = configuration.GetSection(SectionName);

        var allowed = section.GetSection("AllowedModels").GetChildren()
            .Select(x => x.Value?.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .Select(x => x!)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (allowed.Count == 0) allowed.AddRange(DefaultAllowedModels);

        var defaultModel = section["DefaultModel"]?.Trim();
        if (string.IsNullOrEmpty(defaultModel) || !allowed.Contains(defaultModel))
            defaultModel = allowed[0];

        var moderationModel = section["ModerationModel"]?.Trim();
        if (string.IsNullOrEmpty(moderationModel))
            moderationModel = defaultModel;

        var endpoint = section["Endpoint"]?.Trim();
        if (string.IsNullOrEmpty(endpoint) || !Uri.TryCreate(endpoint, UriKind.Absolute, out _))
            endpoint = DefaultEndpoint;

        return new AiOptions
        {
            ApiKey = section["ApiKey"]?.Trim() ?? string.Empty,
            ModerationApiKey = section["ModerationApiKey"]?.Trim() ?? string.Empty,
            FallbackApiKey = section["FallbackApiKey"]?.Trim() ?? string.Empty,
            Endpoint = endpoint,

            DefaultModel = defaultModel,
            AllowedModels = allowed,
            DefaultEnabled = !bool.TryParse(section["DefaultEnabled"], out var enabled) || enabled,
            DefaultSystemPrompt = string.IsNullOrWhiteSpace(section["DefaultSystemPrompt"]) ? DefaultSystemPromptText : section["DefaultSystemPrompt"]!.Trim(),
            MaxTokens = ReadInt(section["MaxTokens"], 1024, 128, 4096),
            TimeoutSeconds = ReadInt(section["TimeoutSeconds"], 50, 5, 120),
            MaxToolRounds = ReadInt(section["MaxToolRounds"], 3, 1, 6),
            ModerationEnabled = !bool.TryParse(section["ModerationEnabled"], out var modEnabled) || modEnabled,
            ModerationModel = moderationModel,
            ModerationTimeoutSeconds = ReadInt(section["ModerationTimeoutSeconds"], 15, 3, 60),
            ModerationMaxImages = ReadInt(section["ModerationMaxImages"], 2, 0, 4)
        };
    }

    private static int ReadInt(string? raw, int fallback, int min, int max) =>
        int.TryParse(raw, out var value) ? Math.Clamp(value, min, max) : fallback;
}
