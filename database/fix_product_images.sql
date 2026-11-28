SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
USE SecondHandMarketplace;
GO

-- -----------------------------------------------------------------------------
-- Sửa toàn bộ đường dẫn ảnh sản phẩm (AnhSanPham & ChiTietDonHang) cho khớp
-- 100% với bộ thư mục ảnh thực tế trong wwwroot/uploads/products/<TenThuMuc>
-- (bao gồm đúng đuôi file .jpg/.png và đúng số lượng ảnh thực tế trong từng thư mục).
-- Chạy lại nhiều lần vẫn an toàn (idempotent).
-- -----------------------------------------------------------------------------

BEGIN TRANSACTION;

IF OBJECT_ID('tempdb..#ProductImageMap') IS NOT NULL DROP TABLE #ProductImageMap;
CREATE TABLE #ProductImageMap (
    MaSanPham int NOT NULL PRIMARY KEY,
    BaseDir   nvarchar(300) NOT NULL,
    Ext       nvarchar(10) NOT NULL,
    MaxImages int NOT NULL
);

INSERT INTO #ProductImageMap (MaSanPham, BaseDir, Ext, MaxImages)
SELECT sp.MaSanPham, m.BaseDir, m.Ext, m.MaxImages
FROM SanPham sp
CROSS APPLY (
    SELECT
        CASE
            -- 1. 120 sản phẩm gốc có thư mục riêng trong wwwroot/uploads/products/<Folder>
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
            WHEN sp.Ten = N'Bộ cờ vua gỗ (bản nháp)'                         THEN N'/uploads/products/Bo-co-vua-go-ban-nhap'
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
            WHEN sp.Ten = N'Máy xay sinh tố Philips'                         THEN N'/uploads/products/Máy-xay-sinh-to-Philips'
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

            -- 2. 80 sản phẩm mở rộng có thư mục riêng mới bổ sung trong wwwroot/uploads/products
            WHEN sp.Ten = N'Quần tây nam ống đứng lưng cao size 30'          THEN N'/uploads/products/Quan-tay-nam-ong-dung-lung-cao-size-30'
            WHEN sp.Ten = N'Giày tây da bò cổ cao size 41 đã đánh xi'        THEN N'/uploads/products/Giay-tay-da-bo-co-cao-size-41-da-danh-xi'
            WHEN sp.Ten = N'Đồng hồ thông minh viền nhôm dây thể thao'       THEN N'/uploads/products/dong-ho-thong-minh-vien-nhom-day-the-thao'
            WHEN sp.Ten = N'Loa Bluetooth cỡ lớn pin khoảng sáu tiếng'       THEN N'/uploads/products/Loa-Bluetooth-co-lon-pin-khoang-sau-tieng'
            WHEN sp.Ten = N'Xe đạp thể thao khung nhôm bánh 26 inch'         THEN N'/uploads/products/Xe-dap-the-thao-khung-nhom-banh-26-inch'
            WHEN sp.Ten = N'Bập bênh gỗ trẻ em có tay vịn'                   THEN N'/uploads/products/Bap-benh-go-tre-em-co-tay-vin'
            WHEN sp.Ten = N'Tủ giày nhựa ba cánh kiểu gấp hai tầng'          THEN N'/uploads/products/Tu-giay-nhua-ba-canh-kieu-gap-hai-tang'
            WHEN sp.Ten = N'Máy giặt mini ba kg cho đồ trẻ em'               THEN N'/uploads/products/May-giat-mini-ba-kg-cho-do-tre-em'
            WHEN sp.Ten = N'Bộ dao và thớt inox cho bếp gia đình'            THEN N'/uploads/products/Bo-dao-va-thot-inox-cho-bep-gia-dinh'
            WHEN sp.Ten = N'Bộ bàn ghế nhựa xếp dùng cho ban công'           THEN N'/uploads/products/Bo-ban-ghe-nhua-xep-dung-cho-ban-cong'
            WHEN sp.Ten = N'Máy in phun bốn màu bình mực liên tục'           THEN N'/uploads/products/May-in-phun-bon-mau-binh-muc-lien-tuc'
            WHEN sp.Ten = N'Đồng hồ đo nhịp tim chạy bộ kèm dây đeo ngực'    THEN N'/uploads/products/dong-ho-do-nhip-tim-chay-bo-kem-day-deo-nguc'
            WHEN sp.Ten = N'Điện thoại Samsung Galaxy A54 128GB'             THEN N'/uploads/products/dien-thoai-Samsung-Galaxy-A54-128GB'
            WHEN sp.Ten = N'Laptop Asus VivoBook 14 Ryzen 5 RAM 8GB'         THEN N'/uploads/products/Laptop-Asus-VivoBook-14-Ryzen-5-RAM-8GB'
            WHEN sp.Ten = N'iPad Gen 9 64GB WiFi viền bạc'                   THEN N'/uploads/products/iPad-Gen-9-64GB-WiFi-vien-bac'
            WHEN sp.Ten = N'Màn hình Dell 24 inch Full HD 75Hz'              THEN N'/uploads/products/Man-hinh-Dell-24-inch-Full-HD-75Hz'
            WHEN sp.Ten = N'Cây máy tính bàn i5 thế hệ 8 RAM 8GB SSD 240GB'  THEN N'/uploads/products/Cay-may-tinh-ban-i5-the-he-8-RAM-8GB-SSD-240GB'
            WHEN sp.Ten = N'Sạc dự phòng Anker 20000mAh chưa bóc seal'       THEN N'/uploads/products/Sac-du-phong-Anker-20000mAh-chua-boc-seal'
            WHEN sp.Ten = N'Đèn để bàn cảm ứng ba mức sáng'                  THEN N'/uploads/products/den-de-ban-cam-ung-ba-muc-sang'
            WHEN sp.Ten = N'Váy denim chữ A cạp cao size S'                  THEN N'/uploads/products/Vay-denim-chu-A-cap-cao-size-S'
            WHEN sp.Ten = N'Áo blazer nữ công sở màu be size M'              THEN N'/uploads/products/Ao-blazer-nu-cong-so-mau-be-size-M'
            WHEN sp.Ten = N'Túi xách da thật màu nâu quai đeo chéo'          THEN N'/uploads/products/Tui-xach-da-that-mau-nau-quai-deo-cheo'
            WHEN sp.Ten = N'Giày cao gót mũi nhọn 5cm size 37'               THEN N'/uploads/products/Giay-cao-got-mui-nhon-5cm-size-37'
            WHEN sp.Ten = N'Áo len cashmere cổ tim màu xám nhạt'             THEN N'/uploads/products/Ao-len-cashmere-co-tim-mau-xam-nhat'
            WHEN sp.Ten = N'Quần kaki nam ống suông lưng chun size 32'       THEN N'/uploads/products/Quan-kaki-nam-ong-suong-lung-chun-size-32'
            WHEN sp.Ten = N'Khăn lụa vuông họa tiết hoa sen 70cm'            THEN N'/uploads/products/Khan-lua-vuong-hoa-tiet-hoa-sen-70cm'
            WHEN sp.Ten = N'Máy lọc không khí phòng dưới 40m2'               THEN N'/uploads/products/May-loc-khong-khi-phong-duoi-40m2'
            WHEN sp.Ten = N'Bếp từ đôi bảng điều khiển rời'                  THEN N'/uploads/products/Bep-tu-doi-bang-dieu-khien-roi'
            WHEN sp.Ten = N'Ấm đun nước siêu tốc 1.7L tự ngắt'               THEN N'/uploads/products/Am-dun-nuoc-sieu-toc-1-7L-tu-ngat'
            WHEN sp.Ten = N'Bộ sáu hộp thủy tinh chịu nhiệt có nắp'          THEN N'/uploads/products/Bo-sau-hop-thuy-tinh-chiu-nhiet-co-nap'
            WHEN sp.Ten = N'Máy hút bụi dạng đứng công suất lớn'             THEN N'/uploads/products/May-hut-bui-dang-dung-cong-suat-lon'
            WHEN sp.Ten = N'Bàn ủi khô đế chống dây nhiệt ổn'                THEN N'/uploads/products/Ban-ui-kho-de-chong-day-nhiet-on'
            WHEN sp.Ten = N'Bộ giáo trình Kinh tế học vĩ mô hai tập'         THEN N'/uploads/products/Bo-giao-trinh-Kinh-te-hoc-vi-mo-hai-tap'
            WHEN sp.Ten = N'Sách Tư duy phản biện bản in 2024'               THEN N'/uploads/products/Sach-Tu-duy-phan-bien-ban-in-2024'
            WHEN sp.Ten = N'Truyện Sherlock Holmes trọn bộ bốn tập'          THEN N'/uploads/products/Truyen-Sherlock-Holmes-tron-bo-bon-tap'
            WHEN sp.Ten = N'Combo năm vở kẻ ngang bìa cứng 200 trang'        THEN N'/uploads/products/Combo-nam-vo-ke-ngang-bia-cung-200-trang'
            WHEN sp.Ten = N'Bút máy nắp bấm ngòi M kèm lọ mực'               THEN N'/uploads/products/But-may-nap-bam-ngoi-M-kem-lo-muc'
            WHEN sp.Ten = N'Giá sách gỗ thông để bàn ba tầng'                THEN N'/uploads/products/Gia-sach-go-thong-de-ban-ba-tang'
            WHEN sp.Ten = N'Bộ tài liệu luyện thi IELTS 18 kèm mã file nghe' THEN N'/uploads/products/Bo-tai-lieu-luyen-thi-IELTS-18-kem-ma-file-nghe'
            WHEN sp.Ten = N'Máy chạy bộ điện trong nhà gập được'             THEN N'/uploads/products/May-chay-bo-dien-trong-nha-gap-duoc'
            WHEN sp.Ten = N'Thảm tập yoga 6mm kèm dây kháng lực'             THEN N'/uploads/products/Tham-tap-yoga-6mm-kem-day-khang-luc'
            WHEN sp.Ten = N'Bộ ba tạ tay bọc gang mỗi bên năm ký'            THEN N'/uploads/products/Bo-ba-ta-tay-boc-gang-moi-ben-nam-ky'
            WHEN sp.Ten = N'Gậy golf sắt số 7 đã thay grip mới'              THEN N'/uploads/products/Gay-golf-sat-so-7-da-thay-grip-moi'
            WHEN sp.Ten = N'Lều du lịch hai người chống mưa'                 THEN N'/uploads/products/Leu-du-lich-hai-nguoi-chong-mua'
            WHEN sp.Ten = N'Xe đạp gấp bánh 20 inch đã bảo dưỡng'            THEN N'/uploads/products/Xe-dap-gap-banh-20-inch-da-bao-duong'
            WHEN sp.Ten = N'Đồ chơi gỗ xếp hình 100 chi tiết'                THEN N'/uploads/products/do-choi-go-xep-hinh-100-chi-tiet'
            WHEN sp.Ten = N'Ghế ăn dặm nhựa nguyên khối có khay rời'         THEN N'/uploads/products/Ghe-an-dam-nhua-nguyen-khoi-co-khay-roi'
            WHEN sp.Ten = N'Xe tập đi trẻ em có vòng nhạc phía trước'        THEN N'/uploads/products/Xe-tap-di-tre-em-co-vong-nhac-phia-truoc'
            WHEN sp.Ten = N'Lắp ráp kiểu 500 mảnh mô hình lâu đài có hộp'    THEN N'/uploads/products/Lap-rap-kieu-500-manh-mo-hinh-lau-dai-co-hop'
            WHEN sp.Ten = N'Nôi điện tự đưa kèm màn chống muỗi'              THEN N'/uploads/products/Noi-dien-tu-dua-kem-man-chong-muoi'
            WHEN sp.Ten = N'Set năm bộ đồ thu đông cho bé size 6'            THEN N'/uploads/products/Set-nam-bo-do-thu-dong-cho-be-size-6'
            WHEN sp.Ten = N'Sofa văng ba chỗ bọc nỉ màu xám'                 THEN N'/uploads/products/Sofa-vang-ba-cho-boc-ni-mau-xam'
            WHEN sp.Ten = N'Bàn ăn mặt đá bốn ghế chân sắt'                  THEN N'/uploads/products/Ban-an-mat-da-bon-ghe-chan-sat'
            WHEN sp.Ten = N'Tủ quần áo bốn cánh gỗ công nghiệp chống ẩm'     THEN N'/uploads/products/Tu-quan-ao-bon-canh-go-cong-nghiep-chong-am'
            WHEN sp.Ten = N'Đèn thả trần gỗ thông ba bóng'                   THEN N'/uploads/products/den-tha-tran-go-thong-ba-bong'
            WHEN sp.Ten = N'Kệ tivi gỗ xoan đào dài một mét sáu'             THEN N'/uploads/products/Ke-tivi-go-xoan-dao-dai-mot-met-sau'
            WHEN sp.Ten = N'Giường tầng sắt có lưới bảo vệ tầng trên'        THEN N'/uploads/products/Giuong-tang-sat-co-luoi-bao-ve-tang-tren'
            WHEN sp.Ten = N'Ghế công thái học lưng lưới có tựa đầu'          THEN N'/uploads/products/Ghe-cong-thai-hoc-lung-luoi-co-tua-dau'
            WHEN sp.Ten = N'Máy ảnh Sony A6000 kèm ống kính 16-50mm'         THEN N'/uploads/products/May-anh-Sony-A6000-kem-ong-kinh-16-50mm'
            WHEN sp.Ten = N'Ống kính 50mm f/1.8 lấy nét tự động'             THEN N'/uploads/products/Ong-kinh-50mm-f-1-8-lay-net-tu-dong'
            WHEN sp.Ten = N'Tay chống rung máy ảnh không gương lật'          THEN N'/uploads/products/Tay-chong-rung-may-anh-khong-guong-lat'
            WHEN sp.Ten = N'Máy quay tay cầm độ phân giải Full HD'           THEN N'/uploads/products/May-quay-tay-cam-do-phan-giai-Full-HD'
            WHEN sp.Ten = N'Đèn flash studio kèm dù tán 80cm'                THEN N'/uploads/products/den-flash-studio-kem-du-tan-80cm'
            WHEN sp.Ten = N'Balo máy ảnh chống nước có áo mưa phủ'           THEN N'/uploads/products/Balo-may-anh-chong-nuoc-co-ao-mua-phu'
            WHEN sp.Ten = N'Đồng hồ bàn cơ lên dây cót kiểu cổ'              THEN N'/uploads/products/dong-ho-ban-co-len-day-cot-kieu-co'
            WHEN sp.Ten = N'Bình gốm men rạn cao 40cm'                       THEN N'/uploads/products/Binh-gom-men-ran-cao-40cm'
            WHEN sp.Ten = N'Tranh sơn mài phong cảnh phố cổ 60x80cm'         THEN N'/uploads/products/Tranh-son-mai-phong-canh-pho-co-60x80cm'
            WHEN sp.Ten = N'Bàn trà gỗ hương kèm hai đôn nhỏ'                THEN N'/uploads/products/Ban-tra-go-huong-kem-hai-don-nho'
            WHEN sp.Ten = N'Bộ tranh chữ thư pháp Phúc Lộc Thọ Khang'        THEN N'/uploads/products/Bo-tranh-chu-thu-phap-Phuc-Loc-Tho-Khang'
            WHEN sp.Ten = N'Vợt padel mặt sợi carbon đã dùng mười trận'      THEN N'/uploads/products/Vot-padel-mat-soi-carbon-da-dung-muoi-tran'
            WHEN sp.Ten = N'Áo khoác chống nước nữ có lót lưới'              THEN N'/uploads/products/Ao-khoac-chong-nuoc-nu-co-lot-luoi'
            WHEN sp.Ten = N'Đồ bơi nữ hai mảnh size M còn đàn hồi'           THEN N'/uploads/products/do-boi-nu-hai-manh-size-M-con-dan-hoi'
            WHEN sp.Ten = N'Ván chèo hơi kèm bơm tay và dây an toàn'         THEN N'/uploads/products/Van-cheo-hoi-kem-bom-tay-va-day-an-toan'
            WHEN sp.Ten = N'Kính bơi kèm cặp mũi silicone'                   THEN N'/uploads/products/Kinh-boi-kem-cap-mui-silicone'
            WHEN sp.Ten = N'Xe máy số 110cc chính chủ một đời sử dụng'       THEN N'/uploads/products/Xe-may-so-110cc-chinh-chu-mot-doi-su-dung'
            WHEN sp.Ten = N'Xe đạp điện đi được khoảng 45km mỗi lần sạc'     THEN N'/uploads/products/Xe-dap-dien-di-duoc-khoang-45km-moi-lan-sac'
            WHEN sp.Ten = N'Mũ bảo hiểm ba phần tư có kính chắn gió size L'  THEN N'/uploads/products/Mu-bao-hiem-ba-phan-tu-co-kinh-chan-gio-size-L'
            WHEN sp.Ten = N'Giá đỡ điện thoại gắn xe máy có cổng USB'        THEN N'/uploads/products/Gia-do-dien-thoai-gan-xe-may-co-cong-USB'
            WHEN sp.Ten = N'Quạt trần ba cánh có điều khiển từ xa'           THEN N'/uploads/products/Quat-tran-ba-canh-co-dieu-khien-tu-xa'
            WHEN sp.Ten = N'Bơm lốp xe mini chạy điện 12V có đồng hồ'        THEN N'/uploads/products/Bom-lop-xe-mini-chay-dien-12V-co-dong-ho'

            -- 3. 70 sản phẩm đơn hàng mẫu (#1..#70) đều có thư mục riêng mới bổ sung
            WHEN sp.Ten LIKE N'Sách Giải tích 1 ĐH Bách Khoa%'               THEN N'/uploads/products/Sach-Giai-tich-1-dH-Bach-Khoa'
            WHEN sp.Ten LIKE N'Giáo trình Cấu trúc dữ liệu và giải thuật%'   THEN N'/uploads/products/Giao-trinh-Cau-truc-du-lieu-va-giai-thuat'
            WHEN sp.Ten LIKE N'Áo thun polo Uniqlo size M màu navy%'         THEN N'/uploads/products/Ao-thun-polo-Uniqlo-size-M-mau-navy'
            WHEN sp.Ten LIKE N'Quần jean Levis 511 size 31%'                 THEN N'/uploads/products/Quan-jean-Levis-511-size-31'
            WHEN sp.Ten LIKE N'Chuột không dây Logitech M331 Silent%'        THEN N'/uploads/products/Chuot-khong-day-Logitech-M331-Silent'
            WHEN sp.Ten LIKE N'Bàn phím DareU EK87 Tenkeyless Blue Switch%'  THEN N'/uploads/products/Ban-phim-DareU-EK87-Tenkeyless-Blue-Switch'
            WHEN sp.Ten LIKE N'Tai nghe gaming chụp tai DareU EH416%'        THEN N'/uploads/products/Tai-nghe-gaming-chup-tai-DareU-EH416'
            WHEN sp.Ten LIKE N'Ấm siêu tốc Philips 1.5L inox 304%'           THEN N'/uploads/products/Am-sieu-toc-Philips-1-5L-inox-304'
            WHEN sp.Ten LIKE N'Quạt bàn mini Senko B102%'                    THEN N'/uploads/products/Quat-ban-mini-Senko-B102'
            WHEN sp.Ten LIKE N'Balo đi học The North Face Router 35L%'       THEN N'/uploads/products/Balo-di-hoc-The-North-Face-Router-35L'
            WHEN sp.Ten LIKE N'Đồng hồ thể thao điện tử Casio F-91W%'        THEN N'/uploads/products/dong-ho-the-thao-dien-tu-Casio-F-91W'
            WHEN sp.Ten LIKE N'Máy tính bỏ túi Casio fx-580VN X%'            THEN N'/uploads/products/May-tinh-bo-tui-Casio-fx-580VN-X'
            WHEN sp.Ten LIKE N'Đèn học để bàn chống cận Rạng Đông LED 6W%'   THEN N'/uploads/products/den-hoc-de-ban-chong-can-Rang-dong-LED-6W'
            WHEN sp.Ten LIKE N'Nồi cơm điện nắp gài Sharp 1.8L%'             THEN N'/uploads/products/Noi-com-dien-nap-gai-Sharp-1-8L'
            WHEN sp.Ten LIKE N'Loa kéo karaoke di động mini vỏ gỗ%'          THEN N'/uploads/products/Loa-keo-karaoke-di-dong-mini-vo-go'
            ELSE NULL
        END AS BaseDir
) rawMap
CROSS APPLY (
    SELECT
        REPLACE(rawMap.BaseDir, N'Máy-xay-sinh-to-Philips', N'May-xay-sinh-to-Philips') AS BaseDir
) normMap
CROSS APPLY (
    SELECT
        normMap.BaseDir,
        CASE
            WHEN normMap.BaseDir IN (
                N'/uploads/products/Ao-khoac-bomber-den-size-M',
                N'/uploads/products/Goi-om-va-chan-mong',
                N'/uploads/products/Ke-giay-4-tang-bang-thep',
                N'/uploads/products/Ke-tivi-go-cong-nghiep-1m2',
                N'/uploads/products/Noi-com-dien-Sharp-1-8L',
                N'/uploads/products/Tu-quan-ao-vai-3-ngan',
                N'/uploads/products/Xe-may-so-110cc-chinh-chu-mot-doi-su-dung',
                N'/uploads/products/Xe-tap-di-tre-em-co-vong-nhac-phia-truoc'
            ) THEN N'.png'
            ELSE N'.jpg'
        END AS Ext,
        CASE
            WHEN normMap.BaseDir IN (
                N'/uploads/products/Balo-di-hoc-The-North-Face-Router-35L',
                N'/uploads/products/Xe-day-em-be-gap-gon'
            ) THEN 1
            WHEN normMap.BaseDir IN (
                N'/uploads/products/Ao-khoac-bomber-den-size-M',
                N'/uploads/products/Ao-khoac-chong-nuoc-nu-co-lot-luoi',
                N'/uploads/products/Ao-len-cashmere-co-tim-mau-xam-nhat',
                N'/uploads/products/Balo-may-anh-chong-nuoc-co-ao-mua-phu',
                N'/uploads/products/Ban-an-mat-da-bon-ghe-chan-sat',
                N'/uploads/products/Bap-benh-go-tre-em-co-tay-vin',
                N'/uploads/products/Bo-ban-ghe-nhua-xep-dung-cho-ban-cong',
                N'/uploads/products/Bo-dao-va-thot-inox-cho-bep-gia-dinh',
                N'/uploads/products/Bo-sau-hop-thuy-tinh-chiu-nhiet-co-nap',
                N'/uploads/products/Bo-tai-lieu-luyen-thi-IELTS-18-kem-ma-file-nghe',
                N'/uploads/products/Bo-tranh-chu-thu-phap-Phuc-Loc-Tho-Khang',
                N'/uploads/products/But-may-nap-bam-ngoi-M-kem-lo-muc',
                N'/uploads/products/Cay-may-tinh-ban-i5-the-he-8-RAM-8GB-SSD-240GB',
                N'/uploads/products/den-flash-studio-kem-du-tan-80cm',
                N'/uploads/products/den-tha-tran-go-thong-ba-bong',
                N'/uploads/products/do-boi-nu-hai-manh-size-M-con-dan-hoi',
                N'/uploads/products/do-choi-go-xep-hinh-100-chi-tiet',
                N'/uploads/products/dong-ho-ban-co-len-day-cot-kieu-co',
                N'/uploads/products/dong-ho-do-nhip-tim-chay-bo-kem-day-deo-nguc',
                N'/uploads/products/dong-ho-thong-minh-vien-nhom-day-the-thao',
                N'/uploads/products/Gay-golf-sat-so-7-da-thay-grip-moi',
                N'/uploads/products/Giay-tay-da-bo-co-cao-size-41-da-danh-xi',
                N'/uploads/products/Giuong-tang-sat-co-luoi-bao-ve-tang-tren',
                N'/uploads/products/Ke-tivi-go-xoan-dao-dai-mot-met-sau',
                N'/uploads/products/Khan-lua-vuong-hoa-tiet-hoa-sen-70cm',
                N'/uploads/products/Kinh-boi-kem-cap-mui-silicone',
                N'/uploads/products/Lap-rap-kieu-500-manh-mo-hinh-lau-dai-co-hop',
                N'/uploads/products/May-anh-Sony-A6000-kem-ong-kinh-16-50mm',
                N'/uploads/products/May-chay-bo-dien-trong-nha-gap-duoc',
                N'/uploads/products/May-giat-mini-ba-kg-cho-do-tre-em',
                N'/uploads/products/May-in-phun-bon-mau-binh-muc-lien-tuc',
                N'/uploads/products/May-quay-tay-cam-do-phan-giai-Full-HD',
                N'/uploads/products/Quan-kaki-nam-ong-suong-lung-chun-size-32',
                N'/uploads/products/Quan-tay-nam-ong-dung-lung-cao-size-30',
                N'/uploads/products/Quat-ban-mini-Senko-B102',
                N'/uploads/products/Quat-tran-ba-canh-co-dieu-khien-tu-xa',
                N'/uploads/products/Sac-du-phong-Anker-20000mAh-chua-boc-seal',
                N'/uploads/products/Sach-Tu-duy-phan-bien-ban-in-2024',
                N'/uploads/products/Set-nam-bo-do-thu-dong-cho-be-size-6',
                N'/uploads/products/Sofa-vang-ba-cho-boc-ni-mau-xam',
                N'/uploads/products/Tay-chong-rung-may-anh-khong-guong-lat',
                N'/uploads/products/Tranh-son-mai-phong-canh-pho-co-60x80cm',
                N'/uploads/products/Truyen-Sherlock-Holmes-tron-bo-bon-tap',
                N'/uploads/products/Tu-giay-nhua-ba-canh-kieu-gap-hai-tang',
                N'/uploads/products/Tu-quan-ao-bon-canh-go-cong-nghiep-chong-am',
                N'/uploads/products/Van-cheo-hoi-kem-bom-tay-va-day-an-toan',
                N'/uploads/products/Vot-padel-mat-soi-carbon-da-dung-muoi-tran',
                N'/uploads/products/Xe-dap-dien-di-duoc-khoang-45km-moi-lan-sac',
                N'/uploads/products/Xe-tap-di-tre-em-co-vong-nhac-phia-truoc'
            ) THEN 2
            ELSE 3
        END AS MaxImages
) m
WHERE rawMap.BaseDir IS NOT NULL;

-- 1. Xóa các bản ghi ảnh dư vượt quá số lượng file ảnh thực tế trong thư mục của sản phẩm đó
DELETE a
FROM AnhSanPham a
JOIN #ProductImageMap m ON m.MaSanPham = a.MaSanPham
WHERE a.ThuTu > m.MaxImages;

-- 2. Cập nhật đường dẫn ảnh hiện có trong AnhSanPham theo đúng thư mục và đuôi file (.jpg / .png)
UPDATE a
SET a.DuongDan = m.BaseDir + N'/' + CAST(CASE WHEN a.ThuTu BETWEEN 1 AND m.MaxImages THEN a.ThuTu ELSE 1 END AS nvarchar(10)) + m.Ext,
    a.LaAnhChinh = CASE WHEN a.ThuTu = 1 THEN 1 ELSE 0 END
FROM AnhSanPham a
JOIN #ProductImageMap m ON m.MaSanPham = a.MaSanPham;

-- 3. Chèn bổ sung ảnh (1..MaxImages) cho các sản phẩm chưa đủ ảnh
INSERT INTO AnhSanPham (MaSanPham, DuongDan, ThuTu, LaAnhChinh, DungLuong)
SELECT
    m.MaSanPham,
    m.BaseDir + N'/' + CAST(n.ThuTu AS nvarchar(10)) + m.Ext,
    n.ThuTu,
    CASE WHEN n.ThuTu = 1 THEN 1 ELSE 0 END,
    210000 + (m.MaSanPham % 50) * 1000 + n.ThuTu * 100
FROM #ProductImageMap m
JOIN SanPham sp ON sp.MaSanPham = m.MaSanPham
CROSS JOIN (VALUES (1), (2), (3)) n (ThuTu)
WHERE n.ThuTu <= m.MaxImages
  AND NOT EXISTS (
      SELECT 1 FROM AnhSanPham a WHERE a.MaSanPham = m.MaSanPham AND a.ThuTu = n.ThuTu
  );

-- 4. Đồng bộ ảnh đại diện trong ChiTietDonHang theo đúng ảnh chính (1.jpg / 1.png) của sản phẩm
IF OBJECT_ID(N'dbo.trg_ChiTietDonHang_KiemTra', N'TR') IS NOT NULL
    DISABLE TRIGGER dbo.trg_ChiTietDonHang_KiemTra ON dbo.ChiTietDonHang;

UPDATE ct
SET ct.AnhDaiDien = m.BaseDir + N'/1' + m.Ext
FROM ChiTietDonHang ct
JOIN #ProductImageMap m ON m.MaSanPham = ct.MaSanPham
WHERE ct.AnhDaiDien IS NULL OR ct.AnhDaiDien <> m.BaseDir + N'/1' + m.Ext;

IF OBJECT_ID(N'dbo.trg_ChiTietDonHang_KiemTra', N'TR') IS NOT NULL
    ENABLE TRIGGER dbo.trg_ChiTietDonHang_KiemTra ON dbo.ChiTietDonHang;

COMMIT TRANSACTION;
GO
