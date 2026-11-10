# Báo cáo đánh giá mức độ sẵn sàng Docker — Secondhand Marketplace

> **Thời điểm khảo sát:** 07/11/2026  
> **Phạm vi:** chỉ đọc source code, file cấu hình, script SQL và tài liệu. Không tạo Dockerfile/compose/.dockerignore, không chạy `docker init`, không build image, không chạy container, không thay đổi database hay source code.  
> **Kết luận nhanh:** Source code **có nền tảng kỹ thuật phù hợp để chạy trên Linux container**, nhưng project **chưa sẵn sàng để triển khai Docker ngay**. Có 3 vấn đề mức **Blocker** và nhiều vấn đề **High** về cấu hình, khởi tạo database, lưu trữ và bảo mật cần xử lý trước.

---

## 1. Tóm tắt điều hành

### 1.1. Những điểm tích cực

- Project dùng `net10.0`, ASP.NET Core MVC, EF Core SQL Server; các package chính đều chạy được trên Linux.
- Không thấy mã C# phụ thuộc Windows API, registry, `System.Drawing`, đường dẫn ổ đĩa Windows hay thư viện native chỉ có trên Windows.
- `AppDbContext` là Database First, không dùng Migration/`EnsureCreated`; phù hợp với định hướng database của nhóm.
- Các script SQL là T-SQL thuần, không dùng `xp_cmdshell`, `BULK INSERT`, CLR, linked server, backup path Windows hay tính năng chỉ có trên SQL Server Windows.
- Các tính năng SQL Server cần dùng (`CREATE OR ALTER`, trigger, view, stored procedure, function, `ISJSON`, `SYSUTCDATETIME`) đều có trên SQL Server Linux container đời mới.
- Static asset (Bootstrap, jQuery, CSS/JS) đã được vendor trong `wwwroot/lib`; không cần Node/npm khi build image.
- Ứng dụng dùng UTC (`DateTime.UtcNow`, `SYSUTCDATETIME`) khá nhất quán; không thấy phụ thuộc múi giờ Windows trong luồng chính.
- Docker Desktop hiện có 16 CPU, khoảng 7,5 GiB RAM, kiến trúc x86_64 — đủ để chạy thử SQL Server 2022 container và web container ở môi trường phát triển.

### 1.2. Những điểm chặn chính

1. **Connection string hiện tại chỉ dùng được trên Windows**: `Server=.\SQLEXPRESS;Trusted_Connection=True`. Linux container không dùng Windows Authentication và không phân giải được named instance `.\SQLEXPRESS`.
2. **JWT key đang là placeholder đã commit**, dài hơn 32 byte nên ứng dụng vẫn khởi động và chấp nhận token ký bằng khóa công khai này.
3. **Chưa có cơ chế khởi tạo database an toàn, lặp lại được**: không có Migration; `01_create_database.sql` xóa database; `run-db.bat` là batch Windows và bỏ sót các file `15`–`18`, `20`.

Ngoài ra, các vấn đề **High** cần xử lý trước khi coi là “deploy được”:

- Chưa có `.dockerignore` (build context có thể chứa `bin/obj/.vs/.git`, file upload local, `appsettings.Development.json`, `.env`).
- Chưa cấu hình Data Protection key persistence (mất session/antiforgery khi container bị tạo lại).
- Chưa cấu hình Forwarded Headers và cookie `Secure` cho môi trường sau reverse proxy.
- Chưa có volume cho SQL Server và chưa có phương án backup.
- Chưa có upload thực tế; `Storage:RootPath` đang không được code nào đọc.
- Seed data chứa tài khoản admin/demo với mật khẩu công khai.

---

## 2. Phương pháp và giới hạn

### 2.1. Đã kiểm tra

- Cấu trúc solution, `.csproj`, `Program.cs`, `ServiceCollectionExtensions.cs`, `AppDbContext.cs`.
- Toàn bộ entity, service, controller, view có liên quan đến cấu hình, authentication, database, upload.
- `appsettings.json`, `appsettings.Example.json`, `launchSettings.json`, `.gitignore`.
- Toàn bộ script trong `database/`, `run-db.bat`, `reset-db.bat`, `database/README.md`.
- Tài liệu `README.md`, `docs/PROJECT-STRUCTURE-GUIDE.md`, `docs/jwt-api.md`, `docs/c/...`.
- Kiểm tra read-only `dotnet --info` và `git status`; không chạy build, test, SQL hay lệnh Docker.

### 2.2. Chưa kiểm tra / chưa xác minh được

Các mục sau **chưa xác minh được** trong phạm vi khảo sát, cần kiểm tra ở giai đoạn tiếp theo:

- Phiên bản, edition và collation thực tế của SQL Server đang dùng trên máy phát triển.
- Database hiện tại đã chạy đủ các file `15`–`18`, `20` hay chưa (script có, nhưng `run-db.bat` không chạy các file này).
- Trạng thái Docker Engine thực tế; báo cáo dùng thông tin do người dùng cung cấp, không chạy lệnh Docker.
- Khả năng build/publish thực tế trên Linux container (chưa chạy `dotnet build`/`dotnet publish` trong Linux).
- Các file ảnh seed `/uploads/seed/*.jpg` có tồn tại ngoài repository hay không (trong repo chỉ có `.gitkeep`).
- Đường dẫn mặc định chính xác của Data Protection key ring trong image `aspnet:10.0` (thường là `$HOME/.aspnet/DataProtection-Keys`, cần xác minh bằng container thật).
- Hành vi của `MapStaticAssets` với file được ghi vào `wwwroot` **sau khi ứng dụng khởi động** (cần kiểm thử khi triển khai upload).
- SQL Server Agent/backup tự động có sẵn trong image container hay không.
- Domain, reverse proxy, chứng chỉ TLS và nhà cung cấp VPS thực tế.

---

## 3. Tổng quan kiến trúc hiện tại

### 3.1. Công nghệ và cấu trúc

| Thành phần | Thực tế trong source |
|---|---|
| Runtime | ASP.NET Core MVC, `TargetFramework` = `net10.0` (`SecondHandMarketplace.Web.csproj:4`) |
| ORM | EF Core 10.0.14, provider `Microsoft.EntityFrameworkCore.SqlServer` 10.0.14 |
| Database | SQL Server; hướng Database First; schema do các file SQL quản lý; không có Migration |
| Authentication | Cookie cho MVC; JWT Bearer cho API; BCrypt cho mật khẩu |
| Background | `AuctionBackgroundService` quét phiên đấu giá định kỳ |
| AI | Gọi Anthropic Messages API qua `HttpClient`; cấu hình runtime trong bảng `CauHinhAI` |
| Static files | Razor view, Bootstrap/jQuery vendored trong `wwwroot/lib`; `MapStaticAssets()` |
| Test | xUnit + EF Core InMemory; không cần SQL Server để chạy unit test |

### 3.2. Luồng khởi động và kết nối

```text
Program.cs
  ├─ AddControllersWithViews()
  ├─ AddDatabase(configuration)
  │    └─ AddDbContext<AppDbContext>(UseSqlServer(ConnectionStrings:DefaultConnection))
  ├─ AddAppServices()
  │    ├─ các service scoped
  │    ├─ AddAiChat() (typed HttpClient)
  │    └─ AuctionBackgroundService (hosted service)
  ├─ AddSingleton<IMatKhauService, MatKhauService>()
  ├─ AddSingleton<JwtTokenService>()
  ├─ đọc Jwt:Key, ném lỗi nếu thiếu hoặc < 32 byte
  ├─ AddAuthentication(Cookie + JwtBearer)
  └─ Build + middleware pipeline
```

Ứng dụng **không tự kết nối database lúc khởi động**; `AddDbContext` chỉ đăng ký service. Vì vậy ứng dụng có thể khởi động khi database chưa sẵn sàng, nhưng request đầu tiên và `AuctionBackgroundService` sẽ lỗi nếu database chưa có schema.

### 3.3. Database và EF Core

- `AppDbContext` có 22 `DbSet`, ánh xạ tên bảng/cột tiếng Việt.
- Các quan hệ chính dùng `DeleteBehavior.Restrict`.
- Các bảng có trigger được khai báo `HasTrigger(...)` để EF không dùng `OUTPUT` khi INSERT:
  - `SanPham` → `trg_SanPham_ChanRutKhoiDauGia`
  - `AnhSanPham` → `trg_AnhSanPham_GioiHan`
  - `ChiTietDonHang` → `trg_ChiTietDonHang_KiemTra`
  - `LuotKiemDuyet` → `trg_LuotKiemDuyet_KiemTra`
- Không có `Database.Migrate()`, `Database.EnsureCreated()` hay thư mục `Migrations/`.
- Schema thực tế nằm trong `database/*.sql`; đây là nguồn chuẩn theo định hướng Database First.

