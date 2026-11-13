SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ARITHABORT ON;
GO

USE SecondHandMarketplace;
GO


-- Khóa chính cho toàn bộ bảng (tạo trước các khóa ngoại).
ALTER TABLE dbo.NguoiDung ADD CONSTRAINT PK_NguoiDung PRIMARY KEY CLUSTERED (MaNguoiDung);
GO
ALTER TABLE dbo.CuaHang ADD CONSTRAINT PK_CuaHang PRIMARY KEY CLUSTERED (MaCuaHang);
GO
ALTER TABLE dbo.DanhMuc ADD CONSTRAINT PK_DanhMuc PRIMARY KEY CLUSTERED (MaDanhMuc);
GO
ALTER TABLE dbo.TinhTrangSanPham ADD CONSTRAINT PK_TinhTrangSanPham PRIMARY KEY CLUSTERED (MaTinhTrang);
GO
ALTER TABLE dbo.SanPham ADD CONSTRAINT PK_SanPham PRIMARY KEY CLUSTERED (MaSanPham);
GO
ALTER TABLE dbo.AnhSanPham ADD CONSTRAINT PK_AnhSanPham PRIMARY KEY CLUSTERED (MaAnhSP);
GO
ALTER TABLE dbo.GioHang ADD CONSTRAINT PK_GioHang PRIMARY KEY CLUSTERED (MaGioHang);
GO
ALTER TABLE dbo.DonHang ADD CONSTRAINT PK_DonHang PRIMARY KEY CLUSTERED (MaDonHang);
GO
ALTER TABLE dbo.ChiTietDonHang ADD CONSTRAINT PK_ChiTietDonHang PRIMARY KEY CLUSTERED (MaChiTietDonHang);
GO
ALTER TABLE dbo.LichSuTrangThaiDonHang ADD CONSTRAINT PK_LichSuTrangThaiDonHang PRIMARY KEY CLUSTERED (MaLichSuTrangThaiDonHang);
GO
ALTER TABLE dbo.ThanhToan ADD CONSTRAINT PK_ThanhToan PRIMARY KEY CLUSTERED (MaThanhToan);
GO
ALTER TABLE dbo.LichSuTrangThaiSanPham ADD CONSTRAINT PK_LichSuTrangThaiSanPham PRIMARY KEY CLUSTERED (MaLichSuTrangThaiSanPham);
GO
ALTER TABLE dbo.LuotKiemDuyet ADD CONSTRAINT PK_LuotKiemDuyet PRIMARY KEY CLUSTERED (MaLuotKiemDuyet);
GO
ALTER TABLE dbo.PhanTichAI ADD CONSTRAINT PK_PhanTichAI PRIMARY KEY CLUSTERED (MaPhanTichAI);
GO
ALTER TABLE dbo.TuKhoaCam ADD CONSTRAINT PK_TuKhoaCam PRIMARY KEY CLUSTERED (MaTuKhoaCam);
GO
ALTER TABLE dbo.ThongBao ADD CONSTRAINT PK_ThongBao PRIMARY KEY CLUSTERED (MaThongBao);
GO
ALTER TABLE dbo.NhatKyHeThong ADD CONSTRAINT PK_NhatKyHeThong PRIMARY KEY CLUSTERED (MaNhatKyHeThong);
GO
ALTER TABLE dbo.YeuThich ADD CONSTRAINT PK_YeuThich PRIMARY KEY CLUSTERED (MaYeuThich);
GO
ALTER TABLE dbo.Banner ADD CONSTRAINT PK_Banner PRIMARY KEY CLUSTERED (MaBanner);
GO
ALTER TABLE dbo.DanhGia ADD CONSTRAINT PK_DanhGia           PRIMARY KEY CLUSTERED (MaDanhGia);
GO
ALTER TABLE dbo.TranhChap ADD CONSTRAINT PK_TranhChap PRIMARY KEY CLUSTERED (MaTranhChap);
GO
ALTER TABLE dbo.BaoCao ADD CONSTRAINT PK_BaoCao PRIMARY KEY CLUSTERED (MaBaoCao);
GO
ALTER TABLE dbo.PhienDauGia ADD CONSTRAINT PK_PhienDauGia PRIMARY KEY CLUSTERED (MaPhienDauGia);
GO
ALTER TABLE dbo.LuotDatGia ADD CONSTRAINT PK_LuotDatGia PRIMARY KEY CLUSTERED (MaLuotDatGia);
GO
ALTER TABLE dbo.LichSuViPham ADD CONSTRAINT PK_LichSuViPham PRIMARY KEY CLUSTERED (MaViPham);
GO
ALTER TABLE dbo.HoiThoai ADD CONSTRAINT PK_HoiThoai PRIMARY KEY CLUSTERED (MaHoiThoai);
GO
ALTER TABLE dbo.TinNhan ADD CONSTRAINT PK_TinNhan PRIMARY KEY CLUSTERED (MaTinNhan);
GO
ALTER TABLE dbo.CauHinhAI ADD CONSTRAINT PK_CauHinhAI PRIMARY KEY CLUSTERED (MaCauHinhAI);
GO
ALTER TABLE dbo.TinhThanh ADD CONSTRAINT PK_TinhThanh PRIMARY KEY CLUSTERED (MaTinhThanh);
GO
ALTER TABLE dbo.PhuongXa ADD CONSTRAINT PK_PhuongXa PRIMARY KEY CLUSTERED (MaPhuongXa);
GO
ALTER TABLE dbo.PasswordResetOtp ADD CONSTRAINT PK_PasswordResetOtp PRIMARY KEY CLUSTERED (Id);
GO
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
    CONSTRAINT FK_DonHang_NguoiDung FOREIGN KEY (MaNguoiDung) REFERENCES NguoiDung (MaNguoiDung),
    CONSTRAINT FK_DonHang_CuaHang   FOREIGN KEY (MaCuaHang)   REFERENCES CuaHang (MaCuaHang);

ALTER TABLE ChiTietDonHang ADD
    CONSTRAINT FK_ChiTietDonHang_DonHang FOREIGN KEY (MaDonHang) REFERENCES DonHang (MaDonHang),
    CONSTRAINT FK_ChiTietDonHang_SanPham FOREIGN KEY (MaSanPham) REFERENCES SanPham (MaSanPham);

