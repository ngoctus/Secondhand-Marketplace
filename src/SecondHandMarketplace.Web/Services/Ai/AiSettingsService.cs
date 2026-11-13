using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SecondHandMarketplace.Web.Data;

namespace SecondHandMarketplace.Web.Services.Ai;

/// <summary>
/// Lưu cấu hình Chatbox trong bảng dbo.CauHinhAI (một dòng duy nhất, MaCauHinhAI = 1; cấu trúc đề xuất xem
/// docs/c3-chatbox-ai-integration.md).
/// GHI CHÚ PHỐI HỢP VỚI A: Entity và DbSet cho bảng này chưa có (thuộc phạm vi A), nên service dùng SQL tham số hóa
/// qua AppDbContext.Database thay vì Entity. Khi A đã thêm Entity CauHinhAI + DbSet, chỉ cần thay phần truy vấn
/// trong hai hàm GetAsync/SaveAsync; interface và các nơi gọi không đổi.
/// </summary>
public sealed class AiSettingsService : IAiSettingsService
{
    public const int PromptMaxLength = 4000;
    private const int SingletonId = 1;
    private const int ModelMaxLength = 100;

    // Số lỗi SQL Server: 207 = tên cột không hợp lệ, 208 = tên bảng không hợp lệ (bảng chưa được tạo).
    private static readonly int[] MissingSchemaErrors = { 207, 208 };

    private readonly AppDbContext _db;
    private readonly AiOptions _options;
    private readonly ILogger<AiSettingsService> _logger;

    public AiSettingsService(AppDbContext db, IConfiguration configuration, ILogger<AiSettingsService> logger)
    {
        _db = db;
        _options = AiOptions.FromConfiguration(configuration);
        _logger = logger;
    }

    public IReadOnlyList<string> AllowedModels => _options.AllowedModels;

    public async Task<AiSettings> GetAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var rows = await _db.Database
                .SqlQuery<AiSettingsRow>($"SELECT DangBat, TenModel, SystemPrompt, NgayCapNhat FROM dbo.CauHinhAI WHERE MaCauHinhAI = {SingletonId}")
                .ToListAsync(cancellationToken);

            var row = rows.FirstOrDefault();
            if (row is null)
            {
                _logger.LogWarning("Bảng CauHinhAI chưa có dòng cấu hình (MaCauHinhAI = {Id}); dùng giá trị mặc định.", SingletonId);
                return AiSettings.FromDefaults(_options);
            }

            return new AiSettings(row.DangBat, row.TenModel, row.SystemPrompt,
                DateTime.SpecifyKind(row.NgayCapNhat, DateTimeKind.Utc), true);
        }
        catch (SqlException ex) when (MissingSchemaErrors.Contains(ex.Number))
        {
            _logger.LogWarning("Bảng CauHinhAI chưa tồn tại hoặc sai cấu trúc (SQL {Number}); dùng giá trị mặc định.", ex.Number);
            return AiSettings.FromDefaults(_options);
        }
    }

    public async Task<(bool Success, string? Error)> SaveAsync(bool enabled, string model, string systemPrompt,
        int? adminUserId, CancellationToken cancellationToken = default)
    {
        model = (model ?? string.Empty).Trim();
        systemPrompt = (systemPrompt ?? string.Empty).Trim();

        // Chỉ nhận model nằm trong danh sách cho phép: không để giá trị tùy ý đi tới nhà cung cấp AI.
        if (!_options.AllowedModels.Contains(model, StringComparer.Ordinal) || model.Length > ModelMaxLength)
            return (false, "Model AI không hợp lệ.");
        if (systemPrompt.Length == 0)
            return (false, "System Prompt không được để trống.");
        if (systemPrompt.Length > PromptMaxLength)
            return (false, $"System Prompt tối đa {PromptMaxLength} ký tự.");

        try
        {
            await _db.Database.ExecuteSqlInterpolatedAsync(
                $@"IF EXISTS (SELECT 1 FROM dbo.CauHinhAI WHERE MaCauHinhAI = {SingletonId})
                   BEGIN
                       UPDATE dbo.CauHinhAI
                       SET DangBat = {enabled}, TenModel = {model}, SystemPrompt = {systemPrompt},
                           MaNguoiCapNhat = {adminUserId}, NgayCapNhat = SYSUTCDATETIME()
                       WHERE MaCauHinhAI = {SingletonId};
                   END
                   ELSE
                   BEGIN
                       INSERT INTO dbo.CauHinhAI (MaCauHinhAI, DangBat, TenModel, SystemPrompt, MaNguoiCapNhat)
                       VALUES ({SingletonId}, {enabled}, {model}, {systemPrompt}, {adminUserId});
                   END", cancellationToken);

            return (true, null);
        }
        catch (SqlException ex) when (MissingSchemaErrors.Contains(ex.Number))
        {
            _logger.LogError("Không lưu được cấu hình AI: bảng CauHinhAI chưa tồn tại hoặc sai cấu trúc (SQL {Number}).", ex.Number);
            return (false, "Chưa lưu được: database chưa có bảng cấu hình AI (CauHinhAI). Vui lòng liên hệ người phụ trách database.");
        }
        catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
        {
            _logger.LogWarning(ex, "Xung đột ghi đồng thời khi lưu cấu hình AI, đang thử lại...");
            try
            {
                await _db.Database.ExecuteSqlInterpolatedAsync(
                    $@"UPDATE dbo.CauHinhAI
                       SET DangBat = {enabled}, TenModel = {model}, SystemPrompt = {systemPrompt},
                           MaNguoiCapNhat = {adminUserId}, NgayCapNhat = SYSUTCDATETIME()
                       WHERE MaCauHinhAI = {SingletonId};", cancellationToken);
                return (true, null);
            }
            catch (Exception retryEx)
            {
                _logger.LogError(retryEx, "Lỗi khi thử lại lưu cấu hình AI.");
                return (false, "Có xung đột cập nhật đồng thời, vui lòng thử lại.");
            }
        }
    }

    /// <summary>Hàng đọc từ bảng CauHinhAI. Tên thuộc tính trùng tên cột (không phải Entity, không đăng ký trong DbContext).</summary>
    public sealed class AiSettingsRow
    {
        public bool DangBat { get; set; }
        public string TenModel { get; set; } = string.Empty;
        public string SystemPrompt { get; set; } = string.Empty;
        public DateTime NgayCapNhat { get; set; }
    }
}
