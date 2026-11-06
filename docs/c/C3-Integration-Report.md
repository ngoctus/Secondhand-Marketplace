# C3 – Báo cáo tích hợp (lần 2)

> **Trạng thái kiểm thử, đọc trước:** phần C# (service, controller, test xUnit) **chưa được biên dịch và chưa được chạy** vì môi trường làm việc không có .NET SDK và không có mạng. Script SQL **chưa được chạy** trên SQL Server. Chỉ có widget JavaScript được chạy thật (Chromium, API giả lập). Chi tiết ở mục 8. Cần chạy `dotnet build` và `dotnet test` trước khi merge.

## 1. Tổng quan

**Vấn đề ban đầu**

1. `/api/ai/chat` chỉ nhận JWT, widget trên web dùng Cookie nên không gọi được; chưa có bảo vệ CSRF cho trường hợp dùng Cookie.
2. `AddAiChat()` chưa được nối vào `ServiceCollectionExtensions.cs`; đăng ký cũ không cấp `HttpClient`/`IAiSettingsService`.
3. Chưa có giới hạn tần suất và giới hạn số request đồng thời theo tài khoản.
4. Chưa có script SQL cho bảng cấu hình AI nằm đúng quy ước `20_`.

**Kết quả (về mặt code)**

1. Endpoint nhận Cookie hoặc JWT, người chưa đăng nhập nhận 401 JSON; Cookie bắt buộc có Antiforgery token.
2. `AddAiChat()` thay đăng ký cũ trong `AddAppServices` (sửa đúng một dòng).
3. `AiRequestThrottle`: USER 5/phút + 30/giờ, ADMIN 10/phút + 60/giờ, tối đa 2 request đồng thời mỗi tài khoản.
4. `database/20_cauhinh_ai.sql` tạo bảng `CauHinhAI` và dòng cấu hình mặc định.

## 2. Danh sách file đã chỉnh sửa

| File | Thay đổi | Lý do |
| --- | --- | --- |
| `src/.../Controllers/Api/AiChatAuthorizeAttribute.cs` (**mới**) | Authorization filter: thử JWT trước, rồi Cookie; không có cả hai thì 401 JSON; Cookie + phương thức không an toàn thì kiểm tra Antiforgery | Hỗ trợ đồng thời hai scheme, biết yêu cầu xác thực bằng scheme nào để chỉ áp CSRF cho Cookie |
| `src/.../Controllers/Api/AiChatController.cs` | Dùng `[AiChatAuthorize]` thay `[Authorize(Bearer)]`; lấy userId/role từ claim; gọi `IAiRequestThrottle` trước khi gọi AI; trả 429 + `Retry-After` | Xác thực, rate limit, concurrency |
| `src/.../Services/Ai/AiRequestThrottle.cs` (**mới**) | Giới hạn phút/giờ (cửa sổ trượt) + semaphore 2 slot theo tài khoản | Mục C, D |
| `src/.../Services/Ai/AiServiceCollectionExtensions.cs` | Thêm đăng ký `TimeProvider` (TryAdd), `IAiRequestThrottle` (singleton); typed client đặt `Timeout` vô hạn | Mục B, C |
| `src/.../Common/Extensions/ServiceCollectionExtensions.cs` | Một dòng: `AddScoped<IAiChatService, AiChatService>()` → `AddAiChat()` | Tích hợp DI (file được phép sửa theo yêu cầu) |
| `src/.../Views/Shared/_AiChatWidget.cshtml` | Cấp Antiforgery token qua `data-csrf-token` (file này do C3 tạo ở lần 1, chưa nằm trong Layout) | Widget gửi token đúng cách |
| `src/.../wwwroot/js/ai-chat.js` | Gửi header `RequestVerificationToken`; tăng timeout phía client 65s → 90s | CSRF; vì request có thể chờ hàng đợi tối đa 30s trước khi xử lý (tối đa 50s) |
| `src/.../Services/Ai/AiChatService.cs` | Chỉ sửa comment về tham số `HttpClient?`/`IAiSettingsService?` đã lỗi thời. Không đổi logic, vẫn giữ tham số mặc định null | DI đã đúng nên comment cũ gây hiểu nhầm |
| `database/20_cauhinh_ai.sql` (**mới**) | Bảng `CauHinhAI` + dòng mặc định | Mục 7 |
| `tests/.../AiRequestThrottleTests.cs`, `AiChatEndpointTests.cs`, `AiDependencyInjectionTests.cs` (**mới**) | Test mục 8 | Yêu cầu kiểm thử |
| `tests/.../SecondHandMarketplace.Tests.csproj` | Thêm `FrameworkReference Microsoft.AspNetCore.App` | Test dựng host ASP.NET Core thật; không thêm package nào |
| `docs/c3-chatbox-ai-integration.md` | Đánh dấu các mục 2.1/2.2/2.4 đã được thay thế | Tránh tài liệu cũ nói ngược với hiện trạng |

