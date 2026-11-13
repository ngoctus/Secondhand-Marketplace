import sys
import os
sys.path.append(os.path.dirname(__file__))
from builder import DrawioDiagramBuilder

builder = DrawioDiagramBuilder()

# ==============================================================================
# PAGE 1: DFD MỨC NGỮ CẢNH (CONTEXT DIAGRAM)
# ==============================================================================
p1_elements = [
    {
        'id': 'p1_title', 'type': 'title',
        'label': '<b>SƠ ĐỒ LUỒNG DỮ LIỆU (DFD) - MỨC NGỮ CẢNH (CONTEXT DIAGRAM)</b><br/>Hệ thống Sàn Giao dịch Đồ Cũ (SecondHand Marketplace - Re:Market)',
        'x': 50, 'y': 30, 'w': 1300, 'h': 50
    },
    {
        'id': 'p0_system', 'type': 'process',
        'label': '<b style="font-size:14px;">0.0</b><br/><br/><b style="font-size:13px; color:#002244;">HỆ THỐNG SÀN GIAO DỊCH<br/>ĐỒ CŨ TRỰC TUYẾN<br/>(RE:MARKET)</b><br/><br/><i>ASP.NET Core 10 & SQL Server</i>',
        'x': 540, 'y': 300, 'w': 320, 'h': 240,
        'custom_style': 'shape=ellipse;whiteSpace=wrap;html=1;fillColor=#cce5ff;strokeColor=#336699;strokeWidth=3;fontFamily=Helvetica;shadow=1;'
    },
    {
        'id': 'ent_buyer', 'type': 'entity',
        'label': '<b style="font-size:13px;">TÁC NHÂN NGOÀI:</b><br/><b style="font-size:14px; color:#4a148c;">NGƯỜI MUA</b><br/><i>(Buyer)</i>',
        'x': 60, 'y': 160, 'w': 180, 'h': 100
    },
    {
        'id': 'ent_seller', 'type': 'entity',
        'label': '<b style="font-size:13px;">TÁC NHÂN NGOÀI:</b><br/><b style="font-size:14px; color:#4a148c;">NGƯỜI BÁN</b><br/><i>(Seller / Shop Owner)</i>',
        'x': 60, 'y': 560, 'w': 180, 'h': 100
    },
    {
        'id': 'ent_admin', 'type': 'entity',
        'label': '<b style="font-size:13px;">TÁC NHÂN NGOÀI:</b><br/><b style="font-size:14px; color:#4a148c;">QUẢN TRỊ VIÊN</b><br/><i>(Administrator)</i>',
        'x': 1160, 'y': 160, 'w': 180, 'h': 100
    },
    {
        'id': 'ent_external', 'type': 'entity',
        'label': '<b style="font-size:13px;">HỆ THỐNG NGOÀI:</b><br/><b style="font-size:13px; color:#004d40;">VIETQR & DỊCH VỤ AI</b><br/><i>(Gemini AI / Ngân hàng)</i>',
        'x': 1160, 'y': 560, 'w': 180, 'h': 100,
        'custom_style': 'rounded=0;whiteSpace=wrap;html=1;fillColor=#d5e8d4;strokeColor=#82b366;fontColor=#1b5e20;fontStyle=1;fontSize=12;fontFamily=Helvetica;strokeWidth=1.5;shadow=1;'
    },
    {
        'id': 'p1_note', 'type': 'note',
        'label': '<b>QUY TẮC BẢO TOÀN DFD (BALANCING):</b><br/>'
                 '• Mọi luồng dữ liệu vào/ra ở Mức ngữ cảnh này được phân rã đầy đủ tại Mức đỉnh (Level 0) và Mức dưới đỉnh (Level 1).<br/>'
                 '• Tác nhân Buyer, Seller, Admin và External Services được giữ nguyên định danh trên mọi sơ đồ.<br/>'
                 '• Đúng nghiệp vụ thực tế của project SecondHand Marketplace (Giao hàng/Gặp mặt, VietQR, Kiểm duyệt AI, Đấu giá, Khiếu nại).',
        'x': 50, 'y': 760, 'w': 1300, 'h': 90
    }
]

p1_connectors = [
    # Buyer -> System
    {'id': 'c1_01', 'source': 'ent_buyer', 'target': 'p0_system', 'exitX': 1.0, 'exitY': 0.25, 'entryX': 0.05, 'entryY': 0.25,
     'label': '1. Đăng ký, đăng nhập, tìm kiếm, lọc theo khu vực'},
    {'id': 'c1_02', 'source': 'ent_buyer', 'target': 'p0_system', 'exitX': 1.0, 'exitY': 0.5, 'entryX': 0.0, 'entryY': 0.45,
     'label': '2. Thêm giỏ hàng, Đặt hàng (chọn Giao hàng/Gặp trực tiếp)'},
    {'id': 'c1_03', 'source': 'ent_buyer', 'target': 'p0_system', 'exitX': 1.0, 'exitY': 0.75, 'entryX': 0.08, 'entryY': 0.65,
     'label': '3. Đặt giá đấu, Xác nhận nhận hàng, Đánh giá, Khiếu nại/Báo cáo'},

    # System -> Buyer
    {'id': 'c1_04', 'source': 'p0_system', 'target': 'ent_buyer', 'exitX': 0.02, 'exitY': 0.35, 'entryX': 0.9, 'entryY': 0.1,
     'label': 'Kết quả tìm kiếm, Chi tiết SP, Mã QR VietQR, Trạng thái đơn, Thông báo', 'waypoints': [(320, 210)]},

    # Seller -> System
    {'id': 'c1_05', 'source': 'ent_seller', 'target': 'p0_system', 'exitX': 1.0, 'exitY': 0.25, 'entryX': 0.08, 'entryY': 0.8,
     'label': '1. Đăng ký shop (Tỉnh/Xã, STK VietQR), Đăng bán/Sửa SP, Soạn mô tả AI'},
    {'id': 'c1_06', 'source': 'ent_seller', 'target': 'p0_system', 'exitX': 1.0, 'exitY': 0.6, 'entryX': 0.2, 'entryY': 0.95,
     'label': '2. Xác nhận đơn, Bắt đầu giao, Hủy đơn, Tạo phiên đấu giá, Phản hồi tranh chấp'},

    # System -> Seller
    {'id': 'c1_07', 'source': 'p0_system', 'target': 'ent_seller', 'exitX': 0.02, 'exitY': 0.9, 'entryX': 0.8, 'entryY': 0.9,
     'label': 'Thông báo đơn mới, Kết quả duyệt SP, Doanh thu, Đánh giá uy tín (vw_DiemCuaHang)', 'waypoints': [(340, 680)]},

    # Admin -> System
    {'id': 'c1_08', 'source': 'ent_admin', 'target': 'p0_system', 'exitX': 0.0, 'exitY': 0.3, 'entryX': 0.95, 'entryY': 0.25,
     'label': '1. Duyệt/Từ chối SP, Quản lý từ khóa cấm, Cấu hình AI'},
    {'id': 'c1_09', 'source': 'ent_admin', 'target': 'p0_system', 'exitX': 0.0, 'exitY': 0.7, 'entryX': 0.98, 'entryY': 0.45,
     'label': '2. Phán quyết tranh chấp (Hoàn tiền/Trả tiền), Xử lý báo cáo, Khóa tài khoản/Shop'},

    # System -> Admin
    {'id': 'c1_10', 'source': 'p0_system', 'target': 'ent_admin', 'exitX': 0.95, 'exitY': 0.15, 'entryX': 0.2, 'entryY': 0.05,
     'label': 'Danh sách chờ duyệt, Báo cáo tranh chấp, Thống kê Dashboard, Audit Logs', 'waypoints': [(1080, 210)]},

    # System <-> External AI / VietQR
    {'id': 'c1_11', 'source': 'p0_system', 'target': 'ent_external', 'exitX': 0.9, 'exitY': 0.8, 'entryX': 0.0, 'entryY': 0.3,
     'label': 'Gửi nội dung phân tích từ khóa cấm / Yêu cầu tạo mã VietQR thanh toán'},
    {'id': 'c1_12', 'source': 'ent_external', 'target': 'p0_system', 'exitX': 0.0, 'exitY': 0.7, 'entryX': 0.8, 'entryY': 0.95,
     'label': 'Kết quả phân tích AI (NONE/LOW/HIGH) / Dữ liệu chuẩn VietQR', 'waypoints': [(1020, 680)]}
]

