SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ARITHABORT ON;
GO

USE SecondHandMarketplace;
GO
-- CHỈ CHẠY TRONG DEV/TEST. run-db.bat tự bỏ qua file này khi DB_MODE=deploy.
-- Thứ tự seed: tài khoản/sản phẩm -> địa chỉ shop -> đơn hàng -> tranh chấp/báo cáo -> đánh giá.

-- ===== Nội dung giữ nguyên từ 29_seed_demo_goc.sql =====
-- Dữ liệu demo cho môi trường Development/Testing. Mật khẩu seed: Admin@12345. Không chạy trên Production.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ARITHABORT ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

USE SecondHandMarketplace;
GO

IF EXISTS (SELECT 1 FROM dbo.NguoiDung WHERE Email IN ('admin@secondhand.local', 'an.seller@demo.local', 'binh.seller@demo.local', 'chi.buyer@demo.local', 'dung.buyer@demo.local', 'locked@demo.local', 'noshop@demo.local'))
    THROW 51090, N'Seed demo đã có dữ liệu. Chỉ chạy file này một lần trên database dev/test trống; reset trước nếu cần chạy lại.', 1;
GO

DECLARE @MatKhauHash varchar(100) = '$2y$11$tFZdjm5I7Pl1SMMfYeaAweEjlCFoRZSY.bHEL40rCIHCPxA.SxkLK';
DECLARE @Now datetime2(3) = SYSUTCDATETIME();
DECLARE @AdminId int;
BEGIN TRANSACTION;

INSERT NguoiDung (HoTen, Email, SoDienThoai, MatKhauBam, VaiTro, TrangThai, LyDoKhoa, ThoiGianKhoa, DiaChiMacDinh)
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

INSERT CuaHang (MaNguoiDung, TenShop, SoDienThoaiLienHe, MoTa)
SELECT nd.MaNguoiDung, v.TenShop, v.SoDienThoai, v.MoTa
FROM (VALUES
    ('an.seller@demo.local',   N'An Store - Đồ cũ chất lượng', '0901000001', N'Chuyên đồ điện tử, thời trang và xe đạp đã qua sử dụng, kiểm tra kỹ trước khi bán.'),
    ('binh.seller@demo.local', N'Bình Vintage',                '0901000002', N'Đồ gia dụng, nội thất và đồ dùng gia đình giá mềm.')
) v (Email, TenShop, SoDienThoai, MoTa)
JOIN NguoiDung nd ON nd.Email = v.Email
WHERE NOT EXISTS (SELECT 1 FROM CuaHang c WHERE c.MaNguoiDung = nd.MaNguoiDung);

INSERT SanPham (MaCuaHang, MaDanhMuc, MaTinhTrang, Ten, ChuoiTimKiem, MoTa, Gia, TrangThai, LyDoGo, NgayTao)
SELECT ch.MaCuaHang, dm.MaDanhMuc, tt.MaTinhTrang, v.Ten, dbo.fn_ChuanHoaChuoi(v.Ten), v.MoTa, v.Gia, v.TrangThai, v.LyDoGo, DATEADD(HOUR, -v.GioTruoc, @Now)
FROM (VALUES
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
) v (EmailChuShop, TenDanhMuc, CodeTinhTrang, Ten, MoTa, Gia, TrangThai, LyDoGo, GioTruoc)
JOIN NguoiDung nd ON nd.Email = v.EmailChuShop
JOIN CuaHang ch   ON ch.MaNguoiDung = nd.MaNguoiDung
LEFT JOIN DanhMuc dm          ON dm.Ten = v.TenDanhMuc
LEFT JOIN TinhTrangSanPham tt ON tt.Ma  = v.CodeTinhTrang
WHERE NOT EXISTS (SELECT 1 FROM SanPham s WHERE s.MaCuaHang = ch.MaCuaHang AND s.Ten = v.Ten);

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
JOIN SanPham sp        ON sp.Ten = v.TenSanPham
JOIN LuotKiemDuyet l   ON l.MaSanPham = sp.MaSanPham AND l.QuyetDinh IS NULL
WHERE NOT EXISTS (SELECT 1 FROM PhanTichAI p WHERE p.MaLuotKiemDuyet = l.MaLuotKiemDuyet);

