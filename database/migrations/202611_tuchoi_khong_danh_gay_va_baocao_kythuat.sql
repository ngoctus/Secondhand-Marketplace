SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ARITHABORT ON;
GO

USE SecondHandMarketplace;
GO

/* =====================================================================
   1. Hỗ trợ Admin từ chối sản phẩm KHÔNG đánh gậy (GhiNhanViPham = 0)
   ===================================================================== */
IF COL_LENGTH(N'dbo.LuotKiemDuyet', N'GhiNhanViPham') IS NULL
BEGIN
    ALTER TABLE dbo.LuotKiemDuyet
        ADD GhiNhanViPham bit NOT NULL
            CONSTRAINT DF_LuotKiemDuyet_GhiNhanViPham DEFAULT (1);
END
GO

CREATE OR ALTER TRIGGER dbo.trg_LuotKiemDuyet_GhiViPhamLow ON dbo.LuotKiemDuyet AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT UPDATE(QuyetDinh) RETURN;

    INSERT dbo.LichSuViPham (MaNguoiBan, MaSanPham, MaLuotKiemDuyet, MucDo, NguonGhiNhan, ChiTietTuKhoa, MaQuanTriVien)
    SELECT c.MaNguoiDung, i.MaSanPham, i.MaLuotKiemDuyet, 'LOW', 'ADMIN_TU_CHOI_LOW',
           CASE WHEN ISJSON(p.DauHieu) = 1 THEN p.DauHieu ELSE N'[]' END,
           i.MaQuanTriVien
    FROM inserted i
    JOIN deleted d ON d.MaLuotKiemDuyet = i.MaLuotKiemDuyet
    LEFT JOIN dbo.PhanTichAI p ON p.MaLuotKiemDuyet = i.MaLuotKiemDuyet
    JOIN dbo.SanPham s ON s.MaSanPham = i.MaSanPham
    JOIN dbo.CuaHang c ON c.MaCuaHang = s.MaCuaHang
    WHERE i.QuyetDinh = 'REJECTED' AND d.QuyetDinh IS NULL
      AND i.QuyetDinhTuDong = 0 AND i.MaQuanTriVien IS NOT NULL
      AND i.GhiNhanViPham = 1
      AND NOT EXISTS (SELECT 1 FROM dbo.LichSuViPham v WHERE v.MaLuotKiemDuyet = i.MaLuotKiemDuyet);
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_QuyetDinhKiemDuyet
    @MaSanPham      int,
    @QuanTriVienId  int,
    @QuyetDinh      varchar(10),
    @LyDo           nvarchar(500) = NULL,
    @GhiNhanViPham  bit = 1
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @LuotId int, @TrangThaiMoi varchar(20), @HanhDong varchar(50);

    IF NOT EXISTS (SELECT 1 FROM NguoiDung WHERE MaNguoiDung = @QuanTriVienId AND VaiTro = 'ADMIN' AND TrangThai = 'ACTIVE')
        THROW 50020, N'Chỉ quản trị viên mới được kiểm duyệt sản phẩm.', 1;
    IF @QuyetDinh NOT IN ('APPROVED','REJECTED')
        THROW 50023, N'Quyết định kiểm duyệt không hợp lệ.', 1;
    IF @QuyetDinh = 'REJECTED' AND LEN(LTRIM(RTRIM(ISNULL(@LyDo, N'')))) < 10
        THROW 50021, N'Lý do từ chối bắt buộc và phải có ít nhất 10 ký tự.', 1;

    BEGIN TRANSACTION;

    SELECT @LuotId = l.MaLuotKiemDuyet
    FROM LuotKiemDuyet l WITH (UPDLOCK, ROWLOCK)
    JOIN SanPham sp ON sp.MaSanPham = l.MaSanPham
    WHERE l.MaSanPham = @MaSanPham AND l.QuyetDinh IS NULL
      AND sp.TrangThai = 'PENDING_REVIEW' AND sp.DaXoa = 0;

    IF @LuotId IS NULL
        THROW 50022, N'Sản phẩm không còn ở trạng thái chờ duyệt.', 1;

    SET @TrangThaiMoi = CASE @QuyetDinh WHEN 'APPROVED' THEN 'ACTIVE' ELSE 'REJECTED' END;

    UPDATE LuotKiemDuyet
    SET QuyetDinh = @QuyetDinh,
        LyDo = CASE WHEN @QuyetDinh = 'REJECTED' THEN @LyDo END,
        GhiNhanViPham = CASE WHEN @QuyetDinh = 'REJECTED' THEN ISNULL(@GhiNhanViPham, 1) ELSE 0 END,
        MaQuanTriVien = @QuanTriVienId,
        ThoiGianQuyetDinh = SYSUTCDATETIME()
    WHERE MaLuotKiemDuyet = @LuotId;

    EXEC sp_ChuyenTrangThaiSanPham @MaSanPham = @MaSanPham, @TrangThaiMoi = @TrangThaiMoi,
                                   @LoaiNguoiThucHien = 'ADMIN', @NguoiThucHienId = @QuanTriVienId,
                                   @LyDo = @LyDo;

    UPDATE PhanTichAI
    SET QuanTriVienLamTheo = CASE
            WHEN DeXuat = 'NEEDS_REVIEW' THEN NULL
            WHEN (DeXuat = 'APPROVE' AND @QuyetDinh = 'APPROVED')
              OR (DeXuat = 'REJECT'  AND @QuyetDinh = 'REJECTED') THEN 1
            ELSE 0 END
    WHERE MaLuotKiemDuyet = @LuotId AND TrangThai = 'DONE';

    SET @HanhDong = CASE
        WHEN @QuyetDinh = 'APPROVED' THEN 'APPROVE_PRODUCT'
        WHEN ISNULL(@GhiNhanViPham, 1) = 0 THEN 'REJECT_PRODUCT_NO_STRIKE'
        ELSE 'REJECT_PRODUCT'
    END;

    EXEC sp_GhiNhatKy @MaNguoiDung = @QuanTriVienId,
                      @HanhDong = @HanhDong,
                      @LoaiDoiTuong = 'SanPham', @DoiTuongId = @MaSanPham, @LyDo = @LyDo;

    COMMIT TRANSACTION;

    SELECT @LuotId AS LuotKiemDuyetId, @TrangThaiMoi AS TrangThaiMoi;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_TuChoiSanPham
    @MaSanPham      int,
    @QuanTriVienId  int,
    @LyDo           nvarchar(500),
    @GhiNhanViPham  bit = 1
