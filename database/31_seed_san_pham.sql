USE SecondHandMarketplace;
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ARITHABORT ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

-- =============================================================================
-- B8: Seed kho sản phẩm demo cho trang tìm kiếm và trang chi tiết.
--   - 80 tin đăng, rải đều cả 9 danh mục hệ thống (09_seed_data.sql), mỗi danh mục >= 6 tin.
--   - Giá phủ đủ 6 phân khúc của bộ lọc (60.000đ đến 14.500.000đ).
--   - Đủ 5 tình trạng NEW/LIKE_NEW/GOOD/FAIR/WORN và đủ trạng thái:
--     PENDING_REVIEW (chờ duyệt), ACTIVE (đang bán), RESERVED (đã giữ), SOLD (đã bán).
--   - Gắn vào 13 shop: 2 shop của 29_seed_demo_goc.sql + 11 shop của 30_seed_dia_chi_shop.sql.
--     Shop nguoiban12 cố ý để trống để test trang shop chưa có sản phẩm.
--   - Ảnh dùng lại 54 file có sẵn trong wwwroot/uploads/seed (mỗi tin 2 ảnh, ảnh 1 là ảnh chính).
--   - Chạy sau 29 và 30. Chạy lại không nhân đôi (đối chiếu theo MaCuaHang + Ten).
-- =============================================================================

IF NOT EXISTS (SELECT 1 FROM CuaHang c JOIN NguoiDung n ON n.MaNguoiDung = c.MaNguoiDung
               WHERE n.Email = 'nguoiban01@demo.local')
    THROW 51092, N'Chưa có shop của 30_seed_dia_chi_shop.sql. Chạy 29 rồi 30 trước khi chạy file này.', 1;
GO

DECLARE @Now datetime2(3) = SYSUTCDATETIME();
DECLARE @AdminId int = (SELECT TOP 1 MaNguoiDung FROM NguoiDung WHERE VaiTro = 'ADMIN' ORDER BY MaNguoiDung);

BEGIN TRANSACTION;

-- 1. Bảng tạm mô tả 80 tin đăng -------------------------------------------------
IF OBJECT_ID('tempdb..#SeedSp') IS NOT NULL DROP TABLE #SeedSp;
CREATE TABLE #SeedSp (
    Stt           int            NOT NULL PRIMARY KEY,
    EmailChuShop  varchar(100)   NOT NULL,
    TenDanhMuc    nvarchar(100)  NOT NULL,
    CodeTinhTrang varchar(20)    NOT NULL,
    Ten           nvarchar(200)  NOT NULL,
    MoTa          nvarchar(2000) NOT NULL,
    Gia           decimal(18,2)  NOT NULL,
    TrangThai     varchar(20)    NOT NULL,
    GioTruoc      int            NOT NULL
);

INSERT #SeedSp (Stt, EmailChuShop, TenDanhMuc, CodeTinhTrang, Ten, MoTa, Gia, TrangThai, GioTruoc) VALUES
-- Long Tech Cũ (nguoiban01) - đồ điện tử
 (1,  'nguoiban01@demo.local', N'Đồ điện tử',             'GOOD',     N'Điện thoại Samsung Galaxy A54 128GB',                 N'Máy còn đẹp, pin còn 88%, đã thay kính lưng vì nứt nhẹ. Kèm sạc nhanh, cáp và ốp lưng.', 4900000, 'ACTIVE',         210),
 (2,  'nguoiban01@demo.local', N'Đồ điện tử',             'LIKE_NEW', N'Laptop Asus VivoBook 14 Ryzen 5 RAM 8GB',              N'Mua đầu năm 2025, mới dùng ba tháng cho học tập, bàn phím chưa bóng. Còn hộp, sạc và túi chống sốc.', 9500000, 'ACTIVE',         190),
 (3,  'nguoiban01@demo.local', N'Đồ điện tử',             'FAIR',     N'iPad Gen 9 64GB WiFi viền bạc',                        N'Mặt kính có một vết trầy mảnh, cảm ứng và loa vẫn ổn. Bán kèm bút cảm ứng cổng sạc và kính cường lực.', 4200000, 'PENDING_REVIEW', 12),
 (4,  'nguoiban01@demo.local', N'Đồ điện tử',             'GOOD',     N'Màn hình Dell 24 inch Full HD 75Hz',                   N'Không điểm chết, chân xoay đã tháo rời, còn dây nguồn và dây HDMI. Phù hợp làm việc văn phòng.', 1800000, 'ACTIVE',         160),
 (5,  'nguoiban01@demo.local', N'Đồ điện tử',             'WORN',     N'Cây máy tính bàn i5 thế hệ 8 RAM 8GB SSD 240GB',        N'Vỏ máy ố vàng, quạt kêu to nhưng khởi động và chạy ổn định. Đã bán cho bạn sinh viên tháng trước.', 3500000, 'SOLD',           700),
 (6,  'nguoiban01@demo.local', N'Đồ điện tử',             'NEW',      N'Sạc dự phòng Anker 20000mAh chưa bóc seal',             N'Mua dư khi đi công tác nên chưa dùng lần nào, tem seal còn nguyên. Kèm cáp và túi vải.', 350000, 'RESERVED',       95),
 (7,  'nguoiban01@demo.local', N'Khác',                   'GOOD',     N'Đèn để bàn cảm ứng ba mức sáng',                       N'Đèn sáng đều, mạch cảm ứng nhạy, kèm adapter đúng chuẩn. Bán vì chuyển sang đèn kẹp bàn.', 220000, 'ACTIVE',         120),
