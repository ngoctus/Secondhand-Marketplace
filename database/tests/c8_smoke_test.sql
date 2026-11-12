SET NOCOUNT ON;
SET XACT_ABORT OFF;
GO
USE SecondHandMarketplace;
GO

/* =====================================================================
   C8 SMOKE TEST (SQL Server) - Module Đánh giá & Uy tín người bán (Người C)
   Chạy sau khi đã áp dụng 01..09, 11_danhgia.sql và 22_hinh_thuc_nhan.sql:
       sqlcmd -S . -d SecondHandMarketplace -i database\tests\c8_smoke_test.sql -f 65001
   - Sử dụng tiền tố 'c8smoke' để độc lập hoàn toàn, không đụng chạm dữ liệu khác.
   - Tự động dọn dẹp trước và sau khi chạy.
   - Kiểm tra các quy tắc nghiệp vụ cốt lõi:
       1. sp_TaoDanhGia:
          - Lỗi 50001: Đơn không tồn tại.
          - Lỗi 50002: Người đánh giá không phải người mua của đơn.
          - Lỗi 50003: Đơn chưa hoàn tất (chỉ COMPLETED mới được đánh giá).
          - Lỗi 50004: Chặn đánh giá lần 2 trên cùng một đơn hàng.
          - Lỗi 50005: Số sao ngoài khoảng 1 đến 5.
          - Đánh giá thành công: lưu đúng người mua, đúng shop, mặc định VISIBLE.
       2. vw_DiemCuaHang:
          - Tính đúng điểm trung bình (làm tròn 2 chữ số thập phân).
          - Chỉ tính các đánh giá có TrangThai = 'VISIBLE'.
          - Khi ẩn đánh giá (HIDDEN), view tự động loại trừ và cập nhật lại điểm.
          - Đếm đúng số đơn thành công (COMPLETED).
       3. Ràng buộc toàn vẹn bảng DanhGia:
          - CHECK CK_DanhGia_TrangThai chặn giá trị ngoài VISIBLE/HIDDEN.
   ===================================================================== */

IF OBJECT_ID('tempdb..#KQ') IS NOT NULL DROP TABLE #KQ;
CREATE TABLE #KQ (Id int IDENTITY(1,1), Ten nvarchar(200), Dat bit, ChiTiet nvarchar(500));
GO

-- ------------------------------------------------------------------ DỌN DẸP TRƯỚC KHI TEST
DELETE dg FROM dbo.DanhGia dg
JOIN dbo.DonHang dh ON dh.MaDonHang = dg.MaDonHang
WHERE dh.MaDon LIKE 'c8smoke%' OR dg.NoiDung LIKE 'c8smoke%';

DELETE FROM dbo.ChiTietDonHang WHERE MaDonHang IN (SELECT MaDonHang FROM dbo.DonHang WHERE MaDon LIKE 'c8smoke%');
DELETE FROM dbo.ThanhToan WHERE MaDonHang IN (SELECT MaDonHang FROM dbo.DonHang WHERE MaDon LIKE 'c8smoke%');
DELETE FROM dbo.LichSuTrangThaiDonHang WHERE MaDonHang IN (SELECT MaDonHang FROM dbo.DonHang WHERE MaDon LIKE 'c8smoke%');
DELETE FROM dbo.DonHang WHERE MaDon LIKE 'c8smoke%';
DELETE FROM dbo.SanPham WHERE Ten LIKE 'c8smoke%';
DELETE FROM dbo.CuaHang WHERE TenShop LIKE 'c8smoke%';
DELETE FROM dbo.NguoiDung WHERE Email LIKE 'c8smoke%';
GO

-- ------------------------------------------------------------------ KHỞI TẠO DỮ LIỆU TEST (c8smoke)
DECLARE @pw varchar(256) = REPLICATE('x', 30);
DECLARE @BuyerId int, @OtherBuyerId int, @SellerId int;
DECLARE @ShopId int;
DECLARE @SpId int;
DECLARE @DonCompleted1Id int, @DonCompleted2Id int, @DonPendingId int;

