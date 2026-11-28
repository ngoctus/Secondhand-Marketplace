# Quy trình triển khai Re Market

## Chuẩn bị môi trường

1. Tạo database production riêng, một tài khoản riêng để chạy migration, và một tài khoản SQL runtime chỉ có quyền đọc/ghi/chạy procedure mà ứng dụng cần. Không dùng tài khoản `sa` cho ứng dụng. Bootstrap qua `run-db.bat` mã hóa kết nối và xác thực chứng chỉ theo mặc định ở `DB_MODE=deploy` (`DB_TRUST_CERT=False`).
   SQL Server cần chứng chỉ TLS hợp lệ vì ứng dụng yêu cầu mã hóa kết nối và kiểm tra chứng chỉ.
2. Sao chép `.env.example` thành `.env` trên máy chủ, đặt thông tin kết nối SQL với chứng chỉ TLS hợp lệ, `ALLOWED_HOSTS`, `SITE_PUBLIC_BASE_URL` bằng URL HTTPS gốc, proxy IP tin cậy, `JWT_KEY` ngẫu nhiên dài tối thiểu 32 byte và thông tin SMTP. Đặt `DB_TRUST_CERT=True` chỉ cho môi trường local dùng chứng chỉ tự ký. Không đưa `.env` vào Git.
   Nếu cấu hình `REDIS_CONNECTION_STRING`, Redis dùng chung lưu trạng thái OTP đăng ký, catalog cache, bộ đếm giới hạn tần suất, khóa Data Protection và distributed lease cho tác vụ nền; bật persistence/AOF ở Redis để giữ khóa qua lần khởi động. Khi để trống, cache dùng bộ nhớ theo tiến trình, giới hạn tần suất theo tiến trình, job nền chỉ an toàn trên một replica và Data Protection dùng volume file. Để chạy nhiều host, cấu hình Redis dùng chung cùng `STORAGE_PROVIDER=S3`, bucket S3-compatible dùng chung và CDN tùy chọn; `/health/ready` kiểm tra Redis và bucket khi đã cấu hình.
3. Dùng reverse proxy có HTTPS trên cùng máy chủ trước cổng ứng dụng `8080`. Compose chỉ bind cổng web vào loopback để không công khai HTTP trực tiếp. Cấu hình proxy được tin cậy trong `ForwardedHeadersOptions` theo địa chỉ thực tế của reverse proxy.
4. Tạo bản sao lưu database và các volume `product_uploads`, `dispute_uploads`, `appeal_uploads`, `data_protection_keys` trước mỗi lần cập nhật. Không xóa volume Data Protection khi thay container vì việc đó sẽ làm mất khả năng đọc cookie/token đã phát hành. Nếu dùng Redis, sao lưu/persist Redis theo cấu hình nhà cung cấp.
5. Với `STORAGE_PROVIDER=Local`, ảnh sản phẩm/banner nằm trong `Storage:RootPath`; ảnh tranh chấp/khiếu nại nằm riêng trong `Storage:PrivateRootPath` và chỉ tải qua endpoint có xác thực. Với S3, ứng dụng lưu ảnh public theo key `products/...` và `banners/...`, bằng chứng theo key `private/disputes/...` và `private/appeals/...`; bucket phải chặn public access/ACL và chỉ mở đọc public cho prefix ảnh sản phẩm/banner khi CDN hoặc bucket public được chọn. Giữ các volume upload trong lúc chuyển dữ liệu cũ; volume `data_protection_keys` vẫn cần nếu không dùng Redis.

## Chuyển sang object storage

1. Tạo bucket dùng chung và credentials ít quyền nhất: `s3:PutObject`, `s3:GetObject`, `s3:DeleteObject` trên object keys ứng dụng; quyền `s3:ListBucket` để health check HEAD bucket. Ưu tiên IAM/task role thay cho access key. Không cấp quyền đọc public cho prefix `private/`; không dùng ACL public cho bằng chứng.
2. Copy dữ liệu trước khi bật provider: nội dung `/uploads/products/...` và `/uploads/banners/...` vào cùng key (bỏ tiền tố `/uploads/`); `/uploads/disputes/...` và `/uploads/appeals/...` phải vào `private/disputes/...` và `private/appeals/...`. Bao gồm các file `-thumb.webp`. Khi dùng AWS CLI, loại trừ bằng chứng cũ khỏi lần đồng bộ public:

   ```sh
   aws s3 sync wwwroot/uploads s3://BUCKET --exclude 'disputes/*' --exclude 'appeals/*'
   aws s3 sync App_Data/private-uploads s3://BUCKET/private
   aws s3 sync wwwroot/uploads/disputes s3://BUCKET/private/disputes
   aws s3 sync wwwroot/uploads/appeals s3://BUCKET/private/appeals
   ```

   Điều chỉnh đường dẫn nguồn theo volume thật trên host. Kiểm tra số lượng và dung lượng sau khi copy; không bỏ bước loại trừ hai thư mục bằng chứng khỏi lệnh đồng bộ public.
