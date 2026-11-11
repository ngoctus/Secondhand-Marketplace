# A8 – Bảng theo dõi chạy lại chức năng cũ

Cách chấm: 0 = hỏng hoàn toàn … 4 = đúng đầy đủ. Chạy trên database tạo mới (`run-db.bat`, `SEED_DEMO=1`).

> [!IMPORTANT]
> Máy dev của trợ lý không có SQL Server nên **chưa chạy thủ công được** các mục dưới đây. Cột Điểm/Ngày để trống, người A điền khi kiểm thử thật. Phần test tự động (165 test, gồm service giỏ hàng, báo cáo, tranh chấp, thông báo…) đã pass.

| # | Chức năng | Ca cần thử | Điểm 0–4 | Ngày | Ghi chú / người chịu trách nhiệm |
|---|---|---|---|---|---|
| 1 | Đăng ký | email trùng, mật khẩu yếu | | | |
| 2 | Đăng nhập | sai mật khẩu, tài khoản bị khóa | | | |
| 3 | Admin khóa/mở khóa user | bắt buộc lý do, không khóa được ADMIN | | | |
| 4 | Danh mục, tình trạng | CRUD; mục đang dùng thì ẩn, không xóa | | | |
| 5 | Nhật ký | lọc theo user, hành động, ngày; phân trang | | | |
| 6 | Giỏ hàng | thêm, xóa, sửa số lượng | | | |
| 7 | Yêu thích, banner, thông báo | thêm/xóa/hiển thị đúng | | | |
| 8 | Phân quyền | USER không vào được trang Admin | | | |

Lỗi menu admin thuộc E1, không sửa ở đây.
