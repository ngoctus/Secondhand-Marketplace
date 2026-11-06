using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using SecondHandMarketplace.Web.Common.Extensions;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Security;

namespace SecondHandMarketplace.Web;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddControllersWithViews();
        builder.Services.AddDatabase(builder.Configuration);
        builder.Services.AddAppServices();
        builder.Services.AddSingleton<IMatKhauService, MatKhauService>();
        builder.Services.AddSingleton<JwtTokenService>();

        var jwt = builder.Configuration.GetSection("Jwt");
        var jwtKey = jwt["Key"];
        if (string.IsNullOrWhiteSpace(jwtKey) && builder.Environment.IsDevelopment())
        {
            jwtKey = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(64));
        }
        if (string.IsNullOrWhiteSpace(jwtKey))
            throw new InvalidOperationException("Thiếu cấu hình Jwt:Key. Hãy đặt Jwt__Key bằng User Secrets hoặc biến môi trường.");
        if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
            throw new InvalidOperationException("Jwt:Key phải có tối thiểu 32 byte.");

        // Chỉ gọi AddAuthentication một lần: Cookie là mặc định cho MVC, API chọn JWT bằng AuthenticationSchemes.
        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        })
        .AddCookie(options =>
        {
            options.LoginPath = "/Account/Login";
            options.AccessDeniedPath = "/Account/AccessDenied";
            options.Cookie.Name = "SecondHand.Auth";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.ExpireTimeSpan = TimeSpan.FromDays(7);
            options.SlidingExpiration = true;
            // Mỗi request kiểm tra lại tài khoản: bị khóa/xóa thì phiên đang đăng nhập mất hiệu lực ngay.
            options.Events.OnValidatePrincipal = async context =>
            {
                if (await IsUserActiveAsync(context.HttpContext, context.Principal)) return;
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            };
        })
        .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwt["Issuer"] ?? "SecondHandMarketplace",
                ValidateAudience = true,
                ValidAudience = jwt["Audience"] ?? "SecondHandMarketplace.Client",
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30),
                NameClaimType = ClaimTypes.Name,
                RoleClaimType = ClaimTypes.Role
            };
            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = async context =>
                {
                    if (!await IsUserActiveAsync(context.HttpContext, context.Principal))
                        context.Fail("Tài khoản không tồn tại hoặc đã bị khóa.");
                }
            };
        });
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy("login", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    }));
        });

        var app = builder.Build();
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }

        app.UseWhen(context => !context.Request.Path.StartsWithSegments("/api"),
            branch => branch.UseStatusCodePagesWithReExecute("/Home/TrangLoi", "?ma={0}"));
        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapStaticAssets();
        app.MapControllers();
        app.MapControllerRoute(name: "areas", pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}").WithStaticAssets();
        app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}").WithStaticAssets();
        app.Run();
    }

    private static async Task<bool> IsUserActiveAsync(HttpContext httpContext, ClaimsPrincipal? principal)
    {
        if (!int.TryParse(principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId)) return false;
        var db = httpContext.RequestServices.GetRequiredService<AppDbContext>();
        return await db.NguoiDungs.AsNoTracking()
            .AnyAsync(x => x.MaNguoiDung == userId && x.TrangThai == "ACTIVE", httpContext.RequestAborted);
    }
}