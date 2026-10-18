SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ARITHABORT ON;
GO

USE SecondHandMarketplace;
GO

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_CuaHang_NguoiDung')
BEGIN
    PRINT N'Rang buoc da ton tai, bo qua 03_constraints.sql.';
    SET NOEXEC ON;
END
GO

-- khóa ngoại (foreign key) để đảm bảo dữ liệu liên quan tồn tại, tránh lỗi khi xóa

ALTER TABLE CuaHang ADD
    CONSTRAINT FK_CuaHang_NguoiDung FOREIGN KEY (MaNguoiDung) REFERENCES NguoiDung (MaNguoiDung);

ALTER TABLE SanPham ADD
    CONSTRAINT FK_SanPham_CuaHang   FOREIGN KEY (MaCuaHang)   REFERENCES CuaHang (MaCuaHang),
    CONSTRAINT FK_SanPham_DanhMuc   FOREIGN KEY (MaDanhMuc)   REFERENCES DanhMuc (MaDanhMuc),
    CONSTRAINT FK_SanPham_TinhTrang FOREIGN KEY (MaTinhTrang) REFERENCES TinhTrangSanPham (MaTinhTrang);

ALTER TABLE AnhSanPham ADD
    CONSTRAINT FK_AnhSanPham_SanPham FOREIGN KEY (MaSanPham) REFERENCES SanPham (MaSanPham);

ALTER TABLE GioHang ADD
    CONSTRAINT FK_GioHang_NguoiDung FOREIGN KEY (MaNguoiDung) REFERENCES NguoiDung (MaNguoiDung),
    CONSTRAINT FK_GioHang_SanPham   FOREIGN KEY (MaSanPham)   REFERENCES SanPham (MaSanPham);

ALTER TABLE DonHang ADD
    CONSTRAINT FK_DonHang_NguoiMua FOREIGN KEY (MaNguoiDung) REFERENCES NguoiDung (MaNguoiDung),
    CONSTRAINT FK_DonHang_CuaHang  FOREIGN KEY (MaCuaHang)   REFERENCES CuaHang (MaCuaHang);

ALTER TABLE ChiTietDonHang ADD
    CONSTRAINT FK_ChiTietDonHang_DonHang  FOREIGN KEY (MaDonHang) REFERENCES DonHang (MaDonHang),
    CONSTRAINT FK_ChiTietDonHang_SanPham  FOREIGN KEY (MaSanPham) REFERENCES SanPham (MaSanPham);

ALTER TABLE LichSuTrangThaiDonHang ADD
    CONSTRAINT FK_LichSuDon_DonHang     FOREIGN KEY (MaDonHang)   REFERENCES DonHang (MaDonHang),
    CONSTRAINT FK_LichSuDon_NguoiDung   FOREIGN KEY (NguoiThucHienId) REFERENCES NguoiDung (MaNguoiDung);

ALTER TABLE LichSuTrangThaiSanPham ADD
    CONSTRAINT FK_LichSuSanPham_SanPham   FOREIGN KEY (MaSanPham)       REFERENCES SanPham (MaSanPham),
    CONSTRAINT FK_LichSuSanPham_NguoiDung FOREIGN KEY (NguoiThucHienId) REFERENCES NguoiDung (MaNguoiDung);

ALTER TABLE LuotKiemDuyet ADD
    CONSTRAINT FK_LuotKiemDuyet_SanPham FOREIGN KEY (MaSanPham)     REFERENCES SanPham (MaSanPham),
    CONSTRAINT FK_LuotKiemDuyet_QuanTri FOREIGN KEY (MaQuanTriVien) REFERENCES NguoiDung (MaNguoiDung);

ALTER TABLE PhanTichAI ADD
    CONSTRAINT FK_PhanTichAI_LuotKiemDuyet FOREIGN KEY (MaLuotKiemDuyet) REFERENCES LuotKiemDuyet (MaLuotKiemDuyet);

