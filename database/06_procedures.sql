/* ================================================================
   PHAN 11: PROCEDURE TẠO ĐƠN HÀNG
   Ten: sp_TaoDonHang

   Chức năng:
   1. Kiểm tra tài khoản người mua.
   2. Kiểm tra sản phẩm và người bán.
   3. Lấy giá từ database.
   4. Tạo DonHang va ChiTietDonHang.
   5. Chuyển sản phẩm sang RESERVED.
   6. Ghi lịch sử và thông báo cho người bán.
   7. Xóa sản phẩm khỏi giỏ hàng sau khi đặt thành công.

   Mỗi đơn hàng trong phiên bản này gồm 1 sản phẩm.
   Nếu giỏ hàng có nhiều người bán, Backend tách thành nhiều đơn.
   ================================================================ */

CREATE OR ALTER PROCEDURE sp_TaoDonHang
    @MaNguoiMua       BIGINT,
    @MaSanPham        BIGINT,
    @TenNguoiNhan     NVARCHAR(150),
    @SDTNguoiNhan     VARCHAR(20),
    @DiaChiGiaoHang   NVARCHAR(1000),
    @PhiVanChuyen     DECIMAL(18,2) = 0,
    @PhuongThucTT     VARCHAR(30) = 'COD',
    @MaDonHangMoi     BIGINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE
        @MaNguoiBan BIGINT,
        @GiaSanPham DECIMAL(18,2),
        @TenSanPham NVARCHAR(200),
        @TrangThaiSP VARCHAR(20),
        @LoaiBan VARCHAR(20),
        @TrangThaiNguoiMua VARCHAR(20),
        @VaiTroNguoiMua VARCHAR(20),
        @TrangThaiNguoiBan VARCHAR(20),
        @VaiTroNguoiBan VARCHAR(20),
        @MaDon VARCHAR(30);

    BEGIN TRY
        BEGIN TRANSACTION;
        SELECT
            @TrangThaiNguoiMua = TrangThaiTaiKhoan,
            @VaiTroNguoiMua = VaiTro
        FROM NguoiDung
        WHERE UserId = @MaNguoiMua;
        IF @TrangThaiNguoiMua IS NULL
            THROW 51101, N'Tài khoản người mua không tồn tại.', 1;

        IF @TrangThaiNguoiMua <> 'ACTIVE'
            THROW 51102, N'Tài khoản người mua không hoạt động.', 1;

        IF @VaiTroNguoiMua NOT IN ('BUYER', 'SELLER')
            THROW 51103, N'Tài khoản không được phép mua hàng.', 1;

        -- Khóa sản phẩm để tránh 2 người đặt cùng 1 sản phẩm.
        SELECT
            @MaNguoiBan = MaNguoiBan,
            @GiaSanPham = GiaCoDinh,
            @TenSanPham = TenSanPham,
            @TrangThaiSP = TrangThaiSanPham,
            @LoaiBan = HinhThucBan
        FROM SanPham WITH (UPDLOCK, HOLDLOCK)
        WHERE MaSanPham = @MaSanPham;

        IF @MaNguoiBan IS NULL
            THROW 51104, N'Sản phẩm không tồn tại.', 1;

        IF @MaNguoiMua = @MaNguoiBan
            THROW 51105, N'Không thể mua sản phẩm của chính mình.', 1;

        IF @TrangThaiSP <> 'APPROVED'
            THROW 51106, N'Sản phẩm không sẵn sàng để đặt hàng.', 1;

        IF @LoaiBan <> 'FIXED_PRICE'
            THROW 51107, N'Sản phẩm đấu giá không đặt theo giá cố định.', 1;

        SELECT
            @TrangThaiNguoiBan = TrangThaiTaiKhoan,
            @VaiTroNguoiBan = VaiTro
        FROM NguoiDung
        WHERE UserId = @MaNguoiBan;

        IF @TrangThaiNguoiBan <> 'ACTIVE'
           OR @VaiTroNguoiBan <> 'SELLER'
            THROW 51108, N'Người bán không hoạt động.', 1;

        IF @PhiVanChuyen < 0
            THROW 51109, N'Phí vận chuyển không hợp lệ.', 1;

        IF @PhuongThucTT NOT IN ('COD', 'BANK_TRANSFER', 'ONLINE')
            THROW 51110, N'phương thức thanh toán không hợp lệ.', 1;

        IF LEN(LTRIM(RTRIM(@TenNguoiNhan))) = 0
            THROW 51111, N'Chưa nhập tên người nhận.', 1;

        IF LEN(LTRIM(RTRIM(@SDTNguoiNhan))) < 9
            THROW 51112, N'Số điện thoại không hợp lệ.', 1;

        IF LEN(LTRIM(RTRIM(@DiaChiGiaoHang))) = 0
            THROW 51113, N'Chưa nhập địa chỉ giao hàng.', 1;

        -- Tạo mã đơn hàng.
        SET @MaDon =
            'DH'
            + CONVERT(VARCHAR(8), GETDATE(), 112)
            + RIGHT
              (
                  '000000'
                  + CAST(ABS(CHECKSUM(NEWID())) % 1000000 AS VARCHAR(6)),
                  6
              );

        INSERT INTO DonHang
        (
            MaDon, MaNguoiMua, MaNguoiBan,
            TenNguoiNhan, SDTNguoiNhan, DiaChiGiaoHang,
            TienHang, PhiVanChuyen, TongTien,
            PhuongThucThanhToan, TrangThaiThanhToan, TrangThaiDonHang
        )
        VALUES
        (
            @MaDon, @MaNguoiMua, @MaNguoiBan,
            @TenNguoiNhan, @SDTNguoiNhan, @DiaChiGiaoHang,
            @GiaSanPham, @PhiVanChuyen, @GiaSanPham + @PhiVanChuyen,
            @PhuongThucTT, 'UNPAID', 'PENDING'
        );

        SET @MaDonHangMoi = SCOPE_IDENTITY();

        INSERT INTO ChiTietDonHang
        (
            MaDonHang, MaSanPham, TenSanPham, DonGia, SoLuong
        )
        VALUES
        (
            @MaDonHangMoi, @MaSanPham, @TenSanPham, @GiaSanPham, 1
        );

        UPDATE SanPham
        SET TrangThaiSanPham = 'RESERVED',
            NgayCapNhat = SYSDATETIME()
        WHERE MaSanPham = @MaSanPham;

        INSERT INTO LichSuTrangThaiDonHang
        (
            MaDonHang, TrangThaiCu, TrangThaiMoi, NguoiThayDoi, GhiChu
        )
        VALUES
        (
            @MaDonHangMoi, NULL, 'PENDING',
            @MaNguoiMua, N'Người mua tạo đơn hàng.'
        );

        INSERT INTO ThongBao
        (
            UserId, TieuDe, NoiDung, LoaiThongBao, MaThamChieu
        )
        VALUES
        (
            @MaNguoiBan,
            N'Bạn có đơn hàng mới',
            N'Bạn vừa nhận được một đơn hàng mới.',
            'ORDER',
            @MaDonHangMoi
        );

        DELETE FROM GioHang
        WHERE MaNguoiMua = @MaNguoiMua
          AND MaSanPham = @MaSanPham;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO

