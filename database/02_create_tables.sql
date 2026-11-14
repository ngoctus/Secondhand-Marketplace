SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ARITHABORT ON;
GO

USE SecondHandMarketplace;
GO
-- Tạo lại cấu trúc trên database mới. run-db.bat kiểm tra database chưa tồn tại trước khi chạy.
-- Toàn bộ 30 bảng đã ở trạng thái schema cuối cùng; không còn ALTER TABLE ADD COLUMN ở các file module.

DROP TABLE IF EXISTS dbo.PasswordResetOtp;
DROP TABLE IF EXISTS dbo.TinNhan;
DROP TABLE IF EXISTS dbo.HoiThoai;
DROP TABLE IF EXISTS dbo.LuotDatGia;
DROP TABLE IF EXISTS dbo.PhienDauGia;
DROP TABLE IF EXISTS dbo.LichSuViPham;
DROP TABLE IF EXISTS dbo.BaoCao;
DROP TABLE IF EXISTS dbo.TranhChap;
DROP TABLE IF EXISTS dbo.DanhGia;
DROP TABLE IF EXISTS dbo.YeuThich;
DROP TABLE IF EXISTS dbo.Banner;
DROP TABLE IF EXISTS dbo.CauHinhAI;
DROP TABLE IF EXISTS dbo.ThongBao;
DROP TABLE IF EXISTS dbo.NhatKyHeThong;
DROP TABLE IF EXISTS dbo.TuKhoaCam;
DROP TABLE IF EXISTS dbo.PhanTichAI;
DROP TABLE IF EXISTS dbo.LuotKiemDuyet;
DROP TABLE IF EXISTS dbo.LichSuTrangThaiSanPham;
DROP TABLE IF EXISTS dbo.ThanhToan;
DROP TABLE IF EXISTS dbo.LichSuTrangThaiDonHang;
DROP TABLE IF EXISTS dbo.ChiTietDonHang;
DROP TABLE IF EXISTS dbo.DonHang;
DROP TABLE IF EXISTS dbo.GioHang;
DROP TABLE IF EXISTS dbo.AnhSanPham;
DROP TABLE IF EXISTS dbo.SanPham;
DROP TABLE IF EXISTS dbo.DanhMuc;
DROP TABLE IF EXISTS dbo.TinhTrangSanPham;
DROP TABLE IF EXISTS dbo.CuaHang;
DROP TABLE IF EXISTS dbo.PhuongXa;
DROP TABLE IF EXISTS dbo.TinhThanh;
DROP TABLE IF EXISTS dbo.NguoiDung;
GO

CREATE TABLE NguoiDung (
    MaNguoiDung     int            IDENTITY(1,1) NOT NULL,
    HoTen           nvarchar(100)  NOT NULL,
    Email           varchar(256)   NOT NULL,
    SoDienThoai     varchar(15)    NULL,
    MatKhauBam      varchar(256)   NOT NULL,
    VaiTro          varchar(10)    NOT NULL CONSTRAINT DF_NguoiDung_VaiTro    DEFAULT ('USER'),
    TrangThai       varchar(10)    NOT NULL CONSTRAINT DF_NguoiDung_TrangThai DEFAULT ('ACTIVE'),
    LyDoKhoa        nvarchar(500)  NULL,
    ThoiGianKhoa    datetime2(3)   NULL,
    AnhDaiDien      varchar(500)   NULL,
    DiaChiMacDinh   nvarchar(500)  NULL,
    NgayTao         datetime2(3)   NOT NULL CONSTRAINT DF_NguoiDung_NgayTao     DEFAULT (SYSUTCDATETIME()),
    NgayCapNhat     datetime2(3)   NOT NULL CONSTRAINT DF_NguoiDung_NgayCapNhat DEFAULT (SYSUTCDATETIME())
);
GO