builder.add_page('page_context', '1. DFD Mức Ngữ Cảnh (Context)', p1_elements, p1_connectors)

# ==============================================================================
# PAGE 2: DFD MỨC ĐỈNH (LEVEL 0 DIAGRAM)
# ==============================================================================
p2_elements = [
    {
        'id': 'p2_title', 'type': 'title',
        'label': '<b>SƠ ĐỒ LUỒNG DỮ LIỆU (DFD) - MỨC ĐỈNH (LEVEL 0)</b><br/>Phân rã Hệ thống Re:Market thành 6 Phân hệ Nghiệp vụ Chính và Các Kho Dữ liệu Chuẩn',
        'x': 50, 'y': 20, 'w': 1300, 'h': 45
    },
    # Entities
    {'id': 'p2_buyer', 'type': 'entity', 'label': '<b>NGƯỜI MUA</b><br/><i>(Buyer)</i>', 'x': 40, 'y': 100, 'w': 140, 'h': 70},
    {'id': 'p2_seller', 'type': 'entity', 'label': '<b>NGƯỜI BÁN</b><br/><i>(Seller)</i>', 'x': 40, 'y': 440, 'w': 140, 'h': 70},
    {'id': 'p2_admin', 'type': 'entity', 'label': '<b>QUẢN TRỊ VIÊN</b><br/><i>(Admin)</i>', 'x': 1220, 'y': 100, 'w': 140, 'h': 70},
    {'id': 'p2_ext', 'type': 'entity', 'label': '<b>AI & VIETQR</b><br/><i>(External Services)</i>', 'x': 1220, 'y': 440, 'w': 140, 'h': 70,
     'custom_style': 'rounded=0;whiteSpace=wrap;html=1;fillColor=#d5e8d4;strokeColor=#82b366;fontColor=#1b5e20;fontStyle=1;fontSize=11;strokeWidth=1.5;'},

    # 6 Core Processes
    {'id': 'proc_1', 'type': 'process', 'label': '<b>1.0</b><br/><b>Quản lý Xác thực & Cửa hàng</b>', 'x': 270, 'y': 95, 'w': 200, 'h': 80},
    {'id': 'proc_2', 'type': 'process', 'label': '<b>2.0</b><br/><b>Quản lý & Kiểm duyệt Sản phẩm</b>', 'x': 570, 'y': 95, 'w': 220, 'h': 80},
    {'id': 'proc_3', 'type': 'process', 'label': '<b>3.0</b><br/><b>Quản lý Đặt hàng & Thanh toán</b>', 'x': 270, 'y': 435, 'w': 210, 'h': 80},
    {'id': 'proc_4', 'type': 'process', 'label': '<b>4.0</b><br/><b>Quản lý Đấu giá Sản phẩm</b>', 'x': 570, 'y': 435, 'w': 210, 'h': 80},
    {'id': 'proc_5', 'type': 'process', 'label': '<b>5.0</b><br/><b>Quản lý Đánh giá & Tranh chấp</b>', 'x': 900, 'y': 435, 'w': 220, 'h': 80},
    {'id': 'proc_6', 'type': 'process', 'label': '<b>6.0</b><br/><b>Quản trị Hệ thống & Thống kê</b>', 'x': 900, 'y': 95, 'w': 220, 'h': 80},

    # Core Data Stores (mapped to SQL tables)
    {'id': 'ds_d1', 'type': 'store', 'label': '<b>D1: dbo.NguoiDung</b>', 'x': 285, 'y': 240, 'w': 170, 'h': 40},
    {'id': 'ds_d2', 'type': 'store', 'label': '<b>D2: dbo.CuaHang</b>', 'x': 285, 'y': 310, 'w': 170, 'h': 40},
    {'id': 'ds_d3', 'type': 'store', 'label': '<b>D3: dbo.SanPham & AnhSP</b>', 'x': 585, 'y': 240, 'w': 190, 'h': 40},
    {'id': 'ds_d4', 'type': 'store', 'label': '<b>D4: dbo.GioHang</b>', 'x': 295, 'y': 560, 'w': 160, 'h': 40},
    {'id': 'ds_d5', 'type': 'store', 'label': '<b>D5: dbo.DonHang & ChiTiet</b>', 'x': 400, 'y': 640, 'w': 200, 'h': 40},
    {'id': 'ds_d6', 'type': 'store', 'label': '<b>D6: dbo.ThanhToan</b>', 'x': 180, 'y': 640, 'w': 160, 'h': 40},
    {'id': 'ds_d7', 'type': 'store', 'label': '<b>D7: dbo.PhienDauGia & Bid</b>', 'x': 595, 'y': 560, 'w': 180, 'h': 40},
    {'id': 'ds_d8', 'type': 'store', 'label': '<b>D8: dbo.TranhChap & BaoCao</b>', 'x': 910, 'y': 560, 'w': 200, 'h': 40},
    {'id': 'ds_d9', 'type': 'store', 'label': '<b>D9: dbo.DanhGia (DiemShop)</b>', 'x': 910, 'y': 640, 'w': 200, 'h': 40},
    {'id': 'ds_d10', 'type': 'store', 'label': '<b>D10: dbo.ThongBao & Audit</b>', 'x': 600, 'y': 730, 'w': 200, 'h': 40},
    {'id': 'ds_d11', 'type': 'store', 'label': '<b>D11: dbo.TuKhoaCam & PhanTichAI</b>', 'x': 890, 'y': 240, 'w': 230, 'h': 40},

    # Bottom notes
    {
        'id': 'p2_note', 'type': 'note',
        'label': '<b>BẢO TOÀN TÁC NHÂN & LUỒNG (LEVEL 0 DFD BALANCING):</b><br/>'
                 '• Toàn bộ 4 tác nhân từ Mức ngữ cảnh kết nối đúng vào 6 tiến trình chức năng tương ứng.<br/>'
                 '• Mỗi tiến trình 1.0 -> 6.0 sẽ được phân rã thành sơ đồ chi tiết Mức dưới đỉnh (Sub-Level 1) ở các trang tiếp theo.<br/>'
                 '• Các kho dữ liệu D1 -> D11 tương ứng chính xác 100% với các bảng và view trong SQL Server của bài tập lớn.',
        'x': 50, 'y': 800, 'w': 1300, 'h': 70
    }
]

