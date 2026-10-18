SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ARITHABORT ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

USE SecondHandMarketplace;
GO

-- !!! THAY chuỗi bên dưới bằng BCrypt hash do BE dùng (xem README) !!!
DECLARE @MatKhauHash varchar(100) = '$2a$11$N9qo8uLOickgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy';
DECLARE @Now datetime2(3) = SYSUTCDATETIME();
DECLARE @AdminId int;

BEGIN TRANSACTION;

-- ---------------------------------------------------------------------
-- 1. Tình trạng sản phẩm
-- ---------------------------------------------------------------------
INSERT TinhTrangSanPham (Ma, Ten, MoTa, ThuTuHienThi)
SELECT v.Ma, v.Ten, v.MoTa, v.ThuTu
FROM (VALUES
    ('NEW',      N'Mới 100%',  N'Chưa qua sử dụng, còn nguyên tem mác', 1),
    ('LIKE_NEW', N'Như mới',   N'Đã dùng rất ít, gần như không có dấu hiệu sử dụng', 2),
    ('GOOD',     N'Tốt',       N'Dùng bình thường, hoạt động tốt, ít trầy xước', 3),
    ('FAIR',     N'Khá',       N'Có dấu hiệu sử dụng rõ nhưng vẫn dùng ổn', 4),
    ('WORN',     N'Cũ',        N'Hao mòn nhiều, phù hợp giá rẻ', 5)
) v (Ma, Ten, MoTa, ThuTu)
WHERE NOT EXISTS (SELECT 1 FROM TinhTrangSanPham t WHERE t.Ma = v.Ma);

-- ---------------------------------------------------------------------
-- 2. Danh mục
-- ---------------------------------------------------------------------
INSERT DanhMuc (Ten, ThuTuHienThi)
SELECT v.Ten, v.ThuTu
FROM (VALUES
    (N'Thời trang', 1), (N'Đồ điện tử', 2), (N'Đồ gia dụng', 3),
    (N'Sách & văn phòng phẩm', 4), (N'Xe cộ & phụ kiện', 5),
    (N'Đồ chơi & mẹ bé', 6), (N'Thể thao & dã ngoại', 7),
    (N'Nội thất', 8), (N'Khác', 99)
) v (Ten, ThuTu)
WHERE NOT EXISTS (SELECT 1 FROM DanhMuc d WHERE d.Ten = v.Ten);

-- ---------------------------------------------------------------------
-- 3. Từ/cụm từ cấm (CumTuChuanHoa sinh bằng fn_ChuanHoaChuoi)
-- ---------------------------------------------------------------------
INSERT TuKhoaCam (CumTu, CumTuChuanHoa, Nhom, GhiChu)
SELECT v.CumTu, fn_ChuanHoaChuoi(v.CumTu), v.Nhom, N'Seed ban đầu'
FROM (VALUES
    (N'hàng giả', 'HANG_GIA'), (N'hàng nhái', 'HANG_GIA'), (N'hàng fake', 'HANG_GIA'),
    (N'super fake', 'HANG_GIA'), (N'replica', 'HANG_GIA'),
    (N'hàng cấm', 'HANG_CAM'), (N'vũ khí', 'HANG_CAM'), (N'ma túy', 'HANG_CAM'),
    (N'pháo nổ', 'HANG_CAM'), (N'hàng trộm cắp', 'HANG_CAM'), (N'giấy tờ giả', 'HANG_CAM'),
    (N'chuyển khoản trước', 'LUA_DAO'), (N'đặt cọc trước', 'LUA_DAO'), (N'trúng thưởng', 'LUA_DAO')
) v (CumTu, Nhom)
WHERE NOT EXISTS (SELECT 1 FROM TuKhoaCam k WHERE k.CumTuChuanHoa = fn_ChuanHoaChuoi(v.CumTu));

-- ---------------------------------------------------------------------
-- 4. Người dùng (1 Admin + 6 user demo)
-- ---------------------------------------------------------------------
INSERT NguoiDung (HoTen, Email, SoDienThoai, MatKhauMaHoa, VaiTro, TrangThai, LyDoKhoa, ThoiGianKhoa, DiaChiMacDinh)
SELECT v.HoTen, v.Email, v.SoDienThoai, @MatKhauHash, v.VaiTro, v.TrangThai, v.LyDoKhoa,
       CASE WHEN v.TrangThai = 'LOCKED' THEN @Now END, v.DiaChi
