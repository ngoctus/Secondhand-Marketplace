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

-- ĐẶT HÀNG: giỏ hàng (nhiều sản phẩm đã chọn) hoặc Mua ngay (1 sản phẩm)
-- - Giữ chỗ bằng UPDATE ... WHERE TrangThai='ACTIVE' (chống 2 người mua trùng)
-- - Tất cả hoặc không: thiếu 1 sản phẩm là rollback toàn bộ
-- - Tách đơn theo shop, lưu snapshot tên/giá/ảnh
CREATE OR ALTER PROCEDURE sp_TaoDonHang
    @NguoiMuaId          int,
    @DanhSachSanPham     DanhSachId READONLY,
    @TenNguoiNhan        nvarchar(100),
    @SoDienThoaiNhan     varchar(15),
    @DiaChiNhan          nvarchar(500),
    @GhiChu              nvarchar(500) = NULL,
    @PhuongThucThanhToan varchar(20)  = 'COD',
    @XoaKhoiGio          bit           = 1          -- Mua ngay: truyền 0 để không đụng giỏ
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NOT EXISTS (SELECT 1 FROM NguoiDung WHERE MaNguoiDung = @NguoiMuaId AND VaiTro = 'USER' AND TrangThai = 'ACTIVE')
        THROW 50040, N'Tài khoản không được phép đặt hàng.', 1;
    IF NOT EXISTS (SELECT 1 FROM @DanhSachSanPham)
        THROW 50041, N'Chưa chọn sản phẩm nào để đặt hàng.', 1;

    DECLARE @DaGiu TABLE (MaSanPham int PRIMARY KEY, MaCuaHang int NOT NULL, Ten nvarchar(200) NOT NULL, Gia decimal(18,2) NOT NULL);
    DECLARE @KetQua TABLE (DonHangId int, MaDon varchar(30), MaCuaHang int, TongTien decimal(18,2));
    DECLARE @Shops TABLE (MaCuaHang int PRIMARY KEY);
    DECLARE @MaCuaHang int, @DonHangId int, @So int, @MaDon varchar(30);

    BEGIN TRANSACTION;

    -- 1. Giữ chỗ nguyên tử: chỉ sản phẩm còn ACTIVE, shop không khóa, không phải của chính mình
    UPDATE sp
    SET TrangThai = 'RESERVED'
    OUTPUT inserted.MaSanPham, inserted.MaCuaHang, inserted.Ten, inserted.Gia INTO @DaGiu (MaSanPham, MaCuaHang, Ten, Gia)
    FROM SanPham sp
    JOIN @DanhSachSanPham ds ON ds.Id = sp.MaSanPham
    JOIN CuaHang ch      ON ch.MaCuaHang = sp.MaCuaHang
    WHERE sp.TrangThai = 'ACTIVE' AND sp.DaXoa = 0
      AND ch.TrangThai = 'ACTIVE' AND ch.MaNguoiDung <> @NguoiMuaId;

    IF (SELECT COUNT(*) FROM @DaGiu) <> (SELECT COUNT(*) FROM @DanhSachSanPham)
        THROW 50042, N'Có sản phẩm vừa được người khác mua hoặc không còn bán. Vui lòng làm mới và chọn lại.', 1;

    -- 2. Mỗi shop một đơn
    INSERT @Shops (MaCuaHang) SELECT DISTINCT MaCuaHang FROM @DaGiu;

    WHILE EXISTS (SELECT 1 FROM @Shops)
    BEGIN
        SELECT TOP (1) @MaCuaHang = MaCuaHang FROM @Shops ORDER BY MaCuaHang;
        DELETE FROM @Shops WHERE MaCuaHang = @MaCuaHang;

        SET @So = NEXT VALUE FOR sq_MaDonHang;
        SET @MaDon = CONCAT('DH', CONVERT(char(8), SYSUTCDATETIME(), 112), '-', RIGHT(CONCAT('000000', @So), 6));

        INSERT DonHang (MaDon, MaNguoiDung, MaCuaHang, TenNguoiNhan, SoDienThoaiNhan, DiaChiNhan, GhiChu,
                        PhuongThucThanhToan, TongTien)
        SELECT @MaDon, @NguoiMuaId, @MaCuaHang, @TenNguoiNhan, @SoDienThoaiNhan, @DiaChiNhan, @GhiChu,
               @PhuongThucThanhToan, SUM(Gia)
        FROM @DaGiu WHERE MaCuaHang = @MaCuaHang;
        SET @DonHangId = SCOPE_IDENTITY();

        INSERT ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, Gia, AnhDaiDien, DangGiuCho)
        SELECT @DonHangId, g.MaSanPham, g.Ten, g.Gia, a.DuongDan, 1
        FROM @DaGiu g
        LEFT JOIN AnhSanPham a ON a.MaSanPham = g.MaSanPham AND a.LaAnhChinh = 1
        WHERE g.MaCuaHang = @MaCuaHang;

        INSERT LichSuTrangThaiDonHang (MaDonHang, TrangThaiCu, TrangThaiMoi, LoaiNguoiThucHien, NguoiThucHienId, LyDo)
        VALUES (@DonHangId, NULL, 'PENDING', 'BUYER', @NguoiMuaId, N'Tạo đơn hàng');

        INSERT LichSuTrangThaiSanPham (MaSanPham, TrangThaiCu, TrangThaiMoi, LoaiNguoiThucHien, NguoiThucHienId, LyDo)
        SELECT g.MaSanPham, 'ACTIVE', 'RESERVED', 'BUYER', @NguoiMuaId, CONCAT(N'Đặt đơn ', @MaDon)
        FROM @DaGiu g WHERE g.MaCuaHang = @MaCuaHang;

        INSERT @KetQua (DonHangId, MaDon, MaCuaHang, TongTien)
        SELECT @DonHangId, @MaDon, @MaCuaHang, SUM(Gia) FROM @DaGiu WHERE MaCuaHang = @MaCuaHang;
    END

    -- 3. Xóa các món đã đặt khỏi giỏ
    IF @XoaKhoiGio = 1
        DELETE gh
        FROM GioHang gh
        JOIN @DaGiu d ON d.MaSanPham = gh.MaSanPham
        WHERE gh.MaNguoiDung = @NguoiMuaId;

    COMMIT TRANSACTION;

    SELECT DonHangId, MaDon, MaCuaHang AS CuaHangId, TongTien FROM @KetQua ORDER BY DonHangId;
