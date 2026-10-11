\# Tài liệu quản lý cơ sở dữ liệu — Secondhand Marketplace



\## 1. Mục đích



Thư mục này lưu trữ các tập lệnh SQL được quản lý bằng Git, phục vụ việc tạo và cấu hình cơ sở dữ liệu cho dự án Secondhand Marketplace.



Cấu trúc hiện tại là \*\*đề xuất để nhóm xem xét và thống nhất\*\*. Nội dung SQL sẽ được triển khai sau khi cấu trúc cơ sở dữ liệu và cách cấu hình SQL Server được thống nhất.



\## 2. Cấu trúc các tập lệnh



| Tệp | Chức năng |

|---|---|

| `01\_create\_database.sql` | Tạo cơ sở dữ liệu |

| `02\_create\_tables.sql` | Tạo các bảng |

| `03\_constraints.sql` | Khai báo khóa và các ràng buộc dữ liệu |

| `04\_indexes.sql` | Tạo các chỉ mục |

| `05\_views.sql` | Khai báo các khung nhìn nếu cần |

| `06\_procedures.sql` | Khai báo các thủ tục lưu trữ nếu cần |

| `07\_functions.sql` | Khai báo các hàm do người dùng định nghĩa nếu cần |

| `08\_triggers.sql` | Khai báo các trình kích hoạt nếu cần |

| `09\_seed\_data.sql` | Thêm dữ liệu khởi tạo, phối hợp với thành viên phụ trách task 3.9 |

| `run-db.bat` | Chạy các tập lệnh thiết lập cơ sở dữ liệu theo thứ tự đã thống nhất |

| `reset-db.bat` | Đặt lại cơ sở dữ liệu phát triển cục bộ |

| `README.md` | Hướng dẫn sử dụng và mô tả các tập lệnh |



\## 3. Nguyên tắc phối hợp



\- Thống nhất cấu trúc cơ sở dữ liệu với người phụ trách thiết kế và tổng hợp schema trước khi viết SQL tạo bảng.

\- Phối hợp với thành viên phụ trách task 3.9 để tránh trùng lặp dữ liệu khởi tạo.

\- Thống nhất tên cơ sở dữ liệu, SQL Server instance và phương thức chạy tập lệnh trước khi hoàn thiện hai tệp `.bat`.

\- Kiểm tra kỹ thao tác đặt lại cơ sở dữ liệu vì thao tác này có thể làm mất dữ liệu cục bộ.

\- Mọi thay đổi về bảng, cột và ràng buộc cần được trao đổi với nhóm để cập nhật thiết kế cơ sở dữ liệu tương ứng.



\## 4. Trạng thái thực hiện



\- \[x] Tạo cấu trúc thư mục và các tệp dự kiến.

\- \[ ] Nhóm xem xét và thống nhất cấu trúc.

\- \[ ] Triển khai nội dung SQL theo schema đã được duyệt.

\- \[ ] Hoàn thiện tập lệnh thiết lập và đặt lại cơ sở dữ liệu.

\- \[ ] Kiểm thử trên môi trường phát triển cục bộ.



\## 5. Lưu ý



Các tệp SQL và `.bat` hiện mới là khung ban đầu, chưa chứa lệnh thực thi hoàn chỉnh. Không chạy các tệp này để thiết lập hoặc xóa cơ sở dữ liệu cho đến khi nội dung được triển khai và kiểm thử.

