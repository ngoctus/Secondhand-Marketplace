-- =============================================================================
-- database/22_hinh_thuc_nhan.sql
-- Module Giao dịch: Thêm hình thức nhận hàng (Giao hàng tận nơi hoặc Gặp trực tiếp)
-- Phụ trách: Người D · Tuân thủ tài liệu Kế hoạch hoàn thiện Re:Market (Mục 4.3 & 5.4)
-- =============================================================================

-- 1. Thêm cột HinhThucNhan vào bảng DonHang (nếu chưa có)
IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID('DonHang') AND name = 'HinhThucNhan'
)
BEGIN
    ALTER TABLE DonHang 
    ADD HinhThucNhan varchar(20) NOT NULL 
        CONSTRAINT DF_DonHang_HinhThucNhan DEFAULT 'GIAO_HANG';

    ALTER TABLE DonHang 
    ADD CONSTRAINT CK_DonHang_HinhThucNhan 
        CHECK (HinhThucNhan IN ('GIAO_HANG', 'GAP_TRUC_TIEP'));

    PRINT N'Đã thêm cột HinhThucNhan vào bảng DonHang.';
END
ELSE
BEGIN
    PRINT N'Cột HinhThucNhan đã tồn tại trong bảng DonHang.';
END
GO

-- 2. Cập nhật Stored Procedure sp_TaoDonHangTheoShop
CREATE OR ALTER PROCEDURE sp_TaoDonHangTheoShop
    @MaNguoiDung         int,
    @MaCuaHang           int,
    @DanhSachSanPham     dbo.DanhSachId READONLY,
    @TenNguoiNhan        nvarchar(100),
    @SoDienThoaiNhan     varchar(15),
    @DiaChiNhan          nvarchar(500),
    @PhiVanChuyen        decimal(18,2) = 0,
    @PhuongThucThanhToan varchar(20)   = 'COD',
    @GiaTuyChon          decimal(18,2) = NULL,   -- giá chốt từ đấu giá (NULL = lấy SanPham.Gia)
    @HinhThucNhan        varchar(20)   = 'GIAO_HANG',
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
    IF EXISTS (SELECT 1 FROM NguoiDung WHERE MaNguoiDung = @ChuShopId AND TrangThai <> 'ACTIVE')
        THROW 51108, N'Chủ cửa hàng đang bị khóa tài khoản, không thể tạo đơn mới.', 1;
    IF @ChuShopId = @MaNguoiDung
        THROW 51105, N'Không thể mua sản phẩm của chính shop mình.', 1;

    IF @PhuongThucThanhToan NOT IN ('COD','BANK_TRANSFER','ONLINE')
        THROW 51110, N'Phương thức thanh toán không hợp lệ.', 1;
    IF @HinhThucNhan NOT IN ('GIAO_HANG','GAP_TRUC_TIEP')
        THROW 51115, N'Hình thức nhận hàng không hợp lệ (chỉ nhận GIAO_HANG hoặc GAP_TRUC_TIEP).', 1;
    IF @PhiVanChuyen IS NULL OR @PhiVanChuyen < 0
        THROW 51109, N'Phí vận chuyển không hợp lệ.', 1;
    IF LEN(LTRIM(RTRIM(ISNULL(@TenNguoiNhan, N'')))) = 0
        THROW 51111, N'Chưa nhập tên người nhận.', 1;
    IF @SoDienThoaiNhan IS NULL OR @SoDienThoaiNhan LIKE '%[^0-9]%' OR LEN(@SoDienThoaiNhan) NOT BETWEEN 9 AND 15
        THROW 51112, N'Số điện thoại người nhận phải gồm 9 đến 15 chữ số.', 1;
    IF LEN(LTRIM(RTRIM(ISNULL(@DiaChiNhan, N'')))) = 0
        THROW 51113, N'Chưa nhập địa chỉ giao hàng hoặc điểm hẹn.', 1;

    SELECT @SoSanPham = COUNT(*) FROM @DanhSachSanPham;
    IF @SoSanPham = 0
        THROW 51114, N'Đơn hàng phải có ít nhất 1 sản phẩm.', 1;

    SELECT @SoHopLe = COUNT(*)
    FROM SanPham sp WITH (UPDLOCK, HOLDLOCK)
    JOIN @DanhSachSanPham d ON d.Id = sp.MaSanPham
    WHERE sp.MaCuaHang = @MaCuaHang AND sp.TrangThai = 'ACTIVE' AND sp.DaXoa = 0;

    IF @SoHopLe <> @SoSanPham
        THROW 51106, N'Có sản phẩm không còn bán hoặc không thuộc cửa hàng này.', 1;

    SELECT @TienHang = SUM(COALESCE(@GiaTuyChon, sp.Gia))
    FROM SanPham sp
    JOIN @DanhSachSanPham d ON d.Id = sp.MaSanPham;

    SET @SoThuTu = NEXT VALUE FOR sq_MaDonHang;
    SET @MaDon = 'DH' + CONVERT(varchar(8), SYSUTCDATETIME(), 112) + '-'
               + RIGHT('000000' + CAST(@SoThuTu AS varchar(10)), 6);

    INSERT DonHang (MaDon, MaNguoiDung, MaCuaHang, TenNguoiNhan, SoDienThoaiNhan, DiaChiNhan,
                    TienHang, PhiVanChuyen, TongTien, PhuongThucThanhToan, HinhThucNhan)
    VALUES (@MaDon, @MaNguoiDung, @MaCuaHang, LTRIM(RTRIM(@TenNguoiNhan)), @SoDienThoaiNhan, LTRIM(RTRIM(@DiaChiNhan)),
            @TienHang, @PhiVanChuyen, @TienHang + @PhiVanChuyen, @PhuongThucThanhToan, @HinhThucNhan);
    SET @MaDonHangMoi = SCOPE_IDENTITY();

    INSERT ThanhToan (MaDonHang, MaGiaoDich, PhuongThucThanhToan, SoTien, TrangThaiThanhToan, ThoiGianThanhToan)
    VALUES (@MaDonHangMoi, 'TXN_' + @MaDon, @PhuongThucThanhToan, @TienHang + @PhiVanChuyen,
            CASE WHEN @PhuongThucThanhToan = 'ONLINE' THEN 'SUCCESS' ELSE 'PENDING' END,
            CASE WHEN @PhuongThucThanhToan = 'ONLINE' THEN SYSUTCDATETIME() ELSE NULL END);

    IF @PhuongThucThanhToan = 'ONLINE'
        UPDATE DonHang SET TrangThaiThanhToan = 'PAID' WHERE MaDonHang = @MaDonHangMoi;

    INSERT ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, AnhDaiDien, Gia, DangGiuCho)
    SELECT @MaDonHangMoi, sp.MaSanPham, sp.Ten, a.DuongDan, COALESCE(@GiaTuyChon, sp.Gia), 1
    FROM SanPham sp
    JOIN @DanhSachSanPham d ON d.Id = sp.MaSanPham
    OUTER APPLY (
        SELECT TOP 1 DuongDan
        FROM AnhSanPham
        WHERE MaSanPham = sp.MaSanPham
        ORDER BY LaAnhChinh DESC, MaAnh ASC
    ) a;

    UPDATE sp
    SET sp.TrangThai = 'RESERVED',
        sp.NgayCapNhat = SYSUTCDATETIME()
    FROM SanPham sp
    JOIN @DanhSachSanPham d ON d.Id = sp.MaSanPham;

    INSERT LichSuTrangThaiDonHang (MaDonHang, TrangThaiCu, TrangThaiMoi, LoaiNguoiThucHien, NguoiThucHienId, LyDo)
    VALUES (@MaDonHangMoi, NULL, 'PENDING', 'BUYER', @MaNguoiDung, 
            CASE WHEN @HinhThucNhan = 'GAP_TRUC_TIEP' 
                 THEN N'Người mua tạo đơn hàng (Hẹn gặp trực tiếp).' 
                 ELSE N'Người mua tạo đơn hàng (Giao tận nơi).' END);

    INSERT ThongBao (MaNguoiDung, Loai, TieuDe, NoiDung, LienKetId)
    VALUES (@ChuShopId, 'ORDER', N'Bạn có đơn hàng mới', 
            CONCAT(N'Bạn vừa nhận được đơn hàng ', @MaDon, 
                   CASE WHEN @HinhThucNhan = 'GAP_TRUC_TIEP' THEN N' (Hẹn gặp trực tiếp).' ELSE N'.' END), 
            @MaDonHangMoi);

    IF @TuTaoTran = 1
        COMMIT TRANSACTION;