p2_connectors = [
    # E_Buyer -> Proc 1, 2, 3, 4, 5
    {'id': 'c2_01', 'source': 'p2_buyer', 'target': 'proc_1', 'label': 'Đăng ký / Đăng nhập / Đổi pass'},
    {'id': 'c2_02', 'source': 'p2_buyer', 'target': 'proc_2', 'label': 'Tìm kiếm & Xem SP', 'waypoints': [(110, 80), (600, 80)]},
    {'id': 'c2_03', 'source': 'p2_buyer', 'target': 'proc_3', 'label': 'Thêm giỏ / Đặt hàng (Giao hàng/Gặp mặt)'},
    {'id': 'c2_04', 'source': 'p2_buyer', 'target': 'proc_4', 'label': 'Tham gia đặt giá bid', 'waypoints': [(110, 420), (590, 420)]},
    {'id': 'c2_05', 'source': 'p2_buyer', 'target': 'proc_5', 'label': 'Đánh giá / Khiếu nại đơn', 'waypoints': [(110, 530), (910, 530)]},

    # E_Seller -> Proc 1, 2, 3, 4, 5
    {'id': 'c2_06', 'source': 'p2_seller', 'target': 'proc_1', 'label': 'Đăng ký Shop (Tỉnh/Xã, STK)', 'waypoints': [(160, 440), (200, 160), (270, 160)]},
    {'id': 'c2_07', 'source': 'p2_seller', 'target': 'proc_2', 'label': 'Đăng/Sửa SP, Soạn mô tả AI', 'waypoints': [(220, 460), (220, 200), (590, 200)]},
    {'id': 'c2_08', 'source': 'p2_seller', 'target': 'proc_3', 'label': 'Xác nhận & Giao hàng (DELIVERED)'},
    {'id': 'c2_09', 'source': 'p2_seller', 'target': 'proc_4', 'label': 'Tạo phiên đấu giá', 'waypoints': [(220, 480), (570, 480)]},
    {'id': 'c2_10', 'source': 'p2_seller', 'target': 'proc_5', 'label': 'Phản hồi khiếu nại', 'waypoints': [(220, 510), (900, 510)]},

    # E_Admin -> Proc 1, 2, 5, 6
    {'id': 'c2_11', 'source': 'p2_admin', 'target': 'proc_1', 'label': 'Khóa/Mở tài khoản & Shop', 'waypoints': [(1290, 80), (370, 80)]},
    {'id': 'c2_12', 'source': 'p2_admin', 'target': 'proc_2', 'label': 'Duyệt / Từ chối SP', 'waypoints': [(1240, 80), (750, 80)]},
    {'id': 'c2_13', 'source': 'p2_admin', 'target': 'proc_5', 'label': 'Phán quyết Tranh chấp & Xử lý báo cáo', 'waypoints': [(1260, 200), (1260, 450), (1120, 450)]},
    {'id': 'c2_14', 'source': 'p2_admin', 'target': 'proc_6', 'label': 'Xem Dashboard / Quản trị hệ thống'},

    # External -> Proc 2, 3
    {'id': 'c2_15', 'source': 'proc_2', 'target': 'p2_ext', 'label': 'Quét từ khóa AI (Gemini)', 'waypoints': [(780, 160), (1240, 160), (1240, 440)]},
    {'id': 'c2_16', 'source': 'proc_3', 'target': 'p2_ext', 'label': 'Tạo mã VietQR chuyển khoản', 'waypoints': [(460, 515), (1220, 515)]},

    # Proc <-> Data Stores
    {'id': 'c2_d01', 'source': 'proc_1', 'target': 'ds_d1', 'label': 'Ghi/Đọc User (BCrypt)'},
    {'id': 'c2_d02', 'source': 'proc_1', 'target': 'ds_d2', 'label': 'Ghi/Đọc Shop'},
    {'id': 'c2_d03', 'source': 'proc_2', 'target': 'ds_d3', 'label': 'Lưu/Cập nhật SP & Ảnh'},
    {'id': 'c2_d04', 'source': 'proc_2', 'target': 'ds_d11', 'label': 'Kiểm tra Từ khóa cấm & Lưu AI'},
    {'id': 'c2_d05', 'source': 'proc_3', 'target': 'ds_d4', 'label': 'sp_ThemVaoGio'},
    {'id': 'c2_d06', 'source': 'proc_3', 'target': 'ds_d5', 'label': 'sp_TaoDonHangTheoShop / Update đơn'},
    {'id': 'c2_d07', 'source': 'proc_3', 'target': 'ds_d6', 'label': 'Ghi sổ ThanhToan'},
    {'id': 'c2_d08', 'source': 'proc_3', 'target': 'ds_d3', 'label': 'Giữ chỗ/Đổi trạng thái RESERVED/SOLD'},
    {'id': 'c2_d09', 'source': 'proc_4', 'target': 'ds_d7', 'label': 'Lưu phiên & lượt bid'},
    {'id': 'c2_d10', 'source': 'proc_4', 'target': 'ds_d5', 'label': 'Tự động tạo đơn người thắng'},
    {'id': 'c2_d11', 'source': 'proc_5', 'target': 'ds_d8', 'label': 'Lưu TranhChap & BaoCao'},
    {'id': 'c2_d12', 'source': 'proc_5', 'target': 'ds_d9', 'label': 'sp_TaoDanhGia & vw_DiemCuaHang'},
    {'id': 'c2_d13', 'source': 'proc_6', 'target': 'ds_d10', 'label': 'Đọc/Ghi Audit Logs & Thống kê'},
]

builder.add_page('page_level0', '2. DFD Mức Đỉnh (Level 0)', p2_elements, p2_connectors)

# ==============================================================================
# PAGE 3: PHÂN RÃ 1.0 - QUẢN LÝ TÀI KHOẢN & CỬA HÀNG
# ==============================================================================
p3_elements = [
    {
        'id': 'p3_title', 'type': 'title',
        'label': '<b>DFD MỨC DƯỚI ĐỈNH (LEVEL 1) - PHÂN RÃ TIẾN TRÌNH 1.0: QUẢN LÝ TÀI KHOẢN & CỬA HÀNG</b><br/>Bảo toàn tác nhân (Người mua, Người bán, Admin) và kho dữ liệu (NguoiDung, CuaHang, NhatKyHeThong)',
        'x': 50, 'y': 20, 'w': 1300, 'h': 45
    },
    # Entities
    {'id': 'p3_buyer', 'type': 'entity', 'label': '<b>NGƯỜI MUA</b><br/><i>(Buyer)</i>', 'x': 50, 'y': 110, 'w': 140, 'h': 70},
    {'id': 'p3_seller', 'type': 'entity', 'label': '<b>NGƯỜI BÁN</b><br/><i>(Seller)</i>', 'x': 50, 'y': 440, 'w': 140, 'h': 70},
    {'id': 'p3_admin', 'type': 'entity', 'label': '<b>QUẢN TRỊ VIÊN</b><br/><i>(Admin)</i>', 'x': 1200, 'y': 260, 'w': 140, 'h': 70},

    # Sub-processes
    {'id': 'p1_1', 'type': 'process', 'label': '<b>1.1</b><br/><b>Đăng ký & Đăng nhập</b><br/><i>Cookie Auth / JWT Bearer<br/>Băm mật khẩu BCrypt</i>', 'x': 290, 'y': 105, 'w': 220, 'h': 80},
    {'id': 'p1_2', 'type': 'process', 'label': '<b>1.2</b><br/><b>Quản lý Hồ sơ & Mật khẩu</b><br/><i>Cập nhật SĐT, Địa chỉ<br/>Đổi mật khẩu</i>', 'x': 290, 'y': 255, 'w': 220, 'h': 80},
    {'id': 'p1_3', 'type': 'process', 'label': '<b>1.3</b><br/><b>Thiết lập & Cập nhật Shop</b><br/><i>sp_TaoCuaHang, TinhThanh<br/>PhuongXa, STK VietQR</i>', 'x': 290, 'y': 435, 'w': 220, 'h': 80},
    {'id': 'p1_4', 'type': 'process', 'label': '<b>1.4</b><br/><b>Quản lý Người dùng & Shop</b><br/><i>Khóa/Mở tài khoản<br/>sp_GhiNhatKy</i>', 'x': 860, 'y': 255, 'w': 230, 'h': 80},

    # Stores
    {'id': 'p3_d1', 'type': 'store', 'label': '<b>D1: dbo.NguoiDung</b><br/>(MaNguoiDung, HoTen, Email, MatKhauBam, VaiTro, TrangThai)', 'x': 590, 'y': 150, 'w': 210, 'h': 60},
    {'id': 'p3_d2', 'type': 'store', 'label': '<b>D2: dbo.CuaHang</b><br/>(MaCuaHang, TenCuaHang, MaTinhThanh, MaPhuongXa, SoTaiKhoan, MaNganHang)', 'x': 590, 'y': 420, 'w': 210, 'h': 65},
    {'id': 'p3_d12', 'type': 'store', 'label': '<b>D12: dbo.NhatKyHeThong</b><br/>(MaNhatKyHeThong, HanhDong, ChiTiet, NguoiThucHien)', 'x': 870, 'y': 440, 'w': 210, 'h': 60},

    # Note
    {
        'id': 'p3_note', 'type': 'note',
        'label': '<b>ĐẶC TẢ NGHIỆP VỤ BẢO TOÀN (1.0):</b><br/>'
                 '• Mật khẩu băm an toàn chuẩn BCrypt. Kiểm tra trạng thái ACTIVE trên từng request.<br/>'
                 '• Cửa hàng hỗ trợ địa chỉ 2 cấp hành chính (TinhThanh, PhuongXa) và thông tin ngân hàng phục vụ VietQR thanh toán.<br/>'
                 '• Mọi thao tác khóa/mở tài khoản của Admin được ghi log tự động vào D12 (NhatKyHeThong).',
        'x': 50, 'y': 620, 'w': 1300, 'h': 75
    }
]

