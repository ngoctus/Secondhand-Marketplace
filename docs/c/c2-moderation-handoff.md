# C2 – Moderation Service (HIGH/LOW): bàn giao (bản 2, nghiệp vụ LOW cuối cùng)

> **Chưa build bằng `dotnet build` thật, chưa chạy SQL Server, chưa chạy script nào lên database chung.** Xem mục 7.

## 1. Nghiệp vụ đã cài

| Tình huống | Kết quả |
|---|---|
| Không từ khóa | Phân tích DONE/NONE, chờ Admin như thường |
| Có ≥1 từ khóa HIGH (tên/mô tả) | C# trong 1 transaction: ghi **1** `LichSuViPham` (AUTO_HIGH), đóng lượt bằng quyết định **SYSTEM** (`QuyetDinhTuDong=1`, `MaQuanTriVien` NULL), sản phẩm PENDING_REVIEW→REJECTED (`LichSuTrangThaiSanPham.LoaiNguoiThucHien='SYSTEM'`). Phân tích lại/ request đồng thời không cộng trùng |
| Chỉ LOW | Lưu `PhanTichAI` (DONE, `MucDoTuKhoa='LOW'`), **chờ Admin ở C1**. Không tự từ chối, không vi phạm |
| LOW + Admin **duyệt** | Không vi phạm |
| LOW + Admin **từ chối** | Đúng **1** vi phạm (`ADMIN_TU_CHOI_LOW`, kèm `MaQuanTriVien`), ghi bởi trigger **trong cùng transaction** với quyết định từ chối |
| QUEUED / FAILED / chưa quét | Không duyệt được; từ chối không tính vi phạm; FAILED phân tích lại được trên cùng dòng `PhanTichAI` |
| Bộ đếm | `COUNT(*) LichSuViPham` theo người bán; ngưỡng `ModerationPolicy.ViolationLockThreshold = 3`; đạt → `IUserAdminService.LockUserAsync(null, …)`; không khóa ADMIN; mở khóa không đụng `LichSuViPham` |

Không còn `ConfirmLowViolationAsync`/`DismissLowViolationAsync`.

**Vì sao LOW dùng trigger DB thay vì C#:** quyết định từ chối của Admin là `UPDATE LuotKiemDuyet` trong procedure của C1. Ghi vi phạm trong trigger của chính `UPDATE` đó đảm bảo cùng commit/rollback, không cần sửa procedure/Controller của C1, và Admin bấm lặp/2 request đồng thời không cộng trùng (procedure chỉ cho UPDATE khi `QuyetDinh` còn NULL, trigger có `NOT EXISTS`, cộng `UNIQUE(MaLuotKiemDuyet)`). Phần **khóa tài khoản** không làm được trong SQL nên C1 gọi `AfterAdminDecisionAsync` (mục 4).

## 2. Thay đổi SQL (tách riêng, **chưa chạy lên DB nào**, chạy lại nhiều lần không lỗi)

Thứ tự: `14 → 15 → 16 → 17` (sau 01..13). A cần thêm vào `run-db.bat`/`reset-db.bat`; C2 không tự sửa script chung.
```
sqlcmd -S . -d SecondHandMarketplace -i database\14_kiemduyet_tukhoa_c2.sql -f 65001   (tương tự 15, 16, 17)
sqlcmd -S . -d SecondHandMarketplace -i database\tests\c2_smoke_test.sql -f 65001
```

| File | Nội dung | Lý do / mức độ |
|---|---|---|
| `14_kiemduyet_tukhoa_c2.sql` | `TuKhoaCam.MucDo` (CHECK LOW/HIGH, mặc định LOW); `PhanTichAI.MucDoTuKhoa`; bảng `LichSuViPham` (UNIQUE theo `MaLuotKiemDuyet`, CHECK nguồn/JSON); trigger `trg_LuotKiemDuyet_ChanDuyetViPham` | Chỉ thêm. Trigger chặn duyệt khi chưa phân tích xong/QUEUED/FAILED/chưa quét (50210) hoặc HIGH/đã có vi phạm (50211). LOW **được** duyệt. Có đoạn dọn bản nháp cũ (chỉ xóa 3 cột LOW-quyết-định do chính C2 tạo trước đó, nếu có) |
| `15_luot_kiem_duyet_tu_choi_he_thong_c2.sql` | Cột `LuotKiemDuyet.QuyetDinhTuDong` + nới `CK_LuotKiemDuyet_DaQuyetDinh`: `MaQuanTriVien` chỉ được NULL khi `QuyetDinhTuDong=1` và `REJECTED` | **Sửa ràng buộc bảng dùng chung.** Cần vì schema cũ không có đường hợp lệ để hệ thống đóng lượt (bắt buộc Admin) mà không giả mạo. Duyệt vẫn bắt buộc Admin. Có đoạn hoàn tác |
| `16_bo_chan_tu_cam_khi_gui_duyet_c2.sql` | `CREATE OR ALTER fn_KiemTraRuleSanPham` = nguyên văn cũ, bỏ 2 khối `TU_CAM` | **Sửa hàm của 07_functions.sql.** Hàm cũ chặn sản phẩm chứa từ khóa cấm ngay lúc gửi duyệt nên không tới được phân tích. Khuyến nghị chủ 07 xóa 2 khối đó trong 07 rồi bỏ file 16 (chạy lại 07 sẽ làm rule quay lại) |
| `17_ghi_vi_pham_low_khi_tu_choi_c2.sql` | Trigger `trg_LuotKiemDuyet_GhiViPhamLow` | Thêm trigger. Khi Admin từ chối (NULL→REJECTED, có Admin, `QuyetDinhTuDong=0`) lượt có phân tích DONE + `MucDoTuKhoa='LOW'` và lượt chưa có vi phạm → ghi 1 vi phạm. Không ghi khi QUEUED/FAILED/NONE. Hoàn tác: `DROP TRIGGER dbo.trg_LuotKiemDuyet_GhiViPhamLow` |