INSERT GioHang (MaNguoiDung, MaSanPham, DaChon, ThoiGianThem)
SELECT nd.MaNguoiDung, sp.MaSanPham, v.DaChon, DATEADD(MINUTE, -v.PhutTruoc, @Now)
FROM (VALUES
    ('chi.buyer@demo.local',  N'iPhone 11 64GB màu đen',        1, 90),
    ('chi.buyer@demo.local',  N'Bàn học gỗ kèm ghế',            0, 60),
    ('chi.buyer@demo.local',  N'Vợt cầu lông Yonex Astrox',     1, 30),
    ('dung.buyer@demo.local', N'Bàn phím cơ Keychron K2',       1, 45),
    ('dung.buyer@demo.local', N'Truyện tranh Doraemon trọn bộ', 1, 20)
) v (EmailNguoiMua, TenSanPham, DaChon, PhutTruoc)
JOIN NguoiDung nd ON nd.Email = v.EmailNguoiMua
JOIN SanPham sp   ON sp.Ten = v.TenSanPham
WHERE NOT EXISTS (SELECT 1 FROM GioHang g WHERE g.MaNguoiDung = nd.MaNguoiDung AND g.MaSanPham = sp.MaSanPham);

INSERT DonHang (MaDon, MaNguoiDung, MaCuaHang, TenNguoiNhan, SoDienThoaiNhan, DiaChiNhan,
                TienHang, PhiVanChuyen, TongTien,
                PhuongThucThanhToan, TrangThaiThanhToan, TrangThai,
                NguoiHuy, LyDoHuy, ThoiGianHuy, ThoiGianXacNhan, ThoiGianBatDauGiao,
                ThoiGianDaGiao, ThoiGianHoanTat, NgayTao)
SELECT v.MaDon, mua.MaNguoiDung, ch.MaCuaHang, mua.HoTen, mua.SoDienThoai, mua.DiaChiMacDinh,
       v.TongTien, 0, v.TongTien,
       v.PhuongThuc, v.ThanhToan, v.TrangThai,
       v.NguoiHuy, v.LyDoHuy,
       CASE WHEN v.TrangThai = 'CANCELLED' THEN DATEADD(HOUR, 5,  t.NgayTao) END,
       CASE WHEN v.Hang >= 1 THEN DATEADD(HOUR, 3,  t.NgayTao) END,
       CASE WHEN v.Hang >= 2 THEN DATEADD(HOUR, 6,  t.NgayTao) END,
       CASE WHEN v.Hang >= 3 THEN DATEADD(HOUR, 30, t.NgayTao) END,
       CASE WHEN v.Hang >= 4 THEN DATEADD(HOUR, 50, t.NgayTao) END,
       t.NgayTao
FROM (VALUES
    ('DH20261001-000001', 'chi.buyer@demo.local',  'binh.seller@demo.local', 'PENDING',   'COD',           'UNPAID', 400000, 1,  0, NULL,     NULL),
    ('DH20261001-000002', 'dung.buyer@demo.local', 'binh.seller@demo.local', 'CONFIRMED', 'COD',           'UNPAID', 400000, 2,  1, NULL,     NULL),
    ('DH20261001-000003', 'chi.buyer@demo.local',  'an.seller@demo.local',   'SHIPPING',  'BANK_TRANSFER', 'PAID',   300000, 3,  2, NULL,     NULL),
    ('DH20261001-000004', 'dung.buyer@demo.local', 'an.seller@demo.local',   'DELIVERED', 'COD',           'UNPAID', 450000, 5,  3, NULL,     NULL),
    ('DH20261001-000005', 'chi.buyer@demo.local',  'binh.seller@demo.local', 'COMPLETED', 'COD',           'PAID',   350000, 10, 4, NULL,     NULL),
    ('DH20261001-000006', 'dung.buyer@demo.local', 'an.seller@demo.local',   'CANCELLED', 'COD',           'UNPAID', 180000, 4,  0, 'BUYER',  N'Đổi ý, không muốn mua nữa.'),
    ('DH20261001-000007', 'chi.buyer@demo.local',  'binh.seller@demo.local', 'CANCELLED', 'COD',           'UNPAID', 350000, 6,  1, 'SELLER', N'Sản phẩm bị hư hỏng khi dọn kho, không thể giao.')
) v (MaDon, EmailMua, EmailShop, TrangThai, PhuongThuc, ThanhToan, TongTien, NgayTruoc, Hang, NguoiHuy, LyDoHuy)
JOIN NguoiDung mua ON mua.Email = v.EmailMua
JOIN NguoiDung chu ON chu.Email = v.EmailShop
JOIN CuaHang ch    ON ch.MaNguoiDung = chu.MaNguoiDung
CROSS APPLY (SELECT DATEADD(DAY, -v.NgayTruoc, @Now) AS NgayTao) t
WHERE NOT EXISTS (SELECT 1 FROM DonHang d WHERE d.MaDon = v.MaDon);

