SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;

    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_DonHang_TrangThaiTT' AND parent_object_id = OBJECT_ID(N'dbo.DonHang'))
        ALTER TABLE dbo.DonHang DROP CONSTRAINT CK_DonHang_TrangThaiTT;
    ALTER TABLE dbo.DonHang WITH CHECK ADD CONSTRAINT CK_DonHang_TrangThaiTT
        CHECK (TrangThaiThanhToan IN ('UNPAID','PENDING','PAID','REFUND_DUE','REFUNDED','FAILED'));
    ALTER TABLE dbo.DonHang CHECK CONSTRAINT CK_DonHang_TrangThaiTT;

    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_ThanhToan_TrangThai' AND parent_object_id = OBJECT_ID(N'dbo.ThanhToan'))
        ALTER TABLE dbo.ThanhToan DROP CONSTRAINT CK_ThanhToan_TrangThai;
    ALTER TABLE dbo.ThanhToan WITH CHECK ADD CONSTRAINT CK_ThanhToan_TrangThai
        CHECK (TrangThaiThanhToan IN ('PENDING','SUCCESS','FAILED','REFUND_DUE','REFUNDED'));
    ALTER TABLE dbo.ThanhToan CHECK CONSTRAINT CK_ThanhToan_TrangThai;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

-- Update existing database procedures so a paid cancellation remains pending until a manual refund is confirmed.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE dbo.sp_CapNhatTrangThaiDon
    @MaDonHang    int,
    @TrangThaiMoi varchar(15),
    @MaNguoiDung  int,
    @LyDo         nvarchar(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @TrangThaiCu varchar(15), @MaNguoiMua int, @ChuShopId int, @MaDon varchar(30),
            @VaiTro varchar(10), @TrangThaiTaiKhoan varchar(10), @Loai varchar(10),
            @Now datetime2(3) = SYSUTCDATETIME(), @TrangThaiSanPhamMoi varchar(20),
            @TrangThaiShop varchar(10), @HinhThucNhan varchar(20);

    DECLARE @SanPhamDoi TABLE (MaSanPham int PRIMARY KEY);

    BEGIN TRY
        BEGIN TRANSACTION;

        SELECT @TrangThaiCu = dh.TrangThai, @MaNguoiMua = dh.MaNguoiDung,
               @ChuShopId = ch.MaNguoiDung, @MaDon = dh.MaDon, @TrangThaiShop = ch.TrangThai,
               @HinhThucNhan = ISNULL(dh.HinhThucNhan, 'GIAO_HANG')
        FROM DonHang dh WITH (UPDLOCK, HOLDLOCK)
        JOIN CuaHang ch ON ch.MaCuaHang = dh.MaCuaHang
        WHERE dh.MaDonHang = @MaDonHang;

        IF @TrangThaiCu IS NULL
            THROW 51201, N'Đơn hàng không tồn tại.', 1;

        SELECT @VaiTro = VaiTro, @TrangThaiTaiKhoan = TrangThai
        FROM NguoiDung WHERE MaNguoiDung = @MaNguoiDung;

        IF @VaiTro IS NULL OR @TrangThaiTaiKhoan <> 'ACTIVE'
            THROW 51202, N'Tài khoản cập nhật không hợp lệ.', 1;

        SET @Loai = CASE WHEN @MaNguoiDung = @ChuShopId  THEN 'SELLER'
                         WHEN @MaNguoiDung = @MaNguoiMua THEN 'BUYER'
                         WHEN @VaiTro = 'ADMIN'          THEN 'ADMIN' END;

        IF @Loai IS NULL
            THROW 51204, N'Bạn không có quyền với đơn hàng này.', 1;

        IF dbo.fn_ChuyenTrangThaiDonHangHopLe(@TrangThaiCu, @TrangThaiMoi) = 0
            THROW 51203, N'Không cho phép chuyển trạng thái này.', 1;

        IF @TrangThaiMoi IN ('CONFIRMED','SHIPPING','DELIVERED') AND @Loai <> 'SELLER'
            THROW 51205, N'Chỉ chủ shop mới được cập nhật xác nhận và giao hàng.', 1;

        IF @TrangThaiMoi IN ('CONFIRMED','SHIPPING','DELIVERED') AND @TrangThaiShop <> 'ACTIVE'
            THROW 51210, N'Cửa hàng đang bị khóa, chỉ có thể hủy đơn.', 1;

        IF @TrangThaiMoi = 'COMPLETED'
        BEGIN
            IF @Loai <> 'BUYER'
                THROW 51206, N'Chỉ người mua mới được xác nhận hoàn tất.', 1;

            -- Đơn GIAO_HANG bắt buộc phải qua DELIVERED mới được hoàn tất (chống nhảy cóc khi đang SHIPPING)
            IF @TrangThaiCu = 'SHIPPING' AND @HinhThucNhan <> 'GAP_TRUC_TIEP'
                THROW 51211, N'Đơn giao hàng tận nơi chỉ có thể xác nhận hoàn tất sau khi đã giao hàng thành công (DELIVERED).', 1;
        END;

        IF @TrangThaiMoi = 'CANCELLED'
        BEGIN
            IF LEN(LTRIM(RTRIM(ISNULL(@LyDo, N'')))) = 0
                THROW 51207, N'Hủy đơn bắt buộc phải có lý do.', 1;

            IF @TrangThaiCu IN ('DELIVERED', 'COMPLETED')
                THROW 51212, N'Không thể hủy đơn hàng đã giao hoặc đã hoàn tất. Vui lòng tạo tranh chấp hoặc khiếu nại nếu có vấn đề.', 1;

            IF @Loai = 'BUYER' AND @TrangThaiCu NOT IN ('PENDING', 'CONFIRMED')
                THROW 51208, N'Người mua chỉ được hủy đơn khi shop chưa giao hàng.', 1;

            IF @Loai = 'SELLER' AND @TrangThaiCu NOT IN ('PENDING', 'CONFIRMED')
                THROW 51209, N'Người bán chỉ được hủy đơn khi chưa giao hàng.', 1;
        END;

        UPDATE DonHang
        SET TrangThai          = @TrangThaiMoi,
            NgayCapNhat        = @Now,
            NguoiHuy           = CASE WHEN @TrangThaiMoi = 'CANCELLED' THEN @Loai ELSE NguoiHuy END,
            LyDoHuy            = CASE WHEN @TrangThaiMoi = 'CANCELLED' THEN LTRIM(RTRIM(@LyDo)) ELSE LyDoHuy END,
            ThoiGianHuy        = CASE WHEN @TrangThaiMoi = 'CANCELLED' THEN @Now ELSE ThoiGianHuy END,
            ThoiGianXacNhan    = CASE WHEN @TrangThaiMoi = 'CONFIRMED' THEN @Now ELSE ThoiGianXacNhan END,
            ThoiGianBatDauGiao = CASE WHEN @TrangThaiMoi = 'SHIPPING'  THEN @Now ELSE ThoiGianBatDauGiao END,
            ThoiGianDaGiao     = CASE WHEN @TrangThaiMoi = 'DELIVERED' THEN @Now ELSE ThoiGianDaGiao END,
            ThoiGianHoanTat    = CASE WHEN @TrangThaiMoi = 'COMPLETED' THEN @Now ELSE ThoiGianHoanTat END,
            TrangThaiThanhToan = CASE WHEN @TrangThaiMoi = 'COMPLETED' AND PhuongThucThanhToan = 'COD' AND TrangThaiThanhToan = 'UNPAID'
                                      THEN 'PAID'
                                      WHEN @TrangThaiMoi = 'CANCELLED' AND TrangThaiThanhToan = 'PAID'
                                      THEN 'REFUND_DUE'
                                      ELSE TrangThaiThanhToan END
        WHERE MaDonHang = @MaDonHang;

        INSERT LichSuTrangThaiDonHang (MaDonHang, TrangThaiCu, TrangThaiMoi, LoaiNguoiThucHien, NguoiThucHienId, LyDo)
        VALUES (@MaDonHang, @TrangThaiCu, @TrangThaiMoi, @Loai, @MaNguoiDung, @LyDo);

        IF @TrangThaiMoi IN ('CANCELLED','COMPLETED')
        BEGIN
            SET @TrangThaiSanPhamMoi = CASE WHEN @TrangThaiMoi = 'COMPLETED' THEN 'SOLD' ELSE 'ACTIVE' END;

            UPDATE ChiTietDonHang SET DangGiuCho = 0 WHERE MaDonHang = @MaDonHang;

            UPDATE sp
            SET TrangThai = @TrangThaiSanPhamMoi, NgayCapNhat = @Now
            OUTPUT inserted.MaSanPham INTO @SanPhamDoi (MaSanPham)
            FROM SanPham sp
            JOIN ChiTietDonHang ct ON ct.MaSanPham = sp.MaSanPham
            WHERE ct.MaDonHang = @MaDonHang AND sp.TrangThai = 'RESERVED';

            INSERT LichSuTrangThaiSanPham (MaSanPham, TrangThaiCu, TrangThaiMoi, LoaiNguoiThucHien, NguoiThucHienId, LyDo)
            SELECT x.MaSanPham, 'RESERVED', @TrangThaiSanPhamMoi, @Loai, @MaNguoiDung,
                   CONCAT(N'Đơn ', @MaDon, CASE WHEN @TrangThaiMoi = 'COMPLETED' THEN N' hoàn tất' ELSE N' bị hủy' END)
            FROM @SanPhamDoi x;
        END;

        -- Đồng bộ bảng ThanhToan khi hoàn tất hoặc hủy đơn đã thanh toán
        IF @TrangThaiMoi = 'COMPLETED' AND EXISTS (SELECT 1 FROM DonHang WHERE MaDonHang = @MaDonHang AND TrangThaiThanhToan = 'PAID')
        BEGIN
            UPDATE ThanhToan
            SET TrangThaiThanhToan = 'SUCCESS',
                ThoiGianThanhToan = ISNULL(ThoiGianThanhToan, @Now)
            WHERE MaDonHang = @MaDonHang;
        END
        ELSE IF @TrangThaiMoi = 'CANCELLED' AND EXISTS (SELECT 1 FROM DonHang WHERE MaDonHang = @MaDonHang AND TrangThaiThanhToan = 'REFUND_DUE')
        BEGIN
            UPDATE ThanhToan
            SET TrangThaiThanhToan = 'REFUND_DUE'
            WHERE MaDonHang = @MaDonHang;
        END;

        INSERT ThongBao (MaNguoiDung, Loai, TieuDe, NoiDung, MaThamChieu)
        SELECT r.Id, 'ORDER', N'Cập nhật đơn hàng',
               CONCAT(N'Đơn ', @MaDon, N' chuyển sang trạng thái "',
                      CASE @TrangThaiMoi
                          WHEN 'CONFIRMED' THEN N'Đã xác nhận' WHEN 'SHIPPING'  THEN N'Đang giao'
                          WHEN 'DELIVERED' THEN N'Đã giao'     WHEN 'COMPLETED' THEN N'Hoàn tất'
                          WHEN 'CANCELLED' THEN N'Đã hủy'      ELSE @TrangThaiMoi END, N'".'), @MaDonHang
        FROM (VALUES (@MaNguoiMua), (@ChuShopId)) r (Id)
        WHERE r.Id <> @MaNguoiDung;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;

    SELECT @MaDonHang AS MaDonHang, @TrangThaiCu AS TrangThaiCu, @TrangThaiMoi AS TrangThaiMoi;
END;
GO

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

        IF @TrangThaiMoi = 'CANCELLED' AND @TrangThaiCu IN ('DELIVERED', 'COMPLETED')
            THROW 51212, N'Không thể hủy đơn hàng đã giao hoặc đã hoàn tất.', 1;

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
            TrangThaiThanhToan = CASE WHEN @TrangThaiMoi = 'CANCELLED' AND TrangThaiThanhToan = 'PAID' THEN 'REFUND_DUE'
                                      WHEN @TrangThaiMoi = 'COMPLETED' AND PhuongThucThanhToan = 'COD' AND TrangThaiThanhToan = 'UNPAID' THEN 'PAID'
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
        IF @TrangThaiMoi = 'CANCELLED' AND EXISTS (SELECT 1 FROM DonHang WHERE MaDonHang = @MaDonHang AND TrangThaiThanhToan = 'REFUND_DUE')
        BEGIN
            UPDATE ThanhToan
            SET TrangThaiThanhToan = 'REFUND_DUE'
            WHERE MaDonHang = @MaDonHang;
        END
        ELSE IF @TrangThaiMoi = 'COMPLETED' AND EXISTS (SELECT 1 FROM DonHang WHERE MaDonHang = @MaDonHang AND TrangThaiThanhToan = 'PAID')
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

-- Historical rows are intentionally left unchanged because the database cannot prove whether a refund was actually transferred.
SELECT MaDonHang, MaDon, TrangThai, TrangThaiThanhToan
FROM dbo.DonHang
WHERE TrangThai = 'CANCELLED' AND TrangThaiThanhToan = 'REFUNDED';
GO
