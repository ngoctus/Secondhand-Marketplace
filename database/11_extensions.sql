SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ARITHABORT ON;
GO

USE SecondHandMarketplace;
GO

-- Stored procedures mở rộng: đánh giá và đấu giá. Bảng ở 02, view ở 05, trigger ở 06.

-- -----------------------------------------------------------------------------
-- 3. Stored procedure sp_TaoDanhGia
--    Trả về một dòng một cột: MaDanhGia vừa tạo.
-- -----------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE dbo.sp_TaoDanhGia
    @MaDonHang  int,
    @MaNguoiMua int,
    @SoSao      tinyint,
    @NoiDung    nvarchar(1000) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @MaCuaHang  int;
    DECLARE @MaNguoiDon int;
    DECLARE @TrangThai  varchar(15);
    DECLARE @MaDanhGia  int;

    IF @SoSao IS NULL OR @SoSao NOT BETWEEN 1 AND 5
        THROW 50005, N'Số sao phải từ 1 đến 5.', 1;

    -- Chuẩn hóa nội dung: bỏ khoảng trắng đầu cuối, chuỗi rỗng thành NULL.
    SET @NoiDung = NULLIF(LTRIM(RTRIM(@NoiDung)), N'');

    SELECT @MaCuaHang  = MaCuaHang,
           @MaNguoiDon = MaNguoiDung,
           @TrangThai  = TrangThai
    FROM dbo.DonHang
    WHERE MaDonHang = @MaDonHang;

    IF @MaNguoiDon IS NULL
        THROW 50001, N'Đơn hàng không tồn tại.', 1;

    IF @MaNguoiDon <> @MaNguoiMua
        THROW 50002, N'Bạn không phải người mua của đơn hàng này nên không thể đánh giá.', 1;

    IF @TrangThai <> 'COMPLETED'
        THROW 50003, N'Chỉ được đánh giá khi đơn hàng đã hoàn tất.', 1;

    IF EXISTS (SELECT 1 FROM dbo.DanhGia WHERE MaDonHang = @MaDonHang)
        THROW 50004, N'Đơn hàng này đã được đánh giá.', 1;

    BEGIN TRY
        INSERT INTO dbo.DanhGia (MaDonHang, MaNguoiMua, MaCuaHang, SoSao, NoiDung)
        VALUES (@MaDonHang, @MaNguoiMua, @MaCuaHang, @SoSao, @NoiDung);

        SET @MaDanhGia = CAST(SCOPE_IDENTITY() AS int);
    END TRY
    BEGIN CATCH
        -- Hai yêu cầu đánh giá cùng lúc: UNIQUE (MaDonHang) chặn bản thứ hai.
        IF ERROR_NUMBER() IN (2601, 2627)
            THROW 50004, N'Đơn hàng này đã được đánh giá.', 1;
        THROW;
    END CATCH;

    SELECT @MaDanhGia AS MaDanhGia;
END;
GO

