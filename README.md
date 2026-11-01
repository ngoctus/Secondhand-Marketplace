# Secondhand Marketplace

Web mua bán đồ second hand (BTL nhóm). ASP.NET Core MVC (.NET 10), SQL Server, Entity Framework Core theo hướng **data-first** (viết SQL trước, scaffold ra entity).

## Quy ước chung

- Namespace gốc: `SecondHandMarketplace.Web`.
- Đăng nhập bằng **cookie** + BCrypt, **không dùng ASP.NET Identity**. Không dùng SignalR.
- Chỉ có 2 role: `USER` và `ADMIN` (viết hoa, khớp cột `NguoiDung.VaiTro`). Không có role Buyer/Seller: ai cũng là `USER`, vừa mua vừa bán được.
- Không sửa tay entity trong `Models/Entities` và `Data/AppDbContext.cs`: đổi DB thì sửa SQL trong `database/` rồi scaffold lại (xem mục 4).

## 1. Yêu cầu máy

- .NET 10 SDK
- SQL Server (Developer / Express đều được) và công cụ `sqlcmd`
- Visual Studio 2022 trở lên, hoặc VS Code + C# Dev Kit
- Node.js 20 LTS trở lên (có sẵn npm), tải tại https://nodejs.org. `dotnet build` dùng Node để biên dịch TypeScript; thiếu Node thì build báo lỗi rõ ràng

## 2. Tạo database

```bat
cd database
run-db.bat
```

Script chạy trên SQL Server `localhost` bằng Windows Authentication và tạo database `SecondHandMarketplace`. Nếu bạn dùng instance khác (ví dụ `.\SQLEXPRESS`, LocalDB), sửa `-S` trong `run-db.bat` cho khớp.

## 3. Cấu hình và chạy

`appsettings.json` đã có chuỗi kết nối mặc định (`Server=localhost;Database=SecondHandMarketplace;...`). Nếu máy bạn khác, **không sửa file này**; hãy tạo file riêng, đã được git bỏ qua:

```bat
copy appsettings.Example.json appsettings.Development.json
```

rồi sửa `ConnectionStrings:DefaultConnection` trong `appsettings.Development.json`.

Chạy:

```bat
dotnet restore
dotnet run
```

Hoặc mở `Secondhand-Marketplace.sln` trong Visual Studio rồi nhấn F5. Địa chỉ mặc định: https://localhost:7080

## 4. Scaffold lại entity từ database (data-first)

Cài công cụ một lần: `dotnet tool install --global dotnet-ef`

```bat
dotnet ef dbcontext scaffold "Name=ConnectionStrings:DefaultConnection" Microsoft.EntityFrameworkCore.SqlServer --context AppDbContext --context-dir Data --output-dir Models/Entities --namespace SecondHandMarketplace.Web.Models.Entities --context-namespace SecondHandMarketplace.Web.Data --no-onconfiguring --force
```

Chạy lại lệnh này mỗi khi schema thay đổi. Code tự viết nên nằm ngoài `Models/Entities` và `AppDbContext.cs` (dùng `partial class` nếu cần mở rộng), vì hai nơi này bị ghi đè.

## 5. Còn đang làm (cập nhật mục này khi xong)

- [ ] Hoàn thiện script SQL trong `database/` và seed tài khoản Admin
- [ ] Scaffold DbContext và entity, dọn code code-first cũ
- [ ] Trang đăng nhập (cookie) và kiểm tra đọc dữ liệu từ DB hiển thị trên View
- [x] TypeScript build ra `wwwroot` (xem mục 7)
- [ ] Tài khoản Admin seed: _(điền email và cách lấy mật khẩu khi có)_

## 6. Git

- Mỗi người làm trên nhánh riêng, tạo Pull Request vào nhánh chung, không push thẳng.
- Khi `git status` báo gần như mọi file bị sửa, thường là do khác kiểu xuống dòng (CRLF/LF). Chỉ `git add` đúng file mình sửa.

## 7. Front-end (TypeScript)

- Mã nguồn nằm ở `Scripts/*.ts`, build ra `wwwroot/dist/` (đã `.gitignore`, **không commit và không sửa tay** file `.js` trong đó).
- `dotnet build` / F5 tự chạy `npm install` (khi `package.json` đổi) và `npm run build`, nên clone về chỉ cần có Node.js rồi chạy như bình thường. Bỏ qua bước này (máy không có Node): `dotnet build -p:SkipTsBuild=true`.
- Đang sửa `.ts` và muốn tự biên dịch lại: `npm run watch` (chạy ở thư mục chứa `package.json`).
- `_Layout` nạp `dist/site.js` dạng module. Trang cần gọi API thì import trong `@section Scripts`:

```html
<script type="module">
    import { api } from "/dist/api.js";
    const kq = await api.post("/api/...", { ... });   // kq: { success, message, data }, khớp Common/KetQua.cs
    if (kq.success) { /* dùng kq.data */ }
</script>
```

  `goiApi` / `api` tự xử lý: 401 chuyển sang `/Account/Login?ReturnUrl=...`, 403 hiện "không có quyền", mất mạng và lỗi 400/500 thành `success = false` kèm `message`. Mặc định tự hiện hộp thông báo khi thất bại; muốn tự xử lý thì truyền `{ hienLoi: false }`.
- Hiện thông báo từ JS: `thongBao.thanhCong("...")`, `thongBao.loi("...")` (import từ `/dist/thongbao.js`).
- Partial dùng chung trong `Views/Shared`:
  - `_ThongBao`: hộp thông báo (TempData từ `this.ThongBaoThanhCong/ThongBaoLoi` và thông báo từ JS), đã nhúng sẵn trong `_Layout`.
  - `_PhanTrang`: `@await Html.PartialAsync("_PhanTrang", (trangHienTai, tongSoTrang))`. Giữ nguyên các tham số lọc trên URL; mặc định dùng tham số `trang`, đổi bằng `ViewData["ThamSoTrang"] = "page"`.
