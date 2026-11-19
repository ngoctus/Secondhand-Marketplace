SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ARITHABORT ON;
GO

USE SecondHandMarketplace;
GO

-- Toàn bộ 6 trigger core và mở rộng.

-- Làm sạch các trigger không dùng đến hoặc drop an toàn
DROP TRIGGER IF EXISTS trg_NguoiDung_CapNhat;
DROP TRIGGER IF EXISTS trg_CuaHang_KiemTra;
DROP TRIGGER IF EXISTS trg_SanPham_ChuanHoa;
DROP TRIGGER IF EXISTS trg_SanPham_ChuyenTrangThai;
DROP TRIGGER IF EXISTS trg_SanPham_ChanXoaMem;
DROP TRIGGER IF EXISTS trg_GioHang_KiemTra;
DROP TRIGGER IF EXISTS trg_DonHang_KiemTraTao;
DROP TRIGGER IF EXISTS trg_DonHang_KiemTraCapNhat;
DROP TRIGGER IF EXISTS trg_AnhSanPham_GioiHan;
DROP TRIGGER IF EXISTS trg_ChiTietDonHang_KiemTra;
DROP TRIGGER IF EXISTS trg_LuotKiemDuyet_KiemTra;
GO

-- AnhSanPham: tối đa 10 ảnh / sản phẩm
CREATE OR ALTER TRIGGER trg_AnhSanPham_GioiHan ON AnhSanPham AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1
               FROM AnhSanPham a
               WHERE a.MaSanPham IN (SELECT MaSanPham FROM inserted)
               GROUP BY a.MaSanPham
               HAVING COUNT(*) > 10)
        THROW 50090, N'Mỗi sản phẩm chỉ được tối đa 10 ảnh.', 1;
END
GO

-- ChiTietDonHang: snapshot bất biến (chỉ DangGiuCho được đổi), không xóa,
-- sản phẩm phải thuộc đúng shop của đơn (không có đơn "lai")
CREATE OR ALTER TRIGGER trg_ChiTietDonHang_KiemTra ON ChiTietDonHang AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CoMoi bit = CASE WHEN EXISTS (SELECT 1 FROM inserted) THEN 1 ELSE 0 END,
            @CoCu  bit = CASE WHEN EXISTS (SELECT 1 FROM deleted)  THEN 1 ELSE 0 END;

    IF @CoMoi = 0 AND @CoCu = 1
        THROW 50072, N'Không được xóa chi tiết đơn hàng.', 1;

    IF @CoMoi = 1 AND @CoCu = 1
    BEGIN
        -- SỬA LỖI: Thay thế toán tử sai cú pháp || bằng OR chuẩn T-SQL
        IF EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON d.MaChiTietDonHang = i.MaChiTietDonHang
                   WHERE i.MaSanPham <> d.MaSanPham 
                      OR i.MaDonHang <> d.MaDonHang 
                      OR i.Gia <> d.Gia 
                      OR i.TenSanPham <> d.TenSanPham
                      OR ISNULL(i.AnhDaiDien, N'') <> ISNULL(d.AnhDaiDien, N''))
            THROW 50073, N'Chi tiết đơn hàng là dữ liệu chốt tại thời điểm đặt, không được sửa.', 1;
    END
    ELSE IF @CoMoi = 1 AND @CoCu = 0
    BEGIN
        IF EXISTS (SELECT 1 FROM inserted i
                   JOIN DonHang dh ON dh.MaDonHang = i.MaDonHang
                   JOIN SanPham sp ON sp.MaSanPham = i.MaSanPham
                   WHERE sp.MaCuaHang <> dh.MaCuaHang)
            THROW 50074, N'Sản phẩm không thuộc cửa hàng của đơn hàng này.', 1;
    END
END
GO

-- LuotKiemDuyet: chỉ ADMIN quyết định; đã quyết định thì không đổi được
CREATE OR ALTER TRIGGER trg_LuotKiemDuyet_KiemTra ON LuotKiemDuyet AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM inserted i JOIN NguoiDung nd ON nd.MaNguoiDung = i.MaQuanTriVien
               WHERE nd.VaiTro <> 'ADMIN')
        THROW 50080, N'Chỉ quản trị viên (ADMIN) mới được quyết định kiểm duyệt.', 1;

    IF EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON d.MaLuotKiemDuyet = i.MaLuotKiemDuyet
               WHERE d.QuyetDinh IS NOT NULL AND (i.QuyetDinh IS NULL OR i.QuyetDinh <> d.QuyetDinh))
        THROW 50081, N'Lượt kiểm duyệt đã có quyết định, không được thay đổi.', 1;
END
GO

-- =====================================================================
-- Chặn đưa sản phẩm đang nằm trong phiên đấu giá (SCHEDULED/RUNNING) ra khỏi RESERVED hoặc xóa mềm.
-- Muốn thay đổi thì phải hủy phiên trước (sp_HuyPhienDauGia).
-- =====================================================================
CREATE OR ALTER TRIGGER trg_SanPham_ChanRutKhoiDauGia ON SanPham AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT (UPDATE(TrangThai) OR UPDATE(DaXoa)) RETURN;

    IF EXISTS (SELECT 1
               FROM inserted i
               JOIN deleted d ON d.MaSanPham = i.MaSanPham
               JOIN PhienDauGia p ON p.MaSanPham = i.MaSanPham AND p.TrangThai IN ('SCHEDULED','RUNNING')
               WHERE d.TrangThai = 'RESERVED' AND (i.TrangThai <> 'RESERVED' OR i.DaXoa = 1))
        THROW 52060, N'Sản phẩm đang nằm trong phiên đấu giá. Hãy hủy phiên đấu giá trước khi thay đổi trạng thái sản phẩm.', 1;
