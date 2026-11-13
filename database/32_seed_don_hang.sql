USE SecondHandMarketplace;
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ARITHABORT ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

-- =============================================================================
-- D6: Seed dữ liệu mẫu Đơn hàng, Thanh toán, Lịch sử, Báo cáo & Tranh chấp
-- Phụ trách: Người D (Giao dịch, Đơn hàng, VietQR, Tranh chấp & Báo cáo)
-- Mục tiêu:
--   - Khoảng 70 đơn hàng (trong đó 52 đơn COMPLETED, đáp ứng tiêu chí >= 50 đơn).
--   - Đầy đủ các trạng thái: PENDING, CONFIRMED, SHIPPING, DELIVERED, COMPLETED, CANCELLED.
--   - Đầy đủ cả 2 hình thức nhận: GIAO_HANG và GAP_TRUC_TIEP.
--   - Đầy đủ 2 phương thức thanh toán: COD và BANK_TRANSFER (kèm bản ghi ThanhToan).
--   - Mỗi đơn có đầy đủ Lịch sử trạng thái đơn hàng (LichSuTrangThaiDonHang).
--   - Kèm 8 Báo cáo vi phạm (BaoCao) và 4 Tranh chấp (TranhChap).
--   - Khả năng chạy lại (Idempotent) thông qua WHERE NOT EXISTS (MaDon).
-- =============================================================================

DECLARE @Now datetime2(3) = SYSUTCDATETIME();
DECLARE @MatKhauHash varchar(100) = '$2y$11$tFZdjm5I7Pl1SMMfYeaAweEjlCFoRZSY.bHEL40rCIHCPxA.SxkLK';
DECLARE @AdminId int = (SELECT TOP 1 MaNguoiDung FROM NguoiDung WHERE VaiTro = 'ADMIN' ORDER BY MaNguoiDung);

BEGIN TRANSACTION;
-- 1. Bổ sung Danh mục & Tình trạng mặc định nếu chưa có
IF NOT EXISTS (SELECT 1 FROM DanhMuc WHERE Ten = N'Đồ điện tử')
    INSERT DanhMuc (Ten) VALUES (N'Đồ điện tử');
IF NOT EXISTS (SELECT 1 FROM DanhMuc WHERE Ten = N'Thời trang')
    INSERT DanhMuc (Ten) VALUES (N'Thời trang');
IF NOT EXISTS (SELECT 1 FROM DanhMuc WHERE Ten = N'Đồ gia dụng')
    INSERT DanhMuc (Ten) VALUES (N'Đồ gia dụng');
IF NOT EXISTS (SELECT 1 FROM DanhMuc WHERE Ten = N'Sách & văn phòng phẩm')
    INSERT DanhMuc (Ten) VALUES (N'Sách & văn phòng phẩm');
