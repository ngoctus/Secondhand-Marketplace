USE SecondHandMarketplace;
GO
SET NOCOUNT ON;

DECLARE @Loi TABLE (KiemTra nvarchar(80) NOT NULL, DoiTuong nvarchar(120) NOT NULL, ChiTiet nvarchar(400) NOT NULL);

-- Mỗi đánh giá phải thuộc đơn COMPLETED của đúng người mua và shop.
INSERT @Loi (KiemTra, DoiTuong, ChiTiet)
SELECT N'DanhGia', CONVERT(nvarchar(120), dg.MaDanhGia), N'Đơn không hoàn tất hoặc người mua/shop không khớp đơn.'
FROM dbo.DanhGia dg
LEFT JOIN dbo.DonHang dh ON dh.MaDonHang = dg.MaDonHang
WHERE dh.MaDonHang IS NULL OR dh.TrangThai <> 'COMPLETED'
   OR dh.MaNguoiDung <> dg.MaNguoiMua OR dh.MaCuaHang <> dg.MaCuaHang;

-- Đơn phải có chủ thể hợp lệ; mọi sản phẩm trong chi tiết phải thuộc shop của đơn.
INSERT @Loi (KiemTra, DoiTuong, ChiTiet)
SELECT N'DonHang', CONVERT(nvarchar(120), dh.MaDonHang), N'Thiếu người mua, shop hoặc shop không thuộc người bán.'
FROM dbo.DonHang dh
LEFT JOIN dbo.NguoiDung nd ON nd.MaNguoiDung = dh.MaNguoiDung
LEFT JOIN dbo.CuaHang ch ON ch.MaCuaHang = dh.MaCuaHang
WHERE nd.MaNguoiDung IS NULL OR ch.MaCuaHang IS NULL OR ch.MaNguoiDung = dh.MaNguoiDung;

INSERT @Loi (KiemTra, DoiTuong, ChiTiet)
SELECT N'ChiTietDonHang', CONVERT(nvarchar(120), ct.MaChiTietDonHang), N'Sản phẩm không thuộc shop của đơn hàng.'
FROM dbo.ChiTietDonHang ct
JOIN dbo.DonHang dh ON dh.MaDonHang = ct.MaDonHang
JOIN dbo.SanPham sp ON sp.MaSanPham = ct.MaSanPham
WHERE sp.MaCuaHang <> dh.MaCuaHang;

-- Sản phẩm có giao dịch hoàn tất không được tiếp tục hiện là còn hàng.
INSERT @Loi (KiemTra, DoiTuong, ChiTiet)
SELECT N'SanPhamDaBan', CONVERT(nvarchar(120), sp.MaSanPham), N'Có đơn COMPLETED nhưng trạng thái sản phẩm chưa là SOLD.'
FROM dbo.SanPham sp
WHERE EXISTS (
    SELECT 1 FROM dbo.ChiTietDonHang ct
    JOIN dbo.DonHang dh ON dh.MaDonHang = ct.MaDonHang
    WHERE ct.MaSanPham = sp.MaSanPham AND dh.TrangThai = 'COMPLETED'
) AND sp.TrangThai <> 'SOLD';

-- Bộ seed phải có ít nhất một shop cần rà soát thật từ 5 đánh giá hiển thị trở lên.
IF NOT EXISTS (
    SELECT 1 FROM dbo.DanhGia dg
    WHERE dg.TrangThai = 'VISIBLE'
    GROUP BY dg.MaCuaHang
    HAVING COUNT(*) >= 5 AND AVG(CONVERT(decimal(5,2), dg.SoSao)) < 3
)
    INSERT @Loi VALUES (N'ShopCanRaSoat', N'(seed)', N'Chưa có shop dưới 3 sao với ít nhất 5 đánh giá hiển thị.');

SELECT KiemTra, DoiTuong, ChiTiet FROM @Loi ORDER BY KiemTra, DoiTuong;
IF EXISTS (SELECT 1 FROM @Loi)
    THROW 51091, N'Seed consistency thất bại; xem các dòng lỗi ở kết quả trước.', 1;
GO
