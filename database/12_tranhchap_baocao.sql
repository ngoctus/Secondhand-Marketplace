SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

USE SecondHandMarketplace;
GO

-- Xóa bảng cũ nếu tồn tại
DROP TABLE IF EXISTS TranhChap;
DROP TABLE IF EXISTS BaoCao;
GO

-- 1. Bảng TranhChap: giải quyết khiếu nại đơn hàng giữa người mua và shop
CREATE TABLE TranhChap (
    MaTranhChap       int            IDENTITY(1,1) NOT NULL,
    MaDonHang         int            NOT NULL,
    MaNguoiTao        int            NOT NULL,
    LyDo              nvarchar(500)  NOT NULL,
    BangChungAnh      nvarchar(500)  NULL,
    PhanHoiNguoiBan   nvarchar(1000) NULL,
    TrangThai         varchar(20)    NOT NULL CONSTRAINT DF_TranhChap_TrangThai DEFAULT ('OPEN'),
    PhanQuyet         nvarchar(500)  NULL,
    GhiChuAdmin       nvarchar(1000) NULL,
    NguoiXuLyId       int            NULL,
    NgayTao           datetime2(3)   NOT NULL CONSTRAINT DF_TranhChap_NgayTao DEFAULT (SYSUTCDATETIME()),
    NgayCapNhat       datetime2(3)   NOT NULL CONSTRAINT DF_TranhChap_NgayCapNhat DEFAULT (SYSUTCDATETIME()),
    NgayGiaiQuyet     datetime2(3)   NULL,

    CONSTRAINT PK_TranhChap PRIMARY KEY CLUSTERED (MaTranhChap),
    CONSTRAINT FK_TranhChap_DonHang FOREIGN KEY (MaDonHang) REFERENCES DonHang(MaDonHang),
    CONSTRAINT FK_TranhChap_NguoiTao FOREIGN KEY (MaNguoiTao) REFERENCES NguoiDung(MaNguoiDung),
    CONSTRAINT FK_TranhChap_NguoiXuLy FOREIGN KEY (NguoiXuLyId) REFERENCES NguoiDung(MaNguoiDung),
    CONSTRAINT CK_TranhChap_TrangThai CHECK (TrangThai IN ('OPEN', 'PROCESSING', 'RESOLVED', 'REJECTED'))
);
GO

-- 2. Bảng BaoCao: báo cáo vi phạm sản phẩm, người dùng hoặc cửa hàng
CREATE TABLE BaoCao (
    MaBaoCao          int            IDENTITY(1,1) NOT NULL,
    LoaiDoiTuong      varchar(10)    NOT NULL,
    MaDoiTuong        int            NOT NULL,
    MaNguoiBaoCao     int            NOT NULL,
    LyDo              nvarchar(255)  NOT NULL,
    MoTa              nvarchar(1000) NULL,
    TrangThai         varchar(15)    NOT NULL CONSTRAINT DF_BaoCao_TrangThai DEFAULT ('PENDING'),
    NguoiXuLyId       int            NULL,
    GhiChuXuLy        nvarchar(500)  NULL,
    NgayTao           datetime2(3)   NOT NULL CONSTRAINT DF_BaoCao_NgayTao DEFAULT (SYSUTCDATETIME()),
    NgayXuLy          datetime2(3)   NULL,

    CONSTRAINT PK_BaoCao PRIMARY KEY CLUSTERED (MaBaoCao),
    CONSTRAINT FK_BaoCao_NguoiBaoCao FOREIGN KEY (MaNguoiBaoCao) REFERENCES NguoiDung(MaNguoiDung),
    CONSTRAINT FK_BaoCao_NguoiXuLy FOREIGN KEY (NguoiXuLyId) REFERENCES NguoiDung(MaNguoiDung),
    CONSTRAINT CK_BaoCao_LoaiDoiTuong CHECK (LoaiDoiTuong IN ('PRODUCT', 'USER', 'SHOP')),
    CONSTRAINT CK_BaoCao_TrangThai CHECK (TrangThai IN ('PENDING', 'RESOLVED', 'DISMISSED'))
);
GO

