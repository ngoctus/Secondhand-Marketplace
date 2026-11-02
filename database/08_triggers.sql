SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

USE SecondHandMarketplace;
GO

-- Làm sạch các trigger không dùng đến hoặc drop an toàn
DROP TRIGGER IF EXISTS trg_NguoiDung_CapNhat;
DROP TRIGGER IF EXISTS trg_CuaHang_KiemTra;
DROP TRIGGER IF EXISTS trg_SanPham_ChuanHoa;
DROP TRIGGER IF EXISTS trg_SanPham_ChuyenTrangThai;
DROP TRIGGER IF EXISTS trg_SanPham_ChanXoaMem;
DROP TRIGGER IF EXISTS trg_GioHang_KiemTra;
DROP TRIGGER IF EXISTS trg_DonHang_KiemTraTao;
DROP TRIGGER IF EXISTS trg_DonHang_KiemTraCapNhat;
DROP TRIGGER IF EXISTS trg_AnhSanPham_GioiHan;
DROP TRIGGER IF EXISTS trg_ChiTietDonHang_KiemTra;
DROP TRIGGER IF EXISTS trg_LuotKiemDuyet_KiemTra;
GO

-- AnhSanPham: tối đa 10 ảnh / sản phẩm
CREATE OR ALTER TRIGGER trg_AnhSanPham_GioiHan ON AnhSanPham AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1
               FROM AnhSanPham a
               WHERE a.MaSanPham IN (SELECT MaSanPham FROM inserted)
               GROUP BY a.MaSanPham
               HAVING COUNT(*) > 10)
        THROW 50090, N'Mỗi sản phẩm chỉ được tối đa 10 ảnh.', 1;
END
GO

-- ChiTietDonHang: snapshot bất biến (chỉ DangGiuCho được đổi), không xóa,
-- sản phẩm phải thuộc đúng shop của đơn (không có đơn "lai")
CREATE OR ALTER TRIGGER trg_ChiTietDonHang_KiemTra ON ChiTietDonHang AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @CoMoi bit = CASE WHEN EXISTS (SELECT 1 FROM inserted) THEN 1 ELSE 0 END,
            @CoCu  bit = CASE WHEN EXISTS (SELECT 1 FROM deleted)  THEN 1 ELSE 0 END;

    IF @CoMoi = 0 AND @CoCu = 1
        THROW 50072, N'Không được xóa chi tiết đơn hàng.', 1;

    IF @CoMoi = 1 AND @CoCu = 1
    BEGIN
        -- SỬA LỖI: Thay thế toán tử sai cú pháp || bằng OR chuẩn T-SQL
        IF EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON d.MaChiTietDonHang = i.MaChiTietDonHang
                   WHERE i.MaSanPham <> d.MaSanPham 
                      OR i.MaDonHang <> d.MaDonHang 
                      OR i.Gia <> d.Gia 
                      OR i.TenSanPham <> d.TenSanPham
                      OR ISNULL(i.AnhDaiDien, N'') <> ISNULL(d.AnhDaiDien, N''))
            THROW 50073, N'Chi tiết đơn hàng là dữ liệu chốt tại thời điểm đặt, không được sửa.', 1;
    END
    ELSE IF @CoMoi = 1 AND @CoCu = 0
    BEGIN
        IF EXISTS (SELECT 1 FROM inserted i
                   JOIN DonHang dh ON dh.MaDonHang = i.MaDonHang
                   JOIN SanPham sp ON sp.MaSanPham = i.MaSanPham
                   WHERE sp.MaCuaHang <> dh.MaCuaHang)
            THROW 50074, N'Sản phẩm không thuộc cửa hàng của đơn hàng này.', 1;
    END
END
GO

-- LuotKiemDuyet: chỉ ADMIN quyết định; đã quyết định thì không đổi được
CREATE OR ALTER TRIGGER trg_LuotKiemDuyet_KiemTra ON LuotKiemDuyet AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM inserted i JOIN NguoiDung nd ON nd.MaNguoiDung = i.MaQuanTriVien
               WHERE nd.VaiTro <> 'ADMIN')
        THROW 50080, N'Chỉ quản trị viên (ADMIN) mới được quyết định kiểm duyệt.', 1;

    IF EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON d.MaLuotKiemDuyet = i.MaLuotKiemDuyet
               WHERE d.QuyetDinh IS NOT NULL AND (i.QuyetDinh IS NULL OR i.QuyetDinh <> d.QuyetDinh))
        THROW 50081, N'Lượt kiểm duyệt đã có quyết định, không được thay đổi.', 1;
END
GO