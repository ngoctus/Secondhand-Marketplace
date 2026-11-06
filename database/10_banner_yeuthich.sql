SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ARITHABORT ON;
GO

USE SecondHandMarketplace;
GO

-- =====================================================================
-- B4: YeuThich — sản phẩm yêu thích của người mua
-- Chạy lại nhiều lần không mất dữ liệu (chỉ tạo khi chưa tồn tại).
-- =====================================================================
IF OBJECT_ID(N'dbo.YeuThich', N'U') IS NULL
BEGIN
    CREATE TABLE YeuThich (
        MaYeuThich      int           IDENTITY(1,1) NOT NULL,
        MaNguoiDung     int           NOT NULL,
        MaSanPham       int           NOT NULL,
        NgayTao         datetime2(3)  NOT NULL CONSTRAINT DF_YeuThich_NgayTao DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_YeuThich PRIMARY KEY CLUSTERED (MaYeuThich),
        CONSTRAINT FK_YeuThich_NguoiDung FOREIGN KEY (MaNguoiDung) REFERENCES NguoiDung (MaNguoiDung),
        CONSTRAINT FK_YeuThich_SanPham   FOREIGN KEY (MaSanPham)   REFERENCES SanPham (MaSanPham),
        -- Mỗi người chỉ yêu thích một sản phẩm đúng 1 lần
        CONSTRAINT UX_YeuThich_NguoiSanPham UNIQUE (MaNguoiDung, MaSanPham)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_YeuThich_NguoiDung_NgayTao' AND object_id = OBJECT_ID(N'dbo.YeuThich'))
    CREATE NONCLUSTERED INDEX IX_YeuThich_NguoiDung_NgayTao ON YeuThich (MaNguoiDung, NgayTao DESC);
GO

-- =====================================================================
-- B5: Banner — banner/ưu đãi trên trang chủ, Admin quản lý
-- =====================================================================
IF OBJECT_ID(N'dbo.Banner', N'U') IS NULL
BEGIN
    CREATE TABLE Banner (
        MaBanner        int           IDENTITY(1,1) NOT NULL,
        TieuDe          nvarchar(200) NOT NULL,
        MoTa            nvarchar(500) NULL,
        DuongDanAnh     nvarchar(500) NOT NULL,
        LienKet         nvarchar(500) NULL,
        ThuTuHienThi    int           NOT NULL CONSTRAINT DF_Banner_ThuTu DEFAULT (1),
        DangHienThi     bit           NOT NULL CONSTRAINT DF_Banner_DangHienThi DEFAULT (1),
        BatDauTu        datetime2(3)  NULL,
        KetThucLuc      datetime2(3)  NULL,
        NgayTao         datetime2(3)  NOT NULL CONSTRAINT DF_Banner_NgayTao DEFAULT (SYSUTCDATETIME()),
        NgayCapNhat     datetime2(3)  NOT NULL CONSTRAINT DF_Banner_NgayCapNhat DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_Banner PRIMARY KEY CLUSTERED (MaBanner),
        CONSTRAINT CK_Banner_ThuTu CHECK (ThuTuHienThi >= 1),
        CONSTRAINT CK_Banner_ThoiGian CHECK (KetThucLuc IS NULL OR BatDauTu IS NULL OR KetThucLuc > BatDauTu)
    );
END
GO

-- Banner mẫu (chỉ chèn khi bảng trống)
IF NOT EXISTS (SELECT 1 FROM Banner)
INSERT Banner (TieuDe, MoTa, DuongDanAnh, LienKet, ThuTuHienThi) VALUES
 (N'Tuần lễ đồ cũ — giá tốt, thêm vui', N'Mua bán đồ đã qua sử dụng, mọi tin đều được kiểm duyệt.', N'/uploads/banners/banner-1.svg', N'/Products', 1),
 (N'Chợ đồ cũ cho người Việt', N'Đăng bán trong 2 phút, giao dịch an toàn với xác nhận hai chiều.', N'/uploads/banners/banner-2.svg', N'/User/Products/Create', 2),
 (N'Thanh lý cuối tuần', N'Hàng điện tử, sách, gia dụng đã qua sử dụng giá tốt mỗi tuần.', N'/uploads/banners/banner-3.svg', N'/Products?MaxPrice=300000', 3);
GO