END;
GO

-- 3. Cập nhật Stored Procedure sp_TaoDonHang (dùng cho Mua Ngay)
CREATE OR ALTER PROCEDURE sp_TaoDonHang
    @MaNguoiDung         int,
    @MaSanPham           int,
    @TenNguoiNhan        nvarchar(100),
    @SoDienThoaiNhan     varchar(15),
    @DiaChiNhan          nvarchar(500),
    @PhiVanChuyen        decimal(18,2) = 0,
    @PhuongThucThanhToan varchar(20)   = 'COD',
    @HinhThucNhan        varchar(20)   = 'GIAO_HANG',
    @MaDonHangMoi        int OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @MaCuaHang int, @DanhSach dbo.DanhSachId;

    SELECT @MaCuaHang = MaCuaHang FROM SanPham WHERE MaSanPham = @MaSanPham;
    IF @MaCuaHang IS NULL
        THROW 51102, N'Sản phẩm không tồn tại.', 1;

    INSERT @DanhSach (Id) VALUES (@MaSanPham);

    EXEC sp_TaoDonHangTheoShop
         @MaNguoiDung = @MaNguoiDung,
         @MaCuaHang = @MaCuaHang,
         @DanhSachSanPham = @DanhSach,
         @TenNguoiNhan = @TenNguoiNhan,
         @SoDienThoaiNhan = @SoDienThoaiNhan,
         @DiaChiNhan = @DiaChiNhan,
         @PhiVanChuyen = @PhiVanChuyen,
         @PhuongThucThanhToan = @PhuongThucThanhToan,
         @HinhThucNhan = @HinhThucNhan,
         @MaDonHangMoi = @MaDonHangMoi OUTPUT;
