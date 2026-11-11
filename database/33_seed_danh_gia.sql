-- =============================================================================
-- 33_seed_danh_gia.sql  (Task C7 - Người C)
-- Seed dữ liệu đánh giá người bán cho bộ dữ liệu demo Re:Market.
--
-- Phụ thuộc (chạy sau):
--   11_danhgia.sql            -> bảng dbo.DanhGia, view dbo.vw_DiemCuaHang
--   09_seed_data.sql          -> đơn COMPLETED DH20261001-000005 (shop binh.seller)
--   30_seed_dia_chi_shop.sql  -> shop của các người bán demo
--   32_seed_don_hang.sql      -> 52 đơn COMPLETED DH202611-000001 .. DH202611-000052
--
-- Kết quả mong đợi: 47 đánh giá (45 VISIBLE, 2 HIDDEN) trải trên 13 shop.
--   - Shop "Bình Vintage" (binh.seller@demo.local): 5 đánh giá VISIBLE, toàn 1-2 sao
--     -> điểm trung bình 1.60 < 3.0 (đạt yêu cầu Task C4 / mục 3.3).
--   - Shop "An Store" (an.seller@demo.local): 4 đánh giá 5 sao -> điểm trung bình 5.00.
--   - 6 đơn COMPLETED còn lại KHÔNG đánh giá (DH202611-000003/000004/000019/
--     000020/000037/000038) để demo người mua tự viết đánh giá.
--
-- Nguyên tắc an toàn dữ liệu:
--   - MaDonHang, MaNguoiMua, MaCuaHang lấy TRỰC TIẾP từ dbo.DonHang theo MaDon,
--     không gán cứng ID IDENTITY (an toàn khi DB được tạo lại ở máy khác).
--   - Chỉ seed cho đơn TrangThai = 'COMPLETED'.
--   - NgayTao của đánh giá luôn sau DonHang.NgayTao và không vượt quá thời điểm chạy.
--   - Idempotent: NOT EXISTS (MaDonHang) + ràng buộc UNIQUE UQ_DanhGia_DonHang.
-- =============================================================================
USE SecondHandMarketplace;
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ARITHABORT ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

-- -----------------------------------------------------------------------------
-- 1. Bảng tạm mô tả đánh giá theo MÃ ĐƠN (không dùng ID tự tăng)
--    GioSauDon = số giờ sau khi tạo đơn (đơn hoàn tất ở mốc +28h trong 32_seed_don_hang).
-- -----------------------------------------------------------------------------
DECLARE @SeedDanhGia TABLE (
    MaDon     varchar(30)    NOT NULL PRIMARY KEY,
    SoSao     tinyint        NOT NULL,
    NoiDung   nvarchar(1000) NULL,
    TrangThai varchar(10)    NOT NULL,
    GioSauDon smallint       NOT NULL
);

INSERT INTO @SeedDanhGia (MaDon, SoSao, NoiDung, TrangThai, GioSauDon) VALUES
-- ===== An Store - an.seller@demo.local (shop điểm cao: 4 đánh giá, TB 5.00) =====
('DH202611-000001', 5, N'Sách đúng mô tả, bìa cứng còn đẹp và không quăn mép. Shop giao nhanh trong buổi sáng, rất đáng tiền.',
    'VISIBLE', 32),
('DH202611-000014', 5, N'Nồi nấu cơm chín đều, lòng nồi sạch không xước. Shop còn dặn kỹ cách vệ sinh men chống dính, rất chu đáo.',
    'VISIBLE', 34),
('DH202611-000027', 5, N'Máy tính hoạt động mượt, đủ nắp và phím bấm nhạy. Shop đóng gói cẩn thận, giao đúng hẹn.',
    'VISIBLE', 30),
('DH202611-000040', 5, N'Balo chắc chắn, ngăn laptop đệm dày đúng như mô tả. Shop thân thiện, giao hàng nhanh, mình sẽ ủng hộ tiếp.',
    'VISIBLE', 36),