INSERT ChiTietDonHang (MaDonHang, MaSanPham, TenSanPham, AnhDaiDien, Gia, DangGiuCho)
SELECT dh.MaDonHang, sp.MaSanPham, sp.Ten, a.DuongDan, sp.Gia,
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
        (0, CAST(NULL AS varchar(15)), 'PENDING',   'BUYER'),
        (1, 'PENDING',   'CONFIRMED', 'SELLER'),
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

INSERT NhatKyHeThong (MaNguoiDung, HanhDong, LoaiDoiTuong, DoiTuongId, KetQua, LyDo)
SELECT @AdminId, 'SEED_DATA', 'HeThong', NULL, 'SUCCESS', N'Khởi tạo dữ liệu mẫu'
WHERE NOT EXISTS (SELECT 1 FROM NhatKyHeThong WHERE HanhDong = 'SEED_DATA');


COMMIT TRANSACTION;
GO

GO

-- ===== Nội dung giữ nguyên từ 30_seed_dia_chi_shop.sql =====
USE SecondHandMarketplace;
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ARITHABORT ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

-- =============================================================================
-- A7: Seed người dùng + cửa hàng có địa chỉ/ngân hàng (CHỈ dùng cho dev/kiểm thử, KHÔNG chạy ở môi trường deploy).
--   Chạy sau 21_dia_chi_khu_vuc.sql (và sau 29_seed_demo_goc.sql của E nếu có).
--   - 2 admin, 12 người bán (mỗi người 1 shop), 25 người mua. Mật khẩu chung: Admin@12345
--     (hash BCrypt copy nguyên từ database/09_seed_data.sql, không tự sinh).
--   - Email dạng ...@demo.local, không trùng 6 tài khoản mẫu của 09_seed_data.sql.
--   - Tỉnh/xã lấy bằng JOIN theo TÊN, không gán cứng ID.
--   - Cố ý: nguoiban05 chưa nhập ngân hàng (D test ẩn QR); nguoiban12 chưa có sản phẩm (test trang shop rỗng).
--   - Danh sách tài khoản/shop: docs/a/A7-tai-khoan-seed.md
-- Chạy lại không lỗi và không nhân đôi dữ liệu (WHERE NOT EXISTS theo email).
-- =============================================================================

DECLARE @MatKhauHash varchar(100) = '$2y$11$tFZdjm5I7Pl1SMMfYeaAweEjlCFoRZSY.bHEL40rCIHCPxA.SxkLK';

BEGIN TRANSACTION;