FROM (VALUES
    (N'Quản trị viên',   'admin@secondhand.local',   NULL,         'ADMIN', 'ACTIVE', NULL, NULL),
    (N'Nguyễn Văn An',   'an.seller@demo.local',      '0901000001', 'USER',  'ACTIVE', NULL, N'25 Lê Lợi, Quận 1, TP. Hồ Chí Minh'),
    (N'Trần Thị Bình',   'binh.seller@demo.local',    '0901000002', 'USER',  'ACTIVE', NULL, N'78 Cách Mạng Tháng 8, Quận 3, TP. Hồ Chí Minh'),
    (N'Lê Minh Chi',     'chi.buyer@demo.local',      '0901000003', 'USER',  'ACTIVE', NULL, N'12 Nguyễn Huệ, Quận 1, TP. Hồ Chí Minh'),
    (N'Phạm Quốc Dũng',  'dung.buyer@demo.local',     '0901000004', 'USER',  'ACTIVE', NULL, N'45 Võ Văn Tần, Quận 3, TP. Hồ Chí Minh'),
    (N'Hoàng Văn Khoa',  'locked@demo.local',         '0901000005', 'USER',  'LOCKED', N'Vi phạm quy định: đăng bán hàng giả nhiều lần.', NULL),
    (N'Vũ Thị Em',       'noshop@demo.local',         '0901000006', 'USER',  'ACTIVE', NULL, N'9 Điện Biên Phủ, Bình Thạnh, TP. Hồ Chí Minh')
) v (HoTen, Email, SoDienThoai, VaiTro, TrangThai, LyDoKhoa, DiaChi)
WHERE NOT EXISTS (SELECT 1 FROM NguoiDung n WHERE n.Email = v.Email);

SET @AdminId = (SELECT MaNguoiDung FROM NguoiDung WHERE Email = 'admin@secondhand.local');

-- ---------------------------------------------------------------------
-- 5. Cửa hàng
-- ---------------------------------------------------------------------
INSERT CuaHang (NguoiDungId, TenShop, SoDienThoaiLienHe, MoTa)
SELECT nd.MaNguoiDung, v.TenShop, v.SoDienThoai, v.MoTa
FROM (VALUES
    ('an.seller@demo.local',   N'An Store - Đồ cũ chất lượng', '0901000001', N'Chuyên đồ điện tử, thời trang và xe đạp đã qua sử dụng, kiểm tra kỹ trước khi bán.'),
    ('binh.seller@demo.local', N'Bình Vintage',                '0901000002', N'Đồ gia dụng, nội thất và đồ dùng gia đình giá mềm.')
) v (Email, TenShop, SoDienThoai, MoTa)
JOIN NguoiDung nd ON nd.Email = v.Email
WHERE NOT EXISTS (SELECT 1 FROM CuaHang c WHERE c.MaNguoiDung = nd.MaNguoiDung);

