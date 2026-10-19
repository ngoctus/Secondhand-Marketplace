# Secondhand Marketplace

## 1. Giới thiệu dự án

Secondhand Marketplace là hệ thống thương mại điện tử dành cho các sản phẩm đã qua sử dụng, được xây dựng bằng ASP.NET Core MVC.

Hệ thống cho phép người dùng đăng bán sản phẩm, tìm kiếm và mua hàng, quản lý đơn hàng, tham gia đấu giá, trò chuyện với người dùng khác và sử dụng các chức năng hỗ trợ từ AI.

### Công nghệ sử dụng

- **Backend:** ASP.NET Core MVC (.NET 10).
- **Ngôn ngữ:** C#.
- **Database:** SQL Server.
- **ORM:** Entity Framework Core.
- **Realtime:** SignalR.
- **Frontend:** Razor Views, HTML, CSS, JavaScript.
- **AI:** Tích hợp API AI khi triển khai chức năng chatbot.

## 2. Cấu trúc thư mục Backend

```text
Web2Hand/
├── Program.cs
├── Common/
│   ├── Constants/
│   │   └── AppRoles.cs
│   └── Extensions/
├── Models/
│   ├── Entities/
│   ├── Enums/
│   │   └── AppEnums.cs
│   └── ViewModels/
│       ├── Products/
│       ├── Orders/
│       ├── Auctions/
│       └── ...
├── Data/
│   ├── AppDbContext.cs
│   ├── Configurations/
│   └── Seed/
│       └── DbSeeder.cs
├── Services/
│   ├── Products/
│   ├── Orders/
│   ├── Auctions/
│   └── ...
├── Controllers/
│   ├── HomeController.cs
│   ├── ProductsController.cs
│   ├── AccountController.cs
│   ├── ProfileController.cs
│   ├── NotificationsController.cs
│   ├── ChatController.cs
│   └── Api/
├── Areas/
│   ├── User/
│   │   ├── Controllers/
│   │   └── Views/
│   └── Admin/
│       ├── Controllers/
│       └── Views/
├── Hubs/
│   ├── AuctionHub.cs
│   ├── ChatHub.cs
│   └── NotificationHub.cs
├── BackgroundJobs/
│   └── AuctionJob.cs
├── Views/
│   └── Shared/
│       └── _Layout.cshtml
└── wwwroot/
    ├── css/
    ├── js/
    └── uploads/
```

### Quy ước tổ chức thư mục

- **Program.cs:** Khởi tạo ứng dụng, đăng ký middleware, cấu hình định tuyến và SignalR Hub.
- **Common/:** Chứa các hằng số dùng chung, tên role và extension methods.
- **Models/Entities/:** Mỗi entity tương ứng với một bảng dữ liệu chính.
- **Models/Enums/:** Tập trung các enum và trạng thái dùng chung, ví dụ `OrderStatus`, `ProductStatus`.
- **Models/ViewModels/:** Chứa dữ liệu đầu vào của form và dữ liệu phục vụ hiển thị.
- **Data/AppDbContext.cs:** Khai báo `DbSet` và quản lý kết nối dữ liệu thông qua Entity Framework Core.
- **Data/Configurations/:** Cấu hình khóa chính, khóa ngoại, kiểu dữ liệu, độ dài và các ràng buộc của entity.
- **Data/Seed/:** Khởi tạo dữ liệu ban đầu như role, tài khoản Admin và danh mục mẫu.
- **Services/:** Chứa logic nghiệp vụ. Mỗi chức năng tổ chức theo cặp interface `IXxxService` và implementation `XxxService`.
- **Controllers/:** Tiếp nhận request, kiểm tra dữ liệu đầu vào, gọi service và trả về View hoặc kết quả phù hợp.
- **Areas/User/:** Chứa giao diện và controller dành cho người dùng thực hiện các chức năng mua hàng, bán hàng và quản lý hoạt động cá nhân.
- **Areas/Admin/:** Chứa giao diện và controller dành riêng cho quản trị viên.
- **Hubs/:** Xử lý giao tiếp thời gian thực bằng SignalR.
- **BackgroundJobs/:** Chứa các tác vụ nền, ví dụ tự động mở và đóng phiên đấu giá.
- **Views/Shared/:** Chứa layout, partial view và giao diện dùng chung.
- **wwwroot/:** Chứa CSS, JavaScript, hình ảnh và các tài nguyên tĩnh.