ALTER TABLE LichSuTrangThaiDonHang ADD
    CONSTRAINT FK_LichSuDon_DonHang   FOREIGN KEY (MaDonHang)       REFERENCES DonHang (MaDonHang),
    CONSTRAINT FK_LichSuDon_NguoiDung FOREIGN KEY (NguoiThucHienId) REFERENCES NguoiDung (MaNguoiDung);

ALTER TABLE ThanhToan ADD
    CONSTRAINT FK_ThanhToan_DonHang FOREIGN KEY (MaDonHang) REFERENCES DonHang (MaDonHang);

ALTER TABLE LichSuTrangThaiSanPham ADD
    CONSTRAINT FK_LichSuSanPham_SanPham   FOREIGN KEY (MaSanPham)       REFERENCES SanPham (MaSanPham),
    CONSTRAINT FK_LichSuSanPham_NguoiDung FOREIGN KEY (NguoiThucHienId) REFERENCES NguoiDung (MaNguoiDung);

ALTER TABLE LuotKiemDuyet ADD
    CONSTRAINT FK_LuotKiemDuyet_SanPham FOREIGN KEY (MaSanPham)     REFERENCES SanPham (MaSanPham),
    CONSTRAINT FK_LuotKiemDuyet_QuanTri FOREIGN KEY (MaQuanTriVien) REFERENCES NguoiDung (MaNguoiDung);

ALTER TABLE PhanTichAI ADD
    CONSTRAINT FK_PhanTichAI_LuotKiemDuyet FOREIGN KEY (MaLuotKiemDuyet) REFERENCES LuotKiemDuyet (MaLuotKiemDuyet);

ALTER TABLE ThongBao ADD
    CONSTRAINT FK_ThongBao_NguoiDung FOREIGN KEY (MaNguoiDung) REFERENCES NguoiDung (MaNguoiDung);

ALTER TABLE NhatKyHeThong ADD
    CONSTRAINT FK_NhatKy_NguoiDung FOREIGN KEY (MaNguoiDung) REFERENCES NguoiDung (MaNguoiDung);
GO
 
ALTER TABLE NguoiDung        ADD CONSTRAINT UQ_NguoiDung_Email            UNIQUE (Email);
ALTER TABLE CuaHang          ADD CONSTRAINT UQ_CuaHang_MaNguoiDung        UNIQUE (MaNguoiDung);
ALTER TABLE DanhMuc          ADD CONSTRAINT UQ_DanhMuc_Ten                UNIQUE (Ten);
ALTER TABLE TinhTrangSanPham ADD CONSTRAINT UQ_TinhTrangSanPham_Ma        UNIQUE (Ma);
ALTER TABLE TinhTrangSanPham ADD CONSTRAINT UQ_TinhTrangSanPham_Ten       UNIQUE (Ten);
ALTER TABLE AnhSanPham       ADD CONSTRAINT UQ_AnhSanPham_SanPham_ThuTu   UNIQUE (MaSanPham, ThuTu);
ALTER TABLE GioHang          ADD CONSTRAINT UQ_GioHang_NguoiDung_SanPham  UNIQUE (MaNguoiDung, MaSanPham);
ALTER TABLE DonHang          ADD CONSTRAINT UQ_DonHang_MaDon              UNIQUE (MaDon);
ALTER TABLE ChiTietDonHang   ADD CONSTRAINT UQ_ChiTietDonHang_Don_SanPham UNIQUE (MaDonHang, MaSanPham);
ALTER TABLE PhanTichAI       ADD CONSTRAINT UQ_PhanTichAI_LuotKiemDuyet   UNIQUE (MaLuotKiemDuyet);
ALTER TABLE TuKhoaCam        ADD CONSTRAINT UQ_TuKhoaCam_CumTuChuanHoa    UNIQUE (CumTuChuanHoa);
GO

CREATE UNIQUE NONCLUSTERED INDEX UX_NguoiDung_SoDienThoai
    ON NguoiDung (SoDienThoai) WHERE SoDienThoai IS NOT NULL;

CREATE UNIQUE NONCLUSTERED INDEX UX_AnhSanPham_AnhChinh
    ON AnhSanPham (MaSanPham) WHERE LaAnhChinh = 1;

CREATE UNIQUE NONCLUSTERED INDEX UX_ChiTietDonHang_DangGiuCho
    ON ChiTietDonHang (MaSanPham) WHERE DangGiuCho = 1;

CREATE UNIQUE NONCLUSTERED INDEX UX_LuotKiemDuyet_DangCho
    ON LuotKiemDuyet (MaSanPham) WHERE QuyetDinh IS NULL;
GO
 
ALTER TABLE NguoiDung ADD
    CONSTRAINT CK_NguoiDung_VaiTro    CHECK (VaiTro IN ('USER','ADMIN')),
    CONSTRAINT CK_NguoiDung_TrangThai CHECK (TrangThai IN ('ACTIVE','LOCKED')),
    CONSTRAINT CK_NguoiDung_HoTen     CHECK (LEN(LTRIM(RTRIM(HoTen))) > 0),
    CONSTRAINT CK_NguoiDung_Email     CHECK (Email LIKE '%_@_%._%' AND Email NOT LIKE '% %'
                                             AND Email COLLATE Latin1_General_BIN = LOWER(Email) COLLATE Latin1_General_BIN),
    CONSTRAINT CK_NguoiDung_SoDienThoai CHECK (SoDienThoai IS NULL OR (SoDienThoai NOT LIKE '%[^0-9]%' AND LEN(SoDienThoai) BETWEEN 9 AND 15)),
    CONSTRAINT CK_NguoiDung_SDTBatBuoc  CHECK (VaiTro = 'ADMIN' OR SoDienThoai IS NOT NULL),
    CONSTRAINT CK_NguoiDung_MatKhau   CHECK (LEN(MatKhauBam) >= 20),
    CONSTRAINT CK_NguoiDung_Khoa      CHECK ((TrangThai = 'ACTIVE' AND LyDoKhoa IS NULL AND ThoiGianKhoa IS NULL)
                                          OR (TrangThai = 'LOCKED' AND LyDoKhoa IS NOT NULL AND LEN(LTRIM(RTRIM(LyDoKhoa))) > 0 AND ThoiGianKhoa IS NOT NULL));

