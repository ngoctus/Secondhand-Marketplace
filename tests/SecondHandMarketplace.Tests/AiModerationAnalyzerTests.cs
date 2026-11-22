using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using SecondHandMarketplace.Web.Services.Ai;
using SecondHandMarketplace.Web.Services.Moderation;
using Xunit;

namespace SecondHandMarketplace.Tests;

/// <summary>Kiểm thử AiOptions key routing và AiModerationAnalyzer (JSON parsing, auto-failover, image retry).</summary>
public class AiModerationAnalyzerTests
{
    private sealed class QueueHttpHandler(params Func<HttpRequestMessage, HttpResponseMessage>[] responders) : HttpMessageHandler
    {
        private int _index;
        public List<(string? Auth, string Body)> Requests { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var auth = request.Headers.TryGetValues("Authorization", out var vals) ? vals.FirstOrDefault() : null;
            var body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add((auth, body));

            var responder = responders[Math.Min(_index++, responders.Length - 1)];
            return responder(request);
        }
    }

    private static IConfiguration BuildConfig(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    private static AiModerationInput SampleInput(bool withImage = false) => new(
        ProductId: 42,
        Name: "Tai nghe Sony cũ",
        Description: "Dùng 6 tháng, đầy đủ hộp",
        Price: 1200000m,
        CategoryName: "Điện tử",
        ConditionName: "Như mới",
        KeywordLevel: "NONE",
        KeywordMatches: Array.Empty<KeywordMatch>(),
        Images: withImage
            ? new[] { new AiModerationImage("image/webp", Convert.ToBase64String(new byte[] { 1, 2, 3, 4 })) }
            : Array.Empty<AiModerationImage>());

    private static HttpResponseMessage JsonOk(string innerContentJson)
    {
        var envelope = JsonSerializer.Serialize(new
        {
            choices = new[] { new { message = new { content = innerContentJson } } }
        });
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(envelope, Encoding.UTF8, "application/json")
        };
    }

    [Fact]
    public void AiOptions_GetModerationApiKeys_PrefersDedicatedKeyThenFallback()
    {
        var cfg = BuildConfig(new()
        {
            ["Ai:ApiKey"] = "chat-key",
            ["Ai:ModerationApiKey"] = "mod-key",
            ["Ai:FallbackApiKey"] = "backup-key"
        });

        var keys = AiOptions.FromConfiguration(cfg).GetModerationApiKeys();
        Assert.Equal(new[] { "mod-key", "backup-key" }, keys);
    }

    [Fact]
    public void AiOptions_GetModerationApiKeys_FallsBackToChatKeyWhenDedicatedKeyMissing()
    {
        var cfg = BuildConfig(new()
        {
            ["Ai:ApiKey"] = "chat-key",
            ["Ai:FallbackApiKey"] = "backup-key"
        });

        var keys = AiOptions.FromConfiguration(cfg).GetModerationApiKeys();
        Assert.Equal(new[] { "chat-key", "backup-key" }, keys);
    }

    [Fact]
    public async Task Analyzer_WhenPrimaryHits429_AutoFailoversToBackupKey()
    {
        var handler = new QueueHttpHandler(
            _ => new HttpResponseMessage(HttpStatusCode.TooManyRequests) { Content = new StringContent("quota exceeded") },
            _ => JsonOk("""{"muc_rui_ro":"LOW","de_xuat":"APPROVE","giai_thich":"Hợp lệ.","dau_hieu":[]}"""));

        var cfg = BuildConfig(new()
        {
            ["Ai:ModerationApiKey"] = "primary-mod-key",
            ["Ai:FallbackApiKey"] = "shared-backup-key",
            ["Ai:ModerationModel"] = "gemini-3.1-flash-lite"
        });

        var analyzer = new AiModerationAnalyzer(new HttpClient(handler), cfg, NullLogger<AiModerationAnalyzer>.Instance);
        var result = await analyzer.AnalyzeAsync(SampleInput());

        Assert.True(result.Succeeded);
        Assert.Equal("LOW", result.RiskLevel);
        Assert.Equal("APPROVE", result.Recommendation);
        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal("Bearer primary-mod-key", handler.Requests[0].Auth);
        Assert.Equal("Bearer shared-backup-key", handler.Requests[1].Auth);
    }