-- Chỉ mục tìm kiếm và chống gửi báo cáo trùng khi chưa xử lý
CREATE NONCLUSTERED INDEX IX_TranhChap_MaDonHang ON TranhChap(MaDonHang);
CREATE NONCLUSTERED INDEX IX_TranhChap_TrangThai ON TranhChap(TrangThai);
CREATE NONCLUSTERED INDEX IX_BaoCao_DoiTuong ON BaoCao(LoaiDoiTuong, MaDoiTuong);
CREATE NONCLUSTERED INDEX IX_BaoCao_TrangThai ON BaoCao(TrangThai);
CREATE UNIQUE NONCLUSTERED INDEX UQ_BaoCao_ChongTrung ON BaoCao(MaNguoiBaoCao, LoaiDoiTuong, MaDoiTuong) WHERE TrangThai = 'PENDING';
GO

-- 3. Dữ liệu mẫu (Seed Data) cho Tranh chấp và Báo cáo vi phạm
DECLARE @AdminId INT = (SELECT TOP 1 MaNguoiDung FROM NguoiDung WHERE VaiTro = 'ADMIN');
DECLARE @DungId  INT = (SELECT TOP 1 MaNguoiDung FROM NguoiDung WHERE Email = 'dung.buyer@demo.local');
DECLARE @ChiId   INT = (SELECT TOP 1 MaNguoiDung FROM NguoiDung WHERE Email = 'chi.buyer@demo.local');
DECLARE @DonHang4 INT = (SELECT TOP 1 MaDonHang FROM DonHang WHERE MaDon = 'DH20261001-000004');
DECLARE @DonHang5 INT = (SELECT TOP 1 MaDonHang FROM DonHang WHERE MaDon = 'DH20261001-000005');
DECLARE @Sp1 INT = (SELECT TOP 1 MaSanPham FROM SanPham WHERE DaXoa = 0);

IF @DonHang4 IS NOT NULL AND @DungId IS NOT NULL
BEGIN
    INSERT INTO TranhChap (MaDonHang, MaNguoiTao, LyDo, BangChungAnh, TrangThai, NgayTao, NgayCapNhat)
    VALUES (@DonHang4, @DungId, N'Hàng nhận được bị trầy xước nặng, không đúng mô tả sản phẩm 99%.', '/uploads/disputes/evidence1.jpg', 'OPEN', DATEADD(DAY, -2, SYSUTCDATETIME()), DATEADD(DAY, -2, SYSUTCDATETIME()));
END

IF @DonHang5 IS NOT NULL AND @ChiId IS NOT NULL AND @AdminId IS NOT NULL
BEGIN
    INSERT INTO TranhChap (MaDonHang, MaNguoiTao, LyDo, BangChungAnh, PhanHoiNguoiBan, TrangThai, PhanQuyet, GhiChuAdmin, NguoiXuLyId, NgayTao, NgayCapNhat, NgayGiaiQuyet)
    VALUES (@DonHang5, @ChiId, N'Giao thiếu phụ kiện đi kèm theo mô tả ban đầu.', '/uploads/disputes/evidence2.jpg', N'Shop đồng ý hoàn lại 50.000đ tiền phụ kiện bị thiếu.', 'RESOLVED', N'Chấp nhận hoàn tiền 50.000đ cho người mua.', N'Hai bên đã thống nhất qua tin nhắn.', @AdminId, DATEADD(DAY, -5, SYSUTCDATETIME()), DATEADD(DAY, -1, SYSUTCDATETIME()), DATEADD(DAY, -1, SYSUTCDATETIME()));
END

IF @ChiId IS NOT NULL AND @Sp1 IS NOT NULL
BEGIN
    INSERT INTO BaoCao (LoaiDoiTuong, MaDoiTuong, MaNguoiBaoCao, LyDo, MoTa, TrangThai, NgayTao)
    VALUES ('PRODUCT', @Sp1, @ChiId, N'Hàng giả, hàng nhái thương hiệu', N'Sản phẩm ghi chính hãng nhưng hình ảnh cho thấy là hàng nhái kém chất lượng.', 'PENDING', DATEADD(DAY, -1, SYSUTCDATETIME()));
END

IF @DungId IS NOT NULL AND @AdminId IS NOT NULL
BEGIN
    INSERT INTO BaoCao (LoaiDoiTuong, MaDoiTuong, MaNguoiBaoCao, LyDo, MoTa, TrangThai, NguoiXuLyId, GhiChuXuLy, NgayTao, NgayXuLy)
    VALUES ('USER', 2, @DungId, N'Hành vi gian lận', N'Người dùng nhắn tin yêu cầu chuyển cọc ngoài hệ thống.', 'RESOLVED', @AdminId, N'Đã gửi cảnh cáo nghiêm khắc đến người dùng.', DATEADD(DAY, -3, SYSUTCDATETIME()), DATEADD(DAY, -2, SYSUTCDATETIME()));
END
GO