-- 2. Tạo bảng tạm chứa 70 đơn hàng cần seed
DECLARE @SeedOrders TABLE (
    Stt int PRIMARY KEY,
    MaDon varchar(30),
    TrangThai varchar(15),
    HinhThucNhan varchar(20),
    PhuongThucThanhToan varchar(20),
    SoNgayTruoc int,
    BuyerEmail varchar(100),
    SellerEmail varchar(100),
    TenSanPham nvarchar(200),
    Gia decimal(18,2),
    TenDanhMuc nvarchar(100),
    TinhTrang varchar(20),
    MoTaSP nvarchar(500)
);
INSERT INTO @SeedOrders (Stt, MaDon, TrangThai, HinhThucNhan, PhuongThucThanhToan, SoNgayTruoc, BuyerEmail, SellerEmail, TenSanPham, Gia, TenDanhMuc, TinhTrang, MoTaSP) VALUES    (1, 'DH202611-000001', 'COMPLETED', 'GIAO_HANG', 'COD', 52, 'chi.buyer@demo.local', 'an.seller@demo.local', N'Sách Giải tích 1 ĐH Bách Khoa #1', 45000, N'Sách & văn phòng phẩm', 'GOOD', N'Sách học đại học, còn mới 90%, bìa cứng không quăn mép.'),
    (2, 'DH202611-000002', 'COMPLETED', 'GAP_TRUC_TIEP', 'COD', 51, 'dung.buyer@demo.local', 'binh.seller@demo.local', N'Giáo trình Cấu trúc dữ liệu và giải thuật #2', 60000, N'Sách & văn phòng phẩm', 'LIKE_NEW', N'Sách tái bản mới, ít sử dụng, không viết vẽ bậy.'),
    (3, 'DH202611-000003', 'COMPLETED', 'GIAO_HANG', 'BANK_TRANSFER', 50, 'noshop@demo.local', 'nguoiban01@demo.local', N'Áo thun polo Uniqlo size M màu navy #3', 120000, N'Thời trang', 'GOOD', N'Áo form ôm nhẹ, chất vải gai thoáng mát, mặc 2-3 lần.'),
    (4, 'DH202611-000004', 'COMPLETED', 'GAP_TRUC_TIEP', 'COD', 49, 'nguoimua01@demo.local', 'nguoiban02@demo.local', N'Quần jean Levis 511 size 31 #4', 280000, N'Thời trang', 'GOOD', N'Quần jean ống đứng dáng đẹp, đã qua sử dụng, chưa bạc màu.'),
    (5, 'DH202611-000005', 'COMPLETED', 'GIAO_HANG', 'COD', 48, 'nguoimua02@demo.local', 'nguoiban03@demo.local', N'Chuột không dây Logitech M331 Silent #5', 150000, N'Đồ điện tử', 'LIKE_NEW', N'Chuột bấm êm không ồn, pin dùng được 6 tháng, kèm đầu thu USB.'),
    (6, 'DH202611-000006', 'COMPLETED', 'GAP_TRUC_TIEP', 'BANK_TRANSFER', 47, 'nguoimua03@demo.local', 'nguoiban04@demo.local', N'Bàn phím DareU EK87 Tenkeyless Blue Switch #6', 320000, N'Đồ điện tử', 'GOOD', N'Bàn phím cơ gõ nảy, led đỏ sáng đều, phím bấm tốt không kẹt.'),
    (7, 'DH202611-000007', 'COMPLETED', 'GIAO_HANG', 'COD', 46, 'nguoimua04@demo.local', 'nguoiban05@demo.local', N'Tai nghe gaming chụp tai DareU EH416 #7', 200000, N'Đồ điện tử', 'GOOD', N'Tai nghe có mic đàm thoại rõ, đệm tai còn êm, cắm cổng USB.'),
    (8, 'DH202611-000008', 'COMPLETED', 'GAP_TRUC_TIEP', 'COD', 45, 'nguoimua05@demo.local', 'nguoiban06@demo.local', N'Ấm siêu tốc Philips 1.5L inox 304 #8', 180000, N'Đồ gia dụng', 'LIKE_NEW', N'Ấm đun nước sôi nhanh trong 4 phút, tự ngắt an toàn, lòng ấm sạch.'),
    (9, 'DH202611-000009', 'COMPLETED', 'GIAO_HANG', 'BANK_TRANSFER', 44, 'nguoimua06@demo.local', 'nguoiban07@demo.local', N'Quạt bàn mini Senko B102 #9', 110000, N'Đồ gia dụng', 'GOOD', N'Quạt để bàn học gọn nhẹ, 2 tốc độ gió, chạy êm mát.'),
    (10, 'DH202611-000010', 'COMPLETED', 'GAP_TRUC_TIEP', 'COD', 43, 'nguoimua07@demo.local', 'nguoiban08@demo.local', N'Balo đi học The North Face Router 35L #10', 350000, N'Thời trang', 'FAIR', N'Balo ngăn laptop 15.6 inch chống sốc dày, quai đeo trợ lực tốt.'),
    (11, 'DH202611-000011', 'COMPLETED', 'GIAO_HANG', 'COD', 42, 'nguoimua08@demo.local', 'nguoiban09@demo.local', N'Đồng hồ thể thao điện tử Casio F-91W #11', 190000, N'Thời trang', 'LIKE_NEW', N'Đồng hồ huyền thoại chống nước nhẹ, pin trâu, dây cao su nguyên bản.'),
    (12, 'DH202611-000012', 'COMPLETED', 'GAP_TRUC_TIEP', 'BANK_TRANSFER', 41, 'nguoimua09@demo.local', 'nguoiban10@demo.local', N'Máy tính bỏ túi Casio fx-580VN X #12', 380000, N'Sách & văn phòng phẩm', 'LIKE_NEW', N'Máy tính cho học sinh sinh viên, vỏ nắp đầy đủ, màn hình sắc nét.'),
    (13, 'DH202611-000013', 'COMPLETED', 'GIAO_HANG', 'COD', 40, 'nguoimua10@demo.local', 'nguoiban11@demo.local', N'Đèn học để bàn chống cận Rạng Đông LED 6W #13', 130000, N'Đồ gia dụng', 'GOOD', N'Đèn ánh sáng vàng dịu mắt, thân gập uốn linh hoạt, tiết kiệm điện.'),
    (14, 'DH202611-000014', 'COMPLETED', 'GAP_TRUC_TIEP', 'COD', 39, 'nguoimua11@demo.local', 'an.seller@demo.local', N'Nồi cơm điện nắp gài Sharp 1.8L #14', 270000, N'Đồ gia dụng', 'FAIR', N'Nồi nấu cơm gia đình 4 người, lòng nồi có chống dính, hoạt động tốt.'),
    (15, 'DH202611-000015', 'COMPLETED', 'GIAO_HANG', 'BANK_TRANSFER', 38, 'nguoimua12@demo.local', 'binh.seller@demo.local', N'Loa kéo karaoke di động mini vỏ gỗ #15', 650000, N'Đồ điện tử', 'GOOD', N'Kèm 1 micro không dây, pin hát được 3 tiếng, kết nối Bluetooth nhanh.'),
    (16, 'DH202611-000016', 'COMPLETED', 'GAP_TRUC_TIEP', 'COD', 37, 'nguoimua13@demo.local', 'nguoiban01@demo.local', N'Sách Giải tích 1 ĐH Bách Khoa #16', 45000, N'Sách & văn phòng phẩm', 'GOOD', N'Sách học đại học, còn mới 90%, bìa cứng không quăn mép.'),
    (17, 'DH202611-000017', 'COMPLETED', 'GIAO_HANG', 'COD', 36, 'nguoimua14@demo.local', 'nguoiban02@demo.local', N'Giáo trình Cấu trúc dữ liệu và giải thuật #17', 60000, N'Sách & văn phòng phẩm', 'LIKE_NEW', N'Sách tái bản mới, ít sử dụng, không viết vẽ bậy.'),
    (18, 'DH202611-000018', 'COMPLETED', 'GAP_TRUC_TIEP', 'BANK_TRANSFER', 35, 'nguoimua15@demo.local', 'nguoiban03@demo.local', N'Áo thun polo Uniqlo size M màu navy #18', 120000, N'Thời trang', 'GOOD', N'Áo form ôm nhẹ, chất vải gai thoáng mát, mặc 2-3 lần.'),
    (19, 'DH202611-000019', 'COMPLETED', 'GIAO_HANG', 'COD', 34, 'chi.buyer@demo.local', 'nguoiban04@demo.local', N'Quần jean Levis 511 size 31 #19', 280000, N'Thời trang', 'GOOD', N'Quần jean ống đứng dáng đẹp, đã qua sử dụng, chưa bạc màu.'),
    (20, 'DH202611-000020', 'COMPLETED', 'GAP_TRUC_TIEP', 'COD', 33, 'dung.buyer@demo.local', 'nguoiban05@demo.local', N'Chuột không dây Logitech M331 Silent #20', 150000, N'Đồ điện tử', 'LIKE_NEW', N'Chuột bấm êm không ồn, pin dùng được 6 tháng, kèm đầu thu USB.'),
    (21, 'DH202611-000021', 'COMPLETED', 'GIAO_HANG', 'BANK_TRANSFER', 32, 'noshop@demo.local', 'nguoiban06@demo.local', N'Bàn phím DareU EK87 Tenkeyless Blue Switch #21', 320000, N'Đồ điện tử', 'GOOD', N'Bàn phím cơ gõ nảy, led đỏ sáng đều, phím bấm tốt không kẹt.'),
    (22, 'DH202611-000022', 'COMPLETED', 'GAP_TRUC_TIEP', 'COD', 31, 'nguoimua01@demo.local', 'nguoiban07@demo.local', N'Tai nghe gaming chụp tai DareU EH416 #22', 200000, N'Đồ điện tử', 'GOOD', N'Tai nghe có mic đàm thoại rõ, đệm tai còn êm, cắm cổng USB.'),
    (23, 'DH202611-000023', 'COMPLETED', 'GIAO_HANG', 'COD', 30, 'nguoimua02@demo.local', 'nguoiban08@demo.local', N'Ấm siêu tốc Philips 1.5L inox 304 #23', 180000, N'Đồ gia dụng', 'LIKE_NEW', N'Ấm đun nước sôi nhanh trong 4 phút, tự ngắt an toàn, lòng ấm sạch.'),
    (24, 'DH202611-000024', 'COMPLETED', 'GAP_TRUC_TIEP', 'BANK_TRANSFER', 29, 'nguoimua03@demo.local', 'nguoiban09@demo.local', N'Quạt bàn mini Senko B102 #24', 110000, N'Đồ gia dụng', 'GOOD', N'Quạt để bàn học gọn nhẹ, 2 tốc độ gió, chạy êm mát.'),
    (25, 'DH202611-000025', 'COMPLETED', 'GIAO_HANG', 'COD', 28, 'nguoimua04@demo.local', 'nguoiban10@demo.local', N'Balo đi học The North Face Router 35L #25', 350000, N'Thời trang', 'FAIR', N'Balo ngăn laptop 15.6 inch chống sốc dày, quai đeo trợ lực tốt.'),
    (26, 'DH202611-000026', 'COMPLETED', 'GAP_TRUC_TIEP', 'COD', 27, 'nguoimua05@demo.local', 'nguoiban11@demo.local', N'Đồng hồ thể thao điện tử Casio F-91W #26', 190000, N'Thời trang', 'LIKE_NEW', N'Đồng hồ huyền thoại chống nước nhẹ, pin trâu, dây cao su nguyên bản.'),
    (27, 'DH202611-000027', 'COMPLETED', 'GIAO_HANG', 'BANK_TRANSFER', 26, 'nguoimua06@demo.local', 'an.seller@demo.local', N'Máy tính bỏ túi Casio fx-580VN X #27', 380000, N'Sách & văn phòng phẩm', 'LIKE_NEW', N'Máy tính cho học sinh sinh viên, vỏ nắp đầy đủ, màn hình sắc nét.'),
    (28, 'DH202611-000028', 'COMPLETED', 'GAP_TRUC_TIEP', 'COD', 25, 'nguoimua07@demo.local', 'binh.seller@demo.local', N'Đèn học để bàn chống cận Rạng Đông LED 6W #28', 130000, N'Đồ gia dụng', 'GOOD', N'Đèn ánh sáng vàng dịu mắt, thân gập uốn linh hoạt, tiết kiệm điện.'),
    (29, 'DH202611-000029', 'COMPLETED', 'GIAO_HANG', 'COD', 24, 'nguoimua08@demo.local', 'nguoiban01@demo.local', N'Nồi cơm điện nắp gài Sharp 1.8L #29', 270000, N'Đồ gia dụng', 'FAIR', N'Nồi nấu cơm gia đình 4 người, lòng nồi có chống dính, hoạt động tốt.'),
    (30, 'DH202611-000030', 'COMPLETED', 'GAP_TRUC_TIEP', 'BANK_TRANSFER', 23, 'nguoimua09@demo.local', 'nguoiban02@demo.local', N'Loa kéo karaoke di động mini vỏ gỗ #30', 650000, N'Đồ điện tử', 'GOOD', N'Kèm 1 micro không dây, pin hát được 3 tiếng, kết nối Bluetooth nhanh.'),
    (31, 'DH202611-000031', 'COMPLETED', 'GIAO_HANG', 'COD', 22, 'nguoimua10@demo.local', 'nguoiban03@demo.local', N'Sách Giải tích 1 ĐH Bách Khoa #31', 45000, N'Sách & văn phòng phẩm', 'GOOD', N'Sách học đại học, còn mới 90%, bìa cứng không quăn mép.'),
    (32, 'DH202611-000032', 'COMPLETED', 'GAP_TRUC_TIEP', 'COD', 21, 'nguoimua11@demo.local', 'nguoiban04@demo.local', N'Giáo trình Cấu trúc dữ liệu và giải thuật #32', 60000, N'Sách & văn phòng phẩm', 'LIKE_NEW', N'Sách tái bản mới, ít sử dụng, không viết vẽ bậy.'),
    (33, 'DH202611-000033', 'COMPLETED', 'GIAO_HANG', 'BANK_TRANSFER', 20, 'nguoimua12@demo.local', 'nguoiban05@demo.local', N'Áo thun polo Uniqlo size M màu navy #33', 120000, N'Thời trang', 'GOOD', N'Áo form ôm nhẹ, chất vải gai thoáng mát, mặc 2-3 lần.'),
    (34, 'DH202611-000034', 'COMPLETED', 'GAP_TRUC_TIEP', 'COD', 19, 'nguoimua13@demo.local', 'nguoiban06@demo.local', N'Quần jean Levis 511 size 31 #34', 280000, N'Thời trang', 'GOOD', N'Quần jean ống đứng dáng đẹp, đã qua sử dụng, chưa bạc màu.'),
    (35, 'DH202611-000035', 'COMPLETED', 'GIAO_HANG', 'COD', 18, 'nguoimua14@demo.local', 'nguoiban07@demo.local', N'Chuột không dây Logitech M331 Silent #35', 150000, N'Đồ điện tử', 'LIKE_NEW', N'Chuột bấm êm không ồn, pin dùng được 6 tháng, kèm đầu thu USB.'),
    (36, 'DH202611-000036', 'COMPLETED', 'GAP_TRUC_TIEP', 'BANK_TRANSFER', 17, 'nguoimua15@demo.local', 'nguoiban08@demo.local', N'Bàn phím DareU EK87 Tenkeyless Blue Switch #36', 320000, N'Đồ điện tử', 'GOOD', N'Bàn phím cơ gõ nảy, led đỏ sáng đều, phím bấm tốt không kẹt.'),
    (37, 'DH202611-000037', 'COMPLETED', 'GIAO_HANG', 'COD', 16, 'chi.buyer@demo.local', 'nguoiban09@demo.local', N'Tai nghe gaming chụp tai DareU EH416 #37', 200000, N'Đồ điện tử', 'GOOD', N'Tai nghe có mic đàm thoại rõ, đệm tai còn êm, cắm cổng USB.'),
    (38, 'DH202611-000038', 'COMPLETED', 'GAP_TRUC_TIEP', 'COD', 15, 'dung.buyer@demo.local', 'nguoiban10@demo.local', N'Ấm siêu tốc Philips 1.5L inox 304 #38', 180000, N'Đồ gia dụng', 'LIKE_NEW', N'Ấm đun nước sôi nhanh trong 4 phút, tự ngắt an toàn, lòng ấm sạch.'),
    (39, 'DH202611-000039', 'COMPLETED', 'GIAO_HANG', 'BANK_TRANSFER', 14, 'noshop@demo.local', 'nguoiban11@demo.local', N'Quạt bàn mini Senko B102 #39', 110000, N'Đồ gia dụng', 'GOOD', N'Quạt để bàn học gọn nhẹ, 2 tốc độ gió, chạy êm mát.'),
    (40, 'DH202611-000040', 'COMPLETED', 'GAP_TRUC_TIEP', 'COD', 13, 'nguoimua01@demo.local', 'an.seller@demo.local', N'Balo đi học The North Face Router 35L #40', 350000, N'Thời trang', 'FAIR', N'Balo ngăn laptop 15.6 inch chống sốc dày, quai đeo trợ lực tốt.'),
    (41, 'DH202611-000041', 'COMPLETED', 'GIAO_HANG', 'COD', 12, 'nguoimua02@demo.local', 'binh.seller@demo.local', N'Đồng hồ thể thao điện tử Casio F-91W #41', 190000, N'Thời trang', 'LIKE_NEW', N'Đồng hồ huyền thoại chống nước nhẹ, pin trâu, dây cao su nguyên bản.'),
    (42, 'DH202611-000042', 'COMPLETED', 'GAP_TRUC_TIEP', 'BANK_TRANSFER', 11, 'nguoimua03@demo.local', 'nguoiban01@demo.local', N'Máy tính bỏ túi Casio fx-580VN X #42', 380000, N'Sách & văn phòng phẩm', 'LIKE_NEW', N'Máy tính cho học sinh sinh viên, vỏ nắp đầy đủ, màn hình sắc nét.'),
    (43, 'DH202611-000043', 'COMPLETED', 'GIAO_HANG', 'COD', 10, 'nguoimua04@demo.local', 'nguoiban02@demo.local', N'Đèn học để bàn chống cận Rạng Đông LED 6W #43', 130000, N'Đồ gia dụng', 'GOOD', N'Đèn ánh sáng vàng dịu mắt, thân gập uốn linh hoạt, tiết kiệm điện.'),
    (44, 'DH202611-000044', 'COMPLETED', 'GAP_TRUC_TIEP', 'COD', 9, 'nguoimua05@demo.local', 'nguoiban03@demo.local', N'Nồi cơm điện nắp gài Sharp 1.8L #44', 270000, N'Đồ gia dụng', 'FAIR', N'Nồi nấu cơm gia đình 4 người, lòng nồi có chống dính, hoạt động tốt.'),
    (45, 'DH202611-000045', 'COMPLETED', 'GIAO_HANG', 'BANK_TRANSFER', 8, 'nguoimua06@demo.local', 'nguoiban04@demo.local', N'Loa kéo karaoke di động mini vỏ gỗ #45', 650000, N'Đồ điện tử', 'GOOD', N'Kèm 1 micro không dây, pin hát được 3 tiếng, kết nối Bluetooth nhanh.'),
    (46, 'DH202611-000046', 'COMPLETED', 'GAP_TRUC_TIEP', 'COD', 7, 'nguoimua07@demo.local', 'nguoiban05@demo.local', N'Sách Giải tích 1 ĐH Bách Khoa #46', 45000, N'Sách & văn phòng phẩm', 'GOOD', N'Sách học đại học, còn mới 90%, bìa cứng không quăn mép.'),
    (47, 'DH202611-000047', 'COMPLETED', 'GIAO_HANG', 'COD', 6, 'nguoimua08@demo.local', 'nguoiban06@demo.local', N'Giáo trình Cấu trúc dữ liệu và giải thuật #47', 60000, N'Sách & văn phòng phẩm', 'LIKE_NEW', N'Sách tái bản mới, ít sử dụng, không viết vẽ bậy.'),
    (48, 'DH202611-000048', 'COMPLETED', 'GAP_TRUC_TIEP', 'BANK_TRANSFER', 5, 'nguoimua09@demo.local', 'nguoiban07@demo.local', N'Áo thun polo Uniqlo size M màu navy #48', 120000, N'Thời trang', 'GOOD', N'Áo form ôm nhẹ, chất vải gai thoáng mát, mặc 2-3 lần.'),
    (49, 'DH202611-000049', 'COMPLETED', 'GIAO_HANG', 'COD', 4, 'nguoimua10@demo.local', 'nguoiban08@demo.local', N'Quần jean Levis 511 size 31 #49', 280000, N'Thời trang', 'GOOD', N'Quần jean ống đứng dáng đẹp, đã qua sử dụng, chưa bạc màu.'),
    (50, 'DH202611-000050', 'COMPLETED', 'GAP_TRUC_TIEP', 'COD', 3, 'nguoimua11@demo.local', 'nguoiban09@demo.local', N'Chuột không dây Logitech M331 Silent #50', 150000, N'Đồ điện tử', 'LIKE_NEW', N'Chuột bấm êm không ồn, pin dùng được 6 tháng, kèm đầu thu USB.'),
    (51, 'DH202611-000051', 'COMPLETED', 'GIAO_HANG', 'BANK_TRANSFER', 2, 'nguoimua12@demo.local', 'nguoiban10@demo.local', N'Bàn phím DareU EK87 Tenkeyless Blue Switch #51', 320000, N'Đồ điện tử', 'GOOD', N'Bàn phím cơ gõ nảy, led đỏ sáng đều, phím bấm tốt không kẹt.'),
    (52, 'DH202611-000052', 'COMPLETED', 'GAP_TRUC_TIEP', 'COD', 1, 'nguoimua13@demo.local', 'nguoiban11@demo.local', N'Tai nghe gaming chụp tai DareU EH416 #52', 200000, N'Đồ điện tử', 'GOOD', N'Tai nghe có mic đàm thoại rõ, đệm tai còn êm, cắm cổng USB.'),
    (53, 'DH202611-000053', 'PENDING', 'GIAO_HANG', 'COD', 1, 'nguoimua14@demo.local', 'an.seller@demo.local', N'Ấm siêu tốc Philips 1.5L inox 304 #53', 180000, N'Đồ gia dụng', 'LIKE_NEW', N'Ấm đun nước sôi nhanh trong 4 phút, tự ngắt an toàn, lòng ấm sạch.'),
    (54, 'DH202611-000054', 'PENDING', 'GAP_TRUC_TIEP', 'BANK_TRANSFER', 1, 'nguoimua15@demo.local', 'binh.seller@demo.local', N'Quạt bàn mini Senko B102 #54', 110000, N'Đồ gia dụng', 'GOOD', N'Quạt để bàn học gọn nhẹ, 2 tốc độ gió, chạy êm mát.'),
    (55, 'DH202611-000055', 'PENDING', 'GIAO_HANG', 'BANK_TRANSFER', 2, 'chi.buyer@demo.local', 'nguoiban01@demo.local', N'Balo đi học The North Face Router 35L #55', 350000, N'Thời trang', 'FAIR', N'Balo ngăn laptop 15.6 inch chống sốc dày, quai đeo trợ lực tốt.'),
    (56, 'DH202611-000056', 'PENDING', 'GAP_TRUC_TIEP', 'COD', 2, 'dung.buyer@demo.local', 'nguoiban02@demo.local', N'Đồng hồ thể thao điện tử Casio F-91W #56', 190000, N'Thời trang', 'LIKE_NEW', N'Đồng hồ huyền thoại chống nước nhẹ, pin trâu, dây cao su nguyên bản.'),
    (57, 'DH202611-000057', 'CONFIRMED', 'GIAO_HANG', 'COD', 3, 'noshop@demo.local', 'nguoiban03@demo.local', N'Máy tính bỏ túi Casio fx-580VN X #57', 380000, N'Sách & văn phòng phẩm', 'LIKE_NEW', N'Máy tính cho học sinh sinh viên, vỏ nắp đầy đủ, màn hình sắc nét.'),
    (58, 'DH202611-000058', 'CONFIRMED', 'GAP_TRUC_TIEP', 'BANK_TRANSFER', 3, 'nguoimua01@demo.local', 'nguoiban04@demo.local', N'Đèn học để bàn chống cận Rạng Đông LED 6W #58', 130000, N'Đồ gia dụng', 'GOOD', N'Đèn ánh sáng vàng dịu mắt, thân gập uốn linh hoạt, tiết kiệm điện.'),
    (59, 'DH202611-000059', 'CONFIRMED', 'GIAO_HANG', 'BANK_TRANSFER', 4, 'nguoimua02@demo.local', 'nguoiban05@demo.local', N'Nồi cơm điện nắp gài Sharp 1.8L #59', 270000, N'Đồ gia dụng', 'FAIR', N'Nồi nấu cơm gia đình 4 người, lòng nồi có chống dính, hoạt động tốt.'),
    (60, 'DH202611-000060', 'CONFIRMED', 'GAP_TRUC_TIEP', 'COD', 4, 'nguoimua03@demo.local', 'nguoiban06@demo.local', N'Loa kéo karaoke di động mini vỏ gỗ #60', 650000, N'Đồ điện tử', 'GOOD', N'Kèm 1 micro không dây, pin hát được 3 tiếng, kết nối Bluetooth nhanh.'),
    (61, 'DH202611-000061', 'SHIPPING', 'GIAO_HANG', 'COD', 5, 'nguoimua04@demo.local', 'nguoiban07@demo.local', N'Sách Giải tích 1 ĐH Bách Khoa #61', 45000, N'Sách & văn phòng phẩm', 'GOOD', N'Sách học đại học, còn mới 90%, bìa cứng không quăn mép.'),
    (62, 'DH202611-000062', 'SHIPPING', 'GAP_TRUC_TIEP', 'COD', 5, 'nguoimua05@demo.local', 'nguoiban08@demo.local', N'Giáo trình Cấu trúc dữ liệu và giải thuật #62', 60000, N'Sách & văn phòng phẩm', 'LIKE_NEW', N'Sách tái bản mới, ít sử dụng, không viết vẽ bậy.'),
    (63, 'DH202611-000063', 'SHIPPING', 'GIAO_HANG', 'BANK_TRANSFER', 6, 'nguoimua06@demo.local', 'nguoiban09@demo.local', N'Áo thun polo Uniqlo size M màu navy #63', 120000, N'Thời trang', 'GOOD', N'Áo form ôm nhẹ, chất vải gai thoáng mát, mặc 2-3 lần.'),
    (64, 'DH202611-000064', 'SHIPPING', 'GAP_TRUC_TIEP', 'BANK_TRANSFER', 6, 'nguoimua07@demo.local', 'nguoiban10@demo.local', N'Quần jean Levis 511 size 31 #64', 280000, N'Thời trang', 'GOOD', N'Quần jean ống đứng dáng đẹp, đã qua sử dụng, chưa bạc màu.'),
    (65, 'DH202611-000065', 'DELIVERED', 'GIAO_HANG', 'COD', 7, 'nguoimua08@demo.local', 'nguoiban11@demo.local', N'Chuột không dây Logitech M331 Silent #65', 150000, N'Đồ điện tử', 'LIKE_NEW', N'Chuột bấm êm không ồn, pin dùng được 6 tháng, kèm đầu thu USB.'),
    (66, 'DH202611-000066', 'DELIVERED', 'GAP_TRUC_TIEP', 'BANK_TRANSFER', 7, 'nguoimua09@demo.local', 'an.seller@demo.local', N'Bàn phím DareU EK87 Tenkeyless Blue Switch #66', 320000, N'Đồ điện tử', 'GOOD', N'Bàn phím cơ gõ nảy, led đỏ sáng đều, phím bấm tốt không kẹt.'),
    (67, 'DH202611-000067', 'CANCELLED', 'GIAO_HANG', 'COD', 8, 'nguoimua10@demo.local', 'binh.seller@demo.local', N'Tai nghe gaming chụp tai DareU EH416 #67', 200000, N'Đồ điện tử', 'GOOD', N'Tai nghe có mic đàm thoại rõ, đệm tai còn êm, cắm cổng USB.'),
    (68, 'DH202611-000068', 'CANCELLED', 'GAP_TRUC_TIEP', 'COD', 8, 'nguoimua11@demo.local', 'nguoiban01@demo.local', N'Ấm siêu tốc Philips 1.5L inox 304 #68', 180000, N'Đồ gia dụng', 'LIKE_NEW', N'Ấm đun nước sôi nhanh trong 4 phút, tự ngắt an toàn, lòng ấm sạch.'),
    (69, 'DH202611-000069', 'CANCELLED', 'GIAO_HANG', 'BANK_TRANSFER', 9, 'nguoimua12@demo.local', 'nguoiban02@demo.local', N'Quạt bàn mini Senko B102 #69', 110000, N'Đồ gia dụng', 'GOOD', N'Quạt để bàn học gọn nhẹ, 2 tốc độ gió, chạy êm mát.'),
    (70, 'DH202611-000070', 'CANCELLED', 'GAP_TRUC_TIEP', 'BANK_TRANSFER', 9, 'nguoimua13@demo.local', 'nguoiban03@demo.local', N'Balo đi học The North Face Router 35L #70', 350000, N'Thời trang', 'FAIR', N'Balo ngăn laptop 15.6 inch chống sốc dày, quai đeo trợ lực tốt.');