END;
GO

-- 4. Cập nhật Stored Procedure sp_DatHangTuGio
CREATE OR ALTER PROCEDURE sp_DatHangTuGio
    @MaNguoiDung          int,
    @TenNguoiNhan         nvarchar(100),
    @SoDienThoaiNhan      varchar(15),
    @DiaChiNhan           nvarchar(500),
    @PhiVanChuyenMoiDon   decimal(18,2) = 0,
    @PhuongThucThanhToan  varchar(20)   = 'COD',
    @HinhThucNhan         varchar(20)   = 'GIAO_HANG'
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @DonDaTao TABLE (MaDonHang int PRIMARY KEY);
    DECLARE @MaCuaHang int, @MaDonHangMoi int, @DanhSach dbo.DanhSachId;

    IF NOT EXISTS (
        SELECT 1
        FROM GioHang gh
        JOIN SanPham sp ON sp.MaSanPham = gh.MaSanPham
        JOIN CuaHang ch ON ch.MaCuaHang = sp.MaCuaHang
        WHERE gh.MaNguoiDung = @MaNguoiDung
          AND gh.DaChon = 1
          AND sp.TrangThai = 'ACTIVE'
          AND sp.DaXoa = 0
          AND ch.TrangThai = 'ACTIVE'
    )
        THROW 51103, N'Không có sản phẩm hợp lệ nào được chọn trong giỏ hàng.', 1;

    BEGIN TRANSACTION;
    BEGIN TRY
        DECLARE cur CURSOR LOCAL FAST_FORWARD FOR
            SELECT DISTINCT sp.MaCuaHang
            FROM GioHang gh
            JOIN SanPham sp ON sp.MaSanPham = gh.MaSanPham
            JOIN CuaHang ch ON ch.MaCuaHang = sp.MaCuaHang
            WHERE gh.MaNguoiDung = @MaNguoiDung
              AND gh.DaChon = 1
              AND sp.TrangThai = 'ACTIVE'
              AND sp.DaXoa = 0
              AND ch.TrangThai = 'ACTIVE';

        OPEN cur;
        FETCH NEXT FROM cur INTO @MaCuaHang;

        WHILE @@FETCH_STATUS = 0
        BEGIN
            DELETE FROM @DanhSach;

            INSERT @DanhSach (Id)
            SELECT gh.MaSanPham
            FROM GioHang gh
            JOIN SanPham sp ON sp.MaSanPham = gh.MaSanPham
            WHERE gh.MaNguoiDung = @MaNguoiDung
              AND gh.DaChon = 1
              AND sp.MaCuaHang = @MaCuaHang
              AND sp.TrangThai = 'ACTIVE'
              AND sp.DaXoa = 0;

            EXEC sp_TaoDonHangTheoShop
                 @MaNguoiDung = @MaNguoiDung,
                 @MaCuaHang = @MaCuaHang,
                 @DanhSachSanPham = @DanhSach,
                 @TenNguoiNhan = @TenNguoiNhan,
                 @SoDienThoaiNhan = @SoDienThoaiNhan,
                 @DiaChiNhan = @DiaChiNhan,
                 @PhiVanChuyen = @PhiVanChuyenMoiDon,
                 @PhuongThucThanhToan = @PhuongThucThanhToan,
                 @HinhThucNhan = @HinhThucNhan,
                 @MaDonHangMoi = @MaDonHangMoi OUTPUT;

            INSERT @DonDaTao (MaDonHang) VALUES (@MaDonHangMoi);

            DELETE gh
            FROM GioHang gh
            JOIN @DanhSach d ON d.Id = gh.MaSanPham
            WHERE gh.MaNguoiDung = @MaNguoiDung;

            FETCH NEXT FROM cur INTO @MaCuaHang;
        END

        CLOSE cur;
        DEALLOCATE cur;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF CURSOR_STATUS('local', 'cur') >= -1
        BEGIN
            CLOSE cur;
            DEALLOCATE cur;
        END
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        THROW;
    END CATCH

    SELECT dh.MaDonHang, dh.MaDon, dh.MaCuaHang, dh.TienHang, dh.PhiVanChuyen, dh.TongTien, dh.HinhThucNhan
    FROM @DonDaTao x
    JOIN DonHang dh ON dh.MaDonHang = x.MaDonHang
    ORDER BY dh.MaDonHang;
END;
GO

PRINT N'Đã hoàn tất cấu hình hình thức nhận hàng (22_hinh_thuc_nhan.sql)!';