ALTER TABLE CuaHang ADD
    CONSTRAINT CK_CuaHang_TrangThai   CHECK (TrangThai IN ('ACTIVE','LOCKED')),
    CONSTRAINT CK_CuaHang_TenShop     CHECK (LEN(LTRIM(RTRIM(TenShop))) > 0),
    CONSTRAINT CK_CuaHang_SoDienThoai CHECK (SoDienThoaiLienHe NOT LIKE '%[^0-9]%' AND LEN(SoDienThoaiLienHe) BETWEEN 9 AND 15),
    CONSTRAINT CK_CuaHang_Khoa        CHECK ((TrangThai = 'ACTIVE' AND LyDoKhoa IS NULL AND ThoiGianKhoa IS NULL)
                                          OR (TrangThai = 'LOCKED' AND LyDoKhoa IS NOT NULL AND LEN(LTRIM(RTRIM(LyDoKhoa))) > 0 AND ThoiGianKhoa IS NOT NULL));

ALTER TABLE DanhMuc ADD
    CONSTRAINT CK_DanhMuc_Ten CHECK (LEN(LTRIM(RTRIM(Ten))) > 0);

ALTER TABLE TinhTrangSanPham ADD
    CONSTRAINT CK_TinhTrang_Ma  CHECK (LEN(LTRIM(RTRIM(Ma))) > 0),
    CONSTRAINT CK_TinhTrang_Ten CHECK (LEN(LTRIM(RTRIM(Ten))) > 0);

ALTER TABLE SanPham ADD
    CONSTRAINT CK_SanPham_TrangThai CHECK (TrangThai IN ('DRAFT','PENDING_REVIEW','REJECTED','ACTIVE','HIDDEN','RESERVED','SOLD','REMOVED')),
    CONSTRAINT CK_SanPham_Ten       CHECK (LEN(LTRIM(RTRIM(Ten))) > 0),
    CONSTRAINT CK_SanPham_Gia       CHECK (Gia IS NULL OR (Gia > 0 AND Gia <= 1000000000)),
    CONSTRAINT CK_SanPham_DayDu     CHECK (TrangThai = 'DRAFT' OR (MaDanhMuc IS NOT NULL AND MaTinhTrang IS NOT NULL AND Gia IS NOT NULL)),
    CONSTRAINT CK_SanPham_LyDoGo    CHECK (TrangThai <> 'REMOVED' OR (LyDoGo IS NOT NULL AND LEN(LTRIM(RTRIM(LyDoGo))) > 0)),
    CONSTRAINT CK_SanPham_XoaMem    CHECK ((DaXoa = 0 AND ThoiGianXoa IS NULL) OR (DaXoa = 1 AND ThoiGianXoa IS NOT NULL));

ALTER TABLE AnhSanPham ADD
    CONSTRAINT CK_AnhSanPham_DuongDan  CHECK (LEN(LTRIM(RTRIM(DuongDan))) > 0),
    CONSTRAINT CK_AnhSanPham_ThuTu     CHECK (ThuTu >= 1),
    CONSTRAINT CK_AnhSanPham_DungLuong CHECK (DungLuong BETWEEN 1 AND 5242880);

ALTER TABLE DonHang ADD
    CONSTRAINT CK_DonHang_SoTien        CHECK (TienHang >= 0 AND PhiVanChuyen >= 0 AND TongTien = TienHang + PhiVanChuyen),
    CONSTRAINT CK_DonHang_PhuongThucTT  CHECK (PhuongThucThanhToan IN ('COD','BANK_TRANSFER','ONLINE')),
    CONSTRAINT CK_DonHang_TrangThaiTT   CHECK (TrangThaiThanhToan IN ('UNPAID','PENDING','PAID','REFUNDED','FAILED')),
    CONSTRAINT CK_DonHang_TrangThai     CHECK (TrangThai IN ('PENDING','CONFIRMED','SHIPPING','DELIVERED','COMPLETED','CANCELLED')),
    CONSTRAINT CK_DonHang_SDT           CHECK (SoDienThoaiNhan NOT LIKE '%[^0-9]%' AND LEN(SoDienThoaiNhan) BETWEEN 9 AND 15),
    CONSTRAINT CK_DonHang_NguoiNhan     CHECK (LEN(LTRIM(RTRIM(TenNguoiNhan))) > 0),
    CONSTRAINT CK_DonHang_DiaChi        CHECK (LEN(LTRIM(RTRIM(DiaChiNhan))) > 0),
    CONSTRAINT CK_DonHang_NguoiHuy      CHECK (NguoiHuy IS NULL OR NguoiHuy IN ('BUYER','SELLER','ADMIN','SYSTEM')),
    CONSTRAINT CK_DonHang_Huy           CHECK ((TrangThai = 'CANCELLED' AND NguoiHuy IS NOT NULL AND LyDoHuy IS NOT NULL AND LEN(LTRIM(RTRIM(LyDoHuy))) > 0 AND ThoiGianHuy IS NOT NULL)
                                            OR (TrangThai <> 'CANCELLED' AND NguoiHuy IS NULL AND LyDoHuy IS NULL AND ThoiGianHuy IS NULL));

ALTER TABLE ChiTietDonHang ADD
    CONSTRAINT CK_ChiTietDonHang_Gia CHECK (Gia > 0);

ALTER TABLE LichSuTrangThaiDonHang ADD
    CONSTRAINT CK_LichSuDon_TrangThaiCu   CHECK (TrangThaiCu IS NULL OR TrangThaiCu IN ('PENDING','CONFIRMED','SHIPPING','DELIVERED','COMPLETED','CANCELLED')),
    CONSTRAINT CK_LichSuDon_TrangThaiMoi  CHECK (TrangThaiMoi IN ('PENDING','CONFIRMED','SHIPPING','DELIVERED','COMPLETED','CANCELLED')),
    CONSTRAINT CK_LichSuDon_Loai          CHECK (LoaiNguoiThucHien IN ('BUYER','SELLER','ADMIN','SYSTEM')),
    CONSTRAINT CK_LichSuDon_NguoiThucHien CHECK ((LoaiNguoiThucHien = 'SYSTEM' AND NguoiThucHienId IS NULL) OR (LoaiNguoiThucHien <> 'SYSTEM' AND NguoiThucHienId IS NOT NULL));