-- =====================================================================
-- Người bán tạo phiên
-- =====================================================================
CREATE OR ALTER PROCEDURE sp_TaoPhienDauGia
    @MaNguoiDung     int,
    @MaSanPham       int,
    @GiaKhoiDiem     decimal(18,2),
    @BuocGia         decimal(18,2),
    @ThoiGianBatDau  datetime2(3),
    @ThoiGianKetThuc datetime2(3),
    @MaPhienMoi      int OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Now datetime2(3) = SYSUTCDATETIME(), @ChuShopId int, @TrangThaiShop varchar(10),
            @TrangThaiSP varchar(20), @Gia decimal(18,2);

    BEGIN TRY
        BEGIN TRANSACTION;

        IF NOT EXISTS (SELECT 1 FROM NguoiDung WHERE MaNguoiDung = @MaNguoiDung AND VaiTro = 'USER' AND TrangThai = 'ACTIVE')
            THROW 52001, N'Tài khoản không hợp lệ hoặc đã bị khóa.', 1;

        SELECT @ChuShopId = ch.MaNguoiDung, @TrangThaiShop = ch.TrangThai, @TrangThaiSP = sp.TrangThai, @Gia = sp.Gia
        FROM SanPham sp WITH (UPDLOCK, ROWLOCK)
        JOIN CuaHang ch ON ch.MaCuaHang = sp.MaCuaHang
        WHERE sp.MaSanPham = @MaSanPham AND sp.DaXoa = 0;

        IF @ChuShopId IS NULL OR @ChuShopId <> @MaNguoiDung
            THROW 52002, N'Sản phẩm không tồn tại hoặc không thuộc cửa hàng của bạn.', 1;
        IF @TrangThaiShop <> 'ACTIVE'
            THROW 52003, N'Cửa hàng đang bị khóa, không thể tạo phiên đấu giá.', 1;
        IF @TrangThaiSP <> 'ACTIVE' OR @Gia IS NULL
            THROW 52004, N'Chỉ sản phẩm đang bán mới được đưa lên đấu giá.', 1;
        IF EXISTS (SELECT 1 FROM PhienDauGia WHERE MaSanPham = @MaSanPham AND TrangThai IN ('SCHEDULED','RUNNING'))
            THROW 52005, N'Sản phẩm này đã nằm trong một phiên đấu giá khác.', 1;

        IF @GiaKhoiDiem IS NULL OR @GiaKhoiDiem < 1000 OR @GiaKhoiDiem > 1000000000
            THROW 52006, N'Giá khởi điểm phải từ 1.000đ đến 1.000.000.000đ.', 1;
        IF @BuocGia IS NULL OR @BuocGia <= 0 OR @BuocGia > 1000000000
            THROW 52007, N'Bước giá không hợp lệ.', 1;

        IF @ThoiGianBatDau < DATEADD(MINUTE, -2, @Now)
            THROW 52008, N'Thời gian bắt đầu không được nằm trong quá khứ.', 1;
        IF @ThoiGianBatDau < @Now SET @ThoiGianBatDau = @Now;
        IF @ThoiGianKetThuc < DATEADD(MINUTE, 5, @ThoiGianBatDau)
            THROW 52009, N'Phiên đấu giá phải kéo dài tối thiểu 5 phút.', 1;
        IF @ThoiGianKetThuc > DATEADD(DAY, 30, @ThoiGianBatDau)
            THROW 52010, N'Phiên đấu giá không được kéo dài quá 30 ngày.', 1;

        INSERT PhienDauGia (MaSanPham, GiaNiemYet, GiaKhoiDiem, BuocGia, GiaHienTai, ThoiGianBatDau, ThoiGianKetThuc, TrangThai)
        VALUES (@MaSanPham, @Gia, @GiaKhoiDiem, @BuocGia, @GiaKhoiDiem, @ThoiGianBatDau, @ThoiGianKetThuc,
                CASE WHEN @ThoiGianBatDau <= @Now THEN 'RUNNING' ELSE 'SCHEDULED' END);
        SET @MaPhienMoi = SCOPE_IDENTITY();

        EXEC sp_ChuyenTrangThaiSanPham @MaSanPham = @MaSanPham, @TrangThaiMoi = 'RESERVED',
             @LoaiNguoiThucHien = 'SELLER', @NguoiThucHienId = @MaNguoiDung, @LyDo = N'Đưa vào phiên đấu giá';

        EXEC sp_GhiNhatKy @MaNguoiDung = @MaNguoiDung, @HanhDong = 'CREATE_AUCTION',
                          @LoaiDoiTuong = 'PhienDauGia', @DoiTuongId = @MaPhienMoi;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END
GO