-- Hà Vintage Closet (nguoiban02) - thời trang
 (8,  'nguoiban02@demo.local', N'Thời trang',             'LIKE_NEW', N'Váy denim chữ A cạp cao size S',                       N'May từ vải denim dày, mới mặc hai buổi đi tiệc, không phai màu. Có khóa kéo sau và túi trong.', 320000, 'ACTIVE',         140),
 (9,  'nguoiban02@demo.local', N'Thời trang',             'GOOD',     N'Áo blazer nữ công sở màu be size M',                    N'Form đứng, lớp lót còn tốt, một cúc ở tay bị lỏng nhẹ. Phù hợp mặc đi làm cả tuần.', 280000, 'ACTIVE',         260),
 (10, 'nguoiban02@demo.local', N'Thời trang',             'FAIR',     N'Túi xách da thật màu nâu quai đeo chéo',                 N'Da có vân xước tự nhiên, góc đáy hơi mòn, khóa và ngăn trong còn nguyên. Không còn hộp.', 450000, 'ACTIVE',         300),
 (11, 'nguoiban02@demo.local', N'Thời trang',             'LIKE_NEW', N'Giày cao gót mũi nhọn 5cm size 37',                     N'Đi được hai buổi, đế chưa mòn, gót có tiếng kêu rất nhỏ. Kèm hộp và túi bụi.', 250000, 'RESERVED',       180),
 (12, 'nguoiban02@demo.local', N'Thời trang',             'WORN',     N'Áo len cashmere cổ tim màu xám nhạt',                    N'Sợi len đã xù ở khuỷu tay nhưng chưa thủng lỗ, giặt tay nước lạnh. Bán vì không còn mặc vừa.', 180000, 'PENDING_REVIEW', 20),
 (13, 'nguoiban02@demo.local', N'Thời trang',             'GOOD',     N'Quần kaki nam ống suông lưng chun size 32',               N'Vải kaki mỏng, ít nhăn, đã cắt lai theo chiều cao 1m68. Khách đã nhận hàng và xác nhận đơn.', 200000, 'SOLD',           640),
 (14, 'nguoiban02@demo.local', N'Thời trang',             'NEW',      N'Khăn lụa vuông họa tiết hoa sen 70cm',                   N'Quà tặng chưa sử dụng, còn nhãn, kích thước 70x70cm, chạm mát, không xù sợi.', 120000, 'ACTIVE',         70),
-- Huy Gia Dụng (nguoiban03) - đồ gia dụng
 (15, 'nguoiban03@demo.local', N'Đồ gia dụng',            'GOOD',     N'Máy lọc không khí phòng dưới 40m2',                     N'Tấm lọc còn dùng tốt, cảm biến bụi hoạt động, vỏ có vệt xước nhẹ. Kèm một tấm lọc dự phòng.', 1600000, 'ACTIVE',         230),
 (16, 'nguoiban03@demo.local', N'Đồ gia dụng',            'FAIR',     N'Bếp từ đôi bảng điều khiển rời',                        N'Mặt kính nhiễm màu ở vùng nấu bên phải, hai bên đều sôi bình thường. Khách đã nhận hàng, hỗ trợ đổi lỗi trong 7 ngày.', 1300000, 'ACTIVE',       280),
 (17, 'nguoiban03@demo.local', N'Đồ gia dụng',            'LIKE_NEW', N'Ấm đun nước siêu tốc 1.7L tự ngắt',                     N'Sôi khoảng bốn phút, tự ngắt khi cạn, lòng ấm còn sáng. Dùng ở phòng trọ một năm, còn lưới lọc cặn.', 250000, 'PENDING_REVIEW', 16),
 (18, 'nguoiban03@demo.local', N'Đồ gia dụng',            'GOOD',     N'Bộ sáu hộp thủy tinh chịu nhiệt có nắp',                 N'Thủy tinh dày, không sứt mẻ, hai nắp hơi trầy. Dùng đựng thực phẩm nóng và đông lạnh đều ổn.', 190000, 'ACTIVE',         150),
 (19, 'nguoiban03@demo.local', N'Đồ gia dụng',            'WORN',     N'Máy hút bụi dạng đứng công suất lớn',                    N'Ống hút nứt một đoạn đã băng tạm, lực hút còn mạnh, túi bụi đã vệ sinh. Đã bán, khách tự sửa tiếp.', 600000, 'SOLD',           720),
 (20, 'nguoiban03@demo.local', N'Đồ gia dụng',            'GOOD',     N'Bàn ủi khô đế chống dây nhiệt ổn',                       N'Đế còn phẳng, không cháy vải, dây nguồn chuẩn. Bán vì chuyển sang dùng bàn ủi hơi nước.', 230000, 'ACTIVE',         190),
