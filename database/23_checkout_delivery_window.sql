USE SecondHandMarketplace;
GO

IF COL_LENGTH('dbo.DonHang', 'KhungGioNhan') IS NULL
BEGIN
    ALTER TABLE dbo.DonHang ADD KhungGioNhan nvarchar(200) NULL;
END
GO
