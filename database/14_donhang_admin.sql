SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ARITHABORT ON;
GO

USE SecondHandMarketplace;
GO

-- Admin can thiệp đơn hàng (sp_CapNhatTrangThaiDon chỉ cho Admin HỦY, không cho xác nhận/giao/hoàn tất).
-- Bắt buộc lý do >= 5 ký tự, kiểm tra fn_ChuyenTrangThaiDonHangHopLe, ghi lịch sử (ADMIN), thông báo cả người mua và người bán, ghi nhật ký.
CREATE OR ALTER PROCEDURE dbo.sp_AdminCapNhatTrangThaiDon
    @MaDonHang     int,
    @TrangThaiMoi  varchar(15),
    @QuanTriVienId int,
    @LyDo          nvarchar(500)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @TrangThaiCu varchar(15), @MaNguoiMua int, @ChuShopId int, @MaDon varchar(30),
            @Now datetime2(3) = SYSUTCDATETIME(), @TrangThaiSanPhamMoi varchar(20), @Nhan nvarchar(30);
    DECLARE @SanPhamDoi TABLE (MaSanPham int PRIMARY KEY, TrangThaiCu varchar(20));

    BEGIN TRY
        BEGIN TRANSACTION;

        IF NOT EXISTS (SELECT 1 FROM NguoiDung WHERE MaNguoiDung = @QuanTriVienId AND VaiTro = 'ADMIN' AND TrangThai = 'ACTIVE')
            THROW 51301, N'Chỉ quản trị viên mới được can thiệp đơn hàng.', 1;
        IF LEN(LTRIM(RTRIM(ISNULL(@LyDo, N'')))) < 5
            THROW 51302, N'Admin can thiệp đơn hàng bắt buộc nhập lý do (tối thiểu 5 ký tự).', 1;

        SELECT @TrangThaiCu = dh.TrangThai, @MaNguoiMua = dh.MaNguoiDung, @ChuShopId = ch.MaNguoiDung, @MaDon = dh.MaDon
        FROM DonHang dh WITH (UPDLOCK, HOLDLOCK)
        JOIN CuaHang ch ON ch.MaCuaHang = dh.MaCuaHang
        WHERE dh.MaDonHang = @MaDonHang;

        IF @TrangThaiCu IS NULL
            THROW 51201, N'Đơn hàng không tồn tại.', 1;

        -- Nếu trạng thái không thay đổi (ví dụ đơn đã COMPLETED và admin duyệt PAY_SELLER), coi như thành công (Fix #6)
        IF @TrangThaiCu = @TrangThaiMoi
        BEGIN
            COMMIT TRANSACTION;
            SELECT @MaDonHang AS MaDonHang, @TrangThaiCu AS TrangThaiCu, @TrangThaiMoi AS TrangThaiMoi;
            RETURN;
        END

        IF dbo.fn_ChuyenTrangThaiDonHangHopLe(@TrangThaiCu, @TrangThaiMoi) = 0
            THROW 51203, N'Không cho phép chuyển trạng thái này.', 1;

        SET @LyDo = LTRIM(RTRIM(@LyDo));
        SET @Nhan = CASE @TrangThaiMoi
                        WHEN 'CONFIRMED' THEN N'Đã xác nhận' WHEN 'SHIPPING'  THEN N'Đang giao'
                        WHEN 'DELIVERED' THEN N'Đã giao'     WHEN 'COMPLETED' THEN N'Hoàn tất'
                        WHEN 'CANCELLED' THEN N'Đã hủy'      ELSE @TrangThaiMoi END;

        UPDATE DonHang
        SET TrangThai          = @TrangThaiMoi,
            NgayCapNhat        = @Now,
            NguoiHuy           = CASE WHEN @TrangThaiMoi = 'CANCELLED' THEN 'ADMIN' ELSE NguoiHuy END,
            LyDoHuy            = CASE WHEN @TrangThaiMoi = 'CANCELLED' THEN @LyDo ELSE LyDoHuy END,
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

        IF @TrangThaiMoi IN ('CANCELLED', 'COMPLETED')
        BEGIN
            -- Fix #1: Bắt buộc giải phóng DangGiuCho để không khóa sản phẩm vĩnh viễn (lỗi UX 2601)
            UPDATE ChiTietDonHang
            SET DangGiuCho = 0
            WHERE MaDonHang = @MaDonHang;

            SET @TrangThaiSanPhamMoi = CASE WHEN @TrangThaiMoi = 'COMPLETED' THEN 'SOLD' ELSE 'ACTIVE' END;

            -- Fix #17: Ghi nhận chính xác TrangThaiCu của sản phẩm trước khi cập nhật
            UPDATE sp
            SET TrangThai   = @TrangThaiSanPhamMoi,
                NgayCapNhat = @Now
            OUTPUT inserted.MaSanPham, deleted.TrangThai INTO @SanPhamDoi (MaSanPham, TrangThaiCu)
            FROM SanPham sp
            JOIN ChiTietDonHang ct ON ct.MaSanPham = sp.MaSanPham
            WHERE ct.MaDonHang = @MaDonHang
              AND (
                  (@TrangThaiMoi = 'CANCELLED' AND sp.TrangThai IN ('RESERVED', 'SOLD'))
                  OR (@TrangThaiMoi = 'COMPLETED' AND sp.TrangThai = 'RESERVED')
              );

            INSERT LichSuTrangThaiSanPham (MaSanPham, TrangThaiCu, TrangThaiMoi, LoaiNguoiThucHien, NguoiThucHienId, LyDo)
            SELECT x.MaSanPham, x.TrangThaiCu, @TrangThaiSanPhamMoi, 'ADMIN', @QuanTriVienId,
                   CONCAT(N'Đơn ', @MaDon, CASE WHEN @TrangThaiMoi = 'COMPLETED' THEN N' hoàn tất' ELSE N' bị hủy/hoàn tiền' END, N': ', @LyDo)
            FROM @SanPhamDoi x;
        END

        -- Fix #8: Đồng bộ bảng ThanhToan khi Admin hoàn tiền hoặc hoàn tất đơn
        IF @TrangThaiMoi = 'CANCELLED'
        BEGIN
            UPDATE ThanhToan
            SET TrangThaiThanhToan = 'REFUNDED'
            WHERE MaDonHang = @MaDonHang;
        END
        ELSE IF @TrangThaiMoi = 'COMPLETED'
        BEGIN
            UPDATE ThanhToan
            SET TrangThaiThanhToan = 'SUCCESS',
                ThoiGianThanhToan = ISNULL(ThoiGianThanhToan, @Now)
            WHERE MaDonHang = @MaDonHang;
        END

        INSERT ThongBao (MaNguoiDung, Loai, TieuDe, NoiDung, MaThamChieu)
        SELECT r.Id, 'ORDER', N'Quản trị viên cập nhật đơn hàng',
               CONCAT(N'Đơn ', @MaDon, N' được quản trị viên chuyển sang "', @Nhan, N'". Lý do: ', @LyDo), @MaDonHang
        FROM (VALUES (@MaNguoiMua), (@ChuShopId)) r (Id);

        EXEC sp_GhiNhatKy @MaNguoiDung = @QuanTriVienId, @HanhDong = 'ADMIN_UPDATE_ORDER',
                          @LoaiDoiTuong = 'DonHang', @DoiTuongId = @MaDonHang, @LyDo = @LyDo,
                          @ChiTiet = @TrangThaiMoi;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;

    SELECT @MaDonHang AS MaDonHang, @TrangThaiCu AS TrangThaiCu, @TrangThaiMoi AS TrangThaiMoi;
END;
GO

PRINT N'Đã tạo thủ tục Admin cập nhật đơn.';
GO
