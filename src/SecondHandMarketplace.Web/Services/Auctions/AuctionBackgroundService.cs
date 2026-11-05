using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SecondHandMarketplace.Web.Data;

namespace SecondHandMarketplace.Web.Services.Auctions;

/// <summary>
/// Chạy nền: chuyển phiên SCHEDULED -> RUNNING khi tới giờ, và chốt các phiên hết giờ
/// (sp_KetThucPhienDauGia: chọn người thắng, tạo đơn, gửi thông báo). Chu kỳ chỉnh bằng Auction:SweepSeconds (mặc định 30 giây).
/// </summary>
public sealed class AuctionBackgroundService(IServiceScopeFactory scopes, IConfiguration config, ILogger<AuctionBackgroundService> log)
    : BackgroundService
{
    private const int ToiDaLanThu = 3;
    private readonly Dictionary<int, int> _soLanLoi = new();
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var giay = Math.Max(10, config.GetValue("Auction:SweepSeconds", 60));
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);   // chờ app khởi động xong
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(giay));
            do
            {
                try { await XuLyAsync(stoppingToken); }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    log.LogError(ex, "Lỗi khi quét phiên đấu giá.");
                }
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) { /* tắt ứng dụng */ }
    }

    private async Task XuLyAsync(CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.ExecuteSqlRawAsync(
            "UPDATE PhienDauGia SET TrangThai = 'RUNNING', NgayCapNhat = SYSUTCDATETIME() " +
            "WHERE TrangThai = 'SCHEDULED' AND ThoiGianBatDau <= SYSUTCDATETIME()", ct);

        var now = DateTime.UtcNow;
        var den = await db.PhienDauGias.AsNoTracking()
            .Where(p => (p.TrangThai == "SCHEDULED" || p.TrangThai == "RUNNING") && p.ThoiGianKetThuc <= now)
            .Select(p => p.MaPhienDauGia).ToListAsync(ct);

        foreach (var id in den)
        {
            try
            {
                await db.Database.ExecuteSqlRawAsync("EXEC dbo.sp_KetThucPhienDauGia @MaPhienDauGia",
                    new object[] { new SqlParameter("@MaPhienDauGia", id) }, ct);
                _soLanLoi.Remove(id);
                log.LogInformation("Đã chốt phiên đấu giá {Id}.", id);
            }
            catch (SqlException ex)
            {
                var lan = _soLanLoi.GetValueOrDefault(id) + 1;
                _soLanLoi[id] = lan;
                log.LogError(ex, "Không chốt được phiên đấu giá {Id} (lần {Lan}/{Max}).", id, lan, ToiDaLanThu);

                if (lan >= ToiDaLanThu)
                {
                    try
                    {
                        await db.Database.ExecuteSqlRawAsync("EXEC dbo.sp_HuyPhienDauGiaHeThong @MaPhienDauGia, @LyDo",
                            new object[]
                            {
                                new SqlParameter("@MaPhienDauGia", id),
                                new SqlParameter("@LyDo", "Hệ thống không thể chốt đơn cho phiên này nên phiên được hủy để bảo vệ quyền lợi các bên.")
                            }, ct);
                        _soLanLoi.Remove(id);
                        log.LogWarning("Đã hủy phiên đấu giá {Id} sau {Max} lần chốt lỗi.", id, ToiDaLanThu);
                    }
                    catch (SqlException ex2)
                    {
                        log.LogError(ex2, "Không hủy được phiên đấu giá lỗi {Id}.", id);
                    }
                }
            }
        }
    }
}