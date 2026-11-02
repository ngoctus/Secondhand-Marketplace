SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ARITHABORT ON;
GO

USE SecondHandMarketplace;
GO

DROP TABLE IF EXISTS NhatKyHeThong;
DROP TABLE IF EXISTS TuKhoaCam;
DROP TABLE IF EXISTS PhanTichAI;
DROP TABLE IF EXISTS LuotKiemDuyet;
DROP TABLE IF EXISTS LichSuTrangThaiSanPham;
DROP TABLE IF EXISTS ThanhToan;
DROP TABLE IF EXISTS LichSuTrangThaiDonHang;
DROP TABLE IF EXISTS ChiTietDonHang;
DROP TABLE IF EXISTS DonHang;
DROP TABLE IF EXISTS GioHang;
DROP TABLE IF EXISTS AnhSanPham;
DROP TABLE IF EXISTS SanPham;
DROP TABLE IF EXISTS TinhTrangSanPham;
DROP TABLE IF EXISTS DanhMuc;
DROP TABLE IF EXISTS CuaHang;
DROP TABLE IF EXISTS NguoiDung;

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
    NgayCapNhat     datetime2(3)   NOT NULL CONSTRAINT DF_NguoiDung_NgayCapNhat DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_NguoiDung PRIMARY KEY CLUSTERED (MaNguoiDung)
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
    CONSTRAINT PK_CuaHang PRIMARY KEY CLUSTERED (MaCuaHang)
);
GO

CREATE TABLE DanhMuc (
    MaDanhMuc         int           IDENTITY(1,1) NOT NULL,
    Ten               nvarchar(100) NOT NULL,
    ThuTuHienThi      int           NOT NULL CONSTRAINT DF_DanhMuc_ThuTu       DEFAULT (0),
    DangHienThi       bit           NOT NULL CONSTRAINT DF_DanhMuc_DangHienThi DEFAULT (1),
    CONSTRAINT PK_DanhMuc PRIMARY KEY CLUSTERED (MaDanhMuc)
);
GO

CREATE TABLE TinhTrangSanPham (
    MaTinhTrang       int           IDENTITY(1,1) NOT NULL,
    Ma                varchar(30)   NOT NULL,
    Ten               nvarchar(100) NOT NULL,
    MoTa              nvarchar(500) NULL,
    ThuTuHienThi      int           NOT NULL CONSTRAINT DF_TinhTrang_ThuTu      DEFAULT (0),
    DangSuDung        bit           NOT NULL CONSTRAINT DF_TinhTrang_DangSuDung DEFAULT (1),
    CONSTRAINT PK_TinhTrangSanPham PRIMARY KEY CLUSTERED (MaTinhTrang)
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
    NgayCapNhat       datetime2(3)   NOT NULL CONSTRAINT DF_SanPham_NgayCapNhat DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_SanPham PRIMARY KEY CLUSTERED (MaSanPham)
);
GO

CREATE TABLE AnhSanPham (
    MaAnhSP           int           IDENTITY(1,1) NOT NULL,
    MaSanPham         int           NOT NULL,
    DuongDan          nvarchar(500) NOT NULL,
    ThuTu             int           NOT NULL CONSTRAINT DF_AnhSanPham_ThuTu      DEFAULT (1),
    LaAnhChinh        bit           NOT NULL CONSTRAINT DF_AnhSanPham_LaAnhChinh DEFAULT (0),
    DungLuong         int           NOT NULL,
    NgayTao           datetime2(3)  NOT NULL CONSTRAINT DF_AnhSanPham_NgayTao    DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_AnhSanPham PRIMARY KEY CLUSTERED (MaAnhSP)
);
GO

