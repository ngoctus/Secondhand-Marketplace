/* GioHang - Bảng Giỏ Hàng:
   Lưu sản phẩm người mua thêm và giỏ.
   Giỏ hàng khác Đơn hàng: chưa phát sinh giao dịch mua.
   Mỗi người mua chỉ có một dòng cho một sản phẩm.
   SoLuong = 1 vì mỗi sản phẩm đồ cũ chỉ có 1 đơn vị. */
CREATE TABLE GioHang
(
    MaGioHang      BIGINT IDENTITY(1,1) PRIMARY KEY,
    MaNguoiMua         BIGINT NOT NULL,
    MaSanPham       BIGINT NOT NULL,
    SoLuong        INT NOT NULL DEFAULT 1,
    NgayTao       DATETIME2 NOT NULL DEFAULT SYSDATETIME()
);
GO


/* DonHang - BẢNG ĐƠN HÀNG
   lưu thông tin của đơn hàng: người mua, người bán, địa chỉ giao hàng, phí ship, tổng tiền và trạng thái. */
CREATE TABLE DonHang
(
    MaDonHang             BIGINT IDENTITY(1,1) PRIMARY KEY,
    MaDon           VARCHAR(30) NOT NULL,
    MaNguoiMua             BIGINT NOT NULL,
    MaNguoiBan            BIGINT NOT NULL,
    TenNguoiNhan       NVARCHAR(150) NOT NULL,
    SDTNguoiNhan      VARCHAR(20) NOT NULL,
    DiaChiGiaoHang     NVARCHAR(1000) NOT NULL,
    NgayGiaoDuKien  DATETIME2 NULL,
    TienHang            DECIMAL(18,2) NOT NULL,
    PhiVanChuyen         DECIMAL(18,2) NOT NULL DEFAULT 0,
    TongTien         DECIMAL(18,2) NOT NULL,
    PhuongThucThanhToan       VARCHAR(30) NOT NULL DEFAULT 'COD',
    TrangThaiThanhToan       VARCHAR(20) NOT NULL DEFAULT 'UNPAID',
    TrangThaiDonHang         VARCHAR(20) NOT NULL DEFAULT 'PENDING',
    LyDoHuy        NVARCHAR(1000) NULL,
    NgayHuy         DATETIME2 NULL,
    NgayTao           DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    NgayCapNhat           DATETIME2 NULL
);
GO

/* ChiTietDonHang - CHI TIẾT ĐƠN HÀNG:
   Lưu sản phẩm, tên sản phẩm và giá tại thời điểm mua.
   ThanhTien đưọc tính từ DonGia * SoLuong. */
CREATE TABLE ChiTietDonHang
(
    MaChiTietDonHang     BIGINT IDENTITY(1,1) PRIMARY KEY,
    MaDonHang         BIGINT NOT NULL,
    MaSanPham       BIGINT NOT NULL,
    TenSanPham     NVARCHAR(200) NOT NULL,
    DonGia       DECIMAL(18,2) NOT NULL,
    SoLuong        INT NOT NULL DEFAULT 1,
    ThanhTien AS (DonGia * SoLuong) PERSISTED
);
GO

/* LichSuTrangThaiDonHang:
   Lưu lịch sử chuyển trạng thái của đơn hàng. */

CREATE TABLE LichSuTrangThaiDonHang
(
    MaLichSu       BIGINT IDENTITY(1,1) PRIMARY KEY,
    MaDonHang         BIGINT NOT NULL,
    TrangThaiCu       VARCHAR(20) NULL,
    TrangThaiMoi       VARCHAR(20) NOT NULL,
    NguoiThayDoi       BIGINT NULL,
    GhiChu            NVARCHAR(1000) NULL,
    NgayThayDoi       DATETIME2 NOT NULL DEFAULT SYSDATETIME()
);
GO

/* ThanhToan:
   Lưu các giao dịch thanh toán, mã giao dịch và kết quả. */

CREATE TABLE ThanhToan
(
    MaThanhToan       BIGINT IDENTITY(1,1) PRIMARY KEY,
    MaDonHang         BIGINT NOT NULL,
    MaGiaoDich VARCHAR(100) NULL,
    PhuongThucThanhToan   VARCHAR(30) NOT NULL,
    SoTien          DECIMAL(18,2) NOT NULL,
    TrangThaiThanhToan   VARCHAR(20) NOT NULL DEFAULT 'PENDING',
    NgayThanhToan          DATETIME2 NULL,
    NgayTao       DATETIME2 NOT NULL DEFAULT SYSDATETIME()
);
GO
