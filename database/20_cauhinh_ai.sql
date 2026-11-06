-- 20_cauhinh_ai.sql
-- C3 - Chatbox AI: bảng cấu hình do Admin quản lý (bật/tắt, model, System Prompt).
--
-- Thứ tự chạy: SAU 02_create_tables.sql (cần bảng NguoiDung) và 03_constraints.sql; độc lập với 10-13.
--   Nếu thêm vào run-db.bat thì đặt ở nhóm file mở rộng, sau 13_daugia.sql.
--
-- Script chạy lại an toàn: chỉ tạo bảng khi chưa có và chỉ chèn dòng mặc định khi chưa có;
-- KHÔNG xóa bảng và KHÔNG ghi đè cấu hình Admin đã lưu.
-- Chỉ có một dòng cấu hình (MaCauHinhAI = 1). Ứng dụng đọc/ghi dòng này qua AiSettingsService.

USE SecondHandMarketplace;
GO

IF OBJECT_ID(N'dbo.CauHinhAI', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CauHinhAI (
        MaCauHinhAI     int            NOT NULL,
        DangBat         bit            NOT NULL CONSTRAINT DF_CauHinhAI_DangBat      DEFAULT (1),
        TenModel        varchar(100)   NOT NULL,
        SystemPrompt    nvarchar(4000) NOT NULL,
        MaNguoiCapNhat  int            NULL,
        NgayCapNhat     datetime2(3)   NOT NULL CONSTRAINT DF_CauHinhAI_NgayCapNhat  DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_CauHinhAI PRIMARY KEY CLUSTERED (MaCauHinhAI),
        CONSTRAINT CK_CauHinhAI_MotDong       CHECK (MaCauHinhAI = 1),
        CONSTRAINT CK_CauHinhAI_TenModel      CHECK (LEN(LTRIM(RTRIM(TenModel))) > 0),
        CONSTRAINT CK_CauHinhAI_SystemPrompt  CHECK (LEN(LTRIM(RTRIM(SystemPrompt))) > 0),
        CONSTRAINT FK_CauHinhAI_NguoiDung     FOREIGN KEY (MaNguoiCapNhat) REFERENCES dbo.NguoiDung (MaNguoiDung)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.CauHinhAI WHERE MaCauHinhAI = 1)
BEGIN
    INSERT INTO dbo.CauHinhAI (MaCauHinhAI, DangBat, TenModel, SystemPrompt)
    VALUES (
        1,
        1,
        'gemini-3.6-flash',
        N'Bạn là trợ lý của Re:market, sàn mua bán đồ đã qua sử dụng. Hãy trả lời bằng tiếng Việt, ngắn gọn, thân thiện. Bạn giúp người dùng tìm sản phẩm, gợi ý sản phẩm theo nhu cầu và soạn mô tả sản phẩm.'
    );
END
GO
