SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ARITHABORT ON;
SET NOCOUNT ON;
SET XACT_ABORT OFF;
GO
USE SecondHandMarketplace;
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

/* =====================================================================
   C8 SMOKE TEST (SQL Server) - Module Đánh giá & Uy tín Shop (Người C)
   Chạy sau khi đã áp dụng 01..11:
       sqlcmd -S . -d SecondHandMarketplace -i database\tests\c8_smoke_test.sql -f 65001
   - Kiểm tra Stored Procedure sp_TaoDanhGia (chặn đơn chưa COMPLETED, chặn sai người mua,
     chặn đánh giá 2 lần, chặn số sao ngoài 1..5).
   - Kiểm tra View vw_DiemCuaHang (tính điểm trung bình, số đánh giá, tự động loại trừ HIDDEN).
   - Tự tạo dữ liệu tiền tố 'c8smoke', chạy xong tự dọn dẹp sạch sẽ.
   - In bảng PASS/FAIL ở cuối. Có FAIL => THROW lỗi.
   ===================================================================== */

IF OBJECT_ID('tempdb..#KQ') IS NOT NULL DROP TABLE #KQ;
CREATE TABLE #KQ (Id int IDENTITY(1,1), Ten nvarchar(200), Dat bit, ChiTiet nvarchar(500));
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- ------------------------------------------------------------------ DỌN DẸP DỮ LIỆU CŨ NẾU CÒN TỒN TẠI
DELETE dg FROM dbo.DanhGia dg
JOIN dbo.DonHang dh ON dh.MaDonHang = dg.MaDonHang
WHERE dh.MaDon LIKE 'c8smoke%';

DELETE ct FROM dbo.ChiTietDonHang ct
JOIN dbo.DonHang dh ON dh.MaDonHang = ct.MaDonHang
WHERE dh.MaDon LIKE 'c8smoke%';

DELETE ls FROM dbo.LichSuTrangThaiDonHang ls
JOIN dbo.DonHang dh ON dh.MaDonHang = ls.MaDonHang
WHERE dh.MaDon LIKE 'c8smoke%';

DELETE tt FROM dbo.ThanhToan tt
JOIN dbo.DonHang dh ON dh.MaDonHang = tt.MaDonHang
WHERE dh.MaDon LIKE 'c8smoke%';

DELETE FROM dbo.DonHang WHERE MaDon LIKE 'c8smoke%';
DELETE FROM dbo.SanPham WHERE Ten LIKE N'c8smoke%';
DELETE FROM dbo.CuaHang WHERE TenShop LIKE N'c8smoke%';
DELETE FROM dbo.NguoiDung WHERE Email LIKE 'c8smoke%';
DELETE FROM dbo.DanhMuc WHERE Ten = N'c8smoke-danhmuc';
DELETE FROM dbo.TinhTrangSanPham WHERE Ma = 'C8SMOKE';
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- ------------------------------------------------------------------ DỮ LIỆU NỀN GIẢ LẬP
DECLARE @pw varchar(256) = REPLICATE('x', 30);
DECLARE @Now datetime2(3) = SYSUTCDATETIME();

-- 1. Tạo Người bán, Người mua 1, Người mua 2
INSERT dbo.NguoiDung (HoTen, Email, SoDienThoai, MatKhauBam, VaiTro, TrangThai)
VALUES 
    (N'C8 Seller', 'c8smoke.seller@test.local', '0988000001', @pw, 'USER', 'ACTIVE'),
    (N'C8 Buyer 1', 'c8smoke.buyer1@test.local', '0988000002', @pw, 'USER', 'ACTIVE'),
    (N'C8 Buyer 2', 'c8smoke.buyer2@test.local', '0988000003', @pw, 'USER', 'ACTIVE');

DECLARE @SellerId int = (SELECT MaNguoiDung FROM dbo.NguoiDung WHERE Email = 'c8smoke.seller@test.local');
DECLARE @Buyer1Id int = (SELECT MaNguoiDung FROM dbo.NguoiDung WHERE Email = 'c8smoke.buyer1@test.local');
DECLARE @Buyer2Id int = (SELECT MaNguoiDung FROM dbo.NguoiDung WHERE Email = 'c8smoke.buyer2@test.local');