-- 1. Người dùng ---------------------------------------------------------------
INSERT NguoiDung (HoTen, Email, SoDienThoai, MatKhauBam, VaiTro, TrangThai, DiaChiMacDinh)
SELECT v.HoTen, v.Email, v.SoDienThoai, @MatKhauHash, v.VaiTro, 'ACTIVE', v.DiaChi
FROM (VALUES
    -- Quản trị
    (N'Quản trị viên Demo 1', 'quantri01@demo.local', NULL, 'ADMIN', NULL),
    (N'Quản trị viên Demo 2', 'quantri02@demo.local', NULL, 'ADMIN', NULL),
    -- Người bán (mỗi người có 1 shop ở bước 2)
    (N'Nguyễn Hoàng Long',  'nguoiban01@demo.local', '0902000001', 'USER', N'15 Đồng Khởi, TP. Hồ Chí Minh'),
    (N'Trần Thu Hà',        'nguoiban02@demo.local', '0902000002', 'USER', N'42 Lê Thánh Tôn, TP. Hồ Chí Minh'),
    (N'Lê Quang Huy',       'nguoiban03@demo.local', '0902000003', 'USER', N'88 Võ Văn Ngân, TP. Hồ Chí Minh'),
    (N'Phạm Ngọc Mai',      'nguoiban04@demo.local', '0902000004', 'USER', N'210 Quang Trung, TP. Hồ Chí Minh'),
    (N'Võ Minh Khôi',       'nguoiban05@demo.local', '0902000005', 'USER', N'33 Điện Biên Phủ, TP. Hồ Chí Minh'),
    (N'Đặng Thị Lan',       'nguoiban06@demo.local', '0902000006', 'USER', N'7 Hoàng Văn Thụ, TP. Hồ Chí Minh'),
    (N'Bùi Anh Tuấn',       'nguoiban07@demo.local', '0902000007', 'USER', N'56 Nguyễn Văn Trỗi, TP. Hồ Chí Minh'),
    (N'Hoàng Thanh Tâm',    'nguoiban08@demo.local', '0902000008', 'USER', N'19 Trần Thái Tông, Hà Nội'),
    (N'Ngô Bảo Châu',       'nguoiban09@demo.local', '0902000009', 'USER', N'101 Tây Sơn, Hà Nội'),
    (N'Dương Gia Hân',      'nguoiban10@demo.local', '0902000010', 'USER', N'64 Bạch Đằng, Đà Nẵng'),
    (N'Lý Tuấn Kiệt',       'nguoiban11@demo.local', '0902000011', 'USER', N'25 Hòa Bình, Cần Thơ'),
    (N'Mai Phương Thảo',    'nguoiban12@demo.local', '0902000012', 'USER', N'12 Nguyễn Ái Quốc, Đồng Nai'),
    -- Người mua
    (N'Nguyễn Gia Bảo',   'nguoimua01@demo.local', '0903000001', 'USER', N'21 Nguyễn Huệ, TP. Hồ Chí Minh'),
    (N'Trần Khánh Linh',  'nguoimua02@demo.local', '0903000002', 'USER', N'35 Lý Tự Trọng, TP. Hồ Chí Minh'),
    (N'Lê Minh Anh',      'nguoimua03@demo.local', '0903000003', 'USER', N'48 Pasteur, TP. Hồ Chí Minh'),
    (N'Phạm Đức Thịnh',   'nguoimua04@demo.local', '0903000004', 'USER', N'9 Nam Kỳ Khởi Nghĩa, TP. Hồ Chí Minh'),
    (N'Võ Thảo Nguyên',   'nguoimua05@demo.local', '0903000005', 'USER', N'72 Cộng Hòa, TP. Hồ Chí Minh'),
    (N'Đỗ Hải Đăng',      'nguoimua06@demo.local', '0903000006', 'USER', N'14 Phan Xích Long, TP. Hồ Chí Minh'),
    (N'Hồ Ngọc Diệp',     'nguoimua07@demo.local', '0903000007', 'USER', N'5 Xô Viết Nghệ Tĩnh, TP. Hồ Chí Minh'),
    (N'Bùi Quốc Việt',    'nguoimua08@demo.local', '0903000008', 'USER', N'63 Lê Văn Việt, TP. Hồ Chí Minh'),
    (N'Đinh Thị Hương',   'nguoimua09@demo.local', '0903000009', 'USER', N'27 Phạm Văn Đồng, TP. Hồ Chí Minh'),
    (N'Trương Công Danh', 'nguoimua10@demo.local', '0903000010', 'USER', N'90 Nguyễn Thị Minh Khai, TP. Hồ Chí Minh'),
    (N'Lương Mỹ Duyên',   'nguoimua11@demo.local', '0903000011', 'USER', N'11 Hai Bà Trưng, TP. Hồ Chí Minh'),
    (N'Cao Thành Đạt',    'nguoimua12@demo.local', '0903000012', 'USER', N'38 Nguyễn Đình Chiểu, TP. Hồ Chí Minh'),
    (N'Phan Bích Ngọc',   'nguoimua13@demo.local', '0903000013', 'USER', N'4 Trường Chinh, TP. Hồ Chí Minh'),
    (N'Tạ Văn Phúc',      'nguoimua14@demo.local', '0903000014', 'USER', N'83 Lạc Long Quân, TP. Hồ Chí Minh'),
    (N'Hà Thanh Vân',     'nguoimua15@demo.local', '0903000015', 'USER', N'17 Cách Mạng Tháng 8, TP. Hồ Chí Minh'),
    (N'Quách Nhật Minh',  'nguoimua16@demo.local', '0903000016', 'USER', N'29 Hoàng Hoa Thám, TP. Hồ Chí Minh'),
    (N'Vương Khả Hân',    'nguoimua17@demo.local', '0903000017', 'USER', N'6 Nơ Trang Long, TP. Hồ Chí Minh'),
    (N'Lâm Gia Huy',      'nguoimua18@demo.local', '0903000018', 'USER', N'52 Trần Hưng Đạo, TP. Hồ Chí Minh'),
    (N'Kiều Bảo Trân',    'nguoimua19@demo.local', '0903000019', 'USER', N'31 Kim Mã, Hà Nội'),
    (N'Tô Anh Khoa',      'nguoimua20@demo.local', '0903000020', 'USER', N'46 Láng Hạ, Hà Nội'),
    (N'Doãn Mỹ Linh',     'nguoimua21@demo.local', '0903000021', 'USER', N'8 Trần Phú, Đà Nẵng'),
    (N'Thái Quốc Hưng',   'nguoimua22@demo.local', '0903000022', 'USER', N'77 Nguyễn Văn Linh, Đà Nẵng'),
    (N'Âu Thiên Ân',      'nguoimua23@demo.local', '0903000023', 'USER', N'23 Mậu Thân, Cần Thơ'),
    (N'Giang Hoài Nam',   'nguoimua24@demo.local', '0903000024', 'USER', N'60 Võ Thị Sáu, Đồng Nai'),
    (N'Khưu Tường Vy',    'nguoimua25@demo.local', '0903000025', 'USER', N'19 Phạm Văn Thuận, Đồng Nai')
) v (HoTen, Email, SoDienThoai, VaiTro, DiaChi)
WHERE NOT EXISTS (SELECT 1 FROM NguoiDung n WHERE n.Email = v.Email);

