# Hướng Dẫn Deploy Hướng 1: Docker Local + Mở Link Công Khai (Tunnel)

Tài liệu này hướng dẫn chi tiết cách chạy hệ thống **Secondhand Marketplace** (ASP.NET Core .NET 10 + SQL Server) bằng Docker trên máy cá nhân và mở đường link công khai qua Internet để phục vụ báo cáo bài tập lớn, cho bạn bè/thầy cô truy cập thử nghiệm kèm tính năng **AI Gemini**.

---

## 1. Cấu hình AI Gemini cho môi trường Docker

### Cơ chế bảo mật và nạp cấu hình:
- Trong Docker, API Key tuyệt đối **không được ghi cứng vào `Dockerfile`** (để tránh việc commit lên GitHub hoặc chia sẻ Docker image làm lộ khóa bí mật).
- Thay vào đó, API Key được đặt trong file **`.env`** (đã được cấu hình trong `.gitignore`).
- Khi khởi động, file `docker-compose.yml` sẽ đọc biến `AI_API_KEY` từ `.env` và truyền vào container dưới dạng biến môi trường `Ai__ApiKey`. Ứng dụng ASP.NET Core sẽ tự động ánh xạ biến này vào `Ai:ApiKey`.

### Cấu hình trong file `.env`:
Tạo `.env` riêng trên máy triển khai dựa trên `.env.example`; không chép khóa hoặc mật khẩu vào tài liệu hay mã nguồn:
```env
DB_SERVER=host.docker.internal
DB_NAME=SecondHandMarketplace
DB_USER=<sql-login>
DB_PASSWORD=<local-secret>
DB_TRUST_CERT=True
JWT_KEY=<random-secret-at-least-32-bytes>
AI_API_KEY=<provider-api-key>
```

Các giá trị bí mật từng xuất hiện trong phiên bản cũ của tài liệu phải được coi là đã lộ: thu hồi/thay JWT key và API key tại nơi cấp, đồng thời đổi mật khẩu DB nếu các giá trị đó còn hiệu lực. Không tái sử dụng chúng.

---

## 2. Quy trình demo tạm thời qua tunnel

Chỉ dùng tunnel cho demo với dữ liệu thử nghiệm và tài khoản riêng. Link tunnel công khai có thể được truy cập bởi người khác; không dùng cấu hình này cho cửa hàng thật hoặc dữ liệu khách hàng. Compose bind ứng dụng vào loopback; tunnel phải chạy trên cùng máy chủ.

### Bước 1: Khởi động Docker Desktop & Web Container
1. Mở ứng dụng **Docker Desktop** trên máy tính (đợi vài giây cho Docker Engine chuyển sang màu xanh).
2. Mở terminal (PowerShell) tại thư mục gốc dự án và chạy:
   ```bash
   docker compose up -d
   ```
3. Kiểm tra web và AI hoạt động nội bộ:
   Mở trình duyệt truy cập: `http://localhost:8080` (đảm bảo tắt VPN trên trình duyệt).

---

### Bước 2: Tạo đường link Internet công khai bằng Pinggy
Windows 10/11 đã tích hợp sẵn công cụ `ssh`, bạn **không cần cài thêm bất cứ phần mềm nào**.

1. Mở một cửa sổ **PowerShell** mới và chạy lệnh:
   ```powershell
   ssh -p 443 -R0:localhost:8080 a.pinggy.io
   ```
2. Nếu terminal hỏi xác nhận kết nối lần đầu:
   ```text
   Are you sure you want to continue connecting (yes/no/[fingerprint])?
   ```
   👉 Gõ **`yes`** rồi nhấn **Enter**.

3. Ngay sau đó, terminal sẽ hiển thị bảng thông tin dạng:
   ```text
   ================================================================
     http://xxxxxxxx.a.pinggy.link
     https://xxxxxxxx.a.pinggy.link   <--- COPY LINK HTTPS NÀY
   ================================================================
   ```

---

### Bước 3: Gửi link và kiểm tra nghiệm thu

1. **Gửi link:** Copy đường link có **`https://...`** gửi cho bạn bè hoặc thầy cô. Mọi thiết bị (điện thoại, máy tính) ở bất kỳ đâu đều vào được.
2. **Kiểm tra tính năng AI:**
   - **Chatbox trợ lý ảo:** Bấm vào biểu tượng Chat AI ở góc phải màn hình để nhắn tin hỏi thông tin đồ cũ, gợi ý sản phẩm.
   - **AI soạn mô tả đăng bán:** Vào trang Đăng sản phẩm mới $\rightarrow$ Nhập tên sản phẩm $\rightarrow$ Bấm nút **"AI soạn mô tả"** để xem Gemini tự động sinh nội dung mô tả chi tiết.

---

## 3. Các lựa chọn tạo Tunnel thay thế (Dự phòng)

Nếu mạng của bạn chặn cổng SSH ra ngoài, bạn có thể dùng một trong hai công cụ phổ biến sau:

### Lựa chọn A: Cloudflare Tunnel (Khuyên dùng - Rất nhanh & Ổn định)
1. Cài đặt qua PowerShell (chỉ cần chạy 1 lần):
   ```powershell
   winget install --id Cloudflare.cloudflared
   ```
2. Chạy lệnh mở link (không cần đăng ký tài khoản):
   ```powershell
   cloudflared tunnel --url http://localhost:8080
   ```
   *(Hệ thống sẽ cấp một link dạng `https://xxxx.trycloudflare.com`).*

### Lựa chọn B: Ngrok
1. Tải [ngrok](https://ngrok.com) về máy hoặc cài qua `winget install ngrok/ngrok`.
2. Mở cổng:
   ```bash
   ngrok http 8080
   ```
   *(Cung cấp link dạng `https://xxxx.ngrok-free.app`).*

---

## 4. Cách kết thúc phiên demo

1. **Tắt đường link công khai:** Tại cửa sổ PowerShell đang chạy `ssh` (hoặc `cloudflared`), nhấn tổ hợp phím **`Ctrl + C`**. Link sẽ ngừng hoạt động ngay lập tức.
2. **Dừng container Docker:** Tại thư mục dự án, chạy lệnh:
   ```bash
   docker compose stop
   ```
3. Khi muốn chạy lại lần sau, chỉ cần lặp lại Bước 1 và Bước 2.
