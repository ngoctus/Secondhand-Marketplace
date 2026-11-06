# Database – SecondHand Marketplace

## Yêu cầu

- SQL Server Developer hoặc Express.
- `sqlcmd` có trong PATH.
- Mặc định dùng `localhost` và Windows Authentication.

## Tạo lại database

Chạy `run-db.bat` để tạo database `SecondHandMarketplace` và nạp dữ liệu mẫu. Nếu dùng SQL Server Express, đặt biến môi trường `SQL_SERVER=.\SQLEXPRESS`. Nếu dùng SQL Authentication, đặt `SQL_AUTH` theo hướng dẫn trong `run-db.bat`.

`reset-db.bat` sẽ xóa database và toàn bộ dữ liệu hiện có trước khi chạy lại các script. Chỉ dùng khi thực sự muốn tạo mới database.

## Các script

- `01`–`09`: schema và dữ liệu lõi của project.
- `10_banner_yeuthich.sql`: phần Banner và Yêu thích (người B phụ trách).
- `11_danhgia.sql`: phần Đánh giá (người C phụ trách).
- `12_tranhchap_baocao.sql`: phần Tranh chấp và Báo cáo (người D phụ trách).
- `13_daugia.sql`: phần Đấu giá (người E phụ trách).
- `14_donhang_admin.sql`: thủ tục Admin can thiệp đơn hàng `sp_AdminCapNhatTrangThaiDon` (người E phụ trách).
- `15_kiemduyet_tukhoa.sql`: mở rộng kiểm duyệt từ khóa và lịch sử vi phạm.
- `16_luot_kiem_duyet_tu_choi_he_thong.sql`: cho phép hệ thống tự từ chối sản phẩm có từ khóa HIGH.
- `17_bo_chan_tu_cam_khi_gui_duyet.sql`: cập nhật rule để sản phẩm được phân tích từ khóa sau khi gửi duyệt.
- `18_ghi_vi_pham_low_khi_tu_choi.sql`: ghi nhận vi phạm LOW khi Admin từ chối.
- `20_cauhinh_ai.sql`: bảng cấu hình AI do Admin quản lý.

Các file mở rộng được thiết kế chạy lặp an toàn; không sửa `02_create_tables.sql` hoặc `09_seed_data.sql` khi bổ sung tính năng.

## Thứ tự thực thi

`run-db.bat` chạy theo thứ tự phụ thuộc: database, tables, constraints, indexes, functions, views, procedures, triggers, seed data, rồi các file mở rộng. `07_functions.sql` phải chạy trước `06_procedures.sql`, còn các file `15`–`18` phải chạy sau các schema kiểm duyệt tương ứng.

## Tài khoản dữ liệu mẫu

- Admin: `admin@secondhand.local`
- USER mẫu: `an.seller@demo.local`, `binh.seller@demo.local`, `chi.buyer@demo.local`, `dung.buyer@demo.local`
- Mật khẩu dùng chung cho các tài khoản seed ở môi trường local: `Admin@12345`

Đổi mật khẩu ngay nếu dùng dữ liệu này ngoài môi trường phát triển. Mật khẩu được lưu dưới dạng BCrypt hash, không lưu dạng rõ trong database.