-- 2. Cửa hàng của 12 người bán --------------------------------------------------
INSERT CuaHang (MaNguoiDung, TenShop, SoDienThoaiLienHe, MoTa, MaTinhThanh, MaPhuongXa, DiaChiChiTiet, KhuVucHienThi,
                NganHang, MaNganHang, SoTaiKhoan, TenChuTaiKhoan)
SELECT nd.MaNguoiDung, v.TenShop, v.SoDienThoai, v.MoTa, tt.MaTinhThanh, px.MaPhuongXa, v.DiaChi, v.KhuVuc,
       v.NganHang, v.MaNganHang, v.SoTaiKhoan, v.ChuTaiKhoan
FROM (VALUES
    ('nguoiban01@demo.local', N'Long Tech Cũ',        '0902000001', N'Điện thoại, laptop và phụ kiện công nghệ đã qua sử dụng, bảo hành 7 ngày.',
        N'Thành phố Hồ Chí Minh', N'Phường Sài Gòn',    N'15 Đồng Khởi',          N'Trung tâm Quận 1 cũ', N'Vietcombank', '970436', '9704360001', N'NGUYEN HOANG LONG'),
    ('nguoiban02@demo.local', N'Hà Vintage Closet',   '0902000002', N'Quần áo, túi xách vintage tuyển chọn, còn đẹp.',
        N'Thành phố Hồ Chí Minh', N'Phường Bến Thành',  N'42 Lê Thánh Tôn',       N'Gần chợ Bến Thành',   N'MBBank',      '970422', '9704220002', N'TRAN THU HA'),
    ('nguoiban03@demo.local', N'Huy Gia Dụng',        '0902000003', N'Đồ gia dụng, nồi chảo, máy xay còn dùng tốt.',
        N'Thành phố Hồ Chí Minh', N'Phường Thủ Đức',    N'88 Võ Văn Ngân',        N'Khu Thủ Đức',         N'VietinBank',  '970415', '9704150003', N'LE QUANG HUY'),
    ('nguoiban04@demo.local', N'Mai Books',           '0902000004', N'Sách cũ, truyện, văn phòng phẩm giá mềm.',
        N'Thành phố Hồ Chí Minh', N'Phường Gò Vấp',     N'210 Quang Trung',       NULL,                   N'BIDV',        '970418', '9704180004', N'PHAM NGOC MAI'),
    ('nguoiban05@demo.local', N'Khôi Sport',          '0902000005', N'Dụng cụ thể thao, xe đạp, đồ dã ngoại.',
        N'Thành phố Hồ Chí Minh', N'Phường Bình Thạnh', N'33 Điện Biên Phủ',      NULL,                   NULL,           NULL,     NULL,         NULL),
    ('nguoiban06@demo.local', N'Lan Mẹ và Bé',        '0902000006', N'Đồ chơi, quần áo, xe đẩy trẻ em còn mới.',
        N'Thành phố Hồ Chí Minh', N'Phường Tân Bình',   N'7 Hoàng Văn Thụ',       N'Gần sân bay',         N'Techcombank', '970407', '9704070006', N'DANG THI LAN'),
    ('nguoiban07@demo.local', N'Tuấn Nội Thất',       '0902000007', N'Bàn ghế, tủ kệ, đèn trang trí đã qua sử dụng.',
        N'Thành phố Hồ Chí Minh', N'Phường Phú Nhuận',  N'56 Nguyễn Văn Trỗi',    NULL,                   N'ACB',         '970416', '9704160007', N'BUI ANH TUAN'),
    ('nguoiban08@demo.local', N'Tâm Camera Hà Nội',   '0902000008', N'Máy ảnh, ống kính, thiết bị quay phim cũ.',
        N'Thành phố Hà Nội',      N'Phường Cầu Giấy',   N'19 Trần Thái Tông',     N'Khu Cầu Giấy',        N'VPBank',      '970432', '9704320008', N'HOANG THANH TAM'),
    ('nguoiban09@demo.local', N'Châu Đồ Cổ',          '0902000009', N'Đồ sưu tầm, đồ gốm, đồng hồ cổ.',
        N'Thành phố Hà Nội',      N'Phường Đống Đa',    N'101 Tây Sơn',           N'Gần Văn Miếu',        N'TPBank',      '970423', '9704230009', N'NGO BAO CHAU'),
    ('nguoiban10@demo.local', N'Hân Biển Xanh',       '0902000010', N'Đồ lướt sóng, quần áo mùa hè, phụ kiện du lịch.',
        N'Thành phố Đà Nẵng',     N'Phường Hải Châu',   N'64 Bạch Đằng',          N'Ven sông Hàn',        N'Sacombank',   '970403', '9704030010', N'DUONG GIA HAN'),
    ('nguoiban11@demo.local', N'Kiệt Miền Tây',       '0902000011', N'Xe máy, xe đạp điện, đồ điện gia dụng.',
        N'Thành phố Cần Thơ',     N'Phường Ninh Kiều',  N'25 Hòa Bình',           N'Bến Ninh Kiều',       N'VIB',         '970441', '9704410011', N'LY TUAN KIET'),
    ('nguoiban12@demo.local', N'Thảo Shop Mới Mở',    '0902000012', N'Cửa hàng mới, chưa đăng sản phẩm nào.',
        N'Tỉnh Đồng Nai',         N'Phường Biên Hòa',   N'12 Nguyễn Ái Quốc',     NULL,                   N'OCB',         '970448', '9704480012', N'MAI PHUONG THAO')
) v (Email, TenShop, SoDienThoai, MoTa, Tinh, Xa, DiaChi, KhuVuc, NganHang, MaNganHang, SoTaiKhoan, ChuTaiKhoan)
JOIN NguoiDung nd ON nd.Email = v.Email
JOIN dbo.TinhThanh tt ON tt.TenTinhThanh = v.Tinh
JOIN dbo.PhuongXa px ON px.TenPhuongXa = v.Xa AND px.MaTinhThanh = tt.MaTinhThanh
WHERE NOT EXISTS (SELECT 1 FROM CuaHang c WHERE c.MaNguoiDung = nd.MaNguoiDung);