AS
BEGIN
    SET NOCOUNT ON;
    EXEC sp_QuyetDinhKiemDuyet @MaSanPham = @MaSanPham, @QuanTriVienId = @QuanTriVienId,
                               @QuyetDinh = 'REJECTED', @LyDo = @LyDo, @GhiNhanViPham = @GhiNhanViPham;
END
GO

/* =====================================================================
   2. Hỗ trợ Báo cáo kỹ thuật (TECHNICAL) trong bảng BaoCao
   ===================================================================== */
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_BaoCao_LoaiDoiTuong' AND parent_object_id = OBJECT_ID(N'dbo.BaoCao'))
BEGIN
    ALTER TABLE dbo.BaoCao DROP CONSTRAINT CK_BaoCao_LoaiDoiTuong;
END
ALTER TABLE dbo.BaoCao ADD CONSTRAINT CK_BaoCao_LoaiDoiTuong CHECK (LoaiDoiTuong IN ('PRODUCT', 'USER', 'SHOP', 'TECHNICAL'));
GO

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_BaoCao_ChongTrung' AND object_id = OBJECT_ID(N'dbo.BaoCao'))
BEGIN
    DROP INDEX UQ_BaoCao_ChongTrung ON dbo.BaoCao;
END
CREATE UNIQUE NONCLUSTERED INDEX UQ_BaoCao_ChongTrung
    ON dbo.BaoCao(MaNguoiBaoCao, LoaiDoiTuong, MaDoiTuong)
    WHERE TrangThai = 'PENDING' AND LoaiDoiTuong <> 'TECHNICAL';
GO