3. Điền `STORAGE_PROVIDER=S3`, bucket, region và endpoint nếu dùng dịch vụ tương thích; cấu hình `S3_PUBLIC_BASE_URL` chỉ khi CDN/custom domain đã trỏ đúng vào public object keys. Để trống `S3_ENDPOINT` cho AWS S3; bật path-style cho các dịch vụ như MinIO nếu nhà cung cấp yêu cầu.
4. Khởi động phiên bản mới và xác nhận `/health/ready` healthy, ảnh public tải được qua CDN/proxy và bằng chứng riêng tư chỉ xem được bởi người có quyền. Giữ volume cũ đến khi đã đối chiếu hoàn tất; sau đó backup theo chính sách của nhà cung cấp object storage.

## Khởi tạo database

Chạy `database/run-db.bat` với `DB_MODE=deploy` trên database production mới. Chế độ này nạp schema và seed hệ thống, không nạp người dùng hay dữ liệu demo. Script sẽ từ chối nếu database đã tồn tại. Tạo admin đầu tiên bằng `database/templates/seed_admin_deploy.template.sql`, truyền BCrypt hash được tạo và lưu ngoài repo qua sqlcmd variable `AdminPasswordHash`.

## Cập nhật phiên bản

1. Sao lưu database, các volume local đang dùng và object storage nếu đã bật; giữ volume `data_protection_keys` khi Data Protection chưa dùng Redis.
2. Với database đã tồn tại, backup trước; đặt `SQL_SERVER`, `DB_NAME` và `SQL_AUTH` trỏ đúng database, rồi chạy migration theo thứ tự trong [`database/migrations/README.md`](../database/migrations/README.md) trước khi triển khai ứng dụng. Trong Command Prompt mở ở thư mục gốc repo:

   ```bat
   sqlcmd -S "%SQL_SERVER%" -d "%DB_NAME%" %SQL_AUTH% -N -b -f 65001 -i "database/migrations/202611_khieu_nai_tai_khoan.sql"
   sqlcmd -S "%SQL_SERVER%" -d "%DB_NAME%" %SQL_AUTH% -N -b -f 65001 -i "database/migrations/202611_danh_gia_phan_hoi_shop.sql"
   sqlcmd -S "%SQL_SERVER%" -d "%DB_NAME%" %SQL_AUTH% -N -b -f 65001 -i "database/migrations/202611_seed_tukhoacam_high_low.sql"
   sqlcmd -S "%SQL_SERVER%" -d "%DB_NAME%" %SQL_AUTH% -N -b -f 65001 -i "database/migrations/202611_payment_refund_due.sql"
   ```

   Lệnh mã hóa kết nối và kiểm tra chứng chỉ máy chủ. Chỉ thêm `-C` cho local/dev dùng chứng chỉ tự ký. Dừng nếu migration seed báo thiếu cột/hàm; không bỏ qua lỗi rồi triển khai app. Migration refund liệt kê các đơn cũ đã ghi `REFUNDED` để đối chiếu chứng từ và không đổi trạng thái các dòng lịch sử này.
3. Build và thay container; giữ nguyên volume upload cũ trong giai đoạn di chuyển và giữ volume Data Protection nếu chưa lưu khóa trên Redis.
4. Kiểm tra `/health/live`, `/health/ready`, đăng nhập, HTTPS, truy cập ảnh, thao tác đơn hàng và log. `/health/ready` phải trả trạng thái healthy trước khi nhận lưu lượng thật.

## Thanh toán và vận chuyển hiện tại

- Giai đoạn đầu dùng COD và chuyển khoản VietQR trực tiếp tới shop; shop xác nhận giao dịch thủ công. Hệ thống không giữ tiền và không tự xác minh chuyển khoản.
- Hủy đơn đã thanh toán đưa khoản tiền sang `REFUND_DUE`; shop chỉ ghi nhận `REFUNDED` sau khi thực sự trả tiền và có thể lưu mã giao dịch/ghi chú.
- Trạng thái giao hàng do shop cập nhật thủ công; hiện chưa gọi API hãng vận chuyển.
- Không nhập thông tin thẻ vào ứng dụng. Chỉ bật cổng thanh toán sau khi cấu hình tài khoản merchant, webhook HTTPS và kiểm tra chữ ký webhook; việc thuê nhà cung cấp không tự động loại bỏ toàn bộ trách nhiệm PCI của đơn vị vận hành.

Không chạy `reset-db.bat` trên production và không nạp seed demo. Nếu triển khai lên cloud/VPS, cần thiết lập secrets và reverse proxy trong chính môi trường đó trước khi bật dịch vụ.
