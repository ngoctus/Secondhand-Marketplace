using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Threading.RateLimiting;
using SecondHandMarketplace.Web.Hubs;
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
        builder.Services.AddMemoryCache();
        builder.Services.AddDataProtection();
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(15),
                    QueueLimit = 0,
                    AutoReplenishment = true
                }));
            options.AddPolicy("password-reset", context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(10),
                    QueueLimit = 0,
                    AutoReplenishment = true
                }));
        });
        builder.Services.AddSignalR();
        builder.Services.AddDatabase(builder.Configuration);
        builder.Services.AddAppServices();
        builder.Services.AddSingleton<IMatKhauService, MatKhauService>();
        builder.Services.AddSingleton<JwtTokenService>();

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
            {
                Title = "SecondHand Marketplace API",
                Version = "v1",
                Description = "Hệ thống RESTful API chuẩn cho Sàn giao dịch đồ cũ (SecondHand Marketplace)"
            });

            options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                Description = "Nhập JWT token theo mẫu: {token}"
            });

            options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
            {
                {
                    new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                    {
                        Reference = new Microsoft.OpenApi.Models.OpenApiReference
                        {
                            Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        var jwt = builder.Configuration.GetSection("Jwt");
        var jwtKey = jwt["Key"] ?? throw new InvalidOperationException("Thiếu cấu hình Jwt:Key.");
        if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
            throw new InvalidOperationException("Jwt:Key phải có tối thiểu 32 byte.");

        var jwtKeyIsPlaceholder = jwtKey.StartsWith("CHANGE_THIS", StringComparison.OrdinalIgnoreCase);
        if (jwtKeyIsPlaceholder && !builder.Environment.IsDevelopment())
            throw new InvalidOperationException("Phải đặt Jwt:Key thật (biến môi trường Jwt__Key) khi chạy ngoài Development.");

        // Cấu hình Authentication: Dùng PolicyScheme để tự động chọn JWT khi có header Bearer hoặc request tới /api, ngược lại dùng Cookie cho MVC.
        builder.Services.AddAuthentication(options =>
        {
            options.DefaultScheme = "SmartScheme";
            options.DefaultChallengeScheme = "SmartScheme";
        })
        .AddPolicyScheme("SmartScheme", "Bearer or Cookie", options =>
        {
            options.ForwardDefaultSelector = context =>
            {
                var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
                if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    return JwtBearerDefaults.AuthenticationScheme;
                }
                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    return JwtBearerDefaults.AuthenticationScheme;
                }
                return CookieAuthenticationDefaults.AuthenticationScheme;
            };
        })
        .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
        {
            options.LoginPath = "/Account/Login";
            options.AccessDeniedPath = "/Account/AccessDenied";
            options.Cookie.Name = "SecondHand.Auth";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.ExpireTimeSpan = TimeSpan.FromDays(7);
            options.SlidingExpiration = true;
            options.Events.OnRedirectToLogin = context =>
            {
                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                }
                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            };
            options.Events.OnRedirectToAccessDenied = context =>
            {
                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                }
                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            };
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
                OnChallenge = context =>
                {
                    context.HandleResponse();
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json";
                    return context.Response.WriteAsync("{\"error\":\"unauthorized\",\"message\":\"Bạn chưa đăng nhập hoặc token không hợp lệ.\"}");
                },
                OnForbidden = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    context.Response.ContentType = "application/json";
                    return context.Response.WriteAsync("{\"error\":\"forbidden\",\"message\":\"Bạn không có quyền truy cập tài nguyên này.\"}");
                },
                OnTokenValidated = async context =>
                {
                    if (!await IsUserActiveAsync(context.HttpContext, context.Principal))
                        context.Fail("Tài khoản không tồn tại hoặc đã bị khóa.");
                }
            };
        });

        var app = builder.Build();
        var forwardedHeaders = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
            ForwardLimit = 1
        };
        forwardedHeaders.KnownProxies.Add(System.Net.IPAddress.Loopback);
        forwardedHeaders.KnownProxies.Add(System.Net.IPAddress.IPv6Loopback);
        app.UseForwardedHeaders(forwardedHeaders);
        if (jwtKeyIsPlaceholder)
            app.Logger.LogWarning("Đang dùng Jwt:Key mặc định. Chỉ chấp nhận khi phát triển local.");
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "SecondHand Marketplace API v1");
                c.RoutePrefix = "swagger";
            });
        }

        app.UseWhen(context => !context.Request.Path.StartsWithSegments("/api") && !context.Request.Path.StartsWithSegments("/swagger"),
            branch => branch.UseStatusCodePagesWithReExecute("/Home/TrangLoi", "?ma={0}"));
        app.UseHttpsRedirection();
        app.UseStaticFiles();   // Phục vụ file trong wwwroot (ảnh seed, css, js...)
        app.UseRouting();
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapStaticAssets();
        app.MapControllers();
        app.MapHub<AuctionHub>("/hubs/auction");
        app.MapHub<NotificationHub>("/hubs/notification");
        app.MapHub<ChatHub>("/hubs/chat");
        app.MapControllerRoute(name: "areas", pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}").WithStaticAssets();
        app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}").WithStaticAssets();
        app.Run();
    }

    private static async Task<bool> IsUserActiveAsync(HttpContext httpContext, ClaimsPrincipal? principal)
    {
        if (!int.TryParse(principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId)) return false;
        var db = httpContext.RequestServices.GetRequiredService<AppDbContext>();
        var user = await db.NguoiDungs.AsNoTracking()
            .Where(x => x.MaNguoiDung == userId)
            .Select(x => new { x.TrangThai, x.MatKhauBam })
            .FirstOrDefaultAsync(httpContext.RequestAborted);

        if (user is null || user.TrangThai != "ACTIVE") return false;

        var stamp = principal?.FindFirst("sec_stamp")?.Value;
        if (!string.IsNullOrEmpty(stamp))
        {
            var expectedStamp = DangNhapHelper.LaySecurityStamp(user.MatKhauBam);
            if (!string.Equals(stamp, expectedStamp, StringComparison.Ordinal))
                return false;
        }

        return true;
    }
}
