# C1 – Kiểm duyệt sản phẩm & quản lý vi phạm: bàn giao

> **Chưa build bằng `dotnet build` thật, chưa chạy SQL Server.** Xem mục 7 để biết cái gì đã/chưa kiểm chứng.

## 1. Phạm vi đã làm
- Hàng chờ duyệt (`Admin/Products`), tab "Đang bán (gỡ vi phạm)", chi tiết sản phẩm (`Admin/Products/Details/{id}`), duyệt, từ chối (bắt buộc lý do), gỡ sản phẩm vi phạm.
- Lịch sử vi phạm chỉ đọc (`Admin/Violations`, lọc theo `sellerId`).
- Hiển thị trong chi tiết: ảnh, thông tin sản phẩm/shop/người bán, số vi phạm/ngưỡng, kết quả phân tích C2 (trạng thái, mức, từ khóa + vị trí), **lịch sử `LuotKiemDuyet` (kể cả lượt C2 tự từ chối)** và **lịch sử `LichSuTrangThaiSanPham`**.
- Không làm: phân tích lại, quản lý từ khóa (C2), khóa/mở khóa (A), thông báo (C5 – chỉ gọi), báo cáo (D).

## 2. Quy tắc quyết định (theo xác nhận của bạn)
| Trạng thái phân tích của lượt đang chờ | Duyệt | Từ chối | Vi phạm |
|---|---|---|---|
| Không từ khóa (DONE/NONE) | được | được | không |
| LOW (DONE) | được → không vi phạm | được → **1 vi phạm** (trigger SQL `18_…`) | khi từ chối |
| QUEUED / FAILED / chưa có dòng phân tích / DONE nhưng `MucDoTuKhoa` NULL | **chặn** (cần `PhanTichSanPhamAsync`) | **được** (procedure cho phép) | không |
| HIGH | C2 đã tự từ chối, lượt đã đóng → C1 không xử lý lại | | C2 đã ghi 1 |

## 3. File
Mới: `Services/ProductReview/{IProductReviewService, ProductReviewService, ReviewDisplay}.cs`, `Models/ViewModels/Admin/ReviewReasonVm.cs`, `Areas/Admin/Controllers/ViolationsController.cs`, `Areas/Admin/Views/{Products/Details, Violations/Index}.cshtml`, `tests/…/ProductReviewServiceTests.cs`, `database/tests/c1_smoke_test.sql`.
Sửa: `Areas/Admin/Controllers/ProductsController.cs` và `Areas/Admin/Views/Products/Index.cshtml` (placeholder TODO của C1), `Common/Extensions/ServiceCollectionExtensions.cs` (+1 dòng DI, +1 using).
Không sửa: `_Layout.cshtml`, ProductsController/Views của B, `AppDbContext`, mã C2, stored procedure/trigger, schema. **Không có SQL mới.**

## 4. Transaction, đồng thời và `AfterAdminDecisionAsync`
- Quyết định là **một lời gọi procedure** (`sp_DuyetSanPham`/`sp_TuChoiSanPham` → `sp_QuyetDinhKiemDuyet`): transaction + `UPDLOCK` lượt nằm trong procedure; trigger C2 chạy trong cùng transaction (chặn duyệt 50210/50211, ghi vi phạm LOW khi từ chối). Lỗi bất kỳ ⇒ rollback cả quyết định lẫn vi phạm.
- C1 kiểm tra trước (không khóa) chỉ để báo lỗi dễ hiểu; **DB là chốt chặn cuối**. Hai Admin cùng quyết định một sản phẩm: người sau bị chờ rồi nhận 50022 → C1 báo "đã có người xử lý", **không** gọi `AfterAdminDecisionAsync`, không thông báo, không cộng vi phạm.
- C2 phân tích lại đua với quyết định: `ApplyAnalysisOnceAsync` của C2 khóa cùng dòng lượt ⇒ tuần tự; nếu lượt vừa bị tự từ chối thì Admin nhận 50022.
- **`AfterAdminDecisionAsync(maSanPham)` chỉ được gọi sau khi quyết định đã commit thành công**, một lần cho request thành công. Nó chỉ đọc lại vi phạm của lượt vừa quyết định và áp dụng ngưỡng 3 (idempotent: gọi lại không khóa/thông báo lần hai). Nếu nó ném lỗi, quyết định vẫn thành công và C1 trả `Warning` (không rollback quyết định).
- Thông báo `PRODUCT` cho người bán gửi sau quyết định (best-effort: lỗi thông báo không hủy quyết định).