-- ---------------------------------------------------------------------
-- 6. Sản phẩm (27 sản phẩm, đủ 8 trạng thái)
-- ---------------------------------------------------------------------
INSERT SanPham (MaCuaHang, MaDanhMuc, MaTinhTrang, Ten, MoTa, Gia, TrangThai, LyDoGo, NgayTao)
SELECT ch.MaCuaHang, dm.MaDanhMuc, tt.MaTinhTrang, v.Ten, v.MoTa, v.Gia, v.TrangThai, v.LyDoGo, DATEADD(HOUR, -v.GioTruoc, @Now)
FROM (VALUES
 -- ===== An Store =====
 ('an.seller@demo.local', N'Thời trang',            'GOOD',     N'Áo khoác denim nam size L',            N'Áo khoác denim dày dặn, size L, ít sờn, giặt sạch sẽ.',                     250000, 'ACTIVE',         NULL, 300),
 ('an.seller@demo.local', N'Thời trang',            'LIKE_NEW', N'Giày sneaker trắng size 42',           N'Giày sneaker trắng size 42, đi vài lần, đế còn rất mới.',                  450000, 'ACTIVE',         NULL, 290),
 ('an.seller@demo.local', N'Đồ điện tử',            'GOOD',     N'iPhone 11 64GB màu đen',               N'iPhone 11 64GB màu đen, pin 85%, máy zin, đầy đủ phụ kiện cơ bản.',        6500000, 'ACTIVE',         NULL, 280),
 ('an.seller@demo.local', N'Đồ điện tử',            'FAIR',     N'Laptop Dell Latitude 7490 i5',         N'Laptop Dell Latitude 7490, Core i5 thế hệ 8, RAM 8GB, SSD 256GB, pin dùng ổn.', 7200000, 'ACTIVE',         NULL, 270),
 ('an.seller@demo.local', N'Đồ điện tử',            'LIKE_NEW', N'Bàn phím cơ Keychron K2',              N'Bàn phím cơ Keychron K2, switch brown, kèm cáp và keycap thay thế.',        1100000, 'ACTIVE',         NULL, 260),
 ('an.seller@demo.local', N'Đồ gia dụng',           'GOOD',     N'Nồi chiên không dầu 5L',               N'Nồi chiên không dầu dung tích 5 lít, hoạt động tốt, lòng nồi còn mới.',     700000, 'ACTIVE',         NULL, 250),
 ('an.seller@demo.local', N'Sách & văn phòng phẩm', 'GOOD',     N'Giáo trình Lập trình Web trọn bộ',     N'Trọn bộ giáo trình Lập trình Web, có ghi chú bút chì nhẹ, không rách trang.', 120000, 'HIDDEN',         NULL, 240),
 ('an.seller@demo.local', N'Xe cộ & phụ kiện',      'FAIR',     N'Xe đạp địa hình Giant ATX',            N'Xe đạp địa hình Giant ATX, khung nhôm, đã thay xích mới, phanh đĩa chạy tốt.', 3200000, 'ACTIVE',         NULL, 230),
 ('an.seller@demo.local', N'Đồ điện tử',            'GOOD',     N'Tai nghe Sony WH-1000XM3',             N'Tai nghe chống ồn Sony WH-1000XM3, pin còn tốt, kèm hộp và cáp sạc.',        2800000, 'PENDING_REVIEW', NULL, 20),
 ('an.seller@demo.local', N'Đồ điện tử',            'GOOD',     N'Máy ảnh Canon EOS 700D kèm lens kit',  N'Máy ảnh Canon EOS 700D kèm lens kit 18-55mm, số shot thấp, kèm thẻ nhớ 32GB.', 5200000, 'PENDING_REVIEW', NULL, 10),
 ('an.seller@demo.local', NULL,                     NULL,       N'Bộ cờ vua gỗ (bản nháp)',              NULL,                                                                        NULL,    'DRAFT',          NULL, 5),
 ('an.seller@demo.local', N'Thời trang',            'FAIR',     N'Đồng hồ Casio vintage',                N'Đồng hồ Casio.',                                                           600000, 'REJECTED',       NULL, 100),
 ('an.seller@demo.local', N'Đồ điện tử',            'GOOD',     N'Loa Bluetooth JBL Flip 4',             N'Loa Bluetooth JBL Flip 4, âm thanh tốt, chống nước, pin dùng khoảng 8 tiếng.', 900000, 'REMOVED',        N'Mô tả không đúng sự thật về tình trạng sản phẩm.', 200),
 ('an.seller@demo.local', N'Thời trang',            'LIKE_NEW', N'Balo laptop chống nước',               N'Balo laptop chống nước, ngăn đệm dày, khóa kéo chắc chắn, dùng ít.',         300000, 'RESERVED',       NULL, 150),
 ('an.seller@demo.local', N'Nội thất',              'GOOD',     N'Kệ sách 5 tầng',                       N'Kệ sách gỗ 5 tầng chắc chắn, dễ tháo lắp, kích thước 60x30x170cm.',          450000, 'RESERVED',       NULL, 140),
 ('an.seller@demo.local', N'Đồ gia dụng',           'GOOD',     N'Gối ôm và chăn mỏng',                  N'Bộ gối ôm kèm chăn mỏng, đã giặt sạch, vải mềm không xù lông.',              180000, 'ACTIVE',         NULL, 130),
 -- ===== Bình Vintage =====
 ('binh.seller@demo.local', N'Nội thất',              'GOOD',     N'Bàn học gỗ kèm ghế',                   N'Bàn học gỗ kèm ghế, mặt bàn rộng 1m2, còn chắc chắn, có ngăn kéo.',          800000, 'ACTIVE',         NULL, 220),
 ('binh.seller@demo.local', N'Đồ gia dụng',           'LIKE_NEW', N'Đèn bàn LED chống cận',                N'Đèn bàn LED chống cận, 3 chế độ sáng, dùng ít, còn bảo hành một phần.',      150000, 'RESERVED',       NULL, 210),
 ('binh.seller@demo.local', N'Đồ gia dụng',           'GOOD',     N'Bộ nồi inox 3 món',                    N'Bộ nồi inox 3 món đáy từ, sáng bóng, dùng được cho mọi loại bếp.',           220000, 'ACTIVE',         NULL, 200),
 ('binh.seller@demo.local', N'Sách & văn phòng phẩm', 'GOOD',     N'Truyện tranh Doraemon trọn bộ',        N'Trọn bộ truyện tranh Doraemon 45 tập, giấy hơi ngả vàng nhưng đầy đủ.',      650000, 'ACTIVE',         NULL, 190),
 ('binh.seller@demo.local', N'Thể thao & dã ngoại', 'GOOD',     N'Vợt cầu lông Yonex Astrox',            N'Vợt cầu lông Yonex Astrox, đã căng dây mới, kèm bao vợt và cuốn cán.',       1200000, 'ACTIVE',         NULL, 180),
 ('binh.seller@demo.local', N'Đồ chơi & mẹ bé',     'FAIR',     N'Xe đẩy em bé gấp gọn',                 N'Xe đẩy em bé gấp gọn, bánh xe chạy êm, mui che còn tốt, có đai an toàn.',     900000, 'ACTIVE',         NULL, 170),
 ('binh.seller@demo.local', N'Nội thất',              'GOOD',     N'Tủ quần áo vải 3 ngăn',                N'Tủ quần áo vải 3 ngăn khung thép, gọn nhẹ, phù hợp phòng trọ.',             250000, 'RESERVED',       NULL, 160),
 ('binh.seller@demo.local', N'Đồ gia dụng',           'GOOD',     N'Máy xay sinh tố Philips',              N'Máy xay sinh tố Philips công suất 600W, cối thủy tinh, chạy mạnh và êm.',     400000, 'RESERVED',       NULL, 120),
 ('binh.seller@demo.local', N'Đồ gia dụng',           'GOOD',     N'Bàn ủi hơi nước Tefal',                N'Bàn ủi hơi nước Tefal, hơi mạnh, đế chống dính còn tốt, đã bán cho khách.',    350000, 'SOLD',           NULL, 110),
 ('binh.seller@demo.local', N'Đồ gia dụng',           'FAIR',     N'Quạt đứng Senko',                      N'Quạt đứng Senko 3 cấp gió, chạy êm, cánh quạt sạch sẽ, thân hơi trầy nhẹ.',    350000, 'ACTIVE',         NULL, 105),
 ('binh.seller@demo.local', N'Đồ chơi & mẹ bé',     'GOOD',     N'Bộ xếp hình Lego City',                N'Bộ xếp hình Lego City đầy đủ chi tiết, có hộp, giá tốt cho người sưu tầm.',  500000, 'PENDING_REVIEW', NULL, 15)
) v (EmailChuShop, TenDanhMuc, MaTinhTrang, Ten, MoTa, Gia, TrangThai, LyDoGo, GioTruoc)
JOIN NguoiDung nd ON nd.Email = v.EmailChuShop
JOIN CuaHang ch   ON ch.MaNguoiDung = nd.MaNguoiDung
LEFT JOIN DanhMuc dm          ON dm.Ten = v.TenDanhMuc
LEFT JOIN TinhTrangSanPham tt ON tt.Ma  = v.MaTinhTrang
WHERE NOT EXISTS (SELECT 1 FROM SanPham s WHERE s.MaCuaHang = ch.MaCuaHang AND s.Ten = v.Ten);

