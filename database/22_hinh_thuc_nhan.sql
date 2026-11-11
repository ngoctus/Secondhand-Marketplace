-- Đảm bảo User-defined Table Type dbo.DanhSachId tồn tại nếu chạy script độc lập
IF TYPE_ID(N'dbo.DanhSachId') IS NULL
    EXEC(N'CREATE TYPE dbo.DanhSachId AS TABLE (Id int NOT NULL PRIMARY KEY);');
GO

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
    IF @HinhThucNhan = 'GAP_TRUC_TIEP'
        SET @PhiVanChuyen = 0;
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

    -- Đồng bộ tổng tiền bằng hàm dbo.fn_TinhTongTienDonHang theo đúng nguyên tắc Data-first
    UPDATE DonHang
    SET TongTien = dbo.fn_TinhTongTienDonHang(@MaDonHangMoi)
    WHERE MaDonHang = @MaDonHangMoi;

    UPDATE sp
    SET sp.TrangThai = 'RESERVED',
        sp.NgayCapNhat = SYSUTCDATETIME()
    FROM SanPham sp
    JOIN @DanhSachSanPham d ON d.Id = sp.MaSanPham;

    INSERT LichSuTrangThaiSanPham (MaSanPham, TrangThaiCu, TrangThaiMoi, LoaiNguoiThucHien, NguoiThucHienId, LyDo)
    SELECT d.Id, 'ACTIVE', 'RESERVED', 'BUYER', @MaNguoiDung, CONCAT(N'Có đơn hàng ', @MaDon)
    FROM @DanhSachSanPham d;

    INSERT LichSuTrangThaiDonHang (MaDonHang, TrangThaiCu, TrangThaiMoi, LoaiNguoiThucHien, NguoiThucHienId, LyDo)
    VALUES (@MaDonHangMoi, NULL, 'PENDING', 'BUYER', @MaNguoiDung, 
            CASE WHEN @HinhThucNhan = 'GAP_TRUC_TIEP' 
                 THEN N'Người mua tạo đơn hàng (Hẹn gặp trực tiếp).' 
                 ELSE N'Người mua tạo đơn hàng (Giao tận nơi).' END);

    INSERT ThongBao (MaNguoiDung, Loai, TieuDe, NoiDung, MaThamChieu)
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

    IF @HinhThucNhan NOT IN ('GIAO_HANG','GAP_TRUC_TIEP')
        THROW 51115, N'Hình thức nhận hàng không hợp lệ (chỉ nhận GIAO_HANG hoặc GAP_TRUC_TIEP).', 1;
    IF @HinhThucNhan = 'GAP_TRUC_TIEP'
        SET @PhiVanChuyenMoiDon = 0;

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
          AND ch.MaNguoiDung <> @MaNguoiDung
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
              AND ch.TrangThai = 'ACTIVE'
              AND ch.MaNguoiDung <> @MaNguoiDung;

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



-- 5. Cập nhật vw_DonHangTongQuan bao gồm hình thức nhận hàng (HinhThucNhan)
CREATE OR ALTER VIEW vw_DonHangTongQuan
AS
SELECT
    dh.MaDonHang AS Id,
    dh.MaDon,
    dh.MaNguoiDung AS NguoiMuaId,
    mua.HoTen AS TenNguoiMua,
    dh.MaCuaHang,
    ch.TenShop,
    ch.MaNguoiDung AS ChuShopId,
    dh.TrangThai,
    dh.PhuongThucThanhToan,
    dh.TrangThaiThanhToan,
    dh.HinhThucNhan,
    dh.TongTien,
    dh.NguoiHuy,
    dh.LyDoHuy,
    dh.NgayTao,
    (
        SELECT COUNT(*)
        FROM ChiTietDonHang ct
        WHERE ct.MaDonHang = dh.MaDonHang
    ) AS SoSanPham,
    (
        SELECT TOP (1) ct.TenSanPham
        FROM ChiTietDonHang ct
        WHERE ct.MaDonHang = dh.MaDonHang
        ORDER BY ct.MaChiTietDonHang
    ) AS TenSanPhamDau,
    (
        SELECT TOP (1) ct.AnhDaiDien
        FROM ChiTietDonHang ct
        WHERE ct.MaDonHang = dh.MaDonHang
        ORDER BY ct.MaChiTietDonHang
    ) AS AnhDau
