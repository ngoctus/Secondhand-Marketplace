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
    public const string DefaultEndpoint = "https://api.anthropic.com/v1/messages";

    public static readonly IReadOnlyList<string> DefaultAllowedModels =
        new[] { "claude-3-5-haiku-20241022", "claude-3-5-sonnet-20241022", "claude-3-opus-20240229" };

    public const string DefaultSystemPromptText =
        "Bạn là trợ lý của Re:market, sàn mua bán đồ đã qua sử dụng. " +
        "Hãy trả lời bằng tiếng Việt, ngắn gọn, thân thiện. " +
        "Bạn giúp người dùng tìm sản phẩm, gợi ý sản phẩm theo nhu cầu và soạn mô tả sản phẩm.";

    /// <summary>Khóa API của nhà cung cấp AI. Rỗng nghĩa là chưa cấu hình.</summary>
    public string ApiKey { get; init; } = string.Empty;

    public string Endpoint { get; init; } = DefaultEndpoint;
    public string AnthropicVersion { get; init; } = "2023-06-01";

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

        var endpoint = section["Endpoint"]?.Trim();
        if (string.IsNullOrEmpty(endpoint) || !Uri.TryCreate(endpoint, UriKind.Absolute, out _))
            endpoint = DefaultEndpoint;

        return new AiOptions
        {
            ApiKey = section["ApiKey"]?.Trim() ?? string.Empty,
            Endpoint = endpoint,
            AnthropicVersion = string.IsNullOrWhiteSpace(section["AnthropicVersion"]) ? "2023-06-01" : section["AnthropicVersion"]!.Trim(),
            DefaultModel = defaultModel,
            AllowedModels = allowed,
            DefaultEnabled = !bool.TryParse(section["DefaultEnabled"], out var enabled) || enabled,
            DefaultSystemPrompt = string.IsNullOrWhiteSpace(section["DefaultSystemPrompt"]) ? DefaultSystemPromptText : section["DefaultSystemPrompt"]!.Trim(),
            MaxTokens = ReadInt(section["MaxTokens"], 1024, 128, 4096),
            TimeoutSeconds = ReadInt(section["TimeoutSeconds"], 50, 5, 120),
            MaxToolRounds = ReadInt(section["MaxToolRounds"], 3, 1, 6)
        };
    }

    private static int ReadInt(string? raw, int fallback, int min, int max) =>
        int.TryParse(raw, out var value) ? Math.Clamp(value, min, max) : fallback;
}