-- Tạo 2 Người mua và 1 Người bán
INSERT dbo.NguoiDung (HoTen, Email, SoDienThoai, MatKhauBam, VaiTro, TrangThai)
VALUES (N'C8 Người Mua', 'c8smoke.buyer@test.local', '0912000001', @pw, 'USER', 'ACTIVE');
SET @BuyerId = SCOPE_IDENTITY();

INSERT dbo.NguoiDung (HoTen, Email, SoDienThoai, MatKhauBam, VaiTro, TrangThai)
VALUES (N'C8 Người Lạ', 'c8smoke.other@test.local', '0912000002', @pw, 'USER', 'ACTIVE');
SET @OtherBuyerId = SCOPE_IDENTITY();

INSERT dbo.NguoiDung (HoTen, Email, SoDienThoai, MatKhauBam, VaiTro, TrangThai)
VALUES (N'C8 Chủ Shop', 'c8smoke.seller@test.local', '0912000003', @pw, 'USER', 'ACTIVE');
SET @SellerId = SCOPE_IDENTITY();

-- Tạo Cửa hàng
INSERT dbo.CuaHang (MaNguoiDung, TenShop, SoDienThoaiLienHe, TrangThai)
VALUES (@SellerId, N'c8smoke Shop Đánh Giá', '0912000003', 'ACTIVE');
SET @ShopId = SCOPE_IDENTITY();

-- Lấy DanhMuc và TinhTrang sẵn có
DECLARE @MaDanhMuc int = (SELECT TOP 1 MaDanhMuc FROM dbo.DanhMuc ORDER BY MaDanhMuc);
DECLARE @MaTinhTrang varchar(20) = (SELECT TOP 1 Ma FROM dbo.TinhTrangSanPham ORDER BY Ma);

-- Tạo 1 sản phẩm
INSERT dbo.SanPham (MaCuaHang, MaDanhMuc, MaTinhTrang, Ten, MoTa, Gia, TrangThai, DaXoa)
VALUES (@ShopId, @MaDanhMuc, @MaTinhTrang, N'c8smoke Sản phẩm Thử Nghiệm', N'Mô tả thử nghiệm đánh giá', 200000, 'ACTIVE', 0);
SET @SpId = SCOPE_IDENTITY();

-- Tạo 3 Đơn hàng:
-- Đơn 1: COMPLETED (dùng để test đánh giá lần 1 & chặn lần 2)
INSERT dbo.DonHang (MaDon, MaNguoiDung, MaCuaHang, TenNguoiNhan, SoDienThoaiNhan, DiaChiNhan, TienHang, PhiVanChuyen, TongTien, TrangThai, HinhThucNhan)
VALUES ('c8smoke-dh01', @BuyerId, @ShopId, N'C8 Người Mua', '0912000001', N'123 Test', 200000, 0, 200000, 'COMPLETED', 'GIAO_HANG');
SET @DonCompleted1Id = SCOPE_IDENTITY();

-- Đơn 2: COMPLETED (dùng để test cộng dồn điểm sao & ẩn đánh giá)
INSERT dbo.DonHang (MaDon, MaNguoiDung, MaCuaHang, TenNguoiNhan, SoDienThoaiNhan, DiaChiNhan, TienHang, PhiVanChuyen, TongTien, TrangThai, HinhThucNhan)
VALUES ('c8smoke-dh02', @BuyerId, @ShopId, N'C8 Người Mua', '0912000001', N'123 Test', 200000, 0, 200000, 'COMPLETED', 'GIAO_HANG');
SET @DonCompleted2Id = SCOPE_IDENTITY();

-- Đơn 3: PENDING (dùng để test chặn đánh giá khi chưa hoàn tất)
INSERT dbo.DonHang (MaDon, MaNguoiDung, MaCuaHang, TenNguoiNhan, SoDienThoaiNhan, DiaChiNhan, TienHang, PhiVanChuyen, TongTien, TrangThai, HinhThucNhan)
VALUES ('c8smoke-dh03', @BuyerId, @ShopId, N'C8 Người Mua', '0912000001', N'123 Test', 200000, 0, 200000, 'PENDING', 'GIAO_HANG');
SET @DonPendingId = SCOPE_IDENTITY();

