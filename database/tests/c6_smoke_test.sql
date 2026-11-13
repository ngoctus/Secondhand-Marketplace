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
SET ARITHABORT ON;
GO

/* =====================================================================
   C6 SMOKE TEST (SQL Server) - Module Báo cáo vi phạm & Tranh chấp (Người C)
   Chạy sau khi đã áp dụng 01..12:
       sqlcmd -S . -d SecondHandMarketplace -i database\tests\c6_smoke_test.sql -f 65001
   - Kiểm tra Báo cáo vi phạm (BaoCao): tạo báo cáo, chống gửi trùng khi PENDING,
     cho phép gửi lại sau khi DISMISSED, Admin xử lý (DISMISS, RESOLVE).
   - Kiểm tra Tranh chấp (TranhChap): tạo tranh chấp đơn hàng, chống mở trùng trên
     cùng 1 đơn hàng (UQ_TranhChap_MaDonHang), shop gửi phản hồi, Admin phân quyết.
   - Tự tạo dữ liệu tiền tố 'c6smoke', chạy xong tự dọn dẹp sạch sẽ.
   - In bảng PASS/FAIL ở cuối. Có FAIL => THROW lỗi.
   ===================================================================== */

IF OBJECT_ID('tempdb..#KQ') IS NOT NULL DROP TABLE #KQ;
CREATE TABLE #KQ (Id int IDENTITY(1,1), Ten nvarchar(200), Dat bit, ChiTiet nvarchar(500));
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ARITHABORT ON;
GO

-- ------------------------------------------------------------------ DỌN DẸP DỮ LIỆU CŨ NẾU CÒN TỒN TẠI
DELETE tc FROM dbo.TranhChap tc
JOIN dbo.DonHang dh ON dh.MaDonHang = tc.MaDonHang
WHERE dh.MaDon LIKE 'c6smoke%';

DELETE FROM dbo.TranhChap WHERE LyDo LIKE N'c6smoke%';
DELETE FROM dbo.BaoCao WHERE LyDo LIKE N'c6smoke%';

DELETE ct FROM dbo.ChiTietDonHang ct
JOIN dbo.DonHang dh ON dh.MaDonHang = ct.MaDonHang
WHERE dh.MaDon LIKE 'c6smoke%';

DELETE ls FROM dbo.LichSuTrangThaiDonHang ls
JOIN dbo.DonHang dh ON dh.MaDonHang = ls.MaDonHang
WHERE dh.MaDon LIKE 'c6smoke%';

DELETE tt FROM dbo.ThanhToan tt
JOIN dbo.DonHang dh ON dh.MaDonHang = tt.MaDonHang
WHERE dh.MaDon LIKE 'c6smoke%';

DELETE FROM dbo.DonHang WHERE MaDon LIKE 'c6smoke%';
DELETE FROM dbo.SanPham WHERE Ten LIKE N'c6smoke%';
DELETE FROM dbo.CuaHang WHERE TenShop LIKE N'c6smoke%';
DELETE FROM dbo.NguoiDung WHERE Email LIKE 'c6smoke%';
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ARITHABORT ON;
GO

-- ------------------------------------------------------------------ DỮ LIỆU NỀN GIẢ LẬP
DECLARE @pw varchar(256) = REPLICATE('x', 30);
DECLARE @Now datetime2(3) = SYSUTCDATETIME();

-- 1. Tạo Admin, Người bán, Người mua
INSERT dbo.NguoiDung (HoTen, Email, SoDienThoai, MatKhauBam, VaiTro, TrangThai)
VALUES 
    (N'C6 Admin',  'c6smoke.admin@test.local',  '0977000001', @pw, 'ADMIN', 'ACTIVE'),
    (N'C6 Seller', 'c6smoke.seller@test.local', '0977000002', @pw, 'USER',  'ACTIVE'),
    (N'C6 Buyer',  'c6smoke.buyer@test.local',  '0977000003', @pw, 'USER',  'ACTIVE');

DECLARE @AdminId  int = (SELECT MaNguoiDung FROM dbo.NguoiDung WHERE Email = 'c6smoke.admin@test.local');
DECLARE @SellerId int = (SELECT MaNguoiDung FROM dbo.NguoiDung WHERE Email = 'c6smoke.seller@test.local');
DECLARE @BuyerId  int = (SELECT MaNguoiDung FROM dbo.NguoiDung WHERE Email = 'c6smoke.buyer@test.local');