ALTER TABLE NhatKyHeThong ADD
    CONSTRAINT FK_NhatKy_NguoiDung FOREIGN KEY (MaNguoiDung) REFERENCES NguoiDung (MaNguoiDung);
GO

-- Unique  
ALTER TABLE NguoiDung        ADD CONSTRAINT UQ_NguoiDung_Email           UNIQUE (Email);
ALTER TABLE CuaHang          ADD CONSTRAINT UQ_CuaHang_MaNguoiDung       UNIQUE (MaNguoiDung);
ALTER TABLE DanhMuc          ADD CONSTRAINT UQ_DanhMuc_Ten               UNIQUE (Ten);
ALTER TABLE TinhTrangSanPham ADD CONSTRAINT UQ_TinhTrangSanPham_Ma       UNIQUE (Ma);
ALTER TABLE TinhTrangSanPham ADD CONSTRAINT UQ_TinhTrangSanPham_Ten      UNIQUE (Ten);
ALTER TABLE AnhSanPham       ADD CONSTRAINT UQ_AnhSanPham_SanPham_ThuTu  UNIQUE (MaSanPham, ThuTu);
ALTER TABLE GioHang          ADD CONSTRAINT UQ_GioHang_NguoiDung_SanPham UNIQUE (MaNguoiDung, MaSanPham);
ALTER TABLE DonHang          ADD CONSTRAINT UQ_DonHang_MaDon             UNIQUE (MaDon);
ALTER TABLE ChiTietDonHang   ADD CONSTRAINT UQ_ChiTietDonHang_Don_SanPham UNIQUE (MaDonHang, MaSanPham);
ALTER TABLE PhanTichAI       ADD CONSTRAINT UQ_PhanTichAI_LuotKiemDuyet  UNIQUE (MaLuotKiemDuyet);
ALTER TABLE TuKhoaCam        ADD CONSTRAINT UQ_TuKhoaCam_CumTuChuanHoa   UNIQUE (CumTuChuanHoa);
GO

-- Unique có điều kiện (filtered)
CREATE UNIQUE NONCLUSTERED INDEX UX_NguoiDung_SoDienThoai
    ON NguoiDung (SoDienThoai) WHERE SoDienThoai IS NOT NULL;

CREATE UNIQUE NONCLUSTERED INDEX UX_AnhSanPham_AnhChinh
    ON AnhSanPham (MaSanPham) WHERE LaAnhChinh = 1;

CREATE UNIQUE NONCLUSTERED INDEX UX_ChiTietDonHang_DangGiuCho
    ON ChiTietDonHang (MaSanPham) WHERE DangGiuCho = 1;

CREATE UNIQUE NONCLUSTERED INDEX UX_LuotKiemDuyet_DangCho
    ON LuotKiemDuyet (MaSanPham) WHERE QuyetDinh IS NULL;
GO

-- Check constraints
ALTER TABLE NguoiDung ADD
    CONSTRAINT CK_NguoiDung_VaiTro       CHECK (VaiTro IN ('USER','ADMIN')),
    CONSTRAINT CK_NguoiDung_TrangThai    CHECK (TrangThai IN ('ACTIVE','LOCKED')),
    CONSTRAINT CK_NguoiDung_HoTen        CHECK (LEN(LTRIM(RTRIM(HoTen))) > 0),
    CONSTRAINT CK_NguoiDung_Email        CHECK (Email LIKE '%_@_%._%' AND Email NOT LIKE '% %'),
    CONSTRAINT CK_NguoiDung_SoDienThoai  CHECK (SoDienThoai IS NULL OR (SoDienThoai NOT LIKE '%[^0-9+]%' AND LEN(SoDienThoai) BETWEEN 9 AND 15)),
    CONSTRAINT CK_NguoiDung_MatKhau      CHECK (LEN(MatKhauMaHoa) >= 20),
    CONSTRAINT CK_NguoiDung_Khoa         CHECK ((TrangThai = 'ACTIVE' AND LyDoKhoa IS NULL AND ThoiGianKhoa IS NULL) OR (TrangThai = 'LOCKED' AND LyDoKhoa IS NOT NULL AND LEN(LTRIM(RTRIM(LyDoKhoa))) > 0 AND ThoiGianKhoa IS NOT NULL)),
    CONSTRAINT CK_NguoiDung_AdminKhongBiKhoa CHECK (VaiTro = 'USER' OR TrangThai = 'ACTIVE');

