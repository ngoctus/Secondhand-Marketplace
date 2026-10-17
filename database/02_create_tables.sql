-- =====================================================================
-- 02a: Tạo bảng nhóm Tài khoản & Cửa hàng (NguoiDung, CuaHang)
-- Chỉ tạo bảng + khóa chính + giá trị mặc định.
-- Khóa ngoại / UNIQUE / CHECK nằm ở file 03a_tai_khoan_shop_constraints.sql
--
-- Quy ước đọc file:
--   -- [Đề xuất]      = chi tiết tài liệu chưa nêu, do mình thêm, nhóm xem lại
-- =====================================================================
USE SecondHandDB;   -- [Đề xuất] : thêm sau
GO

-- ---------------------------------------------------------------------
-- NguoiDung (dòng 1, 2 của bảng phân tích): tài khoản, vai trò USER / ADMIN
-- Cột riêng của Identity (NormalizedEmail, SecurityStamp...) sẽ thêm sau ở E4
-- ---------------------------------------------------------------------
CREATE TABLE NguoiDung (
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
);
GO