CREATE TABLE CuaHang (
    MaCuaHang          int            IDENTITY(1,1) NOT NULL,
    MaNguoiDung        int            NOT NULL,
    TenShop            nvarchar(100)  NOT NULL,
    SoDienThoaiLienHe  varchar(15)    NOT NULL,
    MoTa               nvarchar(1000) NULL,
    Logo               varchar(500)   NULL,
    TrangThai          varchar(10)    NOT NULL CONSTRAINT DF_CuaHang_TrangThai DEFAULT ('ACTIVE'),
    LyDoKhoa           nvarchar(500)  NULL,
    ThoiGianKhoa       datetime2(3)   NULL,
    NgayTao            datetime2(3)   NOT NULL CONSTRAINT DF_CuaHang_NgayTao     DEFAULT (SYSUTCDATETIME()),
    NgayCapNhat        datetime2(3)   NOT NULL CONSTRAINT DF_CuaHang_NgayCapNhat DEFAULT (SYSUTCDATETIME()),
    MaTinhThanh     int            NULL,
    MaPhuongXa      int            NULL,
    DiaChiChiTiet   nvarchar(300)  NULL,
    KhuVucHienThi   nvarchar(100)  NULL,
    NganHang        nvarchar(100)  NULL,
    MaNganHang      varchar(10)    NULL,
    SoTaiKhoan      varchar(30)    NULL,
    TenChuTaiKhoan  nvarchar(100)  NULL
);
GO

CREATE TABLE DanhMuc (
    MaDanhMuc         int           IDENTITY(1,1) NOT NULL,
    Ten               nvarchar(100) NOT NULL,
    ThuTuHienThi      int           NOT NULL CONSTRAINT DF_DanhMuc_ThuTu       DEFAULT (0),
    DangHienThi       bit           NOT NULL CONSTRAINT DF_DanhMuc_DangHienThi DEFAULT (1)
);
GO

CREATE TABLE TinhTrangSanPham (
    MaTinhTrang       int           IDENTITY(1,1) NOT NULL,
    Ma                varchar(30)   NOT NULL,
    Ten               nvarchar(100) NOT NULL,
    MoTa              nvarchar(500) NULL,
    ThuTuHienThi      int           NOT NULL CONSTRAINT DF_TinhTrang_ThuTu      DEFAULT (0),
    DangSuDung        bit           NOT NULL CONSTRAINT DF_TinhTrang_DangSuDung DEFAULT (1)
);
GO

CREATE TABLE SanPham (
    MaSanPham         int            IDENTITY(1,1) NOT NULL,
    MaCuaHang         int            NOT NULL,
    MaDanhMuc         int            NULL,
    MaTinhTrang       int            NULL,
    Ten               nvarchar(200)  NOT NULL,
    ChuoiTimKiem      nvarchar(200)  NOT NULL CONSTRAINT DF_SanPham_ChuoiTimKiem DEFAULT (N''),
    MoTa              nvarchar(4000) NULL,
    Gia               decimal(18,2)  NULL,
    TrangThai         varchar(20)    NOT NULL CONSTRAINT DF_SanPham_TrangThai DEFAULT ('DRAFT'),
    LyDoGo            nvarchar(500)  NULL,
    DaXoa             bit            NOT NULL CONSTRAINT DF_SanPham_DaXoa DEFAULT (0),
    ThoiGianXoa       datetime2(3)   NULL,
    PhienBanDong      rowversion     NOT NULL,
    NgayTao           datetime2(3)   NOT NULL CONSTRAINT DF_SanPham_NgayTao     DEFAULT (SYSUTCDATETIME()),
    NgayCapNhat       datetime2(3)   NOT NULL CONSTRAINT DF_SanPham_NgayCapNhat DEFAULT (SYSUTCDATETIME())
);
GO

CREATE TABLE AnhSanPham (
    MaAnhSP           int           IDENTITY(1,1) NOT NULL,
    MaSanPham         int           NOT NULL,
    DuongDan          nvarchar(500) NOT NULL,
    ThuTu             int           NOT NULL CONSTRAINT DF_AnhSanPham_ThuTu      DEFAULT (1),
    LaAnhChinh        bit           NOT NULL CONSTRAINT DF_AnhSanPham_LaAnhChinh DEFAULT (0),
    DungLuong         int           NOT NULL,
    NgayTao           datetime2(3)  NOT NULL CONSTRAINT DF_AnhSanPham_NgayTao    DEFAULT (SYSUTCDATETIME())
);
GO

