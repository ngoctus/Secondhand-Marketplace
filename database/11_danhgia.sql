-- =============================================================================
-- 11_danhgia.sql  (Task C1 - Người C)
-- Đánh giá người bán: bảng DanhGia, stored procedure sp_TaoDanhGia,
-- view vw_DiemCuaHang (nguồn duy nhất cho điểm sao và uy tín shop).
--
-- Chạy sau 02_create_tables.sql (cần NguoiDung, CuaHang, DonHang).
-- Chạy lại nhiều lần được: bảng chỉ tạo khi chưa có (không mất dữ liệu đánh giá),
-- view và stored procedure dùng CREATE OR ALTER.
--
-- Mã lỗi của sp_TaoDanhGia (SqlException.Number):
--   50001 đơn hàng không tồn tại
--   50002 người đánh giá không phải người mua của đơn
--   50003 đơn chưa hoàn tất (TrangThai khác COMPLETED)
--   50004 đơn đã được đánh giá
--   50005 số sao ngoài khoảng 1 đến 5
-- =============================================================================
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ARITHABORT ON;
GO

USE SecondHandMarketplace;
GO

-- -----------------------------------------------------------------------------
-- 1. Bảng DanhGia
-- -----------------------------------------------------------------------------
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
        NgayTao       datetime2(3)   NOT NULL CONSTRAINT DF_DanhGia_NgayTao   DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_DanhGia           PRIMARY KEY CLUSTERED (MaDanhGia),
        CONSTRAINT UQ_DanhGia_DonHang   UNIQUE (MaDonHang),
        CONSTRAINT FK_DanhGia_DonHang   FOREIGN KEY (MaDonHang)  REFERENCES dbo.DonHang (MaDonHang),
        CONSTRAINT FK_DanhGia_NguoiMua  FOREIGN KEY (MaNguoiMua) REFERENCES dbo.NguoiDung (MaNguoiDung),
        CONSTRAINT FK_DanhGia_CuaHang   FOREIGN KEY (MaCuaHang)  REFERENCES dbo.CuaHang (MaCuaHang),
        CONSTRAINT CK_DanhGia_SoSao     CHECK (SoSao BETWEEN 1 AND 5),
        CONSTRAINT CK_DanhGia_TrangThai CHECK (TrangThai IN ('VISIBLE', 'HIDDEN'))
    );
END;
GO

-- Index phục vụ vw_DiemCuaHang, danh sách đánh giá theo shop và "đánh giá tôi đã viết".
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DanhGia_CuaHang' AND object_id = OBJECT_ID(N'dbo.DanhGia'))
    CREATE NONCLUSTERED INDEX IX_DanhGia_CuaHang
        ON dbo.DanhGia (MaCuaHang, TrangThai) INCLUDE (SoSao, NgayTao);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DanhGia_NguoiMua' AND object_id = OBJECT_ID(N'dbo.DanhGia'))
    CREATE NONCLUSTERED INDEX IX_DanhGia_NguoiMua
        ON dbo.DanhGia (MaNguoiMua);
GO

-- -----------------------------------------------------------------------------
-- 2. View vw_DiemCuaHang
--    Mọi nơi hiển thị sao/uy tín shop (thẻ sản phẩm, chi tiết, trang shop,
--    rà soát shop) phải đọc từ view này, không tự tính lại.
--    - DiemTrungBinh: trung bình các đánh giá VISIBLE, làm tròn 2 chữ số;
--                     shop chưa có đánh giá VISIBLE trả về 0 (phân biệt bằng SoDanhGia = 0).
--    - SoDanhGia:     số đánh giá VISIBLE.
--    - SoDonThanhCong: số đơn COMPLETED của shop.
--    Gom riêng từng bảng rồi LEFT JOIN từ CuaHang để không nhân đôi dòng
--    và vẫn hiện shop mới.
-- -----------------------------------------------------------------------------
CREATE OR ALTER VIEW dbo.vw_DiemCuaHang
AS
SELECT
    ch.MaCuaHang,
    CAST(ISNULL(dg.TongSao * 1.0 / NULLIF(dg.SoDanhGia, 0), 0) AS decimal(3,2)) AS DiemTrungBinh,
    ISNULL(dg.SoDanhGia, 0)                                                     AS SoDanhGia,
    ISNULL(dh.SoDonThanhCong, 0)                                                AS SoDonThanhCong
