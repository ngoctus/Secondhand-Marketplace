USE SecondHandMarketplace;
GO

-- Supply an out-of-repository BCrypt hash through sqlcmd: -v AdminPasswordHash="<bcrypt-hash>".
DECLARE @AdminPasswordHash varchar(100) = '$(AdminPasswordHash)';
IF LEN(@AdminPasswordHash) <> 60 OR LEFT(@AdminPasswordHash, 4) NOT IN ('$2a$', '$2b$', '$2y$')
    THROW 51092, N'AdminPasswordHash phải là BCrypt hash hợp lệ; không dùng mật khẩu dạng rõ.', 1;

IF NOT EXISTS (SELECT 1 FROM dbo.NguoiDung WHERE Email = 'admin@remarket.local')
    INSERT dbo.NguoiDung (HoTen, Email, MatKhauBam, VaiTro, TrangThai)
    VALUES (N'Quản trị viên', 'admin@remarket.local', @AdminPasswordHash, 'ADMIN', 'ACTIVE');
GO
