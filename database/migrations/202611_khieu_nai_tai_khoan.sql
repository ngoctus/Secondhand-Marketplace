/* =====================================================================
   KN-01 — MIGRATION NÂNG CẤP CHO DATABASE ĐANG HOẠT ĐỘNG (IDEMPOTENT)
   Chức năng: Khiếu nại khóa nhầm / Cam kết mở khóa tài khoản & Reset gậy.
   An toàn khi chạy lại nhiều lần, KHÔNG xóa dữ liệu hiện có.
   ===================================================================== */

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ARITHABORT ON;
GO

-- 1. Bổ sung cột MocTinhViPham & SoLanBiKhoa trên NguoiDung
IF COL_LENGTH(N'dbo.NguoiDung', N'MocTinhViPham') IS NULL
BEGIN
    ALTER TABLE dbo.NguoiDung ADD MocTinhViPham datetime2(3) NULL;
END;
GO

IF COL_LENGTH(N'dbo.NguoiDung', N'SoLanBiKhoa') IS NULL
BEGIN
    ALTER TABLE dbo.NguoiDung
        ADD SoLanBiKhoa int NOT NULL CONSTRAINT DF_NguoiDung_SoLanBiKhoa DEFAULT (0);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_NguoiDung_SoLanBiKhoa' AND parent_object_id = OBJECT_ID(N'dbo.NguoiDung'))
BEGIN
    ALTER TABLE dbo.NguoiDung ADD CONSTRAINT CK_NguoiDung_SoLanBiKhoa CHECK (SoLanBiKhoa >= 0);
END;
GO

-- Chuẩn hóa tài khoản đang bị LOCKED từ trước khi có cột SoLanBiKhoa
UPDATE dbo.NguoiDung
SET SoLanBiKhoa = 1
WHERE TrangThai = 'LOCKED' AND SoLanBiKhoa = 0;
GO

