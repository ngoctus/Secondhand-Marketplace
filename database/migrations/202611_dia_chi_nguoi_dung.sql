-- Migration: Sổ địa chỉ người dùng (địa chỉ mặc định + địa chỉ dự phòng)
IF OBJECT_ID(N'dbo.DiaChiNguoiDung', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DiaChiNguoiDung (
        MaDiaChi        int           IDENTITY(1,1) NOT NULL CONSTRAINT PK_DiaChiNguoiDung PRIMARY KEY,
        MaNguoiDung     int           NOT NULL CONSTRAINT FK_DiaChiNguoiDung_NguoiDung FOREIGN KEY REFERENCES dbo.NguoiDung(MaNguoiDung),
        NhanDiaChi      nvarchar(60)  NULL,
        TenNguoiNhan    nvarchar(100) NOT NULL,
        SoDienThoaiNhan varchar(15)   NOT NULL,
        MaTinhThanh     int           NULL,
        MaPhuongXa      int           NULL,
        TenTinhThanh    nvarchar(100) NULL,
        TenPhuongXa     nvarchar(100) NULL,
        DiaChiChiTiet   nvarchar(300) NOT NULL,
        LaMacDinh       bit           NOT NULL CONSTRAINT DF_DiaChiNguoiDung_LaMacDinh DEFAULT (0),
        NgayTao         datetime2(3)  NOT NULL CONSTRAINT DF_DiaChiNguoiDung_NgayTao DEFAULT (SYSUTCDATETIME())
    );

    CREATE INDEX IX_DiaChiNguoiDung_NguoiDung ON dbo.DiaChiNguoiDung(MaNguoiDung, LaMacDinh DESC, MaDiaChi DESC);
END
GO
