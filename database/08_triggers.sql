SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

USE SecondHandMarketplace;
GO

-- NguoiDung: tự cập nhật NgayCapNhat nếu app không tự đặt
CREATE OR ALTER TRIGGER trg_NguoiDung_CapNhat ON NguoiDung AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE nd SET NgayCapNhat = SYSUTCDATETIME()
    FROM NguoiDung nd
    JOIN inserted i ON i.MaNguoiDung = nd.MaNguoiDung
    JOIN deleted  d ON d.MaNguoiDung = i.MaNguoiDung
    WHERE d.NgayCapNhat = i.NgayCapNhat;
END
GO

-- CuaHang: Admin không được mở shop (Admin không mua/bán)
CREATE OR ALTER TRIGGER trg_CuaHang_KiemTra ON CuaHang AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM inserted i JOIN NguoiDung nd ON nd.MaNguoiDung = i.MaNguoiDung
               WHERE nd.VaiTro <> 'USER' OR nd.TrangThai <> 'ACTIVE')
        THROW 50100, N'Chỉ tài khoản USER đang hoạt động mới được tạo shop.', 1;
END
GO

-- SanPham (1/3): tự chuẩn hóa ChuoiTimKiem + NgayCapNhat
CREATE OR ALTER TRIGGER trg_SanPham_ChuanHoa ON SanPham AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE sp
    SET ChuoiTimKiem = fn_ChuanHoaChuoi(sp.Ten),
        NgayCapNhat  = CASE WHEN d.MaSanPham IS NOT NULL AND d.NgayCapNhat = i.NgayCapNhat
                            THEN SYSUTCDATETIME() ELSE sp.NgayCapNhat END
    FROM SanPham sp
    JOIN inserted i  ON i.MaSanPham = sp.MaSanPham
    LEFT JOIN deleted d ON d.MaSanPham = i.MaSanPham
    WHERE sp.ChuoiTimKiem <> fn_ChuanHoaChuoi(sp.Ten)
       OR (d.MaSanPham IS NOT NULL AND d.NgayCapNhat = i.NgayCapNhat);
END
GO

-- SanPham (2/3): chuyển trạng thái hợp lệ + khóa sửa nội dung khi PENDING_REVIEW/RESERVED/SOLD
CREATE OR ALTER TRIGGER trg_SanPham_ChuyenTrangThai ON SanPham AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Msg nvarchar(400);

    IF UPDATE(TrangThai)
    BEGIN
        SELECT TOP (1) @Msg = CONCAT(N'Không thể chuyển sản phẩm từ ', d.TrangThai, N' sang ', i.TrangThai, N'.')
        FROM inserted i JOIN deleted d ON d.MaSanPham = i.MaSanPham
        WHERE i.TrangThai <> d.TrangThai
          AND fn_ChuyenTrangThaiSanPhamHopLe(d.TrangThai, i.TrangThai) = 0;

        IF @Msg IS NOT NULL THROW 50011, @Msg, 1;
    END

    IF EXISTS (SELECT 1
               FROM inserted i JOIN deleted d ON d.MaSanPham = i.MaSanPham
               WHERE i.TrangThai = d.TrangThai
                 AND d.TrangThai IN ('PENDING_REVIEW','RESERVED','SOLD')
                 AND (   i.Ten <> d.Ten
                      OR ISNULL(i.MoTa, N'')    <> ISNULL(d.MoTa, N'')
                      OR ISNULL(i.Gia, -1)      <> ISNULL(d.Gia, -1)
                      OR ISNULL(i.MaDanhMuc, -1)   <> ISNULL(d.MaDanhMuc, -1)
                      OR ISNULL(i.MaTinhTrang, -1) <> ISNULL(d.MaTinhTrang, -1)))
        THROW 50017, N'Không được sửa nội dung sản phẩm khi đang chờ duyệt, đang giữ chỗ hoặc đã bán.', 1;
END
GO

-- SanPham (3/3): chặn xóa mềm khi đã SOLD hoặc đã có đơn (kể cả đơn đã hủy)
CREATE OR ALTER TRIGGER trg_SanPham_ChanXoaMem ON SanPham AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT UPDATE(DaXoa) RETURN;

    IF EXISTS (SELECT 1
               FROM inserted i JOIN deleted d ON d.MaSanPham = i.MaSanPham
               WHERE d.DaXoa = 0 AND i.DaXoa = 1
                 AND (i.TrangThai = 'SOLD'
                      OR EXISTS (SELECT 1 FROM ChiTietDonHang ct WHERE ct.MaSanPham = i.MaSanPham)))
        THROW 50016, N'Sản phẩm đã có đơn hàng hoặc đã bán, không thể xóa.', 1;
