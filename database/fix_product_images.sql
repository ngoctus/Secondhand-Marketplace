SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
USE SecondHandMarketplace;
GO

-- -----------------------------------------------------------------------------
-- Sửa đường dẫn ảnh seed của CSDL đang chạy cho khớp đúng từng sản phẩm.
-- Bảng ánh xạ dùng đúng tên sản phẩm như 13_seed_demo.sql (không dò theo từ khóa
-- nên không bị nhầm nhóm). Chạy lại nhiều lần vẫn an toàn.
-- Dùng khi không muốn xóa/tạo lại CSDL mà chỉ cần cập nhật ảnh.
-- -----------------------------------------------------------------------------

BEGIN TRANSACTION;

UPDATE a
SET a.DuongDan = N'/uploads/seed/' + m.ImgCode + N'-' + CAST(a.ThuTu AS varchar(1)) + N'.jpg'
FROM AnhSanPham a
JOIN SanPham sp ON sp.MaSanPham = a.MaSanPham
CROSS APPLY (
    SELECT CASE
        -- 26 tin đầu tiên (An + Bình)
        WHEN sp.Ten = N'Áo khoác denim nam size L'                      THEN 'sp001'
        WHEN sp.Ten = N'Giày sneaker trắng size 42'                     THEN 'sp002'
        WHEN sp.Ten = N'iPhone 11 64GB màu đen'                         THEN 'sp101'
        WHEN sp.Ten = N'Laptop Dell Latitude 7490 i5'                   THEN 'sp081'
        WHEN sp.Ten = N'Bàn phím cơ Keychron K2'                        THEN 'sp103'
        WHEN sp.Ten = N'Nồi chiên không dầu 5L'                         THEN 'sp006'
        WHEN sp.Ten = N'Giáo trình Lập trình Web trọn bộ'               THEN 'sp007'
        WHEN sp.Ten = N'Xe đạp địa hình Giant ATX'                      THEN 'sp073'
        WHEN sp.Ten = N'Tai nghe Sony WH-1000XM3'                       THEN 'sp009'
        WHEN sp.Ten = N'Máy ảnh Canon EOS 700D kèm lens kit'            THEN 'sp037'
        WHEN sp.Ten = N'Bộ cờ vua gỗ (bản nháp)'                        THEN 'sp011'
        WHEN sp.Ten = N'Đồng hồ Casio vintage'                          THEN 'sp012'
        WHEN sp.Ten = N'Loa Bluetooth JBL Flip 4'                       THEN 'sp013'
        WHEN sp.Ten = N'Balo laptop chống nước'                         THEN 'sp102'
        WHEN sp.Ten = N'Kệ sách 5 tầng'                                 THEN 'sp015'
        WHEN sp.Ten = N'Gối ôm và chăn mỏng'                            THEN 'sp016'
        WHEN sp.Ten = N'Bàn học gỗ kèm ghế'                             THEN 'sp109'
        WHEN sp.Ten = N'Đèn bàn LED chống cận'                          THEN 'sp084'
        WHEN sp.Ten = N'Bộ nồi inox 3 món'                              THEN 'sp019'
        WHEN sp.Ten = N'Truyện tranh Doraemon trọn bộ'                  THEN 'sp020'
        WHEN sp.Ten = N'Vợt cầu lông Yonex Astrox'                      THEN 'sp021'
        WHEN sp.Ten = N'Xe đẩy em bé gấp gọn'                           THEN 'sp110'
        WHEN sp.Ten = N'Tủ quần áo vải 3 ngăn'                          THEN 'sp111'
        WHEN sp.Ten = N'Máy xay sinh tố Philips'                        THEN 'sp024'
        WHEN sp.Ten = N'Bàn ủi hơi nước Tefal'                          THEN 'sp052'
        WHEN sp.Ten = N'Quạt đứng Senko'                                THEN 'sp026'
        WHEN sp.Ten = N'Bộ xếp hình Lego City'                          THEN 'sp027'

        -- 80 tin của nhóm B8
        WHEN sp.Ten = N'Máy tính bảng iPad Gen 9 64GB WiFi viền bạc'    THEN 'sp003'
        WHEN sp.Ten = N'Laptop Dell Latitude 7490 Core i5 RAM 8GB SSD 256GB' THEN 'sp081'
        WHEN sp.Ten = N'iPad Pro 11 inch WiFi 128GB màu xám không gian' THEN 'sp003'
        WHEN sp.Ten = N'Màn hình vi tính Dell 24 inch Full HD chống lóa' THEN 'sp082'
        WHEN sp.Ten = N'Cây máy tính bàn Intel Core i5 RAM 8GB SSD 240GB' THEN 'sp005'
        WHEN sp.Ten = N'Sạc dự phòng Anker 20000mAh hỗ trợ sạc nhanh'   THEN 'sp083'
        WHEN sp.Ten = N'Đèn LED để bàn cảm ứng ba mức sáng chống cận'   THEN 'sp084'
        WHEN sp.Ten = N'Áo khoác denim vintage Levi''s dáng suông size L' THEN 'sp001'
        WHEN sp.Ten = N'Áo khoác gió nữ chống nước 2 lớp có lót lưới size M' THEN 'sp059'
        WHEN sp.Ten = N'Túi xách nữ da thật quai đeo chéo màu nâu cổ điển' THEN 'sp118'
        WHEN sp.Ten = N'Giày sneaker Converse Jack Purcell trắng cổ thấp size 42' THEN 'sp002'
        WHEN sp.Ten = N'Áo len cashmere ấm áp cổ tròn màu xám nhạt size M' THEN 'sp085'
        WHEN sp.Ten = N'Quần kaki nam ống suông màu be nhạt size 32'    THEN 'sp086'
        WHEN sp.Ten = N'Khăn lụa vuông cao cấp họa tiết hoa sen kích thước 70cm' THEN 'sp120'
        WHEN sp.Ten = N'Quạt tháp và lọc không khí gia đình phòng dưới 40m2' THEN 'sp087'
        WHEN sp.Ten = N'Bộ nồi chảo inox đáy từ cao cấp 3 đáy sáng bóng' THEN 'sp019'
        WHEN sp.Ten = N'Nồi chiên không dầu Philips 4.5L lòng nồi chống dính' THEN 'sp006'
        WHEN sp.Ten = N'Bộ 6 hộp thủy tinh chịu nhiệt có nắp khóa khí kín' THEN 'sp122'
        WHEN sp.Ten = N'Bộ nồi inox 3 món kèm nắp vung kính cường lực'  THEN 'sp019'
        WHEN sp.Ten = N'Bàn ủi hơi nước cầm tay Tefal mặt đế chống dính' THEN 'sp052'
        WHEN sp.Ten = N'Trọn bộ truyện tranh Doraemon 45 tập tuổi thơ'  THEN 'sp020'
        WHEN sp.Ten = N'Sách Tư duy phản biện bản in mới xuất bản'      THEN 'sp089'
        WHEN sp.Ten = N'Truyện Sherlock Holmes trọn bộ bốn tập kinh điển' THEN 'sp090'
        WHEN sp.Ten = N'Bộ sách giáo trình Lập trình căn bản và Cấu trúc dữ liệu' THEN 'sp007'
        WHEN sp.Ten = N'Bút máy nắp bấm Jinhao ngòi M kèm piston hút mực' THEN 'sp126'
        WHEN sp.Ten = N'Kệ sách gỗ thông để bàn 3 tầng lắp ghép nhỏ gọn' THEN 'sp015'
        WHEN sp.Ten = N'Bộ tài liệu luyện thi IELTS Cambridge trọn bộ kèm audio' THEN 'sp127'
        WHEN sp.Ten = N'Máy chạy bộ điện trong nhà gập được'            THEN 'sp028'
        WHEN sp.Ten = N'Thảm tập yoga 6mm kèm dây kháng lực'            THEN 'sp029'
        WHEN sp.Ten = N'Bộ ba tạ tay bọc gang mỗi bên năm ký'           THEN 'sp030'
        WHEN sp.Ten = N'Gậy golf sắt số 7 đã thay grip mới'             THEN 'sp031'
        WHEN sp.Ten = N'Lều du lịch hai người chống mưa'                THEN 'sp032'
        WHEN sp.Ten = N'Xe đạp gấp bánh 20 inch đã bảo dưỡng'           THEN 'sp033'
        WHEN sp.Ten = N'Đồ chơi gỗ xếp hình 100 chi tiết'               THEN 'sp091'
        WHEN sp.Ten = N'Ghế ăn dặm nhựa nguyên khối có khay rời'        THEN 'sp035'
        WHEN sp.Ten = N'Xe tập đi trẻ em có vòng nhạc phía trước'       THEN 'sp036'
        WHEN sp.Ten = N'Lắp ráp kiểu 500 mảnh mô hình lâu đài có hộp'   THEN 'sp092'
        WHEN sp.Ten = N'Nôi điện tự đưa kèm màn chống muỗi'             THEN 'sp038'
        WHEN sp.Ten = N'Set năm bộ đồ thu đông cho bé size 6'           THEN 'sp039'
        WHEN sp.Ten = N'Sofa văng ba chỗ bọc nỉ màu xám'                THEN 'sp040'
        WHEN sp.Ten = N'Bàn ăn mặt đá bốn ghế chân sắt'                 THEN 'sp041'
        WHEN sp.Ten = N'Tủ quần áo bốn cánh gỗ công nghiệp chống ẩm'    THEN 'sp042'
        WHEN sp.Ten = N'Đèn thả trần gỗ thông ba bóng'                  THEN 'sp043'
        WHEN sp.Ten = N'Kệ tivi gỗ xoan đào dài một mét sáu'            THEN 'sp093'
        WHEN sp.Ten = N'Giường tầng sắt có lưới bảo vệ tầng trên'       THEN 'sp045'
        WHEN sp.Ten = N'Ghế công thái học lưng lưới có tựa đầu'         THEN 'sp046'
        WHEN sp.Ten = N'Máy ảnh Sony A6000 kèm ống kính 16-50mm'        THEN 'sp047'
        WHEN sp.Ten = N'Ống kính 50mm f/1.8 lấy nét tự động'            THEN 'sp048'
        WHEN sp.Ten = N'Tay chống rung máy ảnh không gương lật'         THEN 'sp049'
        WHEN sp.Ten = N'Máy quay tay cầm độ phân giải Full HD'          THEN 'sp050'
        WHEN sp.Ten = N'Đèn flash studio kèm dù tán 80cm'               THEN 'sp051'
        WHEN sp.Ten = N'Balo máy ảnh chống nước có áo mưa phủ'          THEN 'sp094'
        WHEN sp.Ten = N'Đồng hồ bàn cơ lên dây cót kiểu cổ'             THEN 'sp053'
        WHEN sp.Ten = N'Bình gốm men rạn cao 40cm'                      THEN 'sp054'
        WHEN sp.Ten = N'Tranh sơn mài phong cảnh phố cổ 60x80cm'        THEN 'sp055'
        WHEN sp.Ten = N'Bàn trà gỗ hương kèm hai đôn nhỏ'               THEN 'sp056'
        WHEN sp.Ten = N'Bộ tranh chữ thư pháp Phúc Lộc Thọ Khang'       THEN 'sp057'
        WHEN sp.Ten = N'Vợt padel mặt sợi carbon đã dùng mười trận'     THEN 'sp058'
        WHEN sp.Ten = N'Áo khoác chống nước nữ có lót lưới'             THEN 'sp059'
        WHEN sp.Ten = N'Kính mát phân cực chống tia UV đi biển'         THEN 'sp060'
        WHEN sp.Ten = N'Ván chèo hơi kèm bơm tay và dây an toàn'        THEN 'sp061'
        WHEN sp.Ten = N'Kính bơi kèm cặp mũi silicone'                  THEN 'sp062'
        WHEN sp.Ten = N'Xe máy số 110cc chính chủ một đời sử dụng'      THEN 'sp063'
        WHEN sp.Ten = N'Xe đạp điện đi được khoảng 45km mỗi lần sạc'    THEN 'sp064'
        WHEN sp.Ten = N'Mũ bảo hiểm ba phần tư có kính chắn gió size L' THEN 'sp065'
        WHEN sp.Ten = N'Giá đỡ điện thoại gắn xe máy có cổng USB'       THEN 'sp066'
        WHEN sp.Ten = N'Quạt trần ba cánh có điều khiển từ xa'          THEN 'sp067'
        WHEN sp.Ten = N'Bơm lốp xe mini chạy điện 12V có đồng hồ'       THEN 'sp068'
        WHEN sp.Ten = N'Quần tây nam ống đứng lưng cao size 30'         THEN 'sp069'
        WHEN sp.Ten = N'Giày tây da bò cổ cao size 41 đã đánh xi'       THEN 'sp070'
        WHEN sp.Ten = N'Đồng hồ thông minh viền nhôm dây thể thao'      THEN 'sp071'
        WHEN sp.Ten = N'Loa Bluetooth cỡ lớn pin khoảng sáu tiếng'      THEN 'sp072'
        WHEN sp.Ten = N'Xe đạp thể thao khung nhôm bánh 26 inch'        THEN 'sp073'
        WHEN sp.Ten = N'Bập bênh gỗ trẻ em có tay vịn'                  THEN 'sp074'
        WHEN sp.Ten = N'Tủ giày nhựa ba cánh kiểu gấp hai tầng'         THEN 'sp075'
        WHEN sp.Ten = N'Máy giặt mini ba kg cho đồ trẻ em'              THEN 'sp076'
        WHEN sp.Ten = N'Bộ dao và thớt inox cho bếp gia đình'           THEN 'sp077'
        WHEN sp.Ten = N'Bộ bàn ghế nhựa xếp dùng cho ban công'          THEN 'sp078'
        WHEN sp.Ten = N'Máy in phun bốn màu bình mực liên tục'          THEN 'sp079'
        WHEN sp.Ten = N'Đồng hồ đo nhịp tim chạy bộ kèm dây đeo ngực'   THEN 'sp080'

        -- Sản phẩm legacy của CSDL cũ (tên khác bản seed hiện tại)
        WHEN sp.Ten = N'Điện thoại Samsung Galaxy A54 128GB'            THEN 'sp130'
        WHEN sp.Ten = N'Váy denim chữ A cạp cao size S'                 THEN 'sp116'
        WHEN sp.Ten = N'Áo blazer nữ công sở màu be size M'             THEN 'sp117'
        WHEN sp.Ten = N'Túi xách da thật màu nâu quai đeo chéo'         THEN 'sp118'
        WHEN sp.Ten = N'Giày cao gót mũi nhọn 5cm size 37'              THEN 'sp119'
        WHEN sp.Ten = N'Áo len cashmere cổ tim màu xám nhạt'            THEN 'sp085'
        WHEN sp.Ten = N'Quần kaki nam ống suông lưng chun size 32'      THEN 'sp086'
        WHEN sp.Ten = N'Khăn lụa vuông họa tiết hoa sen 70cm'           THEN 'sp120'
        WHEN sp.Ten = N'Máy lọc không khí phòng dưới 40m2'              THEN 'sp087'
        WHEN sp.Ten = N'Bếp từ đôi bảng điều khiển rời'                 THEN 'sp115'
        WHEN sp.Ten = N'Ấm đun nước siêu tốc 1.7L tự ngắt'              THEN 'sp107'
        WHEN sp.Ten = N'Bộ sáu hộp thủy tinh chịu nhiệt có nắp'         THEN 'sp122'
        WHEN sp.Ten = N'Máy hút bụi dạng đứng công suất lớn'            THEN 'sp123'
        WHEN sp.Ten = N'Bộ giáo trình Kinh tế học vĩ mô hai tập'        THEN 'sp124'
        WHEN sp.Ten = N'Sách Tư duy phản biện bản in 2024'              THEN 'sp089'
        WHEN sp.Ten = N'Truyện Sherlock Holmes trọn bộ bốn tập'         THEN 'sp090'
        WHEN sp.Ten = N'Combo năm vở kẻ ngang bìa cứng 200 trang'       THEN 'sp125'
        WHEN sp.Ten = N'Bút máy nắp bấm ngòi M kèm lọ mực'              THEN 'sp126'
        WHEN sp.Ten = N'Bộ tài liệu luyện thi IELTS 18 kèm mã file nghe' THEN 'sp127'

        -- Sản phẩm sinh ra từ 32_seed_don_hang (đơn mẫu)
        WHEN sp.Ten LIKE N'%Giải tích%'                                 THEN 'sp128'
        WHEN sp.Ten LIKE N'%Cấu trúc dữ liệu%'                          THEN 'sp007'
        WHEN sp.Ten LIKE N'%Áo thun polo%'                              THEN 'sp106'
        WHEN sp.Ten LIKE N'%Quần jean Levis%'                           THEN 'sp104'
        WHEN sp.Ten LIKE N'%Chuột không dây%'                           THEN 'sp105'
        WHEN sp.Ten LIKE N'%Bàn phím DareU%'                            THEN 'sp103'
        WHEN sp.Ten LIKE N'%Tai nghe gaming%'                           THEN 'sp009'
        WHEN sp.Ten LIKE N'%Ấm siêu tốc%'                               THEN 'sp107'
        WHEN sp.Ten LIKE N'%Quạt bàn mini%'                             THEN 'sp129'
        WHEN sp.Ten LIKE N'%Balo đi học%'                               THEN 'sp102'
        WHEN sp.Ten LIKE N'%Đồng hồ thể thao%'                          THEN 'sp012'
        WHEN sp.Ten LIKE N'%Máy tính bỏ túi%'                           THEN 'sp108'
        WHEN sp.Ten LIKE N'%Đèn học%'                                   THEN 'sp084'
        WHEN sp.Ten LIKE N'%Nồi cơm điện%'                              THEN 'sp112'
        WHEN sp.Ten LIKE N'%Loa kéo%'                                   THEN 'sp114'

        ELSE NULL
    END AS ImgCode
) m
WHERE a.DuongDan LIKE '/uploads/seed/%'
  AND m.ImgCode IS NOT NULL;

COMMIT TRANSACTION;
GO

-- Kiểm tra: liệt kê ảnh seed hiện có để đối chiếu với file trong wwwroot/uploads/seed
SELECT sp.MaSanPham, sp.Ten, a.ThuTu, a.DuongDan
FROM AnhSanPham a
JOIN SanPham sp ON sp.MaSanPham = a.MaSanPham
WHERE a.DuongDan LIKE '/uploads/seed/%'
ORDER BY sp.MaSanPham, a.ThuTu;
GO
