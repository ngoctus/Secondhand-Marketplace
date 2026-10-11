# Web2Hand: khung xương Backend (ASP.NET Core MVC, .NET 9)

## 1. Chạy dự án lần đầu

1. Mở `Web2Hand.sln` bằng Visual Studio 2022 (cần .NET 9 SDK).
2. Kiểm tra chuỗi kết nối trong `Web2Hand/appsettings.json` (mặc định dùng LocalDB, máy nào có VS cũng chạy được).
3. Mở **Tools > NuGet Package Manager > Package Manager Console**, chạy:
   ```
   Add-Migration InitialCreate
   ```
   (Khi chạy app, `DbSeeder` sẽ tự áp migration vào DB.)
4. Nhấn F5.
5. Tài khoản Admin mặc định: `admin@web2hand.local` / `Admin@123` (đổi trong `Data/Seed/DbSeeder.cs`).

API key (AI, v.v.) để trong **User Secrets** (chuột phải project > Manage User Secrets), không ghi vào appsettings.json và không commit.

## 2. Sơ đồ thư mục

```
Web2Hand/
├── Program.cs                      Khởi động app, thứ tự middleware, map route + hub
├── Common/
│   ├── Constants/AppRoles.cs       Tên role (Buyer/Seller/Admin)
│   └── Extensions/                 ServiceCollectionExtensions (ĐĂNG KÝ SERVICE Ở ĐÂY), GetUserId()
├── Models/
│   ├── Entities/                   Mỗi bảng DB = 1 file
│   ├── Enums/AppEnums.cs           Mọi enum/trạng thái (OrderStatus, ProductStatus...)
│   └── ViewModels/<Tính năng>/     Dữ liệu form / dữ liệu hiển thị
├── Data/
│   ├── AppDbContext.cs             Danh sách bảng (DbSet)
│   ├── Configurations/             Cấu hình riêng từng bảng (khóa, kiểu dữ liệu)
│   └── Seed/DbSeeder.cs            Role, admin, danh mục mẫu
├── Services/<Tính năng>/           LOGIC NGHIỆP VỤ nằm ở đây (IXxxService + XxxService)
├── Controllers/                    Trang dùng chung: Home, Products, Account, Profile, Notifications, Chat
│   └── Api/                        API JSON (chatbox AI)
├── Areas/
│   ├── Buyer/  (Controllers + Views)   Chỉ role Buyer vào được
│   ├── Seller/ (Controllers + Views)   Chỉ role Seller vào được
│   └── Admin/  (Controllers + Views)   Chỉ role Admin vào được
├── Hubs/                           SignalR realtime: AuctionHub, ChatHub, NotificationHub
├── BackgroundJobs/AuctionJob.cs    Tác vụ chạy nền: mở / đóng phiên đấu giá
├── Views/                          Giao diện dùng chung + Shared/_Layout.cshtml
└── wwwroot/                        css, js, uploads
```

## 3. Gặp tình huống này thì mở file nào?

| Tình huống | Mở file |
|---|---|
| Trang không hiện / 404 | `Areas/<Role>/Controllers/<Tên>Controller.cs`, và kiểm tra có `Views/<Tên>/<Action>.cshtml` |
| Bị đẩy ra trang đăng nhập / AccessDenied | `Areas/<Role>/Controllers/<Role>BaseController.cs` (phân quyền), `Controllers/AccountController.cs` |
| Lỗi `Unable to resolve service for type...` | `Common/Extensions/ServiceCollectionExtensions.cs` > `AddAppServices` (quên đăng ký) |
| Dữ liệu sai / lỗi truy vấn | `Services/<Tính năng>/<Tên>Service.cs` |
| Thêm cột / thêm bảng | `Models/Entities/` + `Data/AppDbContext.cs` + tạo migration |
| Đổi trạng thái đơn / sản phẩm | `Models/Enums/AppEnums.cs` + service tương ứng |
| Sai luật đặt giá, kết thúc đấu giá | `Services/Auctions/AuctionService.cs` |
| Giá không cập nhật realtime | `Hubs/AuctionHub.cs`, `wwwroot/js/auction.js` |
| Phiên đấu giá không tự đóng | `BackgroundJobs/AuctionJob.cs` |
| Thông báo không tới | `Services/Notifications/NotificationService.cs`, `Hubs/NotificationHub.cs` |
| Chatbox AI | `Services/Ai/AiChatService.cs`, `Controllers/Api/AiChatController.cs` |
| Menu / giao diện chung | `Views/Shared/_Layout.cshtml` |
| Tài khoản admin, danh mục mẫu | `Data/Seed/DbSeeder.cs` |

