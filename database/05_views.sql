SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

USE SecondHandMarketplace;
GO

-- Danh sách sản phẩm công khai: ACTIVE, chưa xóa, shop + chủ shop không bị khóa
CREATE OR ALTER VIEW vw_SanPhamCongKhai AS
SELECT sp.MaSanPham AS Id, sp.Ten, sp.ChuoiTimKiem, sp.MoTa, sp.Gia, sp.NgayTao,
       sp.MaDanhMuc,   dm.Ten AS TenDanhMuc,
       sp.MaTinhTrang, tt.Ten AS TenTinhTrang,
       sp.MaCuaHang,   ch.TenShop,
       a.DuongDan AS AnhDaiDien
FROM SanPham sp
JOIN CuaHang ch          ON ch.MaCuaHang = sp.MaCuaHang AND ch.TrangThai = 'ACTIVE'
JOIN NguoiDung nd        ON nd.MaNguoiDung = ch.MaNguoiDung AND nd.TrangThai = 'ACTIVE'
JOIN DanhMuc dm          ON dm.MaDanhMuc = sp.MaDanhMuc
JOIN TinhTrangSanPham tt ON tt.MaTinhTrang = sp.MaTinhTrang
LEFT JOIN AnhSanPham a   ON a.MaSanPham = sp.MaSanPham AND a.LaAnhChinh = 1
WHERE sp.TrangThai = 'ACTIVE' AND sp.DaXoa = 0;
GO

-- Trang chi tiết: ACTIVE / RESERVED / SOLD đều xem được, CoTheMua = 0 thì ẩn nút mua
CREATE OR ALTER VIEW vw_SanPhamChiTietCongKhai AS
SELECT sp.MaSanPham AS Id, sp.Ten, sp.MoTa, sp.Gia, sp.TrangThai, sp.NgayTao,
       CAST(CASE WHEN sp.TrangThai = 'ACTIVE' THEN 1 ELSE 0 END AS bit) AS CoTheMua,
       sp.MaDanhMuc,   dm.Ten AS TenDanhMuc,
       sp.MaTinhTrang, tt.Ten AS TenTinhTrang,
       sp.MaCuaHang,   ch.TenShop, ch.MoTa AS MoTaShop, ch.Logo AS LogoShop,
       ch.MaNguoiDung AS ChuShopId
FROM SanPham sp
JOIN CuaHang ch          ON ch.MaCuaHang = sp.MaCuaHang AND ch.TrangThai = 'ACTIVE'
JOIN NguoiDung nd        ON nd.MaNguoiDung = ch.MaNguoiDung AND nd.TrangThai = 'ACTIVE'
JOIN DanhMuc dm          ON dm.MaDanhMuc = sp.MaDanhMuc
JOIN TinhTrangSanPham tt ON tt.MaTinhTrang = sp.MaTinhTrang
WHERE sp.TrangThai IN ('ACTIVE','RESERVED','SOLD') AND sp.DaXoa = 0;
GO

-- Trang "Sản phẩm của shop mình": trạng thái, lý do từ chối/gỡ, có xóa được không
CREATE OR ALTER VIEW vw_SanPhamCuaShop AS
SELECT sp.MaSanPham AS Id, sp.MaCuaHang, sp.Ten, sp.Gia, sp.TrangThai, sp.LyDoGo, sp.NgayTao, sp.NgayCapNhat,
       sp.MaDanhMuc, dm.Ten AS TenDanhMuc,
       sp.MaTinhTrang, tt.Ten AS TenTinhTrang,
       a.DuongDan AS AnhDaiDien,
       (SELECT COUNT(*) FROM AnhSanPham x WHERE x.MaSanPham = sp.MaSanPham) AS SoAnh,
       lk.QuyetDinh AS KetQuaLanDuyetCuoi, lk.LyDo AS LyDoTuChoi, lk.ThoiGianQuyetDinh AS ThoiGianDuyetCuoi,
       CAST(CASE WHEN sp.TrangThai IN ('SOLD','RESERVED')
                   OR EXISTS (SELECT 1 FROM ChiTietDonHang ct WHERE ct.MaSanPham = sp.MaSanPham)
                 THEN 0 ELSE 1 END AS bit) AS CoTheXoa