-- ===== Bình Vintage - binh.seller@demo.local (shop < 3 sao: 5 đánh giá, TB 1.60) =====
('DH20261001-000005', 2, N'Bàn ủi nhận được bị cháy đế, ủi quần áo bị dính vệt vàng. Ảnh đăng khác hẳn tình trạng lúc giao, khá thất vọng.',
    'VISIBLE', 34),
('DH202611-000002', 1, N'Sách bị rách vài trang và viết chi chít bút mực, khác xa mô tả là ít sử dụng. Mình không hài lòng chút nào.',
    'VISIBLE', 31),
('DH202611-000015', 2, N'Loa hát bị rè khi mở âm lượng lớn, pin tụt rất nhanh và một micro không kết nối được. Đóng gói cũng sơ sài.',
    'VISIBLE', 38),
('DH202611-000028', 1, N'Đèn nhận bị nứt chân đế, bật lên chập chờn lúc sáng lúc tắt. Mình nhắn tin nhiều lần mà shop không phản hồi. Rất tệ.',
    'VISIBLE', 33),
('DH202611-000041', 2, N'Đồng hồ chạy được nhưng mặt kính trầy nhiều, dây đeo ố vàng, không giống ảnh. Mong shop kiểm tra kỹ hàng trước khi giao.',
    'VISIBLE', 35),

-- ===== Long Tech Cũ - nguoiban01@demo.local =====
('DH202611-000016', 5, N'Sách còn mới, không quăn mép, được bọc nilon cẩn thận. Giao nhanh hơn dự kiến, shop rất uy tín.',
    'VISIBLE', 32),
('DH202611-000029', 4, N'Nồi chạy êm, cơm chín đều. Chỉ hơi trầy xước ở nắp nhưng không ảnh hưởng sử dụng, shop có báo trước.',
    'VISIBLE', 40),
('DH202611-000042', 4, NULL,
    'VISIBLE', 36),

-- ===== Hà Vintage Closet - nguoiban02@demo.local =====
('DH202611-000017', 4, N'Sách đẹp hơn mong đợi, chỉ có vài ghi chú bút chì mờ. Shop giao đúng hẹn và đóng gói chắc chắn.',
    'VISIBLE', 30),
('DH202611-000030', 3, N'Loa dùng tạm ổn nhưng âm thanh hơi đục khi mở lớn. Hàng đúng mô tả nên vẫn giữ 3 sao, shop phản hồi hơi chậm.',
    'VISIBLE', 44),
('DH202611-000043', 4, N'Đèn sáng dịu, khớp gập chắc chắn và đúng mẫu. Hàng về trễ hơn hẹn một ngày nên mình trừ 1 sao.',
    'VISIBLE', 35),

-- ===== Huy Gia Dụng - nguoiban03@demo.local =====
('DH202611-000005', 5, N'Chuột bấm êm, im lặng đúng như mô tả, kèm pin và đầu thu USB. Shop tư vấn nhiệt tình, giao hàng nhanh.',
    'VISIBLE', 31),
('DH202611-000018', 4, N'Áo đẹp, vải thoáng mát, form chuẩn size M. Giặt xong không xù lông, shop gói hàng gọn gàng.',
    'VISIBLE', 37),
('DH202611-000031', 3, N'Sách đúng môn mình cần nhưng bìa hơi quăn và có vài trang bị gập góc. Shop nên mô tả kỹ tình trạng hơn.',
    'VISIBLE', 39),
('DH202611-000044', 3, N'Shop này bán đắt hơn chỗ khác. Mình cũng bán nồi cơm y hệt giá rẻ hơn 200k, ai cần inbox zalo 09xx.xxx.xxx nhé.',
    'HIDDEN', 41),

-- ===== Mai Books - nguoiban04@demo.local =====
('DH202611-000006', 5, N'Bàn phím gõ nảy, led sáng đều và không kẹt phím. Shop đóng gói kỹ, giao đúng giờ, rất đáng mua.',
    'VISIBLE', 33),
('DH202611-000032', 4, NULL,
    'VISIBLE', 36),
('DH202611-000045', 3, N'Loa hát ổn trong tầm giá nhưng vỏ hơi bong keo ở góc. Shop có báo trước nên mình vẫn nhận hàng.',
    'VISIBLE', 42),