-- Kiểm tra đủ 12 shop: JOIN theo tên tỉnh/xã, nếu danh mục hành chính đổi tên thì báo lỗi thay vì âm thầm thiếu shop.
IF (SELECT COUNT(*) FROM CuaHang c JOIN NguoiDung n ON n.MaNguoiDung = c.MaNguoiDung
    WHERE n.Email LIKE 'nguoiban[0-9][0-9]@demo.local') <> 12
    THROW 50010, N'Seed A7: không tạo đủ 12 cửa hàng, kiểm tra tên tỉnh/xã trong 12_seed_core.sql.', 1;

-- 3. Bổ sung địa chỉ cho 2 shop mẫu cũ của 09_seed_data.sql (đã có sản phẩm) để demo trang shop và lọc khu vực ---
UPDATE c
SET c.MaTinhThanh = tt.MaTinhThanh, c.MaPhuongXa = px.MaPhuongXa, c.DiaChiChiTiet = v.DiaChi, c.KhuVucHienThi = v.KhuVuc
FROM CuaHang c
JOIN NguoiDung nd ON nd.MaNguoiDung = c.MaNguoiDung
JOIN (VALUES
    ('an.seller@demo.local',   N'Phường Sài Gòn',   N'25 Lê Lợi',                N'Trung tâm Quận 1 cũ'),
    ('binh.seller@demo.local', N'Phường Tân Định',  N'78 Cách Mạng Tháng Tám',   N'Gần ngã tư Hai Bà Trưng')
) v (Email, Xa, DiaChi, KhuVuc) ON v.Email = nd.Email
JOIN dbo.TinhThanh tt ON tt.TenTinhThanh = N'Thành phố Hồ Chí Minh'
JOIN dbo.PhuongXa px ON px.TenPhuongXa = v.Xa AND px.MaTinhThanh = tt.MaTinhThanh
WHERE c.MaTinhThanh IS NULL;