ALTER TABLE ThanhToan ADD
    CONSTRAINT CK_ThanhToan_PhuongThuc CHECK (PhuongThucThanhToan IN ('COD','BANK_TRANSFER','ONLINE')),
    CONSTRAINT CK_ThanhToan_TrangThai  CHECK (TrangThaiThanhToan IN ('PENDING','SUCCESS','FAILED','REFUNDED')),
    CONSTRAINT CK_ThanhToan_SoTien     CHECK (SoTien > 0);

ALTER TABLE LichSuTrangThaiSanPham ADD
    CONSTRAINT CK_LichSuSanPham_TrangThaiCu   CHECK (TrangThaiCu IS NULL OR TrangThaiCu IN ('DRAFT','PENDING_REVIEW','REJECTED','ACTIVE','HIDDEN','RESERVED','SOLD','REMOVED')),
    CONSTRAINT CK_LichSuSanPham_TrangThaiMoi  CHECK (TrangThaiMoi IN ('DRAFT','PENDING_REVIEW','REJECTED','ACTIVE','HIDDEN','RESERVED','SOLD','REMOVED')),
    CONSTRAINT CK_LichSuSanPham_Loai          CHECK (LoaiNguoiThucHien IN ('BUYER','SELLER','ADMIN','SYSTEM')),
    CONSTRAINT CK_LichSuSanPham_NguoiThucHien CHECK ((LoaiNguoiThucHien = 'SYSTEM' AND NguoiThucHienId IS NULL) OR (LoaiNguoiThucHien <> 'SYSTEM' AND NguoiThucHienId IS NOT NULL));

ALTER TABLE LuotKiemDuyet ADD
    CONSTRAINT CK_LuotKiemDuyet_QuyetDinh   CHECK (QuyetDinh IS NULL OR QuyetDinh IN ('APPROVED','REJECTED')),
    CONSTRAINT CK_LuotKiemDuyet_KetQuaRule  CHECK (KetQuaRule IS NULL OR ISJSON(KetQuaRule) = 1),
    CONSTRAINT CK_LuotKiemDuyet_DaQuyetDinh_C2 CHECK (
        (QuyetDinh IS NULL AND MaQuanTriVien IS NULL AND ThoiGianQuyetDinh IS NULL AND LyDo IS NULL AND QuyetDinhTuDong = 0)
     OR (QuyetDinh IS NOT NULL AND MaQuanTriVien IS NOT NULL AND ThoiGianQuyetDinh IS NOT NULL AND QuyetDinhTuDong = 0)
     OR (QuyetDinh = 'REJECTED' AND MaQuanTriVien IS NULL AND ThoiGianQuyetDinh IS NOT NULL AND QuyetDinhTuDong = 1)),
    CONSTRAINT CK_LuotKiemDuyet_LyDoTuChoi  CHECK (QuyetDinh IS NULL OR QuyetDinh <> 'REJECTED' OR LEN(LTRIM(RTRIM(ISNULL(LyDo, N'')))) >= 10);

ALTER TABLE PhanTichAI ADD
    CONSTRAINT CK_PhanTichAI_TrangThai    CHECK (TrangThai IN ('QUEUED','PROCESSING','DONE','FAILED')),
    CONSTRAINT CK_PhanTichAI_MucRuiRo     CHECK (MucRuiRo IS NULL OR MucRuiRo IN ('LOW','MEDIUM','HIGH')),
    CONSTRAINT CK_PhanTichAI_DeXuat       CHECK (DeXuat IS NULL OR DeXuat IN ('APPROVE','REJECT','NEEDS_REVIEW')),
    CONSTRAINT CK_PhanTichAI_DauHieu      CHECK (DauHieu IS NULL OR ISJSON(DauHieu) = 1),
    CONSTRAINT CK_PhanTichAI_SoLanThu     CHECK (SoLanThu >= 0),
    CONSTRAINT CK_PhanTichAI_ThoiGianXuLy CHECK (ThoiGianXuLyMs IS NULL OR ThoiGianXuLyMs >= 0),
    CONSTRAINT CK_PhanTichAI_DaXong       CHECK (TrangThai <> 'DONE' OR (MucRuiRo IS NOT NULL AND DeXuat IS NOT NULL AND ThoiGianHoanThanh IS NOT NULL)),
    CONSTRAINT CK_PhanTichAI_Loi          CHECK (TrangThai <> 'FAILED' OR LoiXuLy IS NOT NULL);

ALTER TABLE TuKhoaCam ADD
    CONSTRAINT CK_TuKhoaCam_CumTu CHECK (LEN(LTRIM(RTRIM(CumTu))) > 0 AND LEN(LTRIM(RTRIM(CumTuChuanHoa))) > 0);

ALTER TABLE ThongBao ADD
    CONSTRAINT CK_ThongBao_Loai   CHECK (Loai IN ('ORDER','PRODUCT','SYSTEM')),
    CONSTRAINT CK_ThongBao_TieuDe CHECK (LEN(LTRIM(RTRIM(TieuDe))) > 0);

ALTER TABLE NhatKyHeThong ADD
    CONSTRAINT CK_NhatKy_KetQua CHECK (KetQua IN ('SUCCESS','FAILED','DENIED'));
GO