-- 3. Tạo sản phẩm tương ứng cho từng đơn hàng nếu chưa tồn tại
INSERT SanPham (MaCuaHang, MaDanhMuc, MaTinhTrang, Ten, ChuoiTimKiem, MoTa, Gia, TrangThai, NgayTao)
SELECT 
    ch.MaCuaHang,
    dm.MaDanhMuc,
    tt.MaTinhTrang,
    so.TenSanPham,
    dbo.fn_ChuanHoaChuoi(so.TenSanPham),
    so.MoTaSP,
    so.Gia,
    CASE 
        WHEN so.TrangThai = 'COMPLETED' THEN 'SOLD'
        WHEN so.TrangThai = 'CANCELLED' THEN 'ACTIVE'
        ELSE 'RESERVED'
    END,
    DATEADD(DAY, -so.SoNgayTruoc - 1, @Now)
FROM @SeedOrders so
JOIN NguoiDung ndSeller ON ndSeller.Email = so.SellerEmail
JOIN CuaHang ch ON ch.MaNguoiDung = ndSeller.MaNguoiDung
JOIN DanhMuc dm ON dm.Ten = so.TenDanhMuc
JOIN TinhTrangSanPham tt ON tt.Ma = so.TinhTrang
WHERE NOT EXISTS (
    SELECT 1 FROM SanPham sp WHERE sp.MaCuaHang = ch.MaCuaHang AND sp.Ten = so.TenSanPham
);

