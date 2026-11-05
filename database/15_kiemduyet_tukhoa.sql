SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ARITHABORT ON;
GO

USE SecondHandMarketplace;
GO

/* =====================================================================
   C2 - TỪ KHÓA CẤM HIGH/LOW + LỊCH SỬ VI PHẠM  (người C)
   File CHỈ THÊM (additive), chạy lại nhiều lần không lỗi, không xóa dữ liệu.
   Thứ tự chạy: sau 01..09 (hoặc sau 13). Xem docs/c2-moderation-handoff.md.
   Không sửa 02/03/06/07/08/09 và không đụng 11_danhgia.sql (C4).
   ===================================================================== */

-- ---------------------------------------------------------------------
-- 1. TuKhoaCam.MucDo (LOW/HIGH). Dòng cũ nhận LOW (không tự từ chối);
--    Admin đổi sang HIGH ở màn hình quản lý từ khóa.
--    sp_ThemTuKhoaCam không đổi: cột có DEFAULT 'LOW', C# cập nhật MucDo ngay sau khi thêm.
-- ---------------------------------------------------------------------
IF COL_LENGTH('dbo.TuKhoaCam', 'MucDo') IS NULL
    ALTER TABLE dbo.TuKhoaCam
        ADD MucDo varchar(4) NOT NULL CONSTRAINT DF_TuKhoaCam_MucDo DEFAULT ('LOW');
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_TuKhoaCam_MucDo' AND parent_object_id = OBJECT_ID('dbo.TuKhoaCam'))
    ALTER TABLE dbo.TuKhoaCam
        ADD CONSTRAINT CK_TuKhoaCam_MucDo CHECK (MucDo IN ('LOW', 'HIGH'));
GO

-- ---------------------------------------------------------------------
-- 2. PhanTichAI.MucDoTuKhoa: mức cao nhất của lần quét từ khóa gần nhất (NONE | LOW | HIGH; NULL = chưa quét).
--    PhanTichAI đã UNIQUE theo MaLuotKiemDuyet => mỗi lượt đúng 1 dòng, phân tích lại cập nhật dòng đó.
--    (Nghiệp vụ LOW cuối cùng: Admin duyệt = không vi phạm, Admin từ chối = 1 vi phạm => KHÔNG cần cột quyết định riêng.)
-- ---------------------------------------------------------------------
IF COL_LENGTH('dbo.PhanTichAI', 'MucDoTuKhoa') IS NULL
    ALTER TABLE dbo.PhanTichAI ADD MucDoTuKhoa varchar(4) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_PhanTichAI_MucDoTuKhoa' AND parent_object_id = OBJECT_ID('dbo.PhanTichAI'))
    ALTER TABLE dbo.PhanTichAI
        ADD CONSTRAINT CK_PhanTichAI_MucDoTuKhoa CHECK (MucDoTuKhoa IS NULL OR MucDoTuKhoa IN ('NONE','LOW','HIGH'));
GO

-- Dọn bản nháp cũ của file này (chỉ khi ai đó đã chạy): 3 cột quyết định LOW kiểu "xác nhận/bỏ qua" nay không dùng nữa.
IF COL_LENGTH('dbo.PhanTichAI', 'QuyetDinhTuKhoaLow') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_PhanTichAI_QuyetDinhTuKhoaDayDu')
        ALTER TABLE dbo.PhanTichAI DROP CONSTRAINT CK_PhanTichAI_QuyetDinhTuKhoaDayDu;
    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_PhanTichAI_QuyetDinhTuKhoa')
        ALTER TABLE dbo.PhanTichAI DROP CONSTRAINT CK_PhanTichAI_QuyetDinhTuKhoa;
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_PhanTichAI_QuanTriVienTuKhoa')
        ALTER TABLE dbo.PhanTichAI DROP CONSTRAINT FK_PhanTichAI_QuanTriVienTuKhoa;
    ALTER TABLE dbo.PhanTichAI DROP COLUMN QuyetDinhTuKhoaLow, MaQuanTriVienTuKhoa, ThoiGianQuyetDinhTuKhoa;