END
GO

-- ---------------------------------------------------------------------
-- 4. CHẶN DUYỆT Ở TẦNG DATABASE (không phụ thuộc C# hay procedure nào gọi).
--    Mọi đường duyệt (sp_QuyetDinhKiemDuyet / sp_DuyetSanPham) đều UPDATE LuotKiemDuyet.QuyetDinh = 'APPROVED';
--    trigger này THROW (rollback cả transaction) khi:
--      - 50210: chưa có phân tích, hoặc phân tích QUEUED/FAILED, hoặc chưa quét từ khóa (MucDoTuKhoa NULL);
--      - 50211: có HIGH, hoặc lượt này đã bị ghi nhận vi phạm.
--    LOW: Admin ĐƯỢC duyệt (không vi phạm). Từ chối (REJECTED) không bị chặn. Không đụng trg_LuotKiemDuyet_KiemTra có sẵn.
-- ---------------------------------------------------------------------
CREATE OR ALTER TRIGGER dbo.trg_LuotKiemDuyet_ChanDuyetViPham ON dbo.LuotKiemDuyet AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT UPDATE(QuyetDinh) RETURN;

    DECLARE @Duyet TABLE (MaLuotKiemDuyet int PRIMARY KEY);
    INSERT @Duyet (MaLuotKiemDuyet)
    SELECT i.MaLuotKiemDuyet
    FROM inserted i
    JOIN deleted d ON d.MaLuotKiemDuyet = i.MaLuotKiemDuyet
    WHERE i.QuyetDinh = 'APPROVED' AND d.QuyetDinh IS NULL;

    IF NOT EXISTS (SELECT 1 FROM @Duyet) RETURN;

    IF EXISTS (SELECT 1
               FROM @Duyet x
               LEFT JOIN dbo.PhanTichAI p ON p.MaLuotKiemDuyet = x.MaLuotKiemDuyet
               WHERE p.MaPhanTichAI IS NULL OR p.TrangThai <> 'DONE' OR p.MucDoTuKhoa IS NULL)
        THROW 50210, N'Không thể duyệt: sản phẩm chưa được phân tích từ khóa xong (QUEUED/FAILED hoặc chưa quét).', 1;

    IF EXISTS (SELECT 1
               FROM @Duyet x
               JOIN dbo.PhanTichAI p ON p.MaLuotKiemDuyet = x.MaLuotKiemDuyet
               WHERE p.MucDoTuKhoa = 'HIGH'
                  OR EXISTS (SELECT 1 FROM dbo.LichSuViPham v WHERE v.MaLuotKiemDuyet = x.MaLuotKiemDuyet))
        THROW 50211, N'Không thể duyệt: sản phẩm có từ khóa cấm mức HIGH hoặc đã bị ghi nhận vi phạm.', 1;
END
GO

/* =====================================================================
   C2 - GHI 1 VI PHẠM KHI ADMIN TỪ CHỐI SẢN PHẨM  (trigger trên LuotKiemDuyet)
   *** Thay đổi schema dùng chung (thêm 1 trigger) - tách riêng để duyệt trước khi chạy. ***

   NGHIỆP VỤ: sản phẩm bị từ chối bởi Admin -> ghi 1 vi phạm cho người bán.
       Admin DUYỆT   -> không vi phạm.
       Admin TỪ CHỐI -> ghi đúng 1 vi phạm cho người bán.
   Đủ 3 vi phạm (ngưỡng ViolationLockThreshold) -> tự động khóa tài khoản và cửa hàng.
   ===================================================================== */

CREATE OR ALTER TRIGGER dbo.trg_LuotKiemDuyet_GhiViPhamLow ON dbo.LuotKiemDuyet AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT UPDATE(QuyetDinh) RETURN;

    INSERT dbo.LichSuViPham (MaNguoiBan, MaSanPham, MaLuotKiemDuyet, MucDo, NguonGhiNhan, ChiTietTuKhoa, MaQuanTriVien)
    SELECT c.MaNguoiDung, i.MaSanPham, i.MaLuotKiemDuyet, 'LOW', 'ADMIN_TU_CHOI_LOW',
           CASE WHEN ISJSON(p.DauHieu) = 1 THEN p.DauHieu ELSE N'[]' END,
           i.MaQuanTriVien
    FROM inserted i
    JOIN deleted d ON d.MaLuotKiemDuyet = i.MaLuotKiemDuyet
    LEFT JOIN dbo.PhanTichAI p ON p.MaLuotKiemDuyet = i.MaLuotKiemDuyet
    JOIN dbo.SanPham s ON s.MaSanPham = i.MaSanPham
    JOIN dbo.CuaHang c ON c.MaCuaHang = s.MaCuaHang
    WHERE i.QuyetDinh = 'REJECTED' AND d.QuyetDinh IS NULL
      AND i.QuyetDinhTuDong = 0 AND i.MaQuanTriVien IS NOT NULL
      AND NOT EXISTS (SELECT 1 FROM dbo.LichSuViPham v WHERE v.MaLuotKiemDuyet = i.MaLuotKiemDuyet);
END
GO