-- 2. Tạo Shop
INSERT dbo.CuaHang (MaNguoiDung, TenShop, SoDienThoaiLienHe, TrangThai)
VALUES (@SellerId, N'c8smoke Shop Uy Tín', '0988000001', 'ACTIVE');
DECLARE @ShopId int = (SELECT MaCuaHang FROM dbo.CuaHang WHERE MaNguoiDung = @SellerId);

-- 3. Tạo Danh mục, Tình trạng và Sản phẩm
INSERT dbo.DanhMuc (Ten) VALUES (N'c8smoke-danhmuc');
DECLARE @DmId int = (SELECT MaDanhMuc FROM dbo.DanhMuc WHERE Ten = N'c8smoke-danhmuc');

INSERT dbo.TinhTrangSanPham (Ma, Ten) VALUES ('C8SMOKE', N'c8smoke Tình trạng');
DECLARE @TtId int = (SELECT MaTinhTrang FROM dbo.TinhTrangSanPham WHERE Ma = 'C8SMOKE');

INSERT dbo.SanPham (MaCuaHang, MaDanhMuc, MaTinhTrang, Ten, MoTa, Gia, TrangThai)
VALUES (@ShopId, @DmId, @TtId, N'c8smoke Laptop Cũ', N'Mô tả sản phẩm thử nghiệm cho smoke test C8', 5000000, 'ACTIVE');
DECLARE @SpId int = SCOPE_IDENTITY();

-- 4. Tạo 3 Đơn hàng mẫu:
--   - Đơn 1 (c8smoke-DH01): PENDING, Người mua 1
--   - Đơn 2 (c8smoke-DH02): COMPLETED, Người mua 1
--   - Đơn 3 (c8smoke-DH03): COMPLETED, Người mua 2
INSERT dbo.DonHang (MaDon, MaNguoiDung, MaCuaHang, TenNguoiNhan, SoDienThoaiNhan, DiaChiNhan, TienHang, TongTien, TrangThai, NgayTao)
VALUES 
    ('c8smoke-DH01', @Buyer1Id, @ShopId, N'Người nhận 1', '0988000002', N'Địa chỉ 1', 5000000, 5000000, 'PENDING', DATEADD(DAY, -2, @Now)),
    ('c8smoke-DH02', @Buyer1Id, @ShopId, N'Người nhận 1', '0988000002', N'Địa chỉ 1', 5000000, 5000000, 'COMPLETED', DATEADD(DAY, -2, @Now)),
    ('c8smoke-DH03', @Buyer2Id, @ShopId, N'Người nhận 2', '0988000003', N'Địa chỉ 2', 5000000, 5000000, 'COMPLETED', DATEADD(DAY, -1, @Now));

GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- ------------------------------------------------------------------ THỰC THI CÁC TEST CASES
DECLARE @Buyer1Id int = (SELECT MaNguoiDung FROM dbo.NguoiDung WHERE Email = 'c8smoke.buyer1@test.local');
DECLARE @Buyer2Id int = (SELECT MaNguoiDung FROM dbo.NguoiDung WHERE Email = 'c8smoke.buyer2@test.local');
DECLARE @ShopId   int = (SELECT ch.MaCuaHang FROM dbo.CuaHang ch JOIN dbo.NguoiDung nd ON nd.MaNguoiDung = ch.MaNguoiDung WHERE nd.Email = 'c8smoke.seller@test.local');

DECLARE @Don1Id int = (SELECT MaDonHang FROM dbo.DonHang WHERE MaDon = 'c8smoke-DH01'); -- PENDING
DECLARE @Don2Id int = (SELECT MaDonHang FROM dbo.DonHang WHERE MaDon = 'c8smoke-DH02'); -- COMPLETED
DECLARE @Don3Id int = (SELECT MaDonHang FROM dbo.DonHang WHERE MaDon = 'c8smoke-DH03'); -- COMPLETED

