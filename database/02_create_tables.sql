/* TASK 3.5 - Nhom bang kiem duyet. Chay sau khi dbo.SanPham va dbo.NguoiDung da ton tai.
   Schema/FK theo E1: SanPham(Id), NguoiDung(Id). AI chi la goi y, khong tu doi trang thai san pham. */
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.TuKhoaCam', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TuKhoaCam (
        Id INT IDENTITY(1,1) NOT NULL,
        CumTu NVARCHAR(100) NOT NULL,
        CumTuChuanHoa NVARCHAR(100) NOT NULL,
        DangApDung BIT NOT NULL CONSTRAINT DF_TuKhoaCam_DangApDung DEFAULT (1),
        NgayTao DATETIME2(3) NOT NULL CONSTRAINT DF_TuKhoaCam_NgayTao DEFAULT (SYSUTCDATETIME()),
        NgayCapNhat DATETIME2(3) NOT NULL CONSTRAINT DF_TuKhoaCam_NgayCapNhat DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_TuKhoaCam PRIMARY KEY (Id),
        CONSTRAINT UQ_TuKhoaCam_CumTuChuanHoa UNIQUE (CumTuChuanHoa),
        CONSTRAINT CK_TuKhoaCam_CumTuChuanHoa_DoDai CHECK (LEN(LTRIM(RTRIM(CumTuChuanHoa))) >= 2),
        CONSTRAINT CK_TuKhoaCam_CumTu_KhongRong CHECK (LEN(LTRIM(RTRIM(CumTu))) >= 1)
    );
END;
GO

IF OBJECT_ID(N'dbo.LuotKiemDuyet', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.LuotKiemDuyet (
        Id INT IDENTITY(1,1) NOT NULL,
        SanPhamId INT NOT NULL,
        ThoiGianGui DATETIME2(3) NOT NULL CONSTRAINT DF_LuotKiemDuyet_ThoiGianGui DEFAULT (SYSUTCDATETIME()),
        KetQuaRule NVARCHAR(MAX) NOT NULL,
        QuyetDinh VARCHAR(10) NULL,
        LyDoTuChoi NVARCHAR(500) NULL,
        AdminId INT NULL,
        ThoiGianQuyetDinh DATETIME2(3) NULL,
        CONSTRAINT PK_LuotKiemDuyet PRIMARY KEY (Id),
        CONSTRAINT FK_LuotKiemDuyet_SanPham FOREIGN KEY (SanPhamId) REFERENCES dbo.SanPham(Id),
        CONSTRAINT FK_LuotKiemDuyet_NguoiDung FOREIGN KEY (AdminId) REFERENCES dbo.NguoiDung(Id),
        CONSTRAINT CK_LuotKiemDuyet_QuyetDinh CHECK (QuyetDinh IS NULL OR QuyetDinh IN ('APPROVED','REJECTED')),
        CONSTRAINT CK_LuotKiemDuyet_TrangThaiXuLy CHECK (
            (QuyetDinh IS NULL AND AdminId IS NULL AND ThoiGianQuyetDinh IS NULL AND LyDoTuChoi IS NULL)
            OR (QuyetDinh IS NOT NULL AND AdminId IS NOT NULL AND ThoiGianQuyetDinh IS NOT NULL)
        ),
        CONSTRAINT CK_LuotKiemDuyet_LyDoTuChoi CHECK (
            QuyetDinh IS NULL OR QuyetDinh <> 'REJECTED' OR NULLIF(LTRIM(RTRIM(LyDoTuChoi)), '') IS NOT NULL
        ),
        CONSTRAINT CK_LuotKiemDuyet_KetQuaRule CHECK (ISJSON(KetQuaRule) = 1),
        CONSTRAINT CK_LuotKiemDuyet_ThoiGian CHECK (ThoiGianQuyetDinh IS NULL OR ThoiGianQuyetDinh >= ThoiGianGui)
    );
END;
GO

IF OBJECT_ID(N'dbo.PhanTichAI', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PhanTichAI (
        Id INT IDENTITY(1,1) NOT NULL,
        LuotKiemDuyetId INT NOT NULL,
        TrangThai VARCHAR(10) NOT NULL CONSTRAINT DF_PhanTichAI_TrangThai DEFAULT ('QUEUED'),
        SoLanThu INT NOT NULL CONSTRAINT DF_PhanTichAI_SoLanThu DEFAULT (0),
        MucRuiRo VARCHAR(10) NULL,
        DauHieu NVARCHAR(MAX) NULL,
        DeXuat VARCHAR(10) NULL,
        GiaiThich NVARCHAR(2000) NULL,
        MaBamDauVao VARCHAR(64) NULL,
        PhienBanMoHinh VARCHAR(50) NULL,
        ThoiGianXuLyMs INT NULL,
        ThongBaoLoi NVARCHAR(500) NULL,
        DaLamTheoDeXuat BIT NULL,
        NgayTao DATETIME2(3) NOT NULL CONSTRAINT DF_PhanTichAI_NgayTao DEFAULT (SYSUTCDATETIME()),
        NgayCapNhat DATETIME2(3) NOT NULL CONSTRAINT DF_PhanTichAI_NgayCapNhat DEFAULT (SYSUTCDATETIME()),
        ThoiGianHoanThanh DATETIME2(3) NULL,
        CONSTRAINT PK_PhanTichAI PRIMARY KEY (Id),
        CONSTRAINT FK_PhanTichAI_LuotKiemDuyet FOREIGN KEY (LuotKiemDuyetId) REFERENCES dbo.LuotKiemDuyet(Id),
        CONSTRAINT UQ_PhanTichAI_LuotKiemDuyet UNIQUE (LuotKiemDuyetId),
        CONSTRAINT CK_PhanTichAI_TrangThai CHECK (TrangThai IN ('QUEUED','RUNNING','DONE','FAILED')),
        CONSTRAINT CK_PhanTichAI_MucRuiRo CHECK (MucRuiRo IS NULL OR MucRuiRo IN ('LOW','MEDIUM','HIGH')),
        CONSTRAINT CK_PhanTichAI_DeXuat CHECK (DeXuat IS NULL OR DeXuat IN ('APPROVE','REJECT','REVIEW')),
        CONSTRAINT CK_PhanTichAI_DauHieu CHECK (DauHieu IS NULL OR ISJSON(DauHieu) = 1),
        CONSTRAINT CK_PhanTichAI_Done CHECK (TrangThai <> 'DONE' OR (MucRuiRo IS NOT NULL AND DeXuat IS NOT NULL)),
        CONSTRAINT CK_PhanTichAI_Failed CHECK (TrangThai <> 'FAILED' OR NULLIF(LTRIM(RTRIM(ThongBaoLoi)), '') IS NOT NULL),
        CONSTRAINT CK_PhanTichAI_HoanThanh CHECK (TrangThai NOT IN ('DONE','FAILED') OR ThoiGianHoanThanh IS NOT NULL),
        CONSTRAINT CK_PhanTichAI_ThoiGianXuLy CHECK (ThoiGianXuLyMs IS NULL OR ThoiGianXuLyMs >= 0),
        CONSTRAINT CK_PhanTichAI_SoLanThu CHECK (SoLanThu >= 0),
        CONSTRAINT CK_PhanTichAI_DaLamTheoDeXuat CHECK (DaLamTheoDeXuat IS NULL OR DeXuat IN ('APPROVE','REJECT'))
    );
END;
GO
