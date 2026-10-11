using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Web2Hand.Models.Entities;

namespace Web2Hand.Data;

public class AppDbContext : IdentityDbContext<AppUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Thêm bảng mới: thêm 1 dòng DbSet ở cuối danh sách + tạo migration.
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Auction> Auctions => Set<Auction>();
    public DbSet<Bid> Bids => Set<Bid>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<Dispute> Disputes => Set<Dispute>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Banner> Banners => Set<Banner>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Cấu hình riêng từng bảng nằm trong Data/Configurations
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Không cho xóa dây chuyền (SQL Server báo lỗi "multiple cascade paths" nếu có nhiều khóa ngoại tới AppUser)
        var entityNamespace = typeof(Product).Namespace;
        var foreignKeys = builder.Model.GetEntityTypes()
            .Where(e => e.ClrType.Namespace == entityNamespace)
            .SelectMany(e => e.GetForeignKeys());
        foreach (var fk in foreignKeys)
            fk.DeleteBehavior = DeleteBehavior.Restrict;
    }
}
