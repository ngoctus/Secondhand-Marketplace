SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ARITHABORT ON;
GO

USE SecondHandMarketplace;
GO

-- Stored procedures tài khoản/cửa hàng.

-- Ghi nhật ký
CREATE OR ALTER PROCEDURE sp_GhiNhatKy
    @MaNguoiDung   int           = NULL,
    @HanhDong      varchar(50),
    @LoaiDoiTuong  varchar(30),
    @DoiTuongId    int           = NULL,
    @KetQua        varchar(10)   = 'SUCCESS',
    @LyDo          nvarchar(500) = NULL,
    @ChiTiet       nvarchar(max) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT NhatKyHeThong (MaNguoiDung, HanhDong, LoaiDoiTuong, DoiTuongId, KetQua, LyDo, ChiTiet)
    VALUES (@MaNguoiDung, @HanhDong, @LoaiDoiTuong, @DoiTuongId, @KetQua, @LyDo, @ChiTiet);
END
GO

-- Tạo shop (có shop = đã kích hoạt bán, có hiệu lực ngay)
CREATE OR ALTER PROCEDURE sp_TaoCuaHang
    @MaNguoiDung       int,
    @TenShop           nvarchar(100),
    @SoDienThoaiLienHe varchar(15),
    @MoTa              nvarchar(1000) = NULL,
    @Logo              varchar(500)   = NULL,
    @CuaHangId         int OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        IF NOT EXISTS (SELECT 1 FROM NguoiDung WITH (UPDLOCK, HOLDLOCK)
                       WHERE MaNguoiDung = @MaNguoiDung AND VaiTro = 'USER' AND TrangThai = 'ACTIVE')
            THROW 50001, N'Tài khoản không hợp lệ hoặc không được phép tạo shop.', 1;
        IF EXISTS (SELECT 1 FROM CuaHang WHERE MaNguoiDung = @MaNguoiDung)
            THROW 50002, N'Tài khoản này đã có shop (mỗi tài khoản chỉ có 1 shop).', 1;

        INSERT CuaHang (MaNguoiDung, TenShop, SoDienThoaiLienHe, MoTa, Logo)
        VALUES (@MaNguoiDung, @TenShop, @SoDienThoaiLienHe, @MoTa, @Logo);
        SET @CuaHangId = SCOPE_IDENTITY();

        EXEC sp_GhiNhatKy @MaNguoiDung = @MaNguoiDung, @HanhDong = 'CREATE_SHOP',
                          @LoaiDoiTuong = 'CuaHang', @DoiTuongId = @CuaHangId;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END
GO
