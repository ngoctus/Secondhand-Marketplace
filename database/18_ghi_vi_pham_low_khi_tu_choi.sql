SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ARITHABORT ON;
GO

USE SecondHandMarketplace;
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

PRINT N'C2: đã cập nhật trigger trg_LuotKiemDuyet_GhiViPhamLow.';
GO