-- ---------------------------------------------------------------------
-- 7. Ảnh sản phẩm (2 ảnh / sản phẩm, trừ bản nháp). File chưa có thật: xem README.
-- ---------------------------------------------------------------------
INSERT AnhSanPham (MaSanPham, DuongDan, ThuTu, LaAnhChinh, DungLuong)
SELECT sp.MaSanPham,
       N'/uploads/seed/sp' + RIGHT('000' + CAST(sp.MaSanPham AS varchar(10)), 3) + N'-' + CAST(n.ThuTu AS varchar(2)) + N'.jpg',
       n.ThuTu,
       CASE WHEN n.ThuTu = 1 THEN 1 ELSE 0 END,
       200000 + sp.MaSanPham * 1000 + n.ThuTu * 100
FROM SanPham sp
JOIN CuaHang ch   ON ch.MaCuaHang = sp.MaCuaHang
JOIN NguoiDung nd ON nd.MaNguoiDung = ch.MaNguoiDung
CROSS JOIN (VALUES (1), (2)) n (ThuTu)
WHERE nd.Email LIKE '%@demo.local' AND sp.TrangThai <> 'DRAFT'
  AND NOT EXISTS (SELECT 1 FROM AnhSanPham a WHERE a.MaSanPham = sp.MaSanPham AND a.ThuTu = n.ThuTu);

