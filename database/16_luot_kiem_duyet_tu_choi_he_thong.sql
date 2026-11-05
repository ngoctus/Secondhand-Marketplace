SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ARITHABORT ON;
GO

USE SecondHandMarketplace;
GO

/* =====================================================================
   C2 - CHO PHÉP "TỪ CHỐI TỰ ĐỘNG BỞI HỆ THỐNG" TRÊN LuotKiemDuyet
   *** THAY ĐỔI RÀNG BUỘC CỦA BẢNG DÙNG CHUNG - CẦN NGƯỜI C/NHÓM ĐỒNG Ý TRƯỚC KHI CHẠY ***

   VÌ SAO CẦN: nghiệp vụ HIGH yêu cầu tự từ chối, không giả mạo Admin.
   Schema hiện tại (03_constraints.sql) có CK_LuotKiemDuyet_DaQuyetDinh bắt buộc
   QuyetDinh NOT NULL <=> MaQuanTriVien NOT NULL, và trigger trg_LuotKiemDuyet_KiemTra chỉ nhận ADMIN.
   => Không có cách hợp lệ để "đóng" một lượt bằng quyết định hệ thống. Nếu để lượt mở
   mà sản phẩm đã REJECTED thì UX_LuotKiemDuyet_DangCho chặn người bán gửi duyệt lại.

   CÁCH SỬA (nhỏ, tương thích ngược):
     - Thêm cột QuyetDinhTuDong bit NOT NULL DEFAULT 0.
     - Thay CHECK DaQuyetDinh bằng bản nới có điều kiện: MaQuanTriVien chỉ được NULL khi
       QuyetDinhTuDong = 1 VÀ QuyetDinh = 'REJECTED'. Duyệt (APPROVED) luôn bắt buộc có Admin.
     - Mọi dòng cũ và mọi procedure cũ (sp_QuyetDinhKiemDuyet không biết cột mới, mặc định 0) vẫn hợp lệ.
   Không sửa procedure/trigger nào. Hoàn tác: xem cuối file.
   Thứ tự: chạy SAU 14_kiemduyet_tukhoa_c2.sql.
   ===================================================================== */

IF COL_LENGTH('dbo.LuotKiemDuyet', 'QuyetDinhTuDong') IS NULL
    ALTER TABLE dbo.LuotKiemDuyet
        ADD QuyetDinhTuDong bit NOT NULL CONSTRAINT DF_LuotKiemDuyet_QuyetDinhTuDong DEFAULT (0);
GO

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_LuotKiemDuyet_DaQuyetDinh' AND parent_object_id = OBJECT_ID('dbo.LuotKiemDuyet'))
    ALTER TABLE dbo.LuotKiemDuyet DROP CONSTRAINT CK_LuotKiemDuyet_DaQuyetDinh;
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_LuotKiemDuyet_DaQuyetDinh_C2' AND parent_object_id = OBJECT_ID('dbo.LuotKiemDuyet'))
    ALTER TABLE dbo.LuotKiemDuyet ADD CONSTRAINT CK_LuotKiemDuyet_DaQuyetDinh_C2 CHECK (
          -- đang chờ
          (QuyetDinh IS NULL AND MaQuanTriVien IS NULL AND ThoiGianQuyetDinh IS NULL AND LyDo IS NULL AND QuyetDinhTuDong = 0)
          -- Admin quyết định (như cũ)
       OR (QuyetDinh IS NOT NULL AND MaQuanTriVien IS NOT NULL AND ThoiGianQuyetDinh IS NOT NULL AND QuyetDinhTuDong = 0)
          -- Hệ thống tự từ chối (chỉ REJECTED, không có Admin)
       OR (QuyetDinh = 'REJECTED' AND MaQuanTriVien IS NULL AND ThoiGianQuyetDinh IS NOT NULL AND QuyetDinhTuDong = 1));
GO

PRINT N'C2: đã áp dụng 15_luot_kiem_duyet_tu_choi_he_thong_c2.sql.';
GO

/* HOÀN TÁC (chỉ khi chưa có dòng QuyetDinhTuDong = 1):
   ALTER TABLE dbo.LuotKiemDuyet DROP CONSTRAINT CK_LuotKiemDuyet_DaQuyetDinh_C2;
   ALTER TABLE dbo.LuotKiemDuyet DROP CONSTRAINT DF_LuotKiemDuyet_QuyetDinhTuDong;
   ALTER TABLE dbo.LuotKiemDuyet DROP COLUMN QuyetDinhTuDong;
   ALTER TABLE dbo.LuotKiemDuyet ADD CONSTRAINT CK_LuotKiemDuyet_DaQuyetDinh CHECK (
       (QuyetDinh IS NULL AND MaQuanTriVien IS NULL AND ThoiGianQuyetDinh IS NULL AND LyDo IS NULL)
    OR (QuyetDinh IS NOT NULL AND MaQuanTriVien IS NOT NULL AND ThoiGianQuyetDinh IS NOT NULL));
*/
