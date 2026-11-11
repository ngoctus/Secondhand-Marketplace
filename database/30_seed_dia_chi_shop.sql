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
    THROW 50010, N'Seed A7: không tạo đủ 12 cửa hàng, kiểm tra tên tỉnh/xã trong 21_dia_chi_khu_vuc.sql.', 1;

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