ALTER TABLE CuaHang ADD
    CONSTRAINT CK_CuaHang_TrangThai      CHECK (TrangThai IN ('ACTIVE','LOCKED')),
    CONSTRAINT CK_CuaHang_TenShop        CHECK (LEN(LTRIM(RTRIM(TenShop))) > 0),
    CONSTRAINT CK_CuaHang_SoDienThoai    CHECK (SoDienThoaiLienHe NOT LIKE '%[^0-9+]%' AND LEN(SoDienThoaiLienHe) BETWEEN 9 AND 15),
    CONSTRAINT CK_CuaHang_Khoa           CHECK ((TrangThai = 'ACTIVE' AND LyDoKhoa IS NULL AND ThoiGianKhoa IS NULL) OR (TrangThai = 'LOCKED' AND LyDoKhoa IS NOT NULL AND LEN(LTRIM(RTRIM(LyDoKhoa))) > 0 AND ThoiGianKhoa IS NOT NULL));

ALTER TABLE DanhMuc ADD
    CONSTRAINT CK_DanhMuc_Ten            CHECK (LEN(LTRIM(RTRIM(Ten))) > 0);

ALTER TABLE TinhTrangSanPham ADD
    CONSTRAINT CK_TinhTrang_Ma           CHECK (LEN(LTRIM(RTRIM(Ma))) > 0),
    CONSTRAINT CK_TinhTrang_Ten          CHECK (LEN(LTRIM(RTRIM(Ten))) > 0);

ALTER TABLE SanPham ADD
    CONSTRAINT CK_SanPham_TrangThai      CHECK (TrangThai IN ('DRAFT','PENDING_REVIEW','REJECTED','ACTIVE','HIDDEN','RESERVED','SOLD','REMOVED')),
    CONSTRAINT CK_SanPham_Ten            CHECK (LEN(LTRIM(RTRIM(Ten))) > 0),
    CONSTRAINT CK_SanPham_Gia            CHECK (Gia IS NULL OR (Gia > 0 AND Gia <= 1000000000)),
    CONSTRAINT CK_SanPham_DayDu          CHECK (TrangThai = 'DRAFT' OR (MaDanhMuc IS NOT NULL AND MaTinhTrang IS NOT NULL AND Gia IS NOT NULL)),
    CONSTRAINT CK_SanPham_LyDoGo         CHECK (TrangThai <> 'REMOVED' OR (LyDoGo IS NOT NULL AND LEN(LTRIM(RTRIM(LyDoGo))) > 0)),
    CONSTRAINT CK_SanPham_XoaMem         CHECK ((DaXoa = 0 AND ThoiGianXoa IS NULL) OR (DaXoa = 1 AND ThoiGianXoa IS NOT NULL));

ALTER TABLE AnhSanPham ADD
    CONSTRAINT CK_AnhSanPham_DuongDan    CHECK (LEN(LTRIM(RTRIM(DuongDan))) > 0),
    CONSTRAINT CK_AnhSanPham_ThuTu       CHECK (ThuTu >= 1),
    CONSTRAINT CK_AnhSanPham_DungLuong   CHECK (DungLuong BETWEEN 1 AND 5242880);

