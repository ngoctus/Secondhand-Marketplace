-- 19_chat.sql
-- Chat 1-1 người mua <-> người bán. Chạy lại an toàn (chỉ tạo khi chưa có, không xóa dữ liệu).
-- Mỗi cặp người dùng có đúng 1 hội thoại: MaNguoiDung1 < MaNguoiDung2.

USE SecondHandMarketplace;
GO

IF OBJECT_ID(N'dbo.HoiThoai', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HoiThoai (
        MaHoiThoai    int           IDENTITY(1,1) NOT NULL,
        MaNguoiDung1  int           NOT NULL,
        MaNguoiDung2  int           NOT NULL,
        NoiDungCuoi   nvarchar(200) NULL,
        NgayTinCuoi   datetime2(3)  NULL,
        NgayTao       datetime2(3)  NOT NULL CONSTRAINT DF_HoiThoai_NgayTao DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_HoiThoai PRIMARY KEY CLUSTERED (MaHoiThoai),
        CONSTRAINT FK_HoiThoai_NguoiDung1 FOREIGN KEY (MaNguoiDung1) REFERENCES dbo.NguoiDung (MaNguoiDung),
        CONSTRAINT FK_HoiThoai_NguoiDung2 FOREIGN KEY (MaNguoiDung2) REFERENCES dbo.NguoiDung (MaNguoiDung),
        CONSTRAINT CK_HoiThoai_ThuTu CHECK (MaNguoiDung1 < MaNguoiDung2),
        CONSTRAINT UQ_HoiThoai_Cap UNIQUE (MaNguoiDung1, MaNguoiDung2)
    );
    CREATE INDEX IX_HoiThoai_NguoiDung2 ON dbo.HoiThoai (MaNguoiDung2);
END
GO

IF OBJECT_ID(N'dbo.TinNhan', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TinNhan (
        MaTinNhan   bigint         IDENTITY(1,1) NOT NULL,
        MaHoiThoai  int            NOT NULL,
        MaNguoiGui  int            NOT NULL,
        NoiDung     nvarchar(1000) NOT NULL,
        DaDoc       bit            NOT NULL CONSTRAINT DF_TinNhan_DaDoc DEFAULT (0),
        NgayGui     datetime2(3)   NOT NULL CONSTRAINT DF_TinNhan_NgayGui DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_TinNhan PRIMARY KEY CLUSTERED (MaTinNhan),
        CONSTRAINT FK_TinNhan_HoiThoai FOREIGN KEY (MaHoiThoai) REFERENCES dbo.HoiThoai (MaHoiThoai),
        CONSTRAINT FK_TinNhan_NguoiDung FOREIGN KEY (MaNguoiGui) REFERENCES dbo.NguoiDung (MaNguoiDung),
        CONSTRAINT CK_TinNhan_NoiDung CHECK (LEN(LTRIM(RTRIM(NoiDung))) > 0)
    );
    CREATE INDEX IX_TinNhan_HoiThoai ON dbo.TinNhan (MaHoiThoai, MaTinNhan DESC) INCLUDE (MaNguoiGui, DaDoc);
    CREATE INDEX IX_TinNhan_NguoiGui ON dbo.TinNhan (MaNguoiGui, NgayGui);
END
GO