Mọi thành viên phải chạy 14 và 15 trước khi dùng code mới (entity có cột mới + bảng `LichSuViPham`); 17 cần 15. Dòng cũ của `TuKhoaCam` nhận `LOW`; Admin đặt HIGH ở màn hình Từ khóa cấm.

## 3. File đã sửa / thêm

Mới – SQL: `database/14_…c2.sql`, `15_…c2.sql`, `16_…c2.sql`, `17_ghi_vi_pham_low_khi_tu_choi_c2.sql`, `database/tests/c2_smoke_test.sql`
Mới – C#: `Services/Moderation/{ModerationPolicy, KeywordMatch, ModerationDecision, IKeywordScanner, SqlKeywordScanner, IModerationService, ModerationService, IForbiddenKeywordService, ForbiddenKeywordService}.cs`, `Models/Entities/LichSuViPham.cs`, `Models/ViewModels/Admin/ForbiddenKeywordsVm.cs`, `Areas/Admin/Controllers/ForbiddenKeywordsController.cs`, `Areas/Admin/Views/ForbiddenKeywords/Index.cshtml` (màn hình quản lý từ khóa – thuộc C2)
Mới – test: `tests/…/{ModerationDecisionTests, ModerationServiceTests, ForbiddenKeywordServiceTests}.cs`
Sửa (thêm vài dòng): `Models/Entities/TuKhoaCam.cs` (+`MucDo`), `PhanTichAI.cs` (+`MucDoTuKhoa`), `LuotKiemDuyet.cs` (+`QuyetDinhTuDong`), `Data/AppDbContext.cs` (+`DbSet<LichSuViPham>` và cấu hình), `Common/Extensions/ServiceCollectionExtensions.cs` (+3 dòng DI + using).
**Không sửa:** `_Layout.cshtml` (đã hoàn lại nguyên bản), `ProductsController`/luồng sản phẩm của B, Controller/View của C1, `UserAdminService`, `NotificationService`, script chung của A, `11_danhgia.sql`.

## 4. Tích hợp

**B** – menu: thêm 1 dòng vào `_Layout.cshtml` (B phụ trách):
`<a class="navlink" asp-area="Admin" asp-controller="ForbiddenKeywords" asp-action="Index">Từ khóa cấm</a>`
**B** – gửi duyệt: ngay sau khi `sp_GuiDuyetSanPham` thành công và đã `SaveChanges` mọi thay đổi chưa lưu:
```csharp
await _moderation.PhanTichSanPhamAsync(maSanPham, cancellationToken);   // IModerationService
// Sản phẩm có thể đã thành REJECTED (HIGH): đọc lại trạng thái nếu cần hiển thị.
```
Lỗi khi quét thành FAILED (không ném); lỗi DB thật vẫn ném – sản phẩm khi đó QUEUED nên không duyệt được.

