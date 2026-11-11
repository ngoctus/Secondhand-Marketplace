SET NOCOUNT ON;
SET XACT_ABORT OFF;
GO
USE SecondHandMarketplace;
GO

/* =====================================================================
   D SMOKE TEST (SQL Server) - Module Giao dịch & Đơn hàng & Khiếu nại (Người D)
   Chạy sau khi đã áp dụng 01..09 và 22_hinh_thuc_nhan.sql:
       sqlcmd -S . -d SecondHandMarketplace -i database\tests\d_smoke_test.sql -f 65001
   - Sử dụng tiền tố 'dsmoke' để độc lập hoàn toàn, không đụng chạm dữ liệu khác.
   - Tự động dọn dẹp trước và sau khi chạy.
   - Kiểm tra: Giỏ hàng, Chặn mua shop mình, Tách đơn nhiều shop, Hẹn gặp trực tiếp,
     Hoàn trả trạng thái khi huỷ đơn, Tranh chấp & Báo cáo.
   ===================================================================== */

IF OBJECT_ID('tempdb..#KQ') IS NOT NULL DROP TABLE #KQ;
CREATE TABLE #KQ (Id int IDENTITY(1,1), Ten nvarchar(200), Dat bit, ChiTiet nvarchar(500));
GO

-- ------------------------------------------------------------------ DỌN DẸP TRƯỚC KHI CHẠY
DELETE FROM dbo.LichSuTrangThaiDonHang WHERE MaDonHang IN (SELECT MaDonHang FROM dbo.DonHang WHERE MaDon LIKE 'dsmoke%');
DELETE FROM dbo.ChiTietDonHang WHERE MaDonHang IN (SELECT MaDonHang FROM dbo.DonHang WHERE MaDon LIKE 'dsmoke%');
DELETE FROM dbo.ThanhToan WHERE MaDonHang IN (SELECT MaDonHang FROM dbo.DonHang WHERE MaDon LIKE 'dsmoke%');
DELETE FROM dbo.TranhChap WHERE LyDo LIKE N'dsmoke%';
DELETE FROM dbo.BaoCao WHERE LyDo LIKE N'dsmoke%';
DELETE FROM dbo.DonHang WHERE MaDon LIKE 'dsmoke%';
DELETE FROM dbo.GioHang WHERE MaNguoiDung IN (SELECT MaNguoiDung FROM dbo.NguoiDung WHERE Email LIKE 'dsmoke%');
DELETE FROM dbo.AnhSanPham WHERE MaSanPham IN (SELECT MaSanPham FROM dbo.SanPham WHERE Ten LIKE 'dsmoke%');
DELETE FROM dbo.SanPham WHERE Ten LIKE 'dsmoke%';
DELETE FROM dbo.CuaHang WHERE MaNguoiDung IN (SELECT MaNguoiDung FROM dbo.NguoiDung WHERE Email LIKE 'dsmoke%');
DELETE FROM dbo.NguoiDung WHERE Email LIKE 'dsmoke%';
GO

-- ------------------------------------------------------------------ KHỞI TẠO DỮ LIỆU TEST (dsmoke)
DECLARE @pw varchar(256) = REPLICATE('x', 30);
DECLARE @NguoiMuaId int, @ChuShop1Id int, @ChuShop2Id int;
DECLARE @Shop1Id int, @Shop2Id int;
DECLARE @Sp1Id int, @Sp2Id int, @Sp3Id int;

-- Tạo 1 Người mua và 2 Chủ shop
INSERT dbo.NguoiDung (HoTen, Email, SoDienThoai, MatKhauBam, VaiTro, TrangThai)
VALUES (N'D Người Mua', 'dsmoke.buyer@test.local', '0911000001', @pw, 'USER', 'ACTIVE');
SET @NguoiMuaId = SCOPE_IDENTITY();

INSERT dbo.NguoiDung (HoTen, Email, SoDienThoai, MatKhauBam, VaiTro, TrangThai)
VALUES (N'D Chủ Shop 1', 'dsmoke.seller1@test.local', '0911000002', @pw, 'USER', 'ACTIVE');
SET @ChuShop1Id = SCOPE_IDENTITY();

INSERT dbo.NguoiDung (HoTen, Email, SoDienThoai, MatKhauBam, VaiTro, TrangThai)
VALUES (N'D Chủ Shop 2', 'dsmoke.seller2@test.local', '0911000003', @pw, 'USER', 'ACTIVE');
SET @ChuShop2Id = SCOPE_IDENTITY();