## 3. Quy ước phân quyền

Hệ thống sử dụng hai role chính:

| Role | Phạm vi chức năng |
|---|---|
| `User` | Tìm kiếm, mua hàng, đăng bán sản phẩm, quản lý đơn mua và đơn bán, đấu giá, yêu thích, đánh giá, chat và các chức năng dành cho người dùng. |
| `Admin` | Quản lý người dùng, danh mục, banner, báo cáo, tranh chấp và các chức năng quản trị hệ thống. |

Một tài khoản `User` có thể đồng thời là người mua và người bán. Không tạo role riêng `Buyer` hoặc `Seller`.

Việc xác định ai là người mua, người bán hoặc chủ sở hữu sản phẩm được thực hiện dựa trên dữ liệu nghiệp vụ, chẳng hạn `BuyerId`, `SellerId` và quyền sở hữu bản ghi.

Các chức năng yêu cầu đăng nhập phải được bảo vệ bằng cơ chế xác thực và phân quyền phù hợp. Những thao tác sửa, xóa hoặc quản lý dữ liệu phải kiểm tra quyền sở hữu ở phía Backend.

## 4. Quy trình phát triển một chức năng mới

Ví dụ: chức năng đăng bán sản phẩm.

### Bước 1. Entity và Enum

Kiểm tra các entity và enum hiện có trong `Models/`.

Nếu cần tạo bảng mới:

1. Tạo entity tương ứng trong `Models/Entities/`.
2. Khai báo `DbSet` trong `Data/AppDbContext.cs`.
3. Bổ sung cấu hình trong `Data/Configurations/` nếu cần.
4. Tạo migration và cập nhật database.

```powershell
Add-Migration TenMigration
Update-Database
```

Chỉ tạo entity hoặc bảng mới khi nghiệp vụ thực sự cần thiết. Không tạo trùng với cấu trúc dữ liệu đã có.

### Bước 2. ViewModel

Tạo ViewModel trong thư mục tương ứng với chức năng, ví dụ:

`Models/ViewModels/Products/ProductCreateVm.cs`

ViewModel chỉ chứa các trường cần thiết cho form hoặc dữ liệu trả về giao diện. Không nhận trực tiếp entity database làm dữ liệu đầu vào nếu việc đó có thể cho phép người dùng gửi các trường nhạy cảm.

### Bước 3. Service

Khai báo interface trong `Services/Products/IProductService.cs` và triển khai trong `Services/Products/ProductService.cs`.

Service chịu trách nhiệm xử lý logic nghiệp vụ, truy vấn dữ liệu, kiểm tra điều kiện, lưu thay đổi và trả kết quả cho Controller.

### Bước 4. Đăng ký Service

Đăng ký service trong phương thức `AddAppServices` thuộc `Common/Extensions/` theo quy ước của dự án.

```csharp
services.AddScoped<IProductService, ProductService>();
```

Không đăng ký trùng service nếu đã có khai báo tương ứng.

### Bước 5. Controller

Tạo hoặc bổ sung action trong controller phù hợp thuộc `Areas/User/Controllers/`.

Controller chỉ tiếp nhận request, kiểm tra dữ liệu đầu vào ở mức cần thiết, gọi service và trả về View hoặc kết quả phù hợp.

**Không viết logic nghiệp vụ trực tiếp trong Controller. Không truy cập `AppDbContext` trực tiếp từ Controller.**

### Bước 6. View

Tạo giao diện tại thư mục tương ứng, ví dụ:

`Areas/User/Views/Products/Create.cshtml`

View sử dụng ViewModel để hiển thị dữ liệu và gửi thông tin đến Controller. Giao diện phải thống nhất với thiết kế hiện có của dự án.

### Bước 7. Kiểm tra chức năng

Trước khi hoàn thành, cần kiểm tra:

- Dữ liệu hợp lệ và dữ liệu không hợp lệ.
- Trường hợp chưa đăng nhập hoặc không đủ quyền.
- Quyền sở hữu dữ liệu.
- Lưu, sửa, xóa và truy vấn database.
- Hiển thị thông báo thành công hoặc lỗi.
- Các chức năng liên quan không bị ảnh hưởng.

## 5. Quy ước code chung