-- Ràng buộc cho các bảng mở rộng và các cột đã hợp nhất vào schema cuối.
ALTER TABLE dbo.TuKhoaCam ADD CONSTRAINT CK_TuKhoaCam_MucDo CHECK (MucDo IN ('LOW', 'HIGH'));
GO
ALTER TABLE dbo.PhanTichAI ADD CONSTRAINT CK_PhanTichAI_MucDoTuKhoa CHECK (MucDoTuKhoa IS NULL OR MucDoTuKhoa IN ('NONE','LOW','HIGH'));
GO
ALTER TABLE dbo.DonHang ADD CONSTRAINT CK_DonHang_HinhThucNhan CHECK (HinhThucNhan IN ('GIAO_HANG', 'GAP_TRUC_TIEP'));
GO
ALTER TABLE dbo.CuaHang ADD CONSTRAINT FK_CuaHang_TinhThanh FOREIGN KEY (MaTinhThanh) REFERENCES dbo.TinhThanh (MaTinhThanh);
GO
ALTER TABLE dbo.CuaHang ADD CONSTRAINT FK_CuaHang_PhuongXa FOREIGN KEY (MaPhuongXa) REFERENCES dbo.PhuongXa (MaPhuongXa);
GO
ALTER TABLE dbo.YeuThich ADD CONSTRAINT FK_YeuThich_NguoiDung FOREIGN KEY (MaNguoiDung) REFERENCES NguoiDung (MaNguoiDung);
GO
ALTER TABLE dbo.YeuThich ADD CONSTRAINT FK_YeuThich_SanPham   FOREIGN KEY (MaSanPham)   REFERENCES SanPham (MaSanPham);
GO
-- Mỗi người chỉ yêu thích một sản phẩm đúng 1 lần.
ALTER TABLE dbo.YeuThich ADD CONSTRAINT UX_YeuThich_NguoiSanPham UNIQUE (MaNguoiDung, MaSanPham);
GO
ALTER TABLE dbo.Banner ADD CONSTRAINT CK_Banner_ThuTu CHECK (ThuTuHienThi >= 1);
GO
ALTER TABLE dbo.Banner ADD CONSTRAINT CK_Banner_ThoiGian CHECK (KetThucLuc IS NULL OR BatDauTu IS NULL OR KetThucLuc > BatDauTu);
GO
ALTER TABLE dbo.DanhGia ADD CONSTRAINT UQ_DanhGia_DonHang   UNIQUE (MaDonHang);
GO
ALTER TABLE dbo.DanhGia ADD CONSTRAINT FK_DanhGia_DonHang   FOREIGN KEY (MaDonHang)  REFERENCES dbo.DonHang (MaDonHang);
GO
ALTER TABLE dbo.DanhGia ADD CONSTRAINT FK_DanhGia_NguoiMua  FOREIGN KEY (MaNguoiMua) REFERENCES dbo.NguoiDung (MaNguoiDung);
GO
ALTER TABLE dbo.DanhGia ADD CONSTRAINT FK_DanhGia_CuaHang   FOREIGN KEY (MaCuaHang)  REFERENCES dbo.CuaHang (MaCuaHang);
GO
ALTER TABLE dbo.DanhGia ADD CONSTRAINT CK_DanhGia_SoSao     CHECK (SoSao BETWEEN 1 AND 5);
GO
ALTER TABLE dbo.DanhGia ADD CONSTRAINT CK_DanhGia_TrangThai CHECK (TrangThai IN ('VISIBLE', 'HIDDEN'));
GO
ALTER TABLE dbo.TranhChap ADD CONSTRAINT FK_TranhChap_DonHang FOREIGN KEY (MaDonHang) REFERENCES DonHang(MaDonHang);
GO
ALTER TABLE dbo.TranhChap ADD CONSTRAINT FK_TranhChap_NguoiTao FOREIGN KEY (MaNguoiTao) REFERENCES NguoiDung(MaNguoiDung);
GO
ALTER TABLE dbo.TranhChap ADD CONSTRAINT FK_TranhChap_NguoiXuLy FOREIGN KEY (NguoiXuLyId) REFERENCES NguoiDung(MaNguoiDung);
GO
ALTER TABLE dbo.TranhChap ADD CONSTRAINT CK_TranhChap_TrangThai CHECK (TrangThai IN ('OPEN', 'PROCESSING', 'RESOLVED', 'REJECTED'));
GO
ALTER TABLE dbo.BaoCao ADD CONSTRAINT FK_BaoCao_NguoiBaoCao FOREIGN KEY (MaNguoiBaoCao) REFERENCES NguoiDung(MaNguoiDung);
GO
ALTER TABLE dbo.BaoCao ADD CONSTRAINT FK_BaoCao_NguoiXuLy FOREIGN KEY (NguoiXuLyId) REFERENCES NguoiDung(MaNguoiDung);
GO
ALTER TABLE dbo.BaoCao ADD CONSTRAINT CK_BaoCao_LoaiDoiTuong CHECK (LoaiDoiTuong IN ('PRODUCT', 'USER', 'SHOP'));
GO
ALTER TABLE dbo.BaoCao ADD CONSTRAINT CK_BaoCao_TrangThai CHECK (TrangThai IN ('PENDING', 'RESOLVED', 'DISMISSED'));
GO
ALTER TABLE dbo.PhienDauGia ADD CONSTRAINT FK_PhienDauGia_SanPham    FOREIGN KEY (MaSanPham)     REFERENCES SanPham (MaSanPham);
GO
ALTER TABLE dbo.PhienDauGia ADD CONSTRAINT FK_PhienDauGia_DanDau     FOREIGN KEY (MaNguoiDanDau) REFERENCES NguoiDung (MaNguoiDung);
GO
ALTER TABLE dbo.PhienDauGia ADD CONSTRAINT FK_PhienDauGia_NguoiThang FOREIGN KEY (MaNguoiThang)  REFERENCES NguoiDung (MaNguoiDung);
GO
ALTER TABLE dbo.PhienDauGia ADD CONSTRAINT FK_PhienDauGia_DonHang    FOREIGN KEY (MaDonHang)     REFERENCES DonHang (MaDonHang);
GO
ALTER TABLE dbo.PhienDauGia ADD CONSTRAINT CK_PhienDauGia_TrangThai  CHECK (TrangThai IN ('SCHEDULED','RUNNING','ENDED','CANCELLED'));
GO
ALTER TABLE dbo.PhienDauGia ADD CONSTRAINT CK_PhienDauGia_Gia        CHECK (GiaKhoiDiem BETWEEN 1000 AND 1000000000 AND BuocGia > 0 AND BuocGia <= 1000000000 AND GiaHienTai >= GiaKhoiDiem);
GO
ALTER TABLE dbo.PhienDauGia ADD CONSTRAINT CK_PhienDauGia_ThoiGian   CHECK (ThoiGianKetThuc > ThoiGianBatDau);
GO
ALTER TABLE dbo.PhienDauGia ADD CONSTRAINT CK_PhienDauGia_SoLuot     CHECK (SoLuotDat >= 0);
GO
ALTER TABLE dbo.PhienDauGia ADD CONSTRAINT CK_PhienDauGia_Huy        CHECK ((TrangThai = 'CANCELLED' AND NguoiHuy IN ('SELLER','ADMIN','SYSTEM') AND LyDoHuy IS NOT NULL AND ThoiGianHuy IS NOT NULL)
                                             OR (TrangThai <> 'CANCELLED' AND NguoiHuy IS NULL AND LyDoHuy IS NULL AND ThoiGianHuy IS NULL));
