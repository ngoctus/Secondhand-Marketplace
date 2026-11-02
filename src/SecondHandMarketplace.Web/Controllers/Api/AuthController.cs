using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecondHandMarketplace.Web.Security;
using SecondHandMarketplace.Web.Services.Auth;

namespace SecondHandMarketplace.Web.Controllers.Api;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService authService, JwtTokenService tokenService) : ControllerBase
{
    public sealed record LoginRequest(string Email, string Password);

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Email và mật khẩu không được để trống." });
        var user = await authService.AuthenticateAsync(request.Email, request.Password, cancellationToken);
        if (user is null) return Unauthorized(new { message = "Email hoặc mật khẩu không chính xác, hoặc tài khoản đã bị khóa." });
        var token = tokenService.CreateToken(user.MaNguoiDung, user.Email, user.HoTen, user.VaiTro);
        return Ok(new { accessToken = token.Token, tokenType = "Bearer", expiresAtUtc = token.ExpiresAtUtc,
            user = new { id = user.MaNguoiDung, fullName = user.HoTen, email = user.Email, role = user.VaiTro } });
    }
}
