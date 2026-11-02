SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

USE SecondHandMarketplace;
GO

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
    UNION ALL
    SELECT N'TU_CAM', N'Ten', N'Tên chứa từ/cụm từ bị cấm: "' + k.CumTu + N'".'
    FROM SanPham sp CROSS APPLY dbo.fn_TimTuKhoaCam(sp.Ten) k WHERE sp.MaSanPham = @MaSanPham
    UNION ALL
    SELECT N'TU_CAM', N'MoTa', N'Mô tả chứa từ/cụm từ bị cấm: "' + k.CumTu + N'".'
    FROM SanPham sp CROSS APPLY dbo.fn_TimTuKhoaCam(sp.MoTa) k WHERE sp.MaSanPham = @MaSanPham
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
            ('DELIVERED', 'COMPLETED')
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

PRINT N'Đã tạo xong file Functions.';
GO