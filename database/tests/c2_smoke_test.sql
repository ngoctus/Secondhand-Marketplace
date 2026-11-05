SET NOCOUNT ON;
SET XACT_ABORT OFF;
GO
USE SecondHandMarketplace;
GO

/* =====================================================================
   C2 SMOKE TEST (SQL Server). Chạy SAU khi đã chạy 01..09 và 14, 15, 16, 17 (xem docs/c2-moderation-handoff.md):
       sqlcmd -S . -d SecondHandMarketplace -i database\tests\c2_smoke_test.sql -f 65001
   - Tự tạo dữ liệu có tiền tố 'c2smoke', chạy xong tự dọn; chạy lại nhiều lần được.
   - In bảng PASS/FAIL ở cuối. Có FAIL => kết quả cuối script là lỗi (THROW).
   - KHÔNG chạy trên database có dữ liệu thật quan trọng ngoài mục đích dev/test.
   Script này CHƯA được chạy trong môi trường soạn thảo (không có SQL Server): chạy thử và báo lại FAIL nếu có.
   ===================================================================== */

IF OBJECT_ID('tempdb..#KQ') IS NOT NULL DROP TABLE #KQ;
CREATE TABLE #KQ (Id int IDENTITY(1,1), Ten nvarchar(200), Dat bit, ChiTiet nvarchar(500));
GO

-- ------------------------------------------------------------------ DỌN DẸP (đầu và cuối)
GO

DELETE FROM dbo.NhatKyHeThong WHERE MaNguoiDung IN (SELECT MaNguoiDung FROM dbo.NguoiDung WHERE Email LIKE 'c2smoke%');
DELETE FROM dbo.ThongBao WHERE MaNguoiDung IN (SELECT MaNguoiDung FROM dbo.NguoiDung WHERE Email LIKE 'c2smoke%');
DELETE FROM dbo.LichSuTrangThaiSanPham WHERE NguoiThucHienId IN (SELECT MaNguoiDung FROM dbo.NguoiDung WHERE Email LIKE 'c2smoke%');
DELETE v FROM dbo.LichSuViPham v JOIN dbo.NguoiDung n ON n.MaNguoiDung = v.MaNguoiBan WHERE n.Email LIKE 'c2smoke%';
DELETE p FROM dbo.PhanTichAI p JOIN dbo.LuotKiemDuyet l ON l.MaLuotKiemDuyet = p.MaLuotKiemDuyet
    JOIN dbo.SanPham s ON s.MaSanPham = l.MaSanPham JOIN dbo.CuaHang c ON c.MaCuaHang = s.MaCuaHang
    JOIN dbo.NguoiDung n ON n.MaNguoiDung = c.MaNguoiDung WHERE n.Email LIKE 'c2smoke%';
DELETE l FROM dbo.LuotKiemDuyet l JOIN dbo.SanPham s ON s.MaSanPham = l.MaSanPham JOIN dbo.CuaHang c ON c.MaCuaHang = s.MaCuaHang
    JOIN dbo.NguoiDung n ON n.MaNguoiDung = c.MaNguoiDung WHERE n.Email LIKE 'c2smoke%';
DELETE h FROM dbo.LichSuTrangThaiSanPham h JOIN dbo.SanPham s ON s.MaSanPham = h.MaSanPham JOIN dbo.CuaHang c ON c.MaCuaHang = s.MaCuaHang
    JOIN dbo.NguoiDung n ON n.MaNguoiDung = c.MaNguoiDung WHERE n.Email LIKE 'c2smoke%';
DELETE s FROM dbo.SanPham s JOIN dbo.CuaHang c ON c.MaCuaHang = s.MaCuaHang JOIN dbo.NguoiDung n ON n.MaNguoiDung = c.MaNguoiDung WHERE n.Email LIKE 'c2smoke%';
DELETE c FROM dbo.CuaHang c JOIN dbo.NguoiDung n ON n.MaNguoiDung = c.MaNguoiDung WHERE n.Email LIKE 'c2smoke%';
DELETE FROM dbo.NguoiDung WHERE Email LIKE 'c2smoke%';
DELETE FROM dbo.TuKhoaCam WHERE CumTu LIKE N'c2smoke%';
DELETE FROM dbo.DanhMuc WHERE Ten = N'c2smoke-danhmuc';
DELETE FROM dbo.TinhTrangSanPham WHERE Ma = 'C2SMOKE';
GO