FROM DonHang dh
JOIN NguoiDung mua
    ON mua.MaNguoiDung = dh.MaNguoiDung
JOIN CuaHang ch
    ON ch.MaCuaHang = dh.MaCuaHang;
GO

-- 6. Cập nhật vw_ChiTietDonHang bao gồm hình thức nhận hàng (HinhThucNhan)
CREATE OR ALTER VIEW vw_ChiTietDonHang
AS
SELECT
    dh.MaDonHang,
    dh.MaDon,
    dh.MaNguoiDung AS MaNguoiMua,
    mua.HoTen AS TenNguoiMua,
    ch.MaNguoiDung AS MaNguoiBan,
    ban.HoTen AS TenNguoiBan,
    dh.TenNguoiNhan,
    dh.SoDienThoaiNhan AS SDTNguoiNhan,
    dh.DiaChiNhan AS DiaChiGiaoHang,
    dh.TienHang,
    dh.PhiVanChuyen,
    dh.TongTien,
    dh.PhuongThucThanhToan,
    dh.TrangThaiThanhToan,
    dh.HinhThucNhan,
    dh.TrangThai AS TrangThaiDonHang,
    dh.NgayTao,
    ct.MaChiTietDonHang,
    ct.MaSanPham,
    ct.TenSanPham,
    ct.Gia AS DonGia
FROM DonHang dh
JOIN NguoiDung mua
    ON mua.MaNguoiDung = dh.MaNguoiDung
JOIN CuaHang ch
    ON ch.MaCuaHang = dh.MaCuaHang
JOIN NguoiDung ban
    ON ban.MaNguoiDung = ch.MaNguoiDung
JOIN ChiTietDonHang ct
    ON ct.MaDonHang = dh.MaDonHang;
GO

-- 7. Cập nhật vw_GioHangChiTiet kiểm tra đầy đủ trạng thái chủ shop và chặn mua shop của chính mình
CREATE OR ALTER VIEW vw_GioHangChiTiet
AS
SELECT
    gh.MaGioHang AS Id,
    gh.MaNguoiDung AS NguoiDungId,
    gh.MaSanPham AS SanPhamId,
    gh.DaChon,
    gh.ThoiGianThem,
    sp.Ten,
    sp.Gia,
    sp.TrangThai AS TrangThaiSanPham,
    sp.MaCuaHang,
    ch.TenShop,
    a.DuongDan AS AnhDaiDien,
    CAST(
        CASE
            WHEN sp.TrangThai = 'ACTIVE'
                AND sp.DaXoa = 0
                AND ch.TrangThai = 'ACTIVE'
                AND ch.MaNguoiDung <> gh.MaNguoiDung
                AND chuShop.TrangThai = 'ACTIVE'
            THEN 1
            ELSE 0
        END AS bit
    ) AS CoTheMua
FROM GioHang gh
JOIN SanPham sp
    ON sp.MaSanPham = gh.MaSanPham
JOIN CuaHang ch
    ON ch.MaCuaHang = sp.MaCuaHang
JOIN NguoiDung chuShop
    ON chuShop.MaNguoiDung = ch.MaNguoiDung
LEFT JOIN AnhSanPham a
    ON a.MaSanPham = sp.MaSanPham
    AND a.LaAnhChinh = 1;
GO

PRINT N'Đã hoàn tất cấu hình hình thức nhận hàng (22_hinh_thuc_nhan.sql)!';

-- 6. Cập nhật Stored Procedure sp_CapNhatTrangThaiDon (Ràng buộc hình thức nhận hàng khi hoàn tất)
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
                                      THEN 'REFUNDED'
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
        IF @TrangThaiMoi = 'COMPLETED'
        BEGIN
            UPDATE ThanhToan
            SET TrangThaiThanhToan = 'SUCCESS',
                ThoiGianThanhToan = ISNULL(ThoiGianThanhToan, @Now)
            WHERE MaDonHang = @MaDonHang;
        END
        ELSE IF @TrangThaiMoi = 'CANCELLED' AND EXISTS (SELECT 1 FROM DonHang WHERE MaDonHang = @MaDonHang AND TrangThaiThanhToan = 'REFUNDED')
        BEGIN
            UPDATE ThanhToan
            SET TrangThaiThanhToan = 'REFUNDED'
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