-- 4. Tạo Đơn hàng (DonHang)
INSERT DonHang (
    MaDon, MaNguoiDung, MaCuaHang, TenNguoiNhan, SoDienThoaiNhan, DiaChiNhan,
    TienHang, PhiVanChuyen, TongTien, PhuongThucThanhToan, TrangThaiThanhToan,
    TrangThai, HinhThucNhan, NguoiHuy, LyDoHuy, ThoiGianHuy, NgayTao, NgayCapNhat
)
SELECT
    so.MaDon,
    ndBuyer.MaNguoiDung,
    ch.MaCuaHang,
    ndBuyer.HoTen,
    COALESCE(ndBuyer.SoDienThoai, '0909000099'),
    CASE 
        WHEN so.HinhThucNhan = 'GAP_TRUC_TIEP' THEN N'Cổng trường UTC2 - Số 450 Lê Văn Việt, Tăng Nhơn Phú A, TP. Thủ Đức'
        ELSE COALESCE(ndBuyer.DiaChiMacDinh, N'123 Lê Lợi, Phường Bến Nghé, Quận 1, TP. Hồ Chí Minh')
    END,
    so.Gia,
    0, -- PhiVanChuyen = 0 (Free ship / Gặp trực tiếp)
    so.Gia,
    so.PhuongThucThanhToan,
    CASE 
        WHEN so.TrangThai = 'COMPLETED' THEN 'PAID'
        WHEN so.TrangThai = 'CANCELLED' THEN 'UNPAID'
        WHEN so.TrangThai IN ('CONFIRMED','SHIPPING','DELIVERED') AND so.PhuongThucThanhToan = 'BANK_TRANSFER' THEN 'PAID'
        ELSE 'UNPAID'
    END,
    so.TrangThai,
    so.HinhThucNhan,
    CASE 
        WHEN so.TrangThai = 'CANCELLED' AND so.Stt % 2 = 1 THEN 'BUYER'
        WHEN so.TrangThai = 'CANCELLED' AND so.Stt % 2 = 0 THEN 'SELLER'
        ELSE NULL
    END,
    CASE 
        WHEN so.TrangThai = 'CANCELLED' AND so.Stt % 2 = 1 THEN N'Thay đổi kế hoạch cá nhân, không có nhu cầu nhận đồ nữa.'
        WHEN so.TrangThai = 'CANCELLED' AND so.Stt % 2 = 0 THEN N'Sản phẩm bị thất lạc trong kho, xin lỗi người mua.'
        ELSE NULL
    END,
    CASE 
        WHEN so.TrangThai = 'CANCELLED' THEN DATEADD(HOUR, 4, DATEADD(DAY, -so.SoNgayTruoc, @Now))
        ELSE NULL
    END,
    DATEADD(DAY, -so.SoNgayTruoc, @Now),
    DATEADD(HOUR, -so.SoNgayTruoc * 2, @Now)