COMMIT TRANSACTION;
GO

PRINT N'Đã seed 2 admin, 12 người bán + shop, 25 người mua (mật khẩu chung: Admin@12345).';
GO

GO

-- ===== Nội dung giữ nguyên từ 32_seed_don_hang.sql =====
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

GO

-- ===== Dữ liệu mẫu TranhChap/BaoCao giữ nguyên từ 12_tranhchap_baocao.sql =====
-- 3. Dữ liệu mẫu (Seed Data) cho Tranh chấp và Báo cáo vi phạm (chỉ chèn nếu chưa có)
DECLARE @AdminId INT = (SELECT TOP 1 MaNguoiDung FROM NguoiDung WHERE VaiTro = 'ADMIN');
DECLARE @DungId  INT = (SELECT TOP 1 MaNguoiDung FROM NguoiDung WHERE Email = 'dung.buyer@demo.local');
DECLARE @ChiId   INT = (SELECT TOP 1 MaNguoiDung FROM NguoiDung WHERE Email = 'chi.buyer@demo.local');
DECLARE @DonHang4 INT = (SELECT TOP 1 MaDonHang FROM DonHang WHERE MaDon = 'DH20261001-000004');
DECLARE @DonHang5 INT = (SELECT TOP 1 MaDonHang FROM DonHang WHERE MaDon = 'DH20261001-000005');
DECLARE @Sp1 INT = (SELECT TOP 1 MaSanPham FROM SanPham WHERE DaXoa = 0);