    [Fact]
    public async Task Analyzer_WhenImageRequestReturns400_RetriesTextOnlyOnSameKey()
    {
        var handler = new QueueHttpHandler(
            _ => new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("invalid image") },
            _ => JsonOk("""{"muc_rui_ro":"MEDIUM","de_xuat":"NEEDS_REVIEW","giai_thich":"Cần kiểm tra mô tả.","dau_hieu":[{"nhom":"BAT_THUONG_GIA","muc_do":"MEDIUM","truong":"Gia","bang_chung":"Giá hơi thấp"}]}"""));

        var cfg = BuildConfig(new()
        {
            ["Ai:ModerationApiKey"] = "primary-mod-key",
            ["Ai:ModerationModel"] = "gemini-3.1-flash-lite"
        });

        var analyzer = new AiModerationAnalyzer(new HttpClient(handler), cfg, NullLogger<AiModerationAnalyzer>.Instance);
        var result = await analyzer.AnalyzeAsync(SampleInput(withImage: true));

        Assert.True(result.Succeeded);
        Assert.Equal("MEDIUM", result.RiskLevel);
        Assert.Equal("NEEDS_REVIEW", result.Recommendation);
        Assert.Single(result.Signals);
        Assert.Equal(2, handler.Requests.Count);
        Assert.Contains("image_url", handler.Requests[0].Body);
        Assert.DoesNotContain("image_url", handler.Requests[1].Body);
        Assert.Equal("Bearer primary-mod-key", handler.Requests[1].Auth);
    }

    [Fact]
    public async Task Analyzer_WhenDisabledOrNoKeys_ReturnsFailWithoutHttpCall()
    {
        var handler = new QueueHttpHandler(_ => throw new InvalidOperationException("Không được gọi HTTP"));
        var cfgNoKey = BuildConfig(new() { ["Ai:ModerationEnabled"] = "true" });
        var analyzerNoKey = new AiModerationAnalyzer(new HttpClient(handler), cfgNoKey, NullLogger<AiModerationAnalyzer>.Instance);

        var r1 = await analyzerNoKey.AnalyzeAsync(SampleInput());
        Assert.False(r1.Succeeded);
        Assert.Empty(handler.Requests);

        var cfgDisabled = BuildConfig(new() { ["Ai:ModerationEnabled"] = "false", ["Ai:ModerationApiKey"] = "k" });
        var analyzerDisabled = new AiModerationAnalyzer(new HttpClient(handler), cfgDisabled, NullLogger<AiModerationAnalyzer>.Instance);

        var r2 = await analyzerDisabled.AnalyzeAsync(SampleInput());
        Assert.False(r2.Succeeded);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public void ParseProviderResponse_HandlesMarkdownFenceAndRejectsInvalidJson()
    {
        var fenced = JsonSerializer.Serialize(new
        {
            choices = new[]
            {
                new
                {
                    message = new
                    {
                        content = "```json\n{\"muc_rui_ro\":\"HIGH\",\"de_xuat\":\"REJECT\",\"giai_thich\":\"Lách luật.\",\"dau_hieu\":[{\"nhom\":\"UNKNOWN\",\"muc_do\":\"HIGH\",\"truong\":\"Ten\",\"bang_chung\":\"Nghi vấn\"}]}\n```"
                    }
                }
            }
        });

        var ok = AiModerationAnalyzer.ParseProviderResponse(fenced, "gemini-3.1-flash-lite");
        Assert.True(ok.Succeeded);
        Assert.Equal("HIGH", ok.RiskLevel);
        Assert.Equal("REJECT", ok.Recommendation);
        var signal = Assert.Single(ok.Signals);
        Assert.Equal("SPAM_KHAC", signal.Group);

        var bad = AiModerationAnalyzer.ParseProviderResponse("not json", "gemini-3.1-flash-lite");
        Assert.False(bad.Succeeded);
    }
}
