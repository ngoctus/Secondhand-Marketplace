using SecondHandMarketplace.Web.Models.Entities;

namespace SecondHandMarketplace.Web.Services.Accounts;

public sealed record UserAddressInput(
    string? NhanDiaChi,
    string TenNguoiNhan,
    string SoDienThoaiNhan,
    int MaTinhThanh,
    int MaPhuongXa,
    string DiaChiChiTiet,
    bool LaMacDinh);

public interface IUserAddressService
{
    Task<IReadOnlyList<DiaChiNguoiDung>> GetAllAsync(int userId, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error, DiaChiNguoiDung? Saved)> AddAsync(int userId, UserAddressInput input, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> UpsertDefaultFromProfileAsync(int userId, UserAddressInput input, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> SetDefaultAsync(int userId, int maDiaChi, CancellationToken cancellationToken = default);
    Task<(bool Success, string? Error)> DeleteAsync(int userId, int maDiaChi, CancellationToken cancellationToken = default);
}