IF @DonHang4 IS NOT NULL AND @DungId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM TranhChap WHERE MaDonHang = @DonHang4)
BEGIN
    INSERT INTO TranhChap (MaDonHang, MaNguoiTao, LyDo, BangChungAnh, TrangThai, NgayTao, NgayCapNhat)
    VALUES (@DonHang4, @DungId, N'Hàng nhận được bị trầy xước nặng, không đúng mô tả sản phẩm 99%.', '/uploads/disputes/evidence1.jpg', 'OPEN', DATEADD(DAY, -2, SYSUTCDATETIME()), DATEADD(DAY, -2, SYSUTCDATETIME()));
END

IF @DonHang5 IS NOT NULL AND @ChiId IS NOT NULL AND @AdminId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM TranhChap WHERE MaDonHang = @DonHang5)
BEGIN
    INSERT INTO TranhChap (MaDonHang, MaNguoiTao, LyDo, BangChungAnh, PhanHoiNguoiBan, TrangThai, PhanQuyet, GhiChuAdmin, NguoiXuLyId, NgayTao, NgayCapNhat, NgayGiaiQuyet)
    VALUES (@DonHang5, @ChiId, N'Giao thiếu phụ kiện đi kèm theo mô tả ban đầu.', '/uploads/disputes/evidence2.jpg', N'Shop đồng ý hoàn lại 50.000đ tiền phụ kiện bị thiếu.', 'RESOLVED', N'Chấp nhận hoàn tiền 50.000đ cho người mua.', N'Hai bên đã thống nhất qua tin nhắn.', @AdminId, DATEADD(DAY, -5, SYSUTCDATETIME()), DATEADD(DAY, -1, SYSUTCDATETIME()), DATEADD(DAY, -1, SYSUTCDATETIME()));
END

IF @ChiId IS NOT NULL AND @Sp1 IS NOT NULL AND NOT EXISTS (SELECT 1 FROM BaoCao WHERE MaNguoiBaoCao = @ChiId AND LoaiDoiTuong = 'PRODUCT' AND MaDoiTuong = @Sp1)
BEGIN
    INSERT INTO BaoCao (LoaiDoiTuong, MaDoiTuong, MaNguoiBaoCao, LyDo, MoTa, TrangThai, NgayTao)
    VALUES ('PRODUCT', @Sp1, @ChiId, N'Hàng giả, hàng nhái thương hiệu', N'Sản phẩm ghi chính hãng nhưng hình ảnh cho thấy là hàng nhái kém chất lượng.', 'PENDING', DATEADD(DAY, -1, SYSUTCDATETIME()));
END

IF @DungId IS NOT NULL AND @AdminId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM BaoCao WHERE MaNguoiBaoCao = @DungId AND LoaiDoiTuong = 'USER' AND MaDoiTuong = 2)
BEGIN
    INSERT INTO BaoCao (LoaiDoiTuong, MaDoiTuong, MaNguoiBaoCao, LyDo, MoTa, TrangThai, NguoiXuLyId, GhiChuXuLy, NgayTao, NgayXuLy)
    VALUES ('USER', 2, @DungId, N'Hành vi gian lận', N'Người dùng nhắn tin yêu cầu chuyển cọc ngoài hệ thống.', 'RESOLVED', @AdminId, N'Đã gửi cảnh cáo nghiêm khắc đến người dùng.', DATEADD(DAY, -3, SYSUTCDATETIME()), DATEADD(DAY, -2, SYSUTCDATETIME()));
END
GO

-- ===== Nội dung giữ nguyên từ 33_seed_danh_gia.sql =====
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
    THROW 50033, N'Seed đánh giá: có mã đơn không tồn tại hoặc chưa COMPLETED. Hãy kiểm tra dữ liệu đơn hàng trong 13_seed_demo.sql.', 1;

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
GO