-- ===== TC01: Chặn đánh giá đơn chưa COMPLETED (TrangThai = 'PENDING') -> Lỗi 50003
BEGIN TRY
    EXEC dbo.sp_TaoDanhGia @MaDonHang = @Don1Id, @MaNguoiMua = @Buyer1Id, @SoSao = 5, @NoiDung = N'Đơn chưa xong mà đánh giá';
    INSERT #KQ VALUES (N'TC01 Chặn đánh giá đơn chưa COMPLETED', 0, N'Không chặn, lại tạo thành công');
END TRY
BEGIN CATCH
    INSERT #KQ VALUES (N'TC01 Chặn đánh giá đơn chưa COMPLETED', 
        IIF(ERROR_NUMBER() = 50003, 1, 0), 
        CONCAT(N'Mã lỗi: ', ERROR_NUMBER(), N' - ', ERROR_MESSAGE()));
END CATCH;

-- ===== TC02: Chặn sai người mua (Buyer2 cố đánh giá Don2 của Buyer1) -> Lỗi 50002
BEGIN TRY
    EXEC dbo.sp_TaoDanhGia @MaDonHang = @Don2Id, @MaNguoiMua = @Buyer2Id, @SoSao = 5, @NoiDung = N'Người lạ đánh giá';
    INSERT #KQ VALUES (N'TC02 Chặn người đánh giá không phải người mua của đơn', 0, N'Không chặn sai người mua');
END TRY
BEGIN CATCH
    INSERT #KQ VALUES (N'TC02 Chặn người đánh giá không phải người mua của đơn', 
        IIF(ERROR_NUMBER() = 50002, 1, 0), 
        CONCAT(N'Mã lỗi: ', ERROR_NUMBER(), N' - ', ERROR_MESSAGE()));
END CATCH;

-- ===== TC03: Chặn số sao không hợp lệ (SoSao = 0 hoặc SoSao = 6) -> Lỗi 50005
BEGIN TRY
    EXEC dbo.sp_TaoDanhGia @MaDonHang = @Don2Id, @MaNguoiMua = @Buyer1Id, @SoSao = 0, @NoiDung = N'0 sao';
    INSERT #KQ VALUES (N'TC03 Chặn số sao < 1 hoặc > 5', 0, N'Cho phép số sao = 0');
END TRY
BEGIN CATCH
    INSERT #KQ VALUES (N'TC03 Chặn số sao < 1 hoặc > 5', 
        IIF(ERROR_NUMBER() = 50005, 1, 0), 
        CONCAT(N'Mã lỗi: ', ERROR_NUMBER(), N' - ', ERROR_MESSAGE()));
END CATCH;

-- ===== TC04: Đánh giá hợp lệ lần đầu cho Đơn 2 (5 sao) -> Thành công
BEGIN TRY
    EXEC dbo.sp_TaoDanhGia @MaDonHang = @Don2Id, @MaNguoiMua = @Buyer1Id, @SoSao = 5, @NoiDung = N'Sản phẩm rất tốt, giao nhanh!';
    
    -- Kiểm tra bản ghi trong bảng DanhGia
    IF EXISTS (SELECT 1 FROM dbo.DanhGia WHERE MaDonHang = @Don2Id AND SoSao = 5 AND TrangThai = 'VISIBLE')
        INSERT #KQ VALUES (N'TC04 Tạo đánh giá hợp lệ lần đầu (5 sao)', 1, N'Tạo thành công, bản ghi lưu đúng bảng DanhGia');
    ELSE
        INSERT #KQ VALUES (N'TC04 Tạo đánh giá hợp lệ lần đầu (5 sao)', 0, N'Không tìm thấy bản ghi trong bảng DanhGia');
END TRY
BEGIN CATCH
    INSERT #KQ VALUES (N'TC04 Tạo đánh giá hợp lệ lần đầu (5 sao)', 0, ERROR_MESSAGE());
END CATCH;

-- ===== TC05: Chặn đánh giá lần 2 trên cùng 1 đơn (Đơn 2) -> Lỗi 50004
BEGIN TRY
    EXEC dbo.sp_TaoDanhGia @MaDonHang = @Don2Id, @MaNguoiMua = @Buyer1Id, @SoSao = 4, @NoiDung = N'Cố tình đánh giá lần 2';
    INSERT #KQ VALUES (N'TC05 Chặn đánh giá lần 2 trên cùng một đơn hàng', 0, N'Không chặn, bị trùng lặp đánh giá');