-- ==================================================================
-- BẮT ĐẦU CÁC TEST CASES
-- ==================================================================

-- ------------------------------------------------------------------ TEST 1: Chặn đơn không tồn tại (50001)
BEGIN TRY
    EXEC dbo.sp_TaoDanhGia @MaDonHang = 999999999, @MaNguoiMua = @BuyerId, @SoSao = 5;
    INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'C8.1 Chan don khong ton tai (50001)', 0, N'Lẽ ra phải chặn lỗi 50001');
END TRY
BEGIN CATCH
    IF ERROR_NUMBER() = 50001
        INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'C8.1 Chan don khong ton tai (50001)', 1, N'Bắt đúng mã lỗi 50001: ' + ERROR_MESSAGE());
    ELSE
        INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'C8.1 Chan don khong ton tai (50001)', 0, N'Sai mã lỗi: ' + CAST(ERROR_NUMBER() AS nvarchar(10)));
END CATCH

-- ------------------------------------------------------------------ TEST 2: Chặn người lạ đánh giá (50002)
BEGIN TRY
    EXEC dbo.sp_TaoDanhGia @MaDonHang = @DonCompleted1Id, @MaNguoiMua = @OtherBuyerId, @SoSao = 5;
    INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'C8.2 Chan nguoi la danh gia (50002)', 0, N'Lẽ ra phải chặn lỗi 50002');
END TRY
BEGIN CATCH
    IF ERROR_NUMBER() = 50002
        INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'C8.2 Chan nguoi la danh gia (50002)', 1, N'Bắt đúng mã lỗi 50002: ' + ERROR_MESSAGE());
    ELSE
        INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'C8.2 Chan nguoi la danh gia (50002)', 0, N'Sai mã lỗi: ' + CAST(ERROR_NUMBER() AS nvarchar(10)));
END CATCH

-- ------------------------------------------------------------------ TEST 3: Chặn đơn chưa hoàn tất COMPLETED (50003)
BEGIN TRY
    EXEC dbo.sp_TaoDanhGia @MaDonHang = @DonPendingId, @MaNguoiMua = @BuyerId, @SoSao = 5;
    INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'C8.3 Chan don chua hoan tat (50003)', 0, N'Lẽ ra phải chặn lỗi 50003');
END TRY
BEGIN CATCH
    IF ERROR_NUMBER() = 50003
        INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'C8.3 Chan don chua hoan tat (50003)', 1, N'Bắt đúng mã lỗi 50003: ' + ERROR_MESSAGE());
    ELSE
        INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'C8.3 Chan don chua hoan tat (50003)', 0, N'Sai mã lỗi: ' + CAST(ERROR_NUMBER() AS nvarchar(10)));
END CATCH

-- ------------------------------------------------------------------ TEST 4: Chặn số sao không hợp lệ (50005)
BEGIN TRY
    EXEC dbo.sp_TaoDanhGia @MaDonHang = @DonCompleted1Id, @MaNguoiMua = @BuyerId, @SoSao = 6;
    INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'C8.4 Chan so sao ngoai 1..5 (50005)', 0, N'Lẽ ra phải chặn số sao = 6');
END TRY
BEGIN CATCH
    IF ERROR_NUMBER() = 50005
        INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'C8.4 Chan so sao ngoai 1..5 (50005)', 1, N'Bắt đúng mã lỗi 50005: ' + ERROR_MESSAGE());
    ELSE
        INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'C8.4 Chan so sao ngoai 1..5 (50005)', 0, N'Sai mã lỗi: ' + CAST(ERROR_NUMBER() AS nvarchar(10)));
END CATCH

-- ------------------------------------------------------------------ TEST 5: Tạo đánh giá hợp lệ lần 1 (5 sao)
BEGIN TRY
    EXEC dbo.sp_TaoDanhGia
        @MaDonHang = @DonCompleted1Id,
        @MaNguoiMua = @BuyerId,
        @SoSao = 5,
        @NoiDung = N'c8smoke Sản phẩm rất đẹp, đóng gói cẩn thận!';

    IF EXISTS (
        SELECT 1 FROM dbo.DanhGia
        WHERE MaDonHang = @DonCompleted1Id
          AND MaNguoiMua = @BuyerId
          AND MaCuaHang = @ShopId
          AND SoSao = 5
          AND TrangThai = 'VISIBLE'
    )
        INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'C8.5 Tao danh gia hop le lan 1', 1, N'Đã tạo đánh giá 5 sao thành công với TrangThai = VISIBLE');
    ELSE
        INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'C8.5 Tao danh gia hop le lan 1', 0, N'Không tìm thấy bản ghi trong bảng DanhGia');