FROM @SeedOrders so
JOIN NguoiDung ndBuyer ON ndBuyer.Email = so.BuyerEmail
JOIN NguoiDung ndSeller ON ndSeller.Email = so.SellerEmail
JOIN CuaHang ch ON ch.MaNguoiDung = ndSeller.MaNguoiDung
WHERE NOT EXISTS (SELECT 1 FROM DonHang dh WHERE dh.MaDon = so.MaDon);

-- 5. Tạo Chi tiết đơn hàng (ChiTietDonHang)
INSERT ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, AnhDaiDien, Gia, DangGiuCho)
SELECT 
    dh.MaDonHang,
    sp.MaSanPham,
    sp.Ten,
    '/uploads/products/placeholder.svg',
    sp.Gia,
    CASE WHEN dh.TrangThai IN ('COMPLETED', 'CANCELLED') THEN 0 ELSE 1 END
FROM DonHang dh
JOIN @SeedOrders so ON so.MaDon = dh.MaDon
JOIN CuaHang ch ON ch.MaCuaHang = dh.MaCuaHang
JOIN SanPham sp ON sp.MaCuaHang = ch.MaCuaHang AND sp.Ten = so.TenSanPham
WHERE NOT EXISTS (SELECT 1 FROM ChiTietDonHang ct WHERE ct.MaDonHang = dh.MaDonHang);

