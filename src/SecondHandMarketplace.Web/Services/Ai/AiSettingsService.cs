using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SecondHandMarketplace.Web.Data;

namespace SecondHandMarketplace.Web.Services.Ai;

/// <summary>
/// Lưu cấu hình hệ thống AI trong bảng dbo.CauHinhAI (một dòng duy nhất, MaCauHinhAI = 1):
/// - Cụm 1: AI Hỗ trợ người mua &amp; người bán (DangBat, TroLyBanHangDangBat, TenModel, SystemPrompt).
/// - Cụm 2: AI Kiểm duyệt sản phẩm Tầng 2 (KiemDuyetDangBat, KiemDuyetTenModel, KiemDuyetSoAnhToiDa).
/// Có fallback tương thích ngược nếu database chưa chạy migration mở rộng cột (SQL error 207).
/// </summary>
public sealed class AiSettingsService : IAiSettingsService
{
    public const int PromptMaxLength = 4000;
    public const int ModerationMaxImagesMin = 0;
    public const int ModerationMaxImagesMax = 4;

    private const int SingletonId = 1;
    private const int ModelMaxLength = 100;

    // Số lỗi SQL Server: 207 = tên cột không hợp lệ, 208 = tên bảng không hợp lệ (bảng chưa được tạo).
    private const int InvalidColumnError = 207;
    private const int InvalidObjectError = 208;
    private static readonly int[] MissingSchemaErrors = { InvalidColumnError, InvalidObjectError };

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
                .SqlQuery<AiSettingsExtendedRow>(
                    $@"SELECT DangBat, TroLyBanHangDangBat, TenModel, SystemPrompt,
                              KiemDuyetDangBat, KiemDuyetTenModel, KiemDuyetSoAnhToiDa, NgayCapNhat
                       FROM dbo.CauHinhAI
                       WHERE MaCauHinhAI = {SingletonId}")
                .ToListAsync(cancellationToken);

            var row = rows.FirstOrDefault();
            if (row is null)
            {
                _logger.LogWarning("Bảng CauHinhAI chưa có dòng cấu hình (MaCauHinhAI = {Id}); dùng giá trị mặc định.", SingletonId);
                return AiSettings.FromDefaults(_options);
            }

            return new AiSettings(
                Enabled: row.DangBat,
                Model: row.TenModel,
                SystemPrompt: row.SystemPrompt,
                UpdatedAtUtc: DateTime.SpecifyKind(row.NgayCapNhat, DateTimeKind.Utc),
                IsPersisted: true,
                SellerAssistantEnabled: row.TroLyBanHangDangBat,
                ModerationEnabled: row.KiemDuyetDangBat,
                ModerationModel: string.IsNullOrWhiteSpace(row.KiemDuyetTenModel) ? _options.ModerationModel : row.KiemDuyetTenModel,
                ModerationMaxImages: Math.Clamp(row.KiemDuyetSoAnhToiDa, ModerationMaxImagesMin, ModerationMaxImagesMax),
                HasExtendedSchema: true);
        }
        catch (SqlException ex) when (ex.Number == InvalidColumnError)
        {
            _logger.LogWarning("Bảng CauHinhAI chưa có các cột mở rộng (SQL 207); đọc schema cũ và dùng cấu hình kiểm duyệt mặc định.");
            return await ReadLegacyRowAsync(cancellationToken);
        }
        catch (SqlException ex) when (ex.Number == InvalidObjectError)
        {
            _logger.LogWarning("Bảng CauHinhAI chưa tồn tại (SQL 208); dùng giá trị mặc định.");
            return AiSettings.FromDefaults(_options);
        }
    }

    private async Task<AiSettings> ReadLegacyRowAsync(CancellationToken cancellationToken)
    {
        try
        {
            var rows = await _db.Database
                .SqlQuery<AiSettingsLegacyRow>(
                    $"SELECT DangBat, TenModel, SystemPrompt, NgayCapNhat FROM dbo.CauHinhAI WHERE MaCauHinhAI = {SingletonId}")
                .ToListAsync(cancellationToken);

            var row = rows.FirstOrDefault();
            if (row is null)
                return AiSettings.FromDefaults(_options);

            return new AiSettings(
                Enabled: row.DangBat,
                Model: row.TenModel,
                SystemPrompt: row.SystemPrompt,
                UpdatedAtUtc: DateTime.SpecifyKind(row.NgayCapNhat, DateTimeKind.Utc),
                IsPersisted: true,
                SellerAssistantEnabled: row.DangBat,
                ModerationEnabled: _options.ModerationEnabled,
                ModerationModel: _options.ModerationModel,
                ModerationMaxImages: _options.ModerationMaxImages,
                HasExtendedSchema: false);
        }
        catch (SqlException ex) when (MissingSchemaErrors.Contains(ex.Number))
        {
            _logger.LogWarning("Bảng CauHinhAI chưa tồn tại hoặc sai cấu trúc (SQL {Number}); dùng giá trị mặc định.", ex.Number);
            return AiSettings.FromDefaults(_options);
        }
    }

    public async Task<(bool Success, string? Error)> SaveAsync(AiSettingsUpdate update, int? adminUserId,
        CancellationToken cancellationToken = default)
    {
        var model = (update.Model ?? string.Empty).Trim();
        var moderationModel = (update.ModerationModel ?? string.Empty).Trim();
        var systemPrompt = (update.SystemPrompt ?? string.Empty).Trim();
        var maxImages = update.ModerationMaxImages;

        // Chỉ nhận model nằm trong danh sách cho phép: không để giá trị tùy ý đi tới nhà cung cấp AI.
        if (!_options.AllowedModels.Contains(model, StringComparer.Ordinal) || model.Length > ModelMaxLength)
            return (false, "Model AI hỗ trợ người mua & người bán không hợp lệ.");
        if (!_options.AllowedModels.Contains(moderationModel, StringComparer.Ordinal) || moderationModel.Length > ModelMaxLength)
            return (false, "Model AI kiểm duyệt không hợp lệ.");
        if (systemPrompt.Length == 0)
            return (false, "System Prompt không được để trống.");
        if (systemPrompt.Length > PromptMaxLength)
            return (false, $"System Prompt tối đa {PromptMaxLength} ký tự.");
        if (maxImages is < ModerationMaxImagesMin or > ModerationMaxImagesMax)
            return (false, $"Số ảnh gửi AI kiểm duyệt phải nằm trong khoảng {ModerationMaxImagesMin}–{ModerationMaxImagesMax}.");

        try
        {
            await SaveExtendedAsync(update.Enabled, update.SellerAssistantEnabled, model, systemPrompt,
                update.ModerationEnabled, moderationModel, maxImages, adminUserId, cancellationToken);
            return (true, null);
        }
        catch (SqlException ex) when (ex.Number == InvalidColumnError)
        {
            _logger.LogWarning("Bảng CauHinhAI thiếu cột mở rộng (SQL 207); thử tự bổ sung cột trước khi lưu.");
            try
            {
                await EnsureExtendedColumnsAsync(cancellationToken);
                await SaveExtendedAsync(update.Enabled, update.SellerAssistantEnabled, model, systemPrompt,
                    update.ModerationEnabled, moderationModel, maxImages, adminUserId, cancellationToken);
                return (true, null);
            }
            catch (Exception migrateEx)
            {
                _logger.LogError(migrateEx, "Không thể tự bổ sung cột mở rộng cho dbo.CauHinhAI.");
                return (false, "Database chưa có cột cấu hình AI mở rộng. Vui lòng chạy script database/migrations/202611_cauhinh_ai_mo_rong.sql.");
            }
        }
        catch (SqlException ex) when (ex.Number == InvalidObjectError)
        {
            _logger.LogError("Không lưu được cấu hình AI: bảng CauHinhAI chưa tồn tại (SQL 208).");
            return (false, "Chưa lưu được: database chưa có bảng cấu hình AI (CauHinhAI). Vui lòng liên hệ người phụ trách database.");
        }
        catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
        {
            _logger.LogWarning(ex, "Xung đột ghi đồng thời khi lưu cấu hình AI, đang thử lại...");
            try
            {
                await _db.Database.ExecuteSqlInterpolatedAsync(
                    $@"UPDATE dbo.CauHinhAI
                       SET DangBat = {update.Enabled},
                           TroLyBanHangDangBat = {update.SellerAssistantEnabled},
                           TenModel = {model},
                           SystemPrompt = {systemPrompt},
                           KiemDuyetDangBat = {update.ModerationEnabled},
                           KiemDuyetTenModel = {moderationModel},
                           KiemDuyetSoAnhToiDa = {maxImages},
                           MaNguoiCapNhat = {adminUserId},
                           NgayCapNhat = SYSUTCDATETIME()
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

    private Task SaveExtendedAsync(
        bool enabled,
        bool sellerAssistantEnabled,
        string model,
        string systemPrompt,
        bool moderationEnabled,
        string moderationModel,
        int moderationMaxImages,
        int? adminUserId,
        CancellationToken cancellationToken) =>
        _db.Database.ExecuteSqlInterpolatedAsync(
            $@"IF EXISTS (SELECT 1 FROM dbo.CauHinhAI WHERE MaCauHinhAI = {SingletonId})
               BEGIN
                   UPDATE dbo.CauHinhAI
                   SET DangBat = {enabled},
                       TroLyBanHangDangBat = {sellerAssistantEnabled},
                       TenModel = {model},
                       SystemPrompt = {systemPrompt},
                       KiemDuyetDangBat = {moderationEnabled},
                       KiemDuyetTenModel = {moderationModel},
                       KiemDuyetSoAnhToiDa = {moderationMaxImages},
                       MaNguoiCapNhat = {adminUserId},
                       NgayCapNhat = SYSUTCDATETIME()
                   WHERE MaCauHinhAI = {SingletonId};
               END
               ELSE
               BEGIN
                   INSERT INTO dbo.CauHinhAI (
                       MaCauHinhAI, DangBat, TroLyBanHangDangBat, TenModel, SystemPrompt,
                       KiemDuyetDangBat, KiemDuyetTenModel, KiemDuyetSoAnhToiDa, MaNguoiCapNhat)
                   VALUES (
                       {SingletonId}, {enabled}, {sellerAssistantEnabled}, {model}, {systemPrompt},
                       {moderationEnabled}, {moderationModel}, {moderationMaxImages}, {adminUserId});
               END", cancellationToken);

    private Task EnsureExtendedColumnsAsync(CancellationToken cancellationToken) =>
        _db.Database.ExecuteSqlRawAsync(
            @"IF COL_LENGTH(N'dbo.CauHinhAI', N'TroLyBanHangDangBat') IS NULL
                  ALTER TABLE dbo.CauHinhAI ADD TroLyBanHangDangBat bit NOT NULL CONSTRAINT DF_CauHinhAI_TroLyBanHangDangBat DEFAULT (1);
              IF COL_LENGTH(N'dbo.CauHinhAI', N'KiemDuyetDangBat') IS NULL
                  ALTER TABLE dbo.CauHinhAI ADD KiemDuyetDangBat bit NOT NULL CONSTRAINT DF_CauHinhAI_KiemDuyetDangBat DEFAULT (1);
              IF COL_LENGTH(N'dbo.CauHinhAI', N'KiemDuyetTenModel') IS NULL
                  ALTER TABLE dbo.CauHinhAI ADD KiemDuyetTenModel varchar(100) NOT NULL CONSTRAINT DF_CauHinhAI_KiemDuyetTenModel DEFAULT ('gemini-3.5-flash-lite');
              IF COL_LENGTH(N'dbo.CauHinhAI', N'KiemDuyetSoAnhToiDa') IS NULL
                  ALTER TABLE dbo.CauHinhAI ADD KiemDuyetSoAnhToiDa int NOT NULL CONSTRAINT DF_CauHinhAI_KiemDuyetSoAnhToiDa DEFAULT (2);",
            cancellationToken);

    /// <summary>Hàng đọc đầy đủ từ bảng CauHinhAI.</summary>
    public sealed class AiSettingsExtendedRow
    {
        public bool DangBat { get; set; }
        public bool TroLyBanHangDangBat { get; set; }
        public string TenModel { get; set; } = string.Empty;
        public string SystemPrompt { get; set; } = string.Empty;
        public bool KiemDuyetDangBat { get; set; }
        public string KiemDuyetTenModel { get; set; } = string.Empty;
        public int KiemDuyetSoAnhToiDa { get; set; }
        public DateTime NgayCapNhat { get; set; }
    }

    /// <summary>Hàng đọc tương thích ngược khi bảng CauHinhAI chưa chạy migration cột mở rộng.</summary>
    public sealed class AiSettingsLegacyRow
    {
        public bool DangBat { get; set; }
        public string TenModel { get; set; } = string.Empty;
        public string SystemPrompt { get; set; } = string.Empty;
        public DateTime NgayCapNhat { get; set; }
    }
}