END TRY
BEGIN CATCH
    INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'C8.5 Tao danh gia hop le lan 1', 0, ERROR_MESSAGE());
END CATCH

-- ------------------------------------------------------------------ TEST 6: Chặn đánh giá lần 2 trên cùng một đơn (50004)
BEGIN TRY
    EXEC dbo.sp_TaoDanhGia
        @MaDonHang = @DonCompleted1Id,
        @MaNguoiMua = @BuyerId,
        @SoSao = 4,
        @NoiDung = N'c8smoke Đánh giá lần 2 cố tình spam';
    INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'C8.6 Chan danh gia trung lap (50004)', 0, N'Lẽ ra phải chặn đánh giá lần 2');
END TRY
BEGIN CATCH
    IF ERROR_NUMBER() IN (50004, 2601, 2627)
        INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'C8.6 Chan danh gia trung lap (50004)', 1, N'Bắt đúng lỗi chặn đánh giá lần 2: ' + ERROR_MESSAGE());
    ELSE
        INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'C8.6 Chan danh gia trung lap (50004)', 0, N'Sai mã lỗi: ' + CAST(ERROR_NUMBER() AS nvarchar(10)));
END CATCH

-- ------------------------------------------------------------------ TEST 7: Tạo đánh giá 3 sao cho đơn 2 & Kiểm tra vw_DiemCuaHang tính đúng 4.00 sao
BEGIN TRY
    EXEC dbo.sp_TaoDanhGia
        @MaDonHang = @DonCompleted2Id,
        @MaNguoiMua = @BuyerId,
        @SoSao = 3,
        @NoiDung = N'c8smoke Tạm ổn, giao hàng hơi trễ một chút.';

    DECLARE @Diem decimal(3,2), @SoDG int, @SoDonTC int;
    SELECT @Diem = DiemTrungBinh, @SoDG = SoDanhGia, @SoDonTC = SoDonThanhCong
    FROM dbo.vw_DiemCuaHang
    WHERE MaCuaHang = @ShopId;

    -- Kỳ vọng: (5 + 3) / 2 = 4.00, SoDanhGia = 2, SoDonThanhCong = 2
    IF @Diem = 4.00 AND @SoDG = 2 AND @SoDonTC = 2
        INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'C8.7 vw_DiemCuaHang tinh dung diem TB (4.00 sao)', 1,
            N'Điểm TB = 4.00, Số đánh giá = 2, Số đơn COMPLETED = 2');
    ELSE
        INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'C8.7 vw_DiemCuaHang tinh dung diem TB (4.00 sao)', 0,
            N'Sai kết quả view: Diem=' + CAST(@Diem AS nvarchar(10)) + N', SoDG=' + CAST(@SoDG AS nvarchar(10)) + N', SoDon=' + CAST(@SoDonTC AS nvarchar(10)));
END TRY
BEGIN CATCH
    INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'C8.7 vw_DiemCuaHang tinh dung diem TB (4.00 sao)', 0, ERROR_MESSAGE());
END CATCH