-- ---------------------------------------------------------------------
-- 8. Lịch sử trạng thái sản phẩm (1 dòng khởi tạo / sản phẩm demo)
-- ---------------------------------------------------------------------
DECLARE @SpChuaLichSu TABLE (Id int PRIMARY KEY);
INSERT @SpChuaLichSu (Id)
SELECT sp.MaSanPham
FROM SanPham sp
JOIN CuaHang ch   ON ch.MaCuaHang = sp.MaCuaHang
JOIN NguoiDung nd ON nd.MaNguoiDung = ch.MaNguoiDung
WHERE nd.Email LIKE '%@demo.local'
  AND NOT EXISTS (SELECT 1 FROM LichSuTrangThaiSanPham h WHERE h.MaSanPham = sp.MaSanPham);

INSERT LichSuTrangThaiSanPham (MaSanPham, TrangThaiCu, TrangThaiMoi, LoaiNguoiThucHien, NguoiThucHienId, LyDo, ThoiGian)
SELECT sp.MaSanPham, NULL, sp.TrangThai, 'SYSTEM', NULL, N'Dữ liệu mẫu', sp.NgayTao
FROM @SpChuaLichSu x JOIN SanPham sp ON sp.MaSanPham = x.Id;

-- ---------------------------------------------------------------------
-- 9. Lượt kiểm duyệt: đã duyệt / bị từ chối / đang chờ (cũ nhất = id nhỏ nhất)
-- ---------------------------------------------------------------------
DECLARE @SpChuaDuyet TABLE (Id int PRIMARY KEY);
INSERT @SpChuaDuyet (Id)
SELECT sp.MaSanPham
FROM SanPham sp
JOIN CuaHang ch   ON ch.MaCuaHang = sp.MaCuaHang
JOIN NguoiDung nd ON nd.MaNguoiDung = ch.MaNguoiDung
WHERE nd.Email LIKE '%@demo.local' AND sp.TrangThai <> 'DRAFT'
  AND NOT EXISTS (SELECT 1 FROM LuotKiemDuyet l WHERE l.MaSanPham = sp.MaSanPham);

INSERT LuotKiemDuyet (MaSanPham, ThoiGianGui, KetQuaRule, QuyetDinh, LyDo, MaQuanTriVien, ThoiGianQuyetDinh)
SELECT sp.MaSanPham, g.Gui, N'{"passed":true,"errors":[]}',
       CASE sp.TrangThai WHEN 'PENDING_REVIEW' THEN NULL WHEN 'REJECTED' THEN 'REJECTED' ELSE 'APPROVED' END,
       CASE WHEN sp.TrangThai = 'REJECTED'
            THEN N'Mô tả quá ngắn, thiếu thông tin về tình trạng và xuất xứ. Vui lòng bổ sung rồi gửi lại.' END,
       CASE WHEN sp.TrangThai = 'PENDING_REVIEW' THEN NULL ELSE @AdminId END,
       CASE WHEN sp.TrangThai = 'PENDING_REVIEW' THEN NULL ELSE DATEADD(HOUR, 1, g.Gui) END
FROM @SpChuaDuyet x
JOIN SanPham sp ON sp.MaSanPham = x.Id
CROSS APPLY (SELECT DATEADD(HOUR, sp.MaSanPham - 500, @Now) AS Gui) g;

-- ---------------------------------------------------------------------
-- 10. Phân tích AI cho các sản phẩm đang chờ duyệt (QUEUED / DONE)
-- ---------------------------------------------------------------------
INSERT PhanTichAI (MaLuotKiemDuyet, TrangThai, SoLanThu, MucRuiRo, DauHieu, DeXuat, GiaiThich,
                        PhienBanMoHinh, ThoiGianXuLyMs, ThoiGianHoanThanh)
SELECT l.MaLuotKiemDuyet, v.TrangThai, v.SoLanThu, v.MucRuiRo, v.DauHieu, v.DeXuat, v.GiaiThich,
       CASE WHEN v.TrangThai = 'DONE' THEN 'demo-v0' END,
       CASE WHEN v.TrangThai = 'DONE' THEN v.Ms END,
       CASE WHEN v.TrangThai = 'DONE' THEN @Now END