-- Mai Books (nguoiban04) - sách và văn phòng phẩm
 (21, 'nguoiban04@demo.local', N'Sách & văn phòng phẩm',  'GOOD',     N'Bộ giáo trình Kinh tế học vĩ mô hai tập',                N'Gáy sách còn chắc, có ghi chú bút chì ở một số trang, không thiếu chương nào.', 150000, 'ACTIVE',         320),
 (22, 'nguoiban04@demo.local', N'Sách & văn phòng phẩm',  'LIKE_NEW', N'Sách Tư duy phản biện bản in 2024',                     N'Mới đọc một lượt, trang sạch, bìa không gấp mép. Bán vì đã giữ bản mềm để mang đi.', 65000, 'ACTIVE',         110),
 (23, 'nguoiban04@demo.local', N'Sách & văn phòng phẩm',  'FAIR',     N'Truyện Sherlock Holmes trọn bộ bốn tập',                 N'Giấy hơi ngả vàng theo thời gian, đủ bốn tập, góc bìa mềm bị cong nhẹ. Đọc không sao.', 120000, 'ACTIVE',         400),
 (24, 'nguoiban04@demo.local', N'Sách & văn phòng phẩm',  'GOOD',     N'Combo năm vở kẻ ngang bìa cứng 200 trang',               N'Chưa viết chữ nào, mua dư khi chuẩn bị năm học mới, giấy mịn không thấm mực.', 60000, 'ACTIVE',         80),
 (25, 'nguoiban04@demo.local', N'Sách & văn phòng phẩm',  'LIKE_NEW', N'Bút máy nắp bấm ngòi M kèm lọ mực',                      N'Viết trơn, ngòi không xẻ, đã vệ sinh sạch. Kèm một lọ mực xanh và hộp giấy.', 280000, 'RESERVED',       170),
 (26, 'nguoiban04@demo.local', N'Sách & văn phòng phẩm',  'FAIR',     N'Giá sách gỗ thông để bàn ba tầng',                        N'Sơn bề mặt tróc ở một thanh ngang, kết cấu còn vững, kích thước 40x20x50cm.', 175000, 'ACTIVE',         240),
 (27, 'nguoiban04@demo.local', N'Sách & văn phòng phẩm',  'GOOD',     N'Bộ tài liệu luyện thi IELTS 18 kèm mã file nghe',         N'Sách mới khoảng 95%, mã nghe chưa kích hoạt, bìa không quăn mép. Khách đã nhận hàng.', 210000, 'SOLD',           610),
-- Khôi Sport (nguoiban05) - thể thao và dã ngoại
 (28, 'nguoiban05@demo.local', N'Thể thao & dã ngoại',    'GOOD',     N'Máy chạy bộ điện trong nhà gập được',                    N'Chạy êm, bảng mạch còn tốt, băng tải có vết mòn nhẹ ở giữa. Người mua tự thuê xe chở trong nội thành.', 6500000, 'ACTIVE',        350),
 (29, 'nguoiban05@demo.local', N'Thể thao & dã ngoại',    'LIKE_NEW', N'Thảm tập yoga 6mm kèm dây kháng lực',                    N'Dùng vài buổi ở nhà, không rách, cuộn lại vẫn gọn. Kèm hai dây kháng lực và túi đựng.', 150000, 'ACTIVE',         90),
 (30, 'nguoiban05@demo.local', N'Thể thao & dã ngoại',    'FAIR',     N'Bộ ba tạ tay bọc gang mỗi bên năm ký',                    N'Lớp cao su bọc trầy nhiều chỗ nhưng chưa nứt, trọng lượng vẫn chuẩn. Bán vì chuyển nhà.', 320000, 'ACTIVE',         200),
 (31, 'nguoiban05@demo.local', N'Thể thao & dã ngoại',    'GOOD',     N'Gậy golf sắt số 7 đã thay grip mới',                      N'Lưng gậy còn nét, grip vừa thay, cán không cong vênh. Phù hợp người mới tập đánh.', 1800000, 'PENDING_REVIEW', 22),
 (32, 'nguoiban05@demo.local', N'Thể thao & dã ngoại',    'GOOD',     N'Lều du lịch hai người chống mưa',                         N'Khung nhôm không gãy, vải có một mối chỉ sờn đã vá lại, xếp gọn vào túi tròn.', 750000, 'ACTIVE',         260),
 (33, 'nguoiban05@demo.local', N'Xe cộ & phụ kiện',        'FAIR',     N'Xe đạp gấp bánh 20 inch đã bảo dưỡng',                    N'Đã thay xích và bố thắng, líp có tiếng lách cách khi lên số. Khách đến lấy xe trực tiếp tại cửa hàng.', 1500000, 'SOLD',          520),
