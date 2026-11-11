# A2 – Nguồn dữ liệu tỉnh/thành, xã/phường

| Mục | Nội dung |
|---|---|
| Nguồn | Vietnamese Provinces Open API, endpoint `https://provinces.open-api.vn/api/v2/?depth=2` (dữ liệu cơ cấu hành chính 2 cấp có hiệu lực từ 01/07/2025) |
| Ngày tải | 11/11/2026 |
| Tệp gốc lưu trong repo | `database/data/provinces_v2_depth2.json` |
| Phạm vi | **Đầy đủ**: 34 tỉnh/thành và 3.321 xã/phường. Không có quận/huyện |
| Script chuyển đổi | `database/tools/gen_dia_chi_insert.ps1` (PowerShell, vì máy dev không có Python). Sinh câu `INSERT` với tiền tố `N'...'` vào cuối `database/21_dia_chi_khu_vuc.sql` |
| `MaHanhChinh` | Tỉnh 2 chữ số (vd `79`), xã/phường 5 chữ số (vd `26740`) |

> [!NOTE]
> Đây là bộ dữ liệu mã nguồn mở tổng hợp lại từ danh mục chính thức, không phải tệp tải trực tiếp từ cơ quan nhà nước. Trước khi nộp báo cáo nên đối chiếu số lượng (34 / 3.321) với Danh mục đơn vị hành chính của Cục Thống kê (danhmuchanhchinh.gso.gov.vn) và ghi rõ trong báo cáo.

## Kiểm tra sau khi chạy `run-db.bat`

```sql
SELECT COUNT(*) FROM dbo.TinhThanh;                                         -- 34
SELECT COUNT(*) FROM dbo.PhuongXa;                                          -- 3321
SELECT COUNT(*) FROM dbo.PhuongXa x LEFT JOIN dbo.TinhThanh t
       ON t.MaTinhThanh = x.MaTinhThanh WHERE t.MaTinhThanh IS NULL;        -- 0 (không xã mồ côi)
SELECT MaHanhChinh FROM dbo.TinhThanh GROUP BY MaHanhChinh HAVING COUNT(*) > 1;  -- rỗng
SELECT MaHanhChinh FROM dbo.PhuongXa  GROUP BY MaHanhChinh HAVING COUNT(*) > 1;  -- rỗng
```

Script sinh dữ liệu đã tự kiểm tra các điều kiện trên ở phía nguồn (đủ 34 tỉnh, không trùng mã, không mồ côi).

## Danh sách ngân hàng / BIN VietQR (A3)

Lấy từ `https://api.vietqr.io/v2/banks` (các dòng `transferSupported = 1`, bỏ ví MoMo/ZaloPay/MVAS), lưu `database/data/vietqr_banks.json`, cài đặt ở `Common/VietQrBanks.cs`. Đã đối chiếu tự động: 45/45 BIN và mã ngân hàng khớp.