FROM (VALUES
    (N'Tai nghe Sony WH-1000XM3', 'DONE', 1, 'LOW', N'[]', 'APPROVE',
        N'Nội dung nhất quán với danh mục, giá nằm trong mặt bằng chung, không phát hiện dấu hiệu bất thường.', 850),
    (N'Máy ảnh Canon EOS 700D kèm lens kit', 'QUEUED', 0, NULL, NULL, NULL, NULL, NULL),
    (N'Bộ xếp hình Lego City', 'DONE', 1, 'MEDIUM',
        N'[{"loai":"GIA_BAT_THUONG","bang_chung":"Giá thấp hơn đáng kể so với mặt bằng của các bộ cùng loại","do_tin_cay":0.62}]',
        'NEEDS_REVIEW', N'Giá có thể thấp bất thường, đề nghị Admin kiểm tra thêm hình ảnh và nguồn gốc.', 1120)
) v (TenSanPham, TrangThai, SoLanThu, MucRuiRo, DauHieu, DeXuat, GiaiThich, Ms)
JOIN SanPham sp      ON sp.Ten = v.TenSanPham
JOIN LuotKiemDuyet l   ON l.MaSanPham = sp.MaSanPham AND l.QuyetDinh IS NULL
WHERE NOT EXISTS (SELECT 1 FROM PhanTichAI p WHERE p.MaLuotKiemDuyet = l.MaLuotKiemDuyet);

-- ---------------------------------------------------------------------
-- 11. Giỏ hàng
-- ---------------------------------------------------------------------
INSERT GioHang (MaNguoiDung, MaSanPham, DaChon, ThoiGianThem)
SELECT nd.MaNguoiDung, sp.MaSanPham, v.DaChon, DATEADD(MINUTE, -v.PhutTruoc, @Now)
FROM (VALUES
    ('chi.buyer@demo.local',  N'iPhone 11 64GB màu đen',    1, 90),
    ('chi.buyer@demo.local',  N'Bàn học gỗ kèm ghế',        0, 60),
    ('chi.buyer@demo.local',  N'Vợt cầu lông Yonex Astrox', 1, 30),
    ('dung.buyer@demo.local', N'Bàn phím cơ Keychron K2',   1, 45),
    ('dung.buyer@demo.local', N'Truyện tranh Doraemon trọn bộ', 1, 20)
) v (EmailNguoiMua, TenSanPham, DaChon, PhutTruoc)
JOIN NguoiDung nd ON nd.Email = v.EmailNguoiMua
JOIN SanPham sp    ON sp.Ten = v.TenSanPham
WHERE NOT EXISTS (SELECT 1 FROM GioHang g WHERE g.MaNguoiDung = nd.MaNguoiDung AND g.MaSanPham = sp.MaSanPham);

-- ---------------------------------------------------------------------
-- 12. Đơn hàng: đủ 6 trạng thái. Hang = mức tiến độ (0 PENDING .. 4 COMPLETED)
-- ---------------------------------------------------------------------
INSERT DonHang (MaDon, MaNguoiDung, MaCuaHang, TenNguoiNhan, SoDienThoaiNhan, DiaChiNhan,
                    PhuongThucThanhToan, TrangThaiThanhToan, TrangThai, TongTien,
                    NguoiHuy, LyDoHuy, ThoiGianHuy, ThoiGianXacNhan, ThoiGianBatDauGiao,
                    ThoiGianDaGiao, ThoiGianHoanTat, NgayTao)
SELECT v.MaDon, mua.MaNguoiDung, ch.MaCuaHang, mua.HoTen, mua.SoDienThoai, mua.DiaChiMacDinh,
       v.PhuongThuc, v.ThanhToan, v.TrangThai, v.TongTien,
       v.NguoiHuy, v.LyDoHuy,
       CASE WHEN v.TrangThai = 'CANCELLED' THEN DATEADD(HOUR, 5,  t.NgayTao) END,
       CASE WHEN v.Hang >= 1 THEN DATEADD(HOUR, 3,  t.NgayTao) END,
       CASE WHEN v.Hang >= 2 THEN DATEADD(HOUR, 6,  t.NgayTao) END,
       CASE WHEN v.Hang >= 3 THEN DATEADD(HOUR, 30, t.NgayTao) END,
       CASE WHEN v.Hang >= 4 THEN DATEADD(HOUR, 50, t.NgayTao) END,
       t.NgayTao
