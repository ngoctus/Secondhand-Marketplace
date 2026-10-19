using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Web2Hand.Common.Constants;
using Web2Hand.Models.Entities;

namespace Web2Hand.Data.Seed;

/// <summary>Chạy mỗi lần mở app: áp migration, tạo role, tài khoản Admin và danh mục mẫu (nếu chưa có).</summary>
public static class DbSeeder
{
    public const string AdminEmail = "admin@web2hand.local";
    public const string AdminPassword = "Admin@123";

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        await db.Database.MigrateAsync();

        foreach (var role in new[] { AppRoles.Buyer, AppRoles.Seller, AppRoles.Admin })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        if (await userManager.FindByEmailAsync(AdminEmail) == null)
        {
            var admin = new AppUser
            {
                UserName = AdminEmail,
                Email = AdminEmail,
                FullName = "Quản trị viên",
                EmailConfirmed = true
            };
            await userManager.CreateAsync(admin, AdminPassword);
            await userManager.AddToRoleAsync(admin, AppRoles.Admin);
        }

        if (!await db.Categories.AnyAsync())
        {
            db.Categories.AddRange(
                new Category { Name = "Điện thoại" },
                new Category { Name = "Laptop" },
                new Category { Name = "Thời trang" },
                new Category { Name = "Đồ gia dụng" },
                new Category { Name = "Sách" },
                new Category { Name = "Khác" });
            await db.SaveChangesAsync();
        }
    }
}
