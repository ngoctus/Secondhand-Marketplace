/* =====================================================================
   AI-02 — MIGRATION NÂNG CẤP CHO DATABASE ĐANG HOẠT ĐỘNG (IDEMPOTENT)
   Mở rộng dbo.CauHinhAI để Admin cấu hình riêng:
   1. Cụm AI Hỗ trợ người mua & người bán:
      - DangBat (Chatbox AI - /api/ai/chat)
      - TroLyBanHangDangBat (Trợ lý bán hàng - /api/ai/suggest-price, /api/ai/draft-description)
      - TenModel, SystemPrompt
   2. Cụm AI Kiểm duyệt sản phẩm Tầng 2 (chạy nội bộ):
      - KiemDuyetDangBat
      - KiemDuyetTenModel
      - KiemDuyetSoAnhToiDa (0..4)
   An toàn khi chạy lại nhiều lần, KHÔNG xóa dữ liệu hiện có.
   ===================================================================== */

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ARITHABORT ON;
GO

USE SecondHandMarketplace;
GO

IF OBJECT_ID(N'dbo.CauHinhAI', N'U') IS NULL
BEGIN
    RAISERROR(N'Bảng dbo.CauHinhAI chưa tồn tại. Vui lòng chạy database/02_create_tables.sql trước.', 16, 1);
    RETURN;
END;
GO

IF COL_LENGTH(N'dbo.CauHinhAI', N'TroLyBanHangDangBat') IS NULL
BEGIN
    ALTER TABLE dbo.CauHinhAI
        ADD TroLyBanHangDangBat bit NOT NULL CONSTRAINT DF_CauHinhAI_TroLyBanHangDangBat DEFAULT (1);
END;
GO

IF COL_LENGTH(N'dbo.CauHinhAI', N'KiemDuyetDangBat') IS NULL
BEGIN
    ALTER TABLE dbo.CauHinhAI
        ADD KiemDuyetDangBat bit NOT NULL CONSTRAINT DF_CauHinhAI_KiemDuyetDangBat DEFAULT (1);
END;
GO

IF COL_LENGTH(N'dbo.CauHinhAI', N'KiemDuyetTenModel') IS NULL
BEGIN
    ALTER TABLE dbo.CauHinhAI
        ADD KiemDuyetTenModel varchar(100) NOT NULL CONSTRAINT DF_CauHinhAI_KiemDuyetTenModel DEFAULT ('gemini-3.5-flash-lite');
END;
GO

IF COL_LENGTH(N'dbo.CauHinhAI', N'KiemDuyetSoAnhToiDa') IS NULL
BEGIN
    ALTER TABLE dbo.CauHinhAI
        ADD KiemDuyetSoAnhToiDa int NOT NULL CONSTRAINT DF_CauHinhAI_KiemDuyetSoAnhToiDa DEFAULT (2);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_CauHinhAI_KiemDuyetTenModel' AND parent_object_id = OBJECT_ID(N'dbo.CauHinhAI'))
BEGIN
    ALTER TABLE dbo.CauHinhAI
        ADD CONSTRAINT CK_CauHinhAI_KiemDuyetTenModel CHECK (LEN(LTRIM(RTRIM(KiemDuyetTenModel))) > 0);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_CauHinhAI_KiemDuyetSoAnhToiDa' AND parent_object_id = OBJECT_ID(N'dbo.CauHinhAI'))
BEGIN
    ALTER TABLE dbo.CauHinhAI
        ADD CONSTRAINT CK_CauHinhAI_KiemDuyetSoAnhToiDa CHECK (KiemDuyetSoAnhToiDa BETWEEN 0 AND 4);
END;
GO
