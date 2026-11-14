using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SecondHandMarketplace.Web.Common.Constants;
using SecondHandMarketplace.Web.Common.Extensions;
using SecondHandMarketplace.Web.Models.DTOs.Auth;
using SecondHandMarketplace.Web.Security;
using SecondHandMarketplace.Web.Services.Auth;
using SecondHandMarketplace.Web.Services.Shops;

namespace SecondHandMarketplace.Web.Controllers.Api.v1;

/// <summary>
/// Epic 1: Quản lý tài khoản, xác thực và hồ sơ người dùng.
/// </summary>
[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
public class AuthController(
    IAuthService authService,
    IShopService shopService,
    JwtTokenService tokenService) : ControllerBase
{
    /// <summary>
    /// Đăng nhập hệ thống, sinh Access Token (JWT).
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("login")]
    [ProducesResponseType(typeof(ServiceResult<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResult), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ServiceResult<AuthResponse>.Fail(ErrorCodes.ValidationError, "Dữ liệu không hợp lệ.",
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()).ToActionResult(this);
        }

        var user = await authService.AuthenticateAsync(request.Email, request.Password, cancellationToken);
        if (user is null)
        {
            return ServiceResult<AuthResponse>.Fail(ErrorCodes.InvalidCredentials, "Email hoặc mật khẩu không chính xác, hoặc tài khoản đã bị khóa.").ToActionResult(this);
        }

        var shop = await shopService.GetByOwnerAsync(user.MaNguoiDung, cancellationToken);
        var token = tokenService.CreateToken(user.MaNguoiDung, user.Email, user.HoTen, user.VaiTro, DangNhapHelper.LaySecurityStamp(user.MatKhauBam));

        var response = new AuthResponse
        {
            AccessToken = token.Token,
            TokenType = "Bearer",
            ExpiresAtUtc = token.ExpiresAtUtc,
            User = new UserDto
            {
                Id = user.MaNguoiDung,
                FullName = user.HoTen,
                Email = user.Email,
                PhoneNumber = user.SoDienThoai ?? string.Empty,
                Address = user.DiaChiMacDinh,
                Avatar = user.AnhDaiDien,
                Role = user.VaiTro,
                HasShop = shop is not null,
                ShopId = shop?.MaCuaHang,
                Status = user.TrangThai,
                CreatedAt = user.NgayTao
            }
        };

        return ServiceResult<AuthResponse>.Ok(response, "Đăng nhập thành công.").ToActionResult(this);
    }

    /// <summary>
    /// Đăng ký tài khoản người dùng mới.
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ServiceResult<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResult), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ServiceResult<UserDto>.Fail(ErrorCodes.ValidationError, "Dữ liệu đăng ký không hợp lệ.",
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()).ToActionResult(this);
        }

        var (success, error, user) = await authService.RegisterAsync(
            request.FullName, request.Email, request.PhoneNumber, request.Password, cancellationToken);

        if (!success || user is null)
        {
            var errStr = error ?? string.Empty;
            var code = errStr.Contains("Email", StringComparison.OrdinalIgnoreCase) && !errStr.Contains("điện thoại", StringComparison.OrdinalIgnoreCase)
                ? ErrorCodes.EmailExists
                : errStr.Contains("điện thoại", StringComparison.OrdinalIgnoreCase) && !errStr.Contains("Email", StringComparison.OrdinalIgnoreCase)
                    ? ErrorCodes.PhoneExists
                    : ErrorCodes.BadRequest;
            return ServiceResult<UserDto>.Fail(code, error ?? "Đăng ký không thành công.").ToActionResult(this);
        }

        if (!string.IsNullOrWhiteSpace(request.Address))
        {
            await authService.UpdateProfileAsync(user.MaNguoiDung, user.HoTen, user.SoDienThoai, request.Address, null, cancellationToken);
        }

        var userDto = new UserDto
        {
            Id = user.MaNguoiDung,
            FullName = user.HoTen,
            Email = user.Email,
            PhoneNumber = user.SoDienThoai ?? string.Empty,
            Address = request.Address,
            Role = user.VaiTro,
            HasShop = false,
            Status = user.TrangThai,
            CreatedAt = user.NgayTao
        };

        return ServiceResult<UserDto>.Ok(userDto, "Đăng ký tài khoản thành công.").ToActionResult(this);
    }

    /// <summary>
    /// Lấy thông tin hồ sơ của tài khoản hiện tại.
    /// </summary>
    [HttpGet("profile")]
    [Authorize]
    [ProducesResponseType(typeof(ServiceResult<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResult), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return ServiceResult<UserDto>.Fail(ErrorCodes.Unauthorized, "Không thể xác định danh tính.").ToActionResult(this);
        }

        var user = await authService.GetUserAsync(userId, cancellationToken);
        if (user is null)
        {
            return ServiceResult<UserDto>.Fail(ErrorCodes.UserNotFound, "Không tìm thấy thông tin tài khoản.").ToActionResult(this);
        }

        var shop = await shopService.GetByOwnerAsync(user.MaNguoiDung, cancellationToken);
        var userDto = new UserDto
        {
            Id = user.MaNguoiDung,
            FullName = user.HoTen,
            Email = user.Email,
            PhoneNumber = user.SoDienThoai ?? string.Empty,
            Address = user.DiaChiMacDinh,
            Avatar = user.AnhDaiDien,
            Role = user.VaiTro,
            HasShop = shop is not null,
            ShopId = shop?.MaCuaHang,
            Status = user.TrangThai,
            CreatedAt = user.NgayTao
        };

        return ServiceResult<UserDto>.Ok(userDto).ToActionResult(this);
    }

    /// <summary>
    /// Cập nhật thông tin cá nhân.
    /// </summary>
    [HttpPut("profile")]
    [Authorize]
    [ProducesResponseType(typeof(ServiceResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResult), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ServiceResult.Fail(ErrorCodes.ValidationError, "Dữ liệu cập nhật không hợp lệ.",
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()).ToActionResult(this);
        }

        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return ServiceResult.Fail(ErrorCodes.Unauthorized, "Không thể xác định danh tính.").ToActionResult(this);
        }

        var (success, error) = await authService.UpdateProfileAsync(
            userId, request.FullName, request.PhoneNumber, request.Address, request.Avatar, cancellationToken);

        if (!success)
        {
            return ServiceResult.Fail(ErrorCodes.BadRequest, error ?? "Cập nhật hồ sơ thất bại.").ToActionResult(this);
        }

        return ServiceResult.Ok("Cập nhật hồ sơ thành công.").ToActionResult(this);
    }

    /// <summary>
    /// Đổi mật khẩu tài khoản.
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(typeof(ServiceResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResult), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ServiceResult.Fail(ErrorCodes.ValidationError, "Dữ liệu đổi mật khẩu không hợp lệ.",
                ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()).ToActionResult(this);
        }

        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return ServiceResult.Fail(ErrorCodes.Unauthorized, "Không thể xác định danh tính.").ToActionResult(this);
        }

        var (success, error) = await authService.ChangePasswordAsync(
            userId, request.CurrentPassword, request.NewPassword, cancellationToken);

        if (!success)
        {
            return ServiceResult.Fail(ErrorCodes.InvalidOldPassword, error ?? "Đổi mật khẩu thất bại.").ToActionResult(this);
        }

        return ServiceResult.Ok("Đổi mật khẩu thành công.").ToActionResult(this);
    }
}