GO
ALTER TABLE dbo.PhienDauGia ADD CONSTRAINT CK_PhienDauGia_Thang      CHECK ((MaNguoiThang IS NULL AND GiaThang IS NULL AND MaDonHang IS NULL)
                                             OR (TrangThai = 'ENDED' AND MaNguoiThang IS NOT NULL AND GiaThang IS NOT NULL));
GO
ALTER TABLE dbo.LuotDatGia ADD CONSTRAINT FK_LuotDatGia_Phien     FOREIGN KEY (MaPhienDauGia) REFERENCES PhienDauGia (MaPhienDauGia);
GO
ALTER TABLE dbo.LuotDatGia ADD CONSTRAINT FK_LuotDatGia_NguoiDung FOREIGN KEY (MaNguoiDung)   REFERENCES NguoiDung (MaNguoiDung);
GO
ALTER TABLE dbo.LuotDatGia ADD CONSTRAINT CK_LuotDatGia_SoTien    CHECK (SoTien > 0);
GO
ALTER TABLE dbo.LichSuViPham ADD CONSTRAINT UQ_LichSuViPham_LuotKiemDuyet UNIQUE (MaLuotKiemDuyet);
GO
ALTER TABLE dbo.LichSuViPham ADD CONSTRAINT FK_LichSuViPham_NguoiBan    FOREIGN KEY (MaNguoiBan)      REFERENCES dbo.NguoiDung (MaNguoiDung);
GO
ALTER TABLE dbo.LichSuViPham ADD CONSTRAINT FK_LichSuViPham_SanPham     FOREIGN KEY (MaSanPham)       REFERENCES dbo.SanPham (MaSanPham);
GO
ALTER TABLE dbo.LichSuViPham ADD CONSTRAINT FK_LichSuViPham_Luot        FOREIGN KEY (MaLuotKiemDuyet) REFERENCES dbo.LuotKiemDuyet (MaLuotKiemDuyet);
GO
ALTER TABLE dbo.LichSuViPham ADD CONSTRAINT FK_LichSuViPham_QuanTriVien FOREIGN KEY (MaQuanTriVien)   REFERENCES dbo.NguoiDung (MaNguoiDung);
GO
ALTER TABLE dbo.LichSuViPham ADD CONSTRAINT CK_LichSuViPham_MucDo       CHECK (MucDo IN ('LOW','HIGH'));
GO
ALTER TABLE dbo.LichSuViPham ADD CONSTRAINT CK_LichSuViPham_Chi_Tiet    CHECK (ISJSON(ChiTietTuKhoa) = 1);
GO
ALTER TABLE dbo.LichSuViPham ADD CONSTRAINT CK_LichSuViPham_Nguon       CHECK (
              (NguonGhiNhan = 'AUTO_HIGH'          AND MucDo = 'HIGH' AND MaQuanTriVien IS NULL)
           OR (NguonGhiNhan = 'ADMIN_TU_CHOI_LOW'  AND MucDo = 'LOW'  AND MaQuanTriVien IS NOT NULL));
GO
ALTER TABLE dbo.HoiThoai ADD CONSTRAINT FK_HoiThoai_NguoiDung1 FOREIGN KEY (MaNguoiDung1) REFERENCES dbo.NguoiDung (MaNguoiDung);
GO
ALTER TABLE dbo.HoiThoai ADD CONSTRAINT FK_HoiThoai_NguoiDung2 FOREIGN KEY (MaNguoiDung2) REFERENCES dbo.NguoiDung (MaNguoiDung);
GO
ALTER TABLE dbo.HoiThoai ADD CONSTRAINT CK_HoiThoai_ThuTu CHECK (MaNguoiDung1 < MaNguoiDung2);
GO
ALTER TABLE dbo.HoiThoai ADD CONSTRAINT UQ_HoiThoai_Cap UNIQUE (MaNguoiDung1, MaNguoiDung2);
GO
ALTER TABLE dbo.TinNhan ADD CONSTRAINT FK_TinNhan_HoiThoai FOREIGN KEY (MaHoiThoai) REFERENCES dbo.HoiThoai (MaHoiThoai);
GO
ALTER TABLE dbo.TinNhan ADD CONSTRAINT FK_TinNhan_NguoiDung FOREIGN KEY (MaNguoiGui) REFERENCES dbo.NguoiDung (MaNguoiDung);
GO
ALTER TABLE dbo.TinNhan ADD CONSTRAINT CK_TinNhan_NoiDung CHECK (LEN(LTRIM(RTRIM(NoiDung))) > 0);
GO
ALTER TABLE dbo.CauHinhAI ADD CONSTRAINT CK_CauHinhAI_MotDong       CHECK (MaCauHinhAI = 1);
GO
ALTER TABLE dbo.CauHinhAI ADD CONSTRAINT CK_CauHinhAI_TenModel      CHECK (LEN(LTRIM(RTRIM(TenModel))) > 0);
GO
ALTER TABLE dbo.CauHinhAI ADD CONSTRAINT CK_CauHinhAI_SystemPrompt  CHECK (LEN(LTRIM(RTRIM(SystemPrompt))) > 0);
GO
ALTER TABLE dbo.CauHinhAI ADD CONSTRAINT FK_CauHinhAI_NguoiDung     FOREIGN KEY (MaNguoiCapNhat) REFERENCES dbo.NguoiDung (MaNguoiDung);
GO
ALTER TABLE dbo.PhuongXa ADD CONSTRAINT FK_PhuongXa_TinhThanh FOREIGN KEY (MaTinhThanh) REFERENCES dbo.TinhThanh (MaTinhThanh);
GO
ALTER TABLE dbo.PasswordResetOtp ADD CONSTRAINT CK_PasswordResetOtp_FailedAttempts CHECK (FailedAttempts BETWEEN 0 AND 5);
GO