### 3.4. Cấu hình hiện tại

`appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=.\\SQLEXPRESS;Database=SecondHandMarketplace;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
},
"Storage": { "RootPath": "wwwroot/uploads" },
"Jwt": {
  "Issuer": "SecondHandMarketplace",
  "Audience": "SecondHandMarketplace.Client",
  "Key": "CHANGE_THIS_DEVELOPMENT_KEY_TO_A_RANDOM_32_BYTE_MINIMUM_SECRET",
  "ExpiresMinutes": 60
}
```

- `Storage:RootPath` **không được đọc ở bất kỳ file C#/Razor nào** trong source hiện tại.
- `Ai` section không có trong `appsettings.json`; API key AI phải đặt qua User Secrets hoặc biến môi trường `Ai__ApiKey`.
- `Jwt:Key` là placeholder, không phải secret thật, nhưng vẫn đủ 32 byte nên ứng dụng không chặn khi khởi động.

---

## 4. Đánh giá chi tiết và danh sách phát hiện

### 4.1. Nhóm A — Ứng dụng ASP.NET Core

| ID | Mức độ | File / vị trí | Nguyên nhân | Ảnh hưởng khi triển khai Docker | Hướng xử lý đề xuất |
|---|---|---|---|---|---|
| **A-01** | **Low** | `Program.cs:88`; `Properties/launchSettings.json:7` | `UseHttpsRedirection()` luôn được đăng ký, trong khi container dự kiến chỉ chạy HTTP; `launchSettings.json` không được dùng trong container | Middleware có thể cảnh báo “không xác định được HTTPS port” và bỏ qua redirect; nếu cấu hình sai có thể redirect sai/loop sau proxy | Chạy HTTP nội bộ (`ASPNETCORE_HTTP_PORTS=8080`), đặt TLS ở reverse proxy; thêm `UseForwardedHeaders` trước redirect/auth; cấu hình theo môi trường |
| **A-02** | **Blocker** | `appsettings.json:18-23`; `Program.cs:25-28`; `Security/JwtTokenService.cs:12-15` | JWT key là placeholder đã commit; `Program.cs` chỉ kiểm tra độ dài ≥ 32 byte, không kiểm tra giá trị placeholder | Bất kỳ ai có source đều có thể ký JWT hợp lệ; API dùng JWT (đặc biệt `AiChatAuthorizeAttribute`) có thể bị giả mạo token | Bắt buộc đặt `Jwt__Key` từ secret ngoài repo; thêm kiểm tra fail-fast nếu key là placeholder ở Production; xoay khóa khi cần |
| **A-03** | **High** | `Program.cs:31-53`, `86-95` | Không có `UseForwardedHeaders`; cookie không đặt `SecurePolicy`; mặc định `SameAsRequest` | Sau reverse proxy TLS, ứng dụng có thể thấy scheme `http`, khiến cookie không có `Secure`; HSTS/redirect và log IP/scheme không chính xác; nguy cơ lộ cookie nếu còn HTTP | Thêm `UseForwardedHeaders` với `KnownProxies`/`KnownNetworks`; đặt `Cookie.SecurePolicy = Always` ở Production; chỉ tin proxy nội bộ; bật HSTS đúng cách |
| **A-04** | **High** | `Program.cs` (không có `AddDataProtection`); không có volume tương ứng | Data Protection dùng key ring mặc định trong filesystem container; chưa cấu hình đường dẫn bền vững | Container bị xóa/tạo lại → toàn bộ cookie đăng nhập và antiforgery token mất hiệu lực; nhiều replica không chia sẻ key | Cấu hình `PersistKeysToFileSystem` tới volume riêng; đặt `SetApplicationName` cố định; backup/restrict quyền; chia sẻ volume khi scale |
| **A-05** | **Medium** | `Program.cs` (không có health check); `Common/Extensions/ServiceCollectionExtensions.cs:30-31` | Không có endpoint health/readiness; `AddDbContext` không bật `EnableRetryOnFailure`; `depends_on` chỉ là điều kiện khởi động, không đảm bảo DB sẵn sàng | Web có thể nhận request trước khi SQL Server init xong; lỗi tạm thời khi DB khởi động chậm; khó tự động hóa restart/healthcheck | Thêm health check (`/health`) và/hoặc `EnableRetryOnFailure`; dùng SQL healthcheck + init job; app chờ DB healthy trước khi start |
| **A-06** | **Low** | `Properties/launchSettings.json:7` | Profile local dùng `https://localhost:7080;http://localhost:5080`, không áp dụng trong container | Dễ nhầm cổng khi test container; không ảnh hưởng nếu cấu hình `ASPNETCORE_HTTP_PORTS` rõ ràng | Đặt cổng container cố định (ví dụ `8080`) và map `8080:8080`; không dựa vào launchSettings |
| **A-07** | **Low** | `Program.cs:92-95` | `MapStaticAssets()` tối ưu cho asset biết trước lúc build; không có `UseStaticFiles()` | Nếu sau này upload file vào `wwwroot/uploads` lúc runtime, file mới có thể không được phục vụ | Nếu triển khai upload, thêm `UseStaticFiles()` hoặc endpoint riêng cho `/uploads`; kiểm thử file mới ngay sau upload |
| **A-08** | **Medium** | `Services/Auctions/AuctionBackgroundService.cs`; `Services/Ai/AiRequestThrottle.cs` | Background service và throttle lưu trạng thái trong bộ nhớ từng instance | Nếu scale nhiều web container: quét đấu giá trùng lặp; giới hạn AI theo user bị bypass giữa các instance | Giai đoạn đầu chạy 1 replica; nếu scale, dùng distributed lock/queue và cache phân tán (Redis) cho throttle |

**Ghi chú thêm cho A:**

- `Microsoft.AspNetCore.Identity.EntityFrameworkCore` có trong `.csproj` nhưng không dùng Identity; không phải vấn đề Docker.
- `BCrypt.Net-Next`, EF Core SQL Server, JWT Bearer đều cross-platform; không thấy package native Windows-only.
- `launchSettings.json` chỉ dùng khi chạy bằng Visual Studio/`dotnet run`, không được dùng khi chạy image.

### 4.2. Nhóm B — Database và EF Core

