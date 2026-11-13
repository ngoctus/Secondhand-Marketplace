# Database – Re:Market (bộ bootstrap SQL đã chuẩn hóa)

Bộ SQL được tổ chức lại theo nguyên tắc đã thống nhất:

> **Chỉ thay đổi cách tổ chức và thứ tự triển khai SQL. Không tự ý thay đổi nghiệp vụ, tên bảng, tên cột, tên procedure hoặc logic mà C# đang sử dụng.**

Các migration bổ sung trước đây đã được hợp nhất vào schema cuối cùng để cài đặt database mới bằng một thứ tự thống nhất. Các định nghĩa nghiệp vụ được giữ nguyên; riêng những đối tượng có nhiều phiên bản thì chỉ giữ phiên bản cuối đã có trong bộ nguồn.

## 0. Yêu cầu môi trường

- SQL Server Developer hoặc Express.
- `sqlcmd` có trong `PATH`.
- Mặc định dùng `localhost` và Windows Authentication.
- SQL Server Express: đặt `SQL_SERVER=.\SQLEXPRESS`; SQL Authentication có thể cấu hình qua `SQL_AUTH` như hướng dẫn trong `run-db.bat`.

## 1. Thứ tự chạy

| File | Nội dung |
|---|---|
| `01_create_database.sql` | Tạo database `SecondHandMarketplace` |
| `02_create_tables.sql` | Tạo đầy đủ 31 bảng với các cột ở trạng thái cuối |
| `03_constraints_indexes.sql` | PK, FK, UNIQUE, CHECK và toàn bộ index |
| `04_functions.sql` | 6 function |
| `05_views.sql` | Toàn bộ view, gồm các view đơn hàng/giỏ hàng phiên bản cuối |
| `06_triggers.sql` | 6 trigger |
| `07_account_shop_procedures.sql` | `sp_GhiNhatKy`, `sp_TaoCuaHang` |
| `08_product_procedures.sql` | Procedure nghiệp vụ sản phẩm |
| `09_order_procedures.sql` | Procedure giỏ hàng/đơn hàng và kiểu bảng `dbo.DanhSachId` |
| `10_moderation_admin_procedures.sql` | Procedure kiểm duyệt và quản trị |
| `11_extensions.sql` | Procedure đánh giá và đấu giá |
| `12_seed_core.sql` | Seed hệ thống, chạy cả DEV/TEST và DEPLOY |
| `13_seed_demo.sql` | Seed tài khoản/sản phẩm/địa chỉ shop/đơn hàng/tranh chấp/báo cáo/đánh giá demo, chỉ DEV/TEST |

Các đối tượng được phân tách theo loại để dễ tra cứu. Dependencies được giữ theo thứ tự: schema → constraints/indexes → functions → views → triggers → procedures → seed.

## 2. Danh sách 31 bảng

**Bảng lõi (17):**

`NguoiDung`, `CuaHang`, `DanhMuc`, `TinhTrangSanPham`, `SanPham`, `AnhSanPham`, `GioHang`, `DonHang`, `ChiTietDonHang`, `LichSuTrangThaiDonHang`, `ThanhToan`, `LichSuTrangThaiSanPham`, `LuotKiemDuyet`, `PhanTichAI`, `TuKhoaCam`, `ThongBao`, `NhatKyHeThong`.

**Bảng mở rộng (14):**

`YeuThich`, `Banner`, `DanhGia`, `TranhChap`, `BaoCao`, `PhienDauGia`, `LuotDatGia`, `LichSuViPham`, `HoiThoai`, `TinNhan`, `CauHinhAI`, `TinhThanh`, `PhuongXa`, `PasswordResetOtp`.

Các cột trước đây được thêm qua migration đã được đưa vào định nghĩa `CREATE TABLE`:
- `CuaHang`: `MaTinhThanh`, `MaPhuongXa`, `DiaChiChiTiet`, `KhuVucHienThi`, `NganHang`, `MaNganHang`, `SoTaiKhoan`, `TenChuTaiKhoan`.
- `DonHang`: `HinhThucNhan`, `KhungGioNhan`.
- `TuKhoaCam`: `MucDo`.
- `PhanTichAI`: `MucDoTuKhoa`.
- `LuotKiemDuyet`: `QuyetDinhTuDong`.

## 3. Các phiên bản đã được ưu tiên