END
GO

-- ---------------------------------------------------------------------
-- 3. LichSuViPham: MỖI DÒNG = 1 VI PHẠM ĐƯỢC TÍNH VÀO BỘ ĐẾM CỦA NGƯỜI BÁN.
--    Bộ đếm = COUNT(*) theo MaNguoiBan. KHÔNG có lệnh xóa/sửa trong code; mở khóa tài khoản không đụng bảng này.
--    UNIQUE (MaLuotKiemDuyet): một lượt kiểm duyệt tối đa một vi phạm, bất kể bao nhiêu từ khóa,
--    phân tích lại bao nhiêu lần, Admin bấm xác nhận bao nhiêu lần hay hai request chạy song song.
--    NguonGhiNhan: AUTO_HIGH (hệ thống, MaQuanTriVien NULL) | ADMIN_TU_CHOI_LOW (Admin từ chối sản phẩm có LOW;
--    dòng này do trigger của file 17 ghi trong CÙNG transaction với quyết định từ chối).
-- ---------------------------------------------------------------------
IF OBJECT_ID('dbo.LichSuViPham', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.LichSuViPham (
        MaViPham         int           IDENTITY(1,1) NOT NULL,
        MaNguoiBan       int           NOT NULL,
        MaSanPham        int           NOT NULL,
        MaLuotKiemDuyet  int           NOT NULL,
        MucDo            varchar(4)    NOT NULL,
        NguonGhiNhan     varchar(20)   NOT NULL,
        ChiTietTuKhoa    nvarchar(max) NOT NULL,
        MaQuanTriVien    int           NULL,
        ThoiGian         datetime2(3)  NOT NULL CONSTRAINT DF_LichSuViPham_ThoiGian DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_LichSuViPham PRIMARY KEY CLUSTERED (MaViPham),
        CONSTRAINT UQ_LichSuViPham_LuotKiemDuyet UNIQUE (MaLuotKiemDuyet),
        CONSTRAINT FK_LichSuViPham_NguoiBan    FOREIGN KEY (MaNguoiBan)      REFERENCES dbo.NguoiDung (MaNguoiDung),
        CONSTRAINT FK_LichSuViPham_SanPham     FOREIGN KEY (MaSanPham)       REFERENCES dbo.SanPham (MaSanPham),
        CONSTRAINT FK_LichSuViPham_Luot        FOREIGN KEY (MaLuotKiemDuyet) REFERENCES dbo.LuotKiemDuyet (MaLuotKiemDuyet),
        CONSTRAINT FK_LichSuViPham_QuanTriVien FOREIGN KEY (MaQuanTriVien)   REFERENCES dbo.NguoiDung (MaNguoiDung),
        CONSTRAINT CK_LichSuViPham_MucDo       CHECK (MucDo IN ('LOW','HIGH')),
        CONSTRAINT CK_LichSuViPham_Chi_Tiet    CHECK (ISJSON(ChiTietTuKhoa) = 1),
        CONSTRAINT CK_LichSuViPham_Nguon       CHECK (
              (NguonGhiNhan = 'AUTO_HIGH'          AND MucDo = 'HIGH' AND MaQuanTriVien IS NULL)
           OR (NguonGhiNhan = 'ADMIN_TU_CHOI_LOW'  AND MucDo = 'LOW'  AND MaQuanTriVien IS NOT NULL))
    );
    CREATE NONCLUSTERED INDEX IX_LichSuViPham_NguoiBan ON dbo.LichSuViPham (MaNguoiBan, ThoiGian DESC);
END
GO

-- Nâng cấp bảng tạo bởi bản nháp cũ (nguồn ADMIN_XAC_NHAN_LOW -> ADMIN_TU_CHOI_LOW). Không làm gì nếu bảng đã đúng.
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_LichSuViPham_Nguon' AND definition LIKE '%ADMIN_XAC_NHAN_LOW%')
BEGIN
    ALTER TABLE dbo.LichSuViPham DROP CONSTRAINT CK_LichSuViPham_Nguon;
    UPDATE dbo.LichSuViPham SET NguonGhiNhan = 'ADMIN_TU_CHOI_LOW' WHERE NguonGhiNhan = 'ADMIN_XAC_NHAN_LOW';
    ALTER TABLE dbo.LichSuViPham ADD CONSTRAINT CK_LichSuViPham_Nguon CHECK (
          (NguonGhiNhan = 'AUTO_HIGH'         AND MucDo = 'HIGH' AND MaQuanTriVien IS NULL)
       OR (NguonGhiNhan = 'ADMIN_TU_CHOI_LOW' AND MucDo = 'LOW'  AND MaQuanTriVien IS NOT NULL));
END
GO

-- ---------------------------------------------------------------------
-- 4. CHẶN DUYỆT Ở TẦNG DATABASE (không phụ thuộc C# hay procedure nào gọi).
--    Mọi đường duyệt (sp_QuyetDinhKiemDuyet / sp_DuyetSanPham) đều UPDATE LuotKiemDuyet.QuyetDinh = 'APPROVED';
--    trigger này THROW (rollback cả transaction) khi:
--      - 50210: chưa có phân tích, hoặc phân tích QUEUED/FAILED, hoặc chưa quét từ khóa (MucDoTuKhoa NULL);
--      - 50211: có HIGH, hoặc lượt này đã bị ghi nhận vi phạm.
--    LOW: Admin ĐƯỢC duyệt (không vi phạm). Từ chối (REJECTED) không bị chặn. Không đụng trg_LuotKiemDuyet_KiemTra có sẵn.
-- ---------------------------------------------------------------------
CREATE OR ALTER TRIGGER dbo.trg_LuotKiemDuyet_ChanDuyetViPham ON dbo.LuotKiemDuyet AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT UPDATE(QuyetDinh) RETURN;

    DECLARE @Duyet TABLE (MaLuotKiemDuyet int PRIMARY KEY);
    INSERT @Duyet (MaLuotKiemDuyet)
    SELECT i.MaLuotKiemDuyet
    FROM inserted i
    JOIN deleted d ON d.MaLuotKiemDuyet = i.MaLuotKiemDuyet
    WHERE i.QuyetDinh = 'APPROVED' AND d.QuyetDinh IS NULL;

    IF NOT EXISTS (SELECT 1 FROM @Duyet) RETURN;

    IF EXISTS (SELECT 1
               FROM @Duyet x
               LEFT JOIN dbo.PhanTichAI p ON p.MaLuotKiemDuyet = x.MaLuotKiemDuyet
               WHERE p.MaPhanTichAI IS NULL OR p.TrangThai <> 'DONE' OR p.MucDoTuKhoa IS NULL)
        THROW 50210, N'Không thể duyệt: sản phẩm chưa được phân tích từ khóa xong (QUEUED/FAILED hoặc chưa quét).', 1;

    IF EXISTS (SELECT 1
               FROM @Duyet x
               JOIN dbo.PhanTichAI p ON p.MaLuotKiemDuyet = x.MaLuotKiemDuyet
               WHERE p.MucDoTuKhoa = 'HIGH'
                  OR EXISTS (SELECT 1 FROM dbo.LichSuViPham v WHERE v.MaLuotKiemDuyet = x.MaLuotKiemDuyet))
        THROW 50211, N'Không thể duyệt: sản phẩm có từ khóa cấm mức HIGH hoặc đã bị ghi nhận vi phạm.', 1;
END
GO

PRINT N'C2: đã áp dụng 14_kiemduyet_tukhoa_c2.sql.';
GO
