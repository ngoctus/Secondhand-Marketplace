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
    DECLARE @Chuan nvarchar(100) = dbo.fn_ChuanHoaChuoi(@CumTu);

    IF @Chuan IS NULL OR LEN(@Chuan) = 0
        THROW 50110, N'Từ khóa không hợp lệ.', 1;
    IF EXISTS (SELECT 1 FROM TuKhoaCam WHERE CumTuChuanHoa = @Chuan)
        THROW 50111, N'Từ khóa đã tồn tại.', 1;

    INSERT TuKhoaCam (CumTu, CumTuChuanHoa, Nhom, GhiChu)
    VALUES (LTRIM(RTRIM(@CumTu)), @Chuan, @Nhom, @GhiChu);

    SELECT SCOPE_IDENTITY() AS Id;
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
            @TrangThai varchar(20), @LuotId int;

    SELECT @ChuShopId = ch.MaNguoiDung, @TrangThaiShop = ch.TrangThai, @TrangThai = sp.TrangThai
    FROM SanPham sp
    JOIN CuaHang ch ON ch.MaCuaHang = sp.MaCuaHang
    WHERE sp.MaSanPham = @MaSanPham AND sp.DaXoa = 0;

    IF @ChuShopId IS NULL OR @ChuShopId <> @MaNguoiDung
        THROW 50010, N'Sản phẩm không tồn tại.', 1;

    SELECT @TrangThaiNguoiDung = TrangThai FROM NguoiDung WHERE MaNguoiDung = @MaNguoiDung;

    IF @TrangThaiNguoiDung <> 'ACTIVE' OR @TrangThaiShop <> 'ACTIVE'
        THROW 50014, N'Tài khoản hoặc cửa hàng đang bị khóa, không thể gửi duyệt.', 1;

    IF @TrangThai NOT IN ('DRAFT','REJECTED','ACTIVE','HIDDEN','REMOVED')
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

