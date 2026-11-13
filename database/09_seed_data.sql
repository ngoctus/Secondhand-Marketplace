SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ARITHABORT ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

USE SecondHandMarketplace;
GO


BEGIN TRANSACTION;

INSERT TinhTrangSanPham (Ma, Ten, MoTa, ThuTuHienThi)
SELECT v.Ma, v.Ten, v.MoTa, v.ThuTu
FROM (VALUES
    ('NEW',      N'Mới 100%',  N'Chưa qua sử dụng, còn nguyên tem mác', 1),
    ('LIKE_NEW', N'Như mới',   N'Đã dùng rất ít, gần như không có dấu hiệu sử dụng', 2),
    ('GOOD',     N'Tốt',       N'Dùng bình thường, hoạt động tốt, ít trầy xước', 3),
    ('FAIR',     N'Khá',       N'Có dấu hiệu sử dụng rõ nhưng vẫn dùng ổn', 4),
    ('WORN',     N'Cũ',        N'Hao mòn nhiều, phù hợp giá rẻ', 5)
) v (Ma, Ten, MoTa, ThuTu)
WHERE NOT EXISTS (SELECT 1 FROM TinhTrangSanPham t WHERE t.Ma = v.Ma);

INSERT DanhMuc (Ten, ThuTuHienThi)
SELECT v.Ten, v.ThuTu
FROM (VALUES
    (N'Thời trang', 1), (N'Đồ điện tử', 2), (N'Đồ gia dụng', 3),
    (N'Sách & văn phòng phẩm', 4), (N'Xe cộ & phụ kiện', 5),
    (N'Đồ chơi & mẹ bé', 6), (N'Thể thao & dã ngoại', 7),
    (N'Nội thất', 8), (N'Khác', 99)
) v (Ten, ThuTu)
WHERE NOT EXISTS (SELECT 1 FROM DanhMuc d WHERE d.Ten = v.Ten);

INSERT TuKhoaCam (CumTu, CumTuChuanHoa, Nhom, GhiChu)
SELECT v.CumTu, dbo.fn_ChuanHoaChuoi(v.CumTu), v.Nhom, N'Seed ban đầu'
FROM (VALUES
    (N'hàng giả', 'HANG_GIA'), (N'hàng nhái', 'HANG_GIA'), (N'hàng fake', 'HANG_GIA'),
    (N'super fake', 'HANG_GIA'), (N'replica', 'HANG_GIA'),
    (N'hàng cấm', 'HANG_CAM'), (N'vũ khí', 'HANG_CAM'), (N'ma túy', 'HANG_CAM'),
    (N'pháo nổ', 'HANG_CAM'), (N'hàng trộm cắp', 'HANG_CAM'), (N'giấy tờ giả', 'HANG_CAM'),
    (N'chuyển khoản trước', 'LUA_DAO'), (N'đặt cọc trước', 'LUA_DAO'), (N'trúng thưởng', 'LUA_DAO')
) v (CumTu, Nhom)
WHERE NOT EXISTS (SELECT 1 FROM TuKhoaCam k WHERE k.CumTuChuanHoa = dbo.fn_ChuanHoaChuoi(v.CumTu));

COMMIT TRANSACTION;
GO

SELECT N'DanhMuc' AS Bang, COUNT(*) AS SoDong FROM DanhMuc
UNION ALL SELECT N'TinhTrangSanPham', COUNT(*) FROM TinhTrangSanPham
UNION ALL SELECT N'TuKhoaCam', COUNT(*) FROM TuKhoaCam;
GO