FROM (VALUES
    ('DH20261001-000001', 'chi.buyer@demo.local',   'binh.seller@demo.local', 'PENDING',    'COD',           'UNPAID', 400000, 1,  0, NULL,     NULL),
    ('DH20261001-000002', 'dung.buyer@demo.local', 'binh.seller@demo.local', 'CONFIRMED', 'COD',           'UNPAID', 400000, 2,  1, NULL,     NULL),
    ('DH20261001-000003', 'chi.buyer@demo.local',   'an.seller@demo.local',   'SHIPPING',  'BANK_TRANSFER', 'PAID',   300000, 3,  2, NULL,     NULL),
    ('DH20261001-000004', 'dung.buyer@demo.local', 'an.seller@demo.local',   'DELIVERED', 'COD',           'UNPAID', 450000, 5,  3, NULL,     NULL),
    ('DH20261001-000005', 'chi.buyer@demo.local',   'binh.seller@demo.local', 'COMPLETED', 'COD',           'PAID',   350000, 10, 4, NULL,     NULL),
    ('DH20261001-000006', 'dung.buyer@demo.local', 'an.seller@demo.local',   'CANCELLED', 'COD',           'UNPAID', 180000, 4,  0, 'BUYER',  N'Đổi ý, không muốn mua nữa.'),
    ('DH20261001-000007', 'chi.buyer@demo.local',   'binh.seller@demo.local', 'CANCELLED', 'COD',           'UNPAID', 350000, 6,  1, 'SELLER', N'Sản phẩm bị hư hỏng khi dọn kho, không thể giao.')
) v (MaDon, EmailMua, EmailShop, TrangThai, PhuongThuc, ThanhToan, TongTien, NgayTruoc, Hang, NguoiHuy, LyDoHuy)
JOIN NguoiDung mua ON mua.Email = v.EmailMua
JOIN NguoiDung chu ON chu.Email = v.EmailShop
JOIN CuaHang ch    ON ch.MaNguoiDung = chu.MaNguoiDung
CROSS APPLY (SELECT DATEADD(DAY, -v.NgayTruoc, @Now) AS NgayTao) t
WHERE NOT EXISTS (SELECT 1 FROM DonHang d WHERE d.MaDon = v.MaDon);

-- Chi tiết đơn (snapshot). Đơn còn hiệu lực thì DangGiuCho = 1
INSERT ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, Gia, AnhDaiDien, DangGiuCho)
SELECT dh.MaDonHang, sp.MaSanPham, sp.Ten, sp.Gia, a.DuongDan,
       CASE WHEN dh.TrangThai IN ('PENDING','CONFIRMED','SHIPPING','DELIVERED') THEN 1 ELSE 0 END
FROM (VALUES
    ('DH20261001-000001', N'Tủ quần áo vải 3 ngăn'),
    ('DH20261001-000001', N'Đèn bàn LED chống cận'),
    ('DH20261001-000002', N'Máy xay sinh tố Philips'),
    ('DH20261001-000003', N'Balo laptop chống nước'),
    ('DH20261001-000004', N'Kệ sách 5 tầng'),
    ('DH20261001-000005', N'Bàn ủi hơi nước Tefal'),
    ('DH20261001-000006', N'Gối ôm và chăn mỏng'),
    ('DH20261001-000007', N'Quạt đứng Senko')
) m (MaDon, TenSanPham)
JOIN DonHang dh        ON dh.MaDon = m.MaDon
JOIN SanPham sp        ON sp.Ten = m.TenSanPham AND sp.MaCuaHang = dh.MaCuaHang
LEFT JOIN AnhSanPham a ON a.MaSanPham = sp.MaSanPham AND a.LaAnhChinh = 1
WHERE NOT EXISTS (SELECT 1 FROM ChiTietDonHang ct WHERE ct.MaDonHang = dh.MaDonHang AND ct.MaSanPham = sp.MaSanPham);

-- Lịch sử trạng thái đơn
DECLARE @DonMoi TABLE (Id int PRIMARY KEY);
INSERT @DonMoi (Id)
SELECT dh.MaDonHang
FROM DonHang dh
WHERE dh.MaDon IN ('DH20261001-000001','DH20261001-000002','DH20261001-000003','DH20261001-000004',
                  'DH20261001-000005','DH20261001-000006','DH20261001-000007')
  AND NOT EXISTS (SELECT 1 FROM LichSuTrangThaiDonHang h WHERE h.MaDonHang = dh.MaDonHang);

INSERT LichSuTrangThaiDonHang (MaDonHang, TrangThaiCu, TrangThaiMoi, LoaiNguoiThucHien, NguoiThucHienId, LyDo, ThoiGian)
SELECT dh.MaDonHang, b.Cu, b.Moi, b.Loai,
       CASE b.Loai WHEN 'BUYER' THEN dh.MaNguoiDung WHEN 'SELLER' THEN ch.MaNguoiDung END,
       NULL, DATEADD(HOUR, b.Buoc * 3, dh.NgayTao)