CREATE TABLE GioHang (
    MaGioHang         int           IDENTITY(1,1) NOT NULL,
    MaNguoiDung       int           NOT NULL,
    MaSanPham         int           NOT NULL,
    DaChon            bit           NOT NULL CONSTRAINT DF_GioHang_DaChon      DEFAULT (1),
    ThoiGianThem      datetime2(3)  NOT NULL CONSTRAINT DF_GioHang_ThoiGianThem DEFAULT (SYSUTCDATETIME())
);
GO

CREATE TABLE DonHang (
    MaDonHang            int            IDENTITY(1,1) NOT NULL,
    MaDon                varchar(30)    NOT NULL,
    MaNguoiDung          int            NOT NULL,
    MaCuaHang            int            NOT NULL,
    TenNguoiNhan         nvarchar(100)  NOT NULL,
    SoDienThoaiNhan      varchar(15)    NOT NULL,
    DiaChiNhan           nvarchar(500)  NOT NULL,
    TienHang             decimal(18,2)  NOT NULL,
    PhiVanChuyen         decimal(18,2)  NOT NULL CONSTRAINT DF_DonHang_PhiVanChuyen DEFAULT (0),
    TongTien             decimal(18,2)  NOT NULL,
    PhuongThucThanhToan  varchar(20)    NOT NULL CONSTRAINT DF_DonHang_PhuongThuc  DEFAULT ('COD'),
    TrangThaiThanhToan   varchar(10)    NOT NULL CONSTRAINT DF_DonHang_TTThanhToan DEFAULT ('UNPAID'),
    TrangThai            varchar(15)    NOT NULL CONSTRAINT DF_DonHang_TrangThai    DEFAULT ('PENDING'),
    NguoiHuy             varchar(10)    NULL,
    LyDoHuy              nvarchar(500)  NULL,
    ThoiGianHuy          datetime2(3)   NULL,
    ThoiGianXacNhan      datetime2(3)   NULL,
    ThoiGianBatDauGiao   datetime2(3)   NULL,
    ThoiGianDaGiao       datetime2(3)   NULL,
    ThoiGianHoanTat      datetime2(3)   NULL,
    NgayTao              datetime2(3)   NOT NULL CONSTRAINT DF_DonHang_NgayTao     DEFAULT (SYSUTCDATETIME()),
    NgayCapNhat          datetime2(3)   NOT NULL CONSTRAINT DF_DonHang_NgayCapNhat DEFAULT (SYSUTCDATETIME()),
    HinhThucNhan    varchar(20)    NOT NULL CONSTRAINT DF_DonHang_HinhThucNhan DEFAULT ('GIAO_HANG'),
    KhungGioNhan    nvarchar(200)  NULL
);
GO

CREATE TABLE ChiTietDonHang (
    MaChiTietDonHang  int            IDENTITY(1,1) NOT NULL,
    MaDonHang         int            NOT NULL,
    MaSanPham         int            NOT NULL,
    TenSanPham        nvarchar(200)  NOT NULL,
    AnhDaiDien        nvarchar(500)  NULL,
    Gia               decimal(18,2)  NOT NULL,
    DangGiuCho        bit            NOT NULL CONSTRAINT DF_ChiTietDonHang_DangGiuCho DEFAULT (1)
);
GO

CREATE TABLE LichSuTrangThaiDonHang (
    MaLichSuTrangThaiDonHang int          IDENTITY(1,1) NOT NULL,
    MaDonHang         int           NOT NULL,
    TrangThaiCu       varchar(15)   NULL,
    TrangThaiMoi      varchar(15)   NOT NULL,
    LoaiNguoiThucHien varchar(10)   NOT NULL,
    NguoiThucHienId   int           NULL,
    LyDo              nvarchar(500) NULL,
    ThoiGian          datetime2(3)  NOT NULL CONSTRAINT DF_LichSuDon_ThoiGian DEFAULT (SYSUTCDATETIME())
);
GO