-- 6. Tạo bản ghi Thanh toán (ThanhToan)
INSERT ThanhToan (MaDonHang, MaGiaoDich, PhuongThucThanhToan, SoTien, TrangThaiThanhToan, ThoiGianThanhToan, NgayTao)
SELECT 
    dh.MaDonHang,
    'TXN_' + dh.MaDon,
    dh.PhuongThucThanhToan,
    dh.TongTien,
    CASE 
        WHEN dh.TrangThaiThanhToan = 'PAID' THEN 'SUCCESS'
        WHEN dh.TrangThai = 'CANCELLED' THEN 'FAILED'
        ELSE 'PENDING'
    END,
    CASE 
        WHEN dh.TrangThaiThanhToan = 'PAID' THEN DATEADD(MINUTE, 30, dh.NgayTao)
        ELSE NULL
    END,
    dh.NgayTao
FROM DonHang dh
WHERE NOT EXISTS (SELECT 1 FROM ThanhToan tt WHERE tt.MaDonHang = dh.MaDonHang);

-- 7. Ghi Lịch sử chuyển trạng thái đơn hàng (LichSuTrangThaiDonHang)
-- Bước A: Tạo đơn ban đầu (PENDING)
INSERT LichSuTrangThaiDonHang (MaDonHang, TrangThaiCu, TrangThaiMoi, LoaiNguoiThucHien, NguoiThucHienId, LyDo, ThoiGian)
SELECT dh.MaDonHang, NULL, 'PENDING', 'BUYER', dh.MaNguoiDung, N'Khách hàng đặt hàng trực tuyến', dh.NgayTao
FROM DonHang dh
WHERE NOT EXISTS (
    SELECT 1 FROM LichSuTrangThaiDonHang ls 
    WHERE ls.MaDonHang = dh.MaDonHang AND ls.TrangThaiMoi = 'PENDING'
);

-- Bước B: CONFIRMED (cho đơn CONFIRMED, SHIPPING, DELIVERED, COMPLETED)
INSERT LichSuTrangThaiDonHang (MaDonHang, TrangThaiCu, TrangThaiMoi, LoaiNguoiThucHien, NguoiThucHienId, LyDo, ThoiGian)
SELECT dh.MaDonHang, 'PENDING', 'CONFIRMED', 'SELLER', ch.MaNguoiDung, N'Shop xác nhận đơn hàng và chuẩn bị giao dịch', DATEADD(HOUR, 2, dh.NgayTao)
FROM DonHang dh
JOIN CuaHang ch ON ch.MaCuaHang = dh.MaCuaHang
WHERE dh.TrangThai IN ('CONFIRMED', 'SHIPPING', 'DELIVERED', 'COMPLETED')
  AND NOT EXISTS (
      SELECT 1 FROM LichSuTrangThaiDonHang ls 
      WHERE ls.MaDonHang = dh.MaDonHang AND ls.TrangThaiMoi = 'CONFIRMED'
  );

-- Bước C: SHIPPING (cho đơn SHIPPING, DELIVERED, COMPLETED)
INSERT LichSuTrangThaiDonHang (MaDonHang, TrangThaiCu, TrangThaiMoi, LoaiNguoiThucHien, NguoiThucHienId, LyDo, ThoiGian)
SELECT dh.MaDonHang, 'CONFIRMED', 'SHIPPING', 'SELLER', ch.MaNguoiDung, 
       CASE WHEN dh.HinhThucNhan = 'GAP_TRUC_TIEP' THEN N'Người bán đang trên đường đến điểm hẹn gặp' ELSE N'Người bán đã bàn giao gói hàng cho đơn vị vận chuyển' END,
       DATEADD(HOUR, 6, dh.NgayTao)
FROM DonHang dh
JOIN CuaHang ch ON ch.MaCuaHang = dh.MaCuaHang
WHERE dh.TrangThai IN ('SHIPPING', 'DELIVERED', 'COMPLETED')
  AND NOT EXISTS (
      SELECT 1 FROM LichSuTrangThaiDonHang ls 
      WHERE ls.MaDonHang = dh.MaDonHang AND ls.TrangThaiMoi = 'SHIPPING'
  );

-- Bước D: DELIVERED (cho đơn DELIVERED, COMPLETED)
INSERT LichSuTrangThaiDonHang (MaDonHang, TrangThaiCu, TrangThaiMoi, LoaiNguoiThucHien, NguoiThucHienId, LyDo, ThoiGian)
SELECT dh.MaDonHang, 'SHIPPING', 'DELIVERED', 'SELLER', ch.MaNguoiDung, 
       CASE WHEN dh.HinhThucNhan = 'GAP_TRUC_TIEP' THEN N'Hai bên đã gặp mặt và kiểm tra sản phẩm' ELSE N'Đơn vị vận chuyển đã giao kiện hàng đến địa chỉ nhận' END,
       DATEADD(HOUR, 24, dh.NgayTao)
FROM DonHang dh
JOIN CuaHang ch ON ch.MaCuaHang = dh.MaCuaHang
WHERE dh.TrangThai IN ('DELIVERED', 'COMPLETED')
  AND NOT EXISTS (
      SELECT 1 FROM LichSuTrangThaiDonHang ls 
      WHERE ls.MaDonHang = dh.MaDonHang AND ls.TrangThaiMoi = 'DELIVERED'
  );

-- Bước E: COMPLETED (cho đơn COMPLETED)
INSERT LichSuTrangThaiDonHang (MaDonHang, TrangThaiCu, TrangThaiMoi, LoaiNguoiThucHien, NguoiThucHienId, LyDo, ThoiGian)
SELECT dh.MaDonHang, 'DELIVERED', 'COMPLETED', 'BUYER', dh.MaNguoiDung, N'Người mua xác nhận đã nhận hàng thành công và hài lòng', DATEADD(HOUR, 28, dh.NgayTao)
FROM DonHang dh
WHERE dh.TrangThai = 'COMPLETED'
  AND NOT EXISTS (
      SELECT 1 FROM LichSuTrangThaiDonHang ls 
      WHERE ls.MaDonHang = dh.MaDonHang AND ls.TrangThaiMoi = 'COMPLETED'
  );

-- Bước F: CANCELLED (cho đơn CANCELLED)
INSERT LichSuTrangThaiDonHang (MaDonHang, TrangThaiCu, TrangThaiMoi, LoaiNguoiThucHien, NguoiThucHienId, LyDo, ThoiGian)
SELECT dh.MaDonHang, 'PENDING', 'CANCELLED', COALESCE(dh.NguoiHuy, 'BUYER'), dh.MaNguoiDung, dh.LyDoHuy, DATEADD(HOUR, 4, dh.NgayTao)
FROM DonHang dh
WHERE dh.TrangThai = 'CANCELLED'
  AND NOT EXISTS (
      SELECT 1 FROM LichSuTrangThaiDonHang ls 
      WHERE ls.MaDonHang = dh.MaDonHang AND ls.TrangThaiMoi = 'CANCELLED'
  );

