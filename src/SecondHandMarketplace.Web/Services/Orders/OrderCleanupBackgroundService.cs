using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Models.Entities;
using SecondHandMarketplace.Web.Services.BackgroundJobs;

namespace SecondHandMarketplace.Web.Services.Orders;

/// <summary>
/// Task E-02: Dịch vụ nền xử lý dọn dẹp đơn hàng tự động (OrderCleanupBackgroundService):
/// 1. Tự động hủy (CANCELLED) các đơn hàng VietQR ở trạng thái PENDING quá 24h chưa thanh toán:
///    - Giải phóng cờ giữ chỗ DangGiuCho = false trong ChiTietDonHang.
///    - Trả sản phẩm RESERVED về trạng thái ACTIVE để mở bán lại.
///    - Ghi nhận lịch sử đơn hàng và lịch sử sản phẩm (SYSTEM).
///    - Gửi thông báo cho người mua và chủ cửa hàng.
/// 2. Tự động chuyển đơn hàng DELIVERED quá 5 ngày sang COMPLETED nếu không có khiếu nại:
///    - Chuyển đơn sang COMPLETED, chốt thanh toán (nếu là COD).
///    - Chuyển sản phẩm sang SOLD.
///    - Ghi nhận lịch sử đơn hàng và lịch sử sản phẩm.
///    - Gửi thông báo cho người mua (mở quyền đánh giá) và chủ shop (nhận doanh thu/điểm).
/// </summary>
public sealed class OrderCleanupBackgroundService(
    IServiceScopeFactory scopes,
    IConfiguration config,
    IBackgroundJobLease jobLease,
    ILogger<OrderCleanupBackgroundService> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var phut = Math.Max(1, config.GetValue("OrderCleanup:IntervalMinutes", 15));
        log.LogInformation("OrderCleanupBackgroundService đã khởi chạy với chu kỳ quét {Minutes} phút/lần.", phut);

        try
        {
            // Chờ 10 giây khi ứng dụng khởi động xong rồi mới quét lần đầu
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(phut));
            do
            {
                try
                {
                    await using var lease = await jobLease.TryAcquireAsync("order-cleanup", stoppingToken);
                    if (lease is not null)
                    {
                        using var jobToken = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, lease.LeaseLost);
                        await QuetDonHangAsync(jobToken.Token);
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    log.LogError(ex, "Lỗi xảy ra trong quá trình quét dọn dẹp đơn hàng nền.");
                }
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            log.LogInformation("OrderCleanupBackgroundService đang dừng lại do ứng dụng tắt.");
        }
    }

    internal async Task QuetDonHangAsync(CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTime.UtcNow;

        // =========================================================================
        // 1. Quét hủy các đơn VietQR PENDING quá 24h chưa thanh toán
        // =========================================================================
        var han24h = now.AddHours(-24);
        var donQuaHan = await db.DonHangs
            .Include(d => d.CuaHang)
            .Include(d => d.ChiTiet)
            .Where(d => d.TrangThai == "PENDING"
                     && d.PhuongThucThanhToan == "BANK_TRANSFER"
                     && d.TrangThaiThanhToan != "PAID"
                     && d.NgayTao <= han24h)
            .ToListAsync(ct);

        var soDonHuy = 0;
        foreach (var don in donQuaHan)
        {
            don.TrangThai = "CANCELLED";
            don.NguoiHuy = "SYSTEM";
            don.LyDoHuy = "Hệ thống tự động hủy do đơn hàng VietQR chưa được thanh toán sau 24 giờ.";
            don.ThoiGianHuy = now;
            don.NgayCapNhat = now;

            var spIds = new List<int>();
            foreach (var item in don.ChiTiet)
            {
                item.DangGiuCho = false;
                spIds.Add(item.MaSanPham);
            }

            if (spIds.Count > 0)
            {
                var sanPhams = await db.SanPhams
                    .Where(s => spIds.Contains(s.MaSanPham) && s.TrangThai == "RESERVED")
                    .ToListAsync(ct);

                foreach (var sp in sanPhams)
                {
                    sp.TrangThai = "ACTIVE";
                    sp.NgayCapNhat = now;

                    db.LichSuTrangThaiSanPhams.Add(new LichSuTrangThaiSanPham
                    {
                        MaSanPham = sp.MaSanPham,
                        TrangThaiCu = "RESERVED",
                        TrangThaiMoi = "ACTIVE",
                        LoaiNguoiThucHien = "SYSTEM",
                        NguoiThucHienId = null,
                        LyDo = $"Đơn hàng #{don.MaDon} quá hạn thanh toán VietQR 24h bị hủy tự động, mở bán lại.",
                        ThoiGian = now
                    });
                }
            }

            db.LichSuTrangThaiDonHangs.Add(new LichSuTrangThaiDonHang
            {
                MaDonHang = don.MaDonHang,
                TrangThaiCu = "PENDING",
                TrangThaiMoi = "CANCELLED",
                LoaiNguoiThucHien = "SYSTEM",
                NguoiThucHienId = null,
                LyDo = "Tự động hủy do quá hạn thanh toán chuyển khoản VietQR 24 giờ.",
                ThoiGian = now
            });

            db.ThongBaos.Add(new ThongBao
            {
                MaNguoiDung = don.MaNguoiDung,
                Loai = "ORDER",
                TieuDe = "Đơn hàng đã bị hủy tự động",
                NoiDung = $"Đơn hàng #{don.MaDon} đã bị hủy do chưa hoàn tất thanh toán chuyển khoản sau 24 giờ.",
                MaThamChieu = don.MaDonHang,
                DaDoc = false,
                NgayTao = now
            });

            if (don.CuaHang != null)
            {
                db.ThongBaos.Add(new ThongBao
                {
                    MaNguoiDung = don.CuaHang.MaNguoiDung,
                    Loai = "ORDER",
                    TieuDe = "Đơn hàng đã bị hủy tự động",
                    NoiDung = $"Đơn hàng #{don.MaDon} đã bị hủy tự động do người mua không chuyển khoản sau 24 giờ. Món hàng đã được mở bán lại trên shop của bạn.",
                    MaThamChieu = don.MaDonHang,
                    DaDoc = false,
                    NgayTao = now
                });
            }

            soDonHuy++;
        }

        // =========================================================================
        // 2. Tự động chuyển đơn DELIVERED quá 5 ngày sang COMPLETED nếu không có khiếu nại
        // =========================================================================
        var han5Ngay = now.AddDays(-5);
        var donGiaoXong = await db.DonHangs
            .Include(d => d.CuaHang)
            .Include(d => d.ChiTiet)
            .Where(d => d.TrangThai == "DELIVERED"
                     && ((d.ThoiGianDaGiao != null && d.ThoiGianDaGiao <= han5Ngay) || (d.ThoiGianDaGiao == null && d.NgayCapNhat <= han5Ngay)))
            .ToListAsync(ct);

        var soDonHoanTat = 0;
        if (donGiaoXong.Count > 0)
        {
            var dsMaDonHang = donGiaoXong.Select(d => d.MaDonHang).ToList();
            var dsDonTranhChap = await db.TranhChaps
                .Where(t => dsMaDonHang.Contains(t.MaDonHang) && (t.TrangThai == "OPEN" || t.TrangThai == "PROCESSING"))
                .Select(t => t.MaDonHang)
                .Distinct()
                .ToListAsync(ct);

            var donHopLeDeHoanTat = donGiaoXong.Where(d => !dsDonTranhChap.Contains(d.MaDonHang)).ToList();

            foreach (var don in donHopLeDeHoanTat)
            {
                don.TrangThai = "COMPLETED";
                don.ThoiGianHoanTat = now;
                don.NgayCapNhat = now;

                if (don.PhuongThucThanhToan == "COD" && don.TrangThaiThanhToan == "UNPAID")
                {
                    don.TrangThaiThanhToan = "PAID";
                }

                var spIds = new List<int>();
                foreach (var item in don.ChiTiet)
                {
                    item.DangGiuCho = false;
                    spIds.Add(item.MaSanPham);
                }

                if (spIds.Count > 0)
                {
                    var sanPhams = await db.SanPhams
                        .Where(s => spIds.Contains(s.MaSanPham) && s.TrangThai == "RESERVED")
                        .ToListAsync(ct);

                    foreach (var sp in sanPhams)
                    {
                        sp.TrangThai = "SOLD";
                        sp.NgayCapNhat = now;

                        db.LichSuTrangThaiSanPhams.Add(new LichSuTrangThaiSanPham
                        {
                            MaSanPham = sp.MaSanPham,
                            TrangThaiCu = "RESERVED",
                            TrangThaiMoi = "SOLD",
                            LoaiNguoiThucHien = "SYSTEM",
                            NguoiThucHienId = null,
                            LyDo = $"Đơn hàng #{don.MaDon} đã hoàn tất tự động sau 5 ngày nhận hàng.",
                            ThoiGian = now
                        });
                    }
                }

                if (don.TrangThaiThanhToan == "PAID")
                {
                    var tt = await db.ThanhToans.FirstOrDefaultAsync(t => t.MaDonHang == don.MaDonHang, ct);
                    if (tt != null)
                    {
                        tt.TrangThaiThanhToan = "SUCCESS";
                        tt.ThoiGianThanhToan = tt.ThoiGianThanhToan ?? now;
                    }
                }

                db.LichSuTrangThaiDonHangs.Add(new LichSuTrangThaiDonHang
                {
                    MaDonHang = don.MaDonHang,
                    TrangThaiCu = "DELIVERED",
                    TrangThaiMoi = "COMPLETED",
                    LoaiNguoiThucHien = "SYSTEM",
                    NguoiThucHienId = null,
                    LyDo = "Hệ thống tự động xác nhận hoàn tất đơn hàng sau 5 ngày giao hàng thành công không có khiếu nại.",
                    ThoiGian = now
                });

                db.ThongBaos.Add(new ThongBao
                {
                    MaNguoiDung = don.MaNguoiDung,
                    Loai = "ORDER",
                    TieuDe = "Đơn hàng đã hoàn tất",
                    NoiDung = $"Đơn hàng #{don.MaDon} đã được hệ thống tự động hoàn tất sau 5 ngày giao thành công. Bạn hãy dành chút thời gian đánh giá sản phẩm nhé!",
                    MaThamChieu = don.MaDonHang,
                    DaDoc = false,
                    NgayTao = now
                });

                if (don.CuaHang != null)
                {
                    db.ThongBaos.Add(new ThongBao
                    {
                        MaNguoiDung = don.CuaHang.MaNguoiDung,
                        Loai = "ORDER",
                        TieuDe = "Đơn hàng đã hoàn tất",
                        NoiDung = $"Đơn hàng #{don.MaDon} đã được tự động hoàn tất sau 5 ngày giao hàng thành công không có khiếu nại.",
                        MaThamChieu = don.MaDonHang,
                        DaDoc = false,
                        NgayTao = now
                    });
                }

                soDonHoanTat++;
            }
        }

        if (soDonHuy > 0 || soDonHoanTat > 0)
        {
            await db.SaveChangesAsync(ct);
            if (soDonHuy > 0)
            {
                var cache = scope.ServiceProvider.GetService<IDistributedCache>();
                await HomeCacheHelper.XoaCacheTrangChuAsync(cache, ct);
            }
            log.LogInformation("OrderCleanup: Đã tự động hủy {Cancelled} đơn quá hạn 24h và tự động hoàn tất {Completed} đơn giao quá 5 ngày.", soDonHuy, soDonHoanTat);
        }
    }
}