-- ------------------------------------------------------------------ DỮ LIỆU NỀN
DECLARE @pw varchar(256) = REPLICATE('x', 30);
INSERT dbo.NguoiDung (HoTen, Email, SoDienThoai, MatKhauBam, VaiTro) VALUES (N'C2 Admin',  'c2smoke.admin@test.local',  NULL,        @pw, 'ADMIN');
INSERT dbo.NguoiDung (HoTen, Email, SoDienThoai, MatKhauBam, VaiTro) VALUES (N'C2 Seller', 'c2smoke.seller@test.local', '0900000001', @pw, 'USER');
INSERT dbo.DanhMuc (Ten) VALUES (N'c2smoke-danhmuc');
INSERT dbo.TinhTrangSanPham (Ma, Ten) VALUES ('C2SMOKE', N'c2smoke');
INSERT dbo.CuaHang (MaNguoiDung, TenShop, SoDienThoaiLienHe)
SELECT MaNguoiDung, N'c2smoke shop', '0900000001' FROM dbo.NguoiDung WHERE Email = 'c2smoke.seller@test.local';

-- 12 sản phẩm PENDING_REVIEW, mỗi sản phẩm 1 lượt mở (giống kết quả của sp_GuiDuyetSanPham)
DECLARE @i int = 1, @shop int = (SELECT MaCuaHang FROM dbo.CuaHang WHERE TenShop = N'c2smoke shop');
DECLARE @dm int = (SELECT MaDanhMuc FROM dbo.DanhMuc WHERE Ten = N'c2smoke-danhmuc');
DECLARE @tt int = (SELECT MaTinhTrang FROM dbo.TinhTrangSanPham WHERE Ma = 'C2SMOKE');
WHILE @i <= 12
BEGIN
    INSERT dbo.SanPham (MaCuaHang, MaDanhMuc, MaTinhTrang, Ten, MoTa, Gia, TrangThai)
    VALUES (@shop, @dm, @tt, N'c2smoke sản phẩm ' + CAST(@i AS nvarchar(5)), N'Mô tả dài hơn hai mươi ký tự cho sản phẩm thử nghiệm', 1000, 'PENDING_REVIEW');
    INSERT dbo.LuotKiemDuyet (MaSanPham) VALUES (SCOPE_IDENTITY());
    SET @i += 1;
END
GO

-- ------------------------------------------------------------------ TEST
DECLARE @admin int  = (SELECT MaNguoiDung FROM dbo.NguoiDung WHERE Email = 'c2smoke.admin@test.local');
DECLARE @seller int = (SELECT MaNguoiDung FROM dbo.NguoiDung WHERE Email = 'c2smoke.seller@test.local');
DECLARE @err int, @msg nvarchar(400);

-- Lấy lượt/sản phẩm thứ n (theo thứ tự tạo)
DECLARE @L TABLE (n int, Luot int, SanPham int);
INSERT @L SELECT ROW_NUMBER() OVER (ORDER BY l.MaLuotKiemDuyet), l.MaLuotKiemDuyet, l.MaSanPham
FROM dbo.LuotKiemDuyet l JOIN dbo.SanPham s ON s.MaSanPham = l.MaSanPham WHERE s.Ten LIKE N'c2smoke sản phẩm %';

-- ===== 1. TuKhoaCam.MucDo
BEGIN TRY
    INSERT dbo.TuKhoaCam (CumTu, CumTuChuanHoa, MucDo) VALUES (N'c2smoke bad', N'c2smoke bad', 'MEDIUM');
    INSERT #KQ VALUES (N'01 MucDo ngoài LOW/HIGH bị CHECK chặn', 0, N'insert MEDIUM lại thành công');
END TRY
BEGIN CATCH
    INSERT #KQ VALUES (N'01 MucDo ngoài LOW/HIGH bị CHECK chặn', IIF(ERROR_NUMBER() = 547, 1, 0), N'lỗi ' + CAST(ERROR_NUMBER() AS nvarchar(10)));
END CATCH