-- =====================================================================
-- Đặt giá (khóa dòng phiên để hai người đặt cùng lúc được xử lý tuần tự)
-- =====================================================================
CREATE OR ALTER PROCEDURE sp_DatGia
    @MaPhienDauGia int,
    @MaNguoiDung   int,
    @SoTien        decimal(18,2)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Now datetime2(3) = SYSUTCDATETIME(), @TrangThai varchar(12), @Bat datetime2(3), @Ket datetime2(3),
            @GiaHienTai decimal(18,2), @GiaKhoiDiem decimal(18,2), @Buoc decimal(18,2), @SoLuot int,
            @DanDau int, @MaSP int, @ChuShopId int, @TenSP nvarchar(200), @GiaToiThieu decimal(18,2),
            @SDT varchar(15), @DiaChi nvarchar(500), @Msg nvarchar(300),
            @TrangThaiSP varchar(20), @TrangThaiShop varchar(10), @DaXoa bit;

    BEGIN TRY
        BEGIN TRANSACTION;

        SELECT @TrangThai = p.TrangThai, @Bat = p.ThoiGianBatDau, @Ket = p.ThoiGianKetThuc,
               @GiaHienTai = p.GiaHienTai, @GiaKhoiDiem = p.GiaKhoiDiem, @Buoc = p.BuocGia,
               @SoLuot = p.SoLuotDat, @DanDau = p.MaNguoiDanDau, @MaSP = p.MaSanPham
        FROM PhienDauGia p WITH (UPDLOCK, HOLDLOCK)
        WHERE p.MaPhienDauGia = @MaPhienDauGia;

        IF @TrangThai IS NULL
            THROW 52020, N'Phiên đấu giá không tồn tại.', 1;
        IF @TrangThai NOT IN ('SCHEDULED','RUNNING') OR @Ket <= @Now
            THROW 52021, N'Phiên đấu giá đã kết thúc.', 1;
        IF @Bat > @Now
            THROW 52022, N'Phiên đấu giá chưa bắt đầu.', 1;

        IF NOT EXISTS (SELECT 1 FROM NguoiDung WHERE MaNguoiDung = @MaNguoiDung AND VaiTro = 'USER' AND TrangThai = 'ACTIVE')
            THROW 52023, N'Chỉ tài khoản người dùng đang hoạt động mới được đặt giá.', 1;

        SELECT @ChuShopId = ch.MaNguoiDung, @TenSP = sp.Ten,
               @TrangThaiSP = sp.TrangThai, @TrangThaiShop = ch.TrangThai, @DaXoa = sp.DaXoa
        FROM SanPham sp JOIN CuaHang ch ON ch.MaCuaHang = sp.MaCuaHang
        WHERE sp.MaSanPham = @MaSP;

        IF @ChuShopId IS NULL
            THROW 52020, N'Phiên đấu giá không tồn tại.', 1;

        -- Chốt chặn cuối: sản phẩm đang đấu giá được giữ ở RESERVED (sp_TaoPhienDauGia),
        -- nên chỉ chấp nhận ACTIVE hoặc RESERVED; chặn sản phẩm đã gỡ/ngừng bán và shop bị khóa.
        IF @DaXoa = 1 OR @TrangThaiSP NOT IN ('ACTIVE','RESERVED')
            THROW 52029, N'Sản phẩm đã bị gỡ hoặc không còn bán, không thể đặt giá.', 1;
        IF @TrangThaiShop <> 'ACTIVE'
            THROW 52030, N'Cửa hàng đang bị khóa, không thể đặt giá.', 1;

        IF @ChuShopId = @MaNguoiDung
            THROW 52024, N'Bạn không thể đặt giá cho sản phẩm của chính mình.', 1;
        IF @DanDau = @MaNguoiDung
            THROW 52025, N'Bạn đang là người dẫn đầu phiên đấu giá này.', 1;

        SELECT @SDT = SoDienThoai, @DiaChi = DiaChiMacDinh FROM NguoiDung WHERE MaNguoiDung = @MaNguoiDung;
        IF @SDT IS NULL OR LEN(LTRIM(RTRIM(ISNULL(@DiaChi, N'')))) = 0
            THROW 52026, N'Vui lòng cập nhật số điện thoại và địa chỉ mặc định trong hồ sơ trước khi đặt giá.', 1;

        IF @SoTien > 1000000000
            THROW 52027, N'Giá đặt tối đa là 1.000.000.000đ.', 1;

        SET @GiaToiThieu = CASE WHEN @SoLuot = 0 THEN @GiaKhoiDiem ELSE @GiaHienTai + @Buoc END;
        IF @SoTien < @GiaToiThieu
        BEGIN
            SET @Msg = CONCAT(N'Giá đặt tối thiểu hiện tại là ', FORMAT(@GiaToiThieu, 'N0', 'vi-VN'), N'đ.');
            THROW 52028, @Msg, 1;
        END

        INSERT LuotDatGia (MaPhienDauGia, MaNguoiDung, SoTien) VALUES (@MaPhienDauGia, @MaNguoiDung, @SoTien);

        UPDATE PhienDauGia
        SET GiaHienTai = @SoTien, SoLuotDat = SoLuotDat + 1, MaNguoiDanDau = @MaNguoiDung,
            TrangThai = 'RUNNING', NgayCapNhat = @Now
        WHERE MaPhienDauGia = @MaPhienDauGia;

        IF @DanDau IS NOT NULL
            INSERT ThongBao (MaNguoiDung, Loai, TieuDe, NoiDung, MaThamChieu)
            VALUES (@DanDau, 'PRODUCT', N'Bạn đã bị vượt giá',
                    CONCAT(N'Có người vừa đặt ', FORMAT(@SoTien, 'N0', 'vi-VN'), N'đ cho "', @TenSP, N'". Hãy vào phiên đấu giá để đặt giá cao hơn.'),
                    @MaSP);

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END
GO

