SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ARITHABORT ON;
GO

USE SecondHandMarketplace;
GO

-- Stored procedures sản phẩm.

-- TRUNG TÂM chuyển trạng thái sản phẩm (ProductStateMachine ở mức DB)
CREATE OR ALTER PROCEDURE sp_ChuyenTrangThaiSanPham
    @MaSanPham         int,
    @TrangThaiMoi      varchar(20),
    @LoaiNguoiThucHien varchar(10),
    @NguoiThucHienId   int           = NULL,
    @LyDo              nvarchar(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @TrangThaiCu varchar(20), @TuTaoTran bit = 0;

    IF @LoaiNguoiThucHien = 'SYSTEM' SET @NguoiThucHienId = NULL;

    BEGIN TRY
        IF @@TRANCOUNT = 0
        BEGIN
            BEGIN TRANSACTION;
            SET @TuTaoTran = 1;
        END

        SELECT @TrangThaiCu = TrangThai
        FROM SanPham WITH (UPDLOCK, ROWLOCK)
        WHERE MaSanPham = @MaSanPham AND DaXoa = 0;

        IF @TrangThaiCu IS NULL
            THROW 50010, N'Sản phẩm không tồn tại.', 1;

        IF dbo.fn_ChuyenTrangThaiSanPhamHopLe(@TrangThaiCu, @TrangThaiMoi) = 0
           AND NOT (@LoaiNguoiThucHien = 'ADMIN' AND @TrangThaiMoi = 'REMOVED')
            THROW 50011, N'Không thể chuyển trạng thái sản phẩm này.', 1;

        IF @TrangThaiMoi = 'REMOVED' AND LEN(LTRIM(RTRIM(ISNULL(@LyDo, N'')))) = 0
            THROW 50012, N'Gỡ sản phẩm bắt buộc phải có lý do.', 1;

        UPDATE SanPham
        SET TrangThai   = @TrangThaiMoi,
            LyDoGo      = CASE WHEN @TrangThaiMoi = 'REMOVED' THEN @LyDo ELSE LyDoGo END,
            NgayCapNhat = SYSUTCDATETIME()
        WHERE MaSanPham = @MaSanPham AND TrangThai = @TrangThaiCu;

        IF @@ROWCOUNT = 0
            THROW 50013, N'Trạng thái sản phẩm vừa bị thay đổi, vui lòng tải lại.', 1;

        IF @TrangThaiMoi = 'REMOVED'
            UPDATE ChiTietDonHang SET DangGiuCho = 0 WHERE MaSanPham = @MaSanPham AND DangGiuCho = 1;

        INSERT LichSuTrangThaiSanPham (MaSanPham, TrangThaiCu, TrangThaiMoi, LoaiNguoiThucHien, NguoiThucHienId, LyDo)
        VALUES (@MaSanPham, @TrangThaiCu, @TrangThaiMoi, @LoaiNguoiThucHien, @NguoiThucHienId, @LyDo);

        IF @TuTaoTran = 1 COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @TuTaoTran = 1 AND @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END
GO

-- Người bán GỬI DUYỆT
CREATE OR ALTER PROCEDURE sp_GuiDuyetSanPham
    @MaSanPham     int,
    @MaNguoiDung   int,
    @TaoPhanTichAI bit = 1
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @ChuShopId int, @TrangThaiShop varchar(10), @TrangThaiNguoiDung varchar(10),
            @TrangThai varchar(20), @LuotId int, @LaPhanTichLai bit = 0;

    SELECT @ChuShopId = ch.MaNguoiDung, @TrangThaiShop = ch.TrangThai, @TrangThai = sp.TrangThai
    FROM SanPham sp
    JOIN CuaHang ch ON ch.MaCuaHang = sp.MaCuaHang
    WHERE sp.MaSanPham = @MaSanPham AND sp.DaXoa = 0;

    IF @ChuShopId IS NULL OR @ChuShopId <> @MaNguoiDung
        THROW 50010, N'Sản phẩm không tồn tại.', 1;

    SELECT @TrangThaiNguoiDung = TrangThai FROM NguoiDung WHERE MaNguoiDung = @MaNguoiDung;

    IF @TrangThaiNguoiDung <> 'ACTIVE' OR @TrangThaiShop <> 'ACTIVE'
        THROW 50014, N'Tài khoản hoặc cửa hàng đang bị khóa, không thể gửi duyệt.', 1;

    -- Cho phép gửi lại khi sản phẩm đang PENDING_REVIEW nhưng lần phân tích trước bị FAILED
    -- (lỗi hệ thống, không phải vi phạm). Dùng lại đúng lượt đang mở để tránh UX_LuotKiemDuyet_DangCho.
    IF @TrangThai = 'PENDING_REVIEW'
       AND EXISTS (SELECT 1
                   FROM LuotKiemDuyet l
                   JOIN PhanTichAI p ON p.MaLuotKiemDuyet = l.MaLuotKiemDuyet
                   WHERE l.MaSanPham = @MaSanPham AND l.QuyetDinh IS NULL
                     AND p.TrangThai = 'FAILED')
        SET @LaPhanTichLai = 1;

    IF @TrangThai NOT IN ('DRAFT','REJECTED','ACTIVE','HIDDEN','REMOVED') AND @LaPhanTichLai = 0
        THROW 50015, N'Sản phẩm ở trạng thái hiện tại không thể gửi duyệt.', 1;

    DECLARE @Loi TABLE (MaLoi nvarchar(50), Truong nvarchar(50), ThongBao nvarchar(500));
    INSERT @Loi (MaLoi, Truong, ThongBao)
    SELECT MaLoi, Truong, ThongBao FROM dbo.fn_KiemTraRuleSanPham(@MaSanPham);

    IF EXISTS (SELECT 1 FROM @Loi)
    BEGIN
        SELECT CAST(0 AS bit) AS ThanhCong, MaLoi, Truong, ThongBao, CAST(NULL AS int) AS LuotKiemDuyetId
        FROM @Loi;
        RETURN 1;
    END

    BEGIN TRANSACTION;

    IF @LaPhanTichLai = 0
    BEGIN
        EXEC sp_ChuyenTrangThaiSanPham @MaSanPham = @MaSanPham, @TrangThaiMoi = 'PENDING_REVIEW',
                                       @LoaiNguoiThucHien = 'SELLER', @NguoiThucHienId = @MaNguoiDung,
                                       @LyDo = N'Gửi kiểm duyệt';

        INSERT LuotKiemDuyet (MaSanPham, KetQuaRule)
        VALUES (@MaSanPham, N'{"passed":true,"errors":[]}');
        SET @LuotId = SCOPE_IDENTITY();

        IF @TaoPhanTichAI = 1
            INSERT PhanTichAI (MaLuotKiemDuyet, TrangThai) VALUES (@LuotId, 'QUEUED');
    END
    ELSE
    BEGIN
        -- Phân tích lại: giữ nguyên lượt mở, chỉ làm mới kết quả rule.
        SELECT @LuotId = l.MaLuotKiemDuyet
        FROM LuotKiemDuyet l WITH (UPDLOCK, ROWLOCK)
        WHERE l.MaSanPham = @MaSanPham AND l.QuyetDinh IS NULL;

        UPDATE LuotKiemDuyet
        SET KetQuaRule = N'{"passed":true,"errors":[]}', ThoiGianGui = SYSUTCDATETIME()
        WHERE MaLuotKiemDuyet = @LuotId;

        IF @TaoPhanTichAI = 1
        BEGIN
            IF NOT EXISTS (SELECT 1 FROM PhanTichAI WHERE MaLuotKiemDuyet = @LuotId)
                INSERT PhanTichAI (MaLuotKiemDuyet, TrangThai) VALUES (@LuotId, 'QUEUED');
            ELSE
                -- Lần trước lỗi hệ thống: đưa dòng phân tích về QUEUED để chạy lại sạch.
                UPDATE PhanTichAI
                SET TrangThai = 'QUEUED', LoiXuLy = NULL, MucDoTuKhoa = NULL, MucRuiRo = NULL,
                    DeXuat = NULL, DauHieu = NULL, GiaiThich = NULL, ThoiGianHoanThanh = NULL,
                    ThoiGianXuLyMs = NULL
                WHERE MaLuotKiemDuyet = @LuotId AND TrangThai = 'FAILED';
        END
    END

    COMMIT TRANSACTION;

    SELECT CAST(1 AS bit) AS ThanhCong, CAST(NULL AS nvarchar(50)) AS MaLoi, CAST(NULL AS nvarchar(50)) AS Truong,
           CAST(NULL AS nvarchar(500)) AS ThongBao, @LuotId AS LuotKiemDuyetId;
    RETURN 0;
END
GO

-- Xóa mềm sản phẩm
CREATE OR ALTER PROCEDURE sp_XoaMemSanPham
    @MaSanPham   int,
    @MaNguoiDung int
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @TrangThai varchar(20);

    BEGIN TRY
        BEGIN TRANSACTION;

        SELECT @TrangThai = sp.TrangThai
        FROM SanPham sp WITH (UPDLOCK, ROWLOCK)
        JOIN CuaHang ch ON ch.MaCuaHang = sp.MaCuaHang
        WHERE sp.MaSanPham = @MaSanPham AND sp.DaXoa = 0 AND ch.MaNguoiDung = @MaNguoiDung;

        IF @TrangThai IS NULL
            THROW 50010, N'Sản phẩm không tồn tại.', 1;
        IF @TrangThai IN ('SOLD','RESERVED')
           OR EXISTS (SELECT 1 FROM ChiTietDonHang WHERE MaSanPham = @MaSanPham)
            THROW 50016, N'Sản phẩm đã có đơn hàng hoặc đã bán, không thể xóa.', 1;

        UPDATE SanPham
        SET DaXoa = 1, ThoiGianXoa = SYSUTCDATETIME(), NgayCapNhat = SYSUTCDATETIME()
        WHERE MaSanPham = @MaSanPham;

        DELETE FROM GioHang WHERE MaSanPham = @MaSanPham;

        EXEC sp_GhiNhatKy @MaNguoiDung = @MaNguoiDung, @HanhDong = 'DELETE_PRODUCT',
                          @LoaiDoiTuong = 'SanPham', @DoiTuongId = @MaSanPham;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END
GO