EXEC dbo.sp_ThemTuKhoaCam @CumTu = N'c2smoke hàng giả', @Nhom = 'HANG_GIA', @GhiChu = N'test' ;
EXEC dbo.sp_ThemTuKhoaCam @CumTu = N'c2smoke thấp',     @Nhom = 'LUA_DAO',  @GhiChu = N'test' ;
INSERT #KQ SELECT N'02 sp_ThemTuKhoaCam dùng được, MucDo mặc định LOW', IIF(MucDo = 'LOW', 1, 0), MucDo FROM dbo.TuKhoaCam WHERE CumTu = N'c2smoke hàng giả';
UPDATE dbo.TuKhoaCam SET MucDo = 'HIGH' WHERE CumTu = N'c2smoke hàng giả';

-- ===== 2. fn_TimTuKhoaCam + MucDo
INSERT #KQ
SELECT N'03 fn_TimTuKhoaCam thấy HIGH (bỏ dấu, hoa/thường) và LOW cùng lượt',
       IIF(SUM(IIF(t.MucDo = 'HIGH', 1, 0)) = 1 AND SUM(IIF(t.MucDo = 'LOW', 1, 0)) = 1, 1, 0), N''
FROM dbo.fn_TimTuKhoaCam(N'Bán c2smoke HÀNG GIẢ và c2smoke THẤP giá rẻ') f JOIN dbo.TuKhoaCam t ON t.MaTuKhoaCam = f.Id;

-- ===== 3. LichSuViPham: UNIQUE theo lượt + CHECK nguồn
DECLARE @L1 int = (SELECT Luot FROM @L WHERE n = 1), @P1 int = (SELECT SanPham FROM @L WHERE n = 1);
INSERT dbo.LichSuViPham (MaNguoiBan, MaSanPham, MaLuotKiemDuyet, MucDo, NguonGhiNhan, ChiTietTuKhoa)
VALUES (@seller, @P1, @L1, 'HIGH', 'AUTO_HIGH', N'[]');
BEGIN TRY
    INSERT dbo.LichSuViPham (MaNguoiBan, MaSanPham, MaLuotKiemDuyet, MucDo, NguonGhiNhan, ChiTietTuKhoa)
    VALUES (@seller, @P1, @L1, 'HIGH', 'AUTO_HIGH', N'[]');
    INSERT #KQ VALUES (N'04 UNIQUE: lượt thứ hai ghi trùng vi phạm bị chặn', 0, N'ghi trùng thành công');
END TRY
BEGIN CATCH
    INSERT #KQ VALUES (N'04 UNIQUE: lượt thứ hai ghi trùng vi phạm bị chặn', IIF(ERROR_NUMBER() IN (2601, 2627), 1, 0), N'lỗi ' + CAST(ERROR_NUMBER() AS nvarchar(10)));
END CATCH

DECLARE @L2 int = (SELECT Luot FROM @L WHERE n = 2), @P2 int = (SELECT SanPham FROM @L WHERE n = 2);
BEGIN TRY   -- AUTO_HIGH mà có Admin
    INSERT dbo.LichSuViPham (MaNguoiBan, MaSanPham, MaLuotKiemDuyet, MucDo, NguonGhiNhan, ChiTietTuKhoa, MaQuanTriVien)
    VALUES (@seller, @P2, @L2, 'HIGH', 'AUTO_HIGH', N'[]', @admin);
    INSERT #KQ VALUES (N'05a CHECK: AUTO_HIGH không được có Admin', 0, N'insert thành công');
END TRY
BEGIN CATCH
    INSERT #KQ VALUES (N'05a CHECK: AUTO_HIGH không được có Admin', IIF(ERROR_NUMBER() = 547, 1, 0), N'lỗi ' + CAST(ERROR_NUMBER() AS nvarchar(10)));
END CATCH
BEGIN TRY   -- ADMIN_TU_CHOI_LOW mà không có Admin
    INSERT dbo.LichSuViPham (MaNguoiBan, MaSanPham, MaLuotKiemDuyet, MucDo, NguonGhiNhan, ChiTietTuKhoa)
    VALUES (@seller, @P2, @L2, 'LOW', 'ADMIN_TU_CHOI_LOW', N'[]');
    INSERT #KQ VALUES (N'05b CHECK: vi phạm LOW do Admin từ chối bắt buộc có Admin', 0, N'insert thành công');