-- ===== Khôi Sport - nguoiban05@demo.local =====
('DH202611-000007', 4, N'Mic đàm thoại rõ, đệm tai còn êm, cắm USB là nhận ngay. Dây hơi cứng một chút nhưng tổng thể tốt.',
    'VISIBLE', 31),
('DH202611-000033', 3, N'Áo tạm được nhưng màu hơi bạc so với ảnh. Shop giao hàng đúng hẹn, đóng gói bình thường.',
    'VISIBLE', 38),
('DH202611-000046', 2, N'Sách bị ẩm và mốc ở phần gáy, mùi rất khó chịu. Shop đóng gói không bọc chống ẩm nên mình không hài lòng.',
    'VISIBLE', 34),

-- ===== Lan Mẹ và Bé - nguoiban06@demo.local =====
('DH202611-000008', 5, N'Ấm đun nhanh, tự ngắt chuẩn, lòng ấm sáng bóng như mới. Shop cực kỳ dễ thương và giao hàng đúng hẹn.',
    'VISIBLE', 30),
('DH202611-000021', 4, N'Bàn phím tốt trong tầm giá, led đỏ sáng đẹp. Hộp hơi móp nhưng thiết bị không sao, shop có xin lỗi.',
    'VISIBLE', 40),
('DH202611-000034', 5, N'Quần đẹp, form chuẩn và gần như không thấy dấu sử dụng. Giao nhanh, đóng gói gọn gàng, rất hài lòng.',
    'VISIBLE', 32),
('DH202611-000047', 4, N'Sách sạch, ít ghi chú và đúng nội dung. Giao hàng trễ nửa ngày nhưng shop có nhắn xin lỗi trước.',
    'VISIBLE', 43),

-- ===== Tuấn Nội Thất - nguoiban07@demo.local =====
('DH202611-000009', 3, N'Quạt chạy êm, mát vừa phải. Cánh quạt hơi bám bụi, mình phải vệ sinh lại khi nhận hàng.',
    'VISIBLE', 33),
('DH202611-000022', 4, N'Tai nghe dùng tốt, mic đàm thoại rõ và đệm tai êm. Shop gói hàng chắc chắn, giao đúng hẹn.',
    'VISIBLE', 36),
('DH202611-000035', 4, N'Chuột bấm êm, pin còn tốt và kết nối ổn định. Nút cuộn hơi cứng nhưng chấp nhận được với giá này.',
    'VISIBLE', 31),
('DH202611-000048', 5, N'Áo mới hơn mô tả, vải đẹp và mặc vừa in. Shop giao siêu nhanh, đóng gói cẩn thận, 5 sao xứng đáng.',
    'VISIBLE', 37),

-- ===== Tâm Camera Hà Nội - nguoiban08@demo.local =====
('DH202611-000010', 2, N'Balo bị bung chỉ ở quai đeo và bẩn ở đáy, khác xa ảnh đăng. Shop chỉ xin lỗi chứ không hỗ trợ đổi trả.',
    'VISIBLE', 38),
('DH202611-000023', 4, N'Ấm hoạt động tốt, nước sôi nhanh và tự ngắt an toàn. Vỏ ngoài có vài vết xước nhỏ đúng như mô tả.',
    'VISIBLE', 34),
('DH202611-000036', 5, N'Bàn phím gõ sướng tay, led đều và không kẹt phím. Shop tư vấn kỹ, giao đúng hẹn, rất đáng tiền.',
    'VISIBLE', 30),
('DH202611-000049', 4, N'Quần còn tốt, màu đẹp và đường may chắc. Hơi rộng so với size ghi nhưng shop đã nhắc trước khi giao.',
    'VISIBLE', 42),

-- ===== Châu Đồ Cổ - nguoiban09@demo.local =====
('DH202611-000011', 5, N'Đồng hồ đẹp như mô tả, pin còn tốt và dây sạch sẽ. Shop đóng gói cẩn thận, giao nhanh ngoài mong đợi.',
    'VISIBLE', 32),