END TRY
BEGIN CATCH
    INSERT #KQ VALUES (N'TC05 Chặn đánh giá lần 2 trên cùng một đơn hàng', 
        IIF(ERROR_NUMBER() = 50004, 1, 0), 
        CONCAT(N'Mã lỗi: ', ERROR_NUMBER(), N' - ', ERROR_MESSAGE()));
END CATCH;

-- ===== TC06: Tạo thêm đánh giá Đơn 3 (3 sao) & Kiểm tra view vw_DiemCuaHang tính đúng điểm
-- Don2 (5 sao) + Don3 (3 sao) => Trung bình = (5 + 3)/2 = 4.00, SoDanhGia = 2, SoDonThanhCong = 2
BEGIN TRY
    EXEC dbo.sp_TaoDanhGia @MaDonHang = @Don3Id, @MaNguoiMua = @Buyer2Id, @SoSao = 3, @NoiDung = N'Dùng tạm được';
    
    DECLARE @DiemTB decimal(3,2), @SoDG int, @SoDonTC int;
    SELECT @DiemTB = DiemTrungBinh, @SoDG = SoDanhGia, @SoDonTC = SoDonThanhCong
    FROM dbo.vw_DiemCuaHang
    WHERE MaCuaHang = @ShopId;
    
    IF @DiemTB = 4.00 AND @SoDG = 2 AND @SoDonTC = 2
        INSERT #KQ VALUES (N'TC06 vw_DiemCuaHang tính đúng điểm trung bình (4.00 sao, 2 đơn, 2 đánh giá)', 1, 
            CONCAT(N'DiemTB=', @DiemTB, N', SoDanhGia=', @SoDG, N', SoDonThanhCong=', @SoDonTC));
    ELSE
        INSERT #KQ VALUES (N'TC06 vw_DiemCuaHang tính đúng điểm trung bình', 0, 
            CONCAT(N'Sai lệch: DiemTB=', ISNULL(CAST(@DiemTB AS nvarchar(10)), 'NULL'), 
                   N' (kỳ vọng 4.00), SoDG=', ISNULL(CAST(@SoDG AS nvarchar(10)), 'NULL'), N' (kỳ vọng 2)'));
END TRY
BEGIN CATCH
    INSERT #KQ VALUES (N'TC06 vw_DiemCuaHang tính đúng điểm trung bình', 0, ERROR_MESSAGE());
END CATCH;

-- ===== TC07: Đổi đánh giá Đơn 3 sang HIDDEN -> vw_DiemCuaHang tự động loại trừ khỏi điểm trung bình
-- Đánh giá 3 sao bị ẩn -> Shop chỉ còn 1 đánh giá VISIBLE 5 sao => DiemTB = 5.00, SoDanhGia = 1, SoDonThanhCong = 2
BEGIN TRY
    UPDATE dbo.DanhGia SET TrangThai = 'HIDDEN' WHERE MaDonHang = @Don3Id;
    
    DECLARE @DiemMoi decimal(3,2), @SoDGMoi int;
    SELECT @DiemMoi = DiemTrungBinh, @SoDGMoi = SoDanhGia
    FROM dbo.vw_DiemCuaHang
    WHERE MaCuaHang = @ShopId;
    
    IF @DiemMoi = 5.00 AND @SoDGMoi = 1
        INSERT #KQ VALUES (N'TC07 vw_DiemCuaHang tự động loại trừ đánh giá HIDDEN (tính lại 5.00 sao, 1 đánh giá)', 1, 
            CONCAT(N'DiemTB sau khi ẩn=', @DiemMoi, N', SoDanhGia=', @SoDGMoi));
    ELSE
        INSERT #KQ VALUES (N'TC07 vw_DiemCuaHang tự động loại trừ đánh giá HIDDEN', 0, 
            CONCAT(N'Sai lệch: DiemTB=', ISNULL(CAST(@DiemMoi AS nvarchar(10)), 'NULL'), 
                   N' (kỳ vọng 5.00), SoDG=', ISNULL(CAST(@SoDGMoi AS nvarchar(10)), 'NULL'), N' (kỳ vọng 1)'));
