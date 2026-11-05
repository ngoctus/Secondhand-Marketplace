SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

USE SecondHandMarketplace;
GO

/* =====================================================================
   C2 - BỎ RULE "TU_CAM" KHỎI fn_KiemTraRuleSanPham
   *** SỬA HÀM THUỘC 07_functions.sql (người làm CSDL/B) - CẦN ĐỒNG Ý TRƯỚC KHI CHẠY ***

   VÌ SAO CẦN: sp_GuiDuyetSanPham gọi fn_KiemTraRuleSanPham; hiện hàm này trả lỗi TU_CAM nếu tên/mô tả
   chứa BẤT KỲ từ khóa cấm nào => sản phẩm bị chặn ngay lúc gửi duyệt, không bao giờ tới được
   PhanTichSanPhamAsync. Nghiệp vụ C2 cần sản phẩm đi qua gửi duyệt rồi mới phân tích:
   HIGH tự từ chối, LOW chờ Admin.

   Bản dưới đây là NGUYÊN VĂN hàm cũ, chỉ bỏ 2 khối UNION ALL 'TU_CAM'. Các rule khác không đổi.
   KHUYẾN NGHỊ: người sở hữu 07_functions.sql xóa 2 khối đó trực tiếp trong 07 (diff nằm trong
   docs/c2-moderation-handoff.md) rồi bỏ file này. Nếu chạy file này mà sau đó chạy lại 07 thì rule TU_CAM quay lại.
   Thứ tự: chạy SAU 07_functions.sql (và sau 14, 15).
   ===================================================================== */

CREATE OR ALTER FUNCTION dbo.fn_KiemTraRuleSanPham (@MaSanPham int)
RETURNS TABLE
AS
RETURN(
    SELECT N'RULE_TEN' AS MaLoi, N'Ten' AS Truong, N'Tên sản phẩm phải có ít nhất 5 ký tự.' AS ThongBao
    FROM SanPham sp WHERE sp.MaSanPham = @MaSanPham AND LEN(LTRIM(RTRIM(sp.Ten))) < 5
    UNION ALL
    SELECT N'RULE_GIA', N'Gia', N'Giá phải lớn hơn 0.'
    FROM SanPham sp WHERE sp.MaSanPham = @MaSanPham AND (sp.Gia IS NULL OR sp.Gia <= 0)
    UNION ALL
    SELECT N'RULE_MO_TA', N'MoTa', N'Mô tả phải có ít nhất 20 ký tự.'
    FROM SanPham sp WHERE sp.MaSanPham = @MaSanPham AND LEN(LTRIM(RTRIM(ISNULL(sp.MoTa, N'')))) < 20
    UNION ALL
    SELECT N'RULE_ANH', N'Anh', N'Sản phẩm phải có ít nhất 1 ảnh.'
    FROM SanPham sp WHERE sp.MaSanPham = @MaSanPham
      AND NOT EXISTS (SELECT 1 FROM AnhSanPham a WHERE a.MaSanPham = sp.MaSanPham)
    UNION ALL
    SELECT N'RULE_DANH_MUC', N'MaDanhMuc', N'Danh mục không hợp lệ hoặc đang bị ẩn.'
    FROM SanPham sp WHERE sp.MaSanPham = @MaSanPham
      AND NOT EXISTS (SELECT 1 FROM DanhMuc dm WHERE dm.MaDanhMuc = sp.MaDanhMuc AND dm.DangHienThi = 1)
    UNION ALL
    SELECT N'RULE_TINH_TRANG', N'MaTinhTrang', N'Tình trạng sản phẩm không hợp lệ hoặc đã ngừng sử dụng.'
    FROM SanPham sp WHERE sp.MaSanPham = @MaSanPham
      AND NOT EXISTS (SELECT 1 FROM TinhTrangSanPham tt WHERE tt.MaTinhTrang = sp.MaTinhTrang AND tt.DangSuDung = 1)
);
GO

PRINT N'C2: đã áp dụng 16_bo_chan_tu_cam_khi_gui_duyet_c2.sql.';
GO