END TRY
BEGIN CATCH
    INSERT #KQ VALUES (N'05b CHECK: vi phạm LOW do Admin từ chối bắt buộc có Admin', IIF(ERROR_NUMBER() = 547, 1, 0), N'lỗi ' + CAST(ERROR_NUMBER() AS nvarchar(10)));
END CATCH
BEGIN TRY   -- JSON không hợp lệ
    INSERT dbo.LichSuViPham (MaNguoiBan, MaSanPham, MaLuotKiemDuyet, MucDo, NguonGhiNhan, ChiTietTuKhoa)
    VALUES (@seller, @P2, @L2, 'HIGH', 'AUTO_HIGH', N'không phải json');
    INSERT #KQ VALUES (N'05c CHECK: ChiTietTuKhoa phải là JSON hợp lệ', 0, N'insert thành công');
END TRY
BEGIN CATCH
    INSERT #KQ VALUES (N'05c CHECK: ChiTietTuKhoa phải là JSON hợp lệ', IIF(ERROR_NUMBER() = 547, 1, 0), N'lỗi ' + CAST(ERROR_NUMBER() AS nvarchar(10)));
END CATCH

-- ===== 4. CHẶN DUYỆT (trigger file 14) qua sp_DuyetSanPham
-- Lượt 3: QUEUED | 4: FAILED | 5: DONE chưa quét từ khóa | 6: HIGH | 7: LOW (duyệt) | 8: LOW (từ chối) | 9: NONE (duyệt) | 10: LOW (từ chối, đã có vi phạm trùng)
DECLARE @n int = 3, @luot int, @sp int, @expect int, @label nvarchar(100);
DECLARE @cases TABLE (n int, nhan nvarchar(100), expect int);
INSERT @cases VALUES (3, N'06a QUEUED không được duyệt', 50210), (4, N'06b FAILED không được duyệt', 50210),
                     (5, N'06c DONE chưa quét từ khóa không được duyệt', 50210), (6, N'06d HIGH không được duyệt', 50211);

INSERT dbo.PhanTichAI (MaLuotKiemDuyet, TrangThai, LoiXuLy) SELECT Luot, 'QUEUED', NULL FROM @L WHERE n = 3;
INSERT dbo.PhanTichAI (MaLuotKiemDuyet, TrangThai, LoiXuLy) SELECT Luot, 'FAILED', N'lỗi giả lập' FROM @L WHERE n = 4;
INSERT dbo.PhanTichAI (MaLuotKiemDuyet, TrangThai, MucRuiRo, DeXuat, ThoiGianHoanThanh) SELECT Luot, 'DONE', 'LOW', 'APPROVE', SYSUTCDATETIME() FROM @L WHERE n = 5;
INSERT dbo.PhanTichAI (MaLuotKiemDuyet, TrangThai, MucRuiRo, DeXuat, ThoiGianHoanThanh, MucDoTuKhoa, DauHieu)
    SELECT Luot, 'DONE', 'HIGH', 'REJECT', SYSUTCDATETIME(), 'HIGH', N'[]' FROM @L WHERE n = 6;
INSERT dbo.PhanTichAI (MaLuotKiemDuyet, TrangThai, MucRuiRo, DeXuat, ThoiGianHoanThanh, MucDoTuKhoa, DauHieu)
    SELECT Luot, 'DONE', 'MEDIUM', 'NEEDS_REVIEW', SYSUTCDATETIME(), 'LOW', N'[{"loai":"TU_KHOA_CAM","tu_khoa":"c2smoke thấp","muc_do":"LOW","truong":"Ten"}]' FROM @L WHERE n IN (7, 8, 10);
INSERT dbo.PhanTichAI (MaLuotKiemDuyet, TrangThai, MucRuiRo, DeXuat, ThoiGianHoanThanh, MucDoTuKhoa, DauHieu)
    SELECT Luot, 'DONE', 'LOW', 'APPROVE', SYSUTCDATETIME(), 'NONE', N'[]' FROM @L WHERE n = 9;