CREATE TABLE ThanhToan (
    MaThanhToan          int            IDENTITY(1,1) NOT NULL,
    MaDonHang            int            NOT NULL,
    MaGiaoDich           varchar(100)   NULL,
    PhuongThucThanhToan  varchar(20)    NOT NULL,
    SoTien               decimal(18,2)  NOT NULL,
    TrangThaiThanhToan   varchar(10)    NOT NULL CONSTRAINT DF_ThanhToan_TrangThai DEFAULT ('PENDING'),
    ThoiGianThanhToan    datetime2(3)   NULL,
    NgayTao              datetime2(3)   NOT NULL CONSTRAINT DF_ThanhToan_NgayTao DEFAULT (SYSUTCDATETIME())
);
GO

CREATE TABLE LichSuTrangThaiSanPham (
    MaLichSuTrangThaiSanPham int           IDENTITY(1,1) NOT NULL,
    MaSanPham         int           NOT NULL,
    TrangThaiCu       varchar(20)   NULL,
    TrangThaiMoi      varchar(20)   NOT NULL,
    LoaiNguoiThucHien varchar(10)   NOT NULL,
    NguoiThucHienId   int           NULL,
    LyDo              nvarchar(500) NULL,
    ThoiGian          datetime2(3)  NOT NULL CONSTRAINT DF_LichSuSanPham_ThoiGian DEFAULT (SYSUTCDATETIME())
);
GO

CREATE TABLE LuotKiemDuyet (
    MaLuotKiemDuyet    int           IDENTITY(1,1) NOT NULL,
    MaSanPham          int           NOT NULL,
    ThoiGianGui        datetime2(3)  NOT NULL CONSTRAINT DF_LuotKiemDuyet_ThoiGianGui DEFAULT (SYSUTCDATETIME()),
    KetQuaRule         nvarchar(max) NULL,
    QuyetDinh          varchar(10)   NULL,
    LyDo               nvarchar(500) NULL,
    MaQuanTriVien      int           NULL,
    ThoiGianQuyetDinh  datetime2(3)  NULL,
    QuyetDinhTuDong bit           NOT NULL CONSTRAINT DF_LuotKiemDuyet_QuyetDinhTuDong DEFAULT (0)
);
GO

CREATE TABLE PhanTichAI (
    MaPhanTichAI       int            IDENTITY(1,1) NOT NULL,
    MaLuotKiemDuyet    int            NOT NULL,
    TrangThai          varchar(12)    NOT NULL CONSTRAINT DF_PhanTichAI_TrangThai DEFAULT ('QUEUED'),
    SoLanThu           int            NOT NULL CONSTRAINT DF_PhanTichAI_SoLanThu  DEFAULT (0),
    MucRuiRo           varchar(10)    NULL,
    MucDoTuKhoa        varchar(4)     NULL,
    DauHieu            nvarchar(max)  NULL,
    DeXuat             varchar(15)    NULL,
    GiaiThich          nvarchar(2000) NULL,
    MaBamDauVao        char(64)       NULL,
    PhienBanMoHinh     varchar(50)    NULL,
    ThoiGianXuLyMs     int            NULL,
    LoiXuLy            nvarchar(1000) NULL,
    QuanTriVienLamTheo bit            NULL,
    NgayTao            datetime2(3)   NOT NULL CONSTRAINT DF_PhanTichAI_NgayTao DEFAULT (SYSUTCDATETIME()),
    ThoiGianHoanThanh  datetime2(3)   NULL
);
GO

CREATE TABLE TuKhoaCam (
    MaTuKhoaCam       int           IDENTITY(1,1) NOT NULL,
    CumTu             nvarchar(100) NOT NULL,
    CumTuChuanHoa     nvarchar(100) NOT NULL,
    Nhom              varchar(30)   NULL,
    GhiChu            nvarchar(500) NULL,
    DangApDung        bit           NOT NULL CONSTRAINT DF_TuKhoaCam_DangApDung DEFAULT (1),
    NgayTao           datetime2(3)  NOT NULL CONSTRAINT DF_TuKhoaCam_NgayTao    DEFAULT (SYSUTCDATETIME()),
    MucDo           varchar(4)     NOT NULL CONSTRAINT DF_TuKhoaCam_MucDo DEFAULT ('LOW')
);
GO