1. **Đặt tên:** Sử dụng PascalCase cho class, method và property; camelCase cho biến cục bộ và tham số.
2. **Ngôn ngữ:** Tên class, method, property và biến dùng tiếng Anh; nội dung giao diện có thể dùng tiếng Việt.
3. **Service:** Mỗi service phụ trách một nhóm nghiệp vụ rõ ràng; interface và implementation phải thống nhất.
4. **Controller:** Không chứa logic nghiệp vụ phức tạp và không truy cập database trực tiếp.
5. **ViewModel:** Dùng cho dữ liệu trao đổi giữa giao diện và Controller; không tùy tiện sử dụng entity thay thế.
6. **Database:** Mọi thay đổi schema phải được quản lý bằng migration; không tự ý sửa cấu trúc làm ảnh hưởng các thành viên khác.
7. **Phân quyền:** Không chỉ ẩn nút trên giao diện; Backend phải kiểm tra quyền thực tế.
8. **Xử lý lỗi:** Không để lộ mật khẩu, connection string, API key hoặc thông tin nhạy cảm trong thông báo lỗi.
9. **Bất đồng bộ:** Ưu tiên các phương thức async phù hợp như `ToListAsync()`, `SaveChangesAsync()` và `Task<IActionResult>`.
10. **Tái sử dụng:** Kiểm tra code hiện có trước khi tạo class, service, ViewModel hoặc chức năng mới.

## 6. Trạng thái triển khai

### Các chức năng đã được triển khai

Theo trạng thái dự án hiện tại:

- Đăng ký, đăng nhập và phân quyền ba role ban đầu.
- Khởi tạo tài khoản Admin.
- Danh sách, tìm kiếm và xem chi tiết sản phẩm.
- Đặt giá đấu, có kiểm tra trùng lặp theo logic hiện có.
- Tự động mở và đóng phiên đấu giá.
- Thông báo được lưu vào database và hỗ trợ cập nhật thời gian thực.
- Audit Log Service.
- Layout giao diện theo role.

**Lưu ý:** Do hệ thống đã thống nhất chuyển sang hai role `User` và `Admin`, cần kiểm tra và cập nhật phần đăng ký, đăng nhập, dữ liệu seed và phân quyền từ ba role ban đầu sang cấu trúc mới.

### Các chức năng còn TODO

Các chức năng sau đã có file hoặc cấu trúc ban đầu nhưng cần tiếp tục kiểm tra và hoàn thiện theo nghiệp vụ:

- Giỏ hàng.
- Đơn hàng.
- Danh sách yêu thích.
- Đánh giá sản phẩm.
- Báo cáo.
- Tranh chấp.
- Quản lý danh mục và banner.
- Quản lý người dùng.
- Dashboard.
- Giao diện chat.
- Chatbot AI.
- Upload và quản lý hình ảnh.
- Các trang quản lý nghiệp vụ dành cho User và Admin.

Chỉ đánh dấu chức năng hoàn thành sau khi đã kiểm tra logic Backend, database, phân quyền và giao diện tương ứng.

## 7. Nguyên tắc làm việc nhóm

- Đọc cấu trúc dự án và kiểm tra code hiện có trước khi triển khai.
- Không tự ý đổi kiến trúc, tên bảng, tên cột, API hoặc quy ước chung khi chưa thống nhất với nhóm.
- Không tạo thêm role `Buyer` hoặc `Seller`; sử dụng `User` cho cả nghiệp vụ mua và bán.
- Khi chỉnh sửa chức năng có sẵn, ưu tiên thay đổi tối thiểu và giữ tương thích với các chức năng liên quan.
- Nếu thay đổi database hoặc API Contract, cần thông báo cho các thành viên phụ trách những phần bị ảnh hưởng.
- Mỗi chức năng phải có đầy đủ các thành phần cần thiết, được kiểm tra trước khi bàn giao.
- Không tự ý thêm chức năng ngoài phạm vi yêu cầu.
- Khi gặp lỗi, xác định nguyên nhân và sửa đúng phạm vi thay vì viết lại toàn bộ phần code liên quan.

**Mục tiêu chung:** Xây dựng Secondhand Marketplace theo kiến trúc thống nhất, dễ bảo trì, dễ mở rộng và giúp các thành viên có thể phát triển các chức năng độc lập mà không gây xung đột với phần còn lại của hệ thống.