-- 2. Tạo Shop
INSERT dbo.CuaHang (MaNguoiDung, TenShop, SoDienThoaiLienHe, MoTa, TrangThai)
VALUES (@SellerId, N'c6smoke Shop Uy Tin', '0977000002', N'Shop phục vụ test C6', 'ACTIVE');
DECLARE @ShopId int = SCOPE_IDENTITY();

-- 3. Lấy Danh mục & Tình trạng có sẵn
DECLARE @MaDanhMuc int = (SELECT TOP 1 MaDanhMuc FROM dbo.DanhMuc WHERE DangHienThi = 1 ORDER BY MaDanhMuc);
DECLARE @MaTinhTrang int = (SELECT TOP 1 MaTinhTrang FROM dbo.TinhTrangSanPham WHERE DangSuDung = 1 ORDER BY MaTinhTrang);

-- 4. Tạo Sản phẩm
INSERT dbo.SanPham (MaCuaHang, MaDanhMuc, MaTinhTrang, Ten, Gia, TrangThai, NgayTao)
VALUES (@ShopId, @MaDanhMuc, @MaTinhTrang, N'c6smoke Sản phẩm test vi phạm', 250000, 'ACTIVE', @Now);
DECLARE @SpId int = SCOPE_IDENTITY();

-- 5. Tạo Đơn hàng
INSERT dbo.DonHang (MaDon, MaNguoiDung, MaCuaHang, TenNguoiNhan, SoDienThoaiNhan, DiaChiNhan, TienHang, PhiVanChuyen, TongTien, PhuongThucThanhToan, TrangThai, NgayTao)
VALUES ('c6smoke-DH001', @BuyerId, @ShopId, N'C6 Buyer', '0977000003', N'TP. Hồ Chí Minh', 250000, 0, 250000, 'COD', 'DELIVERED', @Now);
DECLARE @DonHangId int = SCOPE_IDENTITY();
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ARITHABORT ON;
GO

-- =============================================================================
-- TEST 1: Tạo báo cáo vi phạm hợp lệ (PRODUCT)
-- =============================================================================
DECLARE @BuyerId int = (SELECT MaNguoiDung FROM dbo.NguoiDung WHERE Email = 'c6smoke.buyer@test.local');
DECLARE @SpId int = (SELECT MaSanPham FROM dbo.SanPham WHERE Ten LIKE N'c6smoke%');

BEGIN TRY
    INSERT INTO dbo.BaoCao (LoaiDoiTuong, MaDoiTuong, MaNguoiBaoCao, LyDo, MoTa, TrangThai)
    VALUES ('PRODUCT', @SpId, @BuyerId, N'c6smoke Báo cáo hàng giả', N'Mô tả chi tiết vi phạm sản phẩm', 'PENDING');

    DECLARE @BaoCaoId int = SCOPE_IDENTITY();
    IF EXISTS (SELECT 1 FROM dbo.BaoCao WHERE MaBaoCao = @BaoCaoId AND TrangThai = 'PENDING')
        INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'C6.01 - Tạo báo cáo vi phạm PRODUCT hợp lệ', 1, N'Báo cáo tạo thành công, trạng thái PENDING');
    ELSE
        INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'C6.01 - Tạo báo cáo vi phạm PRODUCT hợp lệ', 0, N'Không tìm thấy bản ghi vừa tạo');
END TRY
BEGIN CATCH
    INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'C6.01 - Tạo báo cáo vi phạm PRODUCT hợp lệ', 0, ERROR_MESSAGE());
END CATCH
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ARITHABORT ON;
GO

-- =============================================================================
-- TEST 2: Chống gửi trùng báo cáo khi đang PENDING (UQ_BaoCao_ChongTrung)
-- =============================================================================
DECLARE @BuyerId int = (SELECT MaNguoiDung FROM dbo.NguoiDung WHERE Email = 'c6smoke.buyer@test.local');
DECLARE @SpId int = (SELECT MaSanPham FROM dbo.SanPham WHERE Ten LIKE N'c6smoke%');

