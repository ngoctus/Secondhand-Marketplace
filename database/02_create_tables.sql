SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ARITHABORT ON;
GO
USE SecondHandMarketplace;
GO

IF OBJECT_ID(N'NguoiDung', N'U') IS NOT NULL
BEGIN
    PRINT N'Cac bang da ton tai, bo qua 02_create_tables.sql.';
    SET NOEXEC ON;
END
GO

CREATE TABLE NguoiDung (
<<<<<<< HEAD
    Id              INT IDENTITY(1,1)  NOT NULL,
    HoTen           NVARCHAR(100)      NOT NULL,   -- [Đề xuất] độ dài 100
    Email           VARCHAR(256)       NOT NULL,   -- luôn chữ thường; [Đề xuất] độ dài 256
    SDT             VARCHAR(10)        NOT NULL,   -- đúng 10 chữ số; [Đề xuất] NOT NULL (UNIQUE chỉ cho 1 dòng NULL)
    MatKhauBam      VARCHAR(256)       NOT NULL,   -- mật khẩu đã băm; [Đề xuất] tên cột và độ dài
    VaiTro          VARCHAR(10)        NOT NULL CONSTRAINT DF_NguoiDung_VaiTro    DEFAULT 'USER',    -- [Đề xuất] mặc định USER
    TrangThai       VARCHAR(10)        NOT NULL CONSTRAINT DF_NguoiDung_TrangThai DEFAULT 'ACTIVE',  -- [Đề xuất] mặc định ACTIVE
    LyDoKhoa        NVARCHAR(500)      NULL,       -- lý do tối đa 500 (mục "điểm đã chốt" số 4)
    ThoiGianKhoa    DATETIME2(3)       NULL,       -- giờ UTC
    AnhDaiDien      VARCHAR(500)       NULL,       -- [Đề xuất] độ dài; lưu đường dẫn (tên file GUID)
    DiaChiMacDinh   NVARCHAR(500)       NULL,      -- [Đề xuất] tên cột, độ dài; gộp thành 1 cột chuỗi
    NgayTao         DATETIME2(3)       NOT NULL CONSTRAINT DF_NguoiDung_NgayTao     DEFAULT SYSUTCDATETIME(),  -- [Đề xuất] cột mốc quản trị theo quy ước tên NgayTao
    NgayCapNhat     DATETIME2(3)       NOT NULL CONSTRAINT DF_NguoiDung_NgayCapNhat DEFAULT SYSUTCDATETIME(),  -- [Đề xuất] như trên
    CONSTRAINT PK_NguoiDung PRIMARY KEY (Id)
);
GO

-- ---------------------------------------------------------------------
-- CuaHang (dòng 3 của bảng phân tích): 1 người dùng = 1 shop
-- Có bản ghi CuaHang = đã kích hoạt bán, không có vai trò Seller riêng
-- ---------------------------------------------------------------------
CREATE TABLE CuaHang (
    Id              INT IDENTITY(1,1)  NOT NULL,
    NguoiDungId     INT                NOT NULL,   -- chủ shop
    Ten             NVARCHAR(100)      NOT NULL,   -- tên shop tối đa 100; [Đề xuất] tên cột "Ten"
    SDTLienHe       VARCHAR(10)        NOT NULL,   -- đúng 10 chữ số; [Đề xuất] tên cột
    MoTa            NVARCHAR(1000)     NULL,       -- mô tả shop tối đa 1.000
    Logo            VARCHAR(500)       NULL,       -- [Đề xuất] độ dài; lưu đường dẫn (tên file GUID)
    TrangThai       VARCHAR(10)        NOT NULL CONSTRAINT DF_CuaHang_TrangThai DEFAULT 'ACTIVE',  -- [Đề xuất] mặc định ACTIVE
    LyDoKhoa        NVARCHAR(500)      NULL,       -- lý do tối đa 500
    ThoiGianKhoa    DATETIME2(3)       NULL,       -- giờ UTC
    NgayTao         DATETIME2(3)       NOT NULL CONSTRAINT DF_CuaHang_NgayTao     DEFAULT SYSUTCDATETIME(),  -- [Đề xuất] cột mốc quản trị
    NgayCapNhat     DATETIME2(3)       NOT NULL CONSTRAINT DF_CuaHang_NgayCapNhat DEFAULT SYSUTCDATETIME(),  -- [Đề xuất] như trên
    CONSTRAINT PK_CuaHang PRIMARY KEY (Id)
=======
    MaNguoiDung             int           IDENTITY(1,1) NOT NULL,
    -- Ngọc Tú   
);
GO

CREATE TABLE CuaHang (
    MaCuaHang                 int           IDENTITY(1,1) NOT NULL,
    --Ngọc Tú 
>>>>>>> 631be27a0ff0bf0d6424b62e5f5517338dfe9028
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
    MaGioHang            int          IDENTITY(1,1) NOT NULL,
    -- Ngọc Ngà 
);
GO

CREATE TABLE DonHang (
    MaDonHang                   int            IDENTITY(1,1) NOT NULL,
    -- Ngọc Ngà 
);
GO

CREATE TABLE ChiTietDonHang (
    MaChiTietDonHang           int           IDENTITY(1,1) NOT NULL,
    -- Ngọc Ngà
);
GO

CREATE TABLE LichSuTrangThaiDonHang (
    MaLichSuTrangThaiDonHang                 int           IDENTITY(1,1) NOT NULL,
    -- Ngọc Ngà
);
GO

CREATE TABLE ThanhToan(
    MaThanhToan                 int           IDENTITY(1,1) NOT NULL,
    -- Ngọc Ngà
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
    MaLuotKiemDuyet                 int           IDENTITY(1,1) NOT NULL,
    --Giàu 
);
GO

CREATE TABLE PhanTichAI (
    MaPhanTichAI      int            IDENTITY(1,1) NOT NULL,
    MaLuotKiemDuyet   int            NOT NULL,
    TrangThai         varchar(12)    NOT NULL CONSTRAINT DF_PhanTichAI_TrangThai DEFAULT ('QUEUED'),
    SoLanThu          int            NOT NULL CONSTRAINT DF_PhanTichAI_SoLanThu  DEFAULT (0),
    MucRuiRo          varchar(10)    NULL,
    DauHieu           nvarchar(max)  NULL,
    DeXuat            varchar(15)    NULL,
    GiaiThich         nvarchar(2000) NULL,
    MaBamDauVao       char(64)       NULL,
    PhienBanMoHinh    varchar(50)    NULL,
    ThoiGianXuLyMs    int            NULL,
    LoiXuLy           nvarchar(1000) NULL,
    QuanTriVienLamTheo bit           NULL,
    NgayTao           datetime2(3)   NOT NULL CONSTRAINT DF_PhanTichAI_NgayTao DEFAULT (SYSUTCDATETIME()),
    ThoiGianHoanThanh datetime2(3)   NULL,
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

-- Bộ đếm sinh mã đơn: DH + yyyyMMdd + '-' + 6 số
CREATE SEQUENCE sq_MaDonHang AS int START WITH 1 INCREMENT BY 1 CACHE 50;
GO

PRINT N'Da tao xong cac bang.';
SET NOEXEC OFF;
GO