## 4. Thêm một chức năng mới (làm theo thứ tự)

Ví dụ: Seller đăng sản phẩm.

1. **Entity / Enum**: đã có sẵn trong `Models/`. Cần bảng mới thì thêm file + 1 dòng `DbSet`, rồi `Add-Migration <Tên>`.
2. **ViewModel**: tạo form dữ liệu trong `Models/ViewModels/Products/` (VD `ProductCreateVm`).
3. **Service**: khai báo hàm trong `IProductService`, cài đặt trong `ProductService`.
4. **Controller**: thêm action trong `Areas/Seller/Controllers/ProductsController.cs`. Controller chỉ nhận request, gọi service, trả View. **Không viết logic và không dùng `AppDbContext` trong controller.**
5. **View**: tạo `Areas/Seller/Views/Products/<Action>.cshtml`.
6. Service mới (nếu có) thì đăng ký trong `AddAppServices`.

## 5. Quy ước chung

- Tên file = tên class. Một class một file.
- Dùng `AppRoles.Admin`, `OrderStatus.Pending`... thay vì gõ chuỗi.
- Lấy Id người dùng: `User.GetUserId()`.
- Mọi hành động quan trọng của Admin phải gọi `IAuditLogService.LogAsync(...)`.
- Mỗi thay đổi trạng thái đơn phải đúng luồng: Pending → Confirmed → Shipping → Delivered → Completed (nhánh Cancelled chỉ khi Pending / Confirmed).
- Thông báo thành công / lỗi: `TempData["Success"]` / `TempData["Error"]` (layout đã tự hiển thị).

## 6. Làm việc nhóm với Git

- Mỗi người nhận một **module** và chỉ sửa trong thư mục của module đó (controller + view + service) nên ít đụng nhau.
- Các file dùng chung, dễ xung đột, **thông báo nhóm trước khi sửa**: `AppDbContext.cs`, `ServiceCollectionExtensions.cs`, `_Layout.cshtml`, `AppEnums.cs`.
- Mỗi người một nhánh (`feature/cart`, `feature/auction`...). `pull` nhánh chính trước khi tạo migration.
- Ai đổi entity thì tự tạo migration và commit cùng code. **Không sửa migration đã push**, cần đổi thì tạo migration mới.

## 7. Gợi ý chia module (4-5 người)

| Người | Module |
|---|---|
| A | Account, Profile, Seller upgrade, Admin Users / Sellers, Audit Log |
| B | Products, Categories, Favorites, tìm kiếm, Seller Products, Admin duyệt sản phẩm |
| C | Cart, Orders (Buyer + Seller + Admin), Disputes |
| D | Auctions, Bids, AuctionJob, SignalR |
| E | Notifications, Chat, Reviews, Reports, Dashboard, Banners, Chatbox AI |

## 8. Đã làm sẵn và còn để trống

**Đã chạy được (viết sẵn):** đăng ký / đăng nhập / phân quyền 3 role, seed admin, danh sách + tìm kiếm + chi tiết sản phẩm, đặt giá có chống trùng, mở / đóng phiên đấu giá tự động, thông báo (lưu DB + realtime), Audit Log service, layout theo role.

**Còn TODO (file đã tạo sẵn, có chú thích `TODO`):** giỏ hàng, đơn hàng, yêu thích, đánh giá, báo cáo, tranh chấp, quản lý danh mục / banner / user, dashboard, chat UI, AI chatbox, upload ảnh, các trang Seller / Admin.