BEGIN TRY
    -- Cố tình gửi báo cáo trùng lặp cho cùng đối tượng khi báo cáo trước đang PENDING
    INSERT INTO dbo.BaoCao (LoaiDoiTuong, MaDoiTuong, MaNguoiBaoCao, LyDo, MoTa, TrangThai)
    VALUES ('PRODUCT', @SpId, @BuyerId, N'c6smoke Báo cáo hàng giả lần 2', N'Gửi trùng lặp', 'PENDING');

    INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'C6.02 - Chống gửi trùng báo cáo khi đang PENDING', 0, N'Lẽ ra phải chặn trùng lặp qua UQ_BaoCao_ChongTrung');
END TRY
BEGIN CATCH
    IF ERROR_NUMBER() IN (2601, 2627)
        INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'C6.02 - Chống gửi trùng báo cáo khi đang PENDING', 1, N'Bắt đúng unique constraint UQ_BaoCao_ChongTrung');
    ELSE
        INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'C6.02 - Chống gửi trùng báo cáo khi đang PENDING', 0, ERROR_MESSAGE());
END CATCH
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ARITHABORT ON;
GO

-- =============================================================================
-- TEST 3: Admin DISMISS báo cáo vi phạm
-- =============================================================================
DECLARE @AdminId int = (SELECT MaNguoiDung FROM dbo.NguoiDung WHERE Email = 'c6smoke.admin@test.local');
DECLARE @BuyerId int = (SELECT MaNguoiDung FROM dbo.NguoiDung WHERE Email = 'c6smoke.buyer@test.local');
DECLARE @SpId int = (SELECT MaSanPham FROM dbo.SanPham WHERE Ten LIKE N'c6smoke%');

BEGIN TRY
    DECLARE @BaoCaoId int = (SELECT TOP 1 MaBaoCao FROM dbo.BaoCao WHERE MaNguoiBaoCao = @BuyerId AND MaDoiTuong = @SpId AND TrangThai = 'PENDING');

    UPDATE dbo.BaoCao
    SET TrangThai = 'DISMISSED',
        NguoiXuLyId = @AdminId,
        GhiChuXuLy = N'Báo cáo không có căn cứ, sản phẩm hợp lệ.',
        NgayXuLy = SYSUTCDATETIME()
    WHERE MaBaoCao = @BaoCaoId;

    IF EXISTS (SELECT 1 FROM dbo.BaoCao WHERE MaBaoCao = @BaoCaoId AND TrangThai = 'DISMISSED' AND NguoiXuLyId = @AdminId)
        INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'C6.03 - Admin DISMISS báo cáo', 1, N'Cập nhật DISMISSED và lưu đúng thông tin Admin xử lý');
    ELSE
        INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'C6.03 - Admin DISMISS báo cáo', 0, N'Trạng thái không khớp DISMISSED');
END TRY
BEGIN CATCH
    INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'C6.03 - Admin DISMISS báo cáo', 0, ERROR_MESSAGE());
END CATCH
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ARITHABORT ON;
GO

-- =============================================================================
-- TEST 4: Cho phép gửi lại báo cáo sau khi báo cáo cũ đã DISMISSED
-- =============================================================================
DECLARE @BuyerId int = (SELECT MaNguoiDung FROM dbo.NguoiDung WHERE Email = 'c6smoke.buyer@test.local');
DECLARE @SpId int = (SELECT MaSanPham FROM dbo.SanPham WHERE Ten LIKE N'c6smoke%');

BEGIN TRY
    -- Lần này gửi mới phải thành công vì báo cáo trước đã DISMISSED (filtered index chỉ áp dụng cho PENDING)
    INSERT INTO dbo.BaoCao (LoaiDoiTuong, MaDoiTuong, MaNguoiBaoCao, LyDo, MoTa, TrangThai)
    VALUES ('PRODUCT', @SpId, @BuyerId, N'c6smoke Báo cáo vi phạm có thêm bằng chứng', N'Bổ sung ảnh bằng chứng', 'PENDING');

    DECLARE @BaoCaoMoiId int = SCOPE_IDENTITY();
    IF EXISTS (SELECT 1 FROM dbo.BaoCao WHERE MaBaoCao = @BaoCaoMoiId AND TrangThai = 'PENDING')
        INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'C6.04 - Gửi lại báo cáo sau khi báo cáo cũ đã kết thúc', 1, N'Filtered index cho phép tạo báo cáo mới khi không còn PENDING');
    ELSE
        INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'C6.04 - Gửi lại báo cáo sau khi báo cáo cũ đã kết thúc', 0, N'Không tìm thấy báo cáo mới');
