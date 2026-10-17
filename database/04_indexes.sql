/* TASK 3.5 indexes - SQL Server */
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
IF OBJECT_ID(N'dbo.LuotKiemDuyet', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.LuotKiemDuyet') AND name=N'UX_LuotKiemDuyet_SanPhamDangCho')
        CREATE UNIQUE INDEX UX_LuotKiemDuyet_SanPhamDangCho ON dbo.LuotKiemDuyet(SanPhamId) WHERE QuyetDinh IS NULL;
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.LuotKiemDuyet') AND name=N'IX_LuotKiemDuyet_HangCho')
        CREATE INDEX IX_LuotKiemDuyet_HangCho ON dbo.LuotKiemDuyet(ThoiGianGui) INCLUDE(SanPhamId) WHERE QuyetDinh IS NULL;
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.LuotKiemDuyet') AND name=N'IX_LuotKiemDuyet_SanPham')
        CREATE INDEX IX_LuotKiemDuyet_SanPham ON dbo.LuotKiemDuyet(SanPhamId, ThoiGianGui DESC);
END;
GO
IF OBJECT_ID(N'dbo.PhanTichAI', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.PhanTichAI') AND name=N'IX_PhanTichAI_HangDoi')
    CREATE INDEX IX_PhanTichAI_HangDoi ON dbo.PhanTichAI(TrangThai, NgayTao);
GO