-- Lan Mẹ và Bé (nguoiban06) - đồ chơi và mẹ bé
 (34, 'nguoiban06@demo.local', N'Đồ chơi & mẹ bé',        'LIKE_NEW', N'Đồ chơi gỗ xếp hình 100 chi tiết',                       N'Đủ mảnh theo danh sách, sơn không bong, đã lau bằng khăn ẩm. Có túi vải đựng kèm.', 95000, 'ACTIVE',         100),
 (35, 'nguoiban06@demo.local', N'Đồ chơi & mẹ bé',        'GOOD',     N'Ghế ăn dặm nhựa nguyên khối có khay rời',                 N'Khay còn chắc, chân không nứt, vệ sinh sạch sẽ. Bé nhà dùng khoảng một năm.', 210000, 'ACTIVE',         220),
 (36, 'nguoiban06@demo.local', N'Đồ chơi & mẹ bé',        'FAIR',     N'Xe tập đi trẻ em có vòng nhạc phía trước',                 N'Bánh xoay còn mượt, khung gấp chắc, vòng nhạc có một nốt kêu nhỏ. Bán vì bé đã biết đi.', 300000, 'PENDING_REVIEW', 18),
 (37, 'nguoiban06@demo.local', N'Đồ chơi & mẹ bé',        'GOOD',     N'Lắp ráp kiểu 500 mảnh mô hình lâu đài có hộp',            N'Đủ mảnh, khớp nối còn chắc, hộp giấy bị móp một góc. Đã phân loại theo túi zip.', 380000, 'ACTIVE',         280),
 (38, 'nguoiban06@demo.local', N'Đồ chơi & mẹ bé',        'LIKE_NEW', N'Nôi điện tự đưa kèm màn chống muỗi',                      N'Dộng cơ chạy êm, nệm sạch không xẹp, màn không thủng. Bé đã ra nôi nên thanh lý.', 1200000, 'RESERVED',       130),
 (39, 'nguoiban06@demo.local', N'Thời trang',             'GOOD',     N'Set năm bộ đồ thu đông cho bé size 6',                    N'Vải cotton không xù, một bộ nhạt màu ở cổ, bốn bộ còn lại đều đẹp. Khách đã nhận hàng.', 160000, 'SOLD',           480),
-- Tuấn Nội Thất (nguoiban07) - nội thất
 (40, 'nguoiban07@demo.local', N'Nội thất',               'GOOD',     N'Sofa văng ba chỗ bọc nỉ màu xám',                        N'Đệm không xẹp, vải có một vết ố nhỏ ở tay tựa, khung gỗ còn chắc. Người mua tự thuê xe vận chuyển.', 3500000, 'ACTIVE',        300),
 (41, 'nguoiban07@demo.local', N'Nội thất',               'FAIR',     N'Bàn ăn mặt đá bốn ghế chân sắt',                         N'Mặt đá có vệt nhiệt tròn, chân ghế xước sơn, đế nhựa còn đủ bốn cái. Bán vì chuyển căn hộ nhỏ hơn.', 2800000, 'ACTIVE',       420),
 (42, 'nguoiban07@demo.local', N'Nội thất',               'LIKE_NEW', N'Tủ quần áo bốn cánh gỗ công nghiệp chống ẩm',              N'Lắp ráp một tháng rồi tháo vì không vừa phòng, ray trượt và bản lề đều còn mới. Có đầy đủ ốc vít.', 2400000, 'PENDING_REVIEW', 26),
 (43, 'nguoiban07@demo.local', N'Nội thất',               'GOOD',     N'Đèn thả trần gỗ thông ba bóng',                           N'Chóa gỗ không nứt, dây điện và đui còn tốt, chưa kèm bóng. Phù hợp bàn ăn nhỏ.', 450000, 'ACTIVE',         210),
 (44, 'nguoiban07@demo.local', N'Nội thất',               'WORN',     N'Kệ tivi gỗ xoan đào dài một mét sáu',                     N'Mặt kệ bạc màu ở giữa, ngăn kéo vẫn trượt, một tay nắm đã thay tạm. Khách đã nhận và lắp đặt.', 1100000, 'SOLD',         690),
 (45, 'nguoiban07@demo.local', N'Nội thất',               'GOOD',     N'Giường tầng sắt có lưới bảo vệ tầng trên',                  N'Không kêu cót két, sơn tĩnh điện còn khoảng 90%, lưới không rách. Nệm bán riêng.', 1750000, 'ACTIVE',         250),
 (46, 'nguoiban07@demo.local', N'Nội thất',               'LIKE_NEW', N'Ghế công thái học lưng lưới có tựa đầu',                   N'Piston nâng hạ bình thường, bánh xe không mòn, tay ghế còn bọc nỉ. Bán vì công ty đã trang bị ghế.', 1450000, 'RESERVED',     160),
