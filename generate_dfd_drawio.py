import xml.etree.ElementTree as ET
import xml.dom.minidom as minidom

class DiagramBuilder:
    def __init__(self, diag_id, name, width=1600, height=1100):
        self.diag_id = diag_id
        self.name = name
        self.width = width
        self.height = height
        self.diagram_elem = ET.Element("diagram", id=diag_id, name=name)
        self.model = ET.SubElement(self.diagram_elem, "mxGraphModel", 
                                   dx="1422", dy="800", grid="1", gridSize="10", guides="1", 
                                   tooltips="1", connect="1", arrows="1", fold="1", page="1", 
                                   pageScale="1", pageWidth=str(width), pageHeight=str(height), 
                                   background="#ffffff", math="0", shadow="0")
        self.root = ET.SubElement(self.model, "root")
        ET.SubElement(self.root, "mxCell", id="0")
        ET.SubElement(self.root, "mxCell", id="1", parent="0")

    def add_entity(self, cell_id, value, x, y, w=160, h=80):
        style = ("rounded=0;whiteSpace=wrap;html=1;fillColor=#dbeafe;strokeColor=#2563eb;"
                 "fontColor=#1e3a8a;fontStyle=1;fontSize=12;shadow=1;arcSize=6;align=center;")
        cell = ET.SubElement(self.root, "mxCell", id=cell_id, value=value, style=style, 
                             vertex="1", parent="1")
        ET.SubElement(cell, "mxGeometry", x=str(x), y=str(y), width=str(w), height=str(h), **{"as": "geometry"})
        return cell

    def add_process(self, cell_id, value, x, y, w=140, h=140):
        style = ("ellipse;whiteSpace=wrap;html=1;aspect=fixed;fillColor=#dcfce7;strokeColor=#16a34a;"
                 "fontColor=#14532d;fontStyle=1;fontSize=11;shadow=1;align=center;")
        cell = ET.SubElement(self.root, "mxCell", id=cell_id, value=value, style=style, 
                             vertex="1", parent="1")
        ET.SubElement(cell, "mxGeometry", x=str(x), y=str(y), width=str(w), height=str(h), **{"as": "geometry"})
        return cell

    def add_store(self, cell_id, value, x, y, w=170, h=50):
        style = ("shape=partialRectangle;top=0;bottom=0;fillColor=#fef3c7;strokeColor=#d97706;"
                 "fontColor=#78350f;fontStyle=1;fontSize=11;align=left;spacingLeft=8;shadow=1;")
        cell = ET.SubElement(self.root, "mxCell", id=cell_id, value=value, style=style, 
                             vertex="1", parent="1")
        ET.SubElement(cell, "mxGeometry", x=str(x), y=str(y), width=str(w), height=str(h), **{"as": "geometry"})
        return cell

    def add_flow(self, cell_id, value, source, target, waypoints=None):
        style = ("edgeStyle=orthogonalEdgeStyle;rounded=1;orthogonalLoop=1;jettySize=auto;html=1;"
                 "strokeColor=#334155;strokeWidth=1.5;endArrow=classic;fontSize=10;fontColor=#0f172a;"
                 "labelBackgroundColor=#ffffff;")
        edge = ET.SubElement(self.root, "mxCell", id=cell_id, value=value, style=style, 
                             edge="1", parent="1", source=source, target=target)
        geo = ET.SubElement(edge, "mxGeometry", relative="1", **{"as": "geometry"})
        if waypoints:
            pts = ET.SubElement(geo, "Array", **{"as": "points"})
            for (px, py) in waypoints:
                ET.SubElement(pts, "mxPoint", x=str(px), y=str(py))
        return edge