END
GO

-- TRUNG TÂM chuyển trạng thái ĐƠN HÀNG (OrderStateMachine ở mức DB)
-- Phân quyền theo vai trò + kiểm tra hợp lệ + liên động trạng thái sản phẩm
--   CANCELLED -> sản phẩm về ACTIVE | COMPLETED -> sản phẩm SOLD
CREATE OR ALTER PROCEDURE sp_CapNhatTrangThaiDonHang
    @DonHangId         int,
    @TrangThaiMoi      varchar(15),
    @LoaiNguoiThucHien varchar(10),                -- BUYER | SELLER | SYSTEM
    @NguoiThucHienId   int           = NULL,
    @LyDo              nvarchar(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @TrangThaiCu varchar(15), @NguoiMuaId int, @ChuShopId int, @MaDon varchar(30),
            @Now datetime2(3) = SYSUTCDATETIME(), @Msg nvarchar(400), @TuTaoTran bit = 0;

    IF @LoaiNguoiThucHien = 'SYSTEM' SET @NguoiThucHienId = NULL;

    IF @@TRANCOUNT = 0
    BEGIN
        BEGIN TRANSACTION;
        SET @TuTaoTran = 1;
    END

    SELECT @TrangThaiCu = dh.TrangThai, @NguoiMuaId = dh.MaNguoiDung, @ChuShopId = ch.MaNguoiDung, @MaDon = dh.MaDon
    FROM DonHang dh WITH (UPDLOCK, ROWLOCK)
    JOIN CuaHang ch ON ch.MaCuaHang = dh.MaCuaHang
    WHERE dh.MaDonHang = @DonHangId;

    -- Không tồn tại hoặc không thuộc về người thao tác -> báo không tồn tại
    IF @TrangThaiCu IS NULL
       OR @LoaiNguoiThucHien NOT IN ('BUYER','SELLER','SYSTEM')
       OR (@LoaiNguoiThucHien = 'BUYER'  AND ISNULL(@NguoiThucHienId, -1) <> @NguoiMuaId)
       OR (@LoaiNguoiThucHien = 'SELLER' AND ISNULL(@NguoiThucHienId, -1) <> @ChuShopId)
        THROW 50050, N'Đơn hàng không tồn tại.', 1;

    -- Mỗi vai trò chỉ được làm một số thao tác
    IF NOT (
           (@LoaiNguoiThucHien = 'BUYER'  AND ((@TrangThaiCu = 'PENDING'   AND @TrangThaiMoi = 'CANCELLED')
                                            OR (@TrangThaiCu = 'DELIVERED' AND @TrangThaiMoi = 'COMPLETED')))
        OR (@LoaiNguoiThucHien = 'SELLER' AND @TrangThaiMoi IN ('CONFIRMED','SHIPPING','DELIVERED','CANCELLED'))
        OR (@LoaiNguoiThucHien = 'SYSTEM' AND ((@TrangThaiCu = 'PENDING'   AND @TrangThaiMoi = 'CANCELLED')
                                            OR (@TrangThaiCu = 'DELIVERED' AND @TrangThaiMoi = 'COMPLETED')))
    )
        THROW 50051, N'Bạn không có quyền thực hiện thao tác này với đơn hàng.', 1;

    IF fn_ChuyenTrangThaiDonHangHopLe(@TrangThaiCu, @TrangThaiMoi) = 0
    BEGIN
        SET @Msg = CONCAT(N'Không thể chuyển đơn hàng từ ', @TrangThaiCu, N' sang ', @TrangThaiMoi, N'.');
        THROW 50052, @Msg, 1;
    END

    IF @TrangThaiMoi = 'CANCELLED' AND @LoaiNguoiThucHien = 'SELLER'
       AND LEN(LTRIM(RTRIM(ISNULL(@LyDo, N'')))) = 0
        THROW 50053, N'Người bán từ chối/hủy đơn bắt buộc phải nhập lý do.', 1;

    IF @TrangThaiMoi = 'CANCELLED' AND @LoaiNguoiThucHien = 'SYSTEM' AND @LyDo IS NULL
        SET @LyDo = N'Hệ thống tự hủy do quá hạn xác nhận';

    UPDATE DonHang
    SET TrangThai          = @TrangThaiMoi,
        ThoiGianXacNhan    = CASE WHEN @TrangThaiMoi = 'CONFIRMED' THEN @Now ELSE ThoiGianXacNhan END,
        ThoiGianBatDauGiao = CASE WHEN @TrangThaiMoi = 'SHIPPING'  THEN @Now ELSE ThoiGianBatDauGiao END,
        ThoiGianDaGiao     = CASE WHEN @TrangThaiMoi = 'DELIVERED' THEN @Now ELSE ThoiGianDaGiao END,
        ThoiGianHoanTat    = CASE WHEN @TrangThaiMoi = 'COMPLETED' THEN @Now ELSE ThoiGianHoanTat END,
        NguoiHuy           = CASE WHEN @TrangThaiMoi = 'CANCELLED' THEN @LoaiNguoiThucHien ELSE NguoiHuy END,
        LyDoHuy            = CASE WHEN @TrangThaiMoi = 'CANCELLED' THEN @LyDo ELSE LyDoHuy END,
        ThoiGianHuy        = CASE WHEN @TrangThaiMoi = 'CANCELLED' THEN @Now ELSE ThoiGianHuy END
    WHERE MaDonHang = @DonHangId AND TrangThai = @TrangThaiCu;

    IF @@ROWCOUNT = 0
        THROW 50054, N'Trạng thái đơn hàng vừa bị thay đổi, vui lòng tải lại.', 1;

    INSERT LichSuTrangThaiDonHang (MaDonHang, TrangThaiCu, TrangThaiMoi, LoaiNguoiThucHien, NguoiThucHienId, LyDo, ThoiGian)
    VALUES (@DonHangId, @TrangThaiCu, @TrangThaiMoi, @LoaiNguoiThucHien, @NguoiThucHienId, @LyDo, @Now);

    -- Liên động sản phẩm
    IF @TrangThaiMoi IN ('CANCELLED','COMPLETED')
    BEGIN
        DECLARE @SanPhamMoi varchar(20) = CASE WHEN @TrangThaiMoi = 'CANCELLED' THEN 'ACTIVE' ELSE 'SOLD' END;
        DECLARE @DoiTrangThai TABLE (MaSanPham int PRIMARY KEY);

        UPDATE ChiTietDonHang SET DangGiuCho = 0 WHERE MaDonHang = @DonHangId;

        UPDATE sp
        SET TrangThai = @SanPhamMoi
        OUTPUT inserted.MaSanPham INTO @DoiTrangThai (MaSanPham)
        FROM SanPham sp
        JOIN ChiTietDonHang ct ON ct.MaSanPham = sp.MaSanPham AND ct.MaDonHang = @DonHangId
        WHERE sp.TrangThai = 'RESERVED';

        INSERT LichSuTrangThaiSanPham (MaSanPham, TrangThaiCu, TrangThaiMoi, LoaiNguoiThucHien, NguoiThucHienId, LyDo, ThoiGian)
        SELECT MaSanPham, 'RESERVED', @SanPhamMoi, @LoaiNguoiThucHien, @NguoiThucHienId,
               CONCAT(N'Đơn ', @MaDon, N' chuyển sang ', @TrangThaiMoi), @Now
        FROM @DoiTrangThai;
    END

    IF @TuTaoTran = 1 COMMIT TRANSACTION;
END
GO

-- Job nền (có thể gọi từ BackgroundService hoặc SQL Agent)
CREATE OR ALTER PROCEDURE sp_TuDongHuyDonQuaHan
    @SoNgay int = 3                        -- đơn PENDING quá N ngày chưa được xác nhận
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Ds TABLE (Id int PRIMARY KEY);
    DECLARE @Id int, @DaXuLy int = 0, @Loi int = 0;

    INSERT @Ds (Id)
    SELECT MaDonHang FROM DonHang
    WHERE TrangThai = 'PENDING' AND NgayTao < DATEADD(DAY, -@SoNgay, SYSUTCDATETIME());

    WHILE EXISTS (SELECT 1 FROM @Ds)
    BEGIN
        SELECT TOP (1) @Id = Id FROM @Ds ORDER BY Id;
        DELETE FROM @Ds WHERE Id = @Id;
        BEGIN TRY
            EXEC sp_CapNhatTrangThaiDonHang @DonHangId = @Id, @TrangThaiMoi = 'CANCELLED',
                                             @LoaiNguoiThucHien = 'SYSTEM';
            SET @DaXuLy += 1;
        END TRY
        BEGIN CATCH
            IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
            SET @Loi += 1;
        END CATCH
    END

    SELECT @DaXuLy AS SoDonDaHuy, @Loi AS SoDonLoi;
END
GO

CREATE OR ALTER PROCEDURE sp_TuDongHoanTatDonQuaHan
    @SoNgay int = 7                        -- đơn DELIVERED quá N ngày người mua chưa bấm "Đã nhận hàng"
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Ds TABLE (Id int PRIMARY KEY);
    DECLARE @Id int, @DaXuLy int = 0, @Loi int = 0;

    INSERT @Ds (Id)
    SELECT MaDonHang FROM DonHang
    WHERE TrangThai = 'DELIVERED' AND ThoiGianDaGiao < DATEADD(DAY, -@SoNgay, SYSUTCDATETIME());

    WHILE EXISTS (SELECT 1 FROM @Ds)
    BEGIN
        SELECT TOP (1) @Id = Id FROM @Ds ORDER BY Id;
        DELETE FROM @Ds WHERE Id = @Id;
        BEGIN TRY
            EXEC sp_CapNhatTrangThaiDonHang @DonHangId = @Id, @TrangThaiMoi = 'COMPLETED',
                                             @LoaiNguoiThucHien = 'SYSTEM',
                                             @LyDo = N'Hệ thống tự hoàn tất do quá hạn xác nhận nhận hàng';
            SET @DaXuLy += 1;
        END TRY
        BEGIN CATCH
            IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
            SET @Loi += 1;
        END CATCH
    END

    SELECT @DaXuLy AS SoDonDaHoanTat, @Loi AS SoDonLoi;
END
GO

-- Tìm kiếm / lọc / sắp xếp / phân trang sản phẩm công khai
--   @TuKhoa         : không phân biệt hoa/thường, không dấu, mỗi từ đều phải khớp
--   @DanhSachDanhMuc : '1,3,5'
--   @SapXep         : MOI_NHAT | GIA_TANG | GIA_GIAM
CREATE OR ALTER PROCEDURE sp_TimKiemSanPham
    @TuKhoa          nvarchar(200) = NULL,
    @DanhSachDanhMuc  varchar(200)  = NULL,
    @TinhTrangId      int           = NULL,
    @GiaMin           decimal(18,2) = NULL,
    @GiaMax           decimal(18,2) = NULL,
    @SapXep           varchar(20)   = 'MOI_NHAT',
    @Trang            int           = 1,
    @KichThuoc        int           = 20
AS
BEGIN
    SET NOCOUNT ON;

    IF @GiaMin IS NOT NULL AND @GiaMax IS NOT NULL AND @GiaMin > @GiaMax
        THROW 50060, N'Giá tối thiểu không được lớn hơn giá tối đa.', 1;

    SET @Trang = CASE WHEN @Trang < 1 THEN 1 ELSE @Trang END;
    SET @KichThuoc = CASE WHEN @KichThuoc < 1 THEN 20 WHEN @KichThuoc > 100 THEN 100 ELSE @KichThuoc END;

    DECLARE @TuKhoaChuan nvarchar(200) = NULLIF(fn_ChuanHoaChuoi(@TuKhoa), N'');

    SELECT v.Id, v.Ten, v.Gia, v.NgayTao,
           v.MaDanhMuc AS DanhMucId, v.TenDanhMuc, v.MaTinhTrang AS TinhTrangId, v.TenTinhTrang,
           v.MaCuaHang AS CuaHangId, v.TenShop, v.AnhDaiDien,
           COUNT(*) OVER () AS TongSoDong
    FROM vw_SanPhamCongKhai v
    WHERE (@TuKhoaChuan IS NULL
           OR NOT EXISTS (SELECT 1 FROM STRING_SPLIT(@TuKhoaChuan, N' ') w
                          WHERE w.value <> N'' AND v.ChuoiTimKiem NOT LIKE N'%' + w.value + N'%'))
      AND (@DanhSachDanhMuc IS NULL
           OR EXISTS (SELECT 1 FROM STRING_SPLIT(@DanhSachDanhMuc, ',') c WHERE TRY_CAST(c.value AS int) = v.MaDanhMuc))
      AND (@TinhTrangId IS NULL OR v.MaTinhTrang = @TinhTrangId)
      AND (@GiaMin IS NULL OR v.Gia >= @GiaMin)
      AND (@GiaMax IS NULL OR v.Gia <= @GiaMax)
    ORDER BY
        CASE WHEN @SapXep = 'GIA_TANG' THEN v.Gia END ASC,
        CASE WHEN @SapXep = 'GIA_GIAM' THEN v.Gia END DESC,
        CASE WHEN @SapXep NOT IN ('GIA_TANG','GIA_GIAM') THEN v.NgayTao END DESC,
        v.Id DESC
    OFFSET (@Trang - 1) * @KichThuoc ROWS FETCH NEXT @KichThuoc ROWS ONLY
    OPTION (RECOMPILE);
END
GO