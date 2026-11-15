SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
USE SecondHandMarketplace;
GO

BEGIN TRANSACTION;

UPDATE a
SET a.DuongDan = N'/uploads/seed/' + m.ImgCode + N'-' + CAST(a.ThuTu AS varchar(1)) + N'.jpg'
FROM AnhSanPham a
JOIN SanPham sp ON sp.MaSanPham = a.MaSanPham
CROSS APPLY (
    SELECT CASE
        -- 1. Các cụm từ ghép đặc thù
        WHEN sp.ChuoiTimKiem LIKE '%gia do dien thoai%' THEN 'sp066'
        WHEN sp.ChuoiTimKiem LIKE '%bom lop%' THEN 'sp068'
        WHEN sp.ChuoiTimKiem LIKE '%mu bao hiem%' THEN 'sp065'
        WHEN sp.ChuoiTimKiem LIKE '%van cheo%' THEN 'sp061'
        WHEN sp.ChuoiTimKiem LIKE '%kinh boi%' OR sp.ChuoiTimKiem LIKE '%kinh lan%' THEN 'sp062'
        WHEN sp.ChuoiTimKiem LIKE '%kinh mat%' OR sp.ChuoiTimKiem LIKE '%kinh ram%' THEN 'sp060'
        WHEN sp.ChuoiTimKiem LIKE '%xe dap dien%' THEN 'sp064'
        WHEN sp.ChuoiTimKiem LIKE '%xe dap gap%' THEN 'sp033'
        WHEN sp.ChuoiTimKiem LIKE '%xe may%' OR sp.ChuoiTimKiem LIKE '%wave%' THEN 'sp063'
        WHEN sp.ChuoiTimKiem LIKE '%xe dap%' OR sp.ChuoiTimKiem LIKE '%giant%' THEN 'sp008'

        WHEN sp.ChuoiTimKiem LIKE '%ke sach%' OR sp.ChuoiTimKiem LIKE '%gia sach%' THEN 'sp015'
        WHEN sp.ChuoiTimKiem LIKE '%ke tivi%' THEN 'sp044'
        WHEN sp.ChuoiTimKiem LIKE '%tu giay%' THEN 'sp075'
        WHEN sp.ChuoiTimKiem LIKE '%tu quan ao%' OR sp.ChuoiTimKiem LIKE '%tu vai%' THEN 'sp042'
        WHEN sp.ChuoiTimKiem LIKE '%giuong tang%' THEN 'sp045'
        WHEN sp.ChuoiTimKiem LIKE '%cong thai hoc%' THEN 'sp046'
        WHEN sp.ChuoiTimKiem LIKE '%ban an%' THEN 'sp041'
        WHEN sp.ChuoiTimKiem LIKE '%ban tra%' THEN 'sp056'
        WHEN sp.ChuoiTimKiem LIKE '%ban hoc%' OR sp.ChuoiTimKiem LIKE '%ban ghe nhua%' THEN 'sp017'
        WHEN sp.ChuoiTimKiem LIKE '%sofa%' THEN 'sp040'
        WHEN sp.ChuoiTimKiem LIKE '%den tha tran%' THEN 'sp043'
        WHEN sp.ChuoiTimKiem LIKE '%den ban%' OR sp.ChuoiTimKiem LIKE '%den led%' THEN 'sp018'

        WHEN sp.ChuoiTimKiem LIKE '%dong ho do nhip tim%' OR sp.ChuoiTimKiem LIKE '%nhip tim%' THEN 'sp080'
        WHEN sp.ChuoiTimKiem LIKE '%dong ho thong minh%' THEN 'sp071'
        WHEN sp.ChuoiTimKiem LIKE '%dong ho ban%' OR sp.ChuoiTimKiem LIKE '%len day cot%' THEN 'sp053'
        WHEN sp.ChuoiTimKiem LIKE '%dong ho%' THEN 'sp012'

        WHEN sp.ChuoiTimKiem LIKE '%noi chien%' THEN 'sp006'
        WHEN sp.ChuoiTimKiem LIKE '%sac du phong%' THEN 'sp006'
        WHEN sp.ChuoiTimKiem LIKE '%quat tran%' THEN 'sp067'
        WHEN sp.ChuoiTimKiem LIKE '%quat%' OR sp.ChuoiTimKiem LIKE '%loc khong khi%' THEN 'sp026'
        WHEN sp.ChuoiTimKiem LIKE '%noi com%' OR sp.ChuoiTimKiem LIKE '%noi chao%' OR sp.ChuoiTimKiem LIKE '%bo noi%' OR sp.ChuoiTimKiem LIKE '%noi inox%' THEN 'sp019'
        WHEN sp.ChuoiTimKiem LIKE '%thuy tinh%' THEN 'sp018'
        WHEN sp.ChuoiTimKiem LIKE '%ban ui%' THEN 'sp025'
        WHEN sp.ChuoiTimKiem LIKE '%xay sinh to%' THEN 'sp024'
        WHEN sp.ChuoiTimKiem LIKE '%may giat%' THEN 'sp076'
        WHEN sp.ChuoiTimKiem LIKE '%dao va thot%' OR sp.ChuoiTimKiem LIKE '%bo dao%' THEN 'sp077'

        WHEN sp.ChuoiTimKiem LIKE '%ao khoac gio%' OR sp.ChuoiTimKiem LIKE '%chong nuoc nu%' THEN 'sp059'
        WHEN sp.ChuoiTimKiem LIKE '%ao khoac denim%' OR sp.ChuoiTimKiem LIKE '%denim%' THEN 'sp001'
        WHEN sp.ChuoiTimKiem LIKE '%giay tay%' THEN 'sp070'
        WHEN sp.ChuoiTimKiem LIKE '%converse%' OR sp.ChuoiTimKiem LIKE '%sneaker%' OR sp.ChuoiTimKiem LIKE '%giay%' THEN 'sp002'
        WHEN sp.ChuoiTimKiem LIKE '%ao len%' THEN 'sp012'
        WHEN sp.ChuoiTimKiem LIKE '%kaki%' THEN 'sp013'
        WHEN sp.ChuoiTimKiem LIKE '%khan lua%' THEN 'sp014'
        WHEN sp.ChuoiTimKiem LIKE '%set nam bo do%' OR sp.ChuoiTimKiem LIKE '%thu dong%' THEN 'sp039'
        WHEN sp.ChuoiTimKiem LIKE '%balo may anh%' THEN 'sp052'
        WHEN sp.ChuoiTimKiem LIKE '%balo%' OR sp.ChuoiTimKiem LIKE '%tui xach%' THEN 'sp014'

        WHEN sp.ChuoiTimKiem LIKE '%do choi go%' THEN 'sp034'
        WHEN sp.ChuoiTimKiem LIKE '%ghe an dam%' THEN 'sp035'
        WHEN sp.ChuoiTimKiem LIKE '%xe tap di%' THEN 'sp036'
        WHEN sp.ChuoiTimKiem LIKE '%lap rap%' OR sp.ChuoiTimKiem LIKE '%lego%' OR sp.ChuoiTimKiem LIKE '%xep hinh%' THEN 'sp027'
        WHEN sp.ChuoiTimKiem LIKE '%noi dien%' THEN 'sp038'
        WHEN sp.ChuoiTimKiem LIKE '%bap benh%' THEN 'sp074'
        WHEN sp.ChuoiTimKiem LIKE '%xe day%' THEN 'sp022'

        WHEN sp.ChuoiTimKiem LIKE '%chay bo%' THEN 'sp028'
        WHEN sp.ChuoiTimKiem LIKE '%yoga%' THEN 'sp029'
        WHEN sp.ChuoiTimKiem LIKE '%ta tay%' THEN 'sp030'
        WHEN sp.ChuoiTimKiem LIKE '%golf%' THEN 'sp031'
        WHEN sp.ChuoiTimKiem LIKE '%padel%' THEN 'sp058'
        WHEN sp.ChuoiTimKiem LIKE '%cau long%' OR sp.ChuoiTimKiem LIKE '%vot%' THEN 'sp021'
        WHEN sp.ChuoiTimKiem LIKE '%leu du lich%' THEN 'sp032'

        -- Sách & Văn phòng phẩm
        WHEN sp.ChuoiTimKiem LIKE '%doraemon%' OR sp.ChuoiTimKiem LIKE '%sherlock%' OR sp.ChuoiTimKiem LIKE '%conan%' THEN 'sp020'
        WHEN sp.ChuoiTimKiem LIKE '%tu duy phan bien%' THEN 'sp056'
        WHEN sp.ChuoiTimKiem LIKE '%ielts%' OR sp.ChuoiTimKiem LIKE '%kinh te hoc%' OR sp.ChuoiTimKiem LIKE '%giao trinh%' OR sp.ChuoiTimKiem LIKE '%giai tich%' OR sp.ChuoiTimKiem LIKE '%cau truc du lieu%' OR sp.ChuoiTimKiem LIKE '%lap trinh%' THEN 'sp007'
        WHEN sp.ChuoiTimKiem LIKE '%vo ke ngang%' THEN 'sp007'
        WHEN sp.ChuoiTimKiem LIKE '%but may%' THEN 'sp025'
        WHEN sp.ChuoiTimKiem LIKE '%truyen%' THEN 'sp020'
        WHEN sp.ChuoiTimKiem LIKE '%casio fx%' OR sp.ChuoiTimKiem LIKE '%may tinh bo tui%' THEN 'sp007'
        WHEN sp.ChuoiTimKiem LIKE '%sach%' THEN 'sp007'

        -- Đồ điện tử & Công nghệ
        WHEN sp.ChuoiTimKiem LIKE '%ipad%' OR sp.ChuoiTimKiem LIKE '%iphone%' OR sp.ChuoiTimKiem LIKE '%dien thoai%' THEN 'sp003'
        WHEN sp.ChuoiTimKiem LIKE '%laptop%' OR sp.ChuoiTimKiem LIKE '%latitude%' OR sp.ChuoiTimKiem LIKE '%man hinh%' THEN 'sp004'
        WHEN sp.ChuoiTimKiem LIKE '%may tinh ban%' OR sp.ChuoiTimKiem LIKE '%ban phim%' OR sp.ChuoiTimKiem LIKE '%keychron%' THEN 'sp005'
        WHEN sp.ChuoiTimKiem LIKE '%tai nghe%' THEN 'sp009'
        WHEN sp.ChuoiTimKiem LIKE '%a6000%' OR sp.ChuoiTimKiem LIKE '%700d%' OR sp.ChuoiTimKiem LIKE '%ong kinh%' OR sp.ChuoiTimKiem LIKE '%may anh%' THEN 'sp010'
        WHEN sp.ChuoiTimKiem LIKE '%loa%' THEN 'sp013'
        WHEN sp.ChuoiTimKiem LIKE '%may in%' THEN 'sp079'
        WHEN sp.ChuoiTimKiem LIKE '%chong rung%' OR sp.ChuoiTimKiem LIKE '%may quay%' THEN 'sp049'
        WHEN sp.ChuoiTimKiem LIKE '%flash%' THEN 'sp051'

        -- Mỹ nghệ & Đồ cổ
        WHEN sp.ChuoiTimKiem LIKE '%binh gom%' OR sp.ChuoiTimKiem LIKE '%binh hoa%' THEN 'sp054'
        WHEN sp.ChuoiTimKiem LIKE '%son mai%' THEN 'sp055'
        WHEN sp.ChuoiTimKiem LIKE '%thu phap%' THEN 'sp057'
        WHEN sp.ChuoiTimKiem LIKE '%co vua%' THEN 'sp011'
        WHEN sp.ChuoiTimKiem LIKE '%goi om%' OR sp.ChuoiTimKiem LIKE '%chan mong%' THEN 'sp016'

        -- Nhóm chung
        WHEN sp.ChuoiTimKiem LIKE '%tu%' THEN 'sp042'
        WHEN sp.ChuoiTimKiem LIKE '%ao%' OR sp.ChuoiTimKiem LIKE '%quan%' THEN 'sp001'

        ELSE 'sp007'
    END AS ImgCode
) m
WHERE a.DuongDan LIKE '/uploads/seed/%';

COMMIT TRANSACTION;
GO