def build_all_diagrams():
    mxfile = ET.Element("mxfile", host="app.diagrams.net", modified="2026-11-14T01:40:00.000Z", 
                        agent="Antigravity/2.0 (Windows)", version="24.0.0")

    # =========================================================================
    # TAB 1: DFD Mức 0 - Sơ đồ Ngữ Cảnh (Context Diagram)
    # =========================================================================
    d0 = DiagramBuilder("diag_d0", "DFD Mức 0 - Ngữ Cảnh", 1400, 950)
    
    # Process
    d0.add_process("p0", "<b>0.0 HỆ THỐNG SÀN THƯƠNG MẠI<br>ĐIỆN TỬ ĐỒ CŨ<br>(SECONDHAND MARKETPLACE)</b>", 
                   530, 370, 220, 160)
    
    # External Entities
    d0.add_entity("e1", "<b>E1: Người Mua<br>(Buyer)</b>", 80, 390, 160, 120)
    d0.add_entity("e2", "<b>E2: Người Bán<br>(Seller)</b>", 1080, 390, 160, 120)
    d0.add_entity("e3", "<b>E3: Quản Trị Viên<br>(Admin)</b>", 560, 60, 160, 100)
    d0.add_entity("e4", "<b>E4: Dịch Vụ AI Bên Ngoài<br>(Anthropic Claude API)</b>", 540, 750, 200, 90)

    # Inflows / Outflows for E1
    d0.add_flow("f0_e1_p0_1", "1. Đăng ký / Đăng nhập / Đổi hồ sơ", "e1", "p0", [(290, 390), (290, 400)])
    d0.add_flow("f0_e1_p0_2", "2. Tìm kiếm, Giỏ hàng & Checkout giữ chỗ", "e1", "p0", [(270, 420), (270, 430)])
    d0.add_flow("f0_e1_p0_3", "3. Lượt trả giá đấu giá (Bidding)", "e1", "p0", [(250, 450), (250, 460)])
    d0.add_flow("f0_e1_p0_4", "4. Hủy đơn, Khiếu nại tranh chấp, Đánh giá & Báo cáo", "e1", "p0", [(230, 480), (230, 490)])
    d0.add_flow("f0_e1_p0_5", "5. Câu hỏi tư vấn giá & Chatbot AI", "e1", "p0", [(210, 510), (210, 520)])

    d0.add_flow("f0_p0_e1_1", "1. Kết quả xác thực (Cookie/JWT) & Profile", "p0", "e1", [(290, 370), (290, 370)])
    d0.add_flow("f0_p0_e1_2", "2. DS sản phẩm, Giữ chỗ thành công & Đơn hàng", "p0", "e1", [(270, 340), (270, 340)])
    d0.add_flow("f0_p0_e1_3", "3. Diễn biến đấu giá & Báo trúng thầu", "p0", "e1", [(250, 310), (250, 310)])
    d0.add_flow("f0_p0_e1_4", "4. Kết quả phán quyết tranh chấp & Thông báo chuông", "p0", "e1", [(230, 280), (230, 280)])
    d0.add_flow("f0_p0_e1_5", "5. Phản hồi tư vấn, gợi ý sản phẩm từ AI", "p0", "e1", [(210, 250), (210, 250)])

    # Inflows / Outflows for E2
    d0.add_flow("f0_e2_p0_1", "1. Hồ sơ Cửa hàng (Shop) & Địa chỉ kho", "e2", "p0", [(980, 390), (980, 400)])
    d0.add_flow("f0_e2_p0_2", "2. Đăng bài sản phẩm, Upload ảnh & Gửi duyệt", "e2", "p0", [(1000, 420), (1000, 430)])
    d0.add_flow("f0_e2_p0_3", "3. Cấu hình phòng đấu giá (Giá sàn, Bước giá)", "e2", "p0", [(1020, 450), (1020, 460)])
    d0.add_flow("f0_e2_p0_4", "4. Cập nhật tiến độ đơn (Xác nhận, Giao hàng)", "e2", "p0", [(1040, 480), (1040, 490)])
    d0.add_flow("f0_e2_p0_5", "5. Phản hồi giải trình tranh chấp", "e2", "p0", [(1060, 510), (1060, 520)])

    d0.add_flow("f0_p0_e2_1", "1. Thẩm định Shop & Cấp quyền bán", "p0", "e2", [(980, 370), (980, 370)])
    d0.add_flow("f0_p0_e2_2", "2. Kết quả kiểm duyệt tin (Duyệt/Từ chối + Lý do)", "p0", "e2", [(1000, 340), (1000, 340)])
    d0.add_flow("f0_p0_e2_3", "3. Thông báo Đơn bán mới & Đơn đấu giá thắng", "p0", "e2", [(1020, 310), (1020, 310)])
    d0.add_flow("f0_p0_e2_4", "4. Quyết định tranh chấp & Cảnh báo vi phạm", "p0", "e2", [(1040, 280), (1040, 280)])
    d0.add_flow("f0_p0_e2_5", "5. Thống kê Doanh thu & Báo cáo bán hàng", "p0", "e2", [(1060, 250), (1060, 250)])

    # Inflows / Outflows for E3
    d0.add_flow("f0_e3_p0_1", "1. Khóa/Mở tài khoản & Shop kèm lý do", "e3", "p0", [(580, 250), (580, 260)])
    d0.add_flow("f0_e3_p0_2", "2. CRUD Danh mục & Bảng chuẩn tình trạng", "e3", "p0", [(610, 250), (610, 260)])
    d0.add_flow("f0_e3_p0_3", "3. Quản lý Từ khóa cấm (HIGH/LOW)", "e3", "p0", [(640, 250), (640, 260)])
    d0.add_flow("f0_e3_p0_4", "4. Duyệt/Từ chối sản phẩm & Gỡ vi phạm", "e3", "p0", [(670, 250), (670, 260)])
    d0.add_flow("f0_e3_p0_5", "5. Phán quyết tranh chấp, Báo cáo & Cấu hình AI", "e3", "p0", [(700, 250), (700, 260)])

    d0.add_flow("f0_p0_e3_1", "1. Báo cáo thống kê tổng quan sàn & Doanh thu", "p0", "e3", [(550, 250), (550, 250)])
    d0.add_flow("f0_p0_e3_2", "2. Hàng chờ duyệt sản phẩm (Kèm cờ cảnh báo)", "p0", "e3", [(520, 250), (520, 250)])
    d0.add_flow("f0_p0_e3_3", "3. Danh sách Tranh chấp & Báo cáo vi phạm", "p0", "e3", [(490, 250), (490, 250)])
    d0.add_flow("f0_p0_e3_4", "4. Nhật ký kiểm toán toàn hệ thống (Audit Logs)", "p0", "e3", [(460, 250), (460, 250)])

    # Inflows / Outflows for E4
    d0.add_flow("f0_p0_e4_1", "Yêu cầu phân tích prompt, metadata & Tool Call", "p0", "e4", [(600, 640), (600, 640)])
    d0.add_flow("f0_e4_p0_1", "Kết quả phân tích nhãn vi phạm, Gợi ý định giá & Chat payload", "e4", "p0", [(680, 640), (680, 640)])

    mxfile.append(d0.diagram_elem)

    # =========================================================================
    # TAB 2: DFD Mức 1 - Mức Đỉnh (Level 1 Diagram)
    # =========================================================================
    d1 = DiagramBuilder("diag_d1", "DFD Mức 1 - Mức Đỉnh", 1850, 1350)
    
    # 5 Processes
    d1.add_process("p1", "<b>1.0 QUẢN LÝ TÀI KHOẢN,<br>CỬA HÀNG & HỆ THỐNG</b>", 420, 180, 160, 160)
    d1.add_process("p2", "<b>2.0 ĐĂNG BÁN, KIỂM DUYỆT<br>NỘI DUNG & TRỢ LÝ AI</b>", 1020, 180, 160, 160)
    d1.add_process("p3", "<b>3.0 TÌM KIẾM, GIỎ HÀNG<br>& ĐẶT HÀNG ĐỘC BẢN</b>", 420, 680, 160, 160)
    d1.add_process("p4", "<b>4.0 ĐẤU GIÁ TRỰC TUYẾN<br>& CHỐT PHIÊN TỰ ĐỘNG</b>", 1020, 680, 160, 160)
    d1.add_process("p5", "<b>5.0 XỬ LÝ ĐƠN HÀNG,<br>TRANH CHẤP & HẬU MÃI</b>", 720, 1020, 170, 170)

    # 4 External Entities
    d1.add_entity("e1", "<b>E1: Người Mua<br>(Buyer)</b>", 60, 520, 150, 110)
    d1.add_entity("e2", "<b>E2: Người Bán<br>(Seller)</b>", 1580, 520, 150, 110)
    d1.add_entity("e3", "<b>E3: Quản Trị Viên<br>(Admin)</b>", 730, 40, 160, 90)
    d1.add_entity("e4", "<b>E4: Dịch Vụ AI Anthropic Claude API</b>", 1400, 160, 180, 80)

    # 7 Data Stores
    d1.add_store("ds1", "<b>D1: Người Dùng & Cửa Hàng</b><br>(NguoiDung, CuaHang)", 300, 440, 180, 50)
    d1.add_store("ds2", "<b>D2: Danh Mục & Sản Phẩm</b><br>(DanhMuc, SanPham, AnhSanPham)", 710, 320, 190, 50)
    d1.add_store("ds3", "<b>D3: Giỏ Hàng & Đơn Hàng</b><br>(GioHang, DonHang, ChiTiet)", 480, 930, 180, 50)
    d1.add_store("ds4", "<b>D4: Đấu Giá Trực Tuyến</b><br>(PhienDauGia, LuotDatGia)", 1250, 730, 170, 50)
    d1.add_store("ds5", "<b>D5: Kiểm Duyệt & Vi Phạm</b><br>(TuKhoaCam, LuotKiemDuyet, PhanTichAI)", 1260, 330, 190, 50)
    d1.add_store("ds6", "<b>D6: Khiếu Nại & Đánh Giá</b><br>(TranhChap, BaoCao, DanhGia)", 980, 1020, 180, 50)
    d1.add_store("ds7", "<b>D7: Thông Báo & Nhật Ký</b><br>(ThongBao, NhatKy, CauHinhAI)", 720, 560, 180, 50)

    # Connect Entities to Processes (Bảo toàn luồng Mức 0)
    # E1
    d1.add_flow("f1_e1_p1", "Đăng ký / Đăng nhập / Profile", "e1", "p1")
    d1.add_flow("f1_p1_e1", "Cookie / JWT & Profile info", "p1", "e1")
    d1.add_flow("f1_e1_p2", "Câu hỏi tư vấn & Chatbot AI", "e1", "p2")
    d1.add_flow("f1_p2_e1", "Gợi ý sản phẩm, tư vấn giá AI", "p2", "e1")
    d1.add_flow("f1_e1_p3", "Tìm kiếm, Giỏ hàng & Checkout giữ chỗ", "e1", "p3")
    d1.add_flow("f1_p3_e1", "DS sản phẩm, Giữ chỗ thành công & Hóa đơn", "p3", "e1")
    d1.add_flow("f1_e1_p4", "Lượt trả giá đấu giá (Bidding)", "e1", "p4")
    d1.add_flow("f1_p4_e1", "Diễn biến đấu giá & Báo trúng thầu", "p4", "e1")
    d1.add_flow("f1_e1_p5", "Hủy đơn, Khiếu nại, Đánh giá đơn COMPLETED & Báo cáo", "e1", "p5")
    d1.add_flow("f1_p5_e1", "Kết quả tranh chấp, Thông báo chuông", "p5", "e1")

    # E2
    d1.add_flow("f1_e2_p1", "Hồ sơ mở Shop & Địa chỉ kho", "e2", "p1")
    d1.add_flow("f1_p1_e2", "Kết quả phê duyệt Shop & Quyền bán", "p1", "e2")
    d1.add_flow("f1_e2_p2", "Đăng bán sản phẩm, Upload ảnh & Gửi duyệt", "e2", "p2")
    d1.add_flow("f1_p2_e2", "Kết quả duyệt tin (Duyệt/Từ chối + Lý do)", "p2", "e2")
    d1.add_flow("f1_p3_e2", "Thông báo đơn bán mới từ giỏ hàng", "p3", "e2")
    d1.add_flow("f1_e2_p4", "Cấu hình phòng đấu giá", "e2", "p4")
    d1.add_flow("f1_p4_e2", "Thông báo kết quả chốt phiên & Đơn đấu giá", "p4", "e2")
    d1.add_flow("f1_e2_p5", "Cập nhật tiến độ đơn hàng & Giải trình tranh chấp", "e2", "p5")
    d1.add_flow("f1_p5_e2", "Quyết định tranh chấp, Thống kê doanh thu & Cảnh báo", "p5", "e2")

    # E3
    d1.add_flow("f1_e3_p1", "Khóa/Mở User & Shop, CRUD Danh mục/Tình trạng", "e3", "p1")
    d1.add_flow("f1_p1_e3", "Thống kê người dùng/shop & Audit logs", "p1", "e3")
    d1.add_flow("f1_e3_p2", "Quản lý Từ cấm (HIGH/LOW), Duyệt tin & Cấu hình AI", "e3", "p2")
    d1.add_flow("f1_p2_e3", "Hàng chờ duyệt sản phẩm (Kèm cờ cảnh báo AI)", "p2", "e3")
    d1.add_flow("f1_e3_p5", "Phán quyết tranh chấp & Xử lý báo cáo vi phạm", "e3", "p5")
    d1.add_flow("f1_p5_e3", "Danh sách tranh chấp, Báo cáo & Doanh thu sàn", "p5", "e3")

    # E4
    d1.add_flow("f1_p2_e4", "Prompt, Metadata & Tool Call", "p2", "e4")
    d1.add_flow("f1_e4_p2", "Kết quả phân tích nhãn, Gợi ý mô tả & Chat payload", "e4", "p2")

    # Connect Processes to Data Stores
    # 1.0 <-> Stores
    d1.add_flow("f1_p1_ds1", "Ghi/Đọc User & Shop", "p1", "ds1")
    d1.add_flow("f1_p1_ds2", "Ghi Danh mục & Tình trạng", "p1", "ds2")
    d1.add_flow("f1_p1_ds7", "Ghi Nhật ký hệ thống (sp_GhiNhatKy)", "p1", "ds7")

    # 2.0 <-> Stores
    d1.add_flow("f1_p2_ds2", "Ghi tin đăng, Ảnh sản phẩm & Đổi trạng thái", "p2", "ds2")
    d1.add_flow("f1_p2_ds5", "Đọc từ cấm, Ghi Lượt duyệt, Phân tích AI & Vi phạm", "p2", "ds5")
    d1.add_flow("f1_p2_ds1", "Khóa tài khoản người bán khi đủ 3 vi phạm", "p2", "ds1")
    d1.add_flow("f1_p2_ds7", "Đọc Cấu hình AI & Ghi Thông báo kết quả duyệt", "p2", "ds7")

    # 3.0 <-> Stores
    d1.add_flow("f1_p3_ds2", "Đọc thông tin bán & Cập nhật trạng thái Đã bán", "p3", "ds2")
    d1.add_flow("f1_p3_ds3", "Ghi Giỏ hàng, Tạo Đơn tách shop & Giữ chỗ độc bản", "p3", "ds3")
    d1.add_flow("f1_p3_ds7", "Ghi thông báo đơn đặt hàng mới", "p3", "ds7")

    # 4.0 <-> Stores
    d1.add_flow("f1_p4_ds2", "Khóa rút sản phẩm đang đấu giá (Trigger)", "p4", "ds2")
    d1.add_flow("f1_p4_ds4", "Ghi Phiên đấu giá & Ghi nhận Lượt đặt giá", "p4", "ds4")
    d1.add_flow("f1_p4_ds3", "Tự sinh Đơn hàng cho người thắng phiên", "p4", "ds3")
    d1.add_flow("f1_p4_ds7", "Ghi thông báo vượt giá & Trúng đấu giá", "p4", "ds7")

    # 5.0 <-> Stores
    d1.add_flow("f1_p5_ds3", "Cập nhật trạng thái đơn & Kiểm tra hợp lệ", "p5", "ds3")
    d1.add_flow("f1_p5_ds6", "Ghi/Đọc Tranh chấp, Báo cáo vi phạm & Đánh giá sao", "p5", "ds6")
    d1.add_flow("f1_p5_ds1", "Cập nhật khóa User/Shop vi phạm chính sách", "p5", "ds1")
    d1.add_flow("f1_p5_ds7", "Ghi Thông báo chuông & Nhật ký kiểm toán", "p5", "ds7")

    mxfile.append(d1.diagram_elem)

    # =========================================================================
    # TAB 3: DFD Mức 2 - Giai đoạn 1: Quản lý Tài khoản, Cửa hàng & Hệ thống (1.0)
    # =========================================================================
    d2_1 = DiagramBuilder("diag_d2_1", "DFD Mức 2 - 1.0 Tài Khoản & Cửa Hàng", 1750, 1100)
    
    # External Entities (Bảo toàn từ Mức 1)
    d2_1.add_entity("e1", "<b>E1: Người Mua<br>(Buyer)</b>", 60, 240, 150, 100)
    d2_1.add_entity("e2", "<b>E2: Người Bán<br>(Seller)</b>", 60, 580, 150, 100)
    d2_1.add_entity("e3", "<b>E3: Quản Trị Viên<br>(Admin)</b>", 1540, 400, 160, 100)

    # Sub-processes
    d2_1.add_process("p1_1", "<b>1.1 Đăng Ký &<br>Xác Thực Tài Khoản</b><br><font size='1'>(Cookie MVC, JWT API,<br>BCrypt Hash)</font>", 360, 120, 150, 150)
    d2_1.add_process("p1_2", "<b>1.2 Quản Lý Hồ Sơ<br>& Đổi Mật Khẩu</b><br><font size='1'>(Địa chỉ nhận, SĐT)</font>", 360, 360, 150, 150)
    d2_1.add_process("p1_3", "<b>1.3 Đăng Ký & Quản Lý<br>Cửa Hàng (Shop)</b><br><font size='1'>(sp_TaoCuaHang, max 1 shop)</font>", 360, 600, 150, 150)
    d2_1.add_process("p1_4", "<b>1.4 Quản Trị Người Dùng<br>& Khóa / Mở Khóa</b><br><font size='1'>(Bảo vệ Admin, ghi lý do)</font>", 1120, 180, 150, 150)
    d2_1.add_process("p1_5", "<b>1.5 Quản Trị Danh Mục<br>& Tình Trạng Hàng</b><br><font size='1'>(Xóa mềm ẩn danh mục)</font>", 1120, 480, 150, 150)
    d2_1.add_process("p1_6", "<b>1.6 Ghi & Truy Vấn<br>Nhật Ký Kiểm Toán</b><br><font size='1'>(sp_GhiNhatKy, IP, Lọc log)</font>", 1120, 780, 150, 150)

    # Data Stores
    d2_1.add_store("ds1_u", "<b>D1.1: Người Dùng</b><br>(NguoiDung: Email, PassHash, TrangThai)", 740, 200, 190, 50)
    d2_1.add_store("ds1_s", "<b>D1.2: Cửa Hàng</b><br>(CuaHang: TenShop, DiaChiKho)", 740, 600, 190, 50)
    d2_1.add_store("ds2_c", "<b>D2.1: Danh Mục & Tình Trạng</b><br>(DanhMuc, TinhTrangSanPham)", 740, 450, 190, 50)
    d2_1.add_store("ds7_l", "<b>D7.1: Nhật Ký Hệ Thống</b><br>(NhatKyHeThong: HanhDong, IP, Time)", 740, 780, 190, 50)

    # Inflows / Outflows (Bảo toàn)
    # E1 & E2 to 1.1
    d2_1.add_flow("f_e1_p1_1", "Yêu cầu Đăng ký / Đăng nhập", "e1", "p1_1")
    d2_1.add_flow("f_p1_1_e1", "Phiên xác thực Cookie / JWT Token", "p1_1", "e1")
    d2_1.add_flow("f_e1_p1_2", "Cập nhật thông tin cá nhân & Đổi pass", "e1", "p1_2")
    d2_1.add_flow("f_p1_2_e1", "Kết quả cập nhật hồ sơ cá nhân", "p1_2", "e1")
    
    # E2 to 1.3
    d2_1.add_flow("f_e2_p1_3", "Hồ sơ mở Shop, Địa chỉ kho hàng", "e2", "p1_3")
    d2_1.add_flow("f_p1_3_e2", "Trạng thái Shop & Cấp quyền chủ shop", "p1_3", "e2")

    # E3 to Admin processes
    d2_1.add_flow("f_e3_p1_4", "Lệnh Khóa / Mở tài khoản (Kèm lý do)", "e3", "p1_4")
    d2_1.add_flow("f_p1_4_e3", "Danh sách User, Shop & Trạng thái tài khoản", "p1_4", "e3")
    d2_1.add_flow("f_e3_p1_5", "CRUD Danh mục & Tình trạng sản phẩm", "e3", "p1_5")
    d2_1.add_flow("f_p1_5_e3", "Danh mục hiện hữu & Thống kê liên kết", "p1_5", "e3")
    d2_1.add_flow("f_e3_p1_6", "Bộ lọc tra cứu nhật ký (User, Action, Date)", "e3", "p1_6")
    d2_1.add_flow("f_p1_6_e3", "Báo cáo lịch sử kiểm toán toàn hệ thống", "p1_6", "e3")

    # Internal Store connections
    d2_1.add_flow("f_p1_1_ds1_u", "Tạo tài khoản / So khớp mật khẩu BCrypt", "p1_1", "ds1_u")
    d2_1.add_flow("f_p1_2_ds1_u", "Đọc / Ghi cập nhật thông tin cá nhân", "p1_2", "ds1_u")
    d2_1.add_flow("f_p1_3_ds1_s", "sp_TaoCuaHang (Check ràng buộc 1 User 1 Shop)", "p1_3", "ds1_s")
    d2_1.add_flow("f_p1_4_ds1_u", "Cập nhật TrangThai = LOCKED/ACTIVE, LyDoKhoa", "p1_4", "ds1_u")
    d2_1.add_flow("f_p1_5_ds2_c", "Ghi DanhMuc, TinhTrang (Cờ IsActive ẩn mềm)", "p1_5", "ds2_c")
    d2_1.add_flow("f_p1_4_p1_6", "Ghi vết hành vi Admin", "p1_4", "p1_6")
    d2_1.add_flow("f_p1_5_p1_6", "Ghi vết sửa đổi danh mục", "p1_5", "p1_6")
    d2_1.add_flow("f_p1_6_ds7_l", "Ghi / Đọc bản ghi nhật ký (sp_GhiNhatKy)", "p1_6", "ds7_l")

    mxfile.append(d2_1.diagram_elem)

    # =========================================================================
    # TAB 4: DFD Mức 2 - Giai đoạn 2: Đăng Bán, Kiểm Duyệt & Trợ Lý AI (2.0)
    # =========================================================================
    d2_2 = DiagramBuilder("diag_d2_2", "DFD Mức 2 - 2.0 Đăng Bán & Kiểm Duyệt", 1750, 1150)
    
    # External Entities
    d2_2.add_entity("e1", "<b>E1: Người Mua<br>(Buyer)</b>", 60, 750, 150, 100)
    d2_2.add_entity("e2", "<b>E2: Người Bán<br>(Seller)</b>", 60, 200, 150, 100)
    d2_2.add_entity("e3", "<b>E3: Quản Trị Viên<br>(Admin)</b>", 1540, 220, 160, 100)
    d2_2.add_entity("e4", "<b>E4: Dịch Vụ AI Anthropic Claude API</b>", 820, 950, 200, 80)

    # Sub-processes
    d2_2.add_process("p2_1", "<b>2.1 Soạn Tin Đăng &<br>Quản Lý Album Ảnh</b><br><font size='1'>(Tối đa 10 ảnh, chọn ảnh chính)</font>", 360, 140, 150, 150)
    d2_2.add_process("p2_2", "<b>2.2 Gửi Duyệt & Quét<br>Tự Động Từ Khóa Cấm</b><br><font size='1'>(sp_GuiDuyetSanPham,<br>fn_TimTuKhoaCam)</font>", 740, 140, 150, 150)
    d2_2.add_process("p2_3", "<b>2.3 Phân Tích & Trợ Lý<br>Thông Minh AI</b><br><font size='1'>(Tool Use: search, advise,<br>draft description)</font>", 520, 720, 160, 160)
    d2_2.add_process("p2_4", "<b>2.4 Quản Trị Từ Cấm<br>& Cấu Hình Tham Số AI</b><br><font size='1'>(CRUD TuKhoaCam HIGH/LOW,<br>Bảng CauHinhAI)</font>", 1160, 440, 160, 160)
    d2_2.add_process("p2_5", "<b>2.5 Admin Thẩm Định<br>Duyệt Tin & Chế Tài</b><br><font size='1'>(sp_DuyetSanPham, sp_TuChoi,<br>Khóa khi đủ 3 vi phạm)</font>", 1160, 140, 160, 160)

    # Data Stores
    d2_2.add_store("ds2_p", "<b>D2.2: Sản Phẩm & Ảnh</b><br>(SanPham, AnhSanPham)", 400, 420, 190, 50)
    d2_2.add_store("ds5_m", "<b>D5.1: Kiểm Duyệt & Từ Cấm</b><br>(TuKhoaCam, LuotKiemDuyet, PhanTichAI)", 800, 420, 210, 50)
    d2_2.add_store("ds5_v", "<b>D5.2: Lịch Sử Vi Phạm</b><br>(LichSuViPham, MaLuotKiemDuyet Unique)", 1250, 30, 200, 50)
    d2_2.add_store("ds1_u", "<b>D1.1: Người Dùng (Seller)</b><br>(Cập nhật khóa tài khoản)", 960, 30, 180, 50)
    d2_2.add_store("ds7_n", "<b>D7.2: Thông Báo & Cấu Hình</b><br>(ThongBao, CauHinhAI)", 820, 680, 190, 50)

    # Flows for Seller
    d2_2.add_flow("f_e2_p2_1", "Tiêu đề, mô tả, giá, đa ảnh sản phẩm", "e2", "p2_1")
    d2_2.add_flow("f_p2_1_e2", "Xác nhận lưu bản nháp & Album ảnh hợp lệ", "p2_1", "e2")
    d2_2.add_flow("f_p2_1_p2_2", "Lệnh gửi duyệt tin đăng", "p2_1", "p2_2")
    d2_2.add_flow("f_p2_5_e2", "Thông báo kết quả duyệt (Duyệt / Từ chối kèm lý do)", "p2_5", "e2")

    # Flows for Buyer & AI Chatbot
    d2_2.add_flow("f_e1_p2_3", "Câu hỏi tư vấn giá, tìm kiếm thông minh", "e1", "p2_3")
    d2_2.add_flow("f_p2_3_e1", "Phản hồi tư vấn, gợi ý định giá & sản phẩm phù hợp", "p2_3", "e1")
    d2_2.add_flow("f_p2_3_e4", "Anthropic Tool Request (Prompt, System Prompt)", "p2_3", "e4")
    d2_2.add_flow("f_e4_p2_3", "Response completion & Tool call parameters", "e4", "p2_3")

    # Flows for Admin
    d2_2.add_flow("f_e3_p2_4", "Quản lý Từ cấm (HIGH/LOW) & Tham số AI", "e3", "p2_4")
    d2_2.add_flow("f_p2_4_e3", "Danh sách từ khóa cấm & Cấu hình AI hiện hành", "p2_4", "e3")
    d2_2.add_flow("f_e3_p2_5", "Quyết định Phê duyệt / Từ chối / Gỡ sản phẩm", "e3", "p2_5")
    d2_2.add_flow("f_p2_5_e3", "Hàng chờ kiểm duyệt (vw_SanPhamChoDuyet, cờ cảnh báo)", "p2_5", "e3")

    # Internal Store Connections
    d2_2.add_flow("f_p2_1_ds2_p", "Lưu thông tin sản phẩm & Album ảnh (trg_AnhSanPham_GioiHan)", "p2_1", "ds2_p")
    d2_2.add_flow("f_p2_2_ds5_m", "Đọc TuKhoaCam; Ghi LuotKiemDuyet & PhanTichAI = QUEUED", "p2_2", "ds5_m")
    d2_2.add_flow("f_p2_2_ds2_p", "Chuyển trạng thái SanPham sang PENDING_REVIEW", "p2_2", "ds2_p")
    d2_2.add_flow("f_p2_5_ds5_m", "Đọc kết quả quét; Ghi trạng thái duyệt / Từ chối", "p2_5", "ds5_m")
    d2_2.add_flow("f_p2_5_ds2_p", "Cập nhật SanPham sang ACTIVE hoặc REJECTED", "p2_5", "ds2_p")
    d2_2.add_flow("f_p2_5_ds5_v", "Ghi nhận 1 vi phạm (Trigger trg_LuotKiemDuyet_GhiViPhamLow)", "p2_5", "ds5_v")
    d2_2.add_flow("f_p2_5_ds1_u", "Tự động khóa tài khoản khi tích lũy đủ 3 lần vi phạm", "p2_5", "ds1_u")
    d2_2.add_flow("f_p2_4_ds5_m", "Cập nhật bảng TuKhoaCam (TuKhoa, MucDo)", "p2_4", "ds5_m")
    d2_2.add_flow("f_p2_4_ds7_n", "Cập nhật cấu hình bảng CauHinhAI", "p2_4", "ds7_n")
    d2_2.add_flow("f_p2_3_ds7_n", "Đọc model/prompt cấu hình AI; Rate Limit 2 slots", "p2_3", "ds7_n")
    d2_2.add_flow("f_p2_3_ds2_p", "Function calling: Đọc kho sản phẩm tư vấn giá", "p2_3", "ds2_p")
    d2_2.add_flow("f_p2_5_ds7_n", "Gửi thông báo chuông kết quả kiểm duyệt", "p2_5", "ds7_n")

    mxfile.append(d2_2.diagram_elem)

    # =========================================================================
    # TAB 5: DFD Mức 2 - Giai đoạn 3: Tìm Kiếm, Giỏ Hàng & Đặt Hàng Độc Bản (3.0)
    # =========================================================================
    d2_3 = DiagramBuilder("diag_d2_3", "DFD Mức 2 - 3.0 Mua Hàng & Độc Bản", 1750, 1050)
    
    # External Entities
    d2_3.add_entity("e1", "<b>E1: Người Mua<br>(Buyer)</b>", 60, 380, 150, 110)
    d2_3.add_entity("e2", "<b>E2: Người Bán<br>(Seller)</b>", 1540, 580, 150, 100)

    # Sub-processes
    d2_3.add_process("p3_1", "<b>3.1 Tra Cứu, Tìm Kiếm<br>& Xem Chi Tiết</b><br><font size='1'>(Lọc danh mục, khoảng giá,<br>độ mới %, ảnh chi tiết)</font>", 380, 160, 160, 160)
    d2_3.add_process("p3_2", "<b>3.2 Quản Lý Giỏ Hàng<br>Đồ Cũ Độc Bản</b><br><font size='1'>(sp_ThemVaoGio, chặn người bán<br>tự mua hàng của mình)</font>", 380, 540, 160, 160)
    d2_3.add_process("p3_3", "<b>3.3 Checkout, Giữ Chỗ<br>& Tách Đơn Theo Shop</b><br><font size='1'>(sp_DatHangTuGio, UPDLOCK/HOLDLOCK,<br>Index UX_ChiTietDonHang_DangGiuCho)</font>", 980, 540, 180, 180)

    # Data Stores
    d2_3.add_store("ds2_p", "<b>D2.2: Sản Phẩm Đang Bán</b><br>(SanPham ACTIVE, AnhSanPham)", 700, 180, 190, 50)
    d2_3.add_store("ds3_g", "<b>D3.1: Giỏ Hàng</b><br>(GioHang, ChiTietGioHang)", 700, 560, 180, 50)
    d2_3.add_store("ds3_d", "<b>D3.2: Đơn Hàng & Chi Tiết</b><br>(DonHang, ChiTietDonHang)", 1300, 360, 190, 50)
    d2_3.add_store("ds7_n", "<b>D7.2: Thông Báo Hệ Thống</b><br>(ThongBao: Đơn mới cho shop/buyer)", 980, 880, 190, 50)

    # Flows for Buyer
    d2_3.add_flow("f_e1_p3_1", "Từ khóa tìm kiếm, bộ lọc danh mục/giá/độ mới", "e1", "p3_1")
    d2_3.add_flow("f_p3_1_e1", "Danh sách sản phẩm phù hợp & Chi tiết món hàng", "p3_1", "e1")
    d2_3.add_flow("f_e1_p3_2", "Thêm vào giỏ hàng, xóa món hàng", "e1", "p3_2")
    d2_3.add_flow("f_p3_2_e1", "Danh sách giỏ hàng tự động nhóm theo Shop", "p3_2", "e1")
    d2_3.add_flow("f_e1_p3_3", "Chọn món thanh toán, địa chỉ nhận hàng & xác nhận đặt", "e1", "p3_3")
    d2_3.add_flow("f_p3_3_e1", "Kết quả đặt hàng thành công / Báo lỗi nếu đã có người giữ chỗ", "p3_3", "e1")

    # Flow for Seller
    d2_3.add_flow("f_p3_3_e2", "Thông báo Đơn bán mới cần xác nhận & chuẩn bị hàng", "p3_3", "e2")

    # Store Connections
    d2_3.add_flow("f_p3_1_ds2_p", "Truy vấn danh mục sản phẩm ACTIVE", "p3_1", "ds2_p")
    d2_3.add_flow("f_p3_2_ds2_p", "Kiểm tra sản phẩm còn bán & Không phải của chính mình", "p3_2", "ds2_p")
    d2_3.add_flow("f_p3_2_ds3_g", "Ghi nhận sản phẩm vào giỏ hàng (vw_GioHangChiTiet)", "p3_2", "ds3_g")
    d2_3.add_flow("f_p3_3_ds3_g", "Đọc các mục đã chọn trong giỏ hàng & Xóa khỏi giỏ sau đặt", "p3_3", "ds3_g")
    d2_3.add_flow("f_p3_3_ds2_p", "Khóa giữ chỗ độc bản (UPDLOCK) & Đổi SanPham sang SOLD/LOCKED", "p3_3", "ds2_p")
    d2_3.add_flow("f_p3_3_ds3_d", "Tách đơn theo từng Shop con (sp_TaoDonHangTheoShop)", "p3_3", "ds3_d")
    d2_3.add_flow("f_p3_3_ds7_n", "Gửi thông báo chuông in-app cho Người mua và Người bán", "p3_3", "ds7_n")

    mxfile.append(d2_3.diagram_elem)

    # =========================================================================
    # TAB 6: DFD Mức 2 - Giai đoạn 4: Đấu Giá Trực Tuyến & Chốt Phiên Tự Động (4.0)
    # =========================================================================
    d2_4 = DiagramBuilder("diag_d2_4", "DFD Mức 2 - 4.0 Đấu Giá Trực Tuyến", 1750, 1050)
    
    # External Entities
    d2_4.add_entity("e1", "<b>E1: Người Mua<br>(Bidder)</b>", 60, 480, 150, 100)
    d2_4.add_entity("e2", "<b>E2: Người Bán<br>(Auctioneer / Shop)</b>", 60, 180, 150, 100)

    # Sub-processes
    d2_4.add_process("p4_1", "<b>4.1 Thiết Lập Phòng<br>Đấu Giá Sản Phẩm</b><br><font size='1'>(Giá khởi điểm, bước giá,<br>hạn kết thúc, khóa rút hàng)</font>", 400, 140, 160, 160)
    d2_4.add_process("p4_2", "<b>4.2 Tiếp Nhận & Xác Thực<br>Lượt Ra Giá (Bid)</b><br><font size='1'>(sp_DatGia, kiểm tra bước giá,<br>chặn shop tự trả giá)</font>", 400, 480, 160, 160)
    d2_4.add_process("p4_3", "<b>4.3 Background Sweeper<br>& Tự Động Chốt Đơn</b><br><font size='1'>(AuctionBackgroundService 60s,<br>chốt phiên, sinh DonHang)</font>", 1020, 320, 170, 170)

    # Data Stores
    d2_4.add_store("ds2_p", "<b>D2.2: Sản Phẩm</b><br>(Khóa rút hàng: trg_SanPham_ChanRutKhoiDauGia)", 760, 120, 200, 50)
    d2_4.add_store("ds4_a", "<b>D4.1: Phiên & Lượt Đặt Giá</b><br>(PhienDauGia, LuotDatGia)", 760, 420, 190, 50)
    d2_4.add_store("ds3_d", "<b>D3.2: Đơn Hàng</b><br>(Tự sinh đơn thắng đấu giá)", 1350, 240, 180, 50)
    d2_4.add_store("ds7_n", "<b>D7.2: Thông Báo</b><br>(Vượt giá, trúng đấu giá, chốt phiên)", 1350, 540, 180, 50)

    # Flows for Seller & Bidder
    d2_4.add_flow("f_e2_p4_1", "Chọn sản phẩm, đặt giá khởi điểm, bước giá & thời gian", "e2", "p4_1")
    d2_4.add_flow("f_p4_1_e2", "Xác nhận tạo phòng đấu giá thành công (SCHEDULED)", "p4_1", "e2")
    d2_4.add_flow("f_e1_p4_2", "Lượt trả giá cạnh tranh (Mức giá mới)", "e1", "p4_2")
    d2_4.add_flow("f_p4_2_e1", "Cập nhật mức giá cao nhất & thời gian đếm ngược", "p4_2", "e1")
    d2_4.add_flow("f_p4_3_e1", "Thông báo chúc mừng trúng đấu giá & thông tin thanh toán", "p4_3", "e1")
    d2_4.add_flow("f_p4_3_e2", "Thông báo phiên kết thúc, thông tin người thắng & đơn hàng", "p4_3", "e2")

    # Store Connections
    d2_4.add_flow("f_p4_1_ds2_p", "Kích hoạt Trigger trg_SanPham_ChanRutKhoiDauGia", "p4_1", "ds2_p")
    d2_4.add_flow("f_p4_1_ds4_a", "Ghi bản ghi PhienDauGia (TrangThai = SCHEDULED/RUNNING)", "p4_1", "ds4_a")
    d2_4.add_flow("f_p4_2_ds4_a", "sp_DatGia (Kiểm tra bước giá, ghi LuotDatGia, đổi GiaHienTai)", "p4_2", "ds4_a")
    d2_4.add_flow("f_p4_2_ds7_n", "Gửi thông báo bị trả giá cao hơn cho người bid trước", "p4_2", "ds7_n")
    d2_4.add_flow("f_p4_3_ds4_a", "Quét phiên hết hạn; Chuyển trạng thái sang ENDED / CANCELLED", "p4_3", "ds4_a")
    d2_4.add_flow("f_p4_3_ds3_d", "Tự động sinh DonHang cho người ra giá cao nhất", "p4_3", "ds3_d")
    d2_4.add_flow("f_p4_3_ds7_n", "Gửi thông báo kết quả chốt phiên cho các bên", "p4_3", "ds7_n")

    mxfile.append(d2_4.diagram_elem)

    # =========================================================================
    # TAB 7: DFD Mức 2 - Giai đoạn 5: Xử Lý Đơn Hàng, Tranh Chấp & Hậu Mãi (5.0)
    # =========================================================================
    d2_5 = DiagramBuilder("diag_d2_5", "DFD Mức 2 - 5.0 Đơn Hàng & Tranh Chấp", 1750, 1150)
    
    # External Entities
    d2_5.add_entity("e1", "<b>E1: Người Mua<br>(Buyer)</b>", 60, 480, 150, 100)
    d2_5.add_entity("e2", "<b>E2: Người Bán<br>(Seller)</b>", 60, 160, 150, 100)
    d2_5.add_entity("e3", "<b>E3: Quản Trị Viên<br>(Admin)</b>", 1540, 480, 160, 100)

    # Sub-processes
    d2_5.add_process("p5_1", "<b>5.1 Quản Lý Vòng Đời<br>Đơn Bán & Giao Hàng</b><br><font size='1'>(fn_ChuyenTrangThaiDonHangHopLe:<br>PENDING->CONFIRMED->SHIPPING)</font>", 400, 120, 160, 160)
    d2_5.add_process("p5_2", "<b>5.2 Khởi Tạo & Phân Xử<br>Khiếu Nại Tranh Chấp</b><br><font size='1'>(Đóng băng đơn TRANH_CHAP,<br>Admin sp_PhanQuyetTranhChap)</font>", 920, 320, 170, 170)
    d2_5.add_process("p5_3", "<b>5.3 Đánh Giá Chất Lượng<br>Sau Mua (Reviews)</b><br><font size='1'>(Chỉ đơn COMPLETED mới được đánh giá,<br>mỗi món 1 lần, 1-5 sao)</font>", 400, 680, 160, 160)
    d2_5.add_process("p5_4", "<b>5.4 Tiếp Nhận & Xử Lý<br>Báo Cáo Vi Phạm (Reports)</b><br><font size='1'>(Báo cáo sản phẩm/shop/user,<br>Admin gỡ tin/khóa tài khoản)</font>", 920, 780, 170, 170)

    # Data Stores
    d2_5.add_store("ds3_d", "<b>D3.2: Đơn Hàng & Lịch Sử</b><br>(DonHang, LichSuTrangThaiDonHang)", 400, 420, 190, 50)
    d2_5.add_store("ds6_t", "<b>D6.1: Tranh Chấp Đơn Hàng</b><br>(TranhChap: LyDo, BangChungAnh, KetQua)", 920, 120, 200, 50)
    d2_5.add_store("ds6_r", "<b>D6.2: Đánh Giá & Báo Cáo</b><br>(DanhGia, BaoCao: NoiDung, TrangThai)", 920, 620, 190, 50)
    d2_5.add_store("ds1_u", "<b>D1.1: Người Dùng & Shop</b><br>(Khóa tài khoản vi phạm)", 1350, 780, 180, 50)
    d2_5.add_store("ds7_n", "<b>D7.2: Thông Báo & Nhật Ký</b><br>(ThongBao, NhatKyHeThong)", 1350, 320, 180, 50)

    # Flows for Seller & Buyer & Admin
    # 5.1
    d2_5.add_flow("f_e2_p5_1", "Cập nhật tiến độ đơn: Xác nhận đơn, Giao vận", "e2", "p5_1")
    d2_5.add_flow("f_p5_1_e2", "Danh sách đơn bán hàng theo trạng thái & Doanh thu", "p5_1", "e2")
    d2_5.add_flow("f_e1_p5_1", "Yêu cầu hủy đơn khi hàng chưa giao", "e1", "p5_1")
    d2_5.add_flow("f_p5_1_e1", "Trạng thái đơn hàng đang giao & Thông tin vận chuyển", "p5_1", "e1")

    # 5.2 Tranh chấp
    d2_5.add_flow("f_e1_p5_2", "Mở khiếu nại tranh chấp & Tải ảnh chứng cứ hàng lỗi", "e1", "p5_2")
    d2_5.add_flow("f_e2_p5_2", "Gửi phản hồi giải trình tranh chấp", "e2", "p5_2")
    d2_5.add_flow("f_e3_p5_2", "Phán quyết tranh chấp: Hoàn tiền người mua / Giải phóng tiền shop", "e3", "p5_2")
    d2_5.add_flow("f_p5_2_e3", "Danh sách đơn tranh chấp kèm chứng cứ hai bên", "p5_2", "e3")
    d2_5.add_flow("f_p5_2_e1", "Quyết định xử lý tranh chấp & Tiền hoàn (nếu có)", "p5_2", "e1")
    d2_5.add_flow("f_p5_2_e2", "Quyết định phân xử của Admin & Cảnh báo", "p5_2", "e2")

    # 5.3 Đánh giá
    d2_5.add_flow("f_e1_p5_3", "Đánh giá sao (1-5) & Viết nhận xét món hàng", "e1", "p5_3")
    d2_5.add_flow("f_p5_3_e1", "Xác nhận ghi nhận đánh giá thành công", "p5_3", "e1")

    # 5.4 Báo cáo
    d2_5.add_flow("f_e1_p5_4", "Gửi báo cáo gian lận sản phẩm / shop lừa đảo", "e1", "p5_4")
    d2_5.add_flow("f_e3_p5_4", "Thụ lý báo cáo: Bỏ qua / Gỡ sản phẩm / Khóa tài khoản", "e3", "p5_4")
    d2_5.add_flow("f_p5_4_e3", "Danh sách báo cáo vi phạm cần giải quyết", "p5_4", "e3")

    # Store Connections
    d2_5.add_flow("f_p5_1_ds3_d", "Kiểm tra fn_ChuyenTrangThaiDonHangHopLe & Cập nhật DonHang", "p5_1", "ds3_d")
    d2_5.add_flow("f_p5_2_ds6_t", "Ghi nhận yêu cầu tranh chấp & Lịch sử giải trình", "p5_2", "ds6_t")
    d2_5.add_flow("f_p5_2_ds3_d", "Đóng băng đơn (TrangThai = TRANH_CHAP); Cập nhật hoàn tất sau phán quyết", "p5_2", "ds3_d")
    d2_5.add_flow("f_p5_3_ds3_d", "Kiểm tra đơn hàng đã COMPLETED & Chưa từng đánh giá món này", "p5_3", "ds3_d")
    d2_5.add_flow("f_p5_3_ds6_r", "Lưu đánh giá vào bảng DanhGia", "p5_3", "ds6_r")
    d2_5.add_flow("f_p5_4_ds6_r", "Ghi nhận phản ánh vào bảng BaoCao", "p5_4", "ds6_r")
    d2_5.add_flow("f_p5_4_ds1_u", "Thực thi khóa Shop / User vi phạm nghiêm trọng", "p5_4", "ds1_u")
    d2_5.add_flow("f_p5_2_ds7_n", "Gửi thông báo kết quả phán quyết & Ghi Audit Log", "p5_2", "ds7_n")
    d2_5.add_flow("f_p5_4_ds7_n", "Ghi nhật ký xử lý vi phạm vào NhatKyHeThong", "p5_4", "ds7_n")

    mxfile.append(d2_5.diagram_elem)

    # =========================================================================
    # Write to XML file
    # =========================================================================
    xml_str = ET.tostring(mxfile, encoding="utf-8")
    dom = minidom.parseString(xml_str)
    pretty_xml = dom.toprettyxml(indent="  ")
    
    output_path = r"c:\Users\Admin\OneDrive\Documents\Secondhand-Marketplace\DFD_Secondhand_Marketplace.xml"
    with open(output_path, "w", encoding="utf-8") as f:
        f.write(pretty_xml)
    print(f"Successfully generated DFD XML at: {output_path}")

if __name__ == "__main__":
    build_all_diagrams()
