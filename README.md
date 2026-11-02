# Secondhand Marketplace

Website mua bán đồ đã qua sử dụng. Công nghệ: ASP.NET Core MVC (.NET 10), SQL Server, Entity Framework Core theo hướng data-first.

## Quy ước kiến trúc

- Namespace gốc: `SecondHandMarketplace.Web`.
- MVC dùng Cookie Authentication; API `/api/...` dùng JWT khi endpoint chỉ định `JwtBearerDefaults.AuthenticationScheme`.
- Mật khẩu được băm/kiểm tra bằng BCrypt; không dùng ASP.NET Identity.
- Chỉ có hai vai trò `USER` và `ADMIN`. Một tài khoản USER có thể vừa mua vừa bán và được tạo tối đa một cửa hàng.
- SQL Server là nguồn schema chính; Entity và `AppDbContext` ánh xạ tên bảng/cột tiếng Việt.
- Giao diện dùng Razor `.cshtml`; CSS/JS đặt trong `wwwroot`.

## 1. Yêu cầu

- .NET 10 SDK.
- SQL Server Developer hoặc Express.
- `sqlcmd` trong PATH.
- Visual Studio hỗ trợ .NET 10 hoặc VS Code + C# Dev Kit.

## 2. Tạo database

Mở terminal tại thư mục `database` và chạy:

```bat
run-db.bat
```

Mặc định dùng SQL Server `localhost` với Windows Authentication. Với SQL Server Express, đặt `SQL_SERVER=.\SQLEXPRESS` trước khi chạy. Có thể cấu hình `SQL_AUTH` theo hướng dẫn trong `database/run-db.bat`.

`reset-db.bat` xóa database hiện tại và toàn bộ dữ liệu trước khi chạy lại. Chỉ sử dụng khi muốn tạo database mới hoàn toàn.

### Các file SQL

- `01`–`09`: database lõi.
- `10_banner_yeuthich.sql`: Banner và Yêu thích – người B phụ trách.
- `11_danhgia.sql`: Đánh giá – người C phụ trách.
- `12_tranhchap_baocao.sql`: Tranh chấp và Báo cáo – người D phụ trách.
- `13_daugia.sql`: Đấu giá – người E phụ trách.

Các file `10`–`13` được tạo sẵn với `USE SecondHandMarketplace; GO`; từng thành viên bổ sung SQL vào file được giao. Không thêm các bảng này vào `02_create_tables.sql`.

Lưu ý: `07_functions.sql` chạy trước `06_procedures.sql` vì các stored procedure sử dụng function trong file 07.

## 3. Cấu hình và chạy ứng dụng

Không commit connection string riêng của máy. Tạo cấu hình local:

```bat
cd src\SecondHandMarketplace.Web
copy appsettings.Example.json appsettings.Development.json
```

Sửa `ConnectionStrings:DefaultConnection` trong `appsettings.Development.json` cho đúng instance SQL Server của bạn. Đặt `Jwt:Key` thành secret ngẫu nhiên tối thiểu 32 byte trong cấu hình Development/User Secrets hoặc biến môi trường; không commit secret thật.

Chạy ứng dụng:

```bat
dotnet restore
dotnet run
```

Hoặc mở `Secondhand-Marketplace.sln` bằng Visual Studio và chạy project `SecondHandMarketplace.Web`.

## 4. Tài khoản seed

Sau khi chạy `database/run-db.bat`:

- Admin: `admin@secondhand.local`
- Mật khẩu seed local: `Admin@12345`
- Một số USER mẫu: `an.seller@demo.local`, `binh.seller@demo.local`, `chi.buyer@demo.local`, `dung.buyer@demo.local`.
- Mật khẩu của các tài khoản seed local dùng chung `Admin@12345`.

Đây chỉ là tài khoản demo phục vụ phát triển/kiểm thử. Không dùng mật khẩu này trong môi trường thật; đổi mật khẩu ngay nếu cần giữ lại database demo.

## 5. Chức năng thuộc Người A

- Đăng ký/đăng nhập Cookie và API login JWT dùng chung `IAuthService`.
- Hồ sơ cá nhân và đổi mật khẩu.
- Admin quản lý người dùng, khóa/mở khóa có lý do; không cho khóa ADMIN.
- USER tạo/cập nhật cửa hàng; Admin xem, khóa/mở khóa cửa hàng.
- Admin CRUD danh mục và tình trạng sản phẩm; danh mục/tình trạng đang được sản phẩm sử dụng sẽ được ẩn thay vì xóa.
- Nhật ký hệ thống được ghi qua `dbo.sp_GhiNhatKy` và có lọc theo người dùng, hành động, ngày, phân trang.

## 6. Scaffold Entity (khi schema lõi thay đổi)

Cài công cụ một lần:

```bat
dotnet tool install --global dotnet-ef
```

Trong thư mục `src\SecondHandMarketplace.Web`:

```bat
dotnet ef dbcontext scaffold "Name=ConnectionStrings:DefaultConnection" Microsoft.EntityFrameworkCore.SqlServer --context AppDbContext --context-dir Data --output-dir Models/Entities --namespace SecondHandMarketplace.Web.Models.Entities --context-namespace SecondHandMarketplace.Web.Data --no-onconfiguring --force
```

Lưu ý: `--force` ghi đè Entity và `AppDbContext.cs`. Trước khi scaffold, commit/backup thay đổi và kiểm tra lại cấu hình quan hệ/trigger trong `AppDbContext` sau khi scaffold.

## 7. Git

- Mỗi thành viên làm trên nhánh riêng, tạo Pull Request vào nhánh chung; không push trực tiếp.
- Chỉ stage file thuộc phần việc của mình.
- Không commit `appsettings.Development.json`, API key, mật khẩu, `bin/`, `obj/`, `.vs/` hoặc file upload local.