DECLARE c CURSOR LOCAL FAST_FORWARD FOR SELECT n, nhan, expect FROM @cases ORDER BY n;
OPEN c; FETCH NEXT FROM c INTO @n, @label, @expect;
WHILE @@FETCH_STATUS = 0
BEGIN
    SELECT @sp = SanPham FROM @L WHERE n = @n;
    SET @err = NULL; SET @msg = NULL;
    BEGIN TRY
        EXEC dbo.sp_DuyetSanPham @MaSanPham = @sp, @QuanTriVienId = @admin;
    END TRY
    BEGIN CATCH
        SET @err = ERROR_NUMBER(); SET @msg = ERROR_MESSAGE();
    END CATCH
    INSERT #KQ VALUES (@label, IIF(@err = @expect AND (SELECT TrangThai FROM dbo.SanPham WHERE MaSanPham = @sp) = 'PENDING_REVIEW', 1, 0),
                       N'mong đợi lỗi ' + CAST(@expect AS nvarchar(10)) + N', thực tế ' + ISNULL(CAST(@err AS nvarchar(10)), N'không lỗi') + ISNULL(N' - ' + LEFT(@msg, 120), N''));
    FETCH NEXT FROM c INTO @n, @label, @expect;
END
CLOSE c; DEALLOCATE c;

-- Các ca ĐƯỢC duyệt: LOW (lượt 7) và không từ khóa (lượt 9); duyệt KHÔNG ghi vi phạm
DECLARE @okCases TABLE (n int, nhan nvarchar(100));
INSERT @okCases VALUES (7, N'07a LOW được Admin duyệt (ACTIVE)'), (9, N'07b không từ khóa (DONE/NONE) được duyệt');
DECLARE c2 CURSOR LOCAL FAST_FORWARD FOR SELECT n, nhan FROM @okCases;
OPEN c2; FETCH NEXT FROM c2 INTO @n, @label;
WHILE @@FETCH_STATUS = 0
BEGIN
    SELECT @sp = SanPham FROM @L WHERE n = @n;
    SET @err = NULL; SET @msg = NULL;
    BEGIN TRY
        EXEC dbo.sp_DuyetSanPham @MaSanPham = @sp, @QuanTriVienId = @admin;
    END TRY
    BEGIN CATCH
        SET @err = ERROR_NUMBER(); SET @msg = ERROR_MESSAGE();
    END CATCH
    INSERT #KQ VALUES (@label, IIF(@err IS NULL AND (SELECT TrangThai FROM dbo.SanPham WHERE MaSanPham = @sp) = 'ACTIVE', 1, 0),
                       ISNULL(N'lỗi ' + CAST(@err AS nvarchar(10)) + N' - ' + LEFT(@msg, 150), N''));
    FETCH NEXT FROM c2 INTO @n, @label;
END
CLOSE c2; DEALLOCATE c2;
INSERT #KQ VALUES (N'07c duyệt LOW/NONE không ghi vi phạm nào',
    IIF(NOT EXISTS (SELECT 1 FROM dbo.LichSuViPham v JOIN @L l ON l.Luot = v.MaLuotKiemDuyet WHERE l.n IN (7, 9)), 1, 0), N'');

-- ===== 4b. LOW + Admin TỪ CHỐI => đúng 1 vi phạm, cùng transaction với quyết định (trigger file 17)
DECLARE @L8 int = (SELECT Luot FROM @L WHERE n = 8), @P8 int = (SELECT SanPham FROM @L WHERE n = 8);
SET @err = NULL;
BEGIN TRY
    EXEC dbo.sp_TuChoiSanPham @MaSanPham = @P8, @QuanTriVienId = @admin, @LyDo = N'Từ chối thử nghiệm C2 smoke test (LOW)';
END TRY
BEGIN CATCH
    SET @err = ERROR_NUMBER();
END CATCH
INSERT #KQ SELECT N'08a LOW + Admin từ chối => đúng 1 vi phạm ADMIN_TU_CHOI_LOW kèm Admin',
    IIF(@err IS NULL AND COUNT(*) = 1 AND MIN(NguonGhiNhan) = 'ADMIN_TU_CHOI_LOW' AND MIN(MucDo) = 'LOW' AND MIN(MaQuanTriVien) = @admin AND MIN(MaNguoiBan) = @seller, 1, 0),
    N'lỗi ' + ISNULL(CAST(@err AS nvarchar(10)), N'không') + N', số dòng ' + CAST(COUNT(*) AS nvarchar(10))
