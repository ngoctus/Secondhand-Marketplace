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
    public DbSet<LichSuViPham> LichSuViPhams => Set<LichSuViPham>();
    public DbSet<ThongBao> ThongBaos => Set<ThongBao>();
    public DbSet<TranhChap> TranhChaps => Set<TranhChap>();
    public DbSet<BaoCao> BaoCaos => Set<BaoCao>();
    public DbSet<PhienDauGia> PhienDauGias => Set<PhienDauGia>();
    public DbSet<LuotDatGia> LuotDatGias => Set<LuotDatGia>();
    public DbSet<YeuThich> YeuThichs => Set<YeuThich>();
    public DbSet<Banner> Banners => Set<Banner>();
    public DbSet<DanhGia> DanhGias => Set<DanhGia>();
    public DbSet<HoiThoai> HoiThoais => Set<HoiThoai>();
    public DbSet<TinNhan> TinNhans => Set<TinNhan>();
    public DbSet<TinhThanh> TinhThanhs => Set<TinhThanh>();
    public DbSet<PhuongXa> PhuongXas => Set<PhuongXa>();
    public DbSet<PasswordResetOtp> PasswordResetOtps => Set<PasswordResetOtp>();
    /// <summary>View tổng hợp điểm shop — chỉ đọc (xem database/11_danhgia.sql).</summary>
    public DbSet<DiemCuaHangView> DiemCuaHangViews => Set<DiemCuaHangView>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<PasswordResetOtp>(e =>
        {
            e.ToTable("PasswordResetOtp");
            e.HasKey(x => x.Id);
            e.Property(x => x.Email).IsUnicode(false).HasMaxLength(256);
            e.Property(x => x.CodeProtected).HasMaxLength(512);
            e.HasIndex(x => new { x.Email, x.CreatedAtUtc });
        });

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
            e.Property(x => x.DiaChiChiTiet).HasMaxLength(300);
            e.Property(x => x.KhuVucHienThi).HasMaxLength(100);
            e.Property(x => x.NganHang).HasMaxLength(100);
            e.Property(x => x.MaNganHang).IsUnicode(false).HasMaxLength(10);
            e.Property(x => x.SoTaiKhoan).IsUnicode(false).HasMaxLength(30);
            e.Property(x => x.TenChuTaiKhoan).HasMaxLength(100);
            e.HasOne(x => x.NguoiDung)
                .WithMany()
                .HasForeignKey(x => x.MaNguoiDung)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.TinhThanh)
                .WithMany()
                .HasForeignKey(x => x.MaTinhThanh)
                .OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.PhuongXa)
                .WithMany()
                .HasForeignKey(x => x.MaPhuongXa)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<TinhThanh>(e =>
        {
            e.ToTable("TinhThanh");
            e.HasKey(x => x.MaTinhThanh);
            e.Property(x => x.MaTinhThanh).ValueGeneratedNever();   // mã cố định theo danh mục hành chính, không IDENTITY
            e.Property(x => x.TenTinhThanh).HasMaxLength(100);
            e.Property(x => x.MaHanhChinh).IsUnicode(false).HasMaxLength(10);
        });

        modelBuilder.Entity<PhuongXa>(e =>
        {
            e.ToTable("PhuongXa");
            e.HasKey(x => x.MaPhuongXa);
            e.Property(x => x.MaPhuongXa).ValueGeneratedNever();
            e.Property(x => x.TenPhuongXa).HasMaxLength(100);
            e.Property(x => x.MaHanhChinh).IsUnicode(false).HasMaxLength(10);
            e.HasOne(x => x.TinhThanh)
                .WithMany(t => t.PhuongXas)
                .HasForeignKey(x => x.MaTinhThanh)
                .OnDelete(DeleteBehavior.NoAction);
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

        modelBuilder.Entity<YeuThich>(e =>
        {
            e.ToTable("YeuThich");
            e.HasKey(x => x.MaYeuThich);
            e.HasOne(x => x.NguoiDung)
                .WithMany()
                .HasForeignKey(x => x.MaNguoiDung)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.SanPham)
                .WithMany()
                .HasForeignKey(x => x.MaSanPham)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Banner>(e =>
        {
            e.ToTable("Banner");
            e.HasKey(x => x.MaBanner);
            e.Property(x => x.TieuDe).HasMaxLength(200);
            e.Property(x => x.MoTa).HasMaxLength(500);
            e.Property(x => x.DuongDanAnh).HasMaxLength(500);
            e.Property(x => x.LienKet).HasMaxLength(500);
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
            e.HasIndex(x => new { x.MaNguoiDung, x.MaSanPham }).IsUnique();
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
            e.Property(x => x.HinhThucNhan).IsUnicode(false).HasMaxLength(20).HasDefaultValue("GIAO_HANG");

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
            e.Property(x => x.MucDo).IsUnicode(false).HasMaxLength(4);
        });

        modelBuilder.Entity<LichSuViPham>(e =>
        {
            e.ToTable("LichSuViPham");
            e.HasKey(x => x.MaViPham);
            e.Property(x => x.MucDo).IsUnicode(false).HasMaxLength(4);
            e.Property(x => x.NguonGhiNhan).IsUnicode(false).HasMaxLength(20);
            // Khớp UQ_LichSuViPham_LuotKiemDuyet: một lượt kiểm duyệt tối đa một vi phạm.
            e.HasIndex(x => x.MaLuotKiemDuyet).IsUnique();
        });

        modelBuilder.Entity<ThongBao>(e =>
        {
            e.ToTable("ThongBao");
            e.HasKey(x => x.MaThongBao);
        });

        modelBuilder.Entity<HoiThoai>(e =>
        {
            e.ToTable("HoiThoai");
            e.HasKey(x => x.MaHoiThoai);
            e.Property(x => x.NoiDungCuoi).HasMaxLength(200);
            e.HasIndex(x => new { x.MaNguoiDung1, x.MaNguoiDung2 }).IsUnique();
            e.HasOne(x => x.NguoiDung1).WithMany().HasForeignKey(x => x.MaNguoiDung1).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.NguoiDung2).WithMany().HasForeignKey(x => x.MaNguoiDung2).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<TinNhan>(e =>
        {
            e.ToTable("TinNhan");
            e.HasKey(x => x.MaTinNhan);
            e.Property(x => x.NoiDung).HasMaxLength(1000);
            e.HasOne(x => x.HoiThoai).WithMany(h => h.TinNhans).HasForeignKey(x => x.MaHoiThoai).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.NguoiGui).WithMany().HasForeignKey(x => x.MaNguoiGui).OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<TranhChap>(e =>
        {
            e.ToTable("TranhChap");
            e.HasKey(x => x.MaTranhChap);
            e.Property(x => x.LyDo).HasMaxLength(500);
            e.Property(x => x.BangChungAnh).HasMaxLength(500);
            e.Property(x => x.PhanHoiNguoiBan).HasMaxLength(1000);
            e.Property(x => x.TrangThai).IsUnicode(false).HasMaxLength(20);
            e.Property(x => x.PhanQuyet).HasMaxLength(500);
            e.Property(x => x.GhiChuAdmin).HasMaxLength(1000);
            e.HasOne(x => x.DonHang).WithMany().HasForeignKey(x => x.MaDonHang).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.NguoiTao).WithMany().HasForeignKey(x => x.MaNguoiTao).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.NguoiXuLy).WithMany().HasForeignKey(x => x.NguoiXuLyId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BaoCao>(e =>
        {
            e.ToTable("BaoCao");
            e.HasKey(x => x.MaBaoCao);
            e.HasIndex(x => new { x.MaNguoiBaoCao, x.LoaiDoiTuong, x.MaDoiTuong })
                .HasFilter("[TrangThai] = 'PENDING'")
                .IsUnique();
            e.Property(x => x.LoaiDoiTuong).IsUnicode(false).HasMaxLength(10);
            e.Property(x => x.LyDo).HasMaxLength(255);
            e.Property(x => x.MoTa).HasMaxLength(1000);
            e.Property(x => x.TrangThai).IsUnicode(false).HasMaxLength(15);
            e.Property(x => x.GhiChuXuLy).HasMaxLength(500);
            e.HasOne(x => x.NguoiBaoCao).WithMany().HasForeignKey(x => x.MaNguoiBaoCao).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.NguoiXuLy).WithMany().HasForeignKey(x => x.NguoiXuLyId).OnDelete(DeleteBehavior.Restrict);
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

        modelBuilder.Entity<DanhGia>(e =>
        {
            e.ToTable("DanhGia");
            e.HasKey(x => x.MaDanhGia);
            e.Property(x => x.NoiDung).HasMaxLength(1000);
            e.Property(x => x.TrangThai).IsUnicode(false).HasMaxLength(10);
            e.HasIndex(x => x.MaDonHang).IsUnique();
            e.HasOne(x => x.DonHang)
                .WithOne()
                .HasForeignKey<DanhGia>(x => x.MaDonHang)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.NguoiMua)
                .WithMany()
                .HasForeignKey(x => x.MaNguoiMua)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.CuaHang)
                .WithMany()
                .HasForeignKey(x => x.MaCuaHang)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // View tổng hợp điểm shop — đọc từ database/11_danhgia.sql (MaCuaHang là khóa duy nhất).
        modelBuilder.Entity<DiemCuaHangView>(e =>
        {
            e.HasKey(x => x.MaCuaHang);
            e.ToView("vw_DiemCuaHang");
            e.Property(x => x.MaCuaHang).HasColumnName("MaCuaHang");
            e.Property(x => x.DiemTrungBinh).HasColumnType("decimal(3,2)").HasColumnName("DiemTrungBinh");
            e.Property(x => x.SoDanhGia).HasColumnName("SoDanhGia");
            e.Property(x => x.SoDonThanhCong).HasColumnName("SoDonThanhCong");
        });
    }
}