p3_connectors = [
    # Buyer / Seller -> 1.1
    {'id': 'c3_01', 'source': 'p3_buyer', 'target': 'p1_1', 'label': 'Email, Password, Đăng ký/Đăng nhập'},
    {'id': 'c3_02', 'source': 'p1_1', 'target': 'p3_buyer', 'label': 'Cookie phiên / Token JWT, Kết quả', 'waypoints': [(270, 160), (210, 160)]},
    {'id': 'c3_03', 'source': 'p1_1', 'target': 'p3_d1', 'label': 'Kiểm tra & Ghi nhận tài khoản'},
    
    # User -> 1.2
    {'id': 'c3_04', 'source': 'p3_buyer', 'target': 'p1_2', 'label': 'Thông tin cá nhân mới, Đổi pass', 'waypoints': [(170, 175), (170, 280), (290, 280)]},
    {'id': 'c3_05', 'source': 'p1_2', 'target': 'p3_d1', 'label': 'UPDATE Hồ sơ / Mật khẩu mới'},

    # Seller -> 1.3
    {'id': 'c3_06', 'source': 'p3_seller', 'target': 'p1_3', 'label': 'Tên Shop, Mô tả, Tỉnh/Xã, STK VietQR'},
    {'id': 'c3_07', 'source': 'p1_3', 'target': 'p3_seller', 'label': 'Hồ sơ Shop đã lưu, Trạng thái ACTIVE'},
    {'id': 'c3_08', 'source': 'p1_3', 'target': 'p3_d2', 'label': 'sp_TaoCuaHang / Cập nhật địa chỉ & bank'},

    # Admin -> 1.4
    {'id': 'c3_09', 'source': 'p3_admin', 'target': 'p1_4', 'label': 'Lệnh Khóa / Mở tài khoản, duyệt Shop'},
    {'id': 'c3_10', 'source': 'p1_4', 'target': 'p3_admin', 'label': 'Danh sách tài khoản & Kết quả'},
    {'id': 'c3_11', 'source': 'p1_4', 'target': 'p3_d1', 'label': 'Cập nhật TrangThai (ACTIVE / LOCKED)'},
    {'id': 'c3_12', 'source': 'p1_4', 'target': 'p3_d2', 'label': 'Cập nhật TrangThai Shop'},
    {'id': 'c3_13', 'source': 'p1_4', 'target': 'p3_d12', 'label': 'sp_GhiNhatKy (Ghi log kiểm toán)'}
]

builder.add_page('page_sub1', '3. DFD Mức 1 - GĐ1 Tài Khoản & Shop', p3_elements, p3_connectors)

# ==============================================================================
# PAGE 4: PHÂN RÃ 2.0 - SẢN PHẨM & KIỂM DUYỆT
# ==============================================================================
p4_elements = [
    {
        'id': 'p4_title', 'type': 'title',
        'label': '<b>DFD MỨC DƯỚI ĐỈNH (LEVEL 1) - PHÂN RÃ TIẾN TRÌNH 2.0: QUẢN LÝ & KIỂM DUYỆT SẢN PHẨM</b><br/>Bảo toàn luồng Đăng bán, Trợ lý AI gợi ý mô tả, Quét từ khóa cấm (LOW/HIGH), Admin duyệt tin & Tìm kiếm lọc khu vực',
        'x': 50, 'y': 20, 'w': 1300, 'h': 45
    },
    # Entities
    {'id': 'p4_seller', 'type': 'entity', 'label': '<b>NGƯỜI BÁN</b><br/><i>(Seller)</i>', 'x': 50, 'y': 120, 'w': 140, 'h': 70},
    {'id': 'p4_buyer', 'type': 'entity', 'label': '<b>NGƯỜI MUA</b><br/><i>(Buyer)</i>', 'x': 50, 'y': 480, 'w': 140, 'h': 70},
    {'id': 'p4_admin', 'type': 'entity', 'label': '<b>QUẢN TRỊ VIÊN</b><br/><i>(Admin)</i>', 'x': 1200, 'y': 120, 'w': 140, 'h': 70},
    {'id': 'p4_ai_svc', 'type': 'entity', 'label': '<b>TRỢ LÝ AI (GEMINI)</b><br/><i>(Anthropic/Gemini API)</i>', 'x': 1200, 'y': 340, 'w': 140, 'h': 70,
     'custom_style': 'rounded=0;whiteSpace=wrap;html=1;fillColor=#d5e8d4;strokeColor=#82b366;fontColor=#1b5e20;fontStyle=1;fontSize=11;strokeWidth=1.5;'},

    # Sub-processes
    {'id': 'p2_1', 'type': 'process', 'label': '<b>2.1</b><br/><b>Đăng & Quản lý Sản phẩm</b><br/><i>sp_GuiDuyetSanPham<br/>Trợ lý AI gợi ý mô tả</i>', 'x': 270, 'y': 115, 'w': 220, 'h': 80},
    {'id': 'p2_2', 'type': 'process', 'label': '<b>2.2</b><br/><b>Quét Từ khóa & Phân tích AI</b><br/><i>fn_TimTuKhoaCam<br/>ModerationService (LOW/HIGH)</i>', 'x': 600, 'y': 115, 'w': 230, 'h': 80},
    {'id': 'p2_3', 'type': 'process', 'label': '<b>2.3</b><br/><b>Kiểm duyệt Tin đăng (Admin)</b><br/><i>sp_DuyetSanPham<br/>sp_TuChoiSanPham, Ghi vi phạm</i>', 'x': 900, 'y': 115, 'w': 220, 'h': 80},
    {'id': 'p2_4', 'type': 'process', 'label': '<b>2.4</b><br/><b>Tìm kiếm & Lọc Sản phẩm</b><br/><i>Lọc Tỉnh/Thành, Giá, Tình trạng<br/>vw_SanPhamCongKhai</i>', 'x': 270, 'y': 475, 'w': 230, 'h': 80},

    # Stores
    {'id': 'p4_d3', 'type': 'store', 'label': '<b>D3: dbo.SanPham & AnhSanPham</b><br/>(MaSanPham, Ten, Gia, TinhTrang, TrangThai: PENDING_REVIEW, ACTIVE, REJECTED)', 'x': 570, 'y': 300, 'w': 290, 'h': 65},
    {'id': 'p4_d13', 'type': 'store', 'label': '<b>D13: dbo.TuKhoaCam & PhanTichAI</b><br/>(TuKhoa, MucDo: LOW/HIGH, KetQua, DiemViPham, LichSuViPham)', 'x': 600, 'y': 420, 'w': 260, 'h': 60},
    {'id': 'p4_d11', 'type': 'store', 'label': '<b>D11: dbo.ThongBao</b><br/>(MaNguoiDung, TieuDe, NoiDung, Loai)', 'x': 910, 'y': 310, 'w': 200, 'h': 50},

    # Note
    {
        'id': 'p4_note', 'type': 'note',
        'label': '<b>BẢO TOÀN NGHIỆP VỤ KIỂM DUYỆT & TÌM KIẾM (2.0):</b><br/>'
                 '• Sản phẩm mới đăng chuyển PENDING_REVIEW, hệ thống tự động quét từ khóa cấm và phân tích AI.<br/>'
                 '• Mức HIGH: Tự động REJECTED và ghi nhận vi phạm. Mức LOW: Đưa vào hàng đợi Admin xem xét quyết định.<br/>'
                 '• Người mua tìm kiếm theo từ khóa, lọc theo danh mục, khoảng giá và theo 34 Tỉnh/Thành của CuaHang.',
        'x': 50, 'y': 620, 'w': 1300, 'h': 75
    }
]

