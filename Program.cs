using Microsoft.AspNetCore.Authentication.Cookies;
using SecondHandMarketplace.Web.Services;

namespace SecondHandMarketplace.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();
            builder.Services.AddScoped<IImageUploadService, ImageUploadService>();
            builder.Services.AddSingleton<IMatKhauService, MatKhauService>();

            // 2.8a: đăng nhập bằng cookie (không dùng Identity)
            builder.Services
                .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Account/Login";            // chưa đăng nhập -> về trang đăng nhập (E4 làm trang này)
                    options.AccessDeniedPath = "/Home/KhongCoQuyen"; // sai quyền -> trang "không có quyền"
                    options.Cookie.Name = "SecondHand.Auth";
                    options.Cookie.HttpOnly = true;
                    options.ExpireTimeSpan = TimeSpan.FromDays(7);
                    options.SlidingExpiration = true;

                    // Endpoint /api/... (gọi bằng fetch) trả mã 401/403 thay vì chuyển trang
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

                    // TODO (sau 2.7, khi có DbContext): dùng options.Events.OnValidatePrincipal
                    // để đọc lại TrangThai của NguoiDung/CuaHang mỗi request, khóa có hiệu lực ngay.
                });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            // 2.8a: 400/403/404... không có nội dung -> hiện trang lỗi thân thiện
            app.UseStatusCodePagesWithReExecute("/Home/TrangLoi", "?ma={0}");

            app.UseHttpsRedirection();
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapStaticAssets();
            app.MapControllers();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();

            app.Run();
        }
    }
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(name: "areas", pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<AuctionHub>("/hubs/auction");
app.MapHub<ChatHub>("/hubs/chat");
app.MapHub<NotificationHub>("/hubs/notification");

await DbSeeder.SeedAsync(app.Services);

app.Run();