-- Dọn dữ liệu trùng cũ trước khi tạo các unique index (không tác động database mới).
-- Dọn dẹp bản ghi tranh chấp trùng cũ nếu có trên DB dev trước khi tạo unique index
IF EXISTS (
    SELECT MaDonHang FROM dbo.TranhChap GROUP BY MaDonHang HAVING COUNT(*) > 1
)
BEGIN
    ;WITH CTE AS (
        SELECT MaTranhChap, ROW_NUMBER() OVER (PARTITION BY MaDonHang ORDER BY NgayTao DESC, MaTranhChap DESC) AS rn
        FROM dbo.TranhChap
    )
    DELETE FROM dbo.TranhChap WHERE MaTranhChap IN (SELECT MaTranhChap FROM CTE WHERE rn > 1);
END
GO
-- Dọn dẹp bản ghi báo cáo trùng PENDING cũ nếu có trên DB dev trước khi tạo unique index
IF EXISTS (
    SELECT MaNguoiBaoCao, LoaiDoiTuong, MaDoiTuong 
    FROM dbo.BaoCao 
    WHERE TrangThai = 'PENDING' 
    GROUP BY MaNguoiBaoCao, LoaiDoiTuong, MaDoiTuong 
    HAVING COUNT(*) > 1
)
BEGIN
    ;WITH CTE AS (
        SELECT MaBaoCao, ROW_NUMBER() OVER (PARTITION BY MaNguoiBaoCao, LoaiDoiTuong, MaDoiTuong ORDER BY NgayTao DESC, MaBaoCao DESC) AS rn
        FROM dbo.BaoCao
        WHERE TrangThai = 'PENDING'
    )
    DELETE FROM dbo.BaoCao WHERE MaBaoCao IN (SELECT MaBaoCao FROM CTE WHERE rn > 1);
END
GO