FROM @DonMoi m
JOIN DonHang dh ON dh.MaDonHang = m.Id
JOIN CuaHang ch ON ch.MaCuaHang = dh.MaCuaHang
CROSS APPLY (SELECT CASE dh.TrangThai
                        WHEN 'PENDING' THEN 0 WHEN 'CONFIRMED' THEN 1 WHEN 'SHIPPING' THEN 2
                        WHEN 'DELIVERED' THEN 3 WHEN 'COMPLETED' THEN 4
                        ELSE CASE WHEN dh.NguoiHuy = 'SELLER' THEN 1 ELSE 0 END END AS Hang) r
JOIN (VALUES
        (0, CAST(NULL AS varchar(15)), 'PENDING',    'BUYER'),
        (1, 'PENDING',    'CONFIRMED', 'SELLER'),
        (2, 'CONFIRMED', 'SHIPPING',  'SELLER'),
        (3, 'SHIPPING',  'DELIVERED', 'SELLER'),
        (4, 'DELIVERED', 'COMPLETED', 'BUYER')
     ) b (Buoc, Cu, Moi, Loai) ON b.Buoc <= r.Hang;

INSERT LichSuTrangThaiDonHang (MaDonHang, TrangThaiCu, TrangThaiMoi, LoaiNguoiThucHien, NguoiThucHienId, LyDo, ThoiGian)
SELECT dh.MaDonHang,
       CASE WHEN dh.NguoiHuy = 'SELLER' THEN 'CONFIRMED' ELSE 'PENDING' END,
       'CANCELLED', dh.NguoiHuy,
       CASE dh.NguoiHuy WHEN 'BUYER' THEN dh.MaNguoiDung WHEN 'SELLER' THEN ch.MaNguoiDung END,
       dh.LyDoHuy, dh.ThoiGianHuy
FROM @DonMoi m
JOIN DonHang dh ON dh.MaDonHang = m.Id
JOIN CuaHang ch ON ch.MaCuaHang = dh.MaCuaHang
WHERE dh.TrangThai = 'CANCELLED';

-- ---------------------------------------------------------------------
-- 13. Nhật ký mẫu
-- ---------------------------------------------------------------------
INSERT NhatKyHeThong (MaNguoiDung, HanhDong, LoaiDoiTuong, DoiTuongId, KetQua, LyDo)
SELECT @AdminId, 'SEED_DATA', 'HeThong', NULL, 'SUCCESS', N'Khởi tạo dữ liệu mẫu'
WHERE NOT EXISTS (SELECT 1 FROM NhatKyHeThong WHERE HanhDong = 'SEED_DATA');

COMMIT TRANSACTION;

-- ---------------------------------------------------------------------
-- Kiểm tra nhanh sau khi seed
-- ---------------------------------------------------------------------
SELECT N'NguoiDung' AS Bang, COUNT(*) AS SoDong FROM NguoiDung
UNION ALL SELECT N'CuaHang',          COUNT(*) FROM CuaHang
UNION ALL SELECT N'DanhMuc',          COUNT(*) FROM DanhMuc
UNION ALL SELECT N'TinhTrangSanPham', COUNT(*) FROM TinhTrangSanPham
UNION ALL SELECT N'SanPham',          COUNT(*) FROM SanPham
UNION ALL SELECT N'AnhSanPham',       COUNT(*) FROM AnhSanPham
UNION ALL SELECT N'LuotKiemDuyet',    COUNT(*) FROM LuotKiemDuyet
UNION ALL SELECT N'PhanTichAI',       COUNT(*) FROM PhanTichAI
UNION ALL SELECT N'TuKhoaCam',        COUNT(*) FROM TuKhoaCam
UNION ALL SELECT N'GioHang',          COUNT(*) FROM GioHang
UNION ALL SELECT N'DonHang',          COUNT(*) FROM DonHang
UNION ALL SELECT N'ChiTietDonHang',   COUNT(*) FROM ChiTietDonHang;

SELECT SanPhamTrangThai = TrangThai, SoSanPham = COUNT(*)
FROM SanPham WHERE DaXoa = 0 GROUP BY TrangThai ORDER BY TrangThai;

-- Đơn có TongTien lệch với tổng chi tiết (kỳ vọng: 0 dòng)
SELECT dh.MaDon, dh.TongTien, fn_TinhTongTienDonHang(dh.MaDonHang) AS TongTheoChiTiet
FROM DonHang dh
WHERE dh.TongTien <> fn_TinhTongTienDonHang(dh.MaDonHang);
GO