ALTER TABLE DonHang ADD
    CONSTRAINT CK_DonHang_MaDon          CHECK (LEN(LTRIM(RTRIM(MaDon))) > 0),
    CONSTRAINT CK_DonHang_TrangThai      CHECK (TrangThai IN ('PENDING','CONFIRMED','SHIPPING','DELIVERED','COMPLETED','CANCELLED')),
    CONSTRAINT CK_DonHang_PhuongThuc     CHECK (PhuongThucThanhToan IN ('COD','BANK_TRANSFER')),
    CONSTRAINT CK_DonHang_ThanhToan      CHECK (TrangThaiThanhToan IN ('UNPAID','PAID')),
    CONSTRAINT CK_DonHang_TongTien       CHECK (TongTien > 0),
    CONSTRAINT CK_DonHang_NguoiNhan      CHECK (LEN(LTRIM(RTRIM(TenNguoiNhan))) > 0 AND LEN(LTRIM(RTRIM(DiaChiNhan))) > 0),
    CONSTRAINT CK_DonHang_SoDienThoaiNhan CHECK (SoDienThoaiNhan NOT LIKE '%[^0-9+]%' AND LEN(SoDienThoaiNhan) BETWEEN 9 AND 15),
    CONSTRAINT CK_DonHang_NguoiHuy       CHECK (NguoiHuy IS NULL OR NguoiHuy IN ('BUYER','SELLER','SYSTEM')),
    CONSTRAINT CK_DonHang_Huy            CHECK ((TrangThai = 'CANCELLED' AND NguoiHuy IS NOT NULL AND ThoiGianHuy IS NOT NULL) OR (TrangThai <> 'CANCELLED' AND NguoiHuy IS NULL AND LyDoHuy IS NULL AND ThoiGianHuy IS NULL)),
    CONSTRAINT CK_DonHang_LyDoHuyCuaNguoiBan CHECK (NguoiHuy IS NULL OR NguoiHuy <> 'SELLER' OR LEN(LTRIM(RTRIM(ISNULL(LyDoHuy, N'')))) > 0),
    CONSTRAINT CK_DonHang_MocThoiGian    CHECK ((TrangThai NOT IN ('CONFIRMED','SHIPPING','DELIVERED','COMPLETED') OR ThoiGianXacNhan IS NOT NULL) AND (TrangThai NOT IN ('SHIPPING','DELIVERED','COMPLETED') OR ThoiGianBatDauGiao IS NOT NULL) AND (TrangThai NOT IN ('DELIVERED','COMPLETED') OR ThoiGianDaGiao IS NOT NULL) AND (TrangThai <> 'COMPLETED' OR ThoiGianHoanTat IS NOT NULL));

ALTER TABLE ChiTietDonHang ADD
    CONSTRAINT CK_ChiTietDonHang_Gia     CHECK (Gia > 0),
    CONSTRAINT CK_ChiTietDonHang_Ten     CHECK (LEN(LTRIM(RTRIM(TenSanPham))) > 0);

ALTER TABLE LichSuTrangThaiDonHang ADD
    CONSTRAINT CK_LichSuDon_TrangThaiCu  CHECK (TrangThaiCu IS NULL OR TrangThaiCu IN ('PENDING','CONFIRMED','SHIPPING','DELIVERED','COMPLETED','CANCELLED')),
    CONSTRAINT CK_LichSuDon_TrangThaiMoi CHECK (TrangThaiMoi IN ('PENDING','CONFIRMED','SHIPPING','DELIVERED','COMPLETED','CANCELLED')),
    CONSTRAINT CK_LichSuDon_Loai         CHECK (LoaiNguoiThucHien IN ('BUYER','SELLER','ADMIN','SYSTEM')),
    CONSTRAINT CK_LichSuDon_NguoiThucHien CHECK ((LoaiNguoiThucHien = 'SYSTEM' AND NguoiThucHienId IS NULL) OR (LoaiNguoiThucHien <> 'SYSTEM' AND NguoiThucHienId IS NOT NULL));