('DH202611-000024', 4, N'Quạt chạy êm, đủ 2 tốc độ và đúng công suất. Dây điện hơi ngắn nhưng không ảnh hưởng nhiều.',
    'VISIBLE', 39),
('DH202611-000050', 4, N'Chuột nhận nhanh, bấm êm và đúng mô tả. Shop thân thiện, còn nhắn hỏi thăm sau khi giao hàng.',
    'VISIBLE', 33),

-- ===== Hân Biển Xanh - nguoiban10@demo.local =====
('DH202611-000012', 4, N'Máy tính tốt, đủ nắp và pin còn dùng được lâu. Shop giao hàng đúng hẹn, đóng gói chắc chắn.',
    'VISIBLE', 35),
('DH202611-000025', 3, N'Balo dùng được nhưng khóa kéo hơi rít, phải tra dầu mới trơn. Shop nên kiểm tra kỹ trước khi giao hàng.',
    'VISIBLE', 40),
('DH202611-000051', 5, N'Bàn phím như mới, gõ nảy và led sáng rất đẹp. Shop giao cực nhanh, đóng gói 3 lớp xốp rất kỹ.',
    'VISIBLE', 30),

-- ===== Kiệt Miền Tây - nguoiban11@demo.local =====
('DH202611-000013', 5, N'Đèn sáng dịu, thân gập linh hoạt và đúng mô tả. Shop dễ thương, giao hàng nhanh, mình rất hài lòng.',
    'VISIBLE', 34),
('DH202611-000026', 4, N'Đồng hồ chạy chính xác, dây còn đẹp và đeo vừa tay. Mặt sau có vết xước nhẹ nhưng không đáng kể.',
    'VISIBLE', 36),
('DH202611-000039', 2, N'Đừng ai mua ở shop này, toàn hàng lỗi. Mình biết chỗ khác bán rẻ hơn nhiều, inbox mình chỉ cho, đảm bảo uy tín.',
    'HIDDEN', 41),
('DH202611-000052', 5, NULL,
    'VISIBLE', 30);

-- -----------------------------------------------------------------------------
-- 2. Kiểm tra toàn vẹn nguồn đơn trước khi ghi: mọi mã đơn phải tồn tại và COMPLETED.
-- -----------------------------------------------------------------------------
IF EXISTS (
    SELECT 1
    FROM @SeedDanhGia sd
    LEFT JOIN dbo.DonHang dh ON dh.MaDon = sd.MaDon
    WHERE dh.MaDonHang IS NULL
       OR dh.TrangThai <> 'COMPLETED'
)
    THROW 50033, N'Seed đánh giá: có mã đơn không tồn tại hoặc chưa COMPLETED. Hãy chạy 09_seed_data.sql và 32_seed_don_hang.sql trước.', 1;

-- -----------------------------------------------------------------------------
-- 3. Ghi đánh giá trong transaction: map FK từ DonHang, chống trùng theo MaDonHang.
--    NgayTao = DonHang.NgayTao + GioSauDon giờ, nhưng không vượt quá thời điểm hiện tại
--    (đơn mới nhất trong seed chỉ cách đây 1 ngày nên vẫn luôn sau NgayTao của đơn).
-- -----------------------------------------------------------------------------
DECLARE @SoDanhGiaThem int = 0;

BEGIN TRY
    BEGIN TRANSACTION;

    INSERT INTO dbo.DanhGia (MaDonHang, MaNguoiMua, MaCuaHang, SoSao, NoiDung, TrangThai, NgayTao)
    SELECT
        dh.MaDonHang,
        dh.MaNguoiDung,                -- người mua của đơn (FK NguoiDung)
        dh.MaCuaHang,                  -- shop của đơn (FK CuaHang)
        sd.SoSao,
        sd.NoiDung,
        sd.TrangThai,
        CASE
            WHEN DATEADD(HOUR, sd.GioSauDon, dh.NgayTao) > SYSUTCDATETIME()
                THEN SYSUTCDATETIME()
            ELSE DATEADD(HOUR, sd.GioSauDon, dh.NgayTao)
        END
    FROM @SeedDanhGia sd
    JOIN dbo.DonHang dh ON dh.MaDon = sd.MaDon
    WHERE dh.TrangThai = 'COMPLETED'
      AND NOT EXISTS (
          SELECT 1 FROM dbo.DanhGia dg WHERE dg.MaDonHang = dh.MaDonHang
      );

    SET @SoDanhGiaThem = @@ROWCOUNT;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;