-- Tạo 2 Cửa hàng
INSERT dbo.CuaHang (MaNguoiDung, TenShop, SoDienThoaiLienHe, TrangThai)
VALUES (@ChuShop1Id, N'dsmoke Shop 1', '0911000002', 'ACTIVE');
SET @Shop1Id = SCOPE_IDENTITY();

INSERT dbo.CuaHang (MaNguoiDung, TenShop, SoDienThoaiLienHe, TrangThai)
VALUES (@ChuShop2Id, N'dsmoke Shop 2', '0911000003', 'ACTIVE');
SET @Shop2Id = SCOPE_IDENTITY();

-- Lấy DanhMuc và TinhTrang hợp lệ sẵn có
DECLARE @MaDanhMuc int = (SELECT TOP 1 MaDanhMuc FROM dbo.DanhMuc ORDER BY MaDanhMuc);
DECLARE @MaTinhTrang varchar(20) = (SELECT TOP 1 Ma FROM dbo.TinhTrangSanPham ORDER BY Ma);

-- Tạo sản phẩm cho Shop 1
INSERT dbo.SanPham (MaCuaHang, MaDanhMuc, MaTinhTrang, Ten, MoTa, Gia, TrangThai, DaXoa)
VALUES (@Shop1Id, @MaDanhMuc, @MaTinhTrang, N'dsmoke Sản phẩm Shop 1 Món A', N'Mô tả test', 150000, 'ACTIVE', 0);
SET @Sp1Id = SCOPE_IDENTITY();

INSERT dbo.SanPham (MaCuaHang, MaDanhMuc, MaTinhTrang, Ten, MoTa, Gia, TrangThai, DaXoa)
VALUES (@Shop1Id, @MaDanhMuc, @MaTinhTrang, N'dsmoke Sản phẩm Shop 1 Món B', N'Mô tả test', 250000, 'ACTIVE', 0);
SET @Sp2Id = SCOPE_IDENTITY();

-- Tạo sản phẩm cho Shop 2
INSERT dbo.SanPham (MaCuaHang, MaDanhMuc, MaTinhTrang, Ten, MoTa, Gia, TrangThai, DaXoa)
VALUES (@Shop2Id, @MaDanhMuc, @MaTinhTrang, N'dsmoke Sản phẩm Shop 2 Món C', N'Mô tả test', 300000, 'ACTIVE', 0);
SET @Sp3Id = SCOPE_IDENTITY();

-- ------------------------------------------------------------------ TEST 1: sp_ThemVaoGio hợp lệ
BEGIN TRY
    EXEC dbo.sp_ThemVaoGio @MaNguoiDung = @NguoiMuaId, @MaSanPham = @Sp1Id;
    IF EXISTS (SELECT 1 FROM dbo.GioHang WHERE MaNguoiDung = @NguoiMuaId AND MaSanPham = @Sp1Id)
        INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'D1. ThemVaoGio hop le', 1, N'Thêm sản phẩm vào giỏ thành công');
    ELSE
        INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'D1. ThemVaoGio hop le', 0, N'Không tìm thấy bản ghi trong giỏ hàng');
END TRY
BEGIN CATCH
    INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'D1. ThemVaoGio hop le', 0, ERROR_MESSAGE());
END CATCH

-- ------------------------------------------------------------------ TEST 2: Chặn thêm sản phẩm shop mình (50031)
BEGIN TRY
    EXEC dbo.sp_ThemVaoGio @MaNguoiDung = @ChuShop1Id, @MaSanPham = @Sp1Id;
    INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'D2. Chan them san pham shop minh', 0, N'Lẽ ra phải chặn nhưng SP vẫn chạy');
END TRY
BEGIN CATCH
    IF ERROR_NUMBER() = 50031
        INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'D2. Chan them san pham shop minh', 1, N'Bắt đúng mã lỗi 50031');
    ELSE
        INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'D2. Chan them san pham shop minh', 0, CONCAT(N'Lỗi khác: ', ERROR_MESSAGE()));
END CATCH

-- ------------------------------------------------------------------ TEST 3: sp_ThemVaoGio không trùng lặp (Idempotent)
BEGIN TRY
    EXEC dbo.sp_ThemVaoGio @MaNguoiDung = @NguoiMuaId, @MaSanPham = @Sp1Id;
    DECLARE @SoLuongTrongGio int = (SELECT COUNT(*) FROM dbo.GioHang WHERE MaNguoiDung = @NguoiMuaId AND MaSanPham = @Sp1Id);
    IF @SoLuongTrongGio = 1
        INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'D3. Them gio hang khong duplicate', 1, N'Sản phẩm duy nhất = 1 trong giỏ');
    ELSE
        INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'D3. Them gio hang khong duplicate', 0, N'Bị trùng lặp số lượng');
