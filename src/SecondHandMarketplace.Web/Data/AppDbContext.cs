using Microsoft.EntityFrameworkCore;
using SecondHandMarketplace.Web.Models.Entities;

namespace SecondHandMarketplace.Web.Data;

/// <summary>
/// DbContext khớp với database/02_create_tables.sql (tên bảng và cột tiếng Việt).
/// Thêm bảng mới: tạo entity trong Models/Entities, thêm DbSet và khối cấu hình bên dưới.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<NguoiDung> NguoiDungs => Set<NguoiDung>();
    public DbSet<CuaHang> CuaHangs => Set<CuaHang>();
    public DbSet<DanhMuc> DanhMucs => Set<DanhMuc>();
    public DbSet<TinhTrangSanPham> TinhTrangSanPhams => Set<TinhTrangSanPham>();
    public DbSet<SanPham> SanPhams => Set<SanPham>();
    public DbSet<AnhSanPham> AnhSanPhams => Set<AnhSanPham>();
    public DbSet<NhatKyHeThong> NhatKyHeThongs => Set<NhatKyHeThong>();
    public DbSet<GioHang> GioHangs => Set<GioHang>();
    public DbSet<DonHang> DonHangs => Set<DonHang>();
    public DbSet<ChiTietDonHang> ChiTietDonHangs => Set<ChiTietDonHang>();
    public DbSet<LichSuTrangThaiDonHang> LichSuTrangThaiDonHangs => Set<LichSuTrangThaiDonHang>();
    public DbSet<ThanhToan> ThanhToans => Set<ThanhToan>();
    public DbSet<LichSuTrangThaiSanPham> LichSuTrangThaiSanPhams => Set<LichSuTrangThaiSanPham>();
    public DbSet<LuotKiemDuyet> LuotKiemDuyets => Set<LuotKiemDuyet>();
    public DbSet<PhanTichAI> PhanTichAIs => Set<PhanTichAI>();
    public DbSet<TuKhoaCam> TuKhoaCams => Set<TuKhoaCam>();
    public DbSet<ThongBao> ThongBaos => Set<ThongBao>();
    public DbSet<PhienDauGia> PhienDauGias => Set<PhienDauGia>();
    public DbSet<LuotDatGia> LuotDatGias => Set<LuotDatGia>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<NguoiDung>(e =>
        {
            e.ToTable("NguoiDung");
            e.HasKey(x => x.MaNguoiDung);
            e.Property(x => x.HoTen).HasMaxLength(100);
            e.Property(x => x.Email).IsUnicode(false).HasMaxLength(256);
            e.Property(x => x.SoDienThoai).IsUnicode(false).HasMaxLength(15);
            e.Property(x => x.MatKhauBam).IsUnicode(false).HasMaxLength(256);
            e.Property(x => x.VaiTro).IsUnicode(false).HasMaxLength(10);
            e.Property(x => x.TrangThai).IsUnicode(false).HasMaxLength(10);
            e.Property(x => x.LyDoKhoa).HasMaxLength(500);
            e.Property(x => x.AnhDaiDien).IsUnicode(false).HasMaxLength(500);
            e.Property(x => x.DiaChiMacDinh).HasMaxLength(500);
        });

        modelBuilder.Entity<CuaHang>(e =>
        {
            e.ToTable("CuaHang");
            e.HasKey(x => x.MaCuaHang);
            e.Property(x => x.TenShop).HasMaxLength(100);
            e.Property(x => x.SoDienThoaiLienHe).IsUnicode(false).HasMaxLength(15);
            e.Property(x => x.MoTa).HasMaxLength(1000);
            e.Property(x => x.Logo).IsUnicode(false).HasMaxLength(500);
            e.Property(x => x.TrangThai).IsUnicode(false).HasMaxLength(10);
            e.Property(x => x.LyDoKhoa).HasMaxLength(500);
            e.HasOne(x => x.NguoiDung)
                .WithMany()
                .HasForeignKey(x => x.MaNguoiDung)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DanhMuc>(e =>
        {
            e.ToTable("DanhMuc");
            e.HasKey(x => x.MaDanhMuc);
            e.Property(x => x.Ten).HasMaxLength(100);
        });

        modelBuilder.Entity<TinhTrangSanPham>(e =>
        {
            e.ToTable("TinhTrangSanPham");
            e.HasKey(x => x.MaTinhTrang);
            e.Property(x => x.Ma).IsUnicode(false).HasMaxLength(30);
            e.Property(x => x.Ten).HasMaxLength(100);
            e.Property(x => x.MoTa).HasMaxLength(500);
        });

        modelBuilder.Entity<SanPham>(e =>
        {
            e.ToTable("SanPham", t => t.HasTrigger("trg_SanPham_ChanRutKhoiDauGia"));
            e.HasKey(x => x.MaSanPham);
            e.Property(x => x.Gia).HasPrecision(18, 2);

            e.HasOne(x => x.CuaHang)
                .WithMany()
                .HasForeignKey(x => x.MaCuaHang)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.DanhMuc)
                .WithMany()
                .HasForeignKey(x => x.MaDanhMuc)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.TinhTrang)
                .WithMany()
                .HasForeignKey(x => x.MaTinhTrang)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.Anh)
                .WithOne(a => a.SanPham)
                .HasForeignKey(a => a.MaSanPham)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<AnhSanPham>(e =>
        {
            // Bảng có trigger (giới hạn 10 ảnh) nên phải khai báo để EF không dùng OUTPUT khi INSERT.
            e.ToTable("AnhSanPham", t => t.HasTrigger("trg_AnhSanPham_GioiHan"));
            e.HasKey(x => x.MaAnhSP);
        });

        modelBuilder.Entity<NhatKyHeThong>(e =>
        {
            e.ToTable("NhatKyHeThong");
            e.HasKey(x => x.MaNhatKyHeThong);
            e.Property(x => x.HanhDong).IsUnicode(false).HasMaxLength(50);
            e.Property(x => x.LoaiDoiTuong).IsUnicode(false).HasMaxLength(30);
            e.Property(x => x.KetQua).IsUnicode(false).HasMaxLength(10);
            e.Property(x => x.LyDo).HasMaxLength(500);
        });

        modelBuilder.Entity<GioHang>(e =>
        {
            e.ToTable("GioHang");
            e.HasKey(x => x.MaGioHang);

            e.HasOne(x => x.NguoiDung)
                .WithMany()
                .HasForeignKey(x => x.MaNguoiDung)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.SanPham)
                .WithMany()
                .HasForeignKey(x => x.MaSanPham)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<DonHang>(e =>
        {
            e.ToTable("DonHang");
            e.HasKey(x => x.MaDonHang);
            e.Property(x => x.TienHang).HasPrecision(18, 2);
            e.Property(x => x.PhiVanChuyen).HasPrecision(18, 2);
            e.Property(x => x.TongTien).HasPrecision(18, 2);

            e.HasOne(x => x.NguoiDung)
                .WithMany()
                .HasForeignKey(x => x.MaNguoiDung)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.CuaHang)
                .WithMany()
                .HasForeignKey(x => x.MaCuaHang)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.ChiTiet)
                .WithOne(c => c.DonHang)
                .HasForeignKey(c => c.MaDonHang)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.LichSu)
                .WithOne(l => l.DonHang)
                .HasForeignKey(l => l.MaDonHang)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.ThanhToans)
                .WithOne(t => t.DonHang)
                .HasForeignKey(t => t.MaDonHang)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ChiTietDonHang>(e =>
        {
            // Bảng có trigger nên phải khai báo để EF không dùng OUTPUT khi INSERT.
            e.ToTable("ChiTietDonHang", t => t.HasTrigger("trg_ChiTietDonHang_KiemTra"));
            e.HasKey(x => x.MaChiTietDonHang);
            e.Property(x => x.Gia).HasPrecision(18, 2);

            e.HasOne(x => x.SanPham)
                .WithMany()
                .HasForeignKey(x => x.MaSanPham)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<LichSuTrangThaiDonHang>(e =>
        {
            e.ToTable("LichSuTrangThaiDonHang");
            e.HasKey(x => x.MaLichSuTrangThaiDonHang);
        });

        modelBuilder.Entity<ThanhToan>(e =>
        {
            e.ToTable("ThanhToan");
            e.HasKey(x => x.MaThanhToan);
            e.Property(x => x.SoTien).HasPrecision(18, 2);
        });

        modelBuilder.Entity<LichSuTrangThaiSanPham>(e =>
        {
            e.ToTable("LichSuTrangThaiSanPham");
            e.HasKey(x => x.MaLichSuTrangThaiSanPham);
        });

        modelBuilder.Entity<LuotKiemDuyet>(e =>
        {
            // Bảng có trigger nên phải khai báo để EF không dùng OUTPUT khi INSERT.
            e.ToTable("LuotKiemDuyet", t => t.HasTrigger("trg_LuotKiemDuyet_KiemTra"));
            e.HasKey(x => x.MaLuotKiemDuyet);

            e.HasOne(x => x.SanPham)
                .WithMany()
                .HasForeignKey(x => x.MaSanPham)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.PhanTich)
                .WithOne(p => p.LuotKiemDuyet)
                .HasForeignKey<PhanTichAI>(p => p.MaLuotKiemDuyet)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PhanTichAI>(e =>
        {
            e.ToTable("PhanTichAI");
            e.HasKey(x => x.MaPhanTichAI);
        });

        modelBuilder.Entity<TuKhoaCam>(e =>
        {
            e.ToTable("TuKhoaCam");
            e.HasKey(x => x.MaTuKhoaCam);
        });

        modelBuilder.Entity<ThongBao>(e =>
        {
            e.ToTable("ThongBao");
            e.HasKey(x => x.MaThongBao);
        });

        modelBuilder.Entity<PhienDauGia>(e =>
        {
            e.ToTable("PhienDauGia");
            e.HasKey(x => x.MaPhienDauGia);
            e.Property(x => x.GiaNiemYet).HasPrecision(18, 2);
            e.Property(x => x.GiaKhoiDiem).HasPrecision(18, 2);
            e.Property(x => x.BuocGia).HasPrecision(18, 2);
            e.Property(x => x.GiaHienTai).HasPrecision(18, 2);
            e.Property(x => x.GiaThang).HasPrecision(18, 2);
            e.Property(x => x.TrangThai).IsUnicode(false).HasMaxLength(12);
            e.Property(x => x.NguoiHuy).IsUnicode(false).HasMaxLength(10);
            e.Property(x => x.LyDoHuy).HasMaxLength(500);

            e.HasOne(x => x.SanPham).WithMany().HasForeignKey(x => x.MaSanPham).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.NguoiDanDau).WithMany().HasForeignKey(x => x.MaNguoiDanDau).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.NguoiThang).WithMany().HasForeignKey(x => x.MaNguoiThang).OnDelete(DeleteBehavior.Restrict);
            e.HasMany(x => x.LuotDat).WithOne(l => l.Phien).HasForeignKey(l => l.MaPhienDauGia).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<LuotDatGia>(e =>
        {
            e.ToTable("LuotDatGia");
            e.HasKey(x => x.MaLuotDatGia);
            e.Property(x => x.SoTien).HasPrecision(18, 2);
            e.HasOne(x => x.NguoiDung).WithMany().HasForeignKey(x => x.MaNguoiDung).OnDelete(DeleteBehavior.Restrict);
        });
    }
}