| ID | Mức độ | File / vị trí | Nguyên nhân | Ảnh hưởng khi triển khai Docker | Hướng xử lý đề xuất |
|---|---|---|---|---|---|
| **B-01** | **Blocker** | `appsettings.json:3`; `appsettings.Example.json:3`; `Common/Extensions/ServiceCollectionExtensions.cs:31` | Connection string dùng `Server=.\SQLEXPRESS`, `Trusted_Connection=True`; file example dùng `(localdb)\MSSQLLocalDB` | Linux container không có Windows Authentication, không có LocalDB, không phân giải named instance; ứng dụng không kết nối được SQL Server container | Dùng SQL Authentication; server là service name trong compose (ví dụ `db`), TCP `1433`; đặt connection string qua `ConnectionStrings__DefaultConnection` |
| **B-02** | **Blocker** | `database/01_create_database.sql:4-14`; `database/run-db.bat:8-9,14-28`; không có Migration | `01` luôn `DROP DATABASE` nếu tồn tại; `run-db.bat` là batch Windows, mặc định `-E` (Windows Auth), và không chạy `15`–`18`, `20`; ứng dụng không tự tạo schema | Nếu chạy init sai: mất toàn bộ dữ liệu; nếu không init: thiếu bảng/view/proc/trigger, ứng dụng lỗi; nếu dùng `run-db.bat` trên Linux: không chạy được; nếu dùng danh sách thiếu: schema không giống DB dev | Thiết kế init một lần (one-shot) an toàn; **không chạy `01` trên volume đã có dữ liệu**; ưu tiên restore `.bak` hoặc wrapper kiểm tra `DB_ID`; dùng `sqlcmd` Linux với danh sách đầy đủ; không dùng `EnsureCreated`/Migration |
| **B-03** | **High** | `database/run-db.bat:14-28`; các file `database/15`–`18`, `20` | Runner hiện chỉ chạy `01`–`14`; các file `15_kiemduyet_tukhoa.sql`, `16_luot_kiem_duyet_tu_choi_he_thong.sql`, `17_bo_chan_tu_cam_khi_gui_duyet.sql`, `18_ghi_vi_pham_low_khi_tu_choi.sql`, `20_cauhinh_ai.sql` không có trong danh sách | Database tạo mới bằng runner sẽ thiếu cột/constraint/trigger/function/bảng `CauHinhAI`; các luồng kiểm duyệt, vi phạm, AI có thể lỗi hoặc khác DB dev | Xác nhận DB dev đã chạy đủ `15`–`18`, `20`; bổ sung danh sách init Linux đầy đủ; so sánh schema (`sys.objects`, `sys.columns`, `sys.triggers`) giữa DB dev và DB container |
| **B-04** | **High** | Chưa có login ứng dụng trong script/compose | Nếu app dùng chung tài khoản `sa` để kết nối | Ứng dụng có toàn quyền database; một lỗ hổng SQL injection hoặc lộ connection string ảnh hưởng toàn bộ server | Tạo login riêng cho app, chỉ cấp quyền cần thiết (`db_datareader`, `db_datawriter`, `GRANT EXECUTE` trên schema); `sa` chỉ dùng cho init/khôi phục |
| **B-05** | **High** | Chưa có compose/volume (giai đoạn sau) | Dữ liệu SQL Server nằm trong filesystem container nếu không mount volume | `docker compose down`/tạo lại container làm mất toàn bộ database | Named volume cho `/var/opt/mssql`; volume backup riêng; kiểm thử restore định kỳ |
| **B-06** | **Medium** | `database/01_create_database.sql` (không khai báo `COLLATE`); image SQL Server chưa chọn | Database tạo theo collation mặc định của server container; có thể khác collation DB dev; phiên bản SQL Server cũng chưa được ghim | Khác biệt sắp xếp/so sánh chuỗi tiếng Việt; backup từ phiên bản mới hơn không restore được vào image cũ hơn | Chạy query read-only trên DB dev để lấy `@@VERSION` và collation; chọn image cùng hoặc mới hơn; đặt `MSSQL_COLLATION` khi tạo volume mới; ghim tag image |
| **B-07** | **Medium** | `Common/Extensions/ServiceCollectionExtensions.cs:30-31` | Không có retry khi kết nối DB; app không chờ DB | Request đầu tiên có thể lỗi nếu SQL Server chưa sẵn sàng; background service log lỗi lặp lại | Bật `EnableRetryOnFailure` (cân nhắc) hoặc chờ healthcheck/init job; đặt `start_period` đủ dài cho SQL Server |
| **B-08** | **Low** | `database/run-db.bat:31`; các file SQL dùng `GO` | Script dùng batch separator `GO` và cần UTF-8; `run-db.bat` dùng `sqlcmd -f 65001` nhưng batch không chạy trên Linux | Nếu init bằng `ExecuteSqlRaw` hoặc công cụ không hỗ trợ `GO`, script dừng/lỗi; nếu sai encoding, dữ liệu tiếng Việt bị lỗi | Dùng `sqlcmd`/`mssql-tools` trong init container với `-b -f 65001 -C`; không split script bằng EF |
| **B-09** | **Low** | Kế hoạch compose | Mapping cổng host `1433` có thể trùng SQL Server đang cài trên máy; production không nên mở 1433 | Không start được do port conflict; tăng bề mặt tấn công nếu mở public | Dev: map `127.0.0.1:14333:1433` hoặc không map; production: không publish cổng DB |
| **B-10** | **Low** | `appsettings.json:3` | `TrustServerCertificate=True` phù hợp dev với cert tự ký của container; `MultipleActiveResultSets=True` được SQL Server Linux hỗ trợ | Nếu production vẫn dùng `TrustServerCertificate=True`, không xác thực cert; nếu bỏ mà không cài CA, kết nối lỗi | Giữ `TrustServerCertificate=True` cho dev nội bộ; production dùng cert hợp lệ/CA, `Encrypt=True`; kiểm tra MARS không gây vấn đề |

**Ghi chú thêm cho B:**

- `AppDbContext` không có `OnConfiguring`; connection string lấy từ DI, nên override bằng biến môi trường hoạt động đúng.
- Không có Migration; `Microsoft.EntityFrameworkCore.Design`/`Tools` chỉ phục vụ scaffold, không tạo schema runtime.
- Các script `10_banner_yeuthich.sql` và `11_danhgia.sql` hiện chỉ có `USE SecondHandMarketplace; GO` (file rỗng), có thể chạy mà không ảnh hưởng.
- Thứ tự chạy hiện tại của `run-db.bat` là `01, 02, 03, 04, 07, 05, 06, 08, 09, 10, 11, 12, 13, 14`. Khi bổ sung, cần giữ `07` trước `06`, `17` sau `07`/`14`/`15`, `20` sau `02`/`03`.

### 4.3. Nhóm C — File upload và lưu trữ ảnh

| ID | Mức độ | File / vị trí | Nguyên nhân | Ảnh hưởng khi triển khai Docker | Hướng xử lý đề xuất |
|---|---|---|---|---|---|
| **C-01** | **High** | `appsettings.json:5-7`; `appsettings.Example.json:5-7`; `wwwroot/uploads` chỉ có `.gitkeep`; `database/09_seed_data.sql:111-122` | `Storage:RootPath` có trong cấu hình nhưng **không có code C#/Razor nào đọc**; không có `IFormFile`, `System.IO`, `File.`, `Directory.` trong source; seed data trỏ tới `/uploads/seed/*.jpg` nhưng file không có trong repo | Hiện tại ứng dụng không tạo file upload; không thể kết luận chức năng upload hoạt động trong Docker. Ảnh seed sẽ 404 nếu không có file | Làm rõ đây là chức năng chưa triển khai; nếu thuộc phạm vi, implement upload và đọc `Storage:RootPath`; bổ sung file seed hoặc mount thư mục uploads của dev |
| **C-02** | **High** | `wwwroot/uploads` (kế hoạch volume) | Filesystem container là tạm; image chạy non-root (mặc định `app` trong image .NET 8+) | Ảnh upload sẽ mất khi container bị xóa/tạo lại; lỗi permission nếu thư mục không thuộc user `app` | Named volume/bind mount cho `/app/wwwroot/uploads`; tạo thư mục và `chown` đúng user trong Dockerfile; production dùng object storage/NFS nếu scale |
| **C-03** | **High** | `Program.cs:92` | `MapStaticAssets()` được thiết kế cho asset biết trước lúc build; file được ghi vào `wwwroot` sau khi app start không nằm trong manifest | Ảnh upload mới có thể không truy cập được qua URL dù file đã nằm trên volume | Thêm `UseStaticFiles()` (hoặc endpoint vật lý) cho `/uploads`; kiểm thử upload → truy cập URL ngay sau khi upload |
| **C-04** | **Medium** | `database/09_seed_data.sql:111-122`; `.gitignore:374-382` | Seed data tạo đường dẫn `/uploads/seed/spXXX-N.jpg`; `.gitignore` bỏ qua toàn bộ file upload, repo chỉ giữ `.gitkeep` | Ảnh demo bị 404; nếu bind mount thư mục dev, ảnh có thể hiển thị nhưng không reproducible trên máy khác | Đưa ảnh seed vào một thư mục seed riêng được version hóa/mount; hoặc tạo placeholder; ghi rõ quy trình |
| **C-05** | **Low** | `SecondHandMarketplace.Web.csproj:11`; `wwwroot/uploads/.gitkeep` | Thư mục rỗng có thể không được copy vào publish image | Nếu code upload giả định thư mục tồn tại, lỗi `DirectoryNotFoundException` | Tạo thư mục trong Dockerfile hoặc lúc startup; volume mount sẽ tạo điểm mount |
| **C-06** | **Medium** | Chưa có code upload | Chưa có kiểm tra loại file, kích thước, tên file, nội dung; file nằm trong web root | Nguy cơ upload file nguy hiểm/XSS, file quá lớn, ghi đè; nếu serve trực tiếp có thể rủi ro | Validate extension/MIME/magic bytes, giới hạn kích thước, đổi tên an toàn, không cho thực thi, cân nhắc lưu ngoài web root và serve qua endpoint kiểm soát |

**Kết luận riêng cho C:** Hiện tại **không có dữ liệu ảnh do ứng dụng tạo ra** vì chưa có code upload. Vì vậy chưa thể nói “ảnh sẽ mất khi container xóa” trong trạng thái hiện tại, nhưng **ngay khi triển khai upload**, nếu không có volume và `UseStaticFiles`, dữ liệu ảnh sẽ mất/không truy cập được. Đây là vấn đề High cho giai đoạn triển khai upload, không phải lỗi đang xảy ra hôm nay.

### 4.4. Nhóm D — Cấu hình và bảo mật

| ID | Mức độ | File / vị trí | Nguyên nhân | Ảnh hưởng khi triển khai Docker | Hướng xử lý đề xuất |
|---|---|---|---|---|---|
| **D-01** | **High** | Repo root: không có `.dockerignore` | `.gitignore` không có tác dụng với Docker build context; nếu Dockerfile dùng `COPY . .`, context có thể chứa `bin/obj/.vs/.git`, `wwwroot/uploads`, `appsettings.Development.json`, `.env`, `*.user` | Build chậm, image phình to; có thể đưa secret/file local vào image; build không reproducible | Tạo `.dockerignore` loại trừ `bin`, `obj`, `.git`, `.vs`, `*.user`, `appsettings.Development.json`, `.env*`, `wwwroot/uploads`, test results; giữ `wwwroot/lib` |
| **D-02** | **High** | `.gitignore` (không có `.env`); compose dự kiến dùng `.env` | `.env` thường chứa mật khẩu SQL/JWT; hiện chưa được ignore | Có thể commit nhầm secret khi tạo compose ở giai đoạn sau | Thêm `.env`, `.env.*` vào `.gitignore`; dùng secret manager/Docker secrets cho production; không log giá trị secret |
| **D-03** | **High** | `database/09_seed_data.sql:11`, `49-61`; `README.md:69-76` | Seed data tạo admin `admin@secondhand.local` và các user demo với mật khẩu chung `Admin@12345` (hash BCrypt) | Nếu deploy nguyên seed data, người ngoài có thể đăng nhập admin bằng mật khẩu đã biết | Không chạy seed ở production; tạo admin riêng với mật khẩu mạnh; đổi/khóa tài khoản demo; tách script seed khỏi init production |
| **D-04** | **Medium** | `appsettings.json:14` (`AllowedHosts: "*"`); `Program.cs:80-84` | Cho phép mọi host; HSTS chỉ bật khi không phải Development; không cấu hình proxy | Host header poisoning; HSTS/redirect không đúng sau proxy; thông tin lỗi production có thể bị lộ nếu chạy nhầm Development | Giới hạn `AllowedHosts` theo domain; chạy `ASPNETCORE_ENVIRONMENT=Production`; cấu hình HSTS + forwarded headers đúng |
| **D-05** | **Medium** | `Program.cs:25-28`, `54-77`; `JwtTokenService.cs` | JWT dùng một khóa tĩnh; không có cơ chế xoay khóa/refresh token; secret có thể nằm trong biến môi trường | Khi xoay khóa, token cũ mất hiệu lực đột ngột; env có thể bị xem qua `docker inspect` | Dùng secret manager; lập kế hoạch xoay khóa; cân nhắc refresh token/thời hạn hợp lý; không đưa key vào image |
| **D-06** | **Medium** | `appsettings.Example.json:3`; `README.md:47-54` | Tài liệu/example dùng LocalDB và Windows Auth; chưa có mẫu cấu hình Docker | Thành viên copy example vào container sẽ lỗi kết nối | Bổ sung tài liệu/mẫu `appsettings.Docker.json` hoặc hướng dẫn biến môi trường; không dùng LocalDB trong container |
| **D-07** | **Medium** | `appsettings.json` (không có `Ai`); `docs/c/c3-chatbox-ai-integration.md:11-21` | AI key chỉ đọc từ User Secrets/env; User Secrets không hoạt động trong container Production | Nếu thiếu `Ai__ApiKey`, chat AI không hoạt động (có thể fallback); nếu đặt sai, lỗi 401/403 | Đặt `Ai__ApiKey` qua secret của môi trường; kiểm tra trạng thái AI; không commit key |
| **D-08** | **Low** | `Program.cs` (không có CORS) | API cùng origin với MVC; chưa cần CORS | Nếu tách frontend/domain khác, request API bị chặn | Chỉ thêm CORS khi có nhu cầu; cấu hình origin cụ thể, không `*` với cookie |
| **D-09** | **Low** | `Program.cs:42-45` | Cookie `SameSite=Lax`, `HttpOnly=true`, chưa đặt `SecurePolicy` | Cùng site MVC hoạt động bình thường; nhúng cross-site có thể không gửi cookie | Giữ Lax cho MVC; nếu cross-site hợp lệ, dùng `None` + `Secure` + HTTPS |
| **D-10** | **Low** | Không có cấu hình logging container | Log mặc định ra stdout; không có giới hạn dung lượng log Docker | Log container tăng vô hạn, đầy disk VPS | Cấu hình Docker log driver `max-size`/`max-file`; log tập trung; không log secret |

**Ghi chú thêm cho D:**

- `appsettings.json` không chứa mật khẩu DB thật (Windows Auth), nhưng có JWT placeholder; đây là secret giả nhưng nguy hiểm nếu dùng nguyên trạng.
- `UserSecretsId` trong `.csproj` chỉ phục vụ local Development; không thay thế biến môi trường/secret manager trong container.
- `Storage:RootPath` và `Ai:ApiKey` nếu dùng biến môi trường phải đặt đúng tên: `Storage__RootPath`, `Ai__ApiKey`.

---

## 5. Những thành phần đã sẵn sàng cho Docker

1. **Nền tảng .NET cross-platform**: `net10.0`, EF Core SQL Server, JWT Bearer, BCrypt đều có bản Linux.
2. **Không phụ thuộc Windows API trong code**: không thấy `System.Drawing`, registry, COM, `DllImport` Windows, đường dẫn ổ đĩa Windows.
3. **Cấu hình qua `IConfiguration`**: connection string, JWT, auction, AI đều đọc từ configuration; có thể override bằng biến môi trường (`ConnectionStrings__DefaultConnection`, `Jwt__Key`, ...).
4. **Database First đúng hướng**: không có Migration/`EnsureCreated`; schema do SQL script quản lý, phù hợp để init bằng SQL script hoặc restore backup.
5. **SQL Server Linux hỗ trợ các tính năng đang dùng**: trigger, view, stored procedure, function, `CREATE OR ALTER`, `ISJSON`, `SYSUTCDATETIME`.
6. **Static asset đã vendor**: Bootstrap/jQuery/CSS/JS nằm trong `wwwroot/lib`; không cần Node/npm trong build image.
7. **Unit test dùng EF InMemory**: có thể chạy test mà không cần SQL Server; hữu ích cho CI trước khi build image.
8. **Tài nguyên Docker Desktop**: 16 CPU, ~7,5 GiB RAM, x86_64 — đủ cho SQL Server 2022 container và web container ở mức dev/test.
9. **Không có Windows-only path trong code ứng dụng**: `Storage:RootPath` dùng dấu `/`; không thấy `Path.Combine` với ổ đĩa Windows.

---

## 6. Những thành phần chưa sẵn sàng hoặc cần điều chỉnh

| Nhóm | Trạng thái | Ghi chú |
|---|---|---|
| Connection string | **Chưa sẵn sàng** | Windows Auth + named instance + LocalDB; phải chuyển sang SQL Auth/TCP |
| JWT key | **Chưa sẵn sàng** | Placeholder đã commit; phải override bằng secret |
| Database initialization | **Chưa sẵn sàng** | Không có init an toàn; `01` xóa DB; runner thiếu file; chưa có chiến lược backup/restore |
| Database volume | **Chưa có** | Chưa có compose/volume; dữ liệu sẽ mất nếu container bị xóa |
| Upload/storage | **Chưa triển khai** | `Storage:RootPath` không được đọc; không có code upload; seed ảnh thiếu |
| Data Protection | **Chưa cấu hình** | Key ring mặc định trong container; session/antiforgery không bền vững |
| HTTPS/reverse proxy | **Chưa cấu hình** | Không có Forwarded Headers, cookie Secure, TLS termination |
| `.dockerignore` | **Chưa có** | Build context có nguy cơ chứa secret/file local |
| Healthcheck/readiness | **Chưa có** | Không có `/health`; chưa có cơ chế chờ DB |
| Backup/restore | **Chưa có** | Chưa có lịch backup, nơi lưu, quy trình restore |
| Least privilege DB | **Chưa có** | Chưa có login riêng cho app |
| Production hardening | **Chưa có** | Chưa có TLS, firewall, secret manager, monitoring, resource limits |

---

## 7. Các vấn đề có thể gây lỗi khi build hoặc chạy container

### 7.1. Lỗi build

| Vấn đề | Khả năng | Giải thích / cách xử lý |
|---|---|---|
| Không có Dockerfile/.dockerignore | Chắc chắn | Chưa thể build image; sẽ tạo ở giai đoạn triển khai |
| Build context quá lớn hoặc chứa secret | Cao | `bin/obj/.vs/.git`, uploads, `appsettings.Development.json`, `.env`; cần `.dockerignore` |
| SDK/runtime image không khớp | Trung bình | Không có `global.json`; dùng `mcr.microsoft.com/dotnet/sdk:10.0` và `aspnet:10.0`, nên pin tag/digest; package EF 10.0.14 cần runtime 10.0.x tương thích |
| NuGet restore cần mạng | Trung bình | Build container cần truy cập nuget.org hoặc mirror nội bộ; kiểm tra proxy/cache |
| Đường dẫn `wwwroot\uploads\` trong `.csproj` | Thấp | Item `Folder` chủ yếu cho IDE; nên build thử trên Linux để xác nhận; không thấy logic build phụ thuộc |
| Test project nằm trong solution | Thấp | Nếu build cả solution, test project cũng restore/build; có thể chỉ publish project web để giảm thời gian |

### 7.2. Lỗi runtime

| Vấn đề | Khả năng | Giải thích / cách xử lý |
|---|---|---|
| Không kết nối được SQL Server | **Rất cao** nếu dùng cấu hình hiện tại | `Trusted_Connection=True`/`.\SQLEXPRESS` không dùng được trong Linux container; đổi sang SQL Auth + `db:1433` |
| Ứng dụng crash khi thiếu JWT key | Cao nếu không đặt env | `Program.cs` ném lỗi nếu thiếu key/<32 byte; đặt `Jwt__Key` |
| Ứng dụng chạy nhưng database trống | Cao nếu không init | Không có auto-migrate; phải chạy init/restore trước |
| Lỗi thiếu bảng/cột/trigger | Cao nếu init thiếu `15`–`18`, `20` | Bổ sung đầy đủ script; so sánh schema |
| Mất session sau khi recreate container | Chắc chắn nếu không có Data Protection volume | Cấu hình `PersistKeysToFileSystem` + volume |
| Cookie không Secure/redirect sai sau proxy | Cao nếu không có Forwarded Headers | Thêm middleware và cấu hình proxy |
| Ảnh upload 404 | Cao khi triển khai upload | `UseStaticFiles` + volume; `MapStaticAssets` chỉ phục vụ asset build-time |
| Permission denied khi ghi uploads/keys | Trung bình | Image chạy non-root; tạo thư mục và `chown` đúng; named volume |
| SQL Server container không start | Trung bình | Thiếu `ACCEPT_EULA`, mật khẩu SA không đủ mạnh, thiếu RAM, port conflict |
| Port 8080 không map | Thấp | Đặt `ASPNETCORE_HTTP_PORTS=8080` và `ports: "8080:8080"` |

---

## 8. Những cấu hình cần thay đổi và lý do

### 8.1. Biến môi trường / cấu hình ứng dụng

| Khóa cấu hình | Nguồn hiện tại | Giá trị đề xuất cho container | Lý do |
|---|---|---|---|
| `ConnectionStrings__DefaultConnection` | `appsettings.json` (Windows Auth) | `Server=db,1433;Database=SecondHandMarketplace;User Id=app_user;Password=<secret>;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;Connection Timeout=30` | Linux container dùng SQL Auth/TCP; `db` là DNS service trong compose |
| `Jwt__Key` | Placeholder trong `appsettings.json` | Secret ngẫu nhiên ≥ 32 byte, không commit | Tránh giả mạo JWT; thay placeholder |
| `Jwt__Issuer`, `Jwt__Audience` | `appsettings.json` | Giữ hoặc đặt theo môi trường | Không phải secret; có thể giữ |
| `Jwt__ExpiresMinutes` | `appsettings.json` | 60 hoặc giá trị phù hợp | Kiểm soát thời hạn token |
| `Storage__RootPath` | `appsettings.json` | `/app/wwwroot/uploads` (nếu upload dùng) | Đường dẫn tuyệt đối trong container; volume mount |
| `Auction__SweepSeconds` | `appsettings.json` | 60 (hoặc tăng nếu cần) | Chu kỳ quét; tránh quá dày |
| `Ai__ApiKey` | Không có trong appsettings | Secret từ secret manager | Bật chat AI; không commit |
| `AllowedHosts` | `*` | Domain thật, ví dụ `market.example.com` | Giảm host header attack |
| `ASPNETCORE_ENVIRONMENT` | Mặc định Production khi chạy image | `Production` cho VPS; `Development` cho dev container | Tránh lộ chi tiết lỗi; bật/tắt HSTS đúng |
| `ASPNETCORE_HTTP_PORTS` | Không đặt | `8080` | Cổng nội bộ container; map ra ngoài |
| `ASPNETCORE_FORWARDEDHEADERS_ENABLED` | Không có | `true` nếu dùng proxy (hoặc cấu hình thủ công) | Nhận scheme/IP từ reverse proxy; cần kiểm tra trust |
| `MSSQL_SA_PASSWORD` | Không có | Secret mạnh, chỉ cho `db`/`db-init` | SQL Server container yêu cầu mật khẩu SA |
| `MSSQL_PID` | Không có | `Developer` cho dev; edition có license cho production | Tuân thủ license |
| `DB_APP_PASSWORD` | Không có | Secret riêng cho login app | Tách quyền app khỏi `sa` |

### 8.2. Thay đổi code/config cần thực hiện ở giai đoạn sau (chưa làm trong báo cáo này)

1. `Program.cs`: thêm `UseForwardedHeaders` trước `UseHttpsRedirection`/`UseAuthentication`.
2. `Program.cs`: đặt `Cookie.SecurePolicy = Always` ở Production; cân nhắc `SameSite`.
3. `Program.cs`: thêm `AddDataProtection().PersistKeysToFileSystem(...)` và `SetApplicationName`.
4. `Program.cs`: thêm health check `/health` (tùy chọn) và/hoặc bật retry DB.
5. Khi triển khai upload: đọc `Storage:RootPath`, tạo thư mục, validate file, thêm `UseStaticFiles` cho `/uploads`.
6. Có thể thêm kiểm tra fail-fast nếu `Jwt:Key` là placeholder trong Production.
7. Không chuyển sang Code First/Migration; giữ Database First.

---

## 9. Kiến trúc Docker đề xuất

### 9.1. Sơ đồ tổng quát

```text
Người dùng / Internet
        │  80/443 (HTTPS)
        ▼
Reverse proxy (Nginx/Caddy/Traefik)        [chỉ production]
        │  http://web:8080
        ▼
┌─────────────────────────────────────────────┐
│ web container (ASP.NET Core 10, non-root)   │
│  ├─ /app/wwwroot/uploads  ← volume uploads  │
│  ├─ /var/aspnet/keys      ← volume dp-keys  │
│  └─ kết nối TCP 1433 ───────────────────┐   │
└─────────────────────────────────────────│───┘
                                          ▼
┌─────────────────────────────────────────────┐
│ db container (SQL Server 2022 Linux)        │
│  └─ /var/opt/mssql  ← volume mssql-data     │
└─────────────────────────────────────────────┘
        ▲
        │ one-shot
┌───────┴─────────────────────────────────────┐
│ db-init container (sqlcmd hoặc restore .bak)│
│  └─ mount ./database:/scripts:ro            │
└─────────────────────────────────────────────┘
```

### 9.2. Thành phần đề xuất

| Thành phần | Image / công nghệ | Vai trò | Ghi chú |
|---|---|---|---|
| `web` | Build multi-stage từ `mcr.microsoft.com/dotnet/sdk:10.0`, chạy `mcr.microsoft.com/dotnet/aspnet:10.0` | ASP.NET Core MVC | Chạy non-root; `EXPOSE 8080`; `ASPNETCORE_HTTP_PORTS=8080`; pin tag/digest |
| `db` | `mcr.microsoft.com/mssql/server:2022-latest` (pin tag cụ thể) | SQL Server | `ACCEPT_EULA=Y`; `MSSQL_SA_PASSWORD`; `MSSQL_PID=Developer` cho dev; volume `/var/opt/mssql`; healthcheck |
| `db-init` | `mcr.microsoft.com/mssql-tools18` hoặc image có `sqlcmd` | Chạy script/restore một lần | Mount `./database:/scripts:ro`; chỉ chạy khi DB trống; không chạy lại mỗi lần `up` nếu đã có dữ liệu |
| Reverse proxy | Nginx/Caddy/Traefik | TLS termination, HTTP/2, security headers | Chỉ production; chuyển `X-Forwarded-Proto/For`; không expose web trực tiếp nếu có thể |
| Network | Docker bridge/user-defined network | DNS nội bộ `web`, `db` | Không dùng `localhost` giữa các container; DB không publish ra public |
| Volumes | Named volume | `mssql-data`, `uploads`, `dp-keys`, `backups` | Dữ liệu sống ngoài vòng đời container |

### 9.3. Phác thảo compose (chỉ là đề xuất, **không tạo file trong giai đoạn này**)

```yaml
# Phác thảo minh họa - chưa phải file compose chính thức
services:
  db:
    image: mcr.microsoft.com/mssql/server:2022-latest   # nên pin tag/digest
    environment:
      ACCEPT_EULA: "Y"
      MSSQL_SA_PASSWORD: "${MSSQL_SA_PASSWORD}"
      MSSQL_PID: "Developer"
    volumes:
      - mssql-data:/var/opt/mssql
    healthcheck:
      test: ["CMD-SHELL", "/opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P \"$$MSSQL_SA_PASSWORD\" -Q 'SELECT 1' -b"]
      interval: 10s
      timeout: 5s
      retries: 12
      start_period: 60s
    # dev: ports: ["127.0.0.1:14333:1433"]; production: không publish

  db-init:
    image: mcr.microsoft.com/mssql-tools18:latest
    profiles: ["init"]
    depends_on:
      db:
        condition: service_healthy
    volumes:
      - ./database:/scripts:ro
    environment:
      MSSQL_SA_PASSWORD: "${MSSQL_SA_PASSWORD}"
    # entrypoint chạy sqlcmd -b -f 65001 -C theo danh sách script an toàn

  web:
    build:
      context: .
      dockerfile: src/SecondHandMarketplace.Web/Dockerfile
    environment:
      ASPNETCORE_ENVIRONMENT: "Production"
      ASPNETCORE_HTTP_PORTS: "8080"
      ConnectionStrings__DefaultConnection: "Server=db,1433;Database=SecondHandMarketplace;User Id=app_user;Password=${DB_APP_PASSWORD};Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
      Jwt__Key: "${JWT_KEY}"
      Storage__RootPath: "/app/wwwroot/uploads"
      Ai__ApiKey: "${AI_API_KEY}"
      AllowedHosts: "market.example.com"
    depends_on:
      db:
        condition: service_healthy
    ports:
      - "8080:8080"       # dev; production nên đặt sau reverse proxy
    volumes:
      - uploads:/app/wwwroot/uploads
      - dp-keys:/var/aspnet/keys
    restart: unless-stopped

volumes:
  mssql-data:
  uploads:
  dp-keys:
```

> Các giá trị `market.example.com`, `app_user`, mật khẩu chỉ là placeholder. Không đưa mật khẩu thật vào báo cáo hay repository.

### 9.4. Dockerfile đề xuất (multi-stage)

```dockerfile
# Phác thảo - chưa tạo file
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/SecondHandMarketplace.Web/SecondHandMarketplace.Web.csproj src/SecondHandMarketplace.Web/
RUN dotnet restore src/SecondHandMarketplace.Web/SecondHandMarketplace.Web.csproj
COPY src/SecondHandMarketplace.Web/ src/SecondHandMarketplace.Web/
RUN dotnet publish src/SecondHandMarketplace.Web/SecondHandMarketplace.Web.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
# Tạo thư mục uploads/keys và chown cho user app (UID trong image .NET)
RUN mkdir -p /app/wwwroot/uploads /var/aspnet/keys \
    && chown -R app:app /app/wwwroot/uploads /var/aspnet/keys
USER app
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080
ENTRYPOINT ["dotnet", "SecondHandMarketplace.Web.dll"]
```

**Lưu ý:** nếu dùng `PersistKeysToFileSystem("/var/aspnet/keys")`, cần đảm bảo user `app` có quyền ghi; nếu không, đổi đường dẫn hoặc `chown` tương ứng. Không đặt `USER root` để “cho nhanh”.

---

## 10. Phương án kết nối SQL Server và ASP.NET Core

### 10.1. Nguyên tắc

- Trong Docker network, **không dùng `localhost`** cho database; dùng service name `db` (hoặc `sqlserver`).
- Dùng **SQL Authentication**, không dùng `Trusted_Connection`/Windows Authentication trong Linux container.
- Dùng **TCP cổng 1433**; không dùng named instance `.\SQLEXPRESS` (không có SQL Browser trong container).
- `TrustServerCertificate=True` cho dev với cert tự ký của container; production nên dùng chứng chỉ hợp lệ/CA.
- Giữ `MultipleActiveResultSets=True` nếu code hiện tại cần; SQL Server Linux hỗ trợ.
- App dùng login riêng `app_user`; `sa` chỉ dùng cho `db`/`db-init`.

### 10.2. Connection string đề xuất

**Cho web container:**

```text
Server=db,1433;Database=SecondHandMarketplace;User Id=app_user;Password=<DB_APP_PASSWORD>;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;Connection Timeout=30
```

**Cho init/restore (chỉ trong container init):**

```text
Server=db,1433;Database=master;User Id=sa;Password=<MSSQL_SA_PASSWORD>;Encrypt=True;TrustServerCertificate=True
```

**Cho SSMS/Azure Data Studio từ máy host (chỉ dev):**

```text
Server=localhost,14333;Database=SecondHandMarketplace;User Id=sa;Password=<MSSQL_SA_PASSWORD>;Encrypt=True;TrustServerCertificate=True
```

### 10.3. Cách khởi tạo database

Có hai phương án; nên chọn một và ghi rõ trong runbook:

#### Phương án 1 — Restore backup từ DB dev (khuyến nghị cho dev/test)

- Tạo `.bak` từ database hiện tại.
- Mount file `.bak` vào container SQL Server hoặc dùng `db-init` để `RESTORE DATABASE ... WITH MOVE ...`.
- Đảm bảo image SQL Server cùng phiên bản hoặc mới hơn DB nguồn.
- Sau restore, tạo login `app_user` và map user trong database (`CREATE USER ... FOR LOGIN ...`).
- Ưu điểm: giữ đúng schema, collation, view/proc/trigger và dữ liệu demo.
- Nhược điểm: file backup có thể chứa dữ liệu nhạy cảm; không dùng làm quy trình production.

#### Phương án 2 — Chạy toàn bộ script SQL bằng `db-init` (khuyến nghị cho môi trường sạch)

- Mount `database/` read-only vào init container.
- Dùng `sqlcmd` với `-b -f 65001 -C` để dừng khi lỗi và giữ UTF-8.
- **Không chạy `01_create_database.sql` trên volume đã có database**, vì file này `DROP DATABASE`.
- Tạo database bằng một bước an toàn (ví dụ kiểm tra `IF DB_ID('SecondHandMarketplace') IS NULL` trước khi chạy phần tạo), sau đó chạy:
  `02`, `03`, `04`, `07`, `05`, `06`, `08`, `09`, `10`, `11`, `12`, `13`, `14`, `15`, `16`, `17`, `18`, `20`.
- Thứ tự lưu ý:
  - `07_functions.sql` trước `06_procedures.sql`.
  - `17_bo_chan_tu_cam_khi_gui_duyet.sql` sau `07`, `14`, `15`.
  - `20_cauhinh_ai.sql` sau `02`, `03`.
- Init job chỉ chạy một lần; không để nó chạy lại mỗi lần `docker compose up` trên volume đã có dữ liệu.
- Không dùng `Database.EnsureCreated()` hay `Database.Migrate()`: `EnsureCreated` sẽ tạo schema từ EF model, thiếu view/proc/function/trigger và sai hướng Database First.

### 10.4. Đảm bảo database sẵn sàng trước khi web kết nối

1. `db` có healthcheck `sqlcmd SELECT 1`.
2. `db-init` chạy sau `db` healthy; thoát với mã 0 khi init xong.
3. `web` chạy sau `db` healthy (và sau `db-init` nếu dùng `service_completed_successfully`), hoặc app có retry/healthcheck.
4. Không chỉ dựa vào `depends_on` thông thường: `depends_on` không đảm bảo SQL Server đã sẵn sàng nhận kết nối.
5. Nếu không dùng init job tự động, chạy init thủ công một lần trước khi `up` web; ghi rõ trong README/runbook.

---

## 11. Phương án lưu trữ database và ảnh upload

### 11.1. Volume đề xuất

| Volume | Mount trong container | Mục đích | Bắt buộc |
|---|---|---|---|
| `mssql-data` | `/var/opt/mssql` | Dữ liệu SQL Server (system DB + user DB) | Có, nếu muốn giữ dữ liệu |
| `uploads` | `/app/wwwroot/uploads` | Ảnh/file người dùng upload (khi có chức năng) | Có, khi triển khai upload |
| `dp-keys` | `/var/aspnet/keys` (theo cấu hình code) | Data Protection key ring cho cookie/antiforgery | Có, nếu muốn giữ session |
| `backups` | `/backups` hoặc host path | File backup `.bak` | Khuyến nghị; nên tách khỏi volume dữ liệu chính |

### 11.2. Dev vs production

**Dev (Docker Desktop):**

- Named volume cho SQL Server và uploads là đủ; không nên bind mount dữ liệu SQL Server từ Windows vì dễ lỗi permission.
- Nếu cần xem file upload từ Windows, có thể bind mount thư mục `wwwroot/uploads` nhưng phải kiểm tra quyền ghi của user container; named volume an toàn hơn.
- Có thể publish SQL port ra `127.0.0.1:14333` để dùng SSMS.

**Production (VPS):**

- Volume phải nằm trên disk ổn định, có backup offsite.
- Ảnh upload nên chuyển sang object storage (S3/MinIO) hoặc NFS nếu chạy nhiều web replica; volume local chỉ phù hợp một node.
- Không backup bằng cách copy thư mục `/var/opt/mssql` khi SQL Server đang chạy; dùng `BACKUP DATABASE` hoặc snapshot ở tầng storage.
- Đặt lịch backup từ host/cron/dịch vụ ngoài; không mặc định rằng SQL Server Agent trong container đã chạy.
- Kiểm thử restore định kỳ, không chỉ kiểm tra file backup tồn tại.

### 11.3. Quyền ghi

- Image .NET chính thức chạy non-root (user `app`). Thư mục uploads/keys phải thuộc user này.
- Nếu dùng bind mount từ Windows, quyền Linux có thể không như mong đợi; ưu tiên named volume cho dev.
- Nếu mount read-only root filesystem ở production, phải mount writable volume cho uploads/keys và `tmpfs` cho thư mục tạm nếu cần.

---

## 12. Lưu ý bảo mật khi deploy production

### 12.1. Secret và cấu hình

- **JWT key**: bắt buộc thay placeholder; dùng secret ngẫu nhiên ≥ 32 byte; không commit; có kế hoạch xoay khóa.
- **Connection string**: chứa mật khẩu DB; đặt qua secret, không đưa vào image/appsettings.
- **SA password**: mật khẩu mạnh, chỉ dùng cho init/khôi phục; không cấu hình app dùng `sa`.
- **AI API key**: đặt qua `Ai__ApiKey`/secret manager; không đưa vào JS phía client (hiện tại code đã đúng hướng).
- **Seed admin/demo**: không chạy seed ở production; đổi mật khẩu admin; xóa/khóa tài khoản demo.
- **`.env`**: thêm vào `.gitignore`; không commit; production dùng Docker secrets/Vault/secret manager nếu có thể.
- **Docker inspect**: biến môi trường có thể bị xem bởi người có quyền truy cập Docker daemon; hạn chế quyền truy cập daemon.

### 12.2. TLS, cookie, proxy

- Bắt buộc HTTPS ở production; terminate TLS tại Nginx/Caddy/Traefik hoặc cấu hình Kestrel HTTPS với cert.
- Thêm `UseForwardedHeaders` và chỉ tin proxy nội bộ; nếu không, kẻ tấn công có thể giả `X-Forwarded-Proto`.
- Đặt cookie `Secure`, `HttpOnly`, `SameSite=Lax` (hoặc cấu hình phù hợp); không dùng `SameSite=None` nếu không cần cross-site.
- Bật HSTS ở production; không bật HSTS khi test local HTTP.
- Giới hạn `AllowedHosts`; cấu hình security headers ở reverse proxy (HSTS, X-Content-Type-Options, Referrer-Policy, CSP nếu phù hợp).

### 12.3. Database

- Không publish cổng 1433 ra Internet.
- Tường lửa chỉ cho phép web container/SSH quản trị cần thiết.
- Dùng login app quyền tối thiểu; `sa` chỉ dùng init.
- Bật backup định kỳ, mã hóa backup nếu chứa dữ liệu người dùng; kiểm thử restore.
- Cân nhắc SQL Server edition có license phù hợp; `Developer` chỉ dùng dev/test.
- Kiểm tra collation/phiên bản trước khi restore; không restore backup từ phiên bản mới hơn vào image cũ.

### 12.4. Container và image

- Chạy web non-root; `USER app`.
- Cân nhắc `read_only: true` cho root filesystem, chỉ mount writable volume cho uploads/keys/tmp.
- `security_opt: ["no-new-privileges:true"]`; giới hạn CPU/RAM; `restart: unless-stopped`.
- Pin image tag/digest; không dùng `latest` ở production; quét image (Trivy/Grype) và cập nhật base image.
- Không mount Docker socket vào container; không chạy privileged.
- Cấu hình log rotation (`max-size`, `max-file`) và giám sát dung lượng disk.
- Không đưa source code/database scripts không cần thiết vào runtime image.
- Kiểm tra image không chứa `appsettings.Development.json`, `.env`, file upload, `.git`.

### 12.5. Ứng dụng

- Không chạy `ASPNETCORE_ENVIRONMENT=Development` trên production (tránh lộ stack trace).
- Kiểm tra `AllowedHosts`, CORS, HSTS, cookie.
- Rate limit/antiforgery cho form; AI đã có throttle trong bộ nhớ nhưng chưa distributed.
- Upload file: validate loại/kích thước/nội dung; không serve trực tiếp file nguy hiểm; cân nhắc antivirus.
- Audit log/notification có thể chứa dữ liệu cá nhân; kiểm tra quyền truy cập và retention.

---

## 13. Checklist công việc giai đoạn triển khai tiếp theo

### Giai đoạn 0 — Chuẩn bị và xác minh baseline

- [ ] Chạy query **read-only** trên DB dev để lấy `@@VERSION`, `SERVERPROPERTY('Collation')`, `DATABASEPROPERTYEX('SecondHandMarketplace','Collation')`.
- [ ] Kiểm tra DB dev đã có đủ object của `15`–`18`, `20` (bảng `CauHinhAI`, cột/trigger/function liên quan).
- [ ] Tạo file backup `.bak` baseline của DB dev (nếu dùng phương án restore).
- [ ] Chốt phiên bản SQL Server container (khuyến nghị 2022, pin tag) và collation.
- [ ] Chốt mô hình chạy: chỉ dev hay cả VPS production; domain/TLS/reverse proxy.

### Giai đoạn 1 — Cấu hình và secret

- [ ] Tạo `.env.example` (không chứa secret thật) và `.env` local; thêm `.env*` vào `.gitignore`.
- [ ] Đặt `Jwt__Key` mạnh; kiểm tra không còn placeholder trong production.
- [ ] Đặt `ConnectionStrings__DefaultConnection` dùng `db:1433`, SQL Auth.
- [ ] Đặt `MSSQL_SA_PASSWORD`, `DB_APP_PASSWORD`, `Ai__ApiKey` qua secret.
- [ ] Đặt `ASPNETCORE_ENVIRONMENT`, `ASPNETCORE_HTTP_PORTS`, `AllowedHosts`.
- [ ] Quyết định có dùng `ASPNETCORE_FORWARDEDHEADERS_ENABLED` hay cấu hình ForwardedHeaders thủ công.

### Giai đoạn 2 — Dockerfile và .dockerignore

- [ ] Tạo `.dockerignore` loại trừ `bin`, `obj`, `.git`, `.vs`, `*.user`, `appsettings.Development.json`, `.env*`, `wwwroot/uploads`, test results.
- [ ] Tạo Dockerfile multi-stage `sdk:10.0` → `aspnet:10.0`, pin tag/digest.
- [ ] Tạo thư mục uploads/keys và `chown` cho user `app`.
- [ ] `EXPOSE 8080`; `ASPNETCORE_HTTP_PORTS=8080`; `ENTRYPOINT` đúng DLL.
- [ ] Kiểm tra image không chứa secret/source/database script không cần thiết.
- [ ] Build thử image ở môi trường dev (sau khi được duyệt; đây là thao tác tạo image, không thực hiện trong giai đoạn khảo sát).

### Giai đoạn 3 — Compose và database init

- [ ] Tạo `compose.yaml` dev với `db`, `db-init` (profile init), `web`.
- [ ] Named volumes: `mssql-data`, `uploads`, `dp-keys`, `backups`.
- [ ] Network nội bộ; chỉ publish cổng web; DB chỉ publish `127.0.0.1:14333` nếu cần.
- [ ] SQL healthcheck bằng `sqlcmd -C -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -Q "SELECT 1"`.
- [ ] Init job dùng `sqlcmd -b -f 65001 -C`; mount `database` read-only.
- [ ] **Không chạy `01_create_database.sql` tự động trên volume đã có dữ liệu.**
- [ ] Chạy đủ script theo thứ tự: `02, 03, 04, 07, 05, 06, 08, 09, 10, 11, 12, 13, 14, 15, 16, 17, 18, 20`.
- [ ] Tạo login `app_user` quyền tối thiểu; cấu hình app dùng login này.
- [ ] Kiểm tra `db-init` chỉ chạy một lần và thoát mã 0.

### Giai đoạn 4 — Chạy thử và kiểm thử chức năng

- [ ] `docker compose up -d db`; chờ healthy.
- [ ] Chạy init một lần; kiểm tra `CauHinhAI`, bảng, view, proc, trigger.
- [ ] `docker compose up -d web`; kiểm tra log khởi động, không lỗi JWT/DB.
- [ ] Đăng nhập admin, duyệt sản phẩm, giỏ hàng, đơn hàng, đấu giá, thông báo.
- [ ] Kiểm tra background service chốt phiên đấu giá.
- [ ] Kiểm tra AI nếu có key (hoặc xác nhận fallback khi thiếu key).
- [ ] Kiểm tra ảnh seed/404; nếu có upload, kiểm tra upload → URL → restart container.
- [ ] Kiểm tra session/antiforgery sau khi recreate web container.

### Giai đoạn 5 — Kiểm thử persistence và vận hành

- [ ] `docker compose down` rồi `up` lại: database còn dữ liệu.
- [ ] Xóa/tạo lại web container: session còn hiệu lực (Data Protection volume).
- [ ] Xóa/tạo lại web container: ảnh upload còn (uploads volume).
- [ ] Restore thử từ backup sang volume mới.
- [ ] Kiểm tra dung lượng volume, log rotation.
- [ ] Kiểm tra healthcheck/restart khi DB chậm hoặc web lỗi.

### Giai đoạn 6 — Production hardening

- [ ] Reverse proxy TLS (80/443), chỉ expose 80/443; web không publish trực tiếp nếu có thể.
- [ ] `UseForwardedHeaders`, cookie `Secure`, HSTS, `AllowedHosts`.
- [ ] `ASPNETCORE_ENVIRONMENT=Production`; tắt seed/demo; đổi mật khẩu admin.
- [ ] Secret manager/Docker secrets; không dùng `.env` plaintext nếu có thể.
- [ ] SQL Server edition/license phù hợp; không dùng `sa` cho app.
- [ ] Backup tự động + offsite + kiểm thử restore.
- [ ] Giới hạn CPU/RAM; non-root; read-only root FS; `no-new-privileges`.
- [ ] Pin image tag/digest; quét lỗ hổng; cập nhật base image.
- [ ] Monitoring, log tập trung, cảnh báo dung lượng/health.
- [ ] Runbook: init, backup, restore, rollback, rotate secret, thay đổi schema.

---

## 14. Kết luận

### 14.1. Có thể triển khai Docker ngay không?

**Không.** Project có nền tảng công nghệ phù hợp, nhưng chưa đủ điều kiện để triển khai Docker ngay, cả ở mức “chạy thử có dữ liệu” lẫn mức production.

- **Có thể build image về mặt source?** Có khả năng, vì không thấy phụ thuộc Windows trong code và các package chính cross-platform. Tuy nhiên **chưa chạy build/publish trên Linux** nên chưa xác minh.
- **Có thể chạy local Docker ngay?** Không, vì connection string hiện tại không dùng được trong Linux container, JWT key là placeholder, và chưa có quy trình init database an toàn.
- **Có thể deploy production ngay?** Không, vì còn thiếu Data Protection, TLS/forwarded headers, volume/backup, secret management, least privilege DB và các biện pháp hardening.

### 14.2. Những vấn đề phải xử lý trước

**Blocker — bắt buộc:**

1. Chuyển connection string sang SQL Authentication + `db:1433`; bỏ `Trusted_Connection`/`.\SQLEXPRESS`/LocalDB.
2. Thay JWT key placeholder bằng secret thật; thêm kiểm tra fail-fast nếu còn placeholder ở Production.
3. Thiết kế quy trình init database an toàn: không chạy `01` xóa DB trên volume có dữ liệu; bổ sung đủ `15`–`18`, `20`; dùng restore hoặc init một lần.

**High — nên xử lý trước khi coi là deploy được:**

4. Tạo `.dockerignore`; bảo vệ `.env`; không đưa secret/file local vào image.
5. Cấu hình Data Protection key persistence + volume.
6. Cấu hình Forwarded Headers, cookie `Secure`, TLS/reverse proxy.
7. Tạo volume SQL Server + backup/restore; tạo login app quyền tối thiểu.
8. Xử lý seed data/demo password; không dùng ở production.
9. Làm rõ và triển khai upload/storage nếu thuộc phạm vi; thêm `UseStaticFiles` cho file runtime; volume uploads.

### 14.3. Thứ tự triển khai phù hợp nhất

1. **Xác minh baseline DB dev** (version, collation, đủ object) và tạo backup.
2. **Chốt cấu hình/secret** cho Docker (connection string, JWT, SA/app password, AI key).
3. **Tạo `.dockerignore` + Dockerfile multi-stage**; build thử image.
4. **Tạo compose dev** với `db`, healthcheck, init job, web, volumes.
5. **Init database một lần** bằng backup hoặc script runner an toàn; tạo app login.
6. **Chạy web và smoke test** các luồng chính; kiểm tra persistence sau recreate.
7. **Hardening production**: reverse proxy TLS, forwarded headers, cookie Secure, secrets, least privilege, backup, monitoring.
8. **Triển khai upload/storage** (nếu cần) với volume/object storage và static serving phù hợp.
9. **Diễn tập restore/rollback** và cập nhật runbook.

> Chỉ sau khi hoàn thành tối thiểu các mục 1–7 mới nên coi cấu hình là “sẵn sàng cho người khác truy cập”. Cấu hình chạy local trên Docker Desktop **không tự động đủ an toàn cho production**.

---

## Phụ lục A — Lệnh kiểm tra read-only đề xuất (chưa chạy trong báo cáo này)

Các lệnh dưới đây chỉ đọc, không thay đổi DB/Docker/source. Chỉ chạy khi có sự đồng ý:

```sql
-- Trên SQL Server dev, chỉ đọc
SELECT @@VERSION;
SELECT SERVERPROPERTY('Collation') AS ServerCollation;
SELECT DATABASEPROPERTYEX('SecondHandMarketplace', 'Collation') AS DatabaseCollation;

-- Kiểm tra object schema hiện có (chỉ đọc)
SELECT type_desc, name
FROM sys.objects
WHERE type IN ('U','V','P','FN','IF','TF','TR')
ORDER BY type_desc, name;

-- Kiểm tra các cột/trigger liên quan moderation/AI
SELECT OBJECT_NAME(object_id) AS Bang, name AS Cot
FROM sys.columns
WHERE OBJECT_NAME(object_id) IN ('CauHinhAI','LuotKiemDuyet','PhanTichAI','LichSuViPham','TuKhoaCam')
ORDER BY Bang, column_id;
```

```powershell
# Chỉ đọc thông tin môi trường .NET; không build, không sửa gì
dotnet --info

# Khi cần xác minh build (giai đoạn sau): chỉ ghi bin/obj, có thể restore NuGet; không đụng DB/Docker
# dotnet build src\SecondHandMarketplace.Web\SecondHandMarketplace.Web.csproj --nologo

# Kiểm thử unit test dùng EF InMemory; không kết nối SQL Server
# dotnet test tests\SecondHandMarketplace.Tests\SecondHandMarketplace.Tests.csproj --nologo
```

> `dotnet build`/`dotnet test` có thể ghi file vào `bin/obj` và tải package từ NuGet; không làm thay đổi database hay Docker. Không chạy nếu chưa được duyệt.

---

## Phụ lục B — Bảng tổng hợp mức độ

| Mức độ | Số lượng | Các ID chính |
|---|---:|---|
| **Blocker** | 3 | A-02, B-01, B-02 |
| **High** | 11 | A-03, A-04, B-03, B-04, B-05, C-01, C-02, C-03, D-01, D-02, D-03 |
| **Medium** | 10 | A-05, A-08, B-06, B-07, C-04, C-06, D-04, D-05, D-06, D-07 |
| **Low** | 10 | A-01, A-06, A-07, B-08, B-09, B-10, C-05, D-08, D-09, D-10 |

Tổng cộng: **34 phát hiện** (3 Blocker, 11 High, 10 Medium, 10 Low).

---

## Phụ lục C — Tài liệu tham khảo

- Microsoft Learn — Serve static files in ASP.NET Core (`MapStaticAssets` và `UseStaticFiles`): https://learn.microsoft.com/aspnet/core/fundamentals/static-files
- Microsoft Learn — Data Protection configuration/key storage: https://learn.microsoft.com/aspnet/core/security/data-protection/configuration/overview
- Microsoft Learn — Configure ASP.NET Core to work with proxy servers and load balancers (Forwarded Headers): https://learn.microsoft.com/aspnet/core/host-and-deploy/proxy-load-balancer
- Microsoft Learn — SQL Server Linux container: https://learn.microsoft.com/sql/linux/sql-server-linux-docker-container-deployment
- Docker Docs — Compose depends_on/healthcheck: https://docs.docker.com/compose/compose-file/05-services/#depends_on

---

*Báo cáo chỉ phục vụ khảo sát và đánh giá. Không có Dockerfile, compose.yaml, .dockerignore nào được tạo; không có image/container/volume/network nào được tạo; không có script SQL hay source code nào bị thay đổi.*
