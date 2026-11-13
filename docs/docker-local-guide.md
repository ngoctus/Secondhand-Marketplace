# Hướng dẫn đóng gói và chạy thử ứng dụng qua Docker trên máy Local

Tài liệu này ghi lại chi tiết toàn bộ các bước thiết lập, đóng gói (build container) và chạy thử nghiệm website **Secondhand Marketplace** (ASP.NET Core .NET 10) bằng **Docker** trên môi trường máy tính cá nhân (Local).

---

## 1. Cơ chế hoạt động (Docker vs. Chạy trực tiếp)

| Tiêu chí | Chạy trực tiếp (Visual Studio / `dotnet run`) | Chạy bằng Docker (`docker compose up`) |
| :--- | :--- | :--- |
| **Môi trường** | Chạy trực tiếp trên hệ điều hành Windows của bạn. | Chạy trong container Linux độc lập. |
| **File cấu hình** | `appsettings.json` / `appsettings.Development.json`. | Đọc từ file `.env` thông qua `docker-compose.yml`. |
| **Xác thực DB** | Hỗ trợ Windows Authentication (`Trusted_Connection=True`). | Bắt buộc dùng SQL Server Authentication (`User Id & Password`). |
| **Độ độc lập** | Phải cài .NET 10 SDK trên máy. | Không cần cài .NET 10 SDK, chỉ cần Docker Engine. |

> Hai luồng trên **hoàn toàn độc lập**. Việc cấu hình Docker không ảnh hưởng tới cách bạn code và debug bằng Visual Studio hàng ngày.

---

## 2. Các file cấu hình Docker trong dự án

Dự án đã có sẵn bộ file phục vụ Docker:

1. **`Dockerfile`**: Sử dụng kỹ thuật **Multi-stage build**:
   - *Stage 1 (Build)*: Dùng image `mcr.microsoft.com/dotnet/sdk:10.0` để restore NuGet và compile mã nguồn ra thư mục `/app/out`.
   - *Stage 2 (Runtime)*: Dùng image nhẹ `mcr.microsoft.com/dotnet/aspnet:10.0` để chạy ứng dụng, mở cổng `8080`.
2. **`docker-compose.yml`**: Định nghĩa container `secondhand_web`, ánh xạ cổng `8080:8080`, gắn biến môi trường và tạo 2 volume lưu trữ ảnh upload (`product_uploads`, `dispute_uploads`).
3. **`.dockerignore`**: Bỏ qua các file/thư mục không cần thiết (`bin/`, `obj/`, `.git/`) giúp quá trình build image diễn ra nhanh nhất.
4. **`.env`**: Nơi lưu trữ tài khoản database, mật khẩu và khóa bí mật JWT cho Docker.

---

## 3. Các bước thiết lập và chạy chi tiết

### Bước 1: Chuẩn bị SQL Server trên máy host

Vì container Linux cần kết nối ngược về SQL Server trên máy tính Windows của bạn:

1. **Bật SQL Server Authentication (Mixed Mode):**
   - Mở **SSMS** $\rightarrow$ Chuột phải vào Server $\rightarrow$ **Properties** $\rightarrow$ tab **Security** $\rightarrow$ Chọn **SQL Server and Windows Authentication mode**.
2. **Kích hoạt tài khoản đăng nhập (Login):**
   - Tạo hoặc dùng tài khoản có sẵn (ví dụ: `NgocTus` với mật khẩu `OwO@123`).
   - Cấp quyền truy cập (vai trò `db_owner` hoặc `dbo`) trên cơ sở dữ liệu `SecondHandMarketplace`.
3. **Bật giao thức mạng TCP/IP:**
   - Mở **SQL Server Configuration Manager** $\rightarrow$ **SQL Server Network Configuration** $\rightarrow$ **Protocols for SQLEXPRESS** $\rightarrow$ Đảm bảo **TCP/IP** đang ở trạng thái **Enabled** (cổng mặc định 1433).

