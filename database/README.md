# Database – SecondHand Marketplace

## Yêu cầu

- SQL Server Developer hoặc Express.
- `sqlcmd` có trong PATH.
- Mặc định dùng `localhost` và Windows Authentication.

## Tạo lại database

Chạy `run-db.bat` trên database mới để tạo database `SecondHandMarketplace`. `DB_MODE=dev` hoặc `test` nạp dữ liệu demo; `DB_MODE=deploy` chỉ nạp schema và seed hệ thống. Nếu dùng SQL Server Express, đặt biến môi trường `SQL_SERVER=.\SQLEXPRESS`. Nếu dùng SQL Authentication, đặt `SQL_AUTH` theo hướng dẫn trong `run-db.bat`.

`reset-db.bat` sẽ xóa database và toàn bộ dữ liệu hiện có trước khi chạy lại các script. Phải đặt `DB_MODE=dev` hoặc `DB_MODE=test`; script từ chối khi chưa đặt chế độ hoặc khi là `deploy`. Chỉ dùng khi thực sự muốn tạo mới database dev/test.

## Các script

- `01`–`09`: schema và dữ liệu lõi của project.
- `10_banner_yeuthich.sql`: phần Banner và Yêu thích (người B phụ trách).
- `11_danhgia.sql`: phần Đánh giá (người C phụ trách).
- `12_tranhchap_baocao.sql`: phần Tranh chấp và Báo cáo (người D phụ trách).
- `13_daugia.sql`: phần Đấu giá (người E phụ trách).
- `14_donhang_admin.sql`: thủ tục Admin can thiệp đơn hàng `sp_AdminCapNhatTrangThaiDon` (người E phụ trách).
- `22_checkout_delivery_window.sql`: bổ sung khung giờ nhận hàng cho đơn; chạy khi nâng cấp database đã có dữ liệu.

Bốn file `10`–`13` được tạo sẵn chỉ có `USE SecondHandMarketplace; GO`; file `14` do người E bổ sung thêm. Thành viên phụ trách bổ sung SQL vào đúng file của mình, không sửa `02_create_tables.sql` hoặc `09_seed_data.sql`.

## Thứ tự thực thi

`run-db.bat` chỉ khởi tạo database mới và chọn seed bằng `DB_MODE=dev`, `test` hoặc `deploy` (mặc định `dev`). Cả ba chế độ chạy schema và seed hệ thống; `dev` và `test` chạy thêm seed demo `29` đến `33`, còn `deploy` không tạo tài khoản demo. Script từ chối chạy nếu database đã tồn tại vì `02_create_tables.sql` tạo lại bảng. `29_seed_demo_goc.sql` chỉ được chạy một lần trên database dev/test trống; reset trước khi chạy lại. Script kiểm tra điều kiện dữ liệu là `tests/seed_consistency.sql`.

Khởi tạo Production theo hướng dẫn trong `docs/deployment-runbook.md`; tài khoản admin production dùng `24_seed_admin_deploy.template.sql` với BCrypt hash cung cấp ngoài repo.

`07_functions.sql` phải chạy trước `06_procedures.sql` vì stored procedure sử dụng các hàm đã định nghĩa. Các thay đổi schema chạy trước thủ tục tương ứng.

`23_checkout_delivery_window.sql` thêm cột khung giờ nhận theo kiểu migration và được chạy sau khi tạo các bảng, trước khi tạo thủ tục đặt hàng.

## Tài khoản dữ liệu mẫu

- Admin: `admin@secondhand.local`
- USER mẫu: `an.seller@demo.local`, `binh.seller@demo.local`, `chi.buyer@demo.local`, `dung.buyer@demo.local`
- Mật khẩu dùng chung cho các tài khoản seed ở môi trường local: `Admin@12345`

Đổi mật khẩu ngay nếu dùng dữ liệu này ngoài môi trường phát triển. Mật khẩu được lưu dưới dạng BCrypt hash, không lưu dạng rõ trong database.
