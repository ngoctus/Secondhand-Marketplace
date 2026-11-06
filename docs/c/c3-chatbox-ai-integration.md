# C3 – Chatbox AI: cấu hình, phối hợp và đề xuất database

> **Cập nhật lần 2:** các mục 2.1 (xác thực), 2.2 (SQL) và 2.4 (DI) bên dưới đã được xử lý, xem `docs/C3-Integration-Report.md` (bản mô tả hiện hành). Bảng cấu hình AI nay nằm ở `database/20_cauhinh_ai.sql`, không thêm vào `11_danhgia.sql`.

Tài liệu này ghi lại những gì C3 đã làm độc lập và những thay đổi **cần A / B thực hiện** (C3 không tự sửa các file đó).

## 1. Cấu hình

Nhà cung cấp AI dùng trong C3: Anthropic Messages API (tool use). Đây là **giả định** vì tài liệu phân công chưa chốt provider; phần phụ thuộc provider nằm gọn trong `Services/Ai/AiChatService.cs`.

### API Key (User Secrets – không commit)

`UserSecretsId` đã có trong `SecondHandMarketplace.Web.csproj`. Mỗi thành viên tự chạy trên máy mình:

```bat
cd src\SecondHandMarketplace.Web
dotnet user-secrets set "Ai:ApiKey" "<khóa-api-của-bạn>"
dotnet user-secrets list
```

Hoặc dùng biến môi trường `Ai__ApiKey`. Không đặt khóa trong `appsettings*.json`. Khóa chỉ nằm ở máy chủ; widget JS không bao giờ nhận được khóa.

### Tùy chọn khác (section `Ai`, đều có mặc định, không bắt buộc)

| Khóa | Mặc định | Ý nghĩa |
| --- | --- | --- |
| `Ai:AllowedModels` | claude-haiku-5-5, claude-sonnet-5-5, claude-opus-5-5 | Danh sách model Admin được chọn |
| `Ai:DefaultModel` | phần tử đầu của AllowedModels | Model dùng khi database chưa có cấu hình |
| `Ai:TimeoutSeconds` | 50 (5–120) | Tổng thời gian tối đa cho một lượt chat |
| `Ai:MaxTokens` | 1024 | Giới hạn độ dài câu trả lời |
| `Ai:MaxToolRounds` | 3 | Số vòng gọi công cụ tối đa |
| `Ai:Endpoint` | https://api.anthropic.com/v1/messages | |

## 2. Việc cần A

### 2.1. Xác thực cho `/api/ai/chat`

Đã xử lý: endpoint nhận cả Cookie và JWT, Cookie có kiểm tra Antiforgery. Xem `docs/C3-Integration-Report.md`, mục 3.

### 2.2. Bảng cấu hình AI

Đã chuyển sang file mới `database/20_cauhinh_ai.sql` (không sửa `11_danhgia.sql`). Xem `docs/C3-Integration-Report.md`, mục 7.

### 2.3. Entity và `AppDbContext` (không bắt buộc ngay)

`AiSettingsService` hiện dùng SQL tham số hóa nên **chạy được ngay khi bảng tồn tại, không cần Entity**. Nếu A muốn thống nhất theo EF, thêm:

`Models/Entities/CauHinhAI.cs`
```csharp
namespace SecondHandMarketplace.Web.Models.Entities;

/// <summary>Bảng CauHinhAI (một dòng duy nhất, MaCauHinhAI = 1).</summary>
public class CauHinhAI
{
    public int MaCauHinhAI { get; set; }
    public bool DangBat { get; set; } = true;
    public string TenModel { get; set; } = string.Empty;
    public string SystemPrompt { get; set; } = string.Empty;
    public int? MaNguoiCapNhat { get; set; }
    public DateTime NgayCapNhat { get; set; }
}
```
`Data/AppDbContext.cs`
```csharp
public DbSet<CauHinhAI> CauHinhAIs => Set<CauHinhAI>();
...
modelBuilder.Entity<CauHinhAI>(e =>
{
    e.ToTable("CauHinhAI");
    e.HasKey(x => x.MaCauHinhAI);
    e.Property(x => x.MaCauHinhAI).ValueGeneratedNever();
    e.Property(x => x.TenModel).IsUnicode(false).HasMaxLength(100);
    e.Property(x => x.SystemPrompt).HasMaxLength(4000);
});
```
Sau đó C3 chỉ cần thay phần truy vấn trong `AiSettingsService.GetAsync/SaveAsync` (interface không đổi).

