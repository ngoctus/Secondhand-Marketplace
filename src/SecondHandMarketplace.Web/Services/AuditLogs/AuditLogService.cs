using SecondHandMarketplace.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace SecondHandMarketplace.Web.Services.AuditLogs;

public sealed class AuditLogService(AppDbContext db) : IAuditLogService
{
    public async Task LogAsync(int? maNguoiDung, string hanhDong, string loaiDoiTuong, int? doiTuongId = null,
        string ketQua = "SUCCESS", string? lyDo = null, string? chiTiet = null)
    {
        await db.Database.ExecuteSqlInterpolatedAsync($@"EXEC dbo.sp_GhiNhatKy
            @MaNguoiDung={maNguoiDung}, @HanhDong={hanhDong}, @LoaiDoiTuong={loaiDoiTuong},
            @DoiTuongId={doiTuongId}, @KetQua={ketQua}, @LyDo={lyDo}, @ChiTiet={chiTiet}");
    }
}