-- 8. Seed 4 Tranh chấp (TranhChap) mẫu
-- Tranh chấp 1: Đơn 10 (RESOLVED - Chấp thuận hoàn tiền)
IF EXISTS (SELECT 1 FROM DonHang WHERE MaDon = 'DH202611-000010') AND NOT EXISTS (SELECT 1 FROM TranhChap tc JOIN DonHang dh ON dh.MaDonHang = tc.MaDonHang WHERE dh.MaDon = 'DH202611-000010')
BEGIN
    INSERT TranhChap (MaDonHang, MaNguoiTao, LyDo, BangChungAnh, PhanHoiNguoiBan, TrangThai, PhanQuyet, GhiChuAdmin, NguoiXuLyId, NgayTao, NgayGiaiQuyet)
    SELECT dh.MaDonHang, dh.MaNguoiDung, N'Hàng nhận được bị nứt vỡ góc màn hình, không giống mô tả của shop.',
           '/uploads/disputes/demo-evidence-1.jpg', N'Shop đóng gói kỹ bọc xốp 3 lớp, có thể do shipper va chạm mạnh khi giao.',
           'RESOLVED', N'Chấp thuận khiếu nại: Hoàn tiền cho người mua và hủy đơn.', N'Admin đã xác minh ảnh chụp sản phẩm bị hư hỏng thật.',
           @AdminId, DATEADD(DAY, -40, @Now), DATEADD(DAY, -39, @Now)
    FROM DonHang dh WHERE dh.MaDon = 'DH202611-000010';
END;

-- Tranh chấp 2: Đơn 20 (RESOLVED - Bác bỏ khiếu nại, người mua khiếu nại vô lý)
IF EXISTS (SELECT 1 FROM DonHang WHERE MaDon = 'DH202611-000020') AND NOT EXISTS (SELECT 1 FROM TranhChap tc JOIN DonHang dh ON dh.MaDonHang = tc.MaDonHang WHERE dh.MaDon = 'DH202611-000020')
BEGIN
    INSERT TranhChap (MaDonHang, MaNguoiTao, LyDo, BangChungAnh, PhanHoiNguoiBan, TrangThai, PhanQuyet, GhiChuAdmin, NguoiXuLyId, NgayTao, NgayGiaiQuyet)
    SELECT dh.MaDonHang, dh.MaNguoiDung, N'Tôi đổi ý không thích màu áo này nữa, muốn trả hàng lấy lại tiền.',
           NULL, N'Sản phẩm gửi đúng size và đúng màu đăng bán trên tin.',
           'RESOLVED', N'Bác bỏ khiếu nại: Thanh toán hoàn tất cho người bán.', N'Lý do đổi ý cá nhân không thuộc trường hợp lỗi sản phẩm.',
           @AdminId, DATEADD(DAY, -30, @Now), DATEADD(DAY, -29, @Now)
    FROM DonHang dh WHERE dh.MaDon = 'DH202611-000020';
END;

-- Tranh chấp 3: Đơn 30 (OPEN - Đang chờ xử lý)
IF EXISTS (SELECT 1 FROM DonHang WHERE MaDon = 'DH202611-000030') AND NOT EXISTS (SELECT 1 FROM TranhChap tc JOIN DonHang dh ON dh.MaDonHang = tc.MaDonHang WHERE dh.MaDon = 'DH202611-000030')
BEGIN
    INSERT TranhChap (MaDonHang, MaNguoiTao, LyDo, BangChungAnh, PhanHoiNguoiBan, TrangThai, NgayTao)
    SELECT dh.MaDonHang, dh.MaNguoiDung, N'Sách bị thiếu 2 trang mục lục và dính mực lem.',
           '/uploads/disputes/demo-evidence-2.jpg', N'Shop sẽ kiểm tra lại bản in.',
           'OPEN', DATEADD(DAY, -2, @Now)
    FROM DonHang dh WHERE dh.MaDon = 'DH202611-000030';
END;

-- Tranh chấp 4: Đơn 40 (PROCESSING - Đang chờ phản hồi thêm)
IF EXISTS (SELECT 1 FROM DonHang WHERE MaDon = 'DH202611-000040') AND NOT EXISTS (SELECT 1 FROM TranhChap tc JOIN DonHang dh ON dh.MaDonHang = tc.MaDonHang WHERE dh.MaDon = 'DH202611-000040')
BEGIN
    INSERT TranhChap (MaDonHang, MaNguoiTao, LyDo, BangChungAnh, PhanHoiNguoiBan, TrangThai, NgayTao)
    SELECT dh.MaDonHang, dh.MaNguoiDung, N'Tai nghe bên trái bị rè khi vặn to âm lượng.',
           NULL, N'Đang xin video quay thực tế từ người mua.',
           'PROCESSING', DATEADD(DAY, -1, @Now)
    FROM DonHang dh WHERE dh.MaDon = 'DH202611-000040';
END;

-- 9. Seed 8 Báo cáo vi phạm (BaoCao) mẫu
-- Báo cáo 1: Báo cáo sản phẩm vi phạm (RESOLVED - Đã gỡ)
IF NOT EXISTS (SELECT 1 FROM BaoCao WHERE LyDo = N'Hàng giả/nhái thương hiệu Apple')
BEGIN
    DECLARE @SpId1 int = (SELECT TOP 1 MaSanPham FROM SanPham WHERE Ten LIKE N'%iPhone%' OR Ten LIKE N'%Tai nghe%');
    DECLARE @ReporterId1 int = (SELECT TOP 1 MaNguoiDung FROM NguoiDung WHERE VaiTro = 'USER');
    IF @SpId1 IS NOT NULL AND @ReporterId1 IS NOT NULL
        INSERT BaoCao (LoaiDoiTuong, MaDoiTuong, MaNguoiBaoCao, LyDo, MoTa, TrangThai, NguoiXuLyId, GhiChuXuLy, NgayTao, NgayXuLy)
        VALUES ('PRODUCT', @SpId1, @ReporterId1, N'Hàng giả/nhái thương hiệu Apple', N'Người bán đăng máy nhái vỏ nhưng ghi máy zin.', 'RESOLVED', @AdminId, N'Đã xác minh và gỡ bỏ sản phẩm khỏi sàn.', DATEADD(DAY, -15, @Now), DATEADD(DAY, -14, @Now));
END;

-- Báo cáo 2: Báo cáo sản phẩm vi phạm từ khóa cấm (PENDING)
IF NOT EXISTS (SELECT 1 FROM BaoCao WHERE LyDo = N'Nội dung sản phẩm không phù hợp')
BEGIN
    DECLARE @SpId2 int = (SELECT TOP 1 MaSanPham FROM SanPham ORDER BY MaSanPham DESC);
    DECLARE @ReporterId2 int = (SELECT TOP 1 MaNguoiDung FROM NguoiDung WHERE VaiTro = 'USER' ORDER BY MaNguoiDung DESC);
    IF @SpId2 IS NOT NULL AND @ReporterId2 IS NOT NULL
        INSERT BaoCao (LoaiDoiTuong, MaDoiTuong, MaNguoiBaoCao, LyDo, MoTa, TrangThai, NgayTao)
        VALUES ('PRODUCT', @SpId2, @ReporterId2, N'Nội dung sản phẩm không phù hợp', N'Mô tả sản phẩm chứa liên kết ngoài nghi ngờ lừa đảo.', 'PENDING', DATEADD(DAY, -2, @Now));
END;

-- Báo cáo 3: Báo cáo cửa hàng lừa đảo (PENDING)
IF NOT EXISTS (SELECT 1 FROM BaoCao WHERE LoaiDoiTuong = 'SHOP' AND LyDo = N'Cửa hàng có dấu hiệu gian lận tiền cọc')
BEGIN
    DECLARE @ShopId1 int = (SELECT TOP 1 MaCuaHang FROM CuaHang ORDER BY MaCuaHang);
    DECLARE @ReporterId3 int = (SELECT TOP 1 MaNguoiDung FROM NguoiDung WHERE VaiTro = 'USER' ORDER BY MaNguoiDung);
    IF @ShopId1 IS NOT NULL AND @ReporterId3 IS NOT NULL
        INSERT BaoCao (LoaiDoiTuong, MaDoiTuong, MaNguoiBaoCao, LyDo, MoTa, TrangThai, NgayTao)
        VALUES ('SHOP', @ShopId1, @ReporterId3, N'Cửa hàng có dấu hiệu gian lận tiền cọc', N'Shop yêu cầu nhắn tin Zalo chuyển cọc trước rồi mới gửi đồ.', 'PENDING', DATEADD(DAY, -1, @Now));
