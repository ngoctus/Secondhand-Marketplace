SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

USE SecondHandMarketplace;
GO

-- Kiểu bảng truyền danh sách Id (dùng cho đặt hàng nhiều sản phẩm)
IF TYPE_ID(N'DanhSachId') IS NULL
    CREATE TYPE DanhSachId AS TABLE (Id int NOT NULL PRIMARY KEY);
GO

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

-- ---------------------------------------------------------------------
-- Thêm từ khóa cấm (tự chuẩn hóa)
-- ---------------------------------------------------------------------
CREATE OR ALTER PROCEDURE sp_ThemTuKhoaCam
    @CumTu  nvarchar(100),
    @Nhom   varchar(30)   = NULL,
    @GhiChu nvarchar(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Chuan nvarchar(100) = fn_ChuanHoaChuoi(@CumTu);

    IF @Chuan IS NULL OR LEN(@Chuan) = 0
        THROW 50110, N'Từ khóa không hợp lệ.', 1;
    IF EXISTS (SELECT 1 FROM TuKhoaCam WHERE CumTuChuanHoa = @Chuan)
        THROW 50111, N'Từ khóa đã tồn tại.', 1;

    INSERT TuKhoaCam (CumTu, CumTuChuanHoa, Nhom, GhiChu)
    VALUES (LTRIM(RTRIM(@CumTu)), @Chuan, @Nhom, @GhiChu);

    SELECT SCOPE_IDENTITY() AS Id;
END
GO

-- Tạo shop (kích hoạt Seller, có hiệu lực ngay)
CREATE OR ALTER PROCEDURE sp_TaoCuaHang
    @MaNguoiDung       int,
    @TenShop           nvarchar(100),
    @SoDienThoaiLienHe varchar(15),
    @MoTa              nvarchar(1000) = NULL,
    @Logo              nvarchar(500)  = NULL,
    @CuaHangId         int OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NOT EXISTS (SELECT 1 FROM NguoiDung WHERE MaNguoiDung = @MaNguoiDung AND VaiTro = 'USER' AND TrangThai = 'ACTIVE')
        THROW 50001, N'Tài khoản không hợp lệ hoặc không được phép tạo shop.', 1;
    IF EXISTS (SELECT 1 FROM CuaHang WHERE MaNguoiDung = @MaNguoiDung)
        THROW 50002, N'Tài khoản này đã có shop (mỗi tài khoản chỉ có 1 shop).', 1;

    BEGIN TRANSACTION;
    INSERT CuaHang (MaNguoiDung, TenShop, SoDienThoaiLienHe, MoTa, Logo)
    VALUES (@MaNguoiDung, @TenShop, @SoDienThoaiLienHe, @MoTa, @Logo);
    SET @CuaHangId = SCOPE_IDENTITY();

    EXEC sp_GhiNhatKy @MaNguoiDung = @MaNguoiDung, @HanhDong = 'CREATE_SHOP',
                          @LoaiDoiTuong = 'CuaHang', @DoiTuongId = @CuaHangId;
    COMMIT TRANSACTION;
END
GO

-- TRUNG TÂM chuyển trạng thái sản phẩm (ProductStateMachine ở mức DB)
-- Kiểm tra hợp lệ + cập nhật có điều kiện + ghi LichSuTrangThaiSanPham
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

    DECLARE @TrangThaiCu varchar(20), @TuTaoTran bit = 0, @Msg nvarchar(400);

    IF @LoaiNguoiThucHien = 'SYSTEM' SET @NguoiThucHienId = NULL;

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

    IF fn_ChuyenTrangThaiSanPhamHopLe(@TrangThaiCu, @TrangThaiMoi) = 0
    BEGIN
        SET @Msg = CONCAT(N'Không thể chuyển sản phẩm từ ', @TrangThaiCu, N' sang ', @TrangThaiMoi, N'.');
        THROW 50011, @Msg, 1;
    END

    IF @TrangThaiMoi = 'REMOVED' AND LEN(LTRIM(RTRIM(ISNULL(@LyDo, N'')))) = 0
        THROW 50012, N'Gỡ sản phẩm bắt buộc phải có lý do.', 1;

    UPDATE SanPham
    SET TrangThai = @TrangThaiMoi,
        LyDoGo    = CASE WHEN @TrangThaiMoi = 'REMOVED' THEN @LyDo ELSE LyDoGo END
    WHERE MaSanPham = @MaSanPham AND TrangThai = @TrangThaiCu;

    IF @@ROWCOUNT = 0
        THROW 50013, N'Trạng thái sản phẩm vừa bị thay đổi, vui lòng tải lại.', 1;

    INSERT LichSuTrangThaiSanPham (MaSanPham, TrangThaiCu, TrangThaiMoi, LoaiNguoiThucHien, NguoiThucHienId, LyDo)
    VALUES (@MaSanPham, @TrangThaiCu, @TrangThaiMoi, @LoaiNguoiThucHien, @NguoiThucHienId, @LyDo);

    IF @TuTaoTran = 1 COMMIT TRANSACTION;
END
GO

-- Người bán GỬI DUYỆT
-- Vi phạm rule/từ cấm: KHÔNG đổi trạng thái, trả danh sách lỗi theo từng trường, RETURN 1
-- Đạt: -> PENDING_REVIEW + tạo LuotKiemDuyet (+ PhanTichAI QUEUED), RETURN 0
CREATE OR ALTER PROCEDURE sp_GuiDuyetSanPham
    @MaSanPham     int,
    @MaNguoiDung   int,
    @TaoPhanTichAI bit = 1
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @ChuShopId int, @TrangThaiShop varchar(10), @TrangThaiNguoiDung varchar(10),
            @TrangThai varchar(20), @LuotId int;

    SELECT @ChuShopId = ch.MaNguoiDung, @TrangThaiShop = ch.TrangThai, @TrangThai = sp.TrangThai
    FROM SanPham sp
    JOIN CuaHang ch ON ch.MaCuaHang = sp.MaCuaHang
    WHERE sp.MaSanPham = @MaSanPham AND sp.DaXoa = 0;

    -- Không phải chủ sản phẩm -> coi như không tồn tại (không lộ sự tồn tại)
    IF @ChuShopId IS NULL OR @ChuShopId <> @MaNguoiDung
        THROW 50010, N'Sản phẩm không tồn tại.', 1;

    SELECT @TrangThaiNguoiDung = TrangThai FROM NguoiDung WHERE MaNguoiDung = @MaNguoiDung;

    IF @TrangThaiNguoiDung <> 'ACTIVE' OR @TrangThaiShop <> 'ACTIVE'
        THROW 50014, N'Tài khoản hoặc cửa hàng đang bị khóa, không thể gửi duyệt.', 1;

    IF @TrangThai NOT IN ('DRAFT','REJECTED','ACTIVE','HIDDEN','REMOVED')
        THROW 50015, N'Sản phẩm ở trạng thái hiện tại không thể gửi duyệt (đang chờ duyệt, đang giữ chỗ hoặc đã bán).', 1;

    DECLARE @Loi TABLE (MaLoi nvarchar(50), Truong nvarchar(50), ThongBao nvarchar(500));
    INSERT @Loi (MaLoi, Truong, ThongBao)
    SELECT MaLoi, Truong, ThongBao FROM fn_KiemTraRuleSanPham(@MaSanPham);

    IF EXISTS (SELECT 1 FROM @Loi)
    BEGIN
        SELECT CAST(0 AS bit) AS ThanhCong, MaLoi, Truong, ThongBao, CAST(NULL AS int) AS LuotKiemDuyetId
        FROM @Loi;
        RETURN 1;
    END

    BEGIN TRANSACTION;

    EXEC sp_ChuyenTrangThaiSanPham @MaSanPham = @MaSanPham, @TrangThaiMoi = 'PENDING_REVIEW',
                                   @LoaiNguoiThucHien = 'SELLER', @NguoiThucHienId = @MaNguoiDung,
                                   @LyDo = N'Gửi kiểm duyệt';

    INSERT LuotKiemDuyet (MaSanPham, KetQuaRule)
    VALUES (@MaSanPham, N'{"passed":true,"errors":[]}');
    SET @LuotId = SCOPE_IDENTITY();

    IF @TaoPhanTichAI = 1
        INSERT PhanTichAI (MaLuotKiemDuyet, TrangThai) VALUES (@LuotId, 'QUEUED');

    COMMIT TRANSACTION;

    SELECT CAST(1 AS bit) AS ThanhCong, CAST(NULL AS nvarchar(50)) AS MaLoi, CAST(NULL AS nvarchar(50)) AS Truong,
           CAST(NULL AS nvarchar(500)) AS ThongBao, @LuotId AS LuotKiemDuyetId;
    RETURN 0;
END
GO

-- Admin DUYỆT / TỪ CHỐI (dùng chung 1 proc lõi)
CREATE OR ALTER PROCEDURE sp_QuyetDinhKiemDuyet
    @MaSanPham     int,
    @QuanTriVienId int,
    @QuyetDinh     varchar(10),              -- APPROVED | REJECTED
    @LyDo          nvarchar(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @LuotId int, @TrangThaiMoi varchar(20);

    IF NOT EXISTS (SELECT 1 FROM NguoiDung WHERE MaNguoiDung = @QuanTriVienId AND VaiTro = 'ADMIN' AND TrangThai = 'ACTIVE')
        THROW 50020, N'Chỉ quản trị viên mới được kiểm duyệt sản phẩm.', 1;
    IF @QuyetDinh NOT IN ('APPROVED','REJECTED')
        THROW 50023, N'Quyết định kiểm duyệt không hợp lệ.', 1;
    IF @QuyetDinh = 'REJECTED' AND LEN(LTRIM(RTRIM(ISNULL(@LyDo, N'')))) < 10
        THROW 50021, N'Lý do từ chối bắt buộc và phải có ít nhất 10 ký tự.', 1;

    BEGIN TRANSACTION;

    SELECT @LuotId = l.MaLuotKiemDuyet
    FROM LuotKiemDuyet l WITH (UPDLOCK, ROWLOCK)
    JOIN SanPham sp ON sp.MaSanPham = l.MaSanPham
    WHERE l.MaSanPham = @MaSanPham AND l.QuyetDinh IS NULL
      AND sp.TrangThai = 'PENDING_REVIEW' AND sp.DaXoa = 0;

    IF @LuotId IS NULL
        THROW 50022, N'Sản phẩm không còn ở trạng thái chờ duyệt (đã được xử lý hoặc đã bị thay đổi).', 1;

    SET @TrangThaiMoi = CASE @QuyetDinh WHEN 'APPROVED' THEN 'ACTIVE' ELSE 'REJECTED' END;

    UPDATE LuotKiemDuyet
    SET QuyetDinh = @QuyetDinh,
        LyDo = CASE WHEN @QuyetDinh = 'REJECTED' THEN @LyDo END,
        MaQuanTriVien = @QuanTriVienId,
        ThoiGianQuyetDinh = SYSUTCDATETIME()
    WHERE MaLuotKiemDuyet = @LuotId;

    EXEC sp_ChuyenTrangThaiSanPham @MaSanPham = @MaSanPham, @TrangThaiMoi = @TrangThaiMoi,
                                   @LoaiNguoiThucHien = 'ADMIN', @NguoiThucHienId = @QuanTriVienId,
                                   @LyDo = @LyDo;

    -- Ghi nhận Admin có làm theo gợi ý AI hay không (nếu AI đã phân tích xong)
    UPDATE PhanTichAI
    SET QuanTriVienLamTheo = CASE
            WHEN DeXuat = 'NEEDS_REVIEW' THEN NULL
            WHEN (DeXuat = 'APPROVE' AND @QuyetDinh = 'APPROVED')
              OR (DeXuat = 'REJECT'  AND @QuyetDinh = 'REJECTED') THEN 1
            ELSE 0 END
    WHERE MaLuotKiemDuyet = @LuotId AND TrangThai = 'DONE';

    EXEC sp_GhiNhatKy @MaNguoiDung = @QuanTriVienId,
                          @HanhDong = CASE @QuyetDinh WHEN 'APPROVED' THEN 'APPROVE_PRODUCT' ELSE 'REJECT_PRODUCT' END,
                          @LoaiDoiTuong = 'SanPham', @DoiTuongId = @MaSanPham, @LyDo = @LyDo;

    COMMIT TRANSACTION;

    SELECT @LuotId AS LuotKiemDuyetId, @TrangThaiMoi AS TrangThaiMoi;
END
GO

CREATE OR ALTER PROCEDURE sp_DuyetSanPham
    @MaSanPham int, @QuanTriVienId int
AS
BEGIN
    SET NOCOUNT ON;
    EXEC sp_QuyetDinhKiemDuyet @MaSanPham = @MaSanPham, @QuanTriVienId = @QuanTriVienId, @QuyetDinh = 'APPROVED';
END
GO

CREATE OR ALTER PROCEDURE sp_TuChoiSanPham
    @MaSanPham int, @QuanTriVienId int, @LyDo nvarchar(500)
AS
BEGIN
    SET NOCOUNT ON;
    EXEC sp_QuyetDinhKiemDuyet @MaSanPham = @MaSanPham, @QuanTriVienId = @QuanTriVienId,
                                @QuyetDinh = 'REJECTED', @LyDo = @LyDo;
END
GO

-- Xóa mềm sản phẩm (không xóa được nếu đã có đơn - kể cả đơn đã hủy - hoặc đã bán)
CREATE OR ALTER PROCEDURE sp_XoaMemSanPham
    @MaSanPham   int,
    @MaNguoiDung int
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @TrangThai varchar(20);

    SELECT @TrangThai = sp.TrangThai
    FROM SanPham sp
    JOIN CuaHang ch ON ch.MaCuaHang = sp.MaCuaHang
    WHERE sp.MaSanPham = @MaSanPham AND sp.DaXoa = 0 AND ch.MaNguoiDung = @MaNguoiDung;

    IF @TrangThai IS NULL
        THROW 50010, N'Sản phẩm không tồn tại.', 1;
    IF @TrangThai IN ('SOLD','RESERVED')
       OR EXISTS (SELECT 1 FROM ChiTietDonHang WHERE MaSanPham = @MaSanPham)
        THROW 50016, N'Sản phẩm đã có đơn hàng hoặc đã bán, không thể xóa.', 1;

    BEGIN TRANSACTION;
    UPDATE SanPham SET DaXoa = 1, ThoiGianXoa = SYSUTCDATETIME() WHERE MaSanPham = @MaSanPham;
    DELETE FROM GioHang WHERE MaSanPham = @MaSanPham;
    EXEC sp_GhiNhatKy @MaNguoiDung = @MaNguoiDung, @HanhDong = 'DELETE_PRODUCT',
                          @LoaiDoiTuong = 'SanPham', @DoiTuongId = @MaSanPham;
    COMMIT TRANSACTION;
END
GO

-- Thêm vào giỏ (đã có trong giỏ thì bỏ qua, không lỗi)
CREATE OR ALTER PROCEDURE sp_ThemVaoGio
    @MaNguoiDung int,
    @MaSanPham   int
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @ChuShopId int, @TrangThai varchar(20), @TrangThaiShop varchar(10);

    SELECT @ChuShopId = ch.MaNguoiDung, @TrangThai = sp.TrangThai, @TrangThaiShop = ch.TrangThai
    FROM SanPham sp
    JOIN CuaHang ch ON ch.MaCuaHang = sp.MaCuaHang
    WHERE sp.MaSanPham = @MaSanPham AND sp.DaXoa = 0;

    IF @ChuShopId IS NULL OR @TrangThai <> 'ACTIVE' OR @TrangThaiShop <> 'ACTIVE'
        THROW 50032, N'Sản phẩm không còn bán.', 1;
    IF @ChuShopId = @MaNguoiDung
        THROW 50031, N'Không thể thêm sản phẩm của chính mình vào giỏ hàng.', 1;

    IF EXISTS (SELECT 1 FROM GioHang WHERE MaNguoiDung = @MaNguoiDung AND MaSanPham = @MaSanPham)
        RETURN 0;

    INSERT GioHang (MaNguoiDung, MaSanPham) VALUES (@MaNguoiDung, @MaSanPham);
    RETURN 0;
END
GO

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