END TRY
BEGIN CATCH
    INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'C6.04 - Gửi lại báo cáo sau khi báo cáo cũ đã kết thúc', 0, ERROR_MESSAGE());
END CATCH
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ARITHABORT ON;
GO

-- =============================================================================
-- TEST 5: Admin RESOLVE báo cáo vi phạm
-- =============================================================================
DECLARE @AdminId int = (SELECT MaNguoiDung FROM dbo.NguoiDung WHERE Email = 'c6smoke.admin@test.local');
DECLARE @BuyerId int = (SELECT MaNguoiDung FROM dbo.NguoiDung WHERE Email = 'c6smoke.buyer@test.local');
DECLARE @SpId int = (SELECT MaSanPham FROM dbo.SanPham WHERE Ten LIKE N'c6smoke%');

BEGIN TRY
    DECLARE @BaoCaoPendingId int = (SELECT TOP 1 MaBaoCao FROM dbo.BaoCao WHERE MaNguoiBaoCao = @BuyerId AND MaDoiTuong = @SpId AND TrangThai = 'PENDING');

    UPDATE dbo.BaoCao
    SET TrangThai = 'RESOLVED',
        NguoiXuLyId = @AdminId,
        GhiChuXuLy = N'Xác nhận vi phạm, đã xử lý đối tượng.',
        NgayXuLy = SYSUTCDATETIME()
    WHERE MaBaoCao = @BaoCaoPendingId;

    IF EXISTS (SELECT 1 FROM dbo.BaoCao WHERE MaBaoCao = @BaoCaoPendingId AND TrangThai = 'RESOLVED' AND NguoiXuLyId = @AdminId)
        INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'C6.05 - Admin RESOLVE báo cáo', 1, N'Cập nhật RESOLVED thành công');
    ELSE
        INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'C6.05 - Admin RESOLVE báo cáo', 0, N'Trạng thái không khớp RESOLVED');
END TRY
BEGIN CATCH
    INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'C6.05 - Admin RESOLVE báo cáo', 0, ERROR_MESSAGE());
END CATCH
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ARITHABORT ON;
GO

-- =============================================================================
-- TEST 6: Mở khiếu nại tranh chấp đơn hàng hợp lệ (TranhChap)
-- =============================================================================
DECLARE @BuyerId int = (SELECT MaNguoiDung FROM dbo.NguoiDung WHERE Email = 'c6smoke.buyer@test.local');
DECLARE @DonHangId int = (SELECT MaDonHang FROM dbo.DonHang WHERE MaDon = 'c6smoke-DH001');

BEGIN TRY
    INSERT INTO dbo.TranhChap (MaDonHang, MaNguoiTao, LyDo, BangChungAnh, TrangThai)
    VALUES (@DonHangId, @BuyerId, N'c6smoke Hàng nhận được bị lỗi màn hình', '/uploads/disputes/c6_evidence.jpg', 'OPEN');

    DECLARE @TranhChapId int = SCOPE_IDENTITY();
    IF EXISTS (SELECT 1 FROM dbo.TranhChap WHERE MaTranhChap = @TranhChapId AND TrangThai = 'OPEN')
        INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'C6.06 - Mở tranh chấp đơn hàng hợp lệ', 1, N'Tranh chấp tạo thành công, trạng thái OPEN');
    ELSE
        INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'C6.06 - Mở tranh chấp đơn hàng hợp lệ', 0, N'Không tìm thấy bản ghi tranh chấp vừa tạo');
END TRY
BEGIN CATCH
    INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'C6.06 - Mở tranh chấp đơn hàng hợp lệ', 0, ERROR_MESSAGE());
END CATCH
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ARITHABORT ON;
GO