END;

-- Báo cáo 4: Báo cáo cửa hàng gửi đồ sai mô tả (RESOLVED)
IF NOT EXISTS (SELECT 1 FROM BaoCao WHERE LoaiDoiTuong = 'SHOP' AND LyDo = N'Cửa hàng bán đồ cũ kém chất lượng liên tục')
BEGIN
    DECLARE @ShopId2 int = (SELECT TOP 1 MaCuaHang FROM CuaHang ORDER BY MaCuaHang DESC);
    DECLARE @ReporterId4 int = (SELECT TOP 1 MaNguoiDung FROM NguoiDung WHERE VaiTro = 'USER' ORDER BY MaNguoiDung);
    IF @ShopId2 IS NOT NULL AND @ReporterId4 IS NOT NULL
        INSERT BaoCao (LoaiDoiTuong, MaDoiTuong, MaNguoiBaoCao, LyDo, MoTa, TrangThai, NguoiXuLyId, GhiChuXuLy, NgayTao, NgayXuLy)
        VALUES ('SHOP', @ShopId2, @ReporterId4, N'Cửa hàng bán đồ cũ kém chất lượng liên tục', N'Giao đồ cũ nát không sử dụng được, từ chối giải quyết.', 'RESOLVED', @AdminId, N'Đã cảnh cáo chủ shop lần 1.', DATEADD(DAY, -10, @Now), DATEADD(DAY, -9, @Now));
END;

-- Báo cáo 5: Báo cáo người dùng có hành vi xúc phạm (RESOLVED - Khóa user)
IF NOT EXISTS (SELECT 1 FROM BaoCao WHERE LoaiDoiTuong = 'USER' AND LyDo = N'Người dùng nhắn tin xúc phạm đe dọa')
BEGIN
    DECLARE @BadUserId int = (SELECT TOP 1 MaNguoiDung FROM NguoiDung WHERE Email = 'locked@demo.local');
    DECLARE @ReporterId5 int = (SELECT TOP 1 MaNguoiDung FROM NguoiDung WHERE VaiTro = 'USER' AND Email <> 'locked@demo.local');
    IF @BadUserId IS NOT NULL AND @ReporterId5 IS NOT NULL
        INSERT BaoCao (LoaiDoiTuong, MaDoiTuong, MaNguoiBaoCao, LyDo, MoTa, TrangThai, NguoiXuLyId, GhiChuXuLy, NgayTao, NgayXuLy)
        VALUES ('USER', @BadUserId, @ReporterId5, N'Người dùng nhắn tin xúc phạm đe dọa', N'Người dùng này liên tục nhắn tin chửi bới ép giảm giá khi mua hàng.', 'RESOLVED', @AdminId, N'Đã khóa tài khoản vi phạm vĩnh viễn.', DATEADD(DAY, -20, @Now), DATEADD(DAY, -19, @Now));
END;

-- Báo cáo 6: Báo cáo người dùng bom hàng (PENDING)
IF NOT EXISTS (SELECT 1 FROM BaoCao WHERE LoaiDoiTuong = 'USER' AND LyDo = N'Người mua đặt hàng nhưng không nhận nhiều lần')
BEGIN
    DECLARE @TargetUserId2 int = (SELECT TOP 1 MaNguoiDung FROM NguoiDung WHERE VaiTro = 'USER' ORDER BY MaNguoiDung DESC);
    DECLARE @ReporterId6 int = (SELECT TOP 1 MaNguoiDung FROM NguoiDung WHERE VaiTro = 'USER' ORDER BY MaNguoiDung);
    IF @TargetUserId2 IS NOT NULL AND @ReporterId6 IS NOT NULL AND @TargetUserId2 <> @ReporterId6
        INSERT BaoCao (LoaiDoiTuong, MaDoiTuong, MaNguoiBaoCao, LyDo, MoTa, TrangThai, NgayTao)
        VALUES ('USER', @TargetUserId2, @ReporterId6, N'Người mua đặt hàng nhưng không nhận nhiều lần', N'Hẹn gặp trực tiếp tại cổng trường UTC2 nhưng không đến và chặn số điện thoại.', 'PENDING', DATEADD(DAY, -3, @Now));
END;

-- Báo cáo 7: Báo cáo sản phẩm vi phạm giá ảo (DISMISSED - Bỏ qua vì không vi phạm)
IF NOT EXISTS (SELECT 1 FROM BaoCao WHERE LyDo = N'Nghi vấn giá ảo phá giá thị trường')
BEGIN
    DECLARE @SpId3 int = (SELECT TOP 1 MaSanPham FROM SanPham WHERE Gia < 100000);
    DECLARE @ReporterId7 int = (SELECT TOP 1 MaNguoiDung FROM NguoiDung WHERE VaiTro = 'USER');
    IF @SpId3 IS NOT NULL AND @ReporterId7 IS NOT NULL
        INSERT BaoCao (LoaiDoiTuong, MaDoiTuong, MaNguoiBaoCao, LyDo, MoTa, TrangThai, NguoiXuLyId, GhiChuXuLy, NgayTao, NgayXuLy)
        VALUES ('PRODUCT', @SpId3, @ReporterId7, N'Nghi vấn giá ảo phá giá thị trường', N'Sản phẩm bán giá quá rẻ so với hàng mới.', 'DISMISSED', @AdminId, N'Đồ cũ thanh lý giá rẻ là hợp lệ, không vi phạm.', DATEADD(DAY, -8, @Now), DATEADD(DAY, -7, @Now));
END;

-- Báo cáo 8: Báo cáo cửa hàng địa chỉ không đúng thực tế (DISMISSED)
IF NOT EXISTS (SELECT 1 FROM BaoCao WHERE LoaiDoiTuong = 'SHOP' AND LyDo = N'Địa chỉ cửa hàng không đúng')
BEGIN
    DECLARE @ShopId3 int = (SELECT TOP 1 MaCuaHang FROM CuaHang WHERE MaCuaHang > 1);
    DECLARE @ReporterId8 int = (SELECT TOP 1 MaNguoiDung FROM NguoiDung WHERE VaiTro = 'USER');
    IF @ShopId3 IS NOT NULL AND @ReporterId8 IS NOT NULL
        INSERT BaoCao (LoaiDoiTuong, MaDoiTuong, MaNguoiBaoCao, LyDo, MoTa, TrangThai, NguoiXuLyId, GhiChuXuLy, NgayTao, NgayXuLy)
        VALUES ('SHOP', @ShopId3, @ReporterId8, N'Địa chỉ cửa hàng không đúng', N'Tìm đến địa chỉ shop không thấy biển hiệu.', 'DISMISSED', @AdminId, N'Cửa hàng online cá nhân bán tại nhà trọ, không bắt buộc biển hiệu.', DATEADD(DAY, -5, @Now), DATEADD(DAY, -4, @Now));
END;

COMMIT TRANSACTION;
GO

PRINT N'Hoàn thành nạp dữ liệu mẫu 32_seed_don_hang.sql thành công!';
PRINT N' - Tổng số đơn hàng: 70 đơn (52 COMPLETED, 4 PENDING, 4 CONFIRMED, 4 SHIPPING, 2 DELIVERED, 4 CANCELLED).';
PRINT N' - Đầy đủ 2 hình thức: GIAO_HANG & GAP_TRUC_TIEP.';
PRINT N' - Đầy đủ 4 Tranh chấp (TranhChap) & 8 Báo cáo vi phạm (BaoCao).';
GO