-- ------------------------------------------------------------------ TEST 8: Ẩn đánh giá (HIDDEN) & Kiểm tra vw_DiemCuaHang tự động loại trừ
BEGIN TRY
    -- Ẩn đánh giá 3 sao của đơn 2
    UPDATE dbo.DanhGia
    SET TrangThai = 'HIDDEN'
    WHERE MaDonHang = @DonCompleted2Id;

    DECLARE @DiemSauAn decimal(3,2), @SoDGSauAn int;
    SELECT @DiemSauAn = DiemTrungBinh, @SoDGSauAn = SoDanhGia
    FROM dbo.vw_DiemCuaHang
    WHERE MaCuaHang = @ShopId;

    -- Sau khi ẩn 3 sao: chỉ còn đánh giá 5 sao của đơn 1 => Điểm = 5.00, Số đánh giá = 1
    IF @DiemSauAn = 5.00 AND @SoDGSauAn = 1
        INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'C8.8 An danh gia (HIDDEN) vw_DiemCuaHang tu cap nhat', 1,
            N'Sau khi ẩn đánh giá 3 sao: Điểm TB tự cập nhật thành 5.00, Số đánh giá VISIBLE = 1');
    ELSE
        INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'C8.8 An danh gia (HIDDEN) vw_DiemCuaHang tu cap nhat', 0,
            N'Sai kết quả sau ẩn: Diem=' + CAST(@DiemSauAn AS nvarchar(10)) + N', SoDG=' + CAST(@SoDGSauAn AS nvarchar(10)));
END TRY
BEGIN CATCH
    INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'C8.8 An danh gia (HIDDEN) vw_DiemCuaHang tu cap nhat', 0, ERROR_MESSAGE());
END CATCH

-- ------------------------------------------------------------------ TEST 9: Ràng buộc CHECK CK_DanhGia_TrangThai
BEGIN TRY
    INSERT INTO dbo.DanhGia (MaDonHang, MaNguoiMua, MaCuaHang, SoSao, TrangThai)
    VALUES (@DonPendingId, @BuyerId, @ShopId, 5, 'INVALID_VAL');

    INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'C8.9 Rang buoc CK_DanhGia_TrangThai chan gia tri la', 0, N'Lẽ ra phải chặn giá trị TrangThai = INVALID_VAL');
END TRY
BEGIN CATCH
    IF ERROR_NUMBER() = 547 -- Check constraint violation
        INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'C8.9 Rang buoc CK_DanhGia_TrangThai chan gia tri la', 1, N'Bắt đúng CHECK constraint CK_DanhGia_TrangThai');
    ELSE
        INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'C8.9 Rang buoc CK_DanhGia_TrangThai chan gia tri la', 0, ERROR_MESSAGE());
END CATCH

-- ------------------------------------------------------------------ DỌN DẸP SAU KHI TEST (ĐÚNG THỨ TỰ KHÓA NGOẠI)
DELETE dg FROM dbo.DanhGia dg
JOIN dbo.DonHang dh ON dh.MaDonHang = dg.MaDonHang
WHERE dh.MaDon LIKE 'c8smoke%' OR dg.NoiDung LIKE 'c8smoke%';

DELETE FROM dbo.ChiTietDonHang WHERE MaDonHang IN (SELECT MaDonHang FROM dbo.DonHang WHERE MaDon LIKE 'c8smoke%');
DELETE FROM dbo.ThanhToan WHERE MaDonHang IN (SELECT MaDonHang FROM dbo.DonHang WHERE MaDon LIKE 'c8smoke%');
DELETE FROM dbo.LichSuTrangThaiDonHang WHERE MaDonHang IN (SELECT MaDonHang FROM dbo.DonHang WHERE MaDon LIKE 'c8smoke%');
DELETE FROM dbo.DonHang WHERE MaDon LIKE 'c8smoke%';
DELETE FROM dbo.SanPham WHERE Ten LIKE 'c8smoke%';
DELETE FROM dbo.CuaHang WHERE TenShop LIKE 'c8smoke%';
DELETE FROM dbo.NguoiDung WHERE Email LIKE 'c8smoke%';
GO

-- ------------------------------------------------------------------ IN BẢNG KẾT QUẢ
SELECT Id, Ten, Dat, ChiTiet FROM #KQ ORDER BY Id;

IF EXISTS (SELECT 1 FROM #KQ WHERE Dat = 0)
BEGIN
    RAISERROR(N'CÓ TEST THẤT BẠI TRONG C8_SMOKE_TEST!', 16, 1);
END
ELSE
BEGIN
    PRINT N'TẤT CẢ TEST MODULE C (C8_SMOKE_TEST) ĐỀU THÀNH CÔNG (PASS)!';
END
GO