* ================================================================
   PHAN 12: PROCEDURE CẬP NHẬT TRẠNG THÁI ĐƠN HÀNG
   Ten: sp_CapNhatTrangThaiDon

   Chuc nang:
   - Kiểm tra trạng thái cũ và trạng thái mới.
   - Kiểm tra quyền người cập nhật.
   - Ghi lịch sử thay đổi.
   - Hủy đơn thì trả sản phẩm về APPROVED.
   - Hoàn tất đơn thì chuyển sản phẩm sang SOLD.

   Luồng trạng thái:
   PENDING -> CONFIRMED -> PREPARING -> SHIPPING
   -> DELIVERED -> COMPLETED.

   Huỷ đơn:
   PENDING/CONFIRMED -> CANCELLED.
   ================================================================ */

CREATE OR ALTER PROCEDURE sp_CapNhatTrangThaiDon
    @MaDonHang      BIGINT,
    @TrangThaiMoi   VARCHAR(20),
    @NguoiCapNhat   BIGINT,
    @GhiChu         NVARCHAR(1000) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE
        @TrangThaiCu VARCHAR(20),
        @MaNguoiMua BIGINT,
        @MaNguoiBan BIGINT,
        @VaiTroNguoiCapNhat VARCHAR(20),
        @TrangThaiTaiKhoan VARCHAR(20);

    BEGIN TRY
        BEGIN TRANSACTION;

        SELECT
            @TrangThaiCu = TrangThaiDonHang,
            @MaNguoiMua = MaNguoiMua,
            @MaNguoiBan = MaNguoiBan
        FROM DonHang WITH (UPDLOCK, HOLDLOCK)
        WHERE MaDonHang = @MaDonHang;

        IF @TrangThaiCu IS NULL
            THROW 51201, N'Đơn hàng không tồn tại.', 1;

        SELECT
            @VaiTroNguoiCapNhat = VaiTro,
            @TrangThaiTaiKhoan = TrangThaiTaiKhoan
        FROM NguoiDung
        WHERE UserId = @NguoiCapNhat;

        IF @VaiTroNguoiCapNhat IS NULL
           OR @TrangThaiTaiKhoan <> 'ACTIVE'
            THROW 51202, N'Tài khoản cập nhật không hợp lệ.', 1;

        IF NOT
        (
            (@TrangThaiCu = 'PENDING' AND @TrangThaiMoi IN ('CONFIRMED', 'CANCELLED'))
            OR
            (@TrangThaiCu = 'CONFIRMED' AND @TrangThaiMoi IN ('PREPARING', 'CANCELLED'))
            OR
            (@TrangThaiCu = 'PREPARING' AND @TrangThaiMoi = 'SHIPPING')
            OR
            (@TrangThaiCu = 'SHIPPING' AND @TrangThaiMoi = 'DELIVERED')
            OR
            (@TrangThaiCu = 'DELIVERED' AND @TrangThaiMoi = 'COMPLETED')
        )
            THROW 51203, N'Không cho phép chuyển trạng thái này.', 1;

        IF @TrangThaiMoi = 'CANCELLED'
           AND @NguoiCapNhat NOT IN (@MaNguoiMua, @MaNguoiBan)
            THROW 51204, N'Bạn không có quyền hủy đơn hàng này.', 1;

        IF @TrangThaiMoi IN
           ('CONFIRMED', 'PREPARING', 'SHIPPING', 'DELIVERED')
           AND @NguoiCapNhat <> @MaNguoiBan
            THROW 51205, N'Chỉ người bán mới được cập nhập giao hàng.', 1;

        IF @TrangThaiMoi = 'COMPLETED'
           AND @NguoiCapNhat <> @MaNguoiMua
            THROW 51206, N'Chỉ người mua mới được xác nhận hoàn tất.', 1;

        UPDATE DonHang
        SET TrangThaiDonHang = @TrangThaiMoi,
            NgayCapNhat = SYSDATETIME(),
            LyDoHuy =
                CASE WHEN @TrangThaiMoi = 'CANCELLED'
                     THEN @GhiChu ELSE LyDoHuy END,
            NgayHuy =
                CASE WHEN @TrangThaiMoi = 'CANCELLED'
                     THEN SYSDATETIME() ELSE NgayHuy END
        WHERE MaDonHang = @MaDonHang;

        INSERT INTO LichSuTrangThaiDonHang
        (
            MaDonHang, TrangThaiCu, TrangThaiMoi, NguoiThayDoi, GhiChu
        )
        VALUES
        (
            @MaDonHang, @TrangThaiCu, @TrangThaiMoi,
            @NguoiCapNhat, @GhiChu
        );

        -- Hủy đơn: cho phép bán lại sản phẩm.
        IF @TrangThaiMoi = 'CANCELLED'
        BEGIN
            UPDATE sp
            SET TrangThaiSanPham = 'APPROVED',
                NgayCapNhat = SYSDATETIME()
            FROM SanPham AS sp
            JOIN ChiTietDonHang AS ct ON ct.MaSanPham = sp.MaSanPham
            WHERE ct.MaDonHang = @MaDonHang
              AND sp.TrangThaiSanPham = 'RESERVED';
        END;

        -- Đơn hoàn tất: sản phẩm đã bán.
        IF @TrangThaiMoi = 'COMPLETED'
        BEGIN
            UPDATE sp
            SET TrangThaiSanPham = 'SOLD',
                NgayCapNhat = SYSDATETIME()
            FROM SanPham AS sp
            JOIN ChiTietDonHang AS ct ON ct.MaSanPham = sp.MaSanPham
            WHERE ct.MaDonHang = @MaDonHang;
        END;

        INSERT INTO ThongBao
        (
            UserId, TieuDe, NoiDung, LoaiThongBao, MaThamChieu
        )
        VALUES
        (
            CASE
                WHEN @NguoiCapNhat = @MaNguoiMua THEN @MaNguoiBan
                ELSE @MaNguoiMua
            END,
            N'Cập nhật đơn hàng',
            N'Đơn hàng của bạn vừa thay đổi trạng thái.',
            'ORDER',
            @MaDonHang
        );

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO