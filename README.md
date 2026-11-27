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

`database/run-db.bat` chạy bộ bootstrap theo đúng thứ tự. Các file hiện có gồm `01_create_database.sql`–`13_seed_demo.sql`; trong đó `02`–`03` tạo schema/index, `04`–`10` tạo function/view/trigger/procedure, `11` mở rộng, `12` seed hệ thống và `13` chỉ seed dữ liệu demo. Tên file cũ trong tài liệu trước đây không còn áp dụng.

`run-db.bat` chỉ tạo database mới và sẽ từ chối khi database đã tồn tại. Để nâng cấp database có dữ liệu, hãy backup rồi áp dụng migration đã rà soát trong `database/migrations`; tuyệt đối không chạy `reset-db.bat` trên production.

## 3. Cấu hình và chạy ứng dụng

Không commit connection string riêng của máy. Tạo cấu hình local:

```bat
cd src\SecondHandMarketplace.Web
copy appsettings.Example.json appsettings.Development.json
```

Sửa `ConnectionStrings:DefaultConnection` trong `appsettings.Development.json` cho đúng instance SQL Server của bạn. Đặt `Jwt:Key` thành secret ngẫu nhiên tối thiểu 32 byte trong cấu hình Development/User Secrets hoặc biến môi trường; không commit secret thật.

### Gửi OTP đặt lại mật khẩu bằng Gmail

Ứng dụng gửi OTP qua Gmail SMTP (`smtp.gmail.com:587`, TLS). Cấu hình thông tin đăng nhập bằng User Secrets trong thư mục `src\SecondHandMarketplace.Web`:

```bat
dotnet user-secrets set "Email:Smtp:Username" "địa_chỉ_gmail_của_bạn@gmail.com"
dotnet user-secrets set "Email:Smtp:Password" "mật_khẩu_ứng_dụng_16_ký_tự"
```

`Password` phải là **Mật khẩu ứng dụng Google**, không phải mật khẩu đăng nhập Gmail. Google yêu cầu bật Xác minh 2 bước trước khi tạo mật khẩu ứng dụng. Tạo tại [Google Account App Passwords](https://myaccount.google.com/apppasswords). Sau khi đặt secrets, khởi động lại website, vào **Quên mật khẩu**, nhập Gmail thuộc tài khoản đã đăng ký và gửi mã. OTP có hiệu lực 10 phút; giới hạn 5 lần nhập sai. Google có thể thu hồi mật khẩu ứng dụng khi mật khẩu tài khoản Google thay đổi.

Không đưa `Email:Smtp:Password` vào `appsettings.json`, Git hoặc ảnh chụp màn hình. User Secrets chỉ dùng cho máy phát triển; khi triển khai, cấu hình cùng khóa bằng secret manager/biến môi trường.

Chạy ứng dụng:

```bat
dotnet restore
dotnet run
```

Ứng dụng cung cấp `/health/live` để kiểm tra tiến trình và `/health/ready` để kiểm tra SQL Server, schema/migration bắt buộc cùng Redis/object storage nếu đã cấu hình. Redis chia sẻ trạng thái giữa các host và cấp lease để chỉ một host chạy mỗi tác vụ nền; nếu không cấu hình Redis, chỉ chạy một replica. Mặc định ảnh dùng filesystem local; triển khai nhiều host có thể đặt `STORAGE_PROVIDER=S3` với bucket S3-compatible và CDN tùy chọn. Khi khóa Data Protection chưa lưu trên Redis, cần backup và giữ volume `data_protection_keys` khi thay container để cookie/phiên đã phát hành tiếp tục dùng được. Xem [hướng dẫn triển khai](docs/deployment-runbook.md) trước khi chuyển ảnh cũ sang bucket.

### Thanh toán và vận chuyển

Giai đoạn hiện tại hỗ trợ COD và chuyển khoản VietQR trực tiếp đến shop; shop xác nhận đã nhận tiền thủ công. Trạng thái giao hàng và phí vận chuyển cũng do shop quản lý thủ công. Ứng dụng không giữ tiền, không tự xác minh giao dịch ngân hàng và chưa kết nối API hãng vận chuyển.

- `Auction:SweepSeconds` (mặc định 60): chu kỳ (giây) service nền quét và chốt các phiên đấu giá hết giờ. Tối thiểu 10.

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