FROM dbo.LichSuViPham WHERE MaLuotKiemDuyet = @L8;

SET @err = NULL;   -- Admin thao tác lặp trên lượt đã quyết định
BEGIN TRY
    EXEC dbo.sp_TuChoiSanPham @MaSanPham = @P8, @QuanTriVienId = @admin, @LyDo = N'Từ chối lặp thử nghiệm C2 smoke test';
END TRY
BEGIN CATCH
    SET @err = ERROR_NUMBER();
END CATCH
INSERT #KQ SELECT N'08b Admin từ chối lặp không cộng thêm vi phạm', IIF(COUNT(*) = 1, 1, 0), N'lỗi lần 2: ' + ISNULL(CAST(@err AS nvarchar(10)), N'không (no-op)')
FROM dbo.LichSuViPham WHERE MaLuotKiemDuyet = @L8;

-- Từ chối khi QUEUED (lượt 3) / FAILED (lượt 4): hợp lệ nhưng KHÔNG tính vi phạm
DECLARE @P3 int = (SELECT SanPham FROM @L WHERE n = 3), @P4 int = (SELECT SanPham FROM @L WHERE n = 4);
BEGIN TRY EXEC dbo.sp_TuChoiSanPham @MaSanPham = @P3, @QuanTriVienId = @admin, @LyDo = N'Từ chối thử nghiệm QUEUED C2 smoke'; END TRY BEGIN CATCH END CATCH
BEGIN TRY EXEC dbo.sp_TuChoiSanPham @MaSanPham = @P4, @QuanTriVienId = @admin, @LyDo = N'Từ chối thử nghiệm FAILED C2 smoke'; END TRY BEGIN CATCH END CATCH
INSERT #KQ VALUES (N'08c từ chối khi QUEUED/FAILED không ghi vi phạm',
    IIF((SELECT COUNT(*) FROM dbo.SanPham WHERE MaSanPham IN (@P3, @P4) AND TrangThai = 'REJECTED') = 2
        AND NOT EXISTS (SELECT 1 FROM dbo.LichSuViPham v JOIN @L l ON l.Luot = v.MaLuotKiemDuyet WHERE l.n IN (3, 4)), 1, 0), N'');

-- Đã có vi phạm cho lượt (giả lập request kia vừa ghi) rồi Admin từ chối: trigger KHÔNG ghi thêm, không lỗi UNIQUE
DECLARE @L10 int = (SELECT Luot FROM @L WHERE n = 10), @P10 int = (SELECT SanPham FROM @L WHERE n = 10);
INSERT dbo.LichSuViPham (MaNguoiBan, MaSanPham, MaLuotKiemDuyet, MucDo, NguonGhiNhan, ChiTietTuKhoa, MaQuanTriVien)
VALUES (@seller, @P10, @L10, 'LOW', 'ADMIN_TU_CHOI_LOW', N'[]', @admin);
SET @err = NULL;
BEGIN TRY
    EXEC dbo.sp_TuChoiSanPham @MaSanPham = @P10, @QuanTriVienId = @admin, @LyDo = N'Từ chối thử nghiệm trùng C2 smoke';
END TRY
BEGIN CATCH
    SET @err = ERROR_NUMBER();
END CATCH
INSERT #KQ SELECT N'08d từ chối khi vi phạm đã có: không trùng, không lỗi', IIF(@err IS NULL AND COUNT(*) = 1, 1, 0), N'lỗi ' + ISNULL(CAST(@err AS nvarchar(10)), N'không')
FROM dbo.LichSuViPham WHERE MaLuotKiemDuyet = @L10;

