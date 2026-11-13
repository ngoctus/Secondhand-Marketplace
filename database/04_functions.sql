SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ARITHABORT ON;
GO

USE SecondHandMarketplace;
GO

-- 06 scalar/table functions; fn_KiemTraRuleSanPham dùng phiên bản cuối từ module 17.

CREATE OR ALTER FUNCTION dbo.fn_ChuanHoaChuoi (@VanBan nvarchar(max))
RETURNS nvarchar(max)
AS
BEGIN
    IF @VanBan IS NULL RETURN NULL;

    DECLARE @KQ nvarchar(max) = LOWER(@VanBan);
    DECLARE @Nhom nvarchar(40);

    SET @Nhom = N'àáảãạăằắẳẵặâầấẩẫậ';   SET @KQ = TRANSLATE(@KQ, @Nhom, REPLICATE(N'a', LEN(@Nhom)));
    SET @Nhom = N'èéẻẽẹêềếểễệ';          SET @KQ = TRANSLATE(@KQ, @Nhom, REPLICATE(N'e', LEN(@Nhom)));
    SET @Nhom = N'ìíỉĩị';                SET @KQ = TRANSLATE(@KQ, @Nhom, REPLICATE(N'i', LEN(@Nhom)));
    SET @Nhom = N'òóỏõọôồốổỗộơờớởỡợ';   SET @KQ = TRANSLATE(@KQ, @Nhom, REPLICATE(N'o', LEN(@Nhom)));
    SET @Nhom = N'ùúủũụưừứửữự';          SET @KQ = TRANSLATE(@KQ, @Nhom, REPLICATE(N'u', LEN(@Nhom)));
    SET @Nhom = N'ỳýỷỹỵ';                SET @KQ = TRANSLATE(@KQ, @Nhom, REPLICATE(N'y', LEN(@Nhom)));
    SET @KQ = REPLACE(@KQ, N'đ', N'd');

    SET @Nhom = N'.,;:!?()[]{}"''/\-_+*&%$#@^~|<>=';
    SET @KQ = TRANSLATE(@KQ, @Nhom, REPLICATE(N' ', LEN(@Nhom)));
    SET @Nhom = NCHAR(9) + NCHAR(10) + NCHAR(13) + NCHAR(160);
    SET @KQ = TRANSLATE(@KQ, @Nhom, REPLICATE(N' ', LEN(@Nhom)));

    WHILE CHARINDEX(N'  ', @KQ) > 0
        SET @KQ = REPLACE(@KQ, N'  ', N' ');

    RETURN LTRIM(RTRIM(@KQ));
END
GO

CREATE OR ALTER FUNCTION dbo.fn_TimTuKhoaCam (@VanBan nvarchar(max))
RETURNS TABLE
AS
RETURN(
    SELECT k.MaTuKhoaCam AS Id, k.CumTu, k.Nhom
    FROM TuKhoaCam k
    CROSS JOIN (SELECT N' ' + dbo.fn_ChuanHoaChuoi(@VanBan) + N' ' AS VB) x
    WHERE k.DangApDung = 1
      AND CHARINDEX(N' ' + k.CumTuChuanHoa + N' ', x.VB) > 0
);
GO

CREATE OR ALTER FUNCTION dbo.fn_ChuyenTrangThaiSanPhamHopLe (@Cu varchar(20), @Moi varchar(20))
RETURNS bit
AS
BEGIN
    RETURN CASE WHEN EXISTS (
        SELECT 1
        FROM (VALUES
            ('DRAFT',          'PENDING_REVIEW'),
            ('PENDING_REVIEW', 'ACTIVE'),
            ('PENDING_REVIEW', 'REJECTED'),
            ('REJECTED',       'PENDING_REVIEW'),
            ('ACTIVE',         'HIDDEN'),
            ('ACTIVE',         'RESERVED'),
            ('ACTIVE',         'REMOVED'),
            ('ACTIVE',         'PENDING_REVIEW'),
            ('HIDDEN',         'ACTIVE'),
            ('HIDDEN',         'PENDING_REVIEW'),
            ('RESERVED',       'ACTIVE'),
            ('RESERVED',       'SOLD'),
            ('RESERVED',       'REMOVED'),
            ('SOLD',           'REMOVED'),
            ('HIDDEN',         'REMOVED'),
            ('PENDING_REVIEW', 'REMOVED'),
            ('REMOVED',        'ACTIVE'),
            ('REMOVED',        'PENDING_REVIEW')
        ) t (Cu, Moi)
        WHERE t.Cu = @Cu AND t.Moi = @Moi) THEN 1 ELSE 0 END;
END
GO

CREATE OR ALTER FUNCTION dbo.fn_ChuyenTrangThaiDonHangHopLe (@Cu varchar(15), @Moi varchar(15))
RETURNS bit
AS
BEGIN
    RETURN CASE WHEN EXISTS (
        SELECT 1
        FROM (VALUES
            ('PENDING',   'CONFIRMED'),
            ('PENDING',   'CANCELLED'),
            ('CONFIRMED', 'SHIPPING'),
            ('CONFIRMED', 'CANCELLED'),
            ('SHIPPING',  'DELIVERED'),
            ('SHIPPING',  'COMPLETED'),
            ('SHIPPING',  'CANCELLED'),
            ('DELIVERED', 'COMPLETED'),
            ('DELIVERED', 'CANCELLED'),
            ('COMPLETED', 'CANCELLED')
        ) t (Cu, Moi)
        WHERE t.Cu = @Cu AND t.Moi = @Moi) THEN 1 ELSE 0 END;
END
GO

CREATE OR ALTER FUNCTION dbo.fn_TinhTongTienDonHang (@MaDonHang int)
RETURNS decimal(18,2)
AS
BEGIN
    RETURN (SELECT ISNULL(SUM(ct.Gia), 0) FROM ChiTietDonHang ct WHERE ct.MaDonHang = @MaDonHang)
         + ISNULL((SELECT dh.PhiVanChuyen FROM DonHang dh WHERE dh.MaDonHang = @MaDonHang), 0);
END
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