END TRY
BEGIN CATCH
    INSERT #KQ VALUES (N'TC07 vw_DiemCuaHang tự động loại trừ đánh giá HIDDEN', 0, ERROR_MESSAGE());
END CATCH;

-- ===== TC08: Ràng buộc CK_DanhGia_TrangThai chặn giá trị bất hợp lệ (ví dụ 'DELETED')
BEGIN TRY
    UPDATE dbo.DanhGia SET TrangThai = 'DELETED' WHERE MaDonHang = @Don3Id;
    INSERT #KQ VALUES (N'TC08 CK_DanhGia_TrangThai chặn trạng thái ngoài VISIBLE/HIDDEN', 0, N'Cho phép cập nhật DELETED');
END TRY
BEGIN CATCH
    INSERT #KQ VALUES (N'TC08 CK_DanhGia_TrangThai chặn trạng thái ngoài VISIBLE/HIDDEN', 
        IIF(ERROR_NUMBER() = 547, 1, 0), 
        CONCAT(N'Mã lỗi: ', ERROR_NUMBER(), N' - ', ERROR_MESSAGE()));
END CATCH;

GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- ------------------------------------------------------------------ BÁO CÁO KẾT QUẢ & DỌN DẸP
DECLARE @SoDat int = (SELECT COUNT(*) FROM #KQ WHERE Dat = 1);
DECLARE @TongSo int = (SELECT COUNT(*) FROM #KQ);
DECLARE @SoTruot int = (SELECT COUNT(*) FROM #KQ WHERE Dat = 0);

PRINT N'=====================================================================';
PRINT CONCAT(N'  KẾT QUẢ C8 SMOKE TEST: ', @SoDat, N'/', @TongSo, N' TEST CASES ĐẠT');
PRINT N'=====================================================================';

SELECT Id, Ten, IIF(Dat = 1, N'PASS', N'FAIL') AS KetQua, ChiTiet FROM #KQ ORDER BY Id;

-- Dọn dẹp sạch sẽ toàn bộ dữ liệu c8smoke
DELETE dg FROM dbo.DanhGia dg JOIN dbo.DonHang dh ON dh.MaDonHang = dg.MaDonHang WHERE dh.MaDon LIKE 'c8smoke%';
DELETE ct FROM dbo.ChiTietDonHang ct JOIN dbo.DonHang dh ON dh.MaDonHang = ct.MaDonHang WHERE dh.MaDon LIKE 'c8smoke%';
DELETE ls FROM dbo.LichSuTrangThaiDonHang ls JOIN dbo.DonHang dh ON dh.MaDonHang = ls.MaDonHang WHERE dh.MaDon LIKE 'c8smoke%';
DELETE tt FROM dbo.ThanhToan tt JOIN dbo.DonHang dh ON dh.MaDonHang = tt.MaDonHang WHERE dh.MaDon LIKE 'c8smoke%';
DELETE FROM dbo.DonHang WHERE MaDon LIKE 'c8smoke%';
DELETE FROM dbo.SanPham WHERE Ten LIKE N'c8smoke%';
DELETE FROM dbo.CuaHang WHERE TenShop LIKE N'c8smoke%';
DELETE FROM dbo.NguoiDung WHERE Email LIKE 'c8smoke%';
DELETE FROM dbo.DanhMuc WHERE Ten = N'c8smoke-danhmuc';
DELETE FROM dbo.TinhTrangSanPham WHERE Ma = 'C8SMOKE';

DROP TABLE #KQ;

IF @SoTruot > 0
BEGIN
    THROW 50999, N'C8 Smoke Test phát hiện lỗi không đạt yêu cầu.', 1;
END
ELSE
BEGIN
    PRINT N'>> CHÚC MỪNG: Toàn bộ Stored Procedure và View của Module Đánh giá (C) đều đạt chuẩn!';
END
GO