**Không sửa:** `Program.cs`, cấu hình Cookie/JWT, `AppDbContext`, Entity, `_Layout.cshtml`, mọi file SQL hiện có, ba tool AI, quyền của các tool.

## 3. Authentication

- **Cookie và JWT:** `AiChatAuthorizeAttribute` gọi `AuthenticateAsync("Bearer")`; thành công thì dùng principal đó. Nếu không thì gọi `AuthenticateAsync("Cookies")`. Tên scheme lấy từ `JwtBearerDefaults`/`CookieAuthenticationDefaults`, đúng với cấu hình hiện có trong `Program.cs` (không thay đổi gì ở đó). Hai handler hiện có vẫn chạy nguyên (gồm kiểm tra tài khoản còn `ACTIVE`).
- **Người chưa đăng nhập:** 401 với body `KetQua` JSON và header `WWW-Authenticate: Bearer`. Không dùng `[Authorize]` vì Cookie scheme là scheme challenge mặc định và sẽ chuyển hướng 302 sang `/Account/Login`, không hợp với API. Người có tài khoản nhưng không phải USER/ADMIN: 403.
- **CSRF (chỉ cho yêu cầu xác thực bằng Cookie, phương thức POST):**
  - Dùng Antiforgery của ASP.NET Core (`IAntiforgery.ValidateRequestAsync`). Không dựa vào `SameSite=Lax` hay `Content-Type`.
  - `_AiChatWidget.cshtml` cấp token (`GetAndStoreTokens`), `ai-chat.js` gửi lại qua header `RequestVerificationToken`. Token gắn với người dùng đang đăng nhập.
  - Thiếu/sai token hoặc token của tài khoản khác: 400, AI không được gọi.
  - JWT không cần token (header `Authorization` do mã client đặt, trình duyệt không tự gửi từ trang khác). Bearer sai/hết hạn **không** được dùng để lách: khi đó yêu cầu rơi về nhánh Cookie và vẫn bị kiểm tra CSRF.
  - `GET /api/ai/status` là phương thức an toàn nên không cần token.
  - Phạm vi gọn: chỉ endpoint chat; không đụng form hay API khác.
- Lưu ý: `GetAndStoreTokens` của Antiforgery thêm header chống cache cho trang có chứa widget (người dùng đã đăng nhập). Trang nào cũng có thể bị ảnh hưởng khi B gắn partial vào Layout.

## 4. Dependency Injection

`AddAppServices()` gọi `AddAiChat()` ở vị trí của dòng cũ. `AddAiChat()` đăng ký:

| Dịch vụ | Vòng đời | Ghi chú |
| --- | --- | --- |
| `IAiChatService` → `AiChatService` | Transient (do `AddHttpClient<,>`) | Typed `HttpClient`; `Timeout = Infinite` vì thời hạn do chính service kiểm soát bằng `CancellationToken` (`Ai:TimeoutSeconds`, mặc định 50s); mọi header/URL đặt theo từng request |
| `IAiSettingsService` → `AiSettingsService` | Scoped | Dùng `AppDbContext` |
| `IAiRequestThrottle` → `AiRequestThrottle` | **Singleton** | Phải giữ trạng thái giữa các request |
| `TimeProvider` | Singleton (`TryAdd`) | Không ghi đè nếu nơi khác đã đăng ký |