-- Admin DUYỆT / TỪ CHỐI (Đã sửa cú pháp CASE tránh lỗi parser EXEC)
CREATE OR ALTER PROCEDURE sp_QuyetDinhKiemDuyet
    @MaSanPham     int,
    @QuanTriVienId int,
    @QuyetDinh     varchar(10),
    @LyDo          nvarchar(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @LuotId int, @TrangThaiMoi varchar(20), @HanhDong varchar(50);

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
        THROW 50022, N'Sản phẩm không còn ở trạng thái chờ duyệt.', 1;

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

    UPDATE PhanTichAI
    SET QuanTriVienLamTheo = CASE
            WHEN DeXuat = 'NEEDS_REVIEW' THEN NULL
            WHEN (DeXuat = 'APPROVE' AND @QuyetDinh = 'APPROVED')
              OR (DeXuat = 'REJECT'  AND @QuyetDinh = 'REJECTED') THEN 1
            ELSE 0 END
    WHERE MaLuotKiemDuyet = @LuotId AND TrangThai = 'DONE';

    -- Khắc phục lỗi parser EXEC bằng cách gán biến trước
    SET @HanhDong = CASE @QuyetDinh WHEN 'APPROVED' THEN 'APPROVE_PRODUCT' ELSE 'REJECT_PRODUCT' END;

    EXEC sp_GhiNhatKy @MaNguoiDung = @QuanTriVienId,
                      @HanhDong = @HanhDong,
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

-- Thêm vào giỏ
CREATE OR ALTER PROCEDURE sp_ThemVaoGio
    @MaNguoiDung int,
    @MaSanPham   int
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @ChuShopId int, @TrangThai varchar(20), @TrangThaiShop varchar(10);

    IF NOT EXISTS (SELECT 1 FROM NguoiDung WHERE MaNguoiDung = @MaNguoiDung AND VaiTro = 'USER' AND TrangThai = 'ACTIVE')
        THROW 50030, N'Chỉ tài khoản USER đang hoạt động mới có giỏ hàng.', 1;

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

-- Tạo đơn hàng theo shop
CREATE OR ALTER PROCEDURE sp_TaoDonHangTheoShop
    @MaNguoiDung         int,
    @MaCuaHang           int,
    @DanhSachSanPham     DanhSachId READONLY,
    @TenNguoiNhan        nvarchar(100),
    @SoDienThoaiNhan     varchar(15),
    @DiaChiNhan          nvarchar(500),
    @PhiVanChuyen        decimal(18,2) = 0,
    @PhuongThucThanhToan varchar(20)   = 'COD',
    @MaDonHangMoi        int OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @TuTaoTran bit = 0, @ChuShopId int, @TrangThaiShop varchar(10),
            @SoSanPham int, @SoHopLe int, @TienHang decimal(18,2),
            @SoThuTu int, @MaDon varchar(30);

    IF @@TRANCOUNT = 0
    BEGIN
        BEGIN TRANSACTION;
        SET @TuTaoTran = 1;
    END

    IF NOT EXISTS (SELECT 1 FROM NguoiDung WHERE MaNguoiDung = @MaNguoiDung AND VaiTro = 'USER' AND TrangThai = 'ACTIVE')
        THROW 51101, N'Chỉ tài khoản USER đang hoạt động mới được đặt hàng.', 1;

    SELECT @ChuShopId = MaNguoiDung, @TrangThaiShop = TrangThai FROM CuaHang WHERE MaCuaHang = @MaCuaHang;
    IF @ChuShopId IS NULL
        THROW 51104, N'Cửa hàng không tồn tại.', 1;
    IF @TrangThaiShop <> 'ACTIVE'
        THROW 51107, N'Cửa hàng đang bị khóa, không thể tạo đơn mới.', 1;
    IF @ChuShopId = @MaNguoiDung
        THROW 51105, N'Không thể mua sản phẩm của chính shop mình.', 1;

    IF @PhuongThucThanhToan NOT IN ('COD','BANK_TRANSFER','ONLINE')
        THROW 51110, N'Phương thức thanh toán không hợp lệ.', 1;
    IF @PhiVanChuyen IS NULL OR @PhiVanChuyen < 0
        THROW 51109, N'Phí vận chuyển không hợp lệ.', 1;
    IF LEN(LTRIM(RTRIM(ISNULL(@TenNguoiNhan, N'')))) = 0
        THROW 51111, N'Chưa nhập tên người nhận.', 1;
    IF @SoDienThoaiNhan IS NULL OR @SoDienThoaiNhan LIKE '%[^0-9]%' OR LEN(@SoDienThoaiNhan) NOT BETWEEN 9 AND 15
        THROW 51112, N'Số điện thoại người nhận phải gồm 9 đến 15 chữ số.', 1;
    IF LEN(LTRIM(RTRIM(ISNULL(@DiaChiNhan, N'')))) = 0
        THROW 51113, N'Chưa nhập địa chỉ giao hàng.', 1;

    SELECT @SoSanPham = COUNT(*) FROM @DanhSachSanPham;
    IF @SoSanPham = 0
        THROW 51114, N'Đơn hàng phải có ít nhất 1 sản phẩm.', 1;

    SELECT @SoHopLe = COUNT(*)
    FROM SanPham sp WITH (UPDLOCK, HOLDLOCK)
    JOIN @DanhSachSanPham d ON d.Id = sp.MaSanPham
    WHERE sp.MaCuaHang = @MaCuaHang AND sp.TrangThai = 'ACTIVE' AND sp.DaXoa = 0;

    IF @SoHopLe <> @SoSanPham
        THROW 51106, N'Có sản phẩm không còn bán hoặc không thuộc cửa hàng này.', 1;

    SELECT @TienHang = SUM(sp.Gia)
    FROM SanPham sp
    JOIN @DanhSachSanPham d ON d.Id = sp.MaSanPham;

    SET @SoThuTu = NEXT VALUE FOR sq_MaDonHang;
    SET @MaDon = 'DH' + CONVERT(varchar(8), SYSUTCDATETIME(), 112) + '-'
               + RIGHT('000000' + CAST(@SoThuTu AS varchar(10)), 6);

    INSERT DonHang (MaDon, MaNguoiDung, MaCuaHang, TenNguoiNhan, SoDienThoaiNhan, DiaChiNhan,
                    TienHang, PhiVanChuyen, TongTien, PhuongThucThanhToan)
    VALUES (@MaDon, @MaNguoiDung, @MaCuaHang, LTRIM(RTRIM(@TenNguoiNhan)), @SoDienThoaiNhan, LTRIM(RTRIM(@DiaChiNhan)),
            @TienHang, @PhiVanChuyen, @TienHang + @PhiVanChuyen, @PhuongThucThanhToan);
    SET @MaDonHangMoi = SCOPE_IDENTITY();

    INSERT ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, AnhDaiDien, Gia, DangGiuCho)
    SELECT @MaDonHangMoi, sp.MaSanPham, sp.Ten, a.DuongDan, sp.Gia, 1
    FROM SanPham sp
    JOIN @DanhSachSanPham d ON d.Id = sp.MaSanPham
    LEFT JOIN AnhSanPham a  ON a.MaSanPham = sp.MaSanPham AND a.LaAnhChinh = 1;

    UPDATE sp
    SET TrangThai = 'RESERVED', NgayCapNhat = SYSUTCDATETIME()
    FROM SanPham sp
    JOIN @DanhSachSanPham d ON d.Id = sp.MaSanPham
    WHERE sp.TrangThai = 'ACTIVE';

    INSERT LichSuTrangThaiSanPham (MaSanPham, TrangThaiCu, TrangThaiMoi, LoaiNguoiThucHien, NguoiThucHienId, LyDo)
    SELECT d.Id, 'ACTIVE', 'RESERVED', 'BUYER', @MaNguoiDung, CONCAT(N'Có đơn hàng ', @MaDon)
    FROM @DanhSachSanPham d;

    INSERT LichSuTrangThaiDonHang (MaDonHang, TrangThaiCu, TrangThaiMoi, LoaiNguoiThucHien, NguoiThucHienId, LyDo)
    VALUES (@MaDonHangMoi, NULL, 'PENDING', 'BUYER', @MaNguoiDung, N'Người mua tạo đơn hàng.');

    INSERT ThongBao (MaNguoiDung, Loai, TieuDe, NoiDung, MaThamChieu)
    VALUES (@ChuShopId, 'ORDER', N'Bạn có đơn hàng mới', CONCAT(N'Bạn vừa nhận được đơn hàng ', @MaDon, N'.'), @MaDonHangMoi);

    DELETE FROM GioHang
    WHERE MaNguoiDung = @MaNguoiDung
      AND MaSanPham IN (SELECT Id FROM @DanhSachSanPham);

    IF @TuTaoTran = 1 COMMIT TRANSACTION;
