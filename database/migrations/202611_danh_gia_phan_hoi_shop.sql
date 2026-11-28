/* =====================================================================
   MIGRATION: Bổ sung cột phản hồi của Shop cho bảng DanhGia (Idempotent)
   ===================================================================== */

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF COL_LENGTH(N'dbo.DanhGia', N'PhanHoiShop') IS NULL
BEGIN
    ALTER TABLE dbo.DanhGia ADD PhanHoiShop nvarchar(1000) NULL;
    PRINT N'Đã thêm cột PhanHoiShop vào bảng DanhGia.';
END;
GO

IF COL_LENGTH(N'dbo.DanhGia', N'NgayPhanHoi') IS NULL
BEGIN
    ALTER TABLE dbo.DanhGia ADD NgayPhanHoi datetime2(3) NULL;
    PRINT N'Đã thêm cột NgayPhanHoi vào bảng DanhGia.';
END;
GO
