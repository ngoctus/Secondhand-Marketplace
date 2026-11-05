SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ARITHABORT ON;
GO

USE SecondHandMarketplace;
GO

/* =====================================================================
   C2 - GHI 1 VI PHẠM LOW KHI ADMIN TỪ CHỐI SẢN PHẨM  (trigger trên LuotKiemDuyet)
   *** Thay đổi schema dùng chung (thêm 1 trigger) - tách riêng để duyệt trước khi chạy. ***

   NGHIỆP VỤ: sản phẩm chỉ có từ khóa LOW -> chờ Admin ở C1:
       Admin DUYỆT   -> không vi phạm.
       Admin TỪ CHỐI -> ghi đúng 1 vi phạm cho người bán.

   VÌ SAO LÀ TRIGGER: quyết định từ chối của Admin đi qua sp_TuChoiSanPham/sp_QuyetDinhKiemDuyet của C1 (UPDATE
   LuotKiemDuyet.QuyetDinh = 'REJECTED'). Ghi vi phạm ngay trong trigger của UPDATE đó nghĩa là:
     - cùng một transaction: quyết định từ chối và vi phạm cùng thành công hoặc cùng bị rollback;
     - không phụ thuộc C1 có nhớ gọi C# hay không, không sửa procedure/Controller của C1;
     - Admin bấm lặp hoặc 2 request đồng thời: procedure chỉ cho UPDATE khi QuyetDinh còn NULL (khóa UPDLOCK),
       nên trigger chỉ chạy một lần; NOT EXISTS + UNIQUE(MaLuotKiemDuyet) là hai lớp chặn trùng nữa.
   Trigger CHỈ ghi khi: lượt chuyển NULL -> REJECTED bởi Admin (MaQuanTriVien có giá trị, QuyetDinhTuDong = 0),
   phân tích DONE với MucDoTuKhoa = 'LOW', và lượt chưa có vi phạm. QUEUED/FAILED/NONE không tính vi phạm.
   HIGH do C# tự ghi (AUTO_HIGH) nên không đi qua đây.
   Việc KHÓA tài khoản khi đủ 3 vi phạm không làm được trong SQL (phải qua IUserAdminService.LockUserAsync):
   C1 gọi IModerationService.AfterAdminDecisionAsync sau khi từ chối.
   Thứ tự: chạy SAU 14 và 15. Hoàn tác: DROP TRIGGER dbo.trg_LuotKiemDuyet_GhiViPhamLow;
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
    JOIN dbo.PhanTichAI p ON p.MaLuotKiemDuyet = i.MaLuotKiemDuyet
    JOIN dbo.SanPham s ON s.MaSanPham = i.MaSanPham
    JOIN dbo.CuaHang c ON c.MaCuaHang = s.MaCuaHang
    WHERE i.QuyetDinh = 'REJECTED' AND d.QuyetDinh IS NULL
      AND i.QuyetDinhTuDong = 0 AND i.MaQuanTriVien IS NOT NULL
      AND p.TrangThai = 'DONE' AND p.MucDoTuKhoa = 'LOW'
      AND NOT EXISTS (SELECT 1 FROM dbo.LichSuViPham v WHERE v.MaLuotKiemDuyet = i.MaLuotKiemDuyet);
END
GO

PRINT N'C2: đã áp dụng 17_ghi_vi_pham_low_khi_tu_choi_c2.sql.';
GO