**C1** – dùng `IModerationService`:
- `GetReviewAsync(maSanPham)` / `GetReviewByTurnAsync(maLuot)` → `ModerationReview`: trạng thái phân tích, mức, từ khóa (cụm từ, mức, trường Ten/MoTa, vị trí), `ViolationRecorded`, `SellerViolationCount`/`LockThreshold`, `CanApprove` + `ApproveBlockedReason`, `RejectionRecordsViolation` (true = LOW: nếu từ chối sẽ ghi 1 vi phạm → nên cảnh báo trên nút Từ chối), `CanReanalyze`.
- Duyệt: ẩn nút khi `CanApprove=false`; DB vẫn chặn bằng lỗi 50210/50211 nếu bỏ qua.
- Từ chối: gọi `sp_TuChoiSanPham` như hiện tại (vi phạm LOW do trigger ghi, không cần C# làm gì thêm).
- **Sau mỗi lần duyệt/từ chối thành công** gọi `AfterAdminDecisionAsync(maSanPham)` → `AdminDecisionFollowUp(ViolationRecorded, ViolationCount, SellerLockedNow, SellerLocked)`: áp dụng ngưỡng 3 → khóa + thông báo (idempotent, gọi lặp/đồng thời an toàn). Nếu C1 quên gọi, vi phạm vẫn được ghi nhưng việc khóa chỉ xảy ra ở lần gọi sau (`EnforceViolationThresholdAsync(sellerId)` có thể gọi để vá).
- Phân tích lại (kể cả FAILED): `PhanTichSanPhamAsync(maSanPham)`.
- Lượt do hệ thống tự từ chối có `MaQuanTriVien = NULL`, `QuyetDinhTuDong = 1`: màn lịch sử cần xử lý null khi hiển thị "người duyệt".
- Hợp đồng context: gọi sau `SaveChanges` của bạn (service dùng chung `AppDbContext` scoped, giống `INotificationService`).

**A**: `LockUserAsync(int? actorId, int userId, string reason, ct)` dùng nguyên, `actorId = null`; việc cần A: thêm file 14–17 vào script chạy DB. **C5**: dùng nguyên `CreateAsync` (`PRODUCT` khi tự từ chối HIGH, `SYSTEM` khi khóa). **D**: không chạm.

## 5. Giả định cần bạn xác nhận
1. Sau mở khóa bộ đếm không reset ⇒ vi phạm kế tiếp lại khóa ngay (đúng "không xóa lịch sử"; muốn đếm từ lần mở khóa cần mốc thời gian).
2. Admin từ chối LOW ghi vi phạm cả khi Admin cố ý từ chối vì lý do khác (không phân biệt lý do).
3. Sản phẩm PENDING_REVIEW có sẵn từ seed (PhanTichAI kiểu cũ, `MucDoTuKhoa` NULL) không duyệt được tới khi `PhanTichSanPhamAsync` quét lại.
4. Bypass còn lại: gọi thẳng `sp_ChuyenTrangThaiSanPham` PENDING_REVIEW→ACTIVE không qua lượt duyệt thì trigger không chặn (ngoài phạm vi C2).
5. Trường hợp Admin từ chối LOW khi chưa chạy file 17: **không** có vi phạm nào được ghi và C# không ghi bù (tránh trùng); `AfterAdminDecisionAsync` báo `ViolationRecorded=false`.

## 6. Việc đã bảo đảm ở DB và C#
- `UNIQUE(LichSuViPham.MaLuotKiemDuyet)` + CHECK nguồn/Admin/JSON; `UNIQUE PhanTichAI(MaLuotKiemDuyet)` có sẵn; CHECK `MucDo`/`MucDoTuKhoa`.
- C#: transaction + `UPDLOCK` dòng lượt và sản phẩm (khi chạy SQL Server), bắt lỗi 2601/2627 thử lại một lần.
- Trigger chặn duyệt chạy với mọi đường duyệt; trigger ghi vi phạm LOW cùng transaction với quyết định.

## 7. Kết quả build/test thực tế (trung thực)

| Hạng mục | Kết quả |
|---|---|
| `dotnet build` / `dotnet test` solution thật | **Không chạy được**: sandbox chặn `api.nuget.org` (restore thất bại). |
| Harness giả lập (EF Core/xunit viết tay tối thiểu + .NET 10 SDK + ASP.NET shared framework) | Code Services/Moderation, controller và 3 file test **biên dịch được; 59/59 test pass**. Chỉ kiểm logic C# và cú pháp trên API giả lập, **không** thay build thật (có thể khác ở `FromSql`, `SqlQuery<int>`, `ReloadAsync`, nạp navigation). |
| Test "LOW từ chối = 1 vi phạm" ở C# | Test giả lập hành vi trigger (tự thêm dòng `LichSuViPham`), vì InMemory không chạy trigger. Bằng chứng thật nằm ở `c2_smoke_test.sql` (08a–08d) – **chưa chạy**. |
| View Razor `ForbiddenKeywords/Index.cshtml` | Chưa biên dịch (chỉ rà bằng mắt). |
| SQL 14–17, trigger, `c2_smoke_test.sql` | **Chưa chạy** (không có SQL Server). Rà bằng mắt, có thể còn lỗi cú pháp/tên cột. |
| Hai request đồng thời | InMemory không có UNIQUE/khóa; chưa kiểm tự động. Có kịch bản 2 phiên thủ công cuối `c2_smoke_test.sql`. |
| `ForbiddenKeywordService.AddAsync` | Phần gọi `sp_ThemTuKhoaCam` chưa kiểm (chỉ kiểm validate). |

## 8. Kiểm thử nhanh sau khi nhận
1. Chạy 14→17, rồi `c2_smoke_test.sql` → mọi dòng PASS (gửi lại dòng FAIL nếu có).
2. `dotnet build` + `dotnet test`.
3. Tay: thêm "hàng giả" (HIGH) và "đặt cọc trước" (LOW) ở Admin → Từ khóa cấm; gửi duyệt sản phẩm chứa HIGH (B gọi `PhanTichSanPhamAsync`) → REJECTED + 1 vi phạm + thông báo; sản phẩm chứa LOW: duyệt → 0 vi phạm, từ chối → 1 vi phạm; đủ 3 → LOCKED.
