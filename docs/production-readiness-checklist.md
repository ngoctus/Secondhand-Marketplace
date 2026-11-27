# Checklist trước khi mở Re:Market cho khách thật

## Đã chuẩn bị trong source

- `/health/live` kiểm tra tiến trình; `/health/ready` kiểm tra SQL Server, các bảng/cột/procedure quan trọng và trả 503 khi migration còn thiếu hoặc database chưa sẵn sàng.
- Cookie xác thực bắt buộc HTTPS ngoài Development; có header chống MIME sniffing, framing và hạn chế quyền trình duyệt.
- Docker chạy bằng user không đặc quyền, chỉ publish cổng vào loopback, lưu ảnh và khóa Data Protection qua volume.
- Ảnh bằng chứng tranh chấp/khiếu nại được lưu riêng tư và chỉ phục vụ cho người mua, người bán liên quan hoặc quản trị viên; cấu hình storage từ chối private root nằm trong public root.
- Compose yêu cầu cấu hình hostname, JWT key và SQL Server; TLS certificate được kiểm tra theo mặc định.
- GitHub Actions build project web khi push/PR; Dependabot theo dõi NuGet và GitHub Actions.
- OTP đăng ký, catalog cache, bộ đếm giới hạn tần suất, khóa Data Protection và khóa tác vụ nền có thể dùng Redis chia sẻ; tác vụ nền chỉ chạy trên replica giữ lease. `/health/ready` kiểm tra Redis khi `REDIS_CONNECTION_STRING` được đặt.
- Upload public và bằng chứng riêng tư đi qua tầng lưu trữ có thể dùng filesystem hoặc S3-compatible; readiness kiểm tra bucket khi provider S3 được chọn.
- Thanh toán COD/VietQR chuyển trực tiếp đến shop; hủy đơn đã trả tiền chuyển sang trạng thái chờ hoàn tiền và shop phải xác nhận sau khi trả tiền thủ công.

## Phải cấu hình ở môi trường triển khai

- Điền `.env` riêng trên máy chủ: `DB_SERVER`, tài khoản SQL ít quyền nhất, `JWT_KEY`, `ALLOWED_HOSTS`, `FORWARDED_HEADERS_KNOWN_PROXY` và SMTP. Không dùng giá trị mẫu.
- Cài chứng chỉ TLS SQL Server hợp lệ; chỉ đặt `DB_TRUST_CERT=True` trong local/dev nếu buộc dùng chứng chỉ tự ký.
- Bootstrap database mới bằng `DB_MODE=deploy`; database đã có dữ liệu chỉ được nâng cấp bằng migration đã backup và review.
- Cấu hình backup database, các volume local còn dùng, Redis và object storage theo provider. Thử restore định kỳ, không chỉ kiểm tra backup đã được tạo.
- Đặt reverse proxy HTTPS trước web; cấu hình đúng địa chỉ proxy tin cậy. Không mở cổng ứng dụng ra internet trực tiếp.
- Theo dõi `/health/ready`, log ứng dụng, lỗi email/SQL và cảnh báo dung lượng ổ đĩa.

## Giới hạn hiện tại cần giữ rõ

- Hệ thống chưa có cổng thanh toán online, hoàn tiền tự động hay API hãng vận chuyển; shop xác nhận chuyển khoản và cập nhật giao hàng thủ công.
- Object storage đã có adapter S3-compatible nhưng chưa được nối với bucket thật hoặc kiểm chứng tích hợp trong môi trường này. Trước khi chạy nhiều host, phải cấu hình Redis, S3 và CDN tùy chọn, di chuyển toàn bộ ảnh cũ, rồi xác minh quyền truy cập và khôi phục dữ liệu. Khi không có Redis, cache/giới hạn theo từng tiến trình và chỉ chạy một replica để tránh xử lý tác vụ nền trùng lặp.
- Checkout, chi tiết đơn, thẻ VietQR, tóm tắt đơn và trang cấu hình cửa hàng đã có nhãn/lỗi validation theo locale; các khu vực khác vẫn cần rà soát trước khi quảng bá hỗ trợ tiếng Anh.
- Không mở dịch vụ thật cho tới khi `/health/ready` healthy và đã thử đặt/hủy/hoàn tất đơn trên dữ liệu staging.