-- =====================================================================
-- Hủy phiên (người bán: chỉ khi chưa có người đặt; Admin: bất cứ lúc nào, kèm lý do)
-- =====================================================================
CREATE OR ALTER PROCEDURE sp_HuyPhienDauGia
    @MaPhienDauGia int,
    @MaNguoiDung   int,
    @LyDo          nvarchar(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Now datetime2(3) = SYSUTCDATETIME(), @VaiTro varchar(10), @Loai varchar(10), @TrangThai varchar(12),
            @SoLuot int, @MaSP int, @ChuShopId int, @TenSP nvarchar(200);

    BEGIN TRY
        BEGIN TRANSACTION;

        SELECT @VaiTro = VaiTro FROM NguoiDung WHERE MaNguoiDung = @MaNguoiDung AND TrangThai = 'ACTIVE';
        IF @VaiTro IS NULL
            THROW 52040, N'Tài khoản không hợp lệ.', 1;

        SELECT @TrangThai = p.TrangThai, @SoLuot = p.SoLuotDat, @MaSP = p.MaSanPham
        FROM PhienDauGia p WITH (UPDLOCK, HOLDLOCK)
        WHERE p.MaPhienDauGia = @MaPhienDauGia;

        IF @TrangThai IS NULL
            THROW 52041, N'Phiên đấu giá không tồn tại.', 1;
        IF @TrangThai NOT IN ('SCHEDULED','RUNNING')
            THROW 52042, N'Phiên đấu giá không còn hoạt động nên không thể hủy.', 1;

        SELECT @ChuShopId = ch.MaNguoiDung, @TenSP = sp.Ten
        FROM SanPham sp JOIN CuaHang ch ON ch.MaCuaHang = sp.MaCuaHang WHERE sp.MaSanPham = @MaSP;

        IF @VaiTro = 'ADMIN' SET @Loai = 'ADMIN';
        ELSE IF @MaNguoiDung = @ChuShopId SET @Loai = 'SELLER';
        ELSE THROW 52043, N'Bạn không có quyền hủy phiên đấu giá này.', 1;

        IF @Loai = 'SELLER' AND @SoLuot > 0
            THROW 52044, N'Phiên đã có người đặt giá nên bạn không thể hủy. Hãy liên hệ quản trị viên nếu có sự cố.', 1;
        IF @Loai = 'ADMIN' AND LEN(LTRIM(RTRIM(ISNULL(@LyDo, N'')))) < 5
            THROW 52045, N'Admin hủy phiên bắt buộc nhập lý do (tối thiểu 5 ký tự).', 1;

        SET @LyDo = ISNULL(NULLIF(LTRIM(RTRIM(@LyDo)), N''), N'Người bán hủy phiên đấu giá.');

        UPDATE PhienDauGia
        SET TrangThai = 'CANCELLED', NguoiHuy = @Loai, LyDoHuy = @LyDo, ThoiGianHuy = @Now, NgayCapNhat = @Now
        WHERE MaPhienDauGia = @MaPhienDauGia;

        IF EXISTS (SELECT 1 FROM SanPham WHERE MaSanPham = @MaSP AND TrangThai = 'RESERVED' AND DaXoa = 0)
            EXEC sp_ChuyenTrangThaiSanPham @MaSanPham = @MaSP, @TrangThaiMoi = 'ACTIVE',
                 @LoaiNguoiThucHien = @Loai, @NguoiThucHienId = @MaNguoiDung, @LyDo = N'Hủy phiên đấu giá';

        INSERT ThongBao (MaNguoiDung, Loai, TieuDe, NoiDung, MaThamChieu)
        SELECT DISTINCT l.MaNguoiDung, 'PRODUCT', N'Phiên đấu giá đã bị hủy',
               CONCAT(N'Phiên đấu giá "', @TenSP, N'" đã bị hủy. Lý do: ', @LyDo), @MaSP
        FROM LuotDatGia l WHERE l.MaPhienDauGia = @MaPhienDauGia;

        IF @Loai = 'ADMIN'
            INSERT ThongBao (MaNguoiDung, Loai, TieuDe, NoiDung, MaThamChieu)
            VALUES (@ChuShopId, 'PRODUCT', N'Phiên đấu giá của bạn bị hủy',
                    CONCAT(N'Quản trị viên đã hủy phiên đấu giá "', @TenSP, N'". Lý do: ', @LyDo), @MaSP);

        EXEC sp_GhiNhatKy @MaNguoiDung = @MaNguoiDung, @HanhDong = 'CANCEL_AUCTION',
                          @LoaiDoiTuong = 'PhienDauGia', @DoiTuongId = @MaPhienDauGia, @LyDo = @LyDo;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END
GO

-- =====================================================================
-- Kết thúc 1 phiên đã hết giờ (BackgroundService gọi mỗi vài chục giây).
-- Có người thắng: tạo đơn bằng sp_TaoDonHangTheoShop (quy trình của D) với GIÁ THẮNG.
-- Không ai đặt: đóng phiên, trả sản phẩm về ACTIVE.
-- =====================================================================
CREATE OR ALTER PROCEDURE sp_KetThucPhienDauGia
    @MaPhienDauGia int
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Now datetime2(3) = SYSUTCDATETIME(), @TrangThai varchar(12), @Ket datetime2(3), @SoLuot int,
            @NguoiThang int, @GiaThang decimal(18,2), @GiaNiemYet decimal(18,2), @MaSP int,
            @MaCuaHang int, @ChuShopId int, @TrangThaiShop varchar(10), @TrangThaiSP varchar(20), @TenSP nvarchar(200),
            @TTNguoiThang varchar(10), @Ten nvarchar(100), @SDT varchar(15), @DiaChi nvarchar(500),
            @MaDonHang int, @MaDon varchar(30), @LyDoHuy nvarchar(500), @DanhSach dbo.DanhSachId;

    BEGIN TRY
        BEGIN TRANSACTION;

        SELECT @TrangThai = p.TrangThai, @Ket = p.ThoiGianKetThuc, @SoLuot = p.SoLuotDat,
               @NguoiThang = p.MaNguoiDanDau, @GiaThang = p.GiaHienTai, @GiaNiemYet = p.GiaNiemYet, @MaSP = p.MaSanPham
        FROM PhienDauGia p WITH (UPDLOCK, HOLDLOCK)
        WHERE p.MaPhienDauGia = @MaPhienDauGia;

        IF @TrangThai IS NULL OR @TrangThai NOT IN ('SCHEDULED','RUNNING') OR @Ket > @Now
        BEGIN
            COMMIT TRANSACTION;
            RETURN;
        END

        SELECT @MaCuaHang = sp.MaCuaHang, @TrangThaiSP = sp.TrangThai, @TenSP = sp.Ten,
               @ChuShopId = ch.MaNguoiDung, @TrangThaiShop = ch.TrangThai
        FROM SanPham sp JOIN CuaHang ch ON ch.MaCuaHang = sp.MaCuaHang WHERE sp.MaSanPham = @MaSP;

        -- (A) Không ai đặt giá
        IF @SoLuot = 0 OR @NguoiThang IS NULL
        BEGIN
            UPDATE PhienDauGia SET TrangThai = 'ENDED', NgayCapNhat = @Now WHERE MaPhienDauGia = @MaPhienDauGia;

            IF @TrangThaiSP = 'RESERVED'
                EXEC sp_ChuyenTrangThaiSanPham @MaSanPham = @MaSP, @TrangThaiMoi = 'ACTIVE',
                     @LoaiNguoiThucHien = 'SYSTEM', @LyDo = N'Phiên đấu giá kết thúc, không có người đặt giá';

            INSERT ThongBao (MaNguoiDung, Loai, TieuDe, NoiDung, MaThamChieu)
            VALUES (@ChuShopId, 'PRODUCT', N'Phiên đấu giá kết thúc',
                    CONCAT(N'Phiên đấu giá "', @TenSP, N'" kết thúc mà chưa có ai đặt giá. Sản phẩm đã được trả về trạng thái đang bán.'), @MaSP);

            EXEC sp_GhiNhatKy @HanhDong = 'END_AUCTION', @LoaiDoiTuong = 'PhienDauGia', @DoiTuongId = @MaPhienDauGia,
                              @LyDo = N'Không có người đặt giá';
            COMMIT TRANSACTION;
            RETURN;
        END

        -- (B) Có người thắng: kiểm tra điều kiện tạo đơn trước, nếu sai thì hủy phiên (tránh lặp lỗi mãi)
        SELECT @TTNguoiThang = TrangThai, @Ten = HoTen, @SDT = SoDienThoai, @DiaChi = DiaChiMacDinh
        FROM NguoiDung WHERE MaNguoiDung = @NguoiThang AND VaiTro = 'USER';

        SET @LyDoHuy = CASE
            WHEN @TrangThaiSP <> 'RESERVED' THEN N'Sản phẩm không còn ở trạng thái đấu giá.'
            WHEN @TrangThaiShop <> 'ACTIVE' THEN N'Cửa hàng đang bị khóa.'
            WHEN @TTNguoiThang IS NULL OR @TTNguoiThang <> 'ACTIVE' THEN N'Tài khoản người thắng không hợp lệ hoặc đã bị khóa.'
            WHEN @SDT IS NULL OR @SDT LIKE '%[^0-9]%' OR LEN(@SDT) NOT BETWEEN 9 AND 15 THEN N'Số điện thoại người thắng không hợp lệ.'
            WHEN @NguoiThang = @ChuShopId THEN N'Người thắng trùng chủ cửa hàng.'
            ELSE NULL END;

        IF @LyDoHuy IS NOT NULL
        BEGIN
            UPDATE PhienDauGia
            SET TrangThai = 'CANCELLED', NguoiHuy = 'SYSTEM', LyDoHuy = @LyDoHuy, ThoiGianHuy = @Now, NgayCapNhat = @Now
            WHERE MaPhienDauGia = @MaPhienDauGia;

            IF @TrangThaiSP = 'RESERVED'
                EXEC sp_ChuyenTrangThaiSanPham @MaSanPham = @MaSP, @TrangThaiMoi = 'ACTIVE',
                     @LoaiNguoiThucHien = 'SYSTEM', @LyDo = N'Không thể chốt đơn đấu giá';

            INSERT ThongBao (MaNguoiDung, Loai, TieuDe, NoiDung, MaThamChieu)
            SELECT DISTINCT x.Id, 'PRODUCT', N'Phiên đấu giá bị hủy',
                   CONCAT(N'Phiên đấu giá "', @TenSP, N'" bị hủy do: ', @LyDoHuy), @MaSP
            FROM (SELECT @ChuShopId AS Id UNION SELECT MaNguoiDung FROM LuotDatGia WHERE MaPhienDauGia = @MaPhienDauGia) x;

            EXEC sp_GhiNhatKy @HanhDong = 'CANCEL_AUCTION', @LoaiDoiTuong = 'PhienDauGia', @DoiTuongId = @MaPhienDauGia,
                              @KetQua = 'FAILED', @LyDo = @LyDoHuy;
            COMMIT TRANSACTION;
            RETURN;
        END

        -- Tạo đơn cho người thắng. Truyền thẳng giá thắng vào sp_TaoDonHangTheoShop
        -- để không phải ghi đè tạm SanPham.Gia (tránh đè giá seller và bắn trigger giả).
        IF LEN(LTRIM(RTRIM(ISNULL(@DiaChi, N'')))) = 0
            SET @DiaChi = N'Người thắng chưa cập nhật địa chỉ, vui lòng liên hệ để xác nhận.';

         -- Đóng phiên (ghi người thắng) TRƯỚC khi trả sản phẩm về ACTIVE để trigger chặn rút sản phẩm không vướng
        UPDATE PhienDauGia
        SET TrangThai = 'ENDED', MaNguoiThang = @NguoiThang, GiaThang = @GiaThang, NgayCapNhat = @Now
        WHERE MaPhienDauGia = @MaPhienDauGia;

        EXEC sp_ChuyenTrangThaiSanPham @MaSanPham = @MaSP, @TrangThaiMoi = 'ACTIVE',
             @LoaiNguoiThucHien = 'SYSTEM', @LyDo = N'Chốt đơn cho người thắng đấu giá';

        INSERT @DanhSach (Id) VALUES (@MaSP);

        EXEC sp_TaoDonHangTheoShop @MaNguoiDung = @NguoiThang, @MaCuaHang = @MaCuaHang, @DanhSachSanPham = @DanhSach,
             @TenNguoiNhan = @Ten, @SoDienThoaiNhan = @SDT, @DiaChiNhan = @DiaChi,
             @PhiVanChuyen = 0, @PhuongThucThanhToan = 'COD', @GiaTuyChon = @GiaThang, @MaDonHangMoi = @MaDonHang OUTPUT;

        SELECT @MaDon = MaDon FROM DonHang WHERE MaDonHang = @MaDonHang;

        UPDATE PhienDauGia SET MaDonHang = @MaDonHang, NgayCapNhat = @Now WHERE MaPhienDauGia = @MaPhienDauGia;

        INSERT ThongBao (MaNguoiDung, Loai, TieuDe, NoiDung, MaThamChieu)
        VALUES (@NguoiThang, 'ORDER', N'Chúc mừng! Bạn đã thắng đấu giá',
                CONCAT(N'Bạn thắng "', @TenSP, N'" với giá ', FORMAT(@GiaThang, 'N0', 'vi-VN'), N'đ. Đơn hàng ', @MaDon, N' đã được tạo.'),
                @MaDonHang);

        INSERT ThongBao (MaNguoiDung, Loai, TieuDe, NoiDung, MaThamChieu)
        SELECT DISTINCT l.MaNguoiDung, 'PRODUCT', N'Phiên đấu giá đã kết thúc',
               CONCAT(N'Phiên đấu giá "', @TenSP, N'" đã kết thúc. Rất tiếc bạn không phải người thắng.'), @MaSP
        FROM LuotDatGia l WHERE l.MaPhienDauGia = @MaPhienDauGia AND l.MaNguoiDung <> @NguoiThang;

        INSERT ThongBao (MaNguoiDung, Loai, TieuDe, NoiDung, MaThamChieu)
        VALUES (@ChuShopId, 'ORDER', N'Phiên đấu giá đã có người thắng',
                CONCAT(N'"', @TenSP, N'" được bán với giá ', FORMAT(@GiaThang, 'N0', 'vi-VN'), N'đ (đơn ', @MaDon, N').'), @MaDonHang);

        EXEC sp_GhiNhatKy @HanhDong = 'END_AUCTION', @LoaiDoiTuong = 'PhienDauGia', @DoiTuongId = @MaPhienDauGia,
                          @ChiTiet = @MaDon;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END
GO

-- Hủy phiên do hệ thống khi không thể chốt đơn (BackgroundService gọi sau nhiều lần lỗi liên tiếp)
CREATE OR ALTER PROCEDURE sp_HuyPhienDauGiaHeThong
    @MaPhienDauGia int,
    @LyDo          nvarchar(500)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Now datetime2(3) = SYSUTCDATETIME(), @TrangThai varchar(12), @MaSP int,
            @ChuShopId int, @TenSP nvarchar(200);

    BEGIN TRY
        BEGIN TRANSACTION;

        SELECT @TrangThai = p.TrangThai, @MaSP = p.MaSanPham
        FROM PhienDauGia p WITH (UPDLOCK, HOLDLOCK)
        WHERE p.MaPhienDauGia = @MaPhienDauGia;

        IF @TrangThai IS NULL OR @TrangThai NOT IN ('SCHEDULED','RUNNING')
        BEGIN
            COMMIT TRANSACTION;
            RETURN;
        END

        SELECT @ChuShopId = ch.MaNguoiDung, @TenSP = sp.Ten
        FROM SanPham sp JOIN CuaHang ch ON ch.MaCuaHang = sp.MaCuaHang WHERE sp.MaSanPham = @MaSP;

        UPDATE PhienDauGia
        SET TrangThai = 'CANCELLED', NguoiHuy = 'SYSTEM', LyDoHuy = @LyDo, ThoiGianHuy = @Now, NgayCapNhat = @Now
        WHERE MaPhienDauGia = @MaPhienDauGia;

        IF EXISTS (SELECT 1 FROM SanPham WHERE MaSanPham = @MaSP AND TrangThai = 'RESERVED' AND DaXoa = 0)
            EXEC sp_ChuyenTrangThaiSanPham @MaSanPham = @MaSP, @TrangThaiMoi = 'ACTIVE',
                 @LoaiNguoiThucHien = 'SYSTEM', @LyDo = N'Hệ thống hủy phiên đấu giá do lỗi chốt đơn';

        INSERT ThongBao (MaNguoiDung, Loai, TieuDe, NoiDung, MaThamChieu)
        SELECT DISTINCT x.Id, 'PRODUCT', N'Phiên đấu giá bị hủy',
               CONCAT(N'Phiên đấu giá "', @TenSP, N'" bị hủy do hệ thống không thể chốt đơn. Xin lỗi vì sự bất tiện.'), @MaSP
        FROM (SELECT @ChuShopId AS Id UNION SELECT MaNguoiDung FROM LuotDatGia WHERE MaPhienDauGia = @MaPhienDauGia) x;

        EXEC sp_GhiNhatKy @HanhDong = 'CANCEL_AUCTION', @LoaiDoiTuong = 'PhienDauGia', @DoiTuongId = @MaPhienDauGia,
                          @KetQua = 'FAILED', @LyDo = @LyDo;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END
GO
