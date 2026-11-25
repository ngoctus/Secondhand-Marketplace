using System.Globalization;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
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
        builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
        builder.Services.AddControllersWithViews()
            .AddViewLocalization()
            .AddDataAnnotationsLocalization(options =>
            {
                options.DataAnnotationLocalizerProvider = (_, factory) =>
                    factory.Create(typeof(SharedResource));
            });
        builder.Services.Configure<RequestLocalizationOptions>(options =>
        {
            var supportedCultures = new[]
            {
                new CultureInfo("vi-VN"),
                new CultureInfo("en-US")
            };
            options.DefaultRequestCulture = new RequestCulture("vi-VN");
            options.SupportedCultures = supportedCultures;
            options.SupportedUICultures = supportedCultures;
            options.RequestCultureProviders = new IRequestCultureProvider[]
            {
                new CookieRequestCultureProvider(),
                new QueryStringRequestCultureProvider()
            };
        });
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
            options.AddPolicy("appeal", context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(15),
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
        var authBuilder = builder.Services.AddAuthentication(options =>
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

        authBuilder.AddCookie("ExternalCookie", options =>
        {
            options.Cookie.Name = "SecondHand.External";
            options.ExpireTimeSpan = TimeSpan.FromMinutes(15);
        });

        var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
        var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
        var isGoogleOAuthActive = !string.IsNullOrWhiteSpace(googleClientId) &&
                                  !googleClientId.StartsWith("CHANGE_THIS", StringComparison.OrdinalIgnoreCase);
        if (isGoogleOAuthActive)
        {
            authBuilder.AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
            {
                options.SignInScheme = "ExternalCookie";
                options.ClientId = googleClientId!;
                options.ClientSecret = googleClientSecret ?? "";
                options.SaveTokens = true;
                options.Events.OnRedirectToAuthorizationEndpoint = context =>
                {
                    context.Response.Redirect(context.RedirectUri + "&prompt=select_account");
                    return Task.CompletedTask;
                };
            });
        }

        var fbAppId = builder.Configuration["Authentication:Facebook:AppId"];
        var fbAppSecret = builder.Configuration["Authentication:Facebook:AppSecret"];
        var isFbOAuthActive = !string.IsNullOrWhiteSpace(fbAppId) &&
                              !fbAppId.StartsWith("CHANGE_THIS", StringComparison.OrdinalIgnoreCase);
        if (isFbOAuthActive)
        {
            authBuilder.AddFacebook(FacebookDefaults.AuthenticationScheme, options =>
            {
                options.SignInScheme = "ExternalCookie";
                options.AppId = fbAppId!;
                options.AppSecret = fbAppSecret ?? "";
            });
        }

        var app = builder.Build();
        EnsureAccountAppealSchema(app);
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
        app.UseRequestLocalization();
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

    /// <summary>
    /// Tự động đồng bộ schema KN-01 (MocTinhViPham, SoLanBiKhoa, KhieuNaiTaiKhoan, CauHinhKhieuNaiTaiKhoan)
    /// cho các máy thành viên đang chạy DB cũ chưa chạy file migration thủ công.
    /// </summary>
    private static void EnsureAccountAppealSchema(WebApplication app)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            if (!db.Database.IsRelational()) return;

            const string sql = """
                IF OBJECT_ID(N'dbo.NguoiDung', N'U') IS NOT NULL
                BEGIN
                    IF COL_LENGTH(N'dbo.NguoiDung', N'MocTinhViPham') IS NULL
                        EXEC(N'ALTER TABLE dbo.NguoiDung ADD MocTinhViPham datetime2(3) NULL;');

                    IF COL_LENGTH(N'dbo.NguoiDung', N'SoLanBiKhoa') IS NULL
                    BEGIN
                        EXEC(N'ALTER TABLE dbo.NguoiDung ADD SoLanBiKhoa int NOT NULL CONSTRAINT DF_NguoiDung_SoLanBiKhoa DEFAULT (0);');
                        EXEC(N'UPDATE dbo.NguoiDung SET SoLanBiKhoa = 1 WHERE TrangThai = ''LOCKED'' AND SoLanBiKhoa = 0;');
                    END;
                END;

                IF OBJECT_ID(N'dbo.DanhGia', N'U') IS NOT NULL
                BEGIN
                    IF COL_LENGTH(N'dbo.DanhGia', N'PhanHoiShop') IS NULL
                        EXEC(N'ALTER TABLE dbo.DanhGia ADD PhanHoiShop nvarchar(1000) NULL;');

                    IF COL_LENGTH(N'dbo.DanhGia', N'NgayPhanHoi') IS NULL
                        EXEC(N'ALTER TABLE dbo.DanhGia ADD NgayPhanHoi datetime2(3) NULL;');
                END;

                IF OBJECT_ID(N'dbo.TranhChap', N'U') IS NOT NULL
                BEGIN
                    IF COL_LENGTH(N'dbo.TranhChap', N'AnhPhanHoi') IS NULL
                        EXEC(N'ALTER TABLE dbo.TranhChap ADD AnhPhanHoi nvarchar(500) NULL;');
                END;

                IF OBJECT_ID(N'dbo.KhieuNaiTaiKhoan', N'U') IS NULL AND OBJECT_ID(N'dbo.NguoiDung', N'U') IS NOT NULL
                BEGIN
                    CREATE TABLE dbo.KhieuNaiTaiKhoan (
                        MaKhieuNai          int            IDENTITY(1,1) NOT NULL,
                        MaNguoiDung         int            NOT NULL,
                        LoaiYeuCau          varchar(20)    NOT NULL,
                        LanKhoaThu          int            NOT NULL,
                        LyDoKhoaLucGui      nvarchar(500)  NOT NULL,
                        NoiDung             nvarchar(2000) NOT NULL,
                        BangChungAnh        varchar(500)   NULL,
                        YeuCauPhi           bit            NOT NULL CONSTRAINT DF_KhieuNaiTaiKhoan_YeuCauPhi DEFAULT (0),
                        SoTienPhi           decimal(18,2)  NOT NULL CONSTRAINT DF_KhieuNaiTaiKhoan_SoTienPhi DEFAULT (0),
                        MaGiaoDichThamChieu varchar(40)    NULL,
                        TrangThai           varchar(20)    NOT NULL CONSTRAINT DF_KhieuNaiTaiKhoan_TrangThai DEFAULT ('PENDING'),
                        GhiChuAdmin         nvarchar(1000) NULL,
                        NguoiXuLyId         int            NULL,
                        NgayTao             datetime2(3)   NOT NULL CONSTRAINT DF_KhieuNaiTaiKhoan_NgayTao   DEFAULT (SYSUTCDATETIME()),
                        NgayXuLy            datetime2(3)   NULL,
                        CONSTRAINT PK_KhieuNaiTaiKhoan PRIMARY KEY CLUSTERED (MaKhieuNai),
                        CONSTRAINT FK_KhieuNaiTaiKhoan_NguoiDung FOREIGN KEY (MaNguoiDung) REFERENCES dbo.NguoiDung (MaNguoiDung),
                        CONSTRAINT FK_KhieuNaiTaiKhoan_NguoiXuLy FOREIGN KEY (NguoiXuLyId) REFERENCES dbo.NguoiDung (MaNguoiDung)
                    );
                END;

                IF OBJECT_ID(N'dbo.CauHinhKhieuNaiTaiKhoan', N'U') IS NULL AND OBJECT_ID(N'dbo.NguoiDung', N'U') IS NOT NULL
                BEGIN
                    CREATE TABLE dbo.CauHinhKhieuNaiTaiKhoan (
                        Id                int           NOT NULL,
                        SoLanGuiMoiChuKy  int           NOT NULL CONSTRAINT DF_CauHinhKhieuNai_SoLanGui    DEFAULT (2),
                        CooldownPhut      int           NOT NULL CONSTRAINT DF_CauHinhKhieuNai_Cooldown    DEFAULT (4320),
                        PhiMoKhoaLan2     decimal(18,2) NOT NULL CONSTRAINT DF_CauHinhKhieuNai_Phi         DEFAULT (200000),
                        TenNganHang       nvarchar(100) NOT NULL,
                        MaNganHang        varchar(20)   NOT NULL,
                        SoTaiKhoan        varchar(30)   NOT NULL,
                        ChuTaiKhoan       nvarchar(100) NOT NULL,
                        NgayCapNhat       datetime2(3)  NOT NULL CONSTRAINT DF_CauHinhKhieuNai_NgayCapNhat DEFAULT (SYSUTCDATETIME()),
                        NguoiCapNhatId    int           NULL,
                        CONSTRAINT PK_CauHinhKhieuNaiTaiKhoan PRIMARY KEY CLUSTERED (Id)
                    );
                    INSERT INTO dbo.CauHinhKhieuNaiTaiKhoan (Id, SoLanGuiMoiChuKy, CooldownPhut, PhiMoKhoaLan2, TenNganHang, MaNganHang, SoTaiKhoan, ChuTaiKhoan)
                    VALUES (1, 2, 4320, 200000, N'MB Bank', 'MB', '090123456789', N'RE MARKET PLATFORM');
                END;
                """;

            db.Database.ExecuteSqlRaw(sql);
        }
        catch (Exception ex)
        {
            app.Logger.LogWarning(ex, "Không thể tự động kiểm tra/đồng bộ schema Khiếu nại tài khoản khi khởi động.");
        }
    }
}