p4_connectors = [
    # Seller -> 2.1
    {'id': 'c4_01', 'source': 'p4_seller', 'target': 'p2_1', 'label': 'Thông tin SP, Ảnh, Yêu cầu gợi ý mô tả'},
    {'id': 'c4_02', 'source': 'p2_1', 'target': 'p4_ai_svc', 'label': 'Prompt tóm tắt thông số SP', 'waypoints': [(430, 90), (1240, 90), (1240, 340)]},
    {'id': 'c4_03', 'source': 'p4_ai_svc', 'target': 'p2_1', 'label': 'Đoạn mô tả chuẩn SEO & chi tiết', 'waypoints': [(1220, 360), (450, 210)]},
    {'id': 'c4_04', 'source': 'p2_1', 'target': 'p4_d3', 'label': 'INSERT SanPham (PENDING_REVIEW)'},
    {'id': 'c4_05', 'source': 'p2_1', 'target': 'p2_2', 'label': 'Kích hoạt hàng đợi phân tích vi phạm'},

    # 2.2 -> D13, D3, AI
    {'id': 'c4_06', 'source': 'p2_2', 'target': 'p4_d13', 'label': 'fn_TimTuKhoaCam (Đọc từ cấm & Ghi PhanTichAI)'},
    {'id': 'c4_07', 'source': 'p2_2', 'target': 'p4_d3', 'label': 'Nếu vi phạm HIGH -> Tự REJECTED'},
    {'id': 'c4_08', 'source': 'p2_2', 'target': 'p2_3', 'label': 'Chuyển danh sách chờ duyệt sang Admin'},

    # Admin -> 2.3
    {'id': 'c4_09', 'source': 'p4_admin', 'target': 'p2_3', 'label': 'Lệnh Duyệt / Từ chối (Lý do)'},
    {'id': 'c4_10', 'source': 'p2_3', 'target': 'p4_d3', 'label': 'UPDATE TrangThai = ACTIVE / REJECTED'},
    {'id': 'c4_11', 'source': 'p2_3', 'target': 'p4_d11', 'label': 'Tạo thông báo gửi Người bán'},

    # Buyer -> 2.4
    {'id': 'c4_12', 'source': 'p4_buyer', 'target': 'p2_4', 'label': 'Từ khóa, Tỉnh/Thành, Bộ lọc giá'},
    {'id': 'c4_13', 'source': 'p2_4', 'target': 'p4_d3', 'label': 'SELECT vw_SanPhamCongKhai (ACTIVE)'},
    {'id': 'c4_14', 'source': 'p2_4', 'target': 'p4_buyer', 'label': 'Danh sách thẻ sản phẩm & Điểm sao shop'}
]

builder.add_page('page_sub2', '4. DFD Mức 1 - GĐ2 Sản Phẩm & Kiểm Duyệt', p4_elements, p4_connectors)

# ==============================================================================
# PAGE 5: PHÂN RÃ 3.0 - ĐẶT HÀNG, GIAO NHẬN & THANH TOÁN
# ==============================================================================
p5_elements = [
    {
        'id': 'p5_title', 'type': 'title',
        'label': '<b>DFD MỨC DƯỚI ĐỈNH (LEVEL 1) - PHÂN RÃ TIẾN TRÌNH 3.0: GIỎ HÀNG, ĐẶT HÀNG & THANH TOÁN</b><br/>Bảo toàn luồng Giỏ hàng, Đặt hàng theo Shop, 2 Hình thức nhận (Giao hàng / Gặp trực tiếp), Thanh toán VietQR & Vòng đời Đơn',
        'x': 50, 'y': 20, 'w': 1300, 'h': 45
    },
    # Entities
    {'id': 'p5_buyer', 'type': 'entity', 'label': '<b>NGƯỜI MUA</b><br/><i>(Buyer)</i>', 'x': 50, 'y': 110, 'w': 140, 'h': 70},
    {'id': 'p5_seller', 'type': 'entity', 'label': '<b>NGƯỜI BÁN</b><br/><i>(Seller)</i>', 'x': 50, 'y': 450, 'w': 140, 'h': 70},
    {'id': 'p5_vietqr', 'type': 'entity', 'label': '<b>CỔNG VIETQR</b><br/><i>(Ngân hàng Napas)</i>', 'x': 1200, 'y': 450, 'w': 140, 'h': 70,
     'custom_style': 'rounded=0;whiteSpace=wrap;html=1;fillColor=#d5e8d4;strokeColor=#82b366;fontColor=#1b5e20;fontStyle=1;fontSize=11;strokeWidth=1.5;'},

    # Sub-processes
    {'id': 'p3_1', 'type': 'process', 'label': '<b>3.1</b><br/><b>Quản lý Giỏ hàng</b><br/><i>sp_ThemVaoGio, Chọn món<br/>Xóa món, Chặn tự mua hàng mình</i>', 'x': 270, 'y': 105, 'w': 220, 'h': 80},
    {'id': 'p3_2', 'type': 'process', 'label': '<b>3.2</b><br/><b>Đặt hàng theo Shop</b><br/><i>sp_TaoDonHangTheoShop<br/>Hình thức: GIAO_HANG / GAP_TRUC_TIEP</i>', 'x': 570, 'y': 105, 'w': 240, 'h': 80},
    {'id': 'p3_3', 'type': 'process', 'label': '<b>3.3</b><br/><b>Khởi tạo Thanh toán</b><br/><i>COD / VietQR Bank Transfer<br/>Tạo mã QR kèm nội dung CK</i>', 'x': 900, 'y': 105, 'w': 220, 'h': 80},
    {'id': 'p3_4', 'type': 'process', 'label': '<b>3.4</b><br/><b>Cập nhật Vòng đời Đơn</b><br/><i>sp_CapNhatTrangThaiDon<br/>CONFIRMED->SHIPPING->DELIVERED->COMPLETED</i>', 'x': 570, 'y': 445, 'w': 240, 'h': 80},

    # Stores
    {'id': 'p5_d4', 'type': 'store', 'label': '<b>D4: dbo.GioHang</b><br/>(MaNguoiDung, MaSanPham, DaChon)', 'x': 285, 'y': 260, 'w': 190, 'h': 50},
    {'id': 'p5_d5', 'type': 'store', 'label': '<b>D5: dbo.DonHang & ChiTietDonHang</b><br/>(MaDonHang, HinhThucNhan, KhungGioNhan, TrangThai, DangGiuCho)', 'x': 570, 'y': 255, 'w': 240, 'h': 65},
    {'id': 'p5_d6', 'type': 'store', 'label': '<b>D6: dbo.ThanhToan</b><br/>(MaDonHang, PhuongThuc, TongTien, TrangThaiThanhToan: PENDING, SUCCESS)', 'x': 900, 'y': 260, 'w': 220, 'h': 55},
    {'id': 'p5_d3', 'type': 'store', 'label': '<b>D3: dbo.SanPham</b><br/>(Trạng thái: ACTIVE -> RESERVED -> SOLD)', 'x': 270, 'y': 445, 'w': 210, 'h': 55},
    {'id': 'p5_d11', 'type': 'store', 'label': '<b>D11: dbo.ThongBao</b><br/>(Thông báo đơn hàng buyer & seller)', 'x': 910, 'y': 455, 'w': 200, 'h': 50},

    # Note
    {
        'id': 'p5_note', 'type': 'note',
        'label': '<b>BẢO TOÀN QUY TẮC CHUYỂN TRẠNG THÁI & HÌNH THỨC NHẬN HÀNG (3.0):</b><br/>'
                 '• Đặt hàng tự động chuyển SP sang RESERVED và DangGiuCho=1 để chống mua trùng.<br/>'
                 '• Đơn GIAO_HANG: Người bán giao -> DELIVERED, Người mua nhận hàng -> COMPLETED.<br/>'
                 '• Đơn GAP_TRUC_TIEP: Phí ship = 0đ, Người bán xác nhận hẹn gặp -> Người mua gặp & bấm Hoàn tất trực tiếp.<br/>'
                 '• Khi đơn COMPLETED: SP đổi sang SOLD, DangGiuCho=0, ThanhToan sang SUCCESS.',
        'x': 50, 'y': 620, 'w': 1300, 'h': 75
    }
]

