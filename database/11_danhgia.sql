USE SecondHandMarketplace;
GO

IF OBJECT_ID(N'dbo.DanhGia', N'U') IS NULL
BEGIN
	CREATE TABLE dbo.DanhGia (
		MaDanhGia        int IDENTITY(1,1) NOT NULL,
		MaDonHang        int NOT NULL,
		MaChiTietDonHang int NOT NULL,
		MaNguoiMua       int NOT NULL,
		MaCuaHang        int NOT NULL,
		SoSao            tinyint NOT NULL,
		NoiDung          nvarchar(1000) NULL,
		TrangThai        varchar(15) NOT NULL CONSTRAINT DF_DanhGia_TrangThai DEFAULT ('PENDING'),
		MaAdminXuLy      int NULL,
		GhiChuAdmin      nvarchar(1000) NULL,
		NgayTao          datetime2(3) NOT NULL CONSTRAINT DF_DanhGia_NgayTao DEFAULT (SYSUTCDATETIME()),
		NgayDuyet        datetime2(3) NULL,
		CONSTRAINT PK_DanhGia PRIMARY KEY CLUSTERED (MaDanhGia),
		CONSTRAINT UX_DanhGia_ChiTiet UNIQUE (MaChiTietDonHang),
		CONSTRAINT CK_DanhGia_SoSao CHECK (SoSao BETWEEN 1 AND 5),
		CONSTRAINT CK_DanhGia_TrangThai CHECK (TrangThai IN ('PENDING', 'APPROVED', 'REJECTED')),
		CONSTRAINT FK_DanhGia_DonHang FOREIGN KEY (MaDonHang) REFERENCES dbo.DonHang(MaDonHang),
		CONSTRAINT FK_DanhGia_ChiTiet FOREIGN KEY (MaChiTietDonHang) REFERENCES dbo.ChiTietDonHang(MaChiTietDonHang),
		CONSTRAINT FK_DanhGia_NguoiMua FOREIGN KEY (MaNguoiMua) REFERENCES dbo.NguoiDung(MaNguoiDung),
		CONSTRAINT FK_DanhGia_CuaHang FOREIGN KEY (MaCuaHang) REFERENCES dbo.CuaHang(MaCuaHang),
		CONSTRAINT FK_DanhGia_Admin FOREIGN KEY (MaAdminXuLy) REFERENCES dbo.NguoiDung(MaNguoiDung)
	);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DanhGia_CuaHang_TrangThai' AND object_id = OBJECT_ID(N'dbo.DanhGia'))
	CREATE NONCLUSTERED INDEX IX_DanhGia_CuaHang_TrangThai ON dbo.DanhGia (MaCuaHang, TrangThai, NgayTao DESC);
GO
