CREATE INDEX IX_DonHang_NguoiMua
ON DonHang(MaNguoiMua, NgayTao DESC);
GO

CREATE INDEX IX_DonHang_NguoiBan
ON DonHang(MaNguoiBan, NgayTao DESC);
GO

CREATE INDEX IX_DonHang_TrangThai
ON DonHang(TrangThaiDonHang, NgayTao DESC);
GO

CREATE INDEX IX_ChiTietDonHang_SanPham
ON ChiTietDonHang(MaSanPham);
GO

CREATE UNIQUE INDEX UX_ThanhToan_MaGiaoDich
ON ThanhToan(MaGiaoDich)
WHERE MaGiaoDich IS NOT NULL;
GO