-- ===== 5. TỪ CHỐI TỰ ĐỘNG BỞI HỆ THỐNG (file 15)
DECLARE @L11 int = (SELECT Luot FROM @L WHERE n = 11), @P11 int = (SELECT SanPham FROM @L WHERE n = 11);
BEGIN TRY   -- quyết định REJECTED không Admin mà KHÔNG đánh dấu tự động => phải bị chặn (không giả mạo)
    UPDATE dbo.LuotKiemDuyet SET QuyetDinh = 'REJECTED', LyDo = N'Tự động từ chối thử nghiệm', ThoiGianQuyetDinh = SYSUTCDATETIME() WHERE MaLuotKiemDuyet = @L11;
    INSERT #KQ VALUES (N'09a REJECTED không Admin và không đánh dấu tự động bị chặn', 0, N'update thành công');
END TRY
BEGIN CATCH
    INSERT #KQ VALUES (N'09a REJECTED không Admin và không đánh dấu tự động bị chặn', IIF(ERROR_NUMBER() = 547, 1, 0), N'lỗi ' + CAST(ERROR_NUMBER() AS nvarchar(10)));
END CATCH
BEGIN TRY   -- tự động nhưng APPROVED => phải bị chặn
    UPDATE dbo.LuotKiemDuyet SET QuyetDinh = 'APPROVED', QuyetDinhTuDong = 1, ThoiGianQuyetDinh = SYSUTCDATETIME() WHERE MaLuotKiemDuyet = @L11;
    INSERT #KQ VALUES (N'09b hệ thống không được tự DUYỆT', 0, N'update thành công');
END TRY
BEGIN CATCH
    INSERT #KQ VALUES (N'09b hệ thống không được tự DUYỆT', IIF(ERROR_NUMBER() = 547, 1, 0), N'lỗi ' + CAST(ERROR_NUMBER() AS nvarchar(10)));
END CATCH
SET @err = NULL;
BEGIN TRY   -- đường hợp lệ: đóng lượt bằng quyết định hệ thống + chuyển trạng thái SYSTEM + 1 vi phạm
    BEGIN TRAN;
    UPDATE dbo.LuotKiemDuyet SET QuyetDinh = 'REJECTED', LyDo = N'Tự động từ chối: chứa từ khóa cấm mức HIGH (test)',
           MaQuanTriVien = NULL, ThoiGianQuyetDinh = SYSUTCDATETIME(), QuyetDinhTuDong = 1 WHERE MaLuotKiemDuyet = @L11;
    UPDATE dbo.SanPham SET TrangThai = 'REJECTED' WHERE MaSanPham = @P11;
    INSERT dbo.LichSuTrangThaiSanPham (MaSanPham, TrangThaiCu, TrangThaiMoi, LoaiNguoiThucHien, NguoiThucHienId, LyDo)
    VALUES (@P11, 'PENDING_REVIEW', 'REJECTED', 'SYSTEM', NULL, N'Tự động từ chối (test)');
    INSERT dbo.LichSuViPham (MaNguoiBan, MaSanPham, MaLuotKiemDuyet, MucDo, NguonGhiNhan, ChiTietTuKhoa)
    VALUES (@seller, @P11, @L11, 'HIGH', 'AUTO_HIGH', N'[]');
    COMMIT;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK;
    SET @err = ERROR_NUMBER();
END CATCH
INSERT #KQ VALUES (N'09c từ chối tự động hợp lệ (không giả Admin) ghi được', IIF(@err IS NULL AND
    (SELECT QuyetDinhTuDong FROM dbo.LuotKiemDuyet WHERE MaLuotKiemDuyet = @L11) = 1 AND
    (SELECT MaQuanTriVien FROM dbo.LuotKiemDuyet WHERE MaLuotKiemDuyet = @L11) IS NULL, 1, 0), ISNULL(CAST(@err AS nvarchar(10)), N''));
-- sau từ chối tự động, người bán gửi duyệt lại được: lượt mới không vướng UX_LuotKiemDuyet_DangCho
SET @err = NULL;
BEGIN TRY
    UPDATE dbo.SanPham SET TrangThai = 'PENDING_REVIEW' WHERE MaSanPham = @P11;
    INSERT dbo.LuotKiemDuyet (MaSanPham) VALUES (@P11);
END TRY
BEGIN CATCH
    SET @err = ERROR_NUMBER();
END CATCH
INSERT #KQ VALUES (N'09d gửi duyệt lại sau từ chối tự động không vướng lượt mở', IIF(@err IS NULL, 1, 0), ISNULL(CAST(@err AS nvarchar(10)), N''));