- `vw_GioHangChiTiet`, `vw_DonHangTongQuan`, `vw_ChiTietDonHang`: giữ bản cuối từ module `22_hinh_thuc_nhan.sql`.
- `sp_TaoDonHangTheoShop`, `sp_TaoDonHang`, `sp_DatHangTuGio`, `sp_CapNhatTrangThaiDon`: giữ bản cuối từ module `22_hinh_thuc_nhan.sql`.
- `fn_KiemTraRuleSanPham`: giữ bản cuối từ `17_bo_chan_tu_cam_khi_gui_duyet.sql`.
- Giữ logic `TU_CAM` theo phiên bản cuối; không khôi phục bản function cũ.
- `QuyetDinhTuDong`, các cột mức độ từ khóa và các cột địa chỉ/nhận hàng được đưa vào schema cuối; constraint tương ứng nằm trong file `03`.

## 4. Chạy database

Mở Command Prompt tại thư mục `database`:

```bat
rem Development (có seed demo)
set DB_MODE=dev
run-db.bat

rem Test (có seed demo)
set DB_MODE=test
run-db.bat

rem Deploy (chỉ schema và seed core, không có dữ liệu demo)
set DB_MODE=deploy
run-db.bat
```

Có thể cấu hình `SQL_SERVER` và `SQL_AUTH` trước khi chạy. Ví dụ SQL Server Express:

```bat
set SQL_SERVER=.\SQLEXPRESS
set DB_MODE=dev
run-db.bat
```

`run-db.bat` kiểm tra database chưa tồn tại trước khi bootstrap. `02_create_tables.sql` có lệnh drop/recreate bảng, do đó **không chạy trực tiếp bộ bootstrap trên database đang có dữ liệu cần giữ**.

`reset-db.bat` chỉ cho phép `DB_MODE=dev` hoặc `DB_MODE=test`; thao tác này xóa toàn bộ database và dữ liệu.

## 5. Seed

- `12_seed_core.sql`: danh mục, tình trạng sản phẩm, từ khóa mặc định, dữ liệu địa giới hành chính, banner mặc định và cấu hình AI.
- `13_seed_demo.sql`: hợp nhất theo đúng thứ tự seed gốc → địa chỉ shop → đơn hàng → đánh giá. Chỉ được gọi khi `DB_MODE=dev` hoặc `test`.
- `templates/seed_admin_deploy.template.sql`: mẫu seed Admin riêng cho triển khai; không tự chạy trong bootstrap.

Dữ liệu địa giới hành chính được sinh từ `data/provinces_v2_depth2.json`. Chạy `tools/gen_dia_chi_insert.ps1` để cập nhật phần giữa hai marker `BEGIN GENERATED DATA` và `END GENERATED DATA` trong `12_seed_core.sql`.

## 6. Kiểm thử

Các file trong `tests/` được giữ nguyên:
- `c2_smoke_test.sql`
- `c6_smoke_test.sql`
- `c8_smoke_test.sql`
- `d_smoke_test.sql`
- `seed_consistency.sql`

Nên chạy smoke test sau khi bootstrap trên một SQL Server thực tế. Bộ file đã được rà soát tĩnh và sắp xếp dependency; môi trường hiện tại không có SQL Server để thực thi toàn bộ T-SQL, vì vậy không tuyên bố đã chạy integration test thành công.

## 7. Lưu ý về database đã tồn tại

Bộ 13 file này là **bộ cài mới (fresh bootstrap)**, không phải bộ migration nâng cấp database đang hoạt động. Nếu cần cập nhật database hiện hữu, phải tạo migration riêng, kiểm tra dữ liệu thực tế và backup trước khi áp dụng.

## 8. Tài khoản dữ liệu demo

- Admin local: `admin@secondhand.local`
- USER mẫu: `an.seller@demo.local`, `binh.seller@demo.local`, `chi.buyer@demo.local`, `dung.buyer@demo.local`
- Mật khẩu dùng chung cho tài khoản seed local: `Admin@12345`.

Chỉ dùng các tài khoản này ở DEV/TEST. Đổi thông tin xác thực nếu dữ liệu được dùng ngoài môi trường phát triển. Mật khẩu trong database được lưu dưới dạng BCrypt hash.

Mẫu tạo Admin production được giữ riêng tại `templates/seed_admin_deploy.template.sql`; không tự động chạy trong bootstrap.