CREATE TABLE GioHang (
    MaGioHang         int           IDENTITY(1,1) NOT NULL,
    MaNguoiDung       int           NOT NULL,
    MaSanPham         int           NOT NULL,
    DaChon            bit           NOT NULL CONSTRAINT DF_GioHang_DaChon      DEFAULT (1),
    ThoiGianThem      datetime2(3)  NOT NULL CONSTRAINT DF_GioHang_ThoiGianThem DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_GioHang PRIMARY KEY CLUSTERED (MaGioHang)
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
    CONSTRAINT PK_DonHang PRIMARY KEY CLUSTERED (MaDonHang)
);
GO

CREATE TABLE ChiTietDonHang (
    MaChiTietDonHang  int            IDENTITY(1,1) NOT NULL,
    MaDonHang         int            NOT NULL,
    MaSanPham         int            NOT NULL,
    TenSanPham        nvarchar(200)  NOT NULL,
    AnhDaiDien        nvarchar(500)  NULL,
    Gia               decimal(18,2)  NOT NULL,
    DangGiuCho        bit            NOT NULL CONSTRAINT DF_ChiTietDonHang_DangGiuCho DEFAULT (1),
    CONSTRAINT PK_ChiTietDonHang PRIMARY KEY CLUSTERED (MaChiTietDonHang)
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
    ThoiGian          datetime2(3)  NOT NULL CONSTRAINT DF_LichSuDon_ThoiGian DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_LichSuTrangThaiDonHang PRIMARY KEY CLUSTERED (MaLichSuTrangThaiDonHang)
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
    NgayTao              datetime2(3)   NOT NULL CONSTRAINT DF_ThanhToan_NgayTao DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_ThanhToan PRIMARY KEY CLUSTERED (MaThanhToan)
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
    ThoiGian          datetime2(3)  NOT NULL CONSTRAINT DF_LichSuSanPham_ThoiGian DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_LichSuTrangThaiSanPham PRIMARY KEY CLUSTERED (MaLichSuTrangThaiSanPham)
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
    CONSTRAINT PK_LuotKiemDuyet PRIMARY KEY CLUSTERED (MaLuotKiemDuyet)
);
GO

CREATE TABLE PhanTichAI (
    MaPhanTichAI       int            IDENTITY(1,1) NOT NULL,
    MaLuotKiemDuyet    int            NOT NULL,
    TrangThai          varchar(12)    NOT NULL CONSTRAINT DF_PhanTichAI_TrangThai DEFAULT ('QUEUED'),
    SoLanThu           int            NOT NULL CONSTRAINT DF_PhanTichAI_SoLanThu  DEFAULT (0),
    MucRuiRo           varchar(10)    NULL,
    DauHieu            nvarchar(max)  NULL,
    DeXuat             varchar(15)    NULL,
    GiaiThich          nvarchar(2000) NULL,
    MaBamDauVao        char(64)       NULL,
    PhienBanMoHinh     varchar(50)    NULL,
    ThoiGianXuLyMs     int            NULL,
    LoiXuLy            nvarchar(1000) NULL,
    QuanTriVienLamTheo bit            NULL,
    NgayTao            datetime2(3)   NOT NULL CONSTRAINT DF_PhanTichAI_NgayTao DEFAULT (SYSUTCDATETIME()),
    ThoiGianHoanThanh  datetime2(3)   NULL,
    CONSTRAINT PK_PhanTichAI PRIMARY KEY CLUSTERED (MaPhanTichAI)
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
    CONSTRAINT PK_TuKhoaCam PRIMARY KEY CLUSTERED (MaTuKhoaCam)
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
    NgayTao           datetime2(3)   NOT NULL CONSTRAINT DF_ThongBao_NgayTao DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_ThongBao PRIMARY KEY CLUSTERED (MaThongBao)
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
    ThoiGian          datetime2(3)  NOT NULL CONSTRAINT DF_NhatKy_ThoiGian DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_NhatKyHeThong PRIMARY KEY CLUSTERED (MaNhatKyHeThong)
);
GO

CREATE SEQUENCE sq_MaDonHang AS int START WITH 1 INCREMENT BY 1 CACHE 50;
GO

PRINT N'Đã tạo xong các bảng.';
GO