SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

USE SecondHandMarketplace;
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SanPham_ACTIVE_MaDanhMuc_Gia' AND object_id = OBJECT_ID(N'SanPham'))
BEGIN
    PRINT N'Index da ton tai, bo qua 04_indexes.sql.';
    SET NOEXEC ON;
END
GO

CREATE NONCLUSTERED INDEX IX_SanPham_ACTIVE_MaDanhMuc_Gia
    ON SanPham (MaDanhMuc, Gia) INCLUDE (MaCuaHang, MaTinhTrang, NgayTao)
    WHERE TrangThai = 'ACTIVE' AND DaXoa = 0;

CREATE NONCLUSTERED INDEX IX_SanPham_ACTIVE_NgayTao
    ON SanPham (NgayTao DESC) INCLUDE (Gia, MaDanhMuc)
    WHERE TrangThai = 'ACTIVE' AND DaXoa = 0;

CREATE NONCLUSTERED INDEX IX_SanPham_ACTIVE_ChuoiTimKiem
    ON SanPham (ChuoiTimKiem)
    WHERE TrangThai = 'ACTIVE' AND DaXoa = 0;

CREATE NONCLUSTERED INDEX IX_SanPham_CuaHang_TrangThai
    ON SanPham (MaCuaHang, TrangThai) INCLUDE (DaXoa, NgayCapNhat);

CREATE NONCLUSTERED INDEX IX_SanPham_TinhTrang ON SanPham (MaTinhTrang);

CREATE NONCLUSTERED INDEX IX_GioHang_NguoiDung ON GioHang (MaNguoiDung) INCLUDE (MaSanPham, DaChon);
CREATE NONCLUSTERED INDEX IX_GioHang_SanPham   ON GioHang (MaSanPham);

CREATE NONCLUSTERED INDEX IX_DonHang_NguoiMua
    ON DonHang (MaNguoiDung, TrangThai, NgayTao DESC);

CREATE NONCLUSTERED INDEX IX_DonHang_CuaHang
    ON DonHang (MaCuaHang, TrangThai, NgayTao DESC);

CREATE NONCLUSTERED INDEX IX_DonHang_PENDING_QuaHan
    ON DonHang (NgayTao) WHERE TrangThai = 'PENDING';

CREATE NONCLUSTERED INDEX IX_DonHang_DELIVERED_QuaHan
    ON DonHang (ThoiGianDaGiao) WHERE TrangThai = 'DELIVERED';

CREATE NONCLUSTERED INDEX IX_ChiTietDonHang_SanPham ON ChiTietDonHang (MaSanPham);

CREATE NONCLUSTERED INDEX IX_LichSuDon_DonHang     ON LichSuTrangThaiDonHang (MaDonHang, ThoiGian);
CREATE NONCLUSTERED INDEX IX_LichSuSanPham_SanPham ON LichSuTrangThaiSanPham (MaSanPham, ThoiGian);

CREATE NONCLUSTERED INDEX IX_LuotKiemDuyet_HangCho
    ON LuotKiemDuyet (ThoiGianGui) WHERE QuyetDinh IS NULL;

CREATE NONCLUSTERED INDEX IX_LuotKiemDuyet_SanPham
    ON LuotKiemDuyet (MaSanPham, ThoiGianGui DESC);

CREATE NONCLUSTERED INDEX IX_PhanTichAI_HangDoi
    ON PhanTichAI (NgayTao) WHERE TrangThai = 'QUEUED';

CREATE NONCLUSTERED INDEX IX_NguoiDung_VaiTro_TrangThai ON NguoiDung (VaiTro, TrangThai);
CREATE NONCLUSTERED INDEX IX_CuaHang_TrangThai          ON CuaHang (TrangThai);
CREATE NONCLUSTERED INDEX IX_NhatKy_NguoiDung_ThoiGian  ON NhatKyHeThong (MaNguoiDung, ThoiGian DESC);
CREATE NONCLUSTERED INDEX IX_NhatKy_DoiTuong            ON NhatKyHeThong (LoaiDoiTuong, DoiTuongId);
GO

PRINT N'Da tao xong index.';
SET NOEXEC OFF;
GO