p5_connectors = [
    # Buyer -> 3.1
    {'id': 'c5_01', 'source': 'p5_buyer', 'target': 'p3_1', 'label': 'Thêm SP, Tích chọn món cần mua'},
    {'id': 'c5_02', 'source': 'p3_1', 'target': 'p5_d4', 'label': 'Ghi nhận dòng giỏ hàng'},

    # 3.1 -> 3.2
    {'id': 'c5_03', 'source': 'p3_1', 'target': 'p3_2', 'label': 'Chuyển các món đã chọn sang Checkout'},
    {'id': 'c5_04', 'source': 'p5_buyer', 'target': 'p3_2', 'label': 'Chọn GIAO_HANG/GAP_TRUC_TIEP, Khung giờ, Địa chỉ'},
    {'id': 'c5_05', 'source': 'p3_2', 'target': 'p5_d5', 'label': 'INSERT DonHang & ChiTietDonHang (PENDING)'},
    {'id': 'c5_06', 'source': 'p3_2', 'target': 'p5_d3', 'label': 'UPDATE SanPham (RESERVED, DangGiuCho=1)'},

    # 3.2 -> 3.3
    {'id': 'c5_07', 'source': 'p3_2', 'target': 'p3_3', 'label': 'Mã đơn & Tổng tiền cần thanh toán'},
    {'id': 'c5_08', 'source': 'p3_3', 'target': 'p5_d6', 'label': 'INSERT ThanhToan (COD/BANK_TRANSFER)'},
    {'id': 'c5_09', 'source': 'p3_3', 'target': 'p5_vietqr', 'label': 'Tạo chuỗi VietQR chuẩn Napas'},
    {'id': 'c5_10', 'source': 'p3_3', 'target': 'p5_buyer', 'label': 'Mã đơn, Mã QR thanh toán & Hóa đơn', 'waypoints': [(960, 80), (120, 80)]},

    # Vòng đời đơn: Seller / Buyer -> 3.4
    {'id': 'c5_11', 'source': 'p5_seller', 'target': 'p3_4', 'label': 'Xác nhận (CONFIRMED) / Giao (SHIPPING/DELIVERED)'},
    {'id': 'c5_12', 'source': 'p5_buyer', 'target': 'p3_4', 'label': 'Xác nhận Đã nhận hàng / Hoàn tất (COMPLETED)', 'waypoints': [(160, 200), (160, 420), (570, 420)]},
    {'id': 'c5_13', 'source': 'p3_4', 'target': 'p5_d5', 'label': 'sp_CapNhatTrangThaiDon (Ghi LichSuTrangThaiDon)'},
    {'id': 'c5_14', 'source': 'p3_4', 'target': 'p5_d3', 'label': 'COMPLETED -> SP=SOLD / CANCELLED -> SP=ACTIVE'},
    {'id': 'c5_15', 'source': 'p3_4', 'target': 'p5_d6', 'label': 'Đồng bộ ThanhToan (PAID / SUCCESS / REFUNDED)'},
    {'id': 'c5_16', 'source': 'p3_4', 'target': 'p5_d11', 'label': 'Bắn thông báo đơn hàng cho cả 2 bên'}
]

builder.add_page('page_sub3', '5. DFD Mức 1 - GĐ3 Đặt Hàng & Thanh Toán', p5_elements, p5_connectors)

# ==============================================================================
# PAGE 6: PHÂN RÃ 4.0 - ĐẤU GIÁ SẢN PHẨM
# ==============================================================================
p6_elements = [
    {
        'id': 'p6_title', 'type': 'title',
        'label': '<b>DFD MỨC DƯỚI ĐỈNH (LEVEL 1) - PHÂN RÃ TIẾN TRÌNH 4.0: QUẢN LÝ ĐẤU GIÁ SẢN PHẨM</b><br/>Bảo toàn luồng Tạo phiên đấu giá, Khóa sản phẩm (trg_SanPham_ChanRutKhoiDauGia), Đặt giá đấu Realtime & Auto-sweeper chốt phiên sinh đơn',
        'x': 50, 'y': 20, 'w': 1300, 'h': 45
    },
    # Entities
    {'id': 'p6_seller', 'type': 'entity', 'label': '<b>NGƯỜI BÁN</b><br/><i>(Seller)</i>', 'x': 50, 'y': 120, 'w': 140, 'h': 70},
    {'id': 'p6_buyer', 'type': 'entity', 'label': '<b>NGƯỜI MUA</b><br/><i>(Buyer / Bidder)</i>', 'x': 50, 'y': 450, 'w': 140, 'h': 70},
    {'id': 'p6_sweeper', 'type': 'entity', 'label': '<b>TIẾN TRÌNH NỀN</b><br/><i>(Auction Sweeper Service)</i>', 'x': 1200, 'y': 280, 'w': 140, 'h': 70,
     'custom_style': 'rounded=0;whiteSpace=wrap;html=1;fillColor=#ffe6cc;strokeColor=#d79b00;fontColor=#b45f06;fontStyle=1;fontSize=11;strokeWidth=1.5;'},

    # Sub-processes
    {'id': 'p4_1', 'type': 'process', 'label': '<b>4.1</b><br/><b>Tạo Phiên Đấu Giá</b><br/><i>Thiết lập Giá khởi điểm,<br/>Bước giá, Giờ Bắt đầu/Kết thúc</i>', 'x': 280, 'y': 115, 'w': 220, 'h': 80},
    {'id': 'p4_2', 'type': 'process', 'label': '<b>4.2</b><br/><b>Đặt Giá Đấu (Bid)</b><br/><i>sp_DatGia, Kiểm tra bước giá,<br/>Polling 3s cập nhật giá cao nhất</i>', 'x': 280, 'y': 445, 'w': 220, 'h': 80},
    {'id': 'p4_3', 'type': 'process', 'label': '<b>4.3</b><br/><b>Giám sát Trạng thái Phiên</b><br/><i>SCHEDULED -> RUNNING<br/>(Khi đến thời gian bắt đầu)</i>', 'x': 600, 'y': 275, 'w': 220, 'h': 80},
    {'id': 'p4_4', 'type': 'process', 'label': '<b>4.4</b><br/><b>Chốt Phiên & Tự Sinh Đơn</b><br/><i>sp_KetThucPhienDauGia<br/>Tạo DonHang cho người thắng</i>', 'x': 890, 'y': 275, 'w': 240, 'h': 80},

    # Stores
    {'id': 'p6_d3', 'type': 'store', 'label': '<b>D3: dbo.SanPham</b><br/>(Khóa rút sản phẩm khỏi sàn)', 'x': 600, 'y': 125, 'w': 210, 'h': 50},
    {'id': 'p6_d7', 'type': 'store', 'label': '<b>D7: dbo.PhienDauGia & LuotDatGia</b><br/>(MaPhien, GiaHienTai, NguoiGiuGia, ThoiGianKetThuc, TrangThai)', 'x': 590, 'y': 440, 'w': 240, 'h': 65},
    {'id': 'p6_d5', 'type': 'store', 'label': '<b>D5: dbo.DonHang</b><br/>(Đơn hàng sinh tự động giá trúng)', 'x': 905, 'y': 445, 'w': 210, 'h': 55},
    {'id': 'p6_d11', 'type': 'store', 'label': '<b>D11: dbo.ThongBao</b><br/>(Thông báo outbid & thắng đấu giá)', 'x': 905, 'y': 125, 'w': 210, 'h': 50},

    # Note
    {
        'id': 'p6_note', 'type': 'note',
        'label': '<b>ĐẶC TẢ BẢO TOÀN PHÂN HỆ ĐẤU GIÁ (4.0):</b><br/>'
                 '• Trigger trg_SanPham_ChanRutKhoiDauGia ngăn chặn người bán xóa hoặc sửa sản phẩm đang có phiên đấu giá.<br/>'
                 '• sp_DatGia kiểm tra hợp lệ: không cho chủ shop tự đấu giá sản phẩm của mình, giá bid phải cao hơn giá hiện tại + bước giá.<br/>'
                 '• AuctionBackgroundService quét định kỳ: đến giờ chốt phiên, tự gọi sp_KetThucPhienDauGia để tạo đơn hàng gán người thắng.',
        'x': 50, 'y': 620, 'w': 1300, 'h': 75
    }
]