-- 1b. Bổ sung cột PhanHoiShop & NgayPhanHoi trên DanhGia, AnhPhanHoi trên TranhChap (nếu DB cũ chưa có)
IF OBJECT_ID(N'dbo.DanhGia', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.DanhGia', N'PhanHoiShop') IS NULL
BEGIN
    ALTER TABLE dbo.DanhGia ADD PhanHoiShop nvarchar(1000) NULL;
END;
GO

IF OBJECT_ID(N'dbo.DanhGia', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.DanhGia', N'NgayPhanHoi') IS NULL
BEGIN
    ALTER TABLE dbo.DanhGia ADD NgayPhanHoi datetime2(3) NULL;
END;
GO

IF OBJECT_ID(N'dbo.TranhChap', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.TranhChap', N'AnhPhanHoi') IS NULL
BEGIN
    ALTER TABLE dbo.TranhChap ADD AnhPhanHoi nvarchar(500) NULL;
END;
GO

-- 2. Tạo bảng KhieuNaiTaiKhoan
IF OBJECT_ID(N'dbo.KhieuNaiTaiKhoan', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.KhieuNaiTaiKhoan (
        MaKhieuNai          int            IDENTITY(1,1) NOT NULL,
        MaNguoiDung         int            NOT NULL,
        LoaiYeuCau          varchar(20)    NOT NULL,
        LanKhoaThu          int            NOT NULL,
        LyDoKhoaLucGui      nvarchar(500)  NOT NULL,
        NoiDung             nvarchar(2000) NOT NULL,
        BangChungAnh        varchar(500)   NULL,
        YeuCauPhi           bit            NOT NULL CONSTRAINT DF_KhieuNaiTaiKhoan_YeuCauPhi DEFAULT (0),
        SoTienPhi           decimal(18,2)  NOT NULL CONSTRAINT DF_KhieuNaiTaiKhoan_SoTienPhi DEFAULT (0),
        MaGiaoDichThamChieu varchar(40)    NULL,
        TrangThai           varchar(20)    NOT NULL CONSTRAINT DF_KhieuNaiTaiKhoan_TrangThai DEFAULT ('PENDING'),
        GhiChuAdmin         nvarchar(1000) NULL,
        NguoiXuLyId         int            NULL,
        NgayTao             datetime2(3)   NOT NULL CONSTRAINT DF_KhieuNaiTaiKhoan_NgayTao   DEFAULT (SYSUTCDATETIME()),
        NgayXuLy            datetime2(3)   NULL,
        CONSTRAINT PK_KhieuNaiTaiKhoan PRIMARY KEY CLUSTERED (MaKhieuNai)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_KhieuNaiTaiKhoan_NguoiDung')
    ALTER TABLE dbo.KhieuNaiTaiKhoan ADD CONSTRAINT FK_KhieuNaiTaiKhoan_NguoiDung FOREIGN KEY (MaNguoiDung) REFERENCES dbo.NguoiDung (MaNguoiDung);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_KhieuNaiTaiKhoan_NguoiXuLy')
    ALTER TABLE dbo.KhieuNaiTaiKhoan ADD CONSTRAINT FK_KhieuNaiTaiKhoan_NguoiXuLy FOREIGN KEY (NguoiXuLyId) REFERENCES dbo.NguoiDung (MaNguoiDung);
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_KhieuNaiTaiKhoan_LoaiYeuCau')
    ALTER TABLE dbo.KhieuNaiTaiKhoan ADD CONSTRAINT CK_KhieuNaiTaiKhoan_LoaiYeuCau CHECK (LoaiYeuCau IN ('APPEAL', 'UNLOCK_REQUEST'));
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_KhieuNaiTaiKhoan_TrangThai')
    ALTER TABLE dbo.KhieuNaiTaiKhoan ADD CONSTRAINT CK_KhieuNaiTaiKhoan_TrangThai CHECK (TrangThai IN ('PENDING', 'APPROVED', 'REJECTED'));
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_KhieuNaiTaiKhoan_LanKhoaThu')
    ALTER TABLE dbo.KhieuNaiTaiKhoan ADD CONSTRAINT CK_KhieuNaiTaiKhoan_LanKhoaThu CHECK (LanKhoaThu >= 1);
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_KhieuNaiTaiKhoan_SoTienPhi')
    ALTER TABLE dbo.KhieuNaiTaiKhoan ADD CONSTRAINT CK_KhieuNaiTaiKhoan_SoTienPhi CHECK (SoTienPhi >= 0);
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_KhieuNaiTaiKhoan_NoiDung')
    ALTER TABLE dbo.KhieuNaiTaiKhoan ADD CONSTRAINT CK_KhieuNaiTaiKhoan_NoiDung CHECK (LEN(LTRIM(RTRIM(NoiDung))) >= 10);
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_KhieuNaiTaiKhoan_XuLy')
    ALTER TABLE dbo.KhieuNaiTaiKhoan ADD CONSTRAINT CK_KhieuNaiTaiKhoan_XuLy CHECK (
        (TrangThai = 'PENDING' AND NgayXuLy IS NULL)
        OR (TrangThai IN ('APPROVED', 'REJECTED') AND NgayXuLy IS NOT NULL));
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_KhieuNaiTaiKhoan_DangCho' AND object_id = OBJECT_ID(N'dbo.KhieuNaiTaiKhoan'))
    CREATE UNIQUE NONCLUSTERED INDEX UQ_KhieuNaiTaiKhoan_DangCho ON dbo.KhieuNaiTaiKhoan (MaNguoiDung) WHERE TrangThai = 'PENDING';
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_KhieuNaiTaiKhoan_TrangThai_NgayTao' AND object_id = OBJECT_ID(N'dbo.KhieuNaiTaiKhoan'))
    CREATE NONCLUSTERED INDEX IX_KhieuNaiTaiKhoan_TrangThai_NgayTao ON dbo.KhieuNaiTaiKhoan (TrangThai, NgayTao DESC);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_KhieuNaiTaiKhoan_MaNguoiDung_NgayTao' AND object_id = OBJECT_ID(N'dbo.KhieuNaiTaiKhoan'))
    CREATE NONCLUSTERED INDEX IX_KhieuNaiTaiKhoan_MaNguoiDung_NgayTao ON dbo.KhieuNaiTaiKhoan (MaNguoiDung, NgayTao DESC);
GO

-- 3. Tạo bảng cấu hình 1 dòng CauHinhKhieuNaiTaiKhoan
IF OBJECT_ID(N'dbo.CauHinhKhieuNaiTaiKhoan', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CauHinhKhieuNaiTaiKhoan (
        Id                int           NOT NULL,
        SoLanGuiMoiChuKy  int           NOT NULL CONSTRAINT DF_CauHinhKhieuNai_SoLanGui    DEFAULT (2),
        CooldownPhut      int           NOT NULL CONSTRAINT DF_CauHinhKhieuNai_Cooldown    DEFAULT (4320),
        PhiMoKhoaLan2     decimal(18,2) NOT NULL CONSTRAINT DF_CauHinhKhieuNai_Phi         DEFAULT (200000),
        TenNganHang       nvarchar(100) NOT NULL,
        MaNganHang        varchar(20)   NOT NULL,
        SoTaiKhoan        varchar(30)   NOT NULL,
        ChuTaiKhoan       nvarchar(100) NOT NULL,
        NgayCapNhat       datetime2(3)  NOT NULL CONSTRAINT DF_CauHinhKhieuNai_NgayCapNhat DEFAULT (SYSUTCDATETIME()),
        NguoiCapNhatId    int           NULL,
        CONSTRAINT PK_CauHinhKhieuNaiTaiKhoan PRIMARY KEY CLUSTERED (Id)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_CauHinhKhieuNai_NguoiCapNhat')
    ALTER TABLE dbo.CauHinhKhieuNaiTaiKhoan ADD CONSTRAINT FK_CauHinhKhieuNai_NguoiCapNhat FOREIGN KEY (NguoiCapNhatId) REFERENCES dbo.NguoiDung (MaNguoiDung);
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_CauHinhKhieuNai_MotDong')
    ALTER TABLE dbo.CauHinhKhieuNaiTaiKhoan ADD CONSTRAINT CK_CauHinhKhieuNai_MotDong CHECK (Id = 1);
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_CauHinhKhieuNai_SoLanGui')
    ALTER TABLE dbo.CauHinhKhieuNaiTaiKhoan ADD CONSTRAINT CK_CauHinhKhieuNai_SoLanGui CHECK (SoLanGuiMoiChuKy >= 1);
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_CauHinhKhieuNai_Cooldown')
    ALTER TABLE dbo.CauHinhKhieuNaiTaiKhoan ADD CONSTRAINT CK_CauHinhKhieuNai_Cooldown CHECK (CooldownPhut >= 1);
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_CauHinhKhieuNai_Phi')
    ALTER TABLE dbo.CauHinhKhieuNaiTaiKhoan ADD CONSTRAINT CK_CauHinhKhieuNai_Phi CHECK (PhiMoKhoaLan2 >= 0);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.CauHinhKhieuNaiTaiKhoan WHERE Id = 1)
BEGIN
    INSERT INTO dbo.CauHinhKhieuNaiTaiKhoan (
        Id, SoLanGuiMoiChuKy, CooldownPhut, PhiMoKhoaLan2,
        TenNganHang, MaNganHang, SoTaiKhoan, ChuTaiKhoan
    )
    VALUES (
        1, 2, 4320, 200000,
        N'MB Bank', 'MB', '090123456789', N'RE MARKET PLATFORM'
    );
END;
GO

PRINT N'Đã áp dụng xong migration KN-01 (KhieuNaiTaiKhoan & CauHinhKhieuNaiTaiKhoan).';
GO