END
GO

-- Mua ngay 1 sản phẩm
CREATE OR ALTER PROCEDURE sp_TaoDonHang
    @MaNguoiDung         int,
    @MaSanPham           int,
    @TenNguoiNhan        nvarchar(100),
    @SoDienThoaiNhan     varchar(15),
    @DiaChiNhan          nvarchar(500),
    @PhiVanChuyen        decimal(18,2) = 0,
    @PhuongThucThanhToan varchar(20)   = 'COD',
    @MaDonHangMoi        int OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @MaCuaHang int, @DanhSach DanhSachId;

    SELECT @MaCuaHang = MaCuaHang FROM SanPham WHERE MaSanPham = @MaSanPham AND DaXoa = 0;
    IF @MaCuaHang IS NULL
        THROW 51104, N'Sản phẩm không tồn tại.', 1;

    INSERT @DanhSach (Id) VALUES (@MaSanPham);

    EXEC sp_TaoDonHangTheoShop
         @MaNguoiDung = @MaNguoiDung, @MaCuaHang = @MaCuaHang, @DanhSachSanPham = @DanhSach,
         @TenNguoiNhan = @TenNguoiNhan, @SoDienThoaiNhan = @SoDienThoaiNhan, @DiaChiNhan = @DiaChiNhan,
         @PhiVanChuyen = @PhiVanChuyen, @PhuongThucThanhToan = @PhuongThucThanhToan,
         @MaDonHangMoi = @MaDonHangMoi OUTPUT;
END
GO

