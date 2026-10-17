-- =====================================================================
-- 03a: Ràng buộc nhóm Tài khoản & Cửa hàng (UNIQUE, CHECK, FOREIGN KEY)
-- Chạy sau 02a_tai_khoan_shop_tables.sql
--
-- Quy ước đọc file:
--   -- [Đề xuất]      = chi tiết tài liệu chưa nêu, do mình thêm, nhóm xem lại
-- =====================================================================
USE SecondHandDB;   -- [Đề xuất] đổi theo tên database nhóm đã chốt
GO

-- ---------------------------------------------------------------------
-- NguoiDung
-- ---------------------------------------------------------------------
-- Email và SĐT duy nhất
ALTER TABLE NguoiDung ADD CONSTRAINT UQ_NguoiDung_Email UNIQUE (Email);
ALTER TABLE NguoiDung ADD CONSTRAINT UQ_NguoiDung_SDT   UNIQUE (SDT);
GO

-- SĐT đúng 10 chữ số
ALTER TABLE NguoiDung ADD CONSTRAINT CK_NguoiDung_SDT
    CHECK (LEN(SDT) = 10 AND SDT NOT LIKE '%[^0-9]%');

-- Vai trò chỉ USER hoặc ADMIN
ALTER TABLE NguoiDung ADD CONSTRAINT CK_NguoiDung_VaiTro
    CHECK (VaiTro IN ('USER', 'ADMIN'));

-- Trạng thái chỉ ACTIVE hoặc LOCKED
ALTER TABLE NguoiDung ADD CONSTRAINT CK_NguoiDung_TrangThai
    CHECK (TrangThai IN ('ACTIVE', 'LOCKED'));

-- Khóa phải có lý do và thời gian khóa
ALTER TABLE NguoiDung ADD CONSTRAINT CK_NguoiDung_KhoaCoLyDo
    CHECK (TrangThai <> 'LOCKED' OR (LyDoKhoa IS NOT NULL AND ThoiGianKhoa IS NOT NULL));

-- Email luôn chữ thường
-- [Đề xuất] cách kiểm: so sánh theo collation BIN2 (collation mặc định không phân biệt hoa/thường)
ALTER TABLE NguoiDung ADD CONSTRAINT CK_NguoiDung_EmailChuThuong
    CHECK (Email COLLATE Latin1_General_BIN2 = LOWER(Email) COLLATE Latin1_General_BIN2);

-- Email đúng dạng cơ bản
-- [Đề xuất] mẫu cụ thể: có @, có dấu chấm sau @, không chứa khoảng trắng
ALTER TABLE NguoiDung ADD CONSTRAINT CK_NguoiDung_EmailDang
    CHECK (Email LIKE '_%@_%._%' AND Email NOT LIKE '% %');

-- Họ tên không rỗng
ALTER TABLE NguoiDung ADD CONSTRAINT CK_NguoiDung_HoTen
    CHECK (LEN(LTRIM(RTRIM(HoTen))) > 0);

-- Admin không bị khóa
ALTER TABLE NguoiDung ADD CONSTRAINT CK_NguoiDung_AdminKhongKhoa
    CHECK (NOT (VaiTro = 'ADMIN' AND TrangThai = 'LOCKED'));
GO

-- ---------------------------------------------------------------------
-- CuaHang
-- ---------------------------------------------------------------------
-- Khóa ngoại tới NguoiDung
ALTER TABLE CuaHang ADD CONSTRAINT FK_CuaHang_NguoiDung
    FOREIGN KEY (NguoiDungId) REFERENCES NguoiDung (Id);

-- Mỗi người dùng một shop
ALTER TABLE CuaHang ADD CONSTRAINT UQ_CuaHang_NguoiDung UNIQUE (NguoiDungId);
GO

-- Tên shop >= 2 ký tự
ALTER TABLE CuaHang ADD CONSTRAINT CK_CuaHang_Ten
    CHECK (LEN(LTRIM(RTRIM(Ten))) >= 2);

-- SĐT liên hệ đúng 10 chữ số
ALTER TABLE CuaHang ADD CONSTRAINT CK_CuaHang_SDTLienHe
    CHECK (LEN(SDTLienHe) = 10 AND SDTLienHe NOT LIKE '%[^0-9]%');

-- Trạng thái chỉ ACTIVE hoặc LOCKED
ALTER TABLE CuaHang ADD CONSTRAINT CK_CuaHang_TrangThai
    CHECK (TrangThai IN ('ACTIVE', 'LOCKED'));

-- Khóa phải có lý do và thời gian khóa
ALTER TABLE CuaHang ADD CONSTRAINT CK_CuaHang_KhoaCoLyDo
    CHECK (TrangThai <> 'LOCKED' OR (LyDoKhoa IS NOT NULL AND ThoiGianKhoa IS NOT NULL));
GO