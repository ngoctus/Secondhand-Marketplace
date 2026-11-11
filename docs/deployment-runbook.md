# Quy trình triển khai Re Market

## Chuẩn bị môi trường

1. Tạo database production riêng và một tài khoản SQL chỉ có quyền cần thiết cho ứng dụng. Không dùng tài khoản `sa`.
   SQL Server cần chứng chỉ TLS hợp lệ vì ứng dụng yêu cầu mã hóa kết nối và kiểm tra chứng chỉ.
2. Sao chép `.env.example` thành `.env` trên máy chủ, đặt thông tin kết nối, `JWT_KEY` ngẫu nhiên dài tối thiểu 32 byte và `AI_API_KEY` nếu bật AI. Không đưa `.env` vào Git.
3. Dùng reverse proxy có HTTPS trên cùng máy chủ trước cổng ứng dụng `8080`. Ứng dụng chỉ tin `X-Forwarded-For` và `X-Forwarded-Proto` từ loopback, còn Compose chỉ bind loopback để không công khai HTTP trực tiếp.
4. Tạo bản sao lưu database và hai volume `product_uploads`, `dispute_uploads` trước mỗi lần cập nhật.

## Khởi tạo database

Chạy `database/run-db.bat` với `DB_MODE=deploy` trên database production mới. Chế độ này nạp schema và seed hệ thống, không nạp người dùng hay dữ liệu demo. Script sẽ từ chối nếu database đã tồn tại. Tạo admin đầu tiên bằng `database/24_seed_admin_deploy.template.sql`, truyền BCrypt hash được tạo và lưu ngoài repo qua sqlcmd variable `AdminPasswordHash`.

## Cập nhật phiên bản

1. Sao lưu database và volume ảnh.
2. Xem các file SQL thay đổi, chạy migration tương ứng theo thứ tự phụ thuộc trên database production.
3. Build và thay container; giữ nguyên hai volume upload.
4. Kiểm tra đăng nhập, HTTPS, truy cập ảnh, thao tác đơn hàng và log. Nếu có lỗi, khôi phục bản sao lưu database và volume trước đó.

Không chạy `reset-db.bat` trên production. Không chạy seed demo `29_seed_demo_goc.sql` ở production. Nếu triển khai lên cloud/VPS, cần thiết lập secrets và reverse proxy trong chính môi trường đó trước khi bật dịch vụ.