-- Tâm Camera Hà Nội (nguoiban08) - thiết bị ảnh
 (47, 'nguoiban08@demo.local', N'Đồ điện tử',             'GOOD',     N'Máy ảnh Sony A6000 kèm ống kính 16-50mm',                 N'Cảm biến sạch, màn hình không ố, số ảnh đã chụp khoảng 12 nghìn. Kèm hai pin và sạc đôi.', 8900000, 'ACTIVE',        330),
 (48, 'nguoiban08@demo.local', N'Đồ điện tử',             'FAIR',     N'Ống kính 50mm f/1.8 lấy nét tự động',                     N'Khung xước sơn ở viền, vòng tiêu cự và lấy nét vẫn mượt, không mốc. Có nắp trước và nắp sau.', 2200000, 'ACTIVE',        270),
 (49, 'nguoiban08@demo.local', N'Đồ điện tử',             'LIKE_NEW', N'Tay chống rung máy ảnh không gương lật',                    N'Các trục cân bằng tốt, pin theo máy còn khỏe, tay cầm không lỏng. Mua để quay một dự án đã xong.', 4100000, 'PENDING_REVIEW', 30),
 (50, 'nguoiban08@demo.local', N'Đồ điện tử',             'GOOD',     N'Máy quay tay cầm độ phân giải Full HD',                    N'Zoom cơ không bị đứng, vỏ có dấu hiệu sử dụng, pin và dây HDMI đi kèm. Khách đã nhận máy.', 3000000, 'SOLD',          560),
 (51, 'nguoiban08@demo.local', N'Đồ điện tử',             'WORN',     N'Đèn flash studio kèm dù tán 80cm',                         N'Bóng flash đã thay một lần, nhiệt màu vẫn ổn, dù tán còn nguyên. Bán vì nâng cấp sang đèn LED.', 550000, 'ACTIVE',        380),
 (52, 'nguoiban08@demo.local', N'Khác',                   'GOOD',     N'Balo máy ảnh chống nước có áo mưa phủ',                     N'Ngăn mút xốp không lún, khóa kéo trơn, áo mưa phủ còn đủ. Đựng vừa hai ống kính và một máy.', 400000, 'ACTIVE',         140),
-- Châu Đồ Cổ (nguoiban09) - hàng sưu tầm
 (53, 'nguoiban09@demo.local', N'Khác',                   'FAIR',     N'Đồng hồ bàn cơ lên dây cót kiểu cổ',                       N'Chạy đúng giờ sau khi vệ sinh bộ lò xo, mặt số ố nhẹ, chân gỗ không nứt. Không còn hộp gốc.', 1200000, 'ACTIVE',        500),
 (54, 'nguoiban09@demo.local', N'Khác',                   'WORN',     N'Bình gốm men rạn cao 40cm',                               N'Men có đường rạn tự nhiên theo phong cách, miệng bình sứt một điểm nhỏ. Phù hợp trưng bày.', 650000, 'ACTIVE',         460),
 (55, 'nguoiban09@demo.local', N'Khác',                   'GOOD',     N'Tranh sơn mài phong cảnh phố cổ 60x80cm',                  N'Màu sơn còn bóng, khung gỗ không mối mọt, mặt kính phía trước nguyên vẹn. Treo được ngay.', 2500000, 'PENDING_REVIEW', 34),
 (56, 'nguoiban09@demo.local', N'Nội thất',               'FAIR',     N'Bàn trà gỗ hương kèm hai đôn nhỏ',                         N'Mặt bàn có vết nhiệt tròn, gỗ không cong vênh, hai đôn còn chắc. Bán kèm khăn phủ.', 1900000, 'ACTIVE',         520),
 (57, 'nguoiban09@demo.local', N'Khác',                   'GOOD',     N'Bộ tranh chữ thư pháp Phúc Lộc Thọ Khang',                  N'Viết tay trên giấy dó, đã phủ bóng, một khung hơi lỏng ở góc. Khách đã nhận và treo phòng khách.', 300000, 'SOLD',         600),
-- Hân Biển Xanh (nguoiban10) - đồ biển và du lịch
 (58, 'nguoiban10@demo.local', N'Thể thao & dã ngoại',    'GOOD',     N'Vợt padel mặt sợi carbon đã dùng mười trận',               N'Mặt vợt chưa phồng, quai cán còn dính, kèm bao đựng. Bán vì chuyển sang thuê vợt theo giờ.', 1800000, 'PENDING_REVIEW', 40),
 (59, 'nguoiban10@demo.local', N'Thời trang',             'LIKE_NEW', N'Áo khoác chống nước nữ có lót lưới',                      N'Khóa chống nước còn mượt, vải không thủng, mới mặc hai chuyến đi biển. Size M.', 260000, 'ACTIVE',         130),
 (60, 'nguoiban10@demo.local', N'Thời trang',             'GOOD',     N'Đồ bơi nữ hai mảnh size M còn đàn hồi',                    N'Vải không bai, đường may nguyên, đã giặt nước ngọt sau mỗi lần dùng. Không kèm hộp.', 150000, 'ACTIVE',         90),
 (61, 'nguoiban10@demo.local', N'Xe cộ & phụ kiện',        'FAIR',     N'Ván chèo hơi kèm bơm tay và dây an toàn',                  N'Van hơi không xì sau khi thử giữ 24 giờ, mặt ván trầy nhẹ. Khách đã nhận ván tại kho.', 4200000, 'SOLD',          540),
 (62, 'nguoiban10@demo.local', N'Thể thao & dã ngoại',    'GOOD',     N'Kính bơi kèm cặp mũi silicone',                           N'Mắt kính không mờ, dây đàn hồi tốt, đã thay cặp mũi mới. Vệ sinh bằng nước ấm sau khi dùng.', 90000, 'ACTIVE',         75),
-- Kiệt Miền Tây (nguoiban11) - xe và đồ dùng miền Tây
 (63, 'nguoiban11@demo.local', N'Xe cộ & phụ kiện',        'GOOD',     N'Xe máy số 110cc chính chủ một đời sử dụng',                N'Động cơ êm, nước sơn còn khoảng 85%, đã thay nhớt và lốp trước. Giấy tờ đầy đủ, công chứng tại quận.', 14500000, 'ACTIVE',       430),
 (64, 'nguoiban11@demo.local', N'Xe cộ & phụ kiện',        'FAIR',     N'Xe đạp điện đi được khoảng 45km mỗi lần sạc',              N'Pin còn khoảng 70% dung lượng gốc, tay ga mượt, yên có vết nứt nhỏ. Cần người tự chở về.', 6800000, 'PENDING_REVIEW', 48),
 (65, 'nguoiban11@demo.local', N'Xe cộ & phụ kiện',        'LIKE_NEW', N'Mũ bảo hiểm ba phần tư có kính chắn gió size L',           N'Vỏ không bị va chạm, mút bên trong còn đàn hồi, quai và khóa chắc. Mới dùng một mùa mưa.', 350000, 'ACTIVE',         110),
 (66, 'nguoiban11@demo.local', N'Xe cộ & phụ kiện',        'WORN',     N'Giá đỡ điện thoại gắn xe máy có cổng USB',                  N'Kẹp tay vẫn chặt, một ngàm nhựa đã hàn lại, dây nguồn đấu thẳng vào bình. Khách đã nhận hàng.', 120000, 'SOLD',         500),
 (67, 'nguoiban11@demo.local', N'Đồ gia dụng',            'GOOD',     N'Quạt trần ba cánh có điều khiển từ xa',                     N'Quay êm ở cả ba số, cánh không cong, remote còn pin. Tháo ra từ phòng khách cũ.', 1250000, 'ACTIVE',         300),
 (68, 'nguoiban11@demo.local', N'Khác',                   'GOOD',     N'Bơm lốp xe mini chạy điện 12V có đồng hồ',                   N'Đồng hồ chỉ gần đúng khi so với bơm trạm, dây nguồn theo thuốc lá xe còn tốt, vỏ trầy nhẹ.', 300000, 'PENDING_REVIEW', 28),
-- An Store (an.seller) - nhiều nhóm hàng
 (69, 'an.seller@demo.local',  N'Thời trang',             'GOOD',     N'Quần tây nam ống đứng lưng cao size 30',                    N'Vải kaki không bóng, đã cắt lai, một túi trượt vẫn kéo được. Mặc đi làm hai năm.', 180000, 'ACTIVE',         120),
 (70, 'an.seller@demo.local',  N'Thời trang',             'LIKE_NEW', N'Giày tây da bò cổ cao size 41 đã đánh xi',                  N'Da không nứt, đế cao su chưa mòn nhiều, dây còn nguyên. Kèm hộp và một hộp xi.', 400000, 'RESERVED',       150),
 (71, 'an.seller@demo.local',  N'Đồ điện tử',             'GOOD',     N'Đồng hồ thông minh viền nhôm dây thể thao',                  N'Màn hình không sọc, pin dùng khoảng một ngày, đã thay dây của hãng khác. Không còn hộp.', 3200000, 'ACTIVE',        240),
 (72, 'an.seller@demo.local',  N'Đồ điện tử',             'FAIR',     N'Loa Bluetooth cỡ lớn pin khoảng sáu tiếng',                  N'Vỏ loa có vết xước, màng loa không rách, nút nguồn hơi cứng. Âm trầm vẫn chắc.', 1400000, 'PENDING_REVIEW', 24),
 (73, 'an.seller@demo.local',  N'Xe cộ & phụ kiện',        'GOOD',     N'Xe đạp thể thao khung nhôm bánh 26 inch',                    N'Đã bảo dưỡng lốp và thắng, bộ sang số mượt, yên có vết ngồi. Bán vì chuyển sang chung cư.', 2100000, 'ACTIVE',        360),
 (74, 'an.seller@demo.local',  N'Đồ chơi & mẹ bé',        'GOOD',     N'Bập bênh gỗ trẻ em có tay vịn',                            N'Ghế gỗ không vênh, thanh trượt còn trơn, sơn không bong. Nhà có bé dùng một mùa rồi thôi.', 350000, 'ACTIVE',         170),
 (75, 'an.seller@demo.local',  N'Nội thất',               'GOOD',     N'Tủ giày nhựa ba cánh kiểu gấp hai tầng',                     N'Khung nhựa chắc, một cánh trượt nhẹ, bề mặt đã lau sạch. Khách nhận hàng và xác nhận dùng tốt.', 280000, 'SOLD',         620),
-- Bình Vintage (binh.seller) - gia dụng và nội thất nhỏ
 (76, 'binh.seller@demo.local', N'Đồ gia dụng',           'GOOD',     N'Máy giặt mini ba kg cho đồ trẻ em',                        N'Giặt êm, không rò nước, lồng inox còn sáng. Trước đây dùng riêng cho quần áo em bé.', 900000, 'ACTIVE',         200),
 (77, 'binh.seller@demo.local', N'Đồ gia dụng',           'LIKE_NEW', N'Bộ dao và thớt inox cho bếp gia đình',                      N'Lưỡi dao còn sắc, thớt chưa sâu rãnh, mua dư so với nhu cầu nên chưa dùng hết bộ.', 180000, 'PENDING_REVIEW', 32),
 (78, 'binh.seller@demo.local', N'Nội thất',              'FAIR',     N'Bộ bàn ghế nhựa xếp dùng cho ban công',                     N'Nhựa hơi phai màu do nắng, khớp xếp còn chắc, mặt bàn không nứt. Phù hợp hai người.', 300000, 'ACTIVE',         260),
 (79, 'binh.seller@demo.local', N'Sách & văn phòng phẩm', 'GOOD',     N'Máy in phun bốn màu bình mực liên tục',                     N'In đều không sọc ngang, đầu phun đã vệ sinh, còn hai bình mực. Bán vì chuyển sang máy in laser.', 1300000, 'ACTIVE',       340),
 (80, 'binh.seller@demo.local', N'Thể thao & dã ngoại',   'GOOD',     N'Đồng hồ đo nhịp tim chạy bộ kèm dây đeo ngực',               N'Bắt mạch nhanh, pin dây đeo còn tốt, màn hình không xước. Kèm cáp sạc micro.', 750000, 'SOLD',           580);

-- 2. Insert sản phẩm (đối chiếu theo shop + tên nên chạy lại không nhân đôi) ------
INSERT SanPham (MaCuaHang, MaDanhMuc, MaTinhTrang, Ten, ChuoiTimKiem, MoTa, Gia, TrangThai, NgayTao, NgayCapNhat)
SELECT ch.MaCuaHang, dm.MaDanhMuc, tt.MaTinhTrang, s.Ten, dbo.fn_ChuanHoaChuoi(s.Ten), s.MoTa, s.Gia, s.TrangThai,
       DATEADD(HOUR, -s.GioTruoc, @Now), DATEADD(HOUR, -s.GioTruoc, @Now)
FROM #SeedSp s
JOIN NguoiDung nd        ON nd.Email = s.EmailChuShop
JOIN CuaHang ch          ON ch.MaNguoiDung = nd.MaNguoiDung
JOIN DanhMuc dm          ON dm.Ten = s.TenDanhMuc
JOIN TinhTrangSanPham tt ON tt.Ma = s.CodeTinhTrang
WHERE NOT EXISTS (SELECT 1 FROM SanPham x WHERE x.MaCuaHang = ch.MaCuaHang AND x.Ten = s.Ten);

-- Lấy MaSanPham của 80 tin theo đúng thứ tự Stt để quay vòng file ảnh.
IF OBJECT_ID('tempdb..#SpB8') IS NOT NULL DROP TABLE #SpB8;
CREATE TABLE #SpB8 (Stt int NOT NULL PRIMARY KEY, MaSanPham int NOT NULL UNIQUE);
INSERT #SpB8 (Stt, MaSanPham)
SELECT s.Stt, sp.MaSanPham
FROM #SeedSp s
JOIN NguoiDung nd ON nd.Email = s.EmailChuShop
JOIN CuaHang ch   ON ch.MaNguoiDung = nd.MaNguoiDung
JOIN SanPham sp   ON sp.MaCuaHang = ch.MaCuaHang AND sp.Ten = s.Ten;

IF (SELECT COUNT(*) FROM #SpB8) <> (SELECT COUNT(*) FROM #SeedSp)
BEGIN
    ROLLBACK TRANSACTION;
    THROW 51093, N'Seed B8: khong insert du 80 san pham, kiem tra ten danh muc, tinh trang hoac shop.', 1;
END;

-- 3. Ảnh: dùng lại 54 file jpg có sẵn trong wwwroot/uploads/seed (sp001..sp027) ---
INSERT AnhSanPham (MaSanPham, DuongDan, ThuTu, LaAnhChinh, DungLuong)
SELECT x.MaSanPham,
       N'/uploads/seed/sp' + RIGHT('000' + CAST(1 + (x.Stt - 1) % 27 AS varchar(3)), 3)
           + N'-' + CAST(n.ThuTu AS varchar(1)) + N'.jpg',
       n.ThuTu,
       CASE WHEN n.ThuTu = 1 THEN 1 ELSE 0 END,
       200000 + (x.Stt % 27) * 1000 + n.ThuTu * 100
FROM #SpB8 x
CROSS JOIN (VALUES (1), (2)) n (ThuTu)
WHERE NOT EXISTS (SELECT 1 FROM AnhSanPham a WHERE a.MaSanPham = x.MaSanPham AND a.ThuTu = n.ThuTu);

-- 4. Lịch sử trạng thái để trang kiểm duyệt và lịch sử tin đăng có dữ liệu --------
INSERT LichSuTrangThaiSanPham (MaSanPham, TrangThaiCu, TrangThaiMoi, LoaiNguoiThucHien, NguoiThucHienId, LyDo, ThoiGian)
SELECT sp.MaSanPham, NULL, sp.TrangThai, 'SYSTEM', NULL, N'Dữ liệu mẫu B8', sp.NgayTao
FROM #SpB8 x
JOIN SanPham sp ON sp.MaSanPham = x.MaSanPham
WHERE NOT EXISTS (SELECT 1 FROM LichSuTrangThaiSanPham h WHERE h.MaSanPham = sp.MaSanPham);

-- 5. Lượt kiểm duyệt: tin đã duyệt thì ghi APPROVED, tin chờ duyệt để trống quyết định
INSERT LuotKiemDuyet (MaSanPham, ThoiGianGui, KetQuaRule, QuyetDinh, LyDo, MaQuanTriVien, ThoiGianQuyetDinh)
SELECT sp.MaSanPham, sp.NgayTao, N'{"passed":true,"errors":[]}',
       CASE WHEN sp.TrangThai = 'PENDING_REVIEW' THEN NULL ELSE 'APPROVED' END,
       NULL,
       CASE WHEN sp.TrangThai = 'PENDING_REVIEW' THEN NULL ELSE @AdminId END,
       CASE WHEN sp.TrangThai = 'PENDING_REVIEW' THEN NULL ELSE DATEADD(HOUR, 2, sp.NgayTao) END
FROM #SpB8 x
JOIN SanPham sp ON sp.MaSanPham = x.MaSanPham
WHERE NOT EXISTS (SELECT 1 FROM LuotKiemDuyet l WHERE l.MaSanPham = sp.MaSanPham);

COMMIT TRANSACTION;
GO

-- 6. Thống kê nhanh -------------------------------------------------------------
SELECT dm.Ten AS DanhMuc, COUNT(*) AS SoTin, MIN(sp.Gia) AS GiaThapNhat, MAX(sp.Gia) AS GiaCaoNhat
FROM SanPham sp
JOIN DanhMuc dm ON dm.MaDanhMuc = sp.MaDanhMuc
WHERE EXISTS (SELECT 1 FROM #SeedSp s WHERE s.Ten = sp.Ten)
GROUP BY dm.Ten
ORDER BY COUNT(*) DESC, dm.Ten;

SELECT sp.TrangThai, COUNT(*) AS SoTin
FROM SanPham sp
WHERE EXISTS (SELECT 1 FROM #SeedSp s WHERE s.Ten = sp.Ten)
GROUP BY sp.TrangThai
ORDER BY SoTin DESC;

SELECT tt.Ten AS TinhTrang, COUNT(*) AS SoTin
FROM SanPham sp
JOIN TinhTrangSanPham tt ON tt.MaTinhTrang = sp.MaTinhTrang
WHERE EXISTS (SELECT 1 FROM #SeedSp s WHERE s.Ten = sp.Ten)
GROUP BY tt.Ten, tt.ThuTuHienThi
ORDER BY tt.ThuTuHienThi;

SELECT CASE
           WHEN (SELECT COUNT(*) FROM SanPham sp WHERE EXISTS (SELECT 1 FROM #SeedSp s WHERE s.Ten = sp.Ten)) = 80
                AND (SELECT COUNT(DISTINCT sp.MaSanPham)
                     FROM SanPham sp
                     JOIN #SpB8 x ON x.MaSanPham = sp.MaSanPham
                     JOIN AnhSanPham a ON a.MaSanPham = sp.MaSanPham AND a.LaAnhChinh = 1) = 80
                AND (SELECT COUNT(DISTINCT sp.TrangThai) FROM SanPham sp
                     WHERE EXISTS (SELECT 1 FROM #SeedSp s WHERE s.Ten = sp.Ten)
                       AND sp.TrangThai IN ('PENDING_REVIEW','ACTIVE','RESERVED','SOLD')) = 4
           THEN N'ĐẠT: 80 tin B8, mỗi tin có ảnh chính, đủ 4 trạng thái duyệt'
           ELSE N'KIỂM TRA LẠI: thiếu tin, thiếu ảnh chính hoặc thiếu trạng thái'
       END AS KiemTra;
GO