END
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

-- GioHang: chỉ USER hoạt động; không thêm sản phẩm của mình; chỉ thêm sản phẩm đang bán
CREATE OR ALTER TRIGGER trg_GioHang_KiemTra ON GioHang AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM inserted i JOIN NguoiDung nd ON nd.MaNguoiDung = i.MaNguoiDung
               WHERE nd.VaiTro <> 'USER' OR nd.TrangThai <> 'ACTIVE')
        THROW 50030, N'Chỉ tài khoản USER đang hoạt động mới có giỏ hàng.', 1;

    IF EXISTS (SELECT 1 FROM inserted i
               JOIN SanPham sp ON sp.MaSanPham = i.MaSanPham
               JOIN CuaHang ch ON ch.MaCuaHang = sp.MaCuaHang
               WHERE ch.MaNguoiDung = i.MaNguoiDung)
        THROW 50031, N'Không thể thêm sản phẩm của chính mình vào giỏ hàng.', 1;

    IF EXISTS (SELECT 1 FROM inserted i
               JOIN SanPham sp ON sp.MaSanPham = i.MaSanPham
               JOIN CuaHang ch ON ch.MaCuaHang = sp.MaCuaHang
               WHERE sp.TrangThai <> 'ACTIVE' OR sp.DaXoa = 1 OR ch.TrangThai <> 'ACTIVE')
        THROW 50032, N'Sản phẩm không còn bán.', 1;
END
GO

-- DonHang (INSERT): người mua là USER hoạt động, không mua shop của mình, shop không bị khóa
CREATE OR ALTER TRIGGER trg_DonHang_KiemTraTao ON DonHang AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM inserted i JOIN NguoiDung nd ON nd.MaNguoiDung = i.MaNguoiDung
               WHERE nd.VaiTro <> 'USER' OR nd.TrangThai <> 'ACTIVE')
        THROW 50070, N'Chỉ tài khoản USER đang hoạt động mới được đặt hàng.', 1;

    IF EXISTS (SELECT 1 FROM inserted i JOIN CuaHang ch ON ch.MaCuaHang = i.MaCuaHang
               WHERE ch.MaNguoiDung = i.MaNguoiDung)
        THROW 50070, N'Không thể mua hàng của chính shop mình.', 1;

    IF EXISTS (SELECT 1 FROM inserted i JOIN CuaHang ch ON ch.MaCuaHang = i.MaCuaHang
               WHERE ch.TrangThai <> 'ACTIVE')
        THROW 50070, N'Cửa hàng đang bị khóa, không thể tạo đơn mới.', 1;
END
GO

-- DonHang (UPDATE): chuyển trạng thái hợp lệ + không sửa thông tin cốt lõi
CREATE OR ALTER TRIGGER trg_DonHang_KiemTraCapNhat ON DonHang AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @Msg nvarchar(400);

    IF UPDATE(TrangThai)
    BEGIN
        SELECT TOP (1) @Msg = CONCAT(N'Không thể chuyển đơn hàng từ ', d.TrangThai, N' sang ', i.TrangThai, N'.')
        FROM inserted i JOIN deleted d ON d.MaDonHang = i.MaDonHang
        WHERE i.TrangThai <> d.TrangThai
          AND fn_ChuyenTrangThaiDonHangHopLe(d.TrangThai, i.TrangThai) = 0;

        IF @Msg IS NOT NULL THROW 50052, @Msg, 1;
    END

    IF EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON d.MaDonHang = i.MaDonHang
               WHERE i.MaNguoiDung <> d.MaNguoiDung OR i.MaCuaHang <> d.MaCuaHang
                  OR i.TongTien <> d.TongTien         OR i.MaDon <> d.MaDon)
        THROW 50071, N'Không được sửa người mua, cửa hàng, tổng tiền hoặc mã của đơn hàng.', 1;
END
GO

-- ChiTietDonHang: snapshot bất biến (chỉ DangGiuCho được đổi), không xóa, sản phẩm phải thuộc đúng shop của đơn (không có đơn "lai")
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
        IF EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON d.MaChiTietDonHang = i.MaChiTietDonHang
                   WHERE i.MaSanPham <> d.MaSanPham OR i.MaDonHang <> d.MaDonHang
                      OR i.Gia <> d.Gia OR i.TenSanPham <> d.TenSanPham
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