Dependency khác của C3 (`IProductService`, `ICategoryService`, `AppDbContext`, `IConfiguration`, `ILogger`) đã có sẵn. Đăng ký cũ `AddScoped<IAiChatService, AiChatService>()` đã được thay thế, không còn đăng ký trùng. `AiChatService` vẫn giữ tham số constructor mặc định null cho `HttpClient?`/`IAiSettingsService?` (không còn cần cho DI, giữ lại để không đổi code đang chạy và để dựng thủ công trong test).

## 5. Rate Limiting

| Role | Mỗi phút | Mỗi giờ |
| --- | --- | --- |
| USER | 5 | 30 |
| ADMIN | 10 | 60 |

- **Định danh:** User ID từ claim `NameIdentifier` của principal đã xác thực (cookie đã mã hóa hoặc JWT đã ký). Role cũng lấy từ claim, không từ body/query/header, nên client không thể tự nâng hạn mức. Nội dung request có `"role":"ADMIN"` vẫn bị áp hạn mức USER (có test).
- **Cơ chế:** cửa sổ trượt: ghi lại thời điểm mỗi request được nhận trong 1 giờ gần nhất. Request mới chỉ được nhận khi thỏa **đồng thời** cả hai hạn mức. Thời gian chờ = giá trị lớn hơn giữa "khi request cũ nhất trong hạn mức phút hết hạn" và "khi request cũ nhất trong hạn mức giờ hết hạn", nên hết chỗ giờ thì giải phóng chỗ phút không đủ.
- **Khi hết quota (cooldown):** request **chờ** đến khi có chỗ, có thể hủy khi client ngắt kết nối. Có giới hạn chờ tối đa 30 giây (`AiThrottleOptions.MaxWait`): nếu phải chờ lâu hơn (ví dụ hết quota giờ thì phải chờ hàng chục phút) thì **trả ngay `429 Too Many Requests`** kèm `Retry-After` (giây) và `data.retryAfterSeconds`, thay vì giữ kết nối rất lâu. Đây là điều chỉnh có chủ đích so với "chờ vô hạn", theo đúng gợi ý của yêu cầu; nếu cần khác (ví dụ 429 ngay, hoặc chờ lâu hơn) chỉ cần đổi `MaxWait`.
- **Độc lập giữa tài khoản:** mỗi tài khoản có lịch sử và khóa riêng.
- **Request không hợp lệ** (tin nhắn rỗng, sai CSRF, chưa đăng nhập) bị từ chối trước khi vào throttle nên không tốn quota. Request đã được nhận rồi mới bị hủy trước khi xử lý thì quota được hoàn lại.
- **Giới hạn đã biết:** trạng thái nằm trong bộ nhớ một tiến trình (khởi động lại hoặc chạy nhiều instance thì không chia sẻ). Tài khoản không hoạt động quá 1 giờ được dọn tự động.

## 6. Concurrency

- Mỗi tài khoản có `SemaphoreSlim(2)` riêng, không có giới hạn toàn hệ thống. Không nhầm với 5/10 mỗi phút.
- **Thứ tự:** chờ quota trước, chỉ sau khi được nhận mới chờ slot đồng thời, nên request đang đợi quota không chiếm slot.
- **Request thứ ba:** chờ tối đa `MaxWait` (30s). Khi một request trước xong thì request chờ được chạy tiếp; nếu quá hạn chờ thì 429 (loại "bận", `Retry-After` 10s).
- **Giải phóng slot:** controller giữ `lease` bằng `using var`, nên slot trả lại khi thành công, khi có exception và khi bị hủy/timeout (`AskAsync` tự có hạn chót 50s). Request đang chờ bị hủy hoặc hết hạn chờ không giữ slot nào và hoàn lại quota. `Dispose` gọi nhiều lần không trả dư slot.

## 7. SQL

