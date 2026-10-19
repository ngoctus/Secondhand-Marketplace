/* vw_ChiTietDonHang:
   Chi tiết đơn hàng gồm người mua, người bán và sản phẩm.
   Dùng cho trang chi tiết đơn hàng. */
CREATE OR ALTER VIEW vw_ChiTietDonHang
AS
SELECT
    dh.MaDonHang,
    dh.MaDon,
    dh.MaNguoiMua,
    nm.HoTen AS TenNguoiMua,
    dh.MaNguoiBan,
    nb.HoTen AS TenNguoiBan,
    dh.TenNguoiNhan,
    dh.SDTNguoiNhan,
    dh.DiaChiGiaoHang,
    dh.TienHang,
    dh.PhiVanChuyen,
    dh.TongTien,
    dh.PhuongThucThanhToan,
    dh.TrangThaiThanhToan,
    dh.TrangThaiDonHang,
    dh.NgayTao,
    ct.MaChiTietDonHang,
    ct.MaSanPham,
    ct.TenSanPham,
    ct.DonGia,
    ct.SoLuong,
    ct.ThanhTien
FROM DonHang AS dh
JOIN NguoiDung AS nm ON nm.UserId = dh.MaNguoiMua
JOIN NguoiDung AS nb ON nb.UserId = dh.MaNguoiBan
JOIN ChiTietDonHang AS ct ON ct.MaDonHang = dh.MaDonHang;
GO


/* vw_ThongKeNguoiBan:
   Thống kê số sản phẩm, số đơn hàng và doanh thu từng người bán.
   Tách tổng hợp sản phẩm và đơn hàng để tránh nhận doanh thu. */
CREATE OR ALTER VIEW vw_ThongKeNguoiBan
AS
WITH ThongKeSanPham AS
(
    SELECT
        MaNguoiBan,
        COUNT(*) AS TongSanPham,
        SUM(CASE WHEN TrangThaiSanPham = 'SOLD'
                 THEN 1 ELSE 0 END) AS SanPhamDaBan
    FROM SanPham
    GROUP BY MaNguoiBan
),
ThongKeDonHang AS
(
    SELECT
        MaNguoiBan,
        COUNT(*) AS TongDonHang,
        SUM(CASE WHEN TrangThaiDonHang = 'COMPLETED'
                 THEN 1 ELSE 0 END) AS DonHangHoanTat,
        SUM(CASE WHEN TrangThaiDonHang = 'COMPLETED'
                 THEN TongTien ELSE 0 END) AS DoanhThu
    FROM DonHang
    GROUP BY MaNguoiBan
)
SELECT
    nd.UserId AS MaNguoiBan,
    nd.HoTen,
    hs.TenCuaHang,
    ISNULL(sp.TongSanPham, 0) AS TongSanPham,
    ISNULL(sp.SanPhamDaBan, 0) AS SanPhamDaBan,
    ISNULL(dh.TongDonHang, 0) AS TongDonHang,
    ISNULL(dh.DonHangHoanTat, 0) AS DonHangHoanTat,
    ISNULL(dh.DoanhThu, 0) AS DoanhThu
FROM NguoiDung AS nd
LEFT JOIN HoSoNguoiBan AS hs ON hs.MaNguoiBan = nd.UserId
LEFT JOIN ThongKeSanPham AS sp ON sp.MaNguoiBan = nd.UserId
LEFT JOIN ThongKeDonHang AS dh ON dh.MaNguoiBan = nd.UserId
WHERE nd.VaiTro = 'SELLER';
GO