---

### Bước 2: Tạo và cấu hình file `.env`

Tạo file `.env` tại thư mục gốc của dự án (cùng cấp với `docker-compose.yml`):

```env
# Kết nối ngược về máy tính host thông qua domain nội bộ của Docker
DB_SERVER=host.docker.internal
DB_NAME=SecondHandMarketplace
DB_USER=NgocTus
DB_PASSWORD=OwO@123

# Khóa bí mật JWT (bắt buộc dài tối thiểu 32 ký tự)
JWT_KEY=SecondhandMarketplace_SecretKey_LocalDev_2026_AtLeast32Bytes

# API key AI (không bắt buộc, để trống nếu chưa dùng)
AI_API_KEY=
```

> File `.env` đã được đưa vào `.gitignore` nên sẽ không bị push lên kho mã nguồn chung.

---

### Bước 3: Cấu hình `docker-compose.yml` cho máy Local

Trong file `docker-compose.yml`:
* **Bỏ qua chứng chỉ SSL tự ký local:** Cấu hình `TrustServerCertificate=${DB_TRUST_CERT:-True}` để tránh lỗi chứng chỉ khi kết nối tới SQL Server ở máy dev.
* **Mở port:** Cấu hình `ports: - "8080:8080"` để hỗ trợ cả IPv4 và IPv6.

---

### Bước 4: Khởi động Docker Desktop và Build ứng dụng

1. **Mở phần mềm Docker Desktop** trên Windows và chờ biểu tượng ở góc dưới chuyển sang màu xanh (Docker Engine is running).
2. Mở terminal (PowerShell hoặc CMD) tại thư mục gốc dự án và chạy:
   ```bash
   docker compose up -d --build
   ```
   *Lệnh này sẽ tự động tải base image, build source code, tạo container và chạy ngầm.*

---

### Bước 5: Kiểm tra trạng thái và xem nhật ký (Logs)

* **Xem danh sách container đang chạy:**
  ```bash
  docker compose ps
  ```
  *(Trạng thái hiển thị `Up ... (healthy)` hoặc `Up ...` là thành công).*

* **Xem log thời gian thực để debug:**
  ```bash
  docker compose logs -f web
  ```
  *(Khi thấy dòng `Now listening on: http://[::]:8080` và các câu lệnh SQL khởi chạy thành công nghĩa là website đã hoạt động bình thường).*

---

### Bước 6: Truy cập thử nghiệm trên trình duyệt

Mở trình duyệt truy cập vào địa chỉ:
```
http://localhost:8080
```
*(hoặc `http://127.0.0.1:8080`)*

> [!CAUTION]
> **Lưu ý quan trọng về VPN trên trình duyệt (Đặc biệt là Opera / Brave / Extension VPN):**
> Nếu trình duyệt báo lỗi `ERR_CONNECTION_REFUSED` (localhost đã từ chối kết nối), hãy kiểm tra xem trình duyệt có đang **bật VPN** hay không.
> - Tính năng VPN sẽ chuyển hướng truy cập qua máy chủ nước ngoài, khiến trình duyệt không thể gọi được vào `localhost` trên máy của bạn.
> - **Khắc phục:** Tắt VPN trên trình duyệt hoặc mở bằng Google Chrome / Microsoft Edge.

---

## 4. Các lệnh quản lý Docker thường dùng

| Thao tác | Lệnh |
| :--- | :--- |
| **Xem trạng thái container** | `docker compose ps` |
| **Xem log container web** | `docker compose logs -f web` |
| **Dừng container (tạm dừng)** | `docker compose stop` |
| **Khởi động lại container** | `docker compose restart` |
| **Gỡ container (giữ nguyên dữ liệu upload)** | `docker compose down` |
| **Cập nhật code mới và build lại** | `docker compose up -d --build` |
| **Truy cập terminal bên trong container** | `docker exec -it secondhand_web /bin/bash` |
