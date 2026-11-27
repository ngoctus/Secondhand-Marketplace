using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SecondHandMarketplace.Web.Data;

namespace SecondHandMarketplace.Web.Services.Health;

public sealed class SqlServerHealthCheck(AppDbContext db) : IHealthCheck
{
    private const string SchemaReadinessSql = """
        SELECT CASE WHEN
            OBJECT_ID(N'dbo.NguoiDung', N'U') IS NOT NULL
            AND COL_LENGTH(N'dbo.NguoiDung', N'MocTinhViPham') IS NOT NULL
            AND COL_LENGTH(N'dbo.NguoiDung', N'SoLanBiKhoa') IS NOT NULL
            AND OBJECT_ID(N'dbo.KhieuNaiTaiKhoan', N'U') IS NOT NULL
            AND OBJECT_ID(N'dbo.CauHinhKhieuNaiTaiKhoan', N'U') IS NOT NULL
            AND COL_LENGTH(N'dbo.DanhGia', N'PhanHoiShop') IS NOT NULL
            AND COL_LENGTH(N'dbo.DanhGia', N'NgayPhanHoi') IS NOT NULL
            AND COL_LENGTH(N'dbo.TranhChap', N'AnhPhanHoi') IS NOT NULL
            AND OBJECT_ID(N'dbo.sp_CapNhatTrangThaiDon', N'P') IS NOT NULL
            AND OBJECT_ID(N'dbo.sp_AdminCapNhatTrangThaiDon', N'P') IS NOT NULL
            AND EXISTS (
                SELECT 1 FROM sys.check_constraints
                WHERE name = N'CK_DonHang_TrangThaiTT' AND parent_object_id = OBJECT_ID(N'dbo.DonHang'))
            AND EXISTS (
                SELECT 1 FROM sys.check_constraints
                WHERE name = N'CK_ThanhToan_TrangThai' AND parent_object_id = OBJECT_ID(N'dbo.ThanhToan'))
            THEN CONVERT(bit, 1) ELSE CONVERT(bit, 0) END;
        """;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await db.Database.OpenConnectionAsync(cancellationToken);
            try
            {
                await using var command = db.Database.GetDbConnection().CreateCommand();
                command.CommandText = SchemaReadinessSql;
                var result = await command.ExecuteScalarAsync(cancellationToken);
                return result is not null && Convert.ToBoolean(result)
                    ? HealthCheckResult.Healthy("SQL Server and required application schema are available.")
                    : HealthCheckResult.Unhealthy("SQL Server is reachable, but one or more required application migrations are missing.");
            }
            finally
            {
                await db.Database.CloseConnectionAsync();
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy("SQL Server is unavailable or its schema readiness check failed.", exception);
        }
    }
}