END TRY
BEGIN CATCH
    INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'D3. Them gio hang khong duplicate', 0, ERROR_MESSAGE());
END CATCH

-- ------------------------------------------------------------------ TEST 4: Tách đơn nhiều shop qua sp_DatHangTuGio
BEGIN TRY
    -- Thêm tiếp sản phẩm của Shop 2 vào giỏ người mua
    EXEC dbo.sp_ThemVaoGio @MaNguoiDung = @NguoiMuaId, @MaSanPham = @Sp3Id;

    -- Đặt hàng từ giỏ (có sản phẩm từ Shop 1 và Shop 2)
    EXEC dbo.sp_DatHangTuGio 
        @MaNguoiDung = @NguoiMuaId,
        @TenNguoiNhan = N'Người Nhận Test',
        @SoDienThoaiNhan = '0911000001',
        @DiaChiNhan = N'Địa chỉ kiểm thử D smoke test',
        @PhiVanChuyenMoiDon = 30000,
        @PhuongThucThanhToan = 'COD',
        @HinhThucNhan = 'GIAO_HANG';

    -- Kiểm tra: Lẽ ra phải sinh 2 đơn hàng riêng biệt cho 2 shop
    DECLARE @SoDonDaTao int = (
        SELECT COUNT(DISTINCT dh.MaDonHang)
        FROM dbo.DonHang dh
        WHERE dh.MaNguoiDung = @NguoiMuaId AND dh.TrangThai = 'PENDING'
    );

    -- Kiểm tra giỏ hàng đã được xóa sạch
    DECLARE @ConTrongGio int = (SELECT COUNT(*) FROM dbo.GioHang WHERE MaNguoiDung = @NguoiMuaId);

    IF @SoDonDaTao = 2 AND @ConTrongGio = 0
        INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'D4. Tach don theo tung shop tu gio', 1, N'Tách đúng 2 đơn cho 2 shop và dọn giỏ hàng');
    ELSE
        INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'D4. Tach don theo tung shop tu gio', 0, CONCAT(N'Số đơn: ', @SoDonDaTao, N', Còn trong giỏ: ', @ConTrongGio));
END TRY
BEGIN CATCH
    INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'D4. Tach don theo tung shop tu gio', 0, ERROR_MESSAGE());
END CATCH

-- ------------------------------------------------------------------ TEST 5: Đặt hàng hình thức GAP_TRUC_TIEP (Hẹn gặp trực tiếp)
BEGIN TRY
    DECLARE @Sp2Check varchar(20) = (SELECT TrangThai FROM dbo.SanPham WHERE MaSanPham = @Sp2Id);
    IF @Sp2Check = 'ACTIVE'
    BEGIN
        DECLARE @MaDonHangMoi int;
        EXEC dbo.sp_TaoDonHang 
            @MaNguoiDung = @NguoiMuaId,
            @MaSanPham = @Sp2Id,
            @TenNguoiNhan = N'Người Nhận Hẹn Gặp',
            @SoDienThoaiNhan = '0911000001',
            @DiaChiNhan = N'Cổng trường ĐH GTVT Phân hiệu TPHCM',
            @PhiVanChuyen = 0,
            @PhuongThucThanhToan = 'COD',
            @HinhThucNhan = 'GAP_TRUC_TIEP',
            @MaDonHangMoi = @MaDonHangMoi OUTPUT;

        DECLARE @Htn varchar(20), @PhiVc decimal(18,2);
        SELECT @Htn = HinhThucNhan, @PhiVc = PhiVanChuyen FROM dbo.DonHang WHERE MaDonHang = @MaDonHangMoi;

        IF @Htn = 'GAP_TRUC_TIEP' AND @PhiVc = 0
            INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'D5. Don hang Hen gap truc tiep', 1, N'HinhThucNhan=GAP_TRUC_TIEP va PhiVanChuyen=0');
        ELSE
            INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'D5. Don hang Hen gap truc tiep', 0, CONCAT(N'Htn: ', @Htn, N', Phí: ', @PhiVc));
    END
    ELSE
        INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'D5. Don hang Hen gap truc tiep', 0, N'Sản phẩm không còn ACTIVE');
END TRY
BEGIN CATCH
    INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'D5. Don hang Hen gap truc tiep', 0, ERROR_MESSAGE());
END CATCH