- **Có tạo file SQL mới:** `database/20_cauhinh_ai.sql` (tiền tố `20_`). Không sửa/đổi tên/xóa file SQL nào có sẵn; không thêm `20_...` vào `run-db.bat` (phần tích hợp là của bạn).
- **Cấu trúc:** bảng `dbo.CauHinhAI`: `MaCauHinhAI int` (PK, ràng buộc `= 1` nên chỉ có một dòng), `DangBat bit` (mặc định 1), `TenModel varchar(100)`, `SystemPrompt nvarchar(4000)`, `MaNguoiCapNhat int NULL` (FK → `NguoiDung.MaNguoiDung`), `NgayCapNhat datetime2(3)` (mặc định `SYSUTCDATETIME()`); CHECK không để trống model/prompt. Dòng mặc định: bật, `claude-haiku-5-5`, System Prompt mặc định (trùng `AiOptions`).
- **Chạy lại an toàn:** chỉ tạo bảng nếu chưa có (`OBJECT_ID`) và chỉ chèn dòng mặc định nếu chưa có; không `DROP`, không ghi đè cấu hình Admin đã lưu.
- **Không tạo bảng trùng:** đã `grep` các file SQL hiện có, chưa có bảng cấu hình AI.
- **Thứ tự chạy:** sau `02_create_tables.sql` (cần `NguoiDung`) và `03_constraints.sql`; không phụ thuộc `10`–`13`. Đề xuất đặt sau `13_daugia.sql` trong `run-db.bat`.
- **Mapping (cần A, không bắt buộc):** `AiSettingsService` dùng SQL tham số hóa nên chạy được khi bảng tồn tại, không cần Entity. Nếu muốn dùng EF: Entity `CauHinhAI` (6 cột như trên, khóa `MaCauHinhAI` `ValueGeneratedNever`), `DbSet<CauHinhAI>` và cấu hình `ToTable("CauHinhAI")` trong `AppDbContext`; mã mẫu ở mục 2.3 của `docs/c3-chatbox-ai-integration.md`.
- **Trạng thái kiểm tra:** chỉ mới **kiểm tra nội dung** (đọc lại, khớp quy ước các file SQL hiện có, đếm cân bằng `BEGIN/END`, ngoặc, `GO`). **Chưa chạy trên SQL Server**, chưa xác nhận biên dịch bằng `sqlcmd`.

## 8. Test

Không test C# nào được chạy (không có .NET SDK trong môi trường). Cần chạy `dotnet test` để biết kết quả thật.

**Đã chạy thành công (Chromium + Playwright, API giả lập, không có database):** widget `ai-chat.js`, 30/30 kiểm tra: mở/đóng, Enter gửi, Shift+Enter không gửi, gửi đúng header Antiforgery, trạng thái "Đang xử lý" và khóa ô nhập, lỗi 429/502/401/mạng/JSON hỏng, widget khóa khi bị tắt, ẩn khi `status` báo tắt, HTML từ AI không được thực thi, giao diện mobile. Ngoài ra `node --check` cho cú pháp JS. (Script test nằm ngoài repo.)

**Đã viết nhưng chưa chạy:**

- `AiChatEndpointTests` (host Kestrel thật, `AiChatController` + filter thật; cấu hình Cookie/JWT sao chép từ `Program.cs` trừ kiểm tra database; AI giả):
  - Cookie + token hợp lệ → 200; Cookie không token → 400; token sai/rỗng → 400; token của tài khoản khác → 400.
  - `status` bằng Cookie không cần token.
  - JWT hợp lệ (USER và ADMIN) → 200, không cần token; JWT sai khóa/hết hạn/sai audience/rác → 401.
  - Bearer sai + Cookie hợp lệ không lách được CSRF → 400.
  - Chưa đăng nhập (POST và GET) → 401 JSON, không chuyển hướng, AI không được gọi.
  - USER request thứ 6 trong phút → 429 + `Retry-After`, role giả trong body bị bỏ qua, tài khoản khác không bị ảnh hưởng; ADMIN request thứ 11 → 429; tin nhắn rỗng không tốn quota.
- `AiRequestThrottleTests` (đồng hồ giả điều khiển bằng tay): USER 5/phút, 30/giờ; ADMIN 10/phút, 60/giờ; quá hạn mức bị từ chối đúng với `Retry-After`; chờ được khi `MaxWait` đủ dài; hạn mức giờ vẫn áp dụng sau khi hạn mức phút đã giải phóng; quota độc lập giữa hai tài khoản; hạn mức theo role; hai request đồng thời, request thứ ba chờ rồi chạy tiếp; slot trả lại sau thành công/exception/hủy; người chờ bị hủy hoàn lại quota; hết hạn chờ slot → "bận" mà không rò slot; hai tài khoản không chặn nhau; `Dispose` hai lần không lố slot; dọn tài khoản nhàn rỗi.
- `AiDependencyInjectionTests`: container dựng được với `ValidateOnBuild`/`ValidateScopes`; throttle là singleton; `AiChatService` nhận `IAiSettingsService` và typed `HttpClient` có `Timeout` vô hạn; `AddAppServices()` chỉ có một đăng ký cho `IAiChatService`/`IAiSettingsService`/`IAiRequestThrottle`/`TimeProvider`, không còn đăng ký cũ; `TryAdd` không ghi đè `TimeProvider` có sẵn.
- Test C3 từ lần 1 (`AiChatServiceTests`, `AiToolExecutorTests`; hồi quy ba tool, API key không lộ, cấu hình DB, v.v.) vẫn chưa chạy.