FROM dbo.CuaHang AS ch
LEFT JOIN (
    SELECT MaCuaHang,
           COUNT(*)                  AS SoDanhGia,
           SUM(CAST(SoSao AS int))   AS TongSao
    FROM dbo.DanhGia
    WHERE TrangThai = 'VISIBLE'
    GROUP BY MaCuaHang
) AS dg ON dg.MaCuaHang = ch.MaCuaHang
LEFT JOIN (
    SELECT MaCuaHang,
           COUNT(*) AS SoDonThanhCong
    FROM dbo.DonHang
    WHERE TrangThai = 'COMPLETED'
    GROUP BY MaCuaHang
) AS dh ON dh.MaCuaHang = ch.MaCuaHang;
GO

-- -----------------------------------------------------------------------------
-- 3. Stored procedure sp_TaoDanhGia
--    Trả về một dòng một cột: MaDanhGia vừa tạo.
-- -----------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.sp_TaoDanhGia
    @MaDonHang  int,
    @MaNguoiMua int,
    @SoSao      tinyint,
    @NoiDung    nvarchar(1000) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @MaCuaHang  int;
    DECLARE @MaNguoiDon int;
    DECLARE @TrangThai  varchar(15);
    DECLARE @MaDanhGia  int;

    IF @SoSao IS NULL OR @SoSao NOT BETWEEN 1 AND 5
        THROW 50005, N'Số sao phải từ 1 đến 5.', 1;

    -- Chuẩn hóa nội dung: bỏ khoảng trắng đầu cuối, chuỗi rỗng thành NULL.
    SET @NoiDung = NULLIF(LTRIM(RTRIM(@NoiDung)), N'');

    SELECT @MaCuaHang  = MaCuaHang,
           @MaNguoiDon = MaNguoiDung,
           @TrangThai  = TrangThai
    FROM dbo.DonHang
    WHERE MaDonHang = @MaDonHang;

    IF @MaNguoiDon IS NULL
        THROW 50001, N'Đơn hàng không tồn tại.', 1;

    IF @MaNguoiDon <> @MaNguoiMua
        THROW 50002, N'Bạn không phải người mua của đơn hàng này nên không thể đánh giá.', 1;

    IF @TrangThai <> 'COMPLETED'
        THROW 50003, N'Chỉ được đánh giá khi đơn hàng đã hoàn tất.', 1;

    IF EXISTS (SELECT 1 FROM dbo.DanhGia WHERE MaDonHang = @MaDonHang)
        THROW 50004, N'Đơn hàng này đã được đánh giá.', 1;

    BEGIN TRY
        INSERT INTO dbo.DanhGia (MaDonHang, MaNguoiMua, MaCuaHang, SoSao, NoiDung)
        VALUES (@MaDonHang, @MaNguoiMua, @MaCuaHang, @SoSao, @NoiDung);

        SET @MaDanhGia = CAST(SCOPE_IDENTITY() AS int);
    END TRY
    BEGIN CATCH
        -- Hai yêu cầu đánh giá cùng lúc: UNIQUE (MaDonHang) chặn bản thứ hai.
        IF ERROR_NUMBER() IN (2601, 2627)
            THROW 50004, N'Đơn hàng này đã được đánh giá.', 1;
        THROW;
    END CATCH;

    SELECT @MaDanhGia AS MaDanhGia;
END;
GO

PRINT N'Đã tạo bảng DanhGia, view vw_DiemCuaHang và stored procedure sp_TaoDanhGia.';
GO