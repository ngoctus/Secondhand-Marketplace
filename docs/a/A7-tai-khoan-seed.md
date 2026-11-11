# A7 – Tài khoản và shop seed (dev/kiểm thử)

Chạy: đặt `SEED_DEMO=1` rồi chạy `database/run-db.bat` (file `30_seed_dia_chi_shop.sql`, sau file 29 của E). Mật khẩu chung: **`Admin@12345`**.

## Quản trị
`quantri01@demo.local`, `quantri02@demo.local`

## Người bán và shop
| Email | Shop | Khu vực | Ngân hàng | Ghi chú |
|---|---|---|---|---|
| nguoiban01 | Long Tech Cũ | P. Sài Gòn, TP.HCM | Vietcombank | |
| nguoiban02 | Hà Vintage Closet | P. Bến Thành, TP.HCM | MBBank | |
| nguoiban03 | Huy Gia Dụng | P. Thủ Đức, TP.HCM | VietinBank | |
| nguoiban04 | Mai Books | P. Gò Vấp, TP.HCM | BIDV | |
| nguoiban05 | Khôi Sport | P. Bình Thạnh, TP.HCM | **không có** | D test ẩn QR |
| nguoiban06 | Lan Mẹ và Bé | P. Tân Bình, TP.HCM | Techcombank | |
| nguoiban07 | Tuấn Nội Thất | P. Phú Nhuận, TP.HCM | ACB | |
| nguoiban08 | Tâm Camera Hà Nội | P. Cầu Giấy, Hà Nội | VPBank | cùng tỉnh với 09 |
| nguoiban09 | Châu Đồ Cổ | P. Đống Đa, Hà Nội | TPBank | cùng tỉnh với 08, khác xã |
| nguoiban10 | Hân Biển Xanh | P. Hải Châu, Đà Nẵng | Sacombank | |
| nguoiban11 | Kiệt Miền Tây | P. Ninh Kiều, Cần Thơ | VIB | |
| nguoiban12 | Thảo Shop Mới Mở | P. Biên Hòa, Đồng Nai | OCB | **chưa có sản phẩm**: B/C/D đừng seed sản phẩm cho shop này |

(Đầy đủ email: `nguoibanNN@demo.local`; SĐT `09020000NN`.)

## Người mua
`nguoimua01` … `nguoimua25` `@demo.local`, SĐT `09030000NN`.

## Shop mẫu cũ (từ `09_seed_data.sql`)
`an.seller` (P. Sài Gòn) và `binh.seller` (P. Tân Định) được bổ sung địa chỉ để có shop vừa có sản phẩm vừa có khu vực.