**Không thể thực hiện:**

- Chạy script SQL và kiểm tra `AiSettingsService` với SQL Server thật (thiếu SQL Server).
- Gọi AI thật (thiếu API key).
- Test thật với `Program.cs` (cần database cho bước kiểm tra tài khoản); test endpoint dùng bản sao cấu hình xác thực nên không bắt được lệch nếu `Program.cs` đổi sau này.

**Rủi ro biên dịch cần lưu ý khi chạy lần đầu:** test endpoint dùng `Microsoft.IdentityModel.JsonWebTokens` (kỳ vọng đi kèm gói JwtBearer của project web); nếu restore báo thiếu, cần thêm tham chiếu gói vào project test. Đã thêm `FrameworkReference Microsoft.AspNetCore.App` vào project test.

## 9. Dependency cần phối hợp với thành viên khác

- **B:** thêm `<partial name="_AiChatWidget" />` trước `</body>` trong `_Layout.cshtml` (partial đã sẵn sàng, đã cấp Antiforgery token). Chưa thêm thì widget chưa hiện. Link menu Admin tới `/Admin/AiSettings` là tùy chọn.
- **A:** không bắt buộc. Entity + `DbSet` + mapping `CauHinhAI` chỉ cần nếu nhóm muốn thống nhất dùng EF thay cho SQL tham số hóa.
- **Bạn (SQL):** chạy `20_cauhinh_ai.sql` trên database (và đưa vào `run-db.bat` nếu muốn). Chưa có bảng này thì Chatbox vẫn chạy bằng cấu hình mặc định trong code nhưng trang Admin AiSettings không lưu được (hiện thông báo rõ ràng, không lỗi 500).
- **Cần nhóm xác nhận quyền các tool (không tự đổi):** hiện USER dùng `search_products`, `recommend_products`, `draft_product_description`; ADMIN chỉ dùng hai tool đầu. Đây là quyết định của lần 1 dựa trên comment ở bản stub, chưa có xác nhận của nhóm.
- **Quan sát ngoài phạm vi C3 (không sửa):** role nằm trong claim nên đổi role người dùng chỉ có hiệu lực sau khi đăng nhập lại/cấp JWT mới (thuộc A); `run-db.bat` chưa gồm file `20_`.

## 10. Những việc chưa hoàn thành

- Chưa build, chưa chạy test C# (mục 8): kết quả thật phải chờ `dotnet build` + `dotnet test`.
- Chưa chạy SQL trên SQL Server.
- Chưa kiểm thử với database và API AI thật.
- Widget chưa gắn vào Layout (chờ B).
- Rate limit chỉ có hiệu lực trong một tiến trình; nếu triển khai nhiều instance cần giải pháp dùng chung (không làm vì ngoài phạm vi).
- `MaxWait` = 30s là lựa chọn của C3, cần bạn xác nhận (xem mục 5).

## 11. Kết luận bàn giao

Phần code cho xác thực Cookie + JWT (kèm CSRF), tích hợp DI, rate limit, concurrency và SQL `20_cauhinh_ai.sql` đã được viết xong theo yêu cầu, **nhưng chưa thể kết luận hoàn thành** vì chưa được build và chạy kiểm thử. Các bước còn lại: (1) chạy `dotnet build` + `dotnet test` và sửa lỗi nếu có; (2) chạy `20_cauhinh_ai.sql`; (3) B thêm một dòng partial vào Layout; (4) `dotnet user-secrets set "Ai:ApiKey" ...` rồi kiểm thử thủ công theo checklist ở `docs/c3-chatbox-ai-integration.md` mục 5.