CREATE TABLE ThongBao (
    MaThongBao        int            IDENTITY(1,1) NOT NULL,
    MaNguoiDung       int            NOT NULL,
    Loai              varchar(10)    NOT NULL,
    TieuDe            nvarchar(200)  NOT NULL,
    NoiDung           nvarchar(1000) NOT NULL,
    MaThamChieu       int            NULL,
    DaDoc             bit            NOT NULL CONSTRAINT DF_ThongBao_DaDoc DEFAULT (0),
    NgayTao           datetime2(3)   NOT NULL CONSTRAINT DF_ThongBao_NgayTao DEFAULT (SYSUTCDATETIME())
);
GO

CREATE TABLE NhatKyHeThong (
    MaNhatKyHeThong   bigint        IDENTITY(1,1) NOT NULL,
    MaNguoiDung       int           NULL,
    HanhDong          varchar(50)   NOT NULL,
    LoaiDoiTuong      varchar(30)   NOT NULL,
    DoiTuongId        int           NULL,
    KetQua            varchar(10)   NOT NULL CONSTRAINT DF_NhatKy_KetQua  DEFAULT ('SUCCESS'),
    LyDo              nvarchar(500) NULL,
    ChiTiet           nvarchar(max) NULL,
    ThoiGian          datetime2(3)  NOT NULL CONSTRAINT DF_NhatKy_ThoiGian DEFAULT (SYSUTCDATETIME())
);
GO

IF OBJECT_ID(N'dbo.YeuThich', N'U') IS NULL
BEGIN
    CREATE TABLE YeuThich (
        MaYeuThich      int           IDENTITY(1,1) NOT NULL,
        MaNguoiDung     int           NOT NULL,
        MaSanPham       int           NOT NULL,
        NgayTao         datetime2(3)  NOT NULL CONSTRAINT DF_YeuThich_NgayTao DEFAULT (SYSUTCDATETIME())
    );
END;
GO
GO

IF OBJECT_ID(N'dbo.Banner', N'U') IS NULL
BEGIN
    CREATE TABLE Banner (
        MaBanner        int           IDENTITY(1,1) NOT NULL,
        TieuDe          nvarchar(200) NOT NULL,
        MoTa            nvarchar(500) NULL,
        DuongDanAnh     nvarchar(500) NOT NULL,
        LienKet         nvarchar(500) NULL,
        ThuTuHienThi    int           NOT NULL CONSTRAINT DF_Banner_ThuTu DEFAULT (1),
        DangHienThi     bit           NOT NULL CONSTRAINT DF_Banner_DangHienThi DEFAULT (1),
        BatDauTu        datetime2(3)  NULL,
        KetThucLuc      datetime2(3)  NULL,
        NgayTao         datetime2(3)  NOT NULL CONSTRAINT DF_Banner_NgayTao DEFAULT (SYSUTCDATETIME()),
        NgayCapNhat     datetime2(3)  NOT NULL CONSTRAINT DF_Banner_NgayCapNhat DEFAULT (SYSUTCDATETIME())
    );
END;
GO
GO