FROM SanPham sp
LEFT JOIN DanhMuc dm          ON dm.MaDanhMuc = sp.MaDanhMuc
LEFT JOIN TinhTrangSanPham tt ON tt.MaTinhTrang = sp.MaTinhTrang
LEFT JOIN AnhSanPham a        ON a.MaSanPham = sp.MaSanPham AND a.LaAnhChinh = 1
OUTER APPLY (SELECT TOP (1) l.QuyetDinh, l.LyDo, l.ThoiGianQuyetDinh
             FROM LuotKiemDuyet l
             WHERE l.MaSanPham = sp.MaSanPham AND l.QuyetDinh IS NOT NULL
             ORDER BY l.ThoiGianQuyetDinh DESC, l.MaSanPham DESC) lk
WHERE sp.DaXoa = 0;
GO

-- Hàng chờ duyệt của Admin (cột ThoiGianGui để sắp xếp cũ nhất trước)
CREATE OR ALTER VIEW vw_SanPhamChoDuyet AS
SELECT l.MaLuotKiemDuyet AS LuotKiemDuyetId, sp.MaSanPham AS SanPhamId, sp.Ten, sp.Gia, sp.MoTa,
       sp.MaDanhMuc AS DanhMucId, dm.Ten AS TenDanhMuc,
       sp.MaCuaHang AS CuaHangId, ch.TenShop,
       a.DuongDan AS AnhDaiDien,
       l.ThoiGianGui, l.KetQuaRule,
       ai.TrangThai AS TrangThaiAI, ai.MucRuiRo, ai.DeXuat AS DeXuatAI
FROM LuotKiemDuyet l
JOIN SanPham sp        ON sp.MaSanPham = l.MaSanPham
JOIN CuaHang ch        ON ch.MaCuaHang = sp.MaCuaHang
LEFT JOIN DanhMuc dm   ON dm.MaDanhMuc = sp.MaDanhMuc
LEFT JOIN AnhSanPham a ON a.MaSanPham = sp.MaSanPham AND a.LaAnhChinh = 1
LEFT JOIN PhanTichAI ai ON ai.MaLuotKiemDuyet = l.MaLuotKiemDuyet
WHERE l.QuyetDinh IS NULL AND sp.TrangThai = 'PENDING_REVIEW' AND sp.DaXoa = 0;
GO

-- Giỏ hàng: CoTheMua = 0 khi sản phẩm đã hết / ẩn / shop bị khóa kể từ lúc thêm vào giỏ
CREATE OR ALTER VIEW vw_GioHangChiTiet AS
SELECT gh.MaGioHang AS Id, gh.MaNguoiDung AS NguoiDungId, gh.MaSanPham AS SanPhamId, gh.DaChon, gh.ThoiGianThem,
       sp.Ten, sp.Gia, sp.TrangThai AS TrangThaiSanPham,
       sp.MaCuaHang, ch.TenShop,
       a.DuongDan AS AnhDaiDien,
       CAST(CASE WHEN sp.TrangThai = 'ACTIVE' AND sp.DaXoa = 0 AND ch.TrangThai = 'ACTIVE'
                 THEN 1 ELSE 0 END AS bit) AS CoTheMua
FROM GioHang gh
JOIN SanPham sp        ON sp.MaSanPham = gh.MaSanPham
JOIN CuaHang ch        ON ch.MaCuaHang = sp.MaCuaHang
LEFT JOIN AnhSanPham a ON a.MaSanPham = sp.MaSanPham AND a.LaAnhChinh = 1;
GO