-- Đặt hàng từ giỏ
CREATE OR ALTER PROCEDURE sp_DatHangTuGio
    @MaNguoiDung         int,
    @TenNguoiNhan        nvarchar(100),
    @SoDienThoaiNhan     varchar(15),
    @DiaChiNhan          nvarchar(500),
    @PhiVanChuyenMoiDon  decimal(18,2) = 0,
    @PhuongThucThanhToan varchar(20)   = 'COD'
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @DonDaTao TABLE (MaDonHang int PRIMARY KEY);
    DECLARE @MaCuaHang int, @MaDonHangMoi int, @DanhSach DanhSachId;

    IF NOT EXISTS (SELECT 1 FROM GioHang WHERE MaNguoiDung = @MaNguoiDung AND DaChon = 1)
        THROW 51120, N'Chưa chọn sản phẩm nào trong giỏ hàng.', 1;

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE cur_Shop CURSOR LOCAL FAST_FORWARD FOR
            SELECT DISTINCT sp.MaCuaHang
            FROM GioHang gh
            JOIN SanPham sp ON sp.MaSanPham = gh.MaSanPham
            WHERE gh.MaNguoiDung = @MaNguoiDung AND gh.DaChon = 1;

        OPEN cur_Shop;
        FETCH NEXT FROM cur_Shop INTO @MaCuaHang;

        WHILE @@FETCH_STATUS = 0
        BEGIN
            DELETE FROM @DanhSach;

            INSERT @DanhSach (Id)
            SELECT gh.MaSanPham
            FROM GioHang gh
            JOIN SanPham sp ON sp.MaSanPham = gh.MaSanPham
            WHERE gh.MaNguoiDung = @MaNguoiDung AND gh.DaChon = 1 AND sp.MaCuaHang = @MaCuaHang;

            EXEC sp_TaoDonHangTheoShop
                 @MaNguoiDung = @MaNguoiDung, @MaCuaHang = @MaCuaHang, @DanhSachSanPham = @DanhSach,
                 @TenNguoiNhan = @TenNguoiNhan, @SoDienThoaiNhan = @SoDienThoaiNhan, @DiaChiNhan = @DiaChiNhan,
                 @PhiVanChuyen = @PhiVanChuyenMoiDon, @PhuongThucThanhToan = @PhuongThucThanhToan,
                 @MaDonHangMoi = @MaDonHangMoi OUTPUT;

            INSERT @DonDaTao (MaDonHang) VALUES (@MaDonHangMoi);

            FETCH NEXT FROM cur_Shop INTO @MaCuaHang;
        END

        CLOSE cur_Shop;
        DEALLOCATE cur_Shop;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;

    SELECT dh.MaDonHang, dh.MaDon, dh.MaCuaHang, dh.TienHang, dh.PhiVanChuyen, dh.TongTien
    FROM @DonDaTao x
    JOIN DonHang dh ON dh.MaDonHang = x.MaDonHang
    ORDER BY dh.MaDonHang;
END
GO

-- Cập nhật trạng thái đơn hàng (Đã sửa gán nhầm @ChuShopId = ch.MaNguoiDung)
CREATE OR ALTER PROCEDURE sp_CapNhatTrangThaiDon
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

        IF @TrangThaiMoi = 'COMPLETED' AND @Loai <> 'BUYER'
            THROW 51206, N'Chỉ người mua mới được xác nhận hoàn tất.', 1;

        IF @TrangThaiMoi = 'CANCELLED' AND LEN(LTRIM(RTRIM(ISNULL(@LyDo, N'')))) = 0
            THROW 51207, N'Hủy đơn bắt buộc phải có lý do.', 1;

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
                                      THEN 'PAID' ELSE TrangThaiThanhToan END
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
        END

        INSERT ThongBao (MaNguoiDung, Loai, TieuDe, NoiDung, MaThamChieu)
        SELECT r.Id, 'ORDER', N'Cập nhật đơn hàng',
               CONCAT(N'Đơn ', @MaDon, N' chuyển sang trạng thái ', @TrangThaiMoi, N'.'), @MaDonHang
        FROM (VALUES (@MaNguoiMua), (@ChuShopId)) r (Id)
        WHERE r.Id <> @MaNguoiDung;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;

    SELECT @MaDonHang AS MaDonHang, @TrangThaiCu AS TrangThaiCu, @TrangThaiMoi AS TrangThaiMoi;
END
GO

PRINT N'Đã chạy xong file Stored Procedures chuẩn hóa!';
GO