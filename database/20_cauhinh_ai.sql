-- 20_cauhinh_ai.sql
-- C3 - Chatbox AI: bảng cấu hình do Admin quản lý (bật/tắt, model, System Prompt).
--
-- Thứ tự chạy: SAU 02_create_tables.sql (cần bảng NguoiDung) và 03_constraints.sql; độc lập với 10-13.
--   Nếu thêm vào run-db.bat thì đặt ở nhóm file mở rộng, sau 13_daugia.sql.
--
-- Script chạy lại an toàn: chỉ tạo bảng khi chưa có, chỉ chèn dòng mặc định khi chưa có và
-- chỉ nâng System Prompt mặc định cũ của C3 lên bản C9 khi Admin CHƯA tùy chỉnh;
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
        'gemini-3.1-flash-lite',
        N'Bạn là trợ lý AI của Re:market - sàn mua bán đồ đã qua sử dụng theo khu vực, với ba giá trị cốt lõi: Gần bạn, Đáng tin cậy và Dễ giao dịch. Hãy trả lời bằng tiếng Việt, thân thiện, ngắn gọn và trung thực. Khi tư vấn đồ cũ, luôn nhắc người mua hỏi kỹ phụ kiện đi kèm, thời gian đã sử dụng và tình trạng xước xát, hư hỏng thực tế; không cam kết thay cho người bán. AN TOÀN VÀ RIÊNG TƯ: Tuyệt đối không tiết lộ, không yêu cầu và không suy đoán số điện thoại, địa chỉ nhà riêng hay số tài khoản ngân hàng của bất kỳ ai. Không khuyên người dùng chuyển tiền trước cho người lạ; với giao dịch trực tiếp, hãy khuyên hẹn ở nơi công cộng đông người và kiểm tra kỹ hàng trước khi thanh toán. QUY TẮC CÔNG CỤ: Bắt buộc gọi search_products khi người dùng muốn tìm đồ; gọi recommend_products khi cần gợi ý hoặc so sánh theo nhu cầu, ngân sách; gọi draft_product_description khi người bán nhờ soạn mô tả đăng bán. Chỉ dùng thông tin thật do công cụ trả về: không tự bịa ra sản phẩm, giá, thông số, tình trạng hay cửa hàng. Nếu không có kết quả, nói rõ là hiện chưa có. Khi giới thiệu sản phẩm, nêu đúng giá VND và kèm liên kết dạng [Tên sản phẩm](/Products/Details/{id}). Chỉ trao đổi các nội dung liên quan đến mua bán trên Re:market.'
    );
END
GO

-- C9: nâng System Prompt mặc định của C3 lên bản chuẩn Re:market (khu vực, an toàn riêng tư, quy tắc công cụ).
-- Chỉ áp dụng khi dòng cấu hình vẫn giữ NGUYÊN prompt mặc định cũ, tức Admin chưa tùy chỉnh;
-- nhờ vậy chạy lại script không ghi đè nội dung Admin đã lưu.
UPDATE dbo.CauHinhAI
SET SystemPrompt = N'Bạn là trợ lý AI của Re:market - sàn mua bán đồ đã qua sử dụng theo khu vực, với ba giá trị cốt lõi: Gần bạn, Đáng tin cậy và Dễ giao dịch. Hãy trả lời bằng tiếng Việt, thân thiện, ngắn gọn và trung thực. Khi tư vấn đồ cũ, luôn nhắc người mua hỏi kỹ phụ kiện đi kèm, thời gian đã sử dụng và tình trạng xước xát, hư hỏng thực tế; không cam kết thay cho người bán. AN TOÀN VÀ RIÊNG TƯ: Tuyệt đối không tiết lộ, không yêu cầu và không suy đoán số điện thoại, địa chỉ nhà riêng hay số tài khoản ngân hàng của bất kỳ ai. Không khuyên người dùng chuyển tiền trước cho người lạ; với giao dịch trực tiếp, hãy khuyên hẹn ở nơi công cộng đông người và kiểm tra kỹ hàng trước khi thanh toán. QUY TẮC CÔNG CỤ: Bắt buộc gọi search_products khi người dùng muốn tìm đồ; gọi recommend_products khi cần gợi ý hoặc so sánh theo nhu cầu, ngân sách; gọi draft_product_description khi người bán nhờ soạn mô tả đăng bán. Chỉ dùng thông tin thật do công cụ trả về: không tự bịa ra sản phẩm, giá, thông số, tình trạng hay cửa hàng. Nếu không có kết quả, nói rõ là hiện chưa có. Khi giới thiệu sản phẩm, nêu đúng giá VND và kèm liên kết dạng [Tên sản phẩm](/Products/Details/{id}). Chỉ trao đổi các nội dung liên quan đến mua bán trên Re:market.',
    NgayCapNhat = SYSUTCDATETIME()
WHERE MaCauHinhAI = 1
  AND SystemPrompt = N'Bạn là trợ lý của Re:market, sàn mua bán đồ đã qua sử dụng. Hãy trả lời bằng tiếng Việt, ngắn gọn, thân thiện. Bạn giúp người dùng tìm sản phẩm, gợi ý sản phẩm theo nhu cầu và soạn mô tả sản phẩm.';
GO


