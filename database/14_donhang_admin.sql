SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ARITHABORT ON;
GO

USE SecondHandMarketplace;
GO

-- =====================================================================
-- E2: Admin can thiệp cập nhật trạng thái đơn hàng (bắt buộc có lý do)
-- =====================================================================
CREATE OR ALTER PROCEDURE dbo.sp_AdminCapNhatTrangThaiDon
    @MaDonHang       int,
    @TrangThaiMoi    varchar(15),
    @QuanTriVienId   int,
    @LyDo            nvarchar(500)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @TrangThaiCu varchar(15), @MaNguoiMua int, @ChuShopId int, @MaDon varchar(30),
            @VaiTro varchar(10), @TrangThaiTaiKhoan varchar(10),
            @Now datetime2(3) = SYSUTCDATETIME(), @TrangThaiSanPhamMoi varchar(20);

    DECLARE @SanPhamDoi TABLE (MaSanPham int PRIMARY KEY);

    BEGIN TRY
        BEGIN TRANSACTION;

        SELECT @TrangThaiCu = dh.TrangThai, @MaNguoiMua = dh.MaNguoiDung,
               @ChuShopId = ch.MaNguoiDung, @MaDon = dh.MaDon
        FROM DonHang dh WITH (UPDLOCK, HOLDLOCK)
        JOIN CuaHang ch ON ch.MaCuaHang = dh.MaCuaHang
        WHERE dh.MaDonHang = @MaDonHang;

        IF @TrangThaiCu IS NULL
            THROW 51201, N'Đơn hàng không tồn tại.', 1;

        SELECT @VaiTro = VaiTro, @TrangThaiTaiKhoan = TrangThai
        FROM NguoiDung WHERE MaNguoiDung = @QuanTriVienId;

        IF @VaiTro IS NULL OR @VaiTro <> 'ADMIN' OR @TrangThaiTaiKhoan <> 'ACTIVE'
            THROW 51202, N'Tài khoản quản trị không hợp lệ hoặc đã bị khóa.', 1;

        IF LEN(LTRIM(RTRIM(ISNULL(@LyDo, N'')))) = 0
            THROW 51207, N'Admin can thiệp trạng thái đơn bắt buộc phải nhập lý do.', 1;

        IF dbo.fn_ChuyenTrangThaiDonHangHopLe(@TrangThaiCu, @TrangThaiMoi) = 0
            THROW 51203, N'Không cho phép chuyển trạng thái này.', 1;

        UPDATE DonHang
        SET TrangThai          = @TrangThaiMoi,
            NgayCapNhat        = @Now,
            NguoiHuy           = CASE WHEN @TrangThaiMoi = 'CANCELLED' THEN 'ADMIN' ELSE NguoiHuy END,
            LyDoHuy            = CASE WHEN @TrangThaiMoi = 'CANCELLED' THEN LTRIM(RTRIM(@LyDo)) ELSE LyDoHuy END,
            ThoiGianHuy        = CASE WHEN @TrangThaiMoi = 'CANCELLED' THEN @Now ELSE ThoiGianHuy END,
            ThoiGianXacNhan    = CASE WHEN @TrangThaiMoi = 'CONFIRMED' THEN @Now ELSE ThoiGianXacNhan END,
            ThoiGianBatDauGiao = CASE WHEN @TrangThaiMoi = 'SHIPPING'  THEN @Now ELSE ThoiGianBatDauGiao END,
            ThoiGianDaGiao     = CASE WHEN @TrangThaiMoi = 'DELIVERED' THEN @Now ELSE ThoiGianDaGiao END,
            ThoiGianHoanTat    = CASE WHEN @TrangThaiMoi = 'COMPLETED' THEN @Now ELSE ThoiGianHoanTat END,
            TrangThaiThanhToan = CASE WHEN @TrangThaiMoi = 'CANCELLED' THEN 'REFUNDED'
                                      WHEN @TrangThaiMoi = 'COMPLETED' THEN 'PAID'
                                      ELSE TrangThaiThanhToan END
        WHERE MaDonHang = @MaDonHang;

        INSERT LichSuTrangThaiDonHang (MaDonHang, TrangThaiCu, TrangThaiMoi, LoaiNguoiThucHien, NguoiThucHienId, LyDo)
        VALUES (@MaDonHang, @TrangThaiCu, @TrangThaiMoi, 'ADMIN', @QuanTriVienId, @LyDo);

        IF @TrangThaiMoi IN ('CANCELLED','COMPLETED')
        BEGIN
            SET @TrangThaiSanPhamMoi = CASE WHEN @TrangThaiMoi = 'COMPLETED' THEN 'SOLD' ELSE 'ACTIVE' END;

            UPDATE ChiTietDonHang SET DangGiuCho = 0 WHERE MaDonHang = @MaDonHang;

            UPDATE sp
            SET TrangThai = @TrangThaiSanPhamMoi, NgayCapNhat = @Now
            OUTPUT inserted.MaSanPham INTO @SanPhamDoi (MaSanPham)
            FROM SanPham sp
            JOIN ChiTietDonHang ct ON ct.MaSanPham = sp.MaSanPham
            WHERE ct.MaDonHang = @MaDonHang
              AND (
                  (@TrangThaiMoi = 'CANCELLED' AND sp.TrangThai IN ('RESERVED', 'SOLD'))
                  OR (@TrangThaiMoi = 'COMPLETED' AND sp.TrangThai = 'RESERVED')
              );

            INSERT LichSuTrangThaiSanPham (MaSanPham, TrangThaiCu, TrangThaiMoi, LoaiNguoiThucHien, NguoiThucHienId, LyDo)
            SELECT x.MaSanPham, CASE WHEN @TrangThaiMoi = 'COMPLETED' THEN 'RESERVED' ELSE 'SOLD' END, @TrangThaiSanPhamMoi, 'ADMIN', @QuanTriVienId,
                   CONCAT(N'Admin cập nhật đơn ', @MaDon, CASE WHEN @TrangThaiMoi = 'COMPLETED' THEN N' hoàn tất' ELSE N' bị hủy/hoàn tiền' END, N': ', @LyDo)
            FROM @SanPhamDoi x;
        END

        INSERT ThongBao (MaNguoiDung, Loai, TieuDe, NoiDung, MaThamChieu)
        SELECT r.Id, 'ORDER', N'Quản trị viên cập nhật đơn hàng',
               CONCAT(N'Đơn ', @MaDon, N' được Quản trị viên chuyển sang ', @TrangThaiMoi, N'. Lý do: ', @LyDo), @MaDonHang
        FROM (VALUES (@MaNguoiMua), (@ChuShopId)) r (Id);

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO
