SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
USE SecondHandMarketplace;
GO

-- -----------------------------------------------------------------------------
-- Sửa toàn bộ đường dẫn ảnh sản phẩm (AnhSanPham & ChiTietDonHang) cho khớp
-- chính xác với thư mục ảnh trong wwwroot/uploads/products/<TenThuMuc> và
-- wwwroot/uploads/seed/. Chạy lại nhiều lần vẫn an toàn (idempotent).
-- -----------------------------------------------------------------------------

BEGIN TRANSACTION;

IF OBJECT_ID('tempdb..#ProductImageMap') IS NOT NULL DROP TABLE #ProductImageMap;
CREATE TABLE #ProductImageMap (
    MaSanPham int NOT NULL PRIMARY KEY,
    BaseDir   nvarchar(300) NOT NULL,
    IsFolder  bit NOT NULL -- 1: /uploads/products/<Folder>/1.jpg..3.jpg; 0: /uploads/seed/spXXX-1.jpg..2.jpg
);

INSERT INTO #ProductImageMap (MaSanPham, BaseDir, IsFolder)
SELECT sp.MaSanPham, m.BaseDir, m.IsFolder
FROM SanPham sp
CROSS APPLY (
    SELECT
        CASE
            -- 1. 119 sản phẩm có thư mục riêng trong wwwroot/uploads/products/<Folder>
            WHEN sp.Ten = N'Ấm siêu tốc inox 1.7L'                           THEN N'/uploads/products/Am-sieu-toc-inox-1-7L'
            WHEN sp.Ten = N'Áo hoodie nỉ màu kem size M'                     THEN N'/uploads/products/Ao-hoodie-ni-mau-kem-size-M'
            WHEN sp.Ten = N'Áo khoác bomber đen size M'                      THEN N'/uploads/products/Ao-khoac-bomber-den-size-M'
            WHEN sp.Ten = N'Áo khoác denim nam size L'                       THEN N'/uploads/products/Ao-khoac-denim-nam-size-L'
            WHEN sp.Ten = N'Áo sơ mi linen trắng size L'                     THEN N'/uploads/products/Ao-so-mi-linen-trang-size-L'
            WHEN sp.Ten = N'Áo thun cotton xanh navy size L'                 THEN N'/uploads/products/Ao-thun-cotton-xanh-navy-size-L'
            WHEN sp.Ten = N'Ba lô trekking 35L'                              THEN N'/uploads/products/Ba-lo-trekking-35L'
            WHEN sp.Ten = N'Baga sau xe đạp nhôm'                            THEN N'/uploads/products/Baga-sau-xe-dap-nhom'
            WHEN sp.Ten = N'Balo Herschel 15 inch màu xám'                   THEN N'/uploads/products/Balo-Herschel-15-inch-mau-xam'
            WHEN sp.Ten = N'Balo laptop chống nước'                          THEN N'/uploads/products/Balo-laptop-chong-nuoc'
            WHEN sp.Ten = N'Bàn học gỗ kèm ghế'                              THEN N'/uploads/products/Ban-hoc-go-kem-ghe'
            WHEN sp.Ten = N'Bàn là khô Panasonic'                            THEN N'/uploads/products/Ban-la-kho-Panasonic'
            WHEN sp.Ten = N'Bàn phím cơ Keychron K2'                         THEN N'/uploads/products/Ban-phim-co-Keychron-K2'
            WHEN sp.Ten = N'Bàn phím Logitech K380'                          THEN N'/uploads/products/Ban-phim-Logitech-K380'
            WHEN sp.Ten = N'Bàn trà gỗ thấp 80cm'                            THEN N'/uploads/products/Ban-tra-go-thap-80cm'
            WHEN sp.Ten = N'Bàn ủi hơi nước Tefal'                           THEN N'/uploads/products/Ban-ui-hoi-nuoc-Tefal'
            WHEN sp.Ten = N'Bảng vẽ Wacom One Small'                         THEN N'/uploads/products/Bang-ve-Wacom-One-Small'
            WHEN sp.Ten = N'Bình giữ nhiệt inox 750ml'                       THEN N'/uploads/products/Binh-giu-nhiet-inox-750ml'
            WHEN sp.Ten = N'Bình hoa gốm trang trí men xanh'                 THEN N'/uploads/products/Binh-hoa-gom-trang-tri-men-xanh'
            WHEN sp.Ten = N'Bộ bút highlight pastel 6 màu'                   THEN N'/uploads/products/Bo-but-highlight-pastel-6-mau'
            WHEN sp.Ten = N'Bộ cờ vua nam châm du lịch'                      THEN N'/uploads/products/Bo-co-vua-nam-cham-du-lich'
            WHEN sp.Ten = N'Bộ dụng cụ sửa xe đạp 16 món'                    THEN N'/uploads/products/Bo-dung-cu-sua-xe-dap-16-mon'
            WHEN sp.Ten = N'Bộ ly thủy tinh 6 chiếc'                         THEN N'/uploads/products/Bo-ly-thuy-tinh-6-chiec'
            WHEN sp.Ten = N'Bộ nồi inox 3 món'                               THEN N'/uploads/products/Bo-noi-inox-3-mon'
            WHEN sp.Ten = N'Bộ từ điển Anh Việt mini'                        THEN N'/uploads/products/Bo-tu-dien-Anh-Viet-mini'
            WHEN sp.Ten = N'Bộ xếp hình khối gỗ 50 chi tiết'                 THEN N'/uploads/products/Bo-xep-hinh-khoi-go-50-chi-tiet'
            WHEN sp.Ten = N'Bộ xếp hình Lego City'                           THEN N'/uploads/products/Bo-xep-hinh-Lego-City'
            WHEN sp.Ten = N'Bơm xe đạp mini hợp kim'                         THEN N'/uploads/products/Bom-xe-dap-mini-hop-kim'
            WHEN sp.Ten = N'Bóng đá size 5'                                  THEN N'/uploads/products/Bong-da-size-5'
            WHEN sp.Ten = N'Cây trầu bà lá xẻ chậu sứ'                       THEN N'/uploads/products/Cay-trau-ba-la-xe-chau-su'
            WHEN sp.Ten = N'Chân váy chữ A màu be size M'                    THEN N'/uploads/products/Chan-vay-chu-A-mau-be-size-M'
            WHEN sp.Ten = N'Chuột Logitech MX Master 2S'                     THEN N'/uploads/products/Chuot-Logitech-MX-Master-2S'
            WHEN sp.Ten = N'Combo 5 sách kỹ năng sống'                       THEN N'/uploads/products/Combo-5-sach-ky-nang-song'
            WHEN sp.Ten = N'Đàn ukulele soprano'                             THEN N'/uploads/products/Dan-ukulele-soprano'
            WHEN sp.Ten = N'Dây nhảy thể thao điều chỉnh'                    THEN N'/uploads/products/Day-nhay-the-thao-dieu-chinh'
            WHEN sp.Ten = N'Đèn bàn LED chống cận'                           THEN N'/uploads/products/Den-ban-LED-chong-can'
            WHEN sp.Ten = N'Đèn học chống cận kèm kẹp bàn'                   THEN N'/uploads/products/Den-hoc-chong-can-kem-kep-ban'
            WHEN sp.Ten = N'Đèn ngủ cảm ứng để bàn'                          THEN N'/uploads/products/Den-ngu-cam-ung-de-ban'
            WHEN sp.Ten = N'Đèn sàn phòng khách'                             THEN N'/uploads/products/Den-san-phong-khach'
            WHEN sp.Ten = N'Đèn xe đạp sạc USB'                              THEN N'/uploads/products/Den-xe-dap-sac-USB'
            WHEN sp.Ten = N'Địu em bé trợ lực'                               THEN N'/uploads/products/Diu-em-be-tro-luc'
            WHEN sp.Ten = N'Đồng hồ Casio F-91W'                             THEN N'/uploads/products/Dong-ho-Casio-F-91W'
            WHEN sp.Ten = N'Đồng hồ Casio vintage'                           THEN N'/uploads/products/Dong-ho-Casio-vintage'
            WHEN sp.Ten = N'Ghế ăn dặm gấp gọn cho bé'                       THEN N'/uploads/products/Ghe-an-dam-gap-gon-cho-be'
            WHEN sp.Ten = N'Ghế xoay văn phòng lưới'                         THEN N'/uploads/products/Ghe-xoay-van-phong-luoi'
            WHEN sp.Ten = N'Giá đỡ điện thoại xe máy'                        THEN N'/uploads/products/Gia-do-dien-thoai-xe-may'
            WHEN sp.Ten = N'Giá đỡ laptop nhôm gấp gọn'                      THEN N'/uploads/products/Gia-do-laptop-nhom-gap-gon'
            WHEN sp.Ten = N'Giá vẽ tranh gỗ gấp gọn'                         THEN N'/uploads/products/Gia-ve-tranh-go-gap-gon'
            WHEN sp.Ten = N'Giáo trình Lập trình Web trọn bộ'                THEN N'/uploads/products/Giao-trinh-Lap-trinh-Web-tron-bo'
            WHEN sp.Ten = N'Giày chạy bộ Adidas size 41'                     THEN N'/uploads/products/Giay-chay-bo-Adidas-size-41'
            WHEN sp.Ten = N'Giày Converse cổ thấp size 39'                   THEN N'/uploads/products/Giay-Converse-co-thap-size-39'
            WHEN sp.Ten = N'Giày sneaker trắng size 42'                      THEN N'/uploads/products/Giay-sneaker-trang-size-42'
            WHEN sp.Ten = N'Gối ôm và chăn mỏng'                             THEN N'/uploads/products/Goi-om-va-chan-mong'
            WHEN sp.Ten = N'Gương đứng khung gỗ'                             THEN N'/uploads/products/Guong-dung-khung-go'
            WHEN sp.Ten = N'Hộp đựng đồ nhựa 3 tầng'                         THEN N'/uploads/products/Hop-dung-do-nhua-3-tang'
            WHEN sp.Ten = N'iPad Gen 9 Wi-Fi 64GB'                           THEN N'/uploads/products/iPad-Gen-9-Wi-Fi-64GB'
            WHEN sp.Ten = N'iPhone 11 64GB màu đen'                          THEN N'/uploads/products/iPhone-11-64GB-mau-den'
            WHEN sp.Ten = N'iPhone 12 128GB màu xanh'                        THEN N'/uploads/products/iPhone-12-128GB-mau-xanh'
            WHEN sp.Ten = N'Kệ giày 4 tầng bằng thép'                        THEN N'/uploads/products/Ke-giay-4-tang-bang-thep'
            WHEN sp.Ten = N'Kệ sách 5 tầng'                                  THEN N'/uploads/products/Ke-sach-5-tang'
            WHEN sp.Ten = N'Kệ tivi gỗ công nghiệp 1m2'                      THEN N'/uploads/products/Ke-tivi-go-cong-nghiep-1m2'
            WHEN sp.Ten = N'Laptop Dell Latitude 7490 i5'                    THEN N'/uploads/products/Laptop-Dell-Latitude-7490-i5'
            WHEN sp.Ten = N'Laptop Lenovo ThinkPad T480'                     THEN N'/uploads/products/Laptop-Lenovo-ThinkPad-T480'
            WHEN sp.Ten = N'Lều cắm trại 2 người'                            THEN N'/uploads/products/Leu-cam-trai-2-nguoi'
            WHEN sp.Ten = N'Lò vi sóng Panasonic 20L'                        THEN N'/uploads/products/Lo-vi-song-Panasonic-20L'
            WHEN sp.Ten = N'Loa Bluetooth JBL Flip 4'                        THEN N'/uploads/products/Loa-Bluetooth-JBL-Flip-4'
            WHEN sp.Ten = N'Loa JBL Charge 4 màu đen'                        THEN N'/uploads/products/Loa-JBL-Charge-4-mau-den'
            WHEN sp.Ten = N'MacBook Air M1 8GB 256GB'                        THEN N'/uploads/products/MacBook-Air-M1-8GB-256GB'
            WHEN sp.Ten = N'Màn hình Dell 24 inch IPS'                       THEN N'/uploads/products/Man-hinh-Dell-24-inch-IPS'
            WHEN sp.Ten = N'Máy ảnh Canon EOS 700D kèm lens kit'             THEN N'/uploads/products/May-anh-Canon-EOS-700D-kem-lens-kit'
            WHEN sp.Ten = N'Máy ảnh Fujifilm X-T20 body'                     THEN N'/uploads/products/May-anh-Fujifilm-X-T20-body'
            WHEN sp.Ten = N'Máy chiếu mini Wanbo T2 Max'                     THEN N'/uploads/products/May-chieu-mini-Wanbo-T2-Max'
            WHEN sp.Ten = N'Máy chơi game Nintendo Switch Lite'              THEN N'/uploads/products/May-choi-game-Nintendo-Switch-Lite'
            WHEN sp.Ten = N'Máy ép trái cây Philips HR1811'                  THEN N'/uploads/products/May-ep-trai-cay-Philips-HR1811'
            WHEN sp.Ten = N'Máy hút bụi cầm tay Deerma DX700'                THEN N'/uploads/products/May-hut-bui-cam-tay-Deerma-DX700'
            WHEN sp.Ten = N'Máy lọc không khí Xiaomi 3H'                     THEN N'/uploads/products/May-loc-khong-khi-Xiaomi-3H'
            WHEN sp.Ten = N'Máy may mini gia đình'                           THEN N'/uploads/products/May-may-mini-gia-dinh'
            WHEN sp.Ten = N'Máy pha cà phê Delonghi nhỏ gọn'                 THEN N'/uploads/products/May-pha-ca-phe-Delonghi-nho-gon'
            WHEN sp.Ten = N'Máy sấy tóc Philips 1600W'                       THEN N'/uploads/products/May-say-toc-Philips-1600W'
            WHEN sp.Ten = N'Máy tính Casio FX-570VN Plus'                    THEN N'/uploads/products/May-tinh-Casio-FX-570VN-Plus'
            WHEN sp.Ten = N'Máy xay cầm tay LocknLock'                       THEN N'/uploads/products/May-xay-cam-tay-LocknLock'
            WHEN sp.Ten = N'Máy xay sinh tố Philips'                         THEN N'/uploads/products/May-xay-sinh-to-Philips'
            WHEN sp.Ten = N'Mũ bảo hiểm 3/4 màu xám'                         THEN N'/uploads/products/Mu-bao-hiem-3-4-mau-xam'
            WHEN sp.Ten = N'Mũ lưỡi trai màu đen'                            THEN N'/uploads/products/Mu-luoi-trai-mau-den'
            WHEN sp.Ten = N'Nệm topper cao su 1m6'                           THEN N'/uploads/products/Nem-topper-cao-su-1m6'
            WHEN sp.Ten = N'Nồi chiên không dầu 5L'                          THEN N'/uploads/products/Noi-chien-khong-dau-5L'
            WHEN sp.Ten = N'Nồi cơm điện Sharp 1.8L'                         THEN N'/uploads/products/Noi-com-dien-Sharp-1-8L'
            WHEN sp.Ten = N'Nôi em bé vải lưới di động'                      THEN N'/uploads/products/Noi-em-be-vai-luoi-di-dong'
            WHEN sp.Ten = N'Ổ cứng SSD Samsung 970 EVO 500GB'                THEN N'/uploads/products/O-cung-SSD-Samsung-970-EVO-500GB'
            WHEN sp.Ten = N'Ống kính Canon EF 50mm f1.8'                     THEN N'/uploads/products/Ong-kinh-Canon-EF-50mm-f1-8'
            WHEN sp.Ten = N'Quần jean xanh ống đứng size 30'                 THEN N'/uploads/products/Quan-jean-xanh-ong-dung-size-30'
            WHEN sp.Ten = N'Quạt bàn Toshiba 30cm'                           THEN N'/uploads/products/Quat-ban-Toshiba-30cm'
            WHEN sp.Ten = N'Quạt đứng Senko'                                 THEN N'/uploads/products/Quat-dung-Senko'
            WHEN sp.Ten = N'Rèm cửa màu kem 2 tấm'                           THEN N'/uploads/products/Rem-cua-mau-kem-2-tam'
            WHEN sp.Ten = N'Router Wi-Fi TP-Link Archer C6'                  THEN N'/uploads/products/Router-Wi-Fi-TP-Link-Archer-C6'
            WHEN sp.Ten = N'Sách Atomic Habits bản tiếng Việt'               THEN N'/uploads/products/Sach-Atomic-Habits-ban-tieng-Viet'
            WHEN sp.Ten = N'Samsung Galaxy S21 FE 5G'                        THEN N'/uploads/products/Samsung-Galaxy-S21-FE-5G'
            WHEN sp.Ten = N'Sổ planner A5 chưa dùng hết'                     THEN N'/uploads/products/So-planner-A5-chua-dung-het'
            WHEN sp.Ten = N'Tạ tay bọc cao su 5kg đôi'                       THEN N'/uploads/products/Ta-tay-boc-cao-su-5kg-doi'
            WHEN sp.Ten = N'Tai nghe AirPods Pro thế hệ 1'                   THEN N'/uploads/products/Tai-nghe-AirPods-Pro-the-he-1'
            WHEN sp.Ten = N'Tai nghe Sony WH-1000XM3'                        THEN N'/uploads/products/Tai-nghe-Sony-WH-1000XM3'
            WHEN sp.Ten = N'Tay cầm DualSense PS5 trắng'                     THEN N'/uploads/products/Tay-cam-DualSense-PS5-trang'
            WHEN sp.Ten = N'Thảm chơi gấp gọn 2 mặt'                         THEN N'/uploads/products/Tham-choi-gap-gon-2-mat'
            WHEN sp.Ten = N'Thảm yoga TPE 6mm'                               THEN N'/uploads/products/Tham-yoga-TPE-6mm'
            WHEN sp.Ten = N'Thắt lưng da nâu bản 3cm'                        THEN N'/uploads/products/That-lung-da-nau-ban-3cm'
            WHEN sp.Ten = N'Truyện Harry Potter tập 1-3'                     THEN N'/uploads/products/Truyen-Harry-Potter-tap-1-3'
            WHEN sp.Ten = N'Truyện tranh Doraemon trọn bộ'                   THEN N'/uploads/products/Truyen-tranh-Doraemon-tron-bo'
            WHEN sp.Ten = N'Tủ đầu giường 2 ngăn kéo'                        THEN N'/uploads/products/Tu-dau-giuong-2-ngan-keo'
            WHEN sp.Ten = N'Tủ quần áo vải 3 ngăn'                           THEN N'/uploads/products/Tu-quan-ao-vai-3-ngan'
            WHEN sp.Ten = N'Túi đeo chéo da nâu'                             THEN N'/uploads/products/Tui-deo-cheo-da-nau'
            WHEN sp.Ten = N'Túi tote canvas màu xanh rêu'                    THEN N'/uploads/products/Tui-tote-canvas-mau-xanh-reu'
            WHEN sp.Ten = N'Váy midi hoa nhí size S'                         THEN N'/uploads/products/Vay-midi-hoa-nhi-size-S'
            WHEN sp.Ten = N'Vợt cầu lông Yonex Astrox'                       THEN N'/uploads/products/Vot-cau-long-Yonex-Astrox'
            WHEN sp.Ten = N'Vợt tennis Wilson cũ'                            THEN N'/uploads/products/Vot-tennis-Wilson-cu'
            WHEN sp.Ten = N'Xe chòi chân trẻ em màu xanh'                    THEN N'/uploads/products/Xe-choi-chan-tre-em-mau-xanh'
            WHEN sp.Ten = N'Xe đạp địa hình Giant ATX'                       THEN N'/uploads/products/Xe-dap-dia-hinh-Giant-ATX'
            WHEN sp.Ten = N'Xe đạp gấp mini 20 inch'                         THEN N'/uploads/products/Xe-dap-gap-mini-20-inch'
            WHEN sp.Ten = N'Xe đẩy em bé gấp gọn'                            THEN N'/uploads/products/Xe-day-em-be-gap-gon'
            WHEN sp.Ten = N'Xiaomi Redmi Note 11 128GB'                      THEN N'/uploads/products/Xiaomi-Redmi-Note-11-128GB'

            -- 2. Các sản phẩm B8 khớp trực tiếp với thư mục ảnh trong wwwroot/uploads/products
            WHEN sp.Ten IN (N'Điện thoại Samsung Galaxy A54 128GB')          THEN N'/uploads/products/Samsung-Galaxy-S21-FE-5G'
            WHEN sp.Ten IN (N'Laptop Asus VivoBook 14 Ryzen 5 RAM 8GB')      THEN N'/uploads/products/Laptop-Lenovo-ThinkPad-T480'
            WHEN sp.Ten IN (N'Laptop Dell Latitude 7490 Core i5 RAM 8GB SSD 256GB') THEN N'/uploads/products/Laptop-Dell-Latitude-7490-i5'
            WHEN sp.Ten IN (N'iPad Gen 9 64GB WiFi viền bạc', N'Máy tính bảng iPad Gen 9 64GB WiFi viền bạc', N'iPad Pro 11 inch WiFi 128GB màu xám không gian') THEN N'/uploads/products/iPad-Gen-9-Wi-Fi-64GB'
            WHEN sp.Ten IN (N'Màn hình Dell 24 inch Full HD 75Hz', N'Màn hình vi tính Dell 24 inch Full HD chống lóa') THEN N'/uploads/products/Man-hinh-Dell-24-inch-IPS'
            WHEN sp.Ten IN (N'Đèn để bàn cảm ứng ba mức sáng', N'Đèn LED để bàn cảm ứng ba mức sáng chống cận') THEN N'/uploads/products/Den-ngu-cam-ung-de-ban'
            WHEN sp.Ten IN (N'Áo khoác denim vintage Levi''s dáng suông size L') THEN N'/uploads/products/Ao-khoac-denim-nam-size-L'
            WHEN sp.Ten IN (N'Váy denim chữ A cạp cao size S')               THEN N'/uploads/products/Chan-vay-chu-A-mau-be-size-M'
            WHEN sp.Ten IN (N'Áo blazer nữ công sở màu be size M')           THEN N'/uploads/products/Ao-khoac-bomber-den-size-M'
            WHEN sp.Ten IN (N'Túi xách da thật màu nâu quai đeo chéo', N'Túi xách nữ da thật quai đeo chéo màu nâu cổ điển') THEN N'/uploads/products/Tui-deo-cheo-da-nau'
            WHEN sp.Ten IN (N'Giày cao gót mũi nhọn 5cm size 37', N'Giày sneaker Converse Jack Purcell trắng cổ thấp size 42') THEN N'/uploads/products/Giay-Converse-co-thap-size-39'
            WHEN sp.Ten IN (N'Máy lọc không khí phòng dưới 40m2', N'Quạt tháp và lọc không khí gia đình phòng dưới 40m2') THEN N'/uploads/products/May-loc-khong-khi-Xiaomi-3H'
            WHEN sp.Ten IN (N'Bếp từ đôi bảng điều khiển rời', N'Bộ nồi chảo inox đáy từ cao cấp 3 đáy sáng bóng', N'Bộ nồi inox 3 món kèm nắp vung kính cường lực') THEN N'/uploads/products/Bo-noi-inox-3-mon'
            WHEN sp.Ten IN (N'Nồi chiên không dầu Philips 4.5L lòng nồi chống dính') THEN N'/uploads/products/Noi-chien-khong-dau-5L'
            WHEN sp.Ten IN (N'Ấm đun nước siêu tốc 1.7L tự ngắt')            THEN N'/uploads/products/Am-sieu-toc-inox-1-7L'
            WHEN sp.Ten IN (N'Máy hút bụi dạng đứng công suất lớn')          THEN N'/uploads/products/May-hut-bui-cam-tay-Deerma-DX700'
            WHEN sp.Ten IN (N'Bàn ủi khô đế chống dây nhiệt ổn')             THEN N'/uploads/products/Ban-la-kho-Panasonic'
            WHEN sp.Ten IN (N'Bàn ủi hơi nước cầm tay Tefal mặt đế chống dính') THEN N'/uploads/products/Ban-ui-hoi-nuoc-Tefal'
            WHEN sp.Ten IN (N'Trọn bộ truyện tranh Doraemon 45 tập tuổi thơ') THEN N'/uploads/products/Truyen-tranh-Doraemon-tron-bo'
            WHEN sp.Ten IN (N'Bộ giáo trình Kinh tế học vĩ mô hai tập', N'Bộ sách giáo trình Lập trình căn bản và Cấu trúc dữ liệu') THEN N'/uploads/products/Giao-trinh-Lap-trinh-Web-tron-bo'
            WHEN sp.Ten IN (N'Sách Tư duy phản biện bản in 2024', N'Sách Tư duy phản biện bản in mới xuất bản') THEN N'/uploads/products/Sach-Atomic-Habits-ban-tieng-Viet'
            WHEN sp.Ten IN (N'Truyện Sherlock Holmes trọn bộ bốn tập', N'Truyện Sherlock Holmes trọn bộ bốn tập kinh điển') THEN N'/uploads/products/Truyen-Harry-Potter-tap-1-3'
            WHEN sp.Ten IN (N'Combo năm vở kẻ ngang bìa cứng 200 trang')     THEN N'/uploads/products/So-planner-A5-chua-dung-het'
            WHEN sp.Ten IN (N'Giá sách gỗ thông để bàn ba tầng', N'Kệ sách gỗ thông để bàn 3 tầng lắp ghép nhỏ gọn') THEN N'/uploads/products/Ke-sach-5-tang'
            WHEN sp.Ten IN (N'Bộ tài liệu luyện thi IELTS 18 kèm mã file nghe', N'Bộ tài liệu luyện thi IELTS Cambridge trọn bộ kèm audio') THEN N'/uploads/products/Combo-5-sach-ky-nang-song'
            WHEN sp.Ten IN (N'Thảm tập yoga 6mm kèm dây kháng lực')          THEN N'/uploads/products/Tham-yoga-TPE-6mm'
            WHEN sp.Ten IN (N'Bộ ba tạ tay bọc gang mỗi bên năm ký')         THEN N'/uploads/products/Ta-tay-boc-cao-su-5kg-doi'
            WHEN sp.Ten IN (N'Lều du lịch hai người chống mưa')              THEN N'/uploads/products/Leu-cam-trai-2-nguoi'
            WHEN sp.Ten IN (N'Xe đạp gấp bánh 20 inch đã bảo dưỡng')         THEN N'/uploads/products/Xe-dap-gap-mini-20-inch'
            WHEN sp.Ten IN (N'Đồ chơi gỗ xếp hình 100 chi tiết')             THEN N'/uploads/products/Bo-xep-hinh-khoi-go-50-chi-tiet'
            WHEN sp.Ten IN (N'Ghế ăn dặm nhựa nguyên khối có khay rời')      THEN N'/uploads/products/Ghe-an-dam-gap-gon-cho-be'
            WHEN sp.Ten IN (N'Lắp ráp kiểu 500 mảnh mô hình lâu đài có hộp') THEN N'/uploads/products/Bo-xep-hinh-Lego-City'
            WHEN sp.Ten IN (N'Nôi điện tự đưa kèm màn chống muỗi')           THEN N'/uploads/products/Noi-em-be-vai-luoi-di-dong'
            WHEN sp.Ten IN (N'Kệ tivi gỗ xoan đào dài một mét sáu')          THEN N'/uploads/products/Ke-tivi-go-cong-nghiep-1m2'
            WHEN sp.Ten IN (N'Ghế công thái học lưng lưới có tựa đầu')       THEN N'/uploads/products/Ghe-xoay-van-phong-luoi'
            WHEN sp.Ten IN (N'Ống kính 50mm f/1.8 lấy nét tự động')          THEN N'/uploads/products/Ong-kinh-Canon-EF-50mm-f1-8'
            WHEN sp.Ten IN (N'Bình gốm men rạn cao 40cm')                    THEN N'/uploads/products/Binh-hoa-gom-trang-tri-men-xanh'
            WHEN sp.Ten IN (N'Bàn trà gỗ hương kèm hai đôn nhỏ')             THEN N'/uploads/products/Ban-tra-go-thap-80cm'
            WHEN sp.Ten IN (N'Mũ bảo hiểm ba phần tư có kính chắn gió size L') THEN N'/uploads/products/Mu-bao-hiem-3-4-mau-xam'
            WHEN sp.Ten IN (N'Giá đỡ điện thoại gắn xe máy có cổng USB')     THEN N'/uploads/products/Gia-do-dien-thoai-xe-may'
            WHEN sp.Ten IN (N'Bơm lốp xe mini chạy điện 12V có đồng hồ')     THEN N'/uploads/products/Bom-xe-dap-mini-hop-kim'
            WHEN sp.Ten IN (N'Loa Bluetooth cỡ lớn pin khoảng sáu tiếng')    THEN N'/uploads/products/Loa-JBL-Charge-4-mau-den'
            WHEN sp.Ten IN (N'Xe đạp thể thao khung nhôm bánh 26 inch')      THEN N'/uploads/products/Xe-dap-dia-hinh-Giant-ATX'

            -- 3. Các sản phẩm sinh từ 32_seed_don_hang (70 đơn hàng mẫu)
            WHEN sp.Ten LIKE N'%Giải tích%'                                  THEN N'/uploads/products/Giao-trinh-Lap-trinh-Web-tron-bo'
            WHEN sp.Ten LIKE N'%Cấu trúc dữ liệu%'                           THEN N'/uploads/products/Giao-trinh-Lap-trinh-Web-tron-bo'
            WHEN sp.Ten LIKE N'%Áo thun polo%'                               THEN N'/uploads/products/Ao-thun-cotton-xanh-navy-size-L'
            WHEN sp.Ten LIKE N'%Quần jean Levis%'                            THEN N'/uploads/products/Quan-jean-xanh-ong-dung-size-30'
            WHEN sp.Ten LIKE N'%Chuột không dây%'                            THEN N'/uploads/products/Chuot-Logitech-MX-Master-2S'
            WHEN sp.Ten LIKE N'%Bàn phím DareU%'                             THEN N'/uploads/products/Ban-phim-co-Keychron-K2'
            WHEN sp.Ten LIKE N'%Tai nghe gaming%'                            THEN N'/uploads/products/Tai-nghe-Sony-WH-1000XM3'
            WHEN sp.Ten LIKE N'%Ấm siêu tốc%'                                THEN N'/uploads/products/Am-sieu-toc-inox-1-7L'
            WHEN sp.Ten LIKE N'%Quạt bàn mini%'                              THEN N'/uploads/products/Quat-ban-Toshiba-30cm'
            WHEN sp.Ten LIKE N'%Balo đi học%'                                THEN N'/uploads/products/Ba-lo-trekking-35L'
            WHEN sp.Ten LIKE N'%Đồng hồ thể thao%'                           THEN N'/uploads/products/Dong-ho-Casio-F-91W'
            WHEN sp.Ten LIKE N'%Máy tính bỏ túi%'                            THEN N'/uploads/products/May-tinh-Casio-FX-570VN-Plus'
            WHEN sp.Ten LIKE N'%Đèn học%'                                    THEN N'/uploads/products/Den-hoc-chong-can-kem-kep-ban'
            WHEN sp.Ten LIKE N'%Nồi cơm điện%'                               THEN N'/uploads/products/Noi-com-dien-Sharp-1-8L'
            WHEN sp.Ten LIKE N'%Loa kéo%'                                    THEN N'/uploads/products/Loa-JBL-Charge-4-mau-den'

            -- 4. Các sản phẩm B8 còn lại dùng bộ ảnh chuyên biệt trong wwwroot/uploads/seed
            WHEN sp.Ten IN (N'Cây máy tính bàn i5 thế hệ 8 RAM 8GB SSD 240GB', N'Cây máy tính bàn Intel Core i5 RAM 8GB SSD 240GB') THEN N'/uploads/seed/sp005'
            WHEN sp.Ten IN (N'Sạc dự phòng Anker 20000mAh chưa bóc seal', N'Sạc dự phòng Anker 20000mAh hỗ trợ sạc nhanh') THEN N'/uploads/seed/sp083'
            WHEN sp.Ten IN (N'Áo len cashmere cổ tim màu xám nhạt', N'Áo len cashmere ấm áp cổ tròn màu xám nhạt size M') THEN N'/uploads/seed/sp085'
            WHEN sp.Ten IN (N'Quần kaki nam ống suông lưng chun size 32', N'Quần kaki nam ống suông màu be nhạt size 32') THEN N'/uploads/seed/sp086'
            WHEN sp.Ten IN (N'Khăn lụa vuông họa tiết hoa sen 70cm', N'Khăn lụa vuông cao cấp họa tiết hoa sen kích thước 70cm') THEN N'/uploads/seed/sp014'
            WHEN sp.Ten IN (N'Bộ sáu hộp thủy tinh chịu nhiệt có nắp', N'Bộ 6 hộp thủy tinh chịu nhiệt có nắp khóa khí kín') THEN N'/uploads/seed/sp018'
            WHEN sp.Ten IN (N'Bút máy nắp bấm ngòi M kèm lọ mực', N'Bút máy nắp bấm Jinhao ngòi M kèm piston hút mực') THEN N'/uploads/seed/sp025'
            WHEN sp.Ten = N'Máy chạy bộ điện trong nhà gập được'             THEN N'/uploads/seed/sp028'
            WHEN sp.Ten = N'Gậy golf sắt số 7 đã thay grip mới'              THEN N'/uploads/seed/sp031'
            WHEN sp.Ten = N'Xe tập đi trẻ em có vòng nhạc phía trước'        THEN N'/uploads/seed/sp036'
            WHEN sp.Ten = N'Set năm bộ đồ thu đông cho bé size 6'            THEN N'/uploads/seed/sp039'
            WHEN sp.Ten = N'Sofa văng ba chỗ bọc nỉ màu xám'                 THEN N'/uploads/seed/sp040'
            WHEN sp.Ten = N'Bàn ăn mặt đá bốn ghế chân sắt'                  THEN N'/uploads/seed/sp041'
            WHEN sp.Ten = N'Tủ quần áo bốn cánh gỗ công nghiệp chống ẩm'     THEN N'/uploads/seed/sp042'
            WHEN sp.Ten = N'Đèn thả trần gỗ thông ba bóng'                   THEN N'/uploads/seed/sp043'
            WHEN sp.Ten = N'Giường tầng sắt có lưới bảo vệ tầng trên'        THEN N'/uploads/seed/sp045'
            WHEN sp.Ten = N'Máy ảnh Sony A6000 kèm ống kính 16-50mm'         THEN N'/uploads/seed/sp047'
            WHEN sp.Ten = N'Tay chống rung máy ảnh không gương lật'          THEN N'/uploads/seed/sp049'
            WHEN sp.Ten = N'Máy quay tay cầm độ phân giải Full HD'           THEN N'/uploads/seed/sp050'
            WHEN sp.Ten = N'Đèn flash studio kèm dù tán 80cm'                THEN N'/uploads/seed/sp051'
            WHEN sp.Ten = N'Balo máy ảnh chống nước có áo mưa phủ'           THEN N'/uploads/seed/sp094'
            WHEN sp.Ten = N'Đồng hồ bàn cơ lên dây cót kiểu cổ'              THEN N'/uploads/seed/sp053'
            WHEN sp.Ten = N'Tranh sơn mài phong cảnh phố cổ 60x80cm'         THEN N'/uploads/seed/sp055'
            WHEN sp.Ten = N'Bộ tranh chữ thư pháp Phúc Lộc Thọ Khang'        THEN N'/uploads/seed/sp057'
            WHEN sp.Ten = N'Vợt padel mặt sợi carbon đã dùng mười trận'      THEN N'/uploads/seed/sp058'
            WHEN sp.Ten IN (N'Áo khoác chống nước nữ có lót lưới', N'Áo khoác gió nữ chống nước 2 lớp có lót lưới size M') THEN N'/uploads/seed/sp059'
            WHEN sp.Ten IN (N'Đồ bơi nữ hai mảnh size M còn đàn hồi', N'Kính mát phân cực chống tia UV đi biển') THEN N'/uploads/seed/sp060'
            WHEN sp.Ten = N'Ván chèo hơi kèm bơm tay và dây an toàn'         THEN N'/uploads/seed/sp061'
            WHEN sp.Ten = N'Kính bơi kèm cặp mũi silicone'                   THEN N'/uploads/seed/sp062'
            WHEN sp.Ten = N'Xe máy số 110cc chính chủ một đời sử dụng'       THEN N'/uploads/seed/sp063'
            WHEN sp.Ten = N'Xe đạp điện đi được khoảng 45km mỗi lần sạc'     THEN N'/uploads/seed/sp064'
            WHEN sp.Ten = N'Quạt trần ba cánh có điều khiển từ xa'           THEN N'/uploads/seed/sp067'
            WHEN sp.Ten = N'Quần tây nam ống đứng lưng cao size 30'          THEN N'/uploads/seed/sp069'
            WHEN sp.Ten = N'Giày tây da bò cổ cao size 41 đã đánh xi'        THEN N'/uploads/seed/sp070'
            WHEN sp.Ten = N'Đồng hồ thông minh viền nhôm dây thể thao'       THEN N'/uploads/seed/sp071'
            WHEN sp.Ten = N'Bập bênh gỗ trẻ em có tay vịn'                   THEN N'/uploads/seed/sp074'
            WHEN sp.Ten = N'Tủ giày nhựa ba cánh kiểu gấp hai tầng'          THEN N'/uploads/seed/sp075'
            WHEN sp.Ten = N'Máy giặt mini ba kg cho đồ trẻ em'               THEN N'/uploads/seed/sp076'
            WHEN sp.Ten = N'Bộ dao và thớt inox cho bếp gia đình'            THEN N'/uploads/seed/sp077'
            WHEN sp.Ten = N'Bộ bàn ghế nhựa xếp dùng cho ban công'           THEN N'/uploads/seed/sp078'
            WHEN sp.Ten = N'Máy in phun bốn màu bình mực liên tục'           THEN N'/uploads/seed/sp079'
            WHEN sp.Ten = N'Đồng hồ đo nhịp tim chạy bộ kèm dây đeo ngực'    THEN N'/uploads/seed/sp080'
            ELSE NULL
        END AS BaseDir
) rawMap
CROSS APPLY (
    SELECT
        rawMap.BaseDir,
        CASE WHEN rawMap.BaseDir LIKE N'/uploads/products/%' THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS IsFolder
) m
WHERE rawMap.BaseDir IS NOT NULL;

-- Cập nhật đường dẫn ảnh hiện có trong AnhSanPham
UPDATE a
SET a.DuongDan = CASE
    WHEN m.IsFolder = 1 THEN m.BaseDir + N'/' + CAST(CASE WHEN a.ThuTu BETWEEN 1 AND 3 THEN a.ThuTu ELSE 1 END AS nvarchar(10)) + N'.jpg'
    ELSE m.BaseDir + N'-' + CAST(CASE WHEN a.ThuTu BETWEEN 1 AND 2 THEN a.ThuTu ELSE 1 END AS nvarchar(10)) + N'.jpg'
END
FROM AnhSanPham a
JOIN #ProductImageMap m ON m.MaSanPham = a.MaSanPham;

-- Chèn bổ sung ảnh (1, 2, 3 cho thư mục /uploads/products; 1, 2 cho /uploads/seed) nếu sản phẩm chưa có ảnh (ví dụ 70 sản phẩm đơn hàng)
INSERT INTO AnhSanPham (MaSanPham, DuongDan, ThuTu, LaAnhChinh, DungLuong)
SELECT
    m.MaSanPham,
    CASE
        WHEN m.IsFolder = 1 THEN m.BaseDir + N'/' + CAST(n.ThuTu AS nvarchar(10)) + N'.jpg'
        ELSE m.BaseDir + N'-' + CAST(n.ThuTu AS nvarchar(10)) + N'.jpg'
    END,
    n.ThuTu,
    CASE WHEN n.ThuTu = 1 THEN 1 ELSE 0 END,
    210000 + (m.MaSanPham % 50) * 1000 + n.ThuTu * 100
FROM #ProductImageMap m
JOIN SanPham sp ON sp.MaSanPham = m.MaSanPham
CROSS JOIN (VALUES (1), (2), (3)) n (ThuTu)
WHERE sp.TrangThai <> 'DRAFT'
  AND (m.IsFolder = 1 OR n.ThuTu <= 2)
  AND NOT EXISTS (
      SELECT 1 FROM AnhSanPham a WHERE a.MaSanPham = m.MaSanPham AND a.ThuTu = n.ThuTu
  );

COMMIT TRANSACTION;
GO