-- =============================================================================
-- TEST 7: Chống mở 2 tranh chấp trên cùng 1 đơn hàng (UQ_TranhChap_MaDonHang)
-- =============================================================================
DECLARE @BuyerId int = (SELECT MaNguoiDung FROM dbo.NguoiDung WHERE Email = 'c6smoke.buyer@test.local');
DECLARE @DonHangId int = (SELECT MaDonHang FROM dbo.DonHang WHERE MaDon = 'c6smoke-DH001');

BEGIN TRY
    -- Cố tình mở thêm tranh chấp thứ 2 trên cùng đơn hàng
    INSERT INTO dbo.TranhChap (MaDonHang, MaNguoiTao, LyDo, TrangThai)
    VALUES (@DonHangId, @BuyerId, N'c6smoke Mở khiếu nại lần 2 trên cùng đơn hàng', 'OPEN');

    INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'C6.07 - Chống mở 2 tranh chấp trên cùng 1 đơn hàng', 0, N'Lẽ ra phải chặn trùng lặp qua UQ_TranhChap_MaDonHang');
END TRY
BEGIN CATCH
    IF ERROR_NUMBER() IN (2601, 2627)
        INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'C6.07 - Chống mở 2 tranh chấp trên cùng 1 đơn hàng', 1, N'Bắt đúng unique constraint UQ_TranhChap_MaDonHang');
    ELSE
        INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'C6.07 - Chống mở 2 tranh chấp trên cùng 1 đơn hàng', 0, ERROR_MESSAGE());
END CATCH
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ARITHABORT ON;
GO

-- =============================================================================
-- TEST 8: Người bán gửi phản hồi cho khiếu nại tranh chấp
-- =============================================================================
DECLARE @DonHangId int = (SELECT MaDonHang FROM dbo.DonHang WHERE MaDon = 'c6smoke-DH001');

BEGIN TRY
    DECLARE @TranhChapId int = (SELECT TOP 1 MaTranhChap FROM dbo.TranhChap WHERE MaDonHang = @DonHangId);

    UPDATE dbo.TranhChap
    SET PhanHoiNguoiBan = N'Shop đồng ý kiểm tra và hỗ trợ đổi máy khác hoặc hoàn lại 100% tiền hàng.',
        TrangThai = 'PROCESSING',
        NgayCapNhat = SYSUTCDATETIME()
    WHERE MaTranhChap = @TranhChapId;

    IF EXISTS (SELECT 1 FROM dbo.TranhChap WHERE MaTranhChap = @TranhChapId AND TrangThai = 'PROCESSING' AND PhanHoiNguoiBan IS NOT NULL)
        INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'C6.08 - Người bán gửi phản hồi khiếu nại', 1, N'Cập nhật phản hồi và trạng thái PROCESSING thành công');
    ELSE
        INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'C6.08 - Người bán gửi phản hồi khiếu nại', 0, N'Cập nhật phản hồi thất bại');
END TRY
BEGIN CATCH
    INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'C6.08 - Người bán gửi phản hồi khiếu nại', 0, ERROR_MESSAGE());
END CATCH
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ARITHABORT ON;
GO

-- =============================================================================
-- TEST 9: Admin phán quyết giải quyết tranh chấp (RESOLVED)
-- =============================================================================
DECLARE @AdminId int = (SELECT MaNguoiDung FROM dbo.NguoiDung WHERE Email = 'c6smoke.admin@test.local');
DECLARE @DonHangId int = (SELECT MaDonHang FROM dbo.DonHang WHERE MaDon = 'c6smoke-DH001');

BEGIN TRY
    DECLARE @TranhChapId int = (SELECT TOP 1 MaTranhChap FROM dbo.TranhChap WHERE MaDonHang = @DonHangId);

    UPDATE dbo.TranhChap
    SET TrangThai = 'RESOLVED',
        PhanQuyet = N'Chấp thuận khiếu nại của người mua, shop hoàn tiền 250.000đ.',
        GhiChuAdmin = N'Hai bên đã thống nhất biên bản hoàn trả.',
        NguoiXuLyId = @AdminId,
        NgayGiaiQuyet = SYSUTCDATETIME(),
        NgayCapNhat = SYSUTCDATETIME()
    WHERE MaTranhChap = @TranhChapId;

    IF EXISTS (SELECT 1 FROM dbo.TranhChap WHERE MaTranhChap = @TranhChapId AND TrangThai = 'RESOLVED' AND NguoiXuLyId = @AdminId)
        INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'C6.09 - Admin phán quyết tranh chấp (RESOLVED)', 1, N'Lưu đúng phán quyết, Admin xử lý và thời gian giải quyết');
    ELSE
        INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'C6.09 - Admin phán quyết tranh chấp (RESOLVED)', 0, N'Trạng thái phán quyết không khớp');