p6_connectors = [
    # Seller -> 4.1
    {'id': 'c6_01', 'source': 'p6_seller', 'target': 'p4_1', 'label': 'Chọn SP, Giá sàn, Bước giá, Thời gian'},
    {'id': 'c6_02', 'source': 'p4_1', 'target': 'p6_d3', 'label': 'Đánh dấu SP tham gia đấu giá (khóa rút)'},
    {'id': 'c6_03', 'source': 'p4_1', 'target': 'p6_d7', 'label': 'INSERT PhienDauGia (SCHEDULED)'},

    # Buyer -> 4.2
    {'id': 'c6_04', 'source': 'p6_buyer', 'target': 'p4_2', 'label': 'Mức giá bid mới (> Giá hiện tại + Bước)'},
    {'id': 'c6_05', 'source': 'p4_2', 'target': 'p6_d7', 'label': 'sp_DatGia (INSERT LuotDatGia, UPDATE GiaHienTai)'},
    {'id': 'c6_06', 'source': 'p4_2', 'target': 'p6_d11', 'label': 'Báo outbid cho người bị vượt giá'},
    {'id': 'c6_07', 'source': 'p6_d7', 'target': 'p4_2', 'label': 'Polling 3s cập nhật giá & bảng bid', 'waypoints': [(590, 480), (500, 480)]},

    # Sweeper -> 4.3 & 4.4
    {'id': 'c6_08', 'source': 'p6_sweeper', 'target': 'p4_3', 'label': 'Quét kích hoạt phiên đến giờ'},
    {'id': 'c6_09', 'source': 'p4_3', 'target': 'p6_d7', 'label': 'UPDATE TrangThai = RUNNING'},
    {'id': 'c6_10', 'source': 'p6_sweeper', 'target': 'p4_4', 'label': 'Quét chốt phiên đã hết thời gian'},
    {'id': 'c6_11', 'source': 'p4_4', 'target': 'p6_d7', 'label': 'UPDATE TrangThai = ENDED'},
    {'id': 'c6_12', 'source': 'p4_4', 'target': 'p6_d5', 'label': 'sp_KetThucPhienDauGia (Tự sinh DonHang cho winner)'},
    {'id': 'c6_13', 'source': 'p4_4', 'target': 'p6_d11', 'label': 'Bắn thông báo trúng đấu giá cho Người mua'}
]

builder.add_page('page_sub4', '6. DFD Mức 1 - GĐ4 Đấu Giá Sản Phẩm', p6_elements, p6_connectors)

# ==============================================================================
# PAGE 7: PHÂN RÃ 5.0 - HẬU MÃI, ĐÁNH GIÁ, TRANH CHẤP & BÁO CÁO
# ==============================================================================
p7_elements = [
    {
        'id': 'p7_title', 'type': 'title',
        'label': '<b>DFD MỨC DƯỚI ĐỈNH (LEVEL 1) - PHÂN RÃ TIẾN TRÌNH 5.0: ĐÁNH GIÁ, TRANH CHẤP & BÁO CÁO</b><br/>Bảo toàn luồng Đánh giá sau mua (sp_TaoDanhGia), View điểm shop (vw_DiemCuaHang), Khiếu nại tranh chấp đơn & Admin phán quyết',
        'x': 50, 'y': 20, 'w': 1300, 'h': 45
    },
    # Entities
    {'id': 'p7_buyer', 'type': 'entity', 'label': '<b>NGƯỜI MUA</b><br/><i>(Buyer)</i>', 'x': 50, 'y': 110, 'w': 140, 'h': 70},
    {'id': 'p7_seller', 'type': 'entity', 'label': '<b>NGƯỜI BÁN</b><br/><i>(Seller)</i>', 'x': 50, 'y': 450, 'w': 140, 'h': 70},
    {'id': 'p7_admin', 'type': 'entity', 'label': '<b>QUẢN TRỊ VIÊN</b><br/><i>(Admin)</i>', 'x': 1200, 'y': 260, 'w': 140, 'h': 70},

    # Sub-processes
    {'id': 'p5_1', 'type': 'process', 'label': '<b>5.1</b><br/><b>Đánh giá & Điểm Shop</b><br/><i>sp_TaoDanhGia (1-5 sao)<br/>vw_DiemCuaHang (chỉ tính VISIBLE)</i>', 'x': 280, 'y': 105, 'w': 220, 'h': 80},
    {'id': 'p5_2', 'type': 'process', 'label': '<b>5.2</b><br/><b>Mở Tranh chấp Đơn hàng</b><br/><i>Người mua khiếu nại<br/>Người bán gửi phản hồi</i>', 'x': 280, 'y': 365, 'w': 220, 'h': 80},
    {'id': 'p5_3', 'type': 'process', 'label': '<b>5.3</b><br/><b>Báo cáo Vi phạm</b><br/><i>Báo cáo Sản phẩm / Shop<br/>Chống trùng báo cáo đang chờ</i>', 'x': 280, 'y': 485, 'w': 220, 'h': 70},
    {'id': 'p5_4', 'type': 'process', 'label': '<b>5.4</b><br/><b>Phán quyết Tranh chấp & Xử lý</b><br/><i>REFUND_BUYER (Hoàn tiền)<br/>PAY_SELLER (Giải phóng tiền), Gỡ SP</i>', 'x': 860, 'y': 255, 'w': 240, 'h': 85},

    # Stores
    {'id': 'p7_d10', 'type': 'store', 'label': '<b>D10: dbo.DanhGia (vw_DiemCuaHang)</b><br/>(MaDonHang, SoSao: 1-5, NoiDung, TrangThai: VISIBLE/HIDDEN)', 'x': 570, 'y': 115, 'w': 240, 'h': 60},
    {'id': 'p7_d8', 'type': 'store', 'label': '<b>D8: dbo.TranhChap & dbo.BaoCao</b><br/>(MaDonHang, LyDo, BangChung, KetQua, TrangThai: OPEN/RESOLVED/REJECTED)', 'x': 570, 'y': 380, 'w': 240, 'h': 65},
    {'id': 'p7_d5', 'type': 'store', 'label': '<b>D5: dbo.DonHang & D6: ThanhToan</b><br/>(Cập nhật hoàn tiền hoặc trả tiền cho shop)', 'x': 870, 'y': 440, 'w': 220, 'h': 55},
    {'id': 'p7_d11', 'type': 'store', 'label': '<b>D11: dbo.ThongBao & D12: Audit</b><br/>(Ghi nhật ký phán quyết & gửi thông báo)', 'x': 880, 'y': 115, 'w': 210, 'h': 55},

    # Note
    {
        'id': 'p7_note', 'type': 'note',
        'label': '<b>ĐẶC TẢ BẢO TOÀN PHÂN HỆ HẬU MÃI (5.0):</b><br/>'
                 '• sp_TaoDanhGia chỉ cho phép đánh giá đơn COMPLETED. Đánh giá bị ẩn (HIDDEN) tự động bị loại khỏi vw_DiemCuaHang.<br/>'
                 '• Tranh chấp ràng buộc 1 đơn chỉ có 1 tranh chấp OPEN/PROCESSING.<br/>'
                 '• Admin phán quyết: REFUND_BUYER hủy đơn + hoàn tiền + mở khóa giữ chỗ sản phẩm; PAY_SELLER giải phóng tiền thanh toán cho shop.',
        'x': 50, 'y': 620, 'w': 1300, 'h': 75
    }
]