-- Danh sách đơn dùng cho cả 2 phía (lọc theo NguoiMuaId hoặc ChuShopId)
CREATE OR ALTER VIEW vw_DonHangTongQuan AS
SELECT dh.MaDonHang AS Id, dh.MaDon, dh.MaNguoiDung AS NguoiMuaId, mua.HoTen AS TenNguoiMua,
       dh.MaCuaHang, ch.TenShop, ch.MaNguoiDung AS ChuShopId,
       dh.TrangThai, dh.PhuongThucThanhToan, dh.TrangThaiThanhToan, dh.TongTien,
       dh.NguoiHuy, dh.LyDoHuy, dh.NgayTao,
       (SELECT COUNT(*) FROM ChiTietDonHang ct WHERE ct.MaDonHang = dh.MaDonHang) AS SoSanPham,
       (SELECT TOP (1) ct.TenSanPham FROM ChiTietDonHang ct WHERE ct.MaDonHang = dh.MaDonHang ORDER BY ct.MaChiTietDonHang) AS TenSanPhamDau,
       (SELECT TOP (1) ct.AnhDaiDien FROM ChiTietDonHang ct WHERE ct.MaDonHang = dh.MaDonHang ORDER BY ct.MaChiTietDonHang) AS AnhDau
FROM DonHang dh
JOIN NguoiDung mua ON mua.MaNguoiDung = dh.MaNguoiDung
JOIN CuaHang ch    ON ch.MaCuaHang = dh.MaCuaHang;
GO

-- Thống kê từng shop
CREATE OR ALTER VIEW vw_ThongKeCuaHang AS
SELECT ch.MaCuaHang AS CuaHangId, ch.TenShop,
       (SELECT COUNT(*) FROM SanPham sp WHERE sp.MaCuaHang = ch.MaCuaHang AND sp.DaXoa = 0)                         AS TongSanPham,
       (SELECT COUNT(*) FROM SanPham sp WHERE sp.MaCuaHang = ch.MaCuaHang AND sp.DaXoa = 0 AND sp.TrangThai = 'ACTIVE') AS SanPhamDangBan,
       (SELECT COUNT(*) FROM SanPham sp WHERE sp.MaCuaHang = ch.MaCuaHang AND sp.TrangThai = 'SOLD')                AS SanPhamDaBan,
       (SELECT COUNT(*) FROM DonHang dh WHERE dh.MaCuaHang = ch.MaCuaHang)                                          AS TongDon,
       (SELECT COUNT(*) FROM DonHang dh WHERE dh.MaCuaHang = ch.MaCuaHang AND dh.TrangThai = 'COMPLETED')           AS DonHoanTat,
       (SELECT ISNULL(SUM(dh.TongTien), 0) FROM DonHang dh WHERE dh.MaCuaHang = ch.MaCuaHang AND dh.TrangThai = 'COMPLETED') AS DoanhThu
FROM CuaHang ch;
GO

-- Danh sách đơn dùng cho cả 2 phía (lọc theo NguoiMuaId hoặc ChuShopId)
CREATE OR ALTER VIEW vw_ThongKeHeThong AS
SELECT (SELECT COUNT(*) FROM NguoiDung WHERE VaiTro = 'USER')                      AS TongNguoiDung,
       (SELECT COUNT(*) FROM CuaHang)                                              AS TongCuaHang,
       (SELECT COUNT(*) FROM SanPham WHERE DaXoa = 0)                              AS TongSanPham,
       (SELECT COUNT(*) FROM SanPham WHERE TrangThai = 'PENDING_REVIEW' AND DaXoa = 0) AS SanPhamChoDuyet,
       (SELECT COUNT(*) FROM DonHang)                                              AS TongDonHang,
       (SELECT COUNT(*) FROM DonHang WHERE TrangThai = 'COMPLETED')                AS DonHoanTat,
       (SELECT ISNULL(SUM(TongTien), 0) FROM DonHang WHERE TrangThai = 'COMPLETED') AS DoanhThu;
GO

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