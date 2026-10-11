# khung xương Backend

## 1. Sơ đồ thư mục

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

## 2. Thêm một chức năng mới (làm theo thứ tự)

Ví dụ: Seller đăng sản phẩm.

1. **Entity / Enum**: đã có sẵn trong `Models/`. Cần bảng mới thì thêm file + 1 dòng `DbSet`, rồi `Add-Migration <Tên>`.
2. **ViewModel**: tạo form dữ liệu trong `Models/ViewModels/Products/` (VD `ProductCreateVm`).
3. **Service**: khai báo hàm trong `IProductService`, cài đặt trong `ProductService`.
4. **Controller**: thêm action trong `Areas/Seller/Controllers/ProductsController.cs`. Controller chỉ nhận request, gọi service, trả View. **Không viết logic và không dùng `AppDbContext` trong controller.**
5. **View**: tạo `Areas/Seller/Views/Products/<Action>.cshtml`.
6. Service mới (nếu có) thì đăng ký trong `AddAppServices`.

**Đã chạy được (viết sẵn):** đăng ký / đăng nhập / phân quyền 3 role, seed admin, danh sách + tìm kiếm + chi tiết sản phẩm, đặt giá có chống trùng, mở / đóng phiên đấu giá tự động, thông báo (lưu DB + realtime), Audit Log service, layout theo role.

**Còn TODO (file đã tạo sẵn, có chú thích `TODO`):** giỏ hàng, đơn hàng, yêu thích, đánh giá, báo cáo, tranh chấp, quản lý danh mục / banner / user, dashboard, chat UI, AI chatbox, upload ảnh, các trang Seller / Admin.