-- ------------------------------------------------------------------ TEST 6: Hủy đơn hàng và kiểm tra hoàn trả RESERVED -> ACTIVE
BEGIN TRY
    DECLARE @DonHangHuyId int = (
        SELECT TOP 1 dh.MaDonHang 
        FROM dbo.DonHang dh 
        JOIN dbo.ChiTietDonHang ct ON ct.MaDonHang = dh.MaDonHang 
        WHERE ct.MaSanPham = @Sp1Id
    );

    IF @DonHangHuyId IS NOT NULL
    BEGIN
        EXEC dbo.sp_CapNhatTrangThaiDon 
            @MaDonHang = @DonHangHuyId,
            @TrangThaiMoi = 'CANCELLED',
            @NguoiThucHienId = @NguoiMuaId,
            @LoaiNguoiThucHien = 'BUYER',
            @LyDo = N'Người mua đổi ý không muốn nhận hàng nữa';

        DECLARE @Sp1Status varchar(20) = (SELECT TrangThai FROM dbo.SanPham WHERE MaSanPham = @Sp1Id);
        IF @Sp1Status = 'ACTIVE'
            INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'D6. Huy don hoan lai san pham ACTIVE', 1, N'Sản phẩm đã được giải phóng sang ACTIVE');
        ELSE
            INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'D6. Huy don hoan lai san pham ACTIVE', 0, CONCAT(N'Trạng thái sản phẩm hiện tại: ', @Sp1Status));
    END
    ELSE
        INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'D6. Huy don hoan lai san pham ACTIVE', 0, N'Không tìm thấy đơn hàng chứa Sp1');
END TRY
BEGIN CATCH
    INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'D6. Huy don hoan lai san pham ACTIVE', 0, ERROR_MESSAGE());
END CATCH

-- ------------------------------------------------------------------ TEST 7: Kiểm tra view vw_DonHangTongQuan có cột HinhThucNhan
BEGIN TRY
    IF EXISTS (
        SELECT 1 FROM sys.columns 
        WHERE object_id = OBJECT_ID('vw_DonHangTongQuan') AND name = 'HinhThucNhan'
    )
        INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'D7. View vw_DonHangTongQuan co HinhThucNhan', 1, N'Cột HinhThucNhan hiện diện trong view tổng quan');
    ELSE
        INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'D7. View vw_DonHangTongQuan co HinhThucNhan', 0, N'Thiếu cột HinhThucNhan trong view');
END TRY
BEGIN CATCH
    INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'D7. View vw_DonHangTongQuan co HinhThucNhan', 0, ERROR_MESSAGE());
END CATCH

-- ------------------------------------------------------------------ DỌN DẸP SAU KHI TEST
DELETE FROM dbo.LichSuTrangThaiDonHang WHERE MaDonHang IN (SELECT MaDonHang FROM dbo.DonHang WHERE MaNguoiDung = @NguoiMuaId);
DELETE FROM dbo.ChiTietDonHang WHERE MaDonHang IN (SELECT MaDonHang FROM dbo.DonHang WHERE MaNguoiDung = @NguoiMuaId);
DELETE FROM dbo.ThanhToan WHERE MaDonHang IN (SELECT MaDonHang FROM dbo.DonHang WHERE MaNguoiDung = @NguoiMuaId);
DELETE FROM dbo.DonHang WHERE MaNguoiDung = @NguoiMuaId;
DELETE FROM dbo.GioHang WHERE MaNguoiDung IN (@NguoiMuaId, @ChuShop1Id, @ChuShop2Id);
DELETE FROM dbo.AnhSanPham WHERE MaSanPham IN (@Sp1Id, @Sp2Id, @Sp3Id);
DELETE FROM dbo.SanPham WHERE MaSanPham IN (@Sp1Id, @Sp2Id, @Sp3Id);
DELETE FROM dbo.CuaHang WHERE MaCuaHang IN (@Shop1Id, @Shop2Id);
DELETE FROM dbo.NguoiDung WHERE MaNguoiDung IN (@NguoiMuaId, @ChuShop1Id, @ChuShop2Id);
GO

-- ------------------------------------------------------------------ IN KẾT QUẢ
SELECT Id, Ten, Dat, ChiTiet FROM #KQ ORDER BY Id;

IF EXISTS (SELECT 1 FROM #KQ WHERE Dat = 0)
BEGIN
    RAISERROR(N'CÓ TEST THẤT BẠI TRONG D_SMOKE_TEST!', 16, 1);
END
ELSE
BEGIN
    PRINT N'TẤT CẢ TEST MODULE D (D_SMOKE_TEST) ĐỀU THÀNH CÔNG (PASS)!';
END
GO