-- Toàn bộ index core và mở rộng.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SanPham_ACTIVE_MaDanhMuc_Gia' AND object_id = OBJECT_ID(N'dbo.SanPham'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_SanPham_ACTIVE_MaDanhMuc_Gia
    ON SanPham (MaDanhMuc, Gia) INCLUDE (MaCuaHang, MaTinhTrang, NgayTao)
    WHERE TrangThai = 'ACTIVE' AND DaXoa = 0;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SanPham_ACTIVE_NgayTao' AND object_id = OBJECT_ID(N'dbo.SanPham'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_SanPham_ACTIVE_NgayTao
    ON SanPham (NgayTao DESC) INCLUDE (Gia, MaDanhMuc)
    WHERE TrangThai = 'ACTIVE' AND DaXoa = 0;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SanPham_ACTIVE_ChuoiTimKiem' AND object_id = OBJECT_ID(N'dbo.SanPham'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_SanPham_ACTIVE_ChuoiTimKiem
    ON SanPham (ChuoiTimKiem)
    WHERE TrangThai = 'ACTIVE' AND DaXoa = 0;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SanPham_CuaHang_TrangThai' AND object_id = OBJECT_ID(N'dbo.SanPham'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_SanPham_CuaHang_TrangThai
    ON SanPham (MaCuaHang, TrangThai) INCLUDE (DaXoa, NgayCapNhat);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SanPham_TinhTrang' AND object_id = OBJECT_ID(N'dbo.SanPham'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_SanPham_TinhTrang ON SanPham (MaTinhTrang);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_GioHang_NguoiDung' AND object_id = OBJECT_ID(N'dbo.GioHang'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_GioHang_NguoiDung ON GioHang (MaNguoiDung) INCLUDE (MaSanPham, DaChon);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_GioHang_SanPham' AND object_id = OBJECT_ID(N'dbo.GioHang'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_GioHang_SanPham   ON GioHang (MaSanPham);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DonHang_NguoiMua' AND object_id = OBJECT_ID(N'dbo.DonHang'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_DonHang_NguoiMua
    ON DonHang (MaNguoiDung, TrangThai, NgayTao DESC);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DonHang_CuaHang' AND object_id = OBJECT_ID(N'dbo.DonHang'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_DonHang_CuaHang
    ON DonHang (MaCuaHang, TrangThai, NgayTao DESC);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DonHang_PENDING_QuaHan' AND object_id = OBJECT_ID(N'dbo.DonHang'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_DonHang_PENDING_QuaHan
    ON DonHang (NgayTao) WHERE TrangThai = 'PENDING';
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DonHang_DELIVERED_QuaHan' AND object_id = OBJECT_ID(N'dbo.DonHang'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_DonHang_DELIVERED_QuaHan
    ON DonHang (ThoiGianDaGiao) WHERE TrangThai = 'DELIVERED';
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ChiTietDonHang_SanPham' AND object_id = OBJECT_ID(N'dbo.ChiTietDonHang'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ChiTietDonHang_SanPham ON ChiTietDonHang (MaSanPham);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ThanhToan_DonHang' AND object_id = OBJECT_ID(N'dbo.ThanhToan'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ThanhToan_DonHang ON ThanhToan (MaDonHang);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_LichSuDon_DonHang' AND object_id = OBJECT_ID(N'dbo.LichSuTrangThaiDonHang'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_LichSuDon_DonHang     ON LichSuTrangThaiDonHang (MaDonHang, ThoiGian);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_LichSuSanPham_SanPham' AND object_id = OBJECT_ID(N'dbo.LichSuTrangThaiSanPham'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_LichSuSanPham_SanPham ON LichSuTrangThaiSanPham (MaSanPham, ThoiGian);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_LuotKiemDuyet_HangCho' AND object_id = OBJECT_ID(N'dbo.LuotKiemDuyet'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_LuotKiemDuyet_HangCho
    ON LuotKiemDuyet (ThoiGianGui) WHERE QuyetDinh IS NULL;
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_LuotKiemDuyet_SanPham' AND object_id = OBJECT_ID(N'dbo.LuotKiemDuyet'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_LuotKiemDuyet_SanPham
    ON LuotKiemDuyet (MaSanPham, ThoiGianGui DESC);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SanPham_PENDING_REVIEW' AND object_id = OBJECT_ID(N'dbo.SanPham'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_SanPham_PENDING_REVIEW
    ON SanPham (NgayTao)
    WHERE TrangThai = 'PENDING_REVIEW';
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PhanTichAI_HangDoi' AND object_id = OBJECT_ID(N'dbo.PhanTichAI'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_PhanTichAI_HangDoi
    ON PhanTichAI (NgayTao) WHERE TrangThai = 'QUEUED';
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_NguoiDung_VaiTro_TrangThai' AND object_id = OBJECT_ID(N'dbo.NguoiDung'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_NguoiDung_VaiTro_TrangThai ON NguoiDung (VaiTro, TrangThai);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CuaHang_TrangThai' AND object_id = OBJECT_ID(N'dbo.CuaHang'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_CuaHang_TrangThai          ON CuaHang (TrangThai);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_NhatKy_NguoiDung_ThoiGian' AND object_id = OBJECT_ID(N'dbo.NhatKyHeThong'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_NhatKy_NguoiDung_ThoiGian  ON NhatKyHeThong (MaNguoiDung, ThoiGian DESC);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_NhatKy_DoiTuong' AND object_id = OBJECT_ID(N'dbo.NhatKyHeThong'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_NhatKy_DoiTuong            ON NhatKyHeThong (LoaiDoiTuong, DoiTuongId);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ThongBao_NguoiDung' AND object_id = OBJECT_ID(N'dbo.ThongBao'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_ThongBao_NguoiDung ON ThongBao (MaNguoiDung, DaDoc, NgayTao DESC);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_YeuThich_NguoiDung_NgayTao' AND object_id = OBJECT_ID(N'dbo.YeuThich'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_YeuThich_NguoiDung_NgayTao ON YeuThich (MaNguoiDung, NgayTao DESC);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DanhGia_CuaHang' AND object_id = OBJECT_ID(N'dbo.DanhGia'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_DanhGia_CuaHang
        ON dbo.DanhGia (MaCuaHang, TrangThai) INCLUDE (SoSao, NgayTao);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DanhGia_NguoiMua' AND object_id = OBJECT_ID(N'dbo.DanhGia'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_DanhGia_NguoiMua
        ON dbo.DanhGia (MaNguoiMua);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_TranhChap_MaDonHang' AND object_id = OBJECT_ID(N'dbo.TranhChap'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_TranhChap_MaDonHang ON TranhChap(MaDonHang);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_TranhChap_TrangThai' AND object_id = OBJECT_ID(N'dbo.TranhChap'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_TranhChap_TrangThai ON TranhChap(TrangThai);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_TranhChap_MaDonHang' AND object_id = OBJECT_ID(N'dbo.TranhChap'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UQ_TranhChap_MaDonHang ON TranhChap(MaDonHang);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_BaoCao_DoiTuong' AND object_id = OBJECT_ID(N'dbo.BaoCao'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_BaoCao_DoiTuong ON BaoCao(LoaiDoiTuong, MaDoiTuong);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_BaoCao_TrangThai' AND object_id = OBJECT_ID(N'dbo.BaoCao'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_BaoCao_TrangThai ON BaoCao(TrangThai);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_BaoCao_ChongTrung' AND object_id = OBJECT_ID(N'dbo.BaoCao'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UQ_BaoCao_ChongTrung ON BaoCao(MaNguoiBaoCao, LoaiDoiTuong, MaDoiTuong) WHERE TrangThai = 'PENDING';
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_PhienDauGia_SanPham_DangMo' AND object_id = OBJECT_ID(N'dbo.PhienDauGia'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UX_PhienDauGia_SanPham_DangMo
    ON PhienDauGia (MaSanPham) WHERE TrangThai IN ('SCHEDULED','RUNNING');
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PhienDauGia_TrangThai_KetThuc' AND object_id = OBJECT_ID(N'dbo.PhienDauGia'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_PhienDauGia_TrangThai_KetThuc ON PhienDauGia (TrangThai, ThoiGianKetThuc);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_LuotDatGia_Phien_SoTien' AND object_id = OBJECT_ID(N'dbo.LuotDatGia'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_LuotDatGia_Phien_SoTien ON LuotDatGia (MaPhienDauGia, SoTien DESC);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_LichSuViPham_NguoiBan' AND object_id = OBJECT_ID(N'dbo.LichSuViPham'))
BEGIN
    CREATE NONCLUSTERED INDEX IX_LichSuViPham_NguoiBan ON dbo.LichSuViPham (MaNguoiBan, ThoiGian DESC);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_HoiThoai_NguoiDung2' AND object_id = OBJECT_ID(N'dbo.HoiThoai'))
BEGIN
    CREATE INDEX IX_HoiThoai_NguoiDung2 ON dbo.HoiThoai (MaNguoiDung2);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_TinNhan_HoiThoai' AND object_id = OBJECT_ID(N'dbo.TinNhan'))
BEGIN
    CREATE INDEX IX_TinNhan_HoiThoai ON dbo.TinNhan (MaHoiThoai, MaTinNhan DESC) INCLUDE (MaNguoiGui, DaDoc);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_TinNhan_NguoiGui' AND object_id = OBJECT_ID(N'dbo.TinNhan'))
BEGIN
    CREATE INDEX IX_TinNhan_NguoiGui ON dbo.TinNhan (MaNguoiGui, NgayGui);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_TinhThanh_MaHanhChinh' AND object_id = OBJECT_ID(N'dbo.TinhThanh'))
BEGIN
    CREATE UNIQUE INDEX UX_TinhThanh_MaHanhChinh ON dbo.TinhThanh (MaHanhChinh);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PhuongXa_MaTinhThanh' AND object_id = OBJECT_ID(N'dbo.PhuongXa'))
BEGIN
    CREATE INDEX IX_PhuongXa_MaTinhThanh ON dbo.PhuongXa (MaTinhThanh) INCLUDE (TenPhuongXa);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_PhuongXa_MaHanhChinh' AND object_id = OBJECT_ID(N'dbo.PhuongXa'))
BEGIN
    CREATE UNIQUE INDEX UX_PhuongXa_MaHanhChinh ON dbo.PhuongXa (MaHanhChinh);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_CuaHang_MaTinhThanh' AND object_id = OBJECT_ID(N'dbo.CuaHang'))
BEGIN
    CREATE INDEX IX_CuaHang_MaTinhThanh ON dbo.CuaHang (MaTinhThanh, MaPhuongXa);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PasswordResetOtp_Email_CreatedAtUtc' AND object_id = OBJECT_ID(N'dbo.PasswordResetOtp'))
BEGIN
    CREATE INDEX IX_PasswordResetOtp_Email_CreatedAtUtc ON dbo.PasswordResetOtp(Email, CreatedAtUtc DESC);
END;
GO

PRINT N'Đã áp dụng xong toàn bộ constraints và indexes.';
GO