PRINT CONCAT(N'Seed đánh giá: đã thêm ', @SoDanhGiaThem, N' đánh giá mới (chạy lại sẽ chỉ thêm các đơn còn thiếu).');
GO

-- =============================================================================
-- 4. KIỂM TRA NHANH SAU KHI SEED
-- =============================================================================

-- 4.1 Tổng quan
SELECT
    COUNT(*)                                                        AS TongSoDanhGia,
    SUM(CASE WHEN TrangThai = 'VISIBLE' THEN 1 ELSE 0 END)          AS SoDanhGiaVisible,
    SUM(CASE WHEN TrangThai = 'HIDDEN'  THEN 1 ELSE 0 END)          AS SoDanhGiaHidden,
    SUM(CASE WHEN NoiDung IS NULL THEN 1 ELSE 0 END)                AS SoDanhGiaChiChamSao,
    CAST(AVG(CAST(SoSao AS decimal(4,2))) AS decimal(4,2))          AS DiemTrungBinhToanHeThong
FROM dbo.DanhGia;
GO

-- 4.2 Điểm từng shop (đọc qua view chuẩn vw_DiemCuaHang)
SELECT
    ch.MaCuaHang,
    ch.TenShop,
    nd.HoTen                                                          AS ChuShop,
    v.DiemTrungBinh,
    v.SoDanhGia                                                       AS SoDanhGiaVisible,
    (SELECT COUNT(*) FROM dbo.DanhGia dg
      WHERE dg.MaCuaHang = ch.MaCuaHang AND dg.TrangThai = 'HIDDEN')  AS SoDanhGiaHidden,
    CASE
        WHEN v.SoDanhGia >= 5 AND v.DiemTrungBinh < 3.0 THEN N'ĐẠT: >= 5 đánh giá và TB < 3.0'
        WHEN v.DiemTrungBinh >= 4.8                     THEN N'Shop điểm cao (>= 4.8)'
        ELSE N''
    END                                                               AS GhiChu
FROM dbo.vw_DiemCuaHang v
JOIN dbo.CuaHang  ch ON ch.MaCuaHang = v.MaCuaHang
JOIN dbo.NguoiDung nd ON nd.MaNguoiDung = ch.MaNguoiDung
WHERE v.SoDanhGia > 0
ORDER BY v.DiemTrungBinh ASC, v.SoDanhGia DESC, ch.MaCuaHang;
GO

-- 4.3 Chốt điều kiện nghiệp vụ: phải có ít nhất 1 shop >= 5 đánh giá và TB < 3.0
SELECT
    CASE
        WHEN EXISTS (SELECT 1 FROM dbo.vw_DiemCuaHang WHERE SoDanhGia >= 5 AND DiemTrungBinh < 3.0)
            THEN N'OK: đã có shop >= 5 đánh giá và điểm trung bình < 3.0'
        ELSE N'CHƯA ĐẠT: thiếu shop >= 5 đánh giá với điểm trung bình < 3.0'
    END AS KiemTraShopDuoi3Sao;
GO

-- 4.4 Các đơn COMPLETED còn trống đánh giá (dùng để demo người mua tự đánh giá)
SELECT
    dh.MaDon,
    nd.HoTen      AS NguoiMua,
    nd.Email      AS EmailNguoiMua,
    ch.TenShop,
    dh.NgayTao
FROM dbo.DonHang dh
JOIN dbo.NguoiDung nd ON nd.MaNguoiDung = dh.MaNguoiDung
JOIN dbo.CuaHang   ch ON ch.MaCuaHang = dh.MaCuaHang
WHERE dh.TrangThai = 'COMPLETED'
  AND NOT EXISTS (SELECT 1 FROM dbo.DanhGia dg WHERE dg.MaDonHang = dh.MaDonHang)
ORDER BY dh.NgayTao DESC;
GO