p7_connectors = [
    # Buyer -> 5.1
    {'id': 'c7_01', 'source': 'p7_buyer', 'target': 'p5_1', 'label': 'Số sao (1-5), Nhận xét đơn COMPLETED'},
    {'id': 'c7_02', 'source': 'p5_1', 'target': 'p7_d10', 'label': 'sp_TaoDanhGia (INSERT DanhGia)'},
    {'id': 'c7_03', 'source': 'p7_d10', 'target': 'p5_1', 'label': 'Tính lại vw_DiemCuaHang cho shop'},

    # Buyer / Seller -> 5.2
    {'id': 'c7_04', 'source': 'p7_buyer', 'target': 'p5_2', 'label': 'Yêu cầu mở tranh chấp, Bằng chứng lỗi'},
    {'id': 'c7_05', 'source': 'p7_seller', 'target': 'p5_2', 'label': 'Gửi phản hồi giải trình tranh chấp', 'waypoints': [(180, 460), (280, 410)]},
    {'id': 'c7_06', 'source': 'p5_2', 'target': 'p7_d8', 'label': 'INSERT TranhChap (OPEN)'},

    # Buyer / Seller -> 5.3
    {'id': 'c7_07', 'source': 'p7_buyer', 'target': 'p5_3', 'label': 'Báo cáo SP vi phạm / Gian lận', 'waypoints': [(160, 180), (160, 520), (280, 520)]},
    {'id': 'c7_08', 'source': 'p5_3', 'target': 'p7_d8', 'label': 'INSERT BaoCao (PENDING)'},

    # 5.2 & 5.3 -> 5.4 Admin
    {'id': 'c7_09', 'source': 'p7_d8', 'target': 'p5_4', 'label': 'Danh sách hồ sơ tranh chấp / báo cáo'},
    {'id': 'c7_10', 'source': 'p7_admin', 'target': 'p5_4', 'label': 'Phán quyết (REFUND_BUYER / PAY_SELLER / REJECT)'},
    {'id': 'c7_11', 'source': 'p5_4', 'target': 'p7_d8', 'label': 'UPDATE TranhChap = RESOLVED'},
    {'id': 'c7_12', 'source': 'p5_4', 'target': 'p7_d5', 'label': 'Cập nhật DonHang (CANCELLED/REFUNDED) & ThanhToan'},
    {'id': 'c7_13', 'source': 'p5_4', 'target': 'p7_d11', 'label': 'sp_GhiNhatKy + Bắn thông báo kết quả cho 2 bên'}
]

builder.add_page('page_sub5', '7. DFD Mức 1 - GĐ5 Đánh Giá & Tranh Chấp', p7_elements, p7_connectors)

# ==============================================================================
# PAGE 8: PHÂN RÃ 6.0 - QUẢN TRỊ HỆ THỐNG & AUDIT
# ==============================================================================
p8_elements = [
    {
        'id': 'p8_title', 'type': 'title',
        'label': '<b>DFD MỨC DƯỚI ĐỈNH (LEVEL 1) - PHÂN RÃ TIẾN TRÌNH 6.0: QUẢN TRỊ HỆ THỐNG & AUDIT</b><br/>Bảo toàn luồng Quản lý danh mục, Cấu hình AI & Từ khóa cấm, Giám sát Nhật ký hệ thống (Audit Logs) & Dashboard Thống kê',
        'x': 50, 'y': 20, 'w': 1300, 'h': 45
    },
    # Entities
    {'id': 'p8_admin', 'type': 'entity', 'label': '<b>QUẢN TRỊ VIÊN</b><br/><i>(Administrator)</i>', 'x': 50, 'y': 280, 'w': 150, 'h': 80},

    # Sub-processes
    {'id': 'p6_1', 'type': 'process', 'label': '<b>6.1</b><br/><b>Quản lý Danh mục & Cấu hình</b><br/><i>CRUD Danh mục hàng hóa<br/>Cấu hình AI (CauHinhAI), Từ khóa cấm</i>', 'x': 320, 'y': 105, 'w': 250, 'h': 80},
    {'id': 'p6_2', 'type': 'process', 'label': '<b>6.2</b><br/><b>Giám sát Nhật ký Kiểm toán</b><br/><i>sp_GhiNhatKy, dbo.NhatKyHeThong<br/>Lọc theo User, Hành động, Thời gian</i>', 'x': 320, 'y': 280, 'w': 250, 'h': 80},
    {'id': 'p6_3', 'type': 'process', 'label': '<b>6.3</b><br/><b>Tổng hợp Thống kê & Báo cáo</b><br/><i>vw_ThongKeHeThong, Doanh thu<br/>Biểu đồ Chart.js, Tỷ lệ đơn thành công</i>', 'x': 320, 'y': 455, 'w': 250, 'h': 80},

    # Stores
    {'id': 'p8_d_cfg', 'type': 'store', 'label': '<b>D: DanhMuc, TuKhoaCam & CauHinhAI</b><br/>(Bảng cấu hình hệ thống & từ khóa cấm)', 'x': 720, 'y': 115, 'w': 280, 'h': 60},
    {'id': 'p8_d12', 'type': 'store', 'label': '<b>D12: dbo.NhatKyHeThong</b><br/>(MaNhatKyHeThong, HanhDong, ChiTiet, IP, NgayTao)', 'x': 720, 'y': 290, 'w': 280, 'h': 60},
    {'id': 'p8_d_all', 'type': 'store', 'label': '<b>D: NguoiDung, DonHang, SanPham, DanhGia</b><br/>(Dữ liệu nghiệp vụ phục vụ view vw_ThongKeHeThong)', 'x': 720, 'y': 465, 'w': 290, 'h': 60},

    # Note
    {
        'id': 'p8_note', 'type': 'note',
        'label': '<b>ĐẶC TẢ BẢO TOÀN PHÂN HỆ QUẢN TRỊ & THỐNG KÊ (6.0):</b><br/>'
                 '• Quản lý từ khóa cấm phân cấp độ (LOW/HIGH) trực tiếp can thiệp vào bộ lọc kiểm duyệt tin đăng.<br/>'
                 '• Hệ thống ghi nhận mọi biến động quan trọng (khóa tài khoản, phán quyết tranh chấp, đổi trạng thái) vào NhatKyHeThong.<br/>'
                 '• Dashboard cung cấp số liệu trực quan: tổng số đơn, tỷ lệ giao thành công, doanh thu sàn và top shop uy tín.',
        'x': 50, 'y': 620, 'w': 1300, 'h': 75
    }
]

p8_connectors = [
    # Admin -> 6.1
    {'id': 'c8_01', 'source': 'p8_admin', 'target': 'p6_1', 'label': 'Thêm/Sửa Danh mục, Cấu hình model AI, Thêm từ khóa cấm', 'waypoints': [(125, 280), (125, 145), (320, 145)]},
    {'id': 'c8_02', 'source': 'p6_1', 'target': 'p8_d_cfg', 'label': 'Lưu bảng DanhMuc, TuKhoaCam, CauHinhAI'},
    {'id': 'c8_03', 'source': 'p6_1', 'target': 'p8_admin', 'label': 'Kết quả cập nhật cấu hình', 'waypoints': [(320, 165), (145, 165), (145, 280)]},

    # Admin -> 6.2
    {'id': 'c8_04', 'source': 'p8_admin', 'target': 'p6_2', 'label': 'Tra cứu nhật ký, Bộ lọc theo người dùng/thời gian'},
    {'id': 'c8_05', 'source': 'p8_d12', 'target': 'p6_2', 'label': 'SELECT từ dbo.NhatKyHeThong'},
    {'id': 'c8_06', 'source': 'p6_2', 'target': 'p8_admin', 'label': 'Danh sách Audit Logs có phân trang'},

    # Admin -> 6.3
    {'id': 'c8_07', 'source': 'p8_admin', 'target': 'p6_3', 'label': 'Xem Dashboard / Yêu cầu dữ liệu biểu đồ', 'waypoints': [(125, 360), (125, 495), (320, 495)]},
    {'id': 'c8_08', 'source': 'p8_d_all', 'target': 'p6_3', 'label': 'Đọc vw_ThongKeHeThong & vw_ThongKeCuaHang'},
    {'id': 'c8_09', 'source': 'p6_3', 'target': 'p8_admin', 'label': 'Dữ liệu KPI, Biểu đồ doanh thu, Tỷ lệ đơn hàng', 'waypoints': [(320, 515), (145, 515), (145, 360)]}
]

builder.add_page('page_sub6', '8. DFD Mức 1 - GĐ6 Quản Trị & Audit', p8_elements, p8_connectors)

# ==============================================================================
# SAVE TO BOTH .xml AND .drawio
# ==============================================================================
out_xml = r'c:\Users\Admin\OneDrive\Documents\Secondhand-Marketplace\docs\diagrams\DFD_Secondhand_Marketplace.drawio.xml'
out_drawio = r'c:\Users\Admin\OneDrive\Documents\Secondhand-Marketplace\docs\diagrams\DFD_Secondhand_Marketplace.drawio'
builder.save(out_xml)
builder.save(out_drawio)

print("SUCCESS: Generated 8 comprehensive DFD diagrams.")