-- ===== 6. Bộ đếm, khóa tài khoản, mở khóa không xóa lịch sử
INSERT dbo.LichSuViPham (MaNguoiBan, MaSanPham, MaLuotKiemDuyet, MucDo, NguonGhiNhan, ChiTietTuKhoa)
SELECT @seller, SanPham, Luot, 'HIGH', 'AUTO_HIGH', N'[]' FROM @L WHERE n = 12;   -- tổng 5 dòng: lượt 1, 8, 10, 11, 12
DECLARE @cnt int = (SELECT COUNT(*) FROM dbo.LichSuViPham WHERE MaNguoiBan = @seller);
INSERT #KQ VALUES (N'10 bộ đếm = số dòng LichSuViPham của người bán (mỗi lượt 1 dòng)', IIF(@cnt = 5, 1, 0), N'đếm được ' + CAST(@cnt AS nvarchar(10)));

UPDATE dbo.NguoiDung SET TrangThai = 'LOCKED', LyDoKhoa = N'Tự động khóa: test', ThoiGianKhoa = SYSUTCDATETIME() WHERE MaNguoiDung = @seller;
UPDATE dbo.NguoiDung SET TrangThai = 'ACTIVE', LyDoKhoa = NULL, ThoiGianKhoa = NULL WHERE MaNguoiDung = @seller;   -- mở khóa
INSERT #KQ VALUES (N'11 mở khóa tài khoản không xóa lịch sử vi phạm',
    IIF((SELECT COUNT(*) FROM dbo.LichSuViPham WHERE MaNguoiBan = @seller) = @cnt, 1, 0), N'');

-- ===== 7. Bỏ chặn TU_CAM khi gửi duyệt (file 16)
INSERT #KQ
SELECT N'12 fn_KiemTraRuleSanPham không còn rule TU_CAM (sản phẩm chứa từ khóa cấm vẫn gửi duyệt được)',
       IIF(NOT EXISTS (SELECT 1 FROM dbo.fn_KiemTraRuleSanPham(@P1) WHERE MaLoi = N'TU_CAM'), 1, 0), N'';
GO

-- ------------------------------------------------------------------ KẾT QUẢ
SELECT Ten AS [Test], IIF(Dat = 1, 'PASS', 'FAIL') AS [KetQua], ChiTiet FROM #KQ ORDER BY Id;
DECLARE @fail int = (SELECT COUNT(*) FROM #KQ WHERE Dat = 0);
PRINT CONCAT(N'Tổng: ', (SELECT COUNT(*) FROM #KQ), N' test, ', @fail, N' FAIL.');
GO

-- DỌN DẸP: chạy lại đoạn DỌN DẸP đầu file (hoặc chạy lại toàn bộ script, nó tự dọn ở đầu).

/* =====================================================================
   TEST HAI PHIÊN ĐỒNG THỜI (làm tay bằng 2 cửa sổ SSMS) - chống ghi trùng vi phạm
   Lấy @luot = MaLuotKiemDuyet bất kỳ chưa có vi phạm, @seller, @sp tương ứng.
   Phiên A:  BEGIN TRAN;
             INSERT dbo.LichSuViPham (MaNguoiBan, MaSanPham, MaLuotKiemDuyet, MucDo, NguonGhiNhan, ChiTietTuKhoa)
             VALUES (@seller, @sp, @luot, 'HIGH', 'AUTO_HIGH', N'[]');      -- CHƯA COMMIT
   Phiên B:  chạy đúng câu INSERT trên  -> bị CHỜ (khóa UNIQUE).
   Phiên A:  COMMIT;
   Phiên B:  nhận lỗi 2627 (UNIQUE KEY UQ_LichSuViPham_LuotKiemDuyet) => không có bản ghi trùng.
   Kiểm: SELECT COUNT(*) FROM dbo.LichSuViPham WHERE MaLuotKiemDuyet = @luot;   -- = 1
   (C# đã bắt lỗi 2601/2627 và coi như "đã ghi nhận", đồng thời khóa dòng LuotKiemDuyet bằng UPDLOCK để tuần tự hóa.)
   ===================================================================== */