### 2.4. Đăng ký DI

Đã tích hợp `AddAiChat()` vào `AddAppServices`. Xem `docs/C3-Integration-Report.md`, mục 4.

## 3. Việc cần B

1. Thêm **một dòng** vào `Views/Shared/_Layout.cshtml`, ngay trước `</body>`:
   ```cshtml
   <partial name="_AiChatWidget" />
   ```
   Partial đã có sẵn (`Views/Shared/_AiChatWidget.cshtml`), tự nạp `ai-chat.css`/`ai-chat.js` và chỉ hiện cho người đã đăng nhập.
2. (Tùy chọn) thêm link menu Admin: `<a class="navlink" asp-area="Admin" asp-controller="AiSettings" asp-action="Index">Chatbox AI</a>`. Hiện vào được bằng địa chỉ `/Admin/AiSettings`.
3. Vị trí nút chat: góc dưới bên phải (`right:20px; bottom:20px`, `z-index:12`). Nếu trùng với phần tử B đặt ở đó thì chỉnh hai biến này đầu file `wwwroot/css/ai-chat.css`. CSS của C3 dùng biến `--green`, `--lime`, `--line`, `--paper`, `--ink`, `--muted` của `site.css` (có giá trị dự phòng) và font DM Sans.

## 4. Thành phần C3

| Thành phần | File |
| --- | --- |
| Gọi AI + function calling | `Services/Ai/AiChatService.cs`, `AiToolExecutor.cs` |
| Cấu hình (DB + mặc định) | `Services/Ai/AiSettingsService.cs`, `AiOptions.cs`, `AiSettings.cs` |
| API | `Controllers/Api/AiChatController.cs` (`POST /api/ai/chat`, `GET /api/ai/status`) |
| Admin | `Areas/Admin/Controllers/AiSettingsController.cs`, `Views/AiSettings/Index.cshtml` |
| Widget | `wwwroot/js/ai-chat.js`, `wwwroot/css/ai-chat.css`, `Views/Shared/_AiChatWidget.cshtml` |

Công cụ AI được phép gọi (danh sách cố định, mọi truy vấn qua `IProductService`/`ICategoryService`): `search_products`, `recommend_products`, `draft_product_description` (USER); ADMIN chỉ dùng hai công cụ đầu. Không có đường nào cho AI chạy SQL hay ghi dữ liệu.

Phản hồi API: `{ "success": bool, "message": string, "data": { "reply": string } }`. Mã lỗi: 400 dữ liệu sai, 401 chưa xác thực, 403 không đủ quyền, 503 Chatbox tắt (`data.disabled = true`) hoặc chưa cấu hình khóa, 502 lỗi nhà cung cấp/phản hồi sai, 504 quá thời gian.

Chưa làm (ngoài phạm vi): giới hạn tần suất gọi theo người dùng (cần middleware trong `Program.cs`, thuộc A) – nên cân nhắc vì mỗi tin nhắn tốn chi phí gọi AI.

## 5. Checklist kiểm thử thủ công (cần SQL Server + khóa API thật)

1. Chạy `run-db.bat` (sau khi A thêm bảng CauHinhAI), `dotnet user-secrets set "Ai:ApiKey" ...`, chạy web.
2. Đăng nhập, gửi "chào" → có phản hồi.
3. "Tìm điện thoại dưới 5 triệu" → danh sách khớp sản phẩm ACTIVE trong DB (đối chiếu trang `/Products`).
4. "Tôi cần laptop học online ngân sách 8 triệu" → chỉ gợi ý sản phẩm có thật, đúng giá.
5. "Viết mô tả cho áo khoác jeans size M, còn mới" → bản nháp không thêm thông tin ngoài đầu vào; DB không đổi.
6. Khóa API sai → thông báo chung, không lộ khóa; log máy chủ ghi 401.
7. Ngắt mạng/chặn domain AI → báo lỗi, trang không treo.
8. Tin nhắn rỗng / > 1000 ký tự → báo lỗi 400.
9. Admin tắt Chatbox → `POST /api/ai/chat` trả 503, widget ẩn sau khi tải lại trang.
10. Tài khoản USER vào `/Admin/AiSettings` → bị từ chối; ADMIN vào được.
11. Admin đổi model/System Prompt → tin nhắn kế tiếp dùng cấu hình mới (kiểm tra bảng CauHinhAI và log).
12. Xem Network/HTML nguồn của trang: không có khóa API.