IF OBJECT_ID(N'dbo.DanhGia', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DanhGia (
        MaDanhGia     int            IDENTITY(1,1) NOT NULL,
        MaDonHang     int            NOT NULL,
        MaNguoiMua    int            NOT NULL,
        MaCuaHang     int            NOT NULL,
        SoSao         tinyint        NOT NULL,
        NoiDung       nvarchar(1000) NULL,
        TrangThai     varchar(10)    NOT NULL CONSTRAINT DF_DanhGia_TrangThai DEFAULT ('VISIBLE'),
        NgayTao       datetime2(3)   NOT NULL CONSTRAINT DF_DanhGia_NgayTao   DEFAULT (SYSUTCDATETIME())
    );
END;
GO
GO

IF OBJECT_ID(N'dbo.TranhChap', N'U') IS NULL
BEGIN
    CREATE TABLE TranhChap (
        MaTranhChap       int            IDENTITY(1,1) NOT NULL,
        MaDonHang         int            NOT NULL,
        MaNguoiTao        int            NOT NULL,
        LyDo              nvarchar(500)  NOT NULL,
        BangChungAnh      nvarchar(500)  NULL,
        PhanHoiNguoiBan   nvarchar(1000) NULL,
        TrangThai         varchar(20)    NOT NULL CONSTRAINT DF_TranhChap_TrangThai DEFAULT ('OPEN'),
        PhanQuyet         nvarchar(500)  NULL,
        GhiChuAdmin       nvarchar(1000) NULL,
        NguoiXuLyId       int            NULL,
        NgayTao           datetime2(3)   NOT NULL CONSTRAINT DF_TranhChap_NgayTao DEFAULT (SYSUTCDATETIME()),
        NgayCapNhat       datetime2(3)   NOT NULL CONSTRAINT DF_TranhChap_NgayCapNhat DEFAULT (SYSUTCDATETIME()),
        NgayGiaiQuyet     datetime2(3)   NULL
    );
END;
GO
GO

IF OBJECT_ID(N'dbo.BaoCao', N'U') IS NULL
BEGIN
    CREATE TABLE BaoCao (
        MaBaoCao          int            IDENTITY(1,1) NOT NULL,
        LoaiDoiTuong      varchar(10)    NOT NULL,
        MaDoiTuong        int            NOT NULL,
        MaNguoiBaoCao     int            NOT NULL,
        LyDo              nvarchar(255)  NOT NULL,
        MoTa              nvarchar(1000) NULL,
        TrangThai         varchar(15)    NOT NULL CONSTRAINT DF_BaoCao_TrangThai DEFAULT ('PENDING'),
        NguoiXuLyId       int            NULL,
        GhiChuXuLy        nvarchar(500)  NULL,
        NgayTao           datetime2(3)   NOT NULL CONSTRAINT DF_BaoCao_NgayTao DEFAULT (SYSUTCDATETIME()),
        NgayXuLy          datetime2(3)   NULL
    );
END;
GO
GO

IF OBJECT_ID(N'dbo.PhienDauGia', N'U') IS NULL
BEGIN
    CREATE TABLE PhienDauGia (
        MaPhienDauGia    int           IDENTITY(1,1) NOT NULL,
        MaSanPham        int           NOT NULL,
        GiaNiemYet       decimal(18,2) NOT NULL,
        -- giá gốc của sản phẩm lúc tạo phiên (để khôi phục)
        GiaKhoiDiem      decimal(18,2) NOT NULL,
        BuocGia          decimal(18,2) NOT NULL,
        GiaHienTai       decimal(18,2) NOT NULL,
        SoLuotDat        int           NOT NULL CONSTRAINT DF_PhienDauGia_SoLuot DEFAULT (0),
        MaNguoiDanDau    int           NULL,
        ThoiGianBatDau   datetime2(3)  NOT NULL,
        ThoiGianKetThuc  datetime2(3)  NOT NULL,
        TrangThai        varchar(12)   NOT NULL CONSTRAINT DF_PhienDauGia_TrangThai DEFAULT ('SCHEDULED'),
        MaNguoiThang     int           NULL,
        GiaThang         decimal(18,2) NULL,
        MaDonHang        int           NULL,
        NguoiHuy         varchar(10)   NULL,
        LyDoHuy          nvarchar(500) NULL,
        ThoiGianHuy      datetime2(3)  NULL,
        NgayTao          datetime2(3)  NOT NULL CONSTRAINT DF_PhienDauGia_NgayTao     DEFAULT (SYSUTCDATETIME()),
        NgayCapNhat      datetime2(3)  NOT NULL CONSTRAINT DF_PhienDauGia_NgayCapNhat DEFAULT (SYSUTCDATETIME())
    );
END;
GO
GO

IF OBJECT_ID(N'dbo.LuotDatGia', N'U') IS NULL
BEGIN
    CREATE TABLE LuotDatGia (
        MaLuotDatGia   bigint        IDENTITY(1,1) NOT NULL,
        MaPhienDauGia  int           NOT NULL,
        MaNguoiDung    int           NOT NULL,
        SoTien         decimal(18,2) NOT NULL,
        ThoiGian       datetime2(3)  NOT NULL CONSTRAINT DF_LuotDatGia_ThoiGian DEFAULT (SYSUTCDATETIME())
    );
END;
GO
GO

IF OBJECT_ID(N'dbo.LichSuViPham', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.LichSuViPham (
        MaViPham         int           IDENTITY(1,1) NOT NULL,
        MaNguoiBan       int           NOT NULL,
        MaSanPham        int           NOT NULL,
        MaLuotKiemDuyet  int           NOT NULL,
        MucDo            varchar(4)    NOT NULL,
        NguonGhiNhan     varchar(20)   NOT NULL,
        ChiTietTuKhoa    nvarchar(max) NOT NULL,
        MaQuanTriVien    int           NULL,
        ThoiGian         datetime2(3)  NOT NULL CONSTRAINT DF_LichSuViPham_ThoiGian DEFAULT (SYSUTCDATETIME())
    );
END;
GO
GO

IF OBJECT_ID(N'dbo.HoiThoai', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HoiThoai (
        MaHoiThoai    int           IDENTITY(1,1) NOT NULL,
        MaNguoiDung1  int           NOT NULL,
        MaNguoiDung2  int           NOT NULL,
        NoiDungCuoi   nvarchar(200) NULL,
        NgayTinCuoi   datetime2(3)  NULL,
        NgayTao       datetime2(3)  NOT NULL CONSTRAINT DF_HoiThoai_NgayTao DEFAULT (SYSUTCDATETIME())
    );
END;
GO
GO

IF OBJECT_ID(N'dbo.TinNhan', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TinNhan (
        MaTinNhan   bigint         IDENTITY(1,1) NOT NULL,
        MaHoiThoai  int            NOT NULL,
        MaNguoiGui  int            NOT NULL,
        NoiDung     nvarchar(1000) NOT NULL,
        DaDoc       bit            NOT NULL CONSTRAINT DF_TinNhan_DaDoc DEFAULT (0),
        NgayGui     datetime2(3)   NOT NULL CONSTRAINT DF_TinNhan_NgayGui DEFAULT (SYSUTCDATETIME())
    );
END;
GO
GO

IF OBJECT_ID(N'dbo.CauHinhAI', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CauHinhAI (
        MaCauHinhAI     int            NOT NULL,
        DangBat         bit            NOT NULL CONSTRAINT DF_CauHinhAI_DangBat      DEFAULT (1),
        TenModel        varchar(100)   NOT NULL,
        SystemPrompt    nvarchar(4000) NOT NULL,
        MaNguoiCapNhat  int            NULL,
        NgayCapNhat     datetime2(3)   NOT NULL CONSTRAINT DF_CauHinhAI_NgayCapNhat  DEFAULT (SYSUTCDATETIME())
    );
END;
GO
GO

IF OBJECT_ID(N'dbo.TinhThanh', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TinhThanh (
        MaTinhThanh  int           NOT NULL,
        TenTinhThanh nvarchar(100) NOT NULL,
        MaHanhChinh  varchar(10)   NOT NULL
    );
END;
GO
GO

IF OBJECT_ID(N'dbo.PhuongXa', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PhuongXa (
        MaPhuongXa   int           NOT NULL,
        MaTinhThanh  int           NOT NULL,
        TenPhuongXa  nvarchar(100) NOT NULL,
        MaHanhChinh  varchar(10)   NOT NULL
    );
END;
GO

IF OBJECT_ID(N'dbo.PasswordResetOtp', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PasswordResetOtp (
        Id             bigint        IDENTITY(1,1) NOT NULL,
        Email          varchar(256)  NOT NULL,
        CodeProtected  nvarchar(512) NOT NULL,
        CreatedAtUtc   datetime2(0)  NOT NULL,
        ExpiresAtUtc   datetime2(0)  NOT NULL,
        UsedAtUtc      datetime2(0)  NULL,
        FailedAttempts int           NOT NULL CONSTRAINT DF_PasswordResetOtp_FailedAttempts DEFAULT (0)
    );
END;
GO

DROP SEQUENCE IF EXISTS dbo.sq_MaDonHang;
CREATE SEQUENCE dbo.sq_MaDonHang AS int START WITH 1 INCREMENT BY 1 CACHE 50;
GO

PRINT N'Đã tạo xong 31 bảng và sequence sq_MaDonHang.';
GO