ALTER TABLE LichSuTrangThaiSanPham ADD
    CONSTRAINT CK_LichSuSanPham_TrangThaiCu CHECK (TrangThaiCu IS NULL OR TrangThaiCu IN ('DRAFT','PENDING_REVIEW','REJECTED','ACTIVE','HIDDEN','RESERVED','SOLD','REMOVED')),
    CONSTRAINT CK_LichSuSanPham_TrangThaiMoi CHECK (TrangThaiMoi IN ('DRAFT','PENDING_REVIEW','REJECTED','ACTIVE','HIDDEN','RESERVED','SOLD','REMOVED')),
    CONSTRAINT CK_LichSuSanPham_Loai     CHECK (LoaiNguoiThucHien IN ('BUYER','SELLER','ADMIN','SYSTEM')),
    CONSTRAINT CK_LichSuSanPham_NguoiThucHien CHECK ((LoaiNguoiThucHien = 'SYSTEM' AND NguoiThucHienId IS NULL) OR (LoaiNguoiThucHien <> 'SYSTEM' AND NguoiThucHienId IS NOT NULL));

ALTER TABLE LuotKiemDuyet ADD
    CONSTRAINT CK_LuotKiemDuyet_QuyetDinh CHECK (QuyetDinh IS NULL OR QuyetDinh IN ('APPROVED','REJECTED')),
    CONSTRAINT CK_LuotKiemDuyet_KetQuaRule CHECK (KetQuaRule IS NULL OR ISJSON(KetQuaRule) = 1),
    CONSTRAINT CK_LuotKiemDuyet_DaQuyetDinh CHECK ((QuyetDinh IS NULL AND MaQuanTriVien IS NULL AND ThoiGianQuyetDinh IS NULL AND LyDo IS NULL) OR (QuyetDinh IS NOT NULL AND MaQuanTriVien IS NOT NULL AND ThoiGianQuyetDinh IS NOT NULL)),
    CONSTRAINT CK_LuotKiemDuyet_LyDoTuChoi CHECK (QuyetDinh IS NULL OR QuyetDinh <> 'REJECTED' OR LEN(LTRIM(RTRIM(ISNULL(LyDo, N'')))) >= 10);

ALTER TABLE PhanTichAI ADD
    CONSTRAINT CK_PhanTichAI_TrangThai CHECK (TrangThai IN ('QUEUED','PROCESSING','DONE','FAILED')),
    CONSTRAINT CK_PhanTichAI_MucRuiRo  CHECK (MucRuiRo IS NULL OR MucRuiRo IN ('LOW','MEDIUM','HIGH')),
    CONSTRAINT CK_PhanTichAI_DeXuat    CHECK (DeXuat IS NULL OR DeXuat IN ('APPROVE','REJECT','NEEDS_REVIEW')),
    CONSTRAINT CK_PhanTichAI_DauHieu   CHECK (DauHieu IS NULL OR ISJSON(DauHieu) = 1),
    CONSTRAINT CK_PhanTichAI_SoLanThu  CHECK (SoLanThu >= 0),
    CONSTRAINT CK_PhanTichAI_ThoiGianXuLy CHECK (ThoiGianXuLyMs IS NULL OR ThoiGianXuLyMs >= 0),
    CONSTRAINT CK_PhanTichAI_DaXong    CHECK (TrangThai <> 'DONE' OR (MucRuiRo IS NOT NULL AND DeXuat IS NOT NULL AND ThoiGianHoanThanh IS NOT NULL)),
    CONSTRAINT CK_PhanTichAI_Loi       CHECK (TrangThai <> 'FAILED' OR LoiXuLy IS NOT NULL);

ALTER TABLE TuKhoaCam ADD
    CONSTRAINT CK_TuKhoaCam_CumTu      CHECK (LEN(LTRIM(RTRIM(CumTu))) > 0 AND LEN(LTRIM(RTRIM(CumTuChuanHoa))) > 0);

ALTER TABLE NhatKyHeThong ADD
    CONSTRAINT CK_NhatKy_KetQua        CHECK (KetQua IN ('SUCCESS','FAILED','DENIED'));
GO

PRINT N'Da ap dung xong rang buoc.';
SET NOEXEC OFF;
GO 