## 5. Tích hợp
- **B (menu)** – thêm vào `_Layout.cshtml` (C1 không tự sửa):
  `<a class="navlink" asp-area="Admin" asp-controller="Products" asp-action="Index">Kiểm duyệt sản phẩm</a>`
  `<a class="navlink" asp-area="Admin" asp-controller="Violations" asp-action="Index">Lịch sử vi phạm</a>`
  (và link "Từ khóa cấm" của C2 nếu chưa có).
- **B (gửi duyệt)**: phải gọi `IModerationService.PhanTichSanPhamAsync(maSanPham)` ngay sau `sp_GuiDuyetSanPham`; nếu không, sản phẩm mãi ở QUEUED (chỉ từ chối được). Dữ liệu seed hiện có (Sony/Lego DONE kiểu cũ, Canon QUEUED) cũng cần quét lại mới duyệt được.
- **B (ảnh)**: chưa thấy code upload của B1. C1 hiển thị `AnhSanPham.DuongDan` nguyên giá trị vào `<img src>` (cùng quy ước các view `Views/Products` hiện có và seed `/uploads/seed/…`). Nếu B1 lưu kiểu khác (đường dẫn vật lý, tên file trần…), cần báo C1 đổi cách hiển thị.
- **D5 (gỡ sản phẩm)** – hàm để D5 gọi, không phải sửa C1:
  ```csharp
  var r = await _productReview.RemoveProductAsync(adminId, maSanPham, "Lý do (10–500 ký tự)", ct);
  // r.Success / r.Error ; chỉ gỡ được sản phẩm ACTIVE (sp_ChuyenTrangThaiSanPham ACTIVE→REMOVED, lý do bắt buộc);
  // đã ghi nhật ký REMOVE_PRODUCT + thông báo người bán; KHÔNG ghi vi phạm.
  ```
  D5 tự lo phần báo cáo (đổi trạng thái báo cáo, thông báo cho người báo cáo).
- **A**: không sửa gì. Trang Violations link sang `Admin/Users?search=<email>` để mở khóa.
- **C5**: dùng `INotificationService.CreateAsync` (loại `PRODUCT`/`SYSTEM`), không sửa.

## 6. Giả định / điểm cần biết
1. Admin từ chối LOW luôn tính 1 vi phạm, bất kể lý do (trigger không phân biệt).
2. Chỉ gỡ được sản phẩm `ACTIVE` vì `fn_ChuyenTrangThaiSanPhamHopLe` chỉ cho `ACTIVE → REMOVED` (HIDDEN/RESERVED không gỡ được; muốn khác phải sửa SQL, chưa làm).
3. Nếu chưa chạy `18_ghi_vi_pham_low_khi_tu_choi.sql` thì từ chối LOW **không** ghi vi phạm (C1 không ghi bù).
4. Quyết định thủ công cho lượt HIGH còn mở (trạng thái bất thường) bị C1 chặn.

## 7. Kết quả kiểm chứng (trung thực)
| Hạng mục | Kết quả |
|---|---|
| `dotnet build` / `dotnet test` thật | **Không chạy được** (sandbox chặn NuGet) |
| Harness giả lập EF Core/xunit (như C2) | Toàn bộ code C1 (service, 2 controller) + test **biên dịch được; 88/88 test pass** (59 của C2 + 29 của C1). Có kiểm đột biến: bỏ chốt chặn duyệt của C1 thì 4 test fail như mong đợi. Chỉ kiểm logic C# trên API giả lập |
| Test C1 | Procedure/trigger được **giả lập** (`EmulatedProcReviewService`), không phải SQL thật |
| Views Razor (`Products/Index, Details`, `Violations/Index`) | **Chưa biên dịch/chạy**, chỉ rà bằng mắt |
| `database/tests/c1_smoke_test.sql` (+ kịch bản 2 phiên) | **Chưa chạy** (không có SQL Server) |
| Truy vấn EF phức tạp (projection có `Anh.Where(...).FirstOrDefault()`, `GroupBy`) | Chưa chạy trên SQL Server thật; có thể cần chỉnh nếu EF báo lỗi dịch |

## 8. Cách kiểm thử
1. Chạy `15 → 18` rồi `c2_smoke_test.sql` và `c1_smoke_test.sql` (cần cả hai PASS).
2. `dotnet build && dotnet test`.
3. Tay: đăng nhập Admin → Kiểm duyệt sản phẩm → mở một sản phẩm đã được `PhanTichSanPhamAsync` quét: duyệt, từ chối (thử lý do ngắn), mở tab "Đang bán" và gỡ; mở Lịch sử vi phạm.
