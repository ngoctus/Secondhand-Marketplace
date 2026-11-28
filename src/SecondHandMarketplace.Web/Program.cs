using System.Globalization;
using System.Security.Claims;
using System.Text;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using System.Threading.RateLimiting;
using SecondHandMarketplace.Web.Hubs;
using SecondHandMarketplace.Web.Common.Extensions;
using SecondHandMarketplace.Web.Data;
using SecondHandMarketplace.Web.Security;
using SecondHandMarketplace.Web.Services.BackgroundJobs;
using SecondHandMarketplace.Web.Services.Health;
using SecondHandMarketplace.Web.Services.Storage;

namespace SecondHandMarketplace.Web;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        // Avoid a Windows Event Log provider failure taking down startup when the app
        // logs a recoverable database/configuration error under a non-admin account.
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.AddDebug();
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
        var storageProvider = builder.Configuration["Storage:Provider"];
        if (!string.IsNullOrWhiteSpace(storageProvider)
            && !storageProvider.Equals("Local", StringComparison.OrdinalIgnoreCase)
            && !storageProvider.Equals("S3", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Storage:Provider must be Local or S3.");
        var redisConnectionString = builder.Configuration["Cache:Redis:ConnectionString"];
        IConnectionMultiplexer? redisConnection = null;
        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            redisConnection = ConnectionMultiplexer.Connect(redisConnectionString);
            builder.Services.AddSingleton(redisConnection);
            builder.Services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                options.InstanceName = "remarket:";
            });
        }
        else
        {
            builder.Services.AddDistributedMemoryCache();
        }
        var dataProtectionPath = builder.Configuration["DataProtection:KeysPath"];
        var dataProtection = builder.Services.AddDataProtection().SetApplicationName("SecondHandMarketplace");
        if (redisConnection is not null)
        {
            dataProtection.PersistKeysToStackExchangeRedis(redisConnection, "remarket:data-protection-keys");
        }
        else if (!string.IsNullOrWhiteSpace(dataProtectionPath))
        {
            Directory.CreateDirectory(dataProtectionPath);
            dataProtection.PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath));
        }
        builder.Services.AddSingleton<IBackgroundJobLease>(services =>
            new RedisBackgroundJobLease(redisConnection, services.GetRequiredService<ILogger<RedisBackgroundJobLease>>()));
        builder.Services.AddHealthChecks().AddCheck<SqlServerHealthCheck>("sql", tags: ["ready"]);
        if (!string.IsNullOrWhiteSpace(redisConnectionString))
            builder.Services.AddHealthChecks().AddCheck<RedisHealthCheck>("redis", tags: ["ready"]);
        if (string.Equals(storageProvider, "S3", StringComparison.OrdinalIgnoreCase))
            builder.Services.AddHealthChecks().AddCheck<S3StorageHealthCheck>("object-storage", tags: ["ready"]);
        var authRateLimit = builder.Environment.IsDevelopment() ? 50 : 5;
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = authRateLimit,
                    Window = TimeSpan.FromMinutes(15),
                    QueueLimit = 0,
                    AutoReplenishment = true
                }));
            options.AddPolicy("password-reset", context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = authRateLimit,
                    Window = TimeSpan.FromMinutes(10),
                    QueueLimit = 0,
                    AutoReplenishment = true
                }));
            options.AddPolicy("appeal", context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = authRateLimit,
                    Window = TimeSpan.FromMinutes(15),
                    QueueLimit = 0,
                    AutoReplenishment = true
                }));
            options.AddPolicy("register", context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = authRateLimit,
                    Window = TimeSpan.FromMinutes(15),
                    QueueLimit = 0,
                    AutoReplenishment = true
                }));
        });
        builder.Services.AddSignalR();
        builder.Services.AddDatabase(builder.Configuration);
        builder.Services.AddAppServices();
        if (string.Equals(storageProvider, "S3", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(builder.Configuration["Storage:S3:Bucket"]))
                throw new InvalidOperationException("Storage:S3:Bucket is required when Storage:Provider is S3.");

            builder.Services.AddSingleton<IAmazonS3>(_ =>
            {
                var regionName = builder.Configuration["Storage:S3:Region"] ?? "ap-southeast-1";
                var s3Config = new AmazonS3Config
                {
                    RegionEndpoint = RegionEndpoint.GetBySystemName(regionName),
                    ForcePathStyle = builder.Configuration.GetValue("Storage:S3:ForcePathStyle", true)
                };
                var endpoint = builder.Configuration["Storage:S3:Endpoint"];
                if (!string.IsNullOrWhiteSpace(endpoint)) s3Config.ServiceURL = endpoint;

                var accessKey = builder.Configuration["Storage:S3:AccessKeyId"];
                var secretKey = builder.Configuration["Storage:S3:SecretAccessKey"];
                if (string.IsNullOrWhiteSpace(accessKey) != string.IsNullOrWhiteSpace(secretKey))
                    throw new InvalidOperationException("S3 AccessKeyId and SecretAccessKey must be configured together.");
                if (!string.IsNullOrWhiteSpace(accessKey))
                    return new AmazonS3Client(new BasicAWSCredentials(accessKey, secretKey), s3Config);
                return new AmazonS3Client(s3Config);
            });
        }
        builder.Services.AddSingleton<IUploadStorage, UploadStorage>();
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
            options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
                ? CookieSecurePolicy.SameAsRequest
                : CookieSecurePolicy.Always;
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
        var forwardedHeaders = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
            ForwardLimit = 1
        };
        forwardedHeaders.KnownProxies.Add(System.Net.IPAddress.Loopback);
        forwardedHeaders.KnownProxies.Add(System.Net.IPAddress.IPv6Loopback);
        foreach (var proxy in builder.Configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [])
        {
            if (System.Net.IPAddress.TryParse(proxy, out var address))
                forwardedHeaders.KnownProxies.Add(address);
        }
        app.UseForwardedHeaders(forwardedHeaders);
        app.Use(async (context, next) =>
        {
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["X-Frame-Options"] = "DENY";
            context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
            await next();
        });
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
        var publicUploadsRoot = UploadStoragePaths.PublicRoot(app.Environment, app.Configuration);
        var privateUploadsRoot = UploadStoragePaths.PrivateRoot(app.Environment, app.Configuration);
        UploadStoragePaths.EnsurePrivateRootIsNotPublic(publicUploadsRoot, privateUploadsRoot);
        Directory.CreateDirectory(publicUploadsRoot);
        Directory.CreateDirectory(privateUploadsRoot);
        app.Use(async (context, next) =>
        {
            var requestPath = context.Request.Path.Value ?? string.Empty;
            if (requestPath.StartsWith("/uploads/appeals/", StringComparison.OrdinalIgnoreCase)
                || requestPath.StartsWith("/uploads/disputes/", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }
            await next();
        });
        app.UseMiddleware<PublicObjectStorageMiddleware>();
        app.UseStaticFiles();   // Phục vụ file trong wwwroot (ảnh seed, css, js...)
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(publicUploadsRoot),
            RequestPath = "/uploads",
            OnPrepareResponse = context =>
                context.Context.Response.Headers.CacheControl = "public, max-age=31536000, immutable"
        });
        app.UseRequestLocalization();
        app.UseRouting();
        if (redisConnection is not null)
            app.UseMiddleware<DistributedRateLimitMiddleware>();
        else
            app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapStaticAssets();
        app.MapControllers();
        app.MapHub<AuctionHub>("/hubs/auction");
        app.MapHub<NotificationHub>("/hubs/notification");
        app.MapHub<ChatHub>("/hubs/chat");
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = WriteHealthResponseAsync
        });
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready"),
            ResponseWriter = WriteHealthResponseAsync
        });
        app.MapControllerRoute(name: "areas", pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}").WithStaticAssets();
        app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}").WithStaticAssets();
        app.Run();
    }

    private static Task WriteHealthResponseAsync(HttpContext context, HealthReport report) =>
        context.Response.WriteAsJsonAsync(new
        {
            status = report.Status.ToString().ToLowerInvariant(),
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString().ToLowerInvariant()
            })
        });

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