END TRY
BEGIN CATCH
    INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'C6.09 - Admin phán quyết tranh chấp (RESOLVED)', 0, ERROR_MESSAGE());
END CATCH
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ARITHABORT ON;
GO

-- =============================================================================
-- TEST 10: Ràng buộc CHECK trạng thái không hợp lệ
-- =============================================================================
DECLARE @BuyerId int = (SELECT MaNguoiDung FROM dbo.NguoiDung WHERE Email = 'c6smoke.buyer@test.local');
DECLARE @SpId int = (SELECT MaSanPham FROM dbo.SanPham WHERE Ten LIKE N'c6smoke%');

BEGIN TRY
    -- Thử chèn trạng thái không hợp lệ cho BaoCao
    INSERT INTO dbo.BaoCao (LoaiDoiTuong, MaDoiTuong, MaNguoiBaoCao, LyDo, TrangThai)
    VALUES ('PRODUCT', @SpId, @BuyerId, N'c6smoke Test sai trạng thái', 'INVALID_STATUS');

    INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'C6.10 - Chặn trạng thái không hợp lệ qua CHECK constraint', 0, N'Lẽ ra phải chặn trạng thái sai');
END TRY
BEGIN CATCH
    IF ERROR_NUMBER() = 547 -- Check constraint violation
        INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'C6.10 - Chặn trạng thái không hợp lệ qua CHECK constraint', 1, N'Bắt đúng CHECK constraint CK_BaoCao_TrangThai');
    ELSE
        INSERT #KQ (Ten, Dat, ChiTiet) VALUES (N'C6.10 - Chặn trạng thái không hợp lệ qua CHECK constraint', 0, ERROR_MESSAGE());
END CATCH
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ARITHABORT ON;
GO

-- ------------------------------------------------------------------ DỌN DẸP AN TOÀN SAU KHI TEST XONG
DELETE tc FROM dbo.TranhChap tc
JOIN dbo.DonHang dh ON dh.MaDonHang = tc.MaDonHang
WHERE dh.MaDon LIKE 'c6smoke%';

DELETE FROM dbo.TranhChap WHERE LyDo LIKE N'c6smoke%';
DELETE FROM dbo.BaoCao WHERE LyDo LIKE N'c6smoke%';

DELETE ct FROM dbo.ChiTietDonHang ct
JOIN dbo.DonHang dh ON dh.MaDonHang = ct.MaDonHang
WHERE dh.MaDon LIKE 'c6smoke%';

DELETE ls FROM dbo.LichSuTrangThaiDonHang ls
JOIN dbo.DonHang dh ON dh.MaDonHang = ls.MaDonHang
WHERE dh.MaDon LIKE 'c6smoke%';

DELETE tt FROM dbo.ThanhToan tt
JOIN dbo.DonHang dh ON dh.MaDonHang = tt.MaDonHang
WHERE dh.MaDon LIKE 'c6smoke%';

DELETE FROM dbo.DonHang WHERE MaDon LIKE 'c6smoke%';
DELETE FROM dbo.SanPham WHERE Ten LIKE N'c6smoke%';
DELETE FROM dbo.CuaHang WHERE TenShop LIKE N'c6smoke%';
DELETE FROM dbo.NguoiDung WHERE Email LIKE 'c6smoke%';
GO

-- ------------------------------------------------------------------ IN KẾT QUẢ TỔNG HỢP
SELECT Id, Ten, Dat, ChiTiet FROM #KQ ORDER BY Id;

IF EXISTS (SELECT 1 FROM #KQ WHERE Dat = 0)
BEGIN
    THROW 51099, N'CÓ TEST THẤT BẠI TRONG C6_SMOKE_TEST!', 1;
END
ELSE
BEGIN
    PRINT N'>>> TẤT CẢ TEST MODULE C6 (C6_SMOKE_TEST) ĐỀU THÀNH CÔNG (PASS 10/10)! <<<';
END
GO
