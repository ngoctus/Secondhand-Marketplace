-- =========================================
-- 1. CONSTRAINTS CHO BẢNG GioHang
-- =========================================
-- 1.1. Khóa ngoại: người mua
ALTER TABLE GioHang
ADD CONSTRAINT FK_GioHang_NguoiMua
FOREIGN KEY (MaNguoiMua)
REFERENCES NguoiDung(MaNguoiDung);
GO

-- 1.2. Khóa ngoại: sản phẩm
ALTER TABLE GioHang
ADD CONSTRAINT FK_GioHang_SanPham
FOREIGN KEY (MaSanPham)
REFERENCES SanPham(MaSanPham);
GO

-- 1.3. Không cho phép trùng sản phẩm trong giỏ của cùng người mua
ALTER TABLE GioHang
ADD CONSTRAINT UQ_GioHang_NguoiMua_SanPham
UNIQUE (MaNguoiMua, MaSanPham);
GO

-- 1.4. Số lượng phải lớn hơn hoặc bằng 1
ALTER TABLE GioHang
ADD CONSTRAINT CK_GioHang_SoLuong
CHECK (SoLuong >= 1);
GO

-- =========================================
-- 2. CONSTRAINTS CHO BẢNG DonHang
-- =========================================
-- 2.1. Mã đơn hàng không được trùng
ALTER TABLE DonHang
ADD CONSTRAINT UQ_DonHang_MaDon
UNIQUE (MaDon);
GO

-- 2.2. Khóa ngoại: người mua
ALTER TABLE DonHang
ADD CONSTRAINT FK_DonHang_NguoiMua
FOREIGN KEY (MaNguoiMua)
REFERENCES NguoiDung(MaNguoiDung);
GO

-- 2.3. Khóa ngoại: người bán
ALTER TABLE DonHang
ADD CONSTRAINT FK_DonHang_NguoiBan
FOREIGN KEY (MaNguoiBan)
REFERENCES NguoiDung(MaNguoiDung);
GO

-- 2.4. Người mua phải khác người bán
ALTER TABLE DonHang
ADD CONSTRAINT CK_DonHang_NguoiMuaKhacNguoiBan
CHECK (MaNguoiMua <> MaNguoiBan);
GO

-- 2.5. Kiểm tra tiền hàng, phí vận chuyển và tổng tiền
ALTER TABLE DonHang
ADD CONSTRAINT CK_DonHang_SoTien
CHECK
(
    TienHang >= 0
    AND PhiVanChuyen >= 0
    AND TongTien = TienHang + PhiVanChuyen
);
GO

-- 2.6. Kiểm tra phương thức thanh toán
ALTER TABLE DonHang
ADD CONSTRAINT CK_DonHang_PhuongThucTT
CHECK (PhuongThucThanhToan IN
    ('COD', 'BANK_TRANSFER', 'ONLINE'));
GO

-- 2.7. Kiểm tra trạng thái thanh toán
ALTER TABLE DonHang
ADD CONSTRAINT CK_DonHang_TrangThaiTT
CHECK (TrangThaiThanhToan IN
    ('UNPAID', 'PENDING', 'PAID', 'REFUNDED', 'FAILED'));
GO

-- 2.8. Kiểm tra trạng thái đơn hàng
ALTER TABLE DonHang
ADD CONSTRAINT CK_DonHang_TrangThai
CHECK (TrangThaiDonHang IN
(
    'PENDING', 'CONFIRMED', 'PREPARING', 'SHIPPING',
    'DELIVERED', 'COMPLETED', 'CANCELLED', 'DISPUTED'
));
GO

-- 2.9. Số điện thoại người nhận tối thiểu 9 ký tự
ALTER TABLE DonHang
ADD CONSTRAINT CK_DonHang_SDT
CHECK (LEN(LTRIM(RTRIM(SDTNguoiNhan))) >= 9);
GO

-- 2.10. Tên người nhận không được để trống
ALTER TABLE DonHang
ADD CONSTRAINT CK_DonHang_NguoiNhan
CHECK (LEN(LTRIM(RTRIM(TenNguoiNhan))) > 0);
GO

-- =========================================
-- 3. CONSTRAINTS CHO BẢNG ChiTietDonHang
-- =========================================
ALTER TABLE ChiTietDonHang
ADD CONSTRAINT FK_ChiTietDonHang_DonHang 
FOREIGN KEY (MaDonHang) 
REFERENCES DonHang(MaDonHang);
GO

ALTER TABLE ChiTietDonHang
ADD CONSTRAINT FK_ChiTietDonHang_SanPham 
FOREIGN KEY (MaSanPham) 
REFERENCES SanPham(MaSanPham);
GO

ALTER TABLE ChiTietDonHang
ADD CONSTRAINT CK_ChiTietDonHang_DonGia 
CHECK (DonGia > 0);
GO

ALTER TABLE ChiTietDonHang
ADD CONSTRAINT CK_ChiTietDonHang_SoLuong 
CHECK (SoLuong = 1);
GO

-- =========================================
-- 4. CONSTRAINTS CHO BẢNG LichSuTrangThaiDonHang
-- =========================================
ALTER TABLE LichSuTrangThaiDonHang
ADD CONSTRAINT FK_LichSuDonHang_DonHang 
FOREIGN KEY (MaDonHang) 
REFERENCES DonHang(MaDonHang);
GO

ALTER TABLE LichSuTrangThaiDonHang
ADD CONSTRAINT FK_LichSuDonHang_NguoiDung 
FOREIGN KEY (NguoiThayDoi) 
REFERENCES NguoiDung(UserId);
GO


-- =========================================
-- 5. CONSTRAINTS CHO BẢNG ThanhToan
-- =========================================
ALTER TABLE ThanhToan
ADD CONSTRAINT FK_ThanhToan_DonHang 
FOREIGN KEY (MaDonHang) 
REFERENCES DonHang(MaDonHang);
GO

ALTER TABLE ThanhToan
ADD CONSTRAINT CK_ThanhToan_PhuongThuc 
CHECK (PhuongThucThanhToan IN ('COD', 'BANK_TRANSFER', 'ONLINE'));
GO

ALTER TABLE ThanhTOan
ADD CONSTRAINT CK_ThanhToan_TrangThai 
CHECK (TrangThaiThanhToan IN ('PENDING', 'SUCCESS', 'FAILED', 'REFUNDED'));
GO

ALTER TABLE ThanhToan
ADD CONSTRAINT CK_ThanhToan_SoTien 
CHECK (SoTien > 0);
GO
