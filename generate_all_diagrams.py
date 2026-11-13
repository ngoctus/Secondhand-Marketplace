import xml.etree.ElementTree as ET
import xml.dom.minidom as minidom
import os

class BeautifulDiagramBuilder:
    def __init__(self, diag_id, name, width=2400, height=1500):
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

    # ------------------ USE CASE & GENERAL SHAPES ------------------
    def add_boundary(self, cell_id, value, x, y, w, h):
        style = ("shape=rect;html=1;whiteSpace=wrap;fillColor=#f8fafc;strokeColor=#475569;"
                 "strokeWidth=2.5;dashed=1;dashPattern=8 6;fontStyle=1;fontSize=15;fontColor=#0f172a;"
                 "verticalAlign=top;align=center;spacingTop=16;shadow=0;")
        cell = ET.SubElement(self.root, "mxCell", id=cell_id, value=value, style=style, 
                             vertex="1", parent="1")
        ET.SubElement(cell, "mxGeometry", x=str(x), y=str(y), width=str(w), height=str(h), **{"as": "geometry"})
        return cell

    def add_actor(self, cell_id, value, x, y, w=90, h=130):
        style = ("shape=umlActor;verticalLabelPosition=bottom;verticalAlign=top;html=1;"
                 "outlineConnect=0;fillColor=#e0f2fe;strokeColor=#0284c7;strokeWidth=2;"
                 "fontStyle=1;fontSize=13;fontColor=#0369a1;shadow=1;")
        cell = ET.SubElement(self.root, "mxCell", id=cell_id, value=value, style=style, 
                             vertex="1", parent="1")
        ET.SubElement(cell, "mxGeometry", x=str(x), y=str(y), width=str(w), height=str(h), **{"as": "geometry"})
        return cell

    def add_usecase(self, cell_id, value, x, y, w=240, h=80):
        style = ("ellipse;whiteSpace=wrap;html=1;fillColor=#f0fdf4;strokeColor=#16a34a;strokeWidth=2;"
                 "fontColor=#14532d;fontStyle=1;fontSize=12;shadow=1;align=center;spacing=4;")
        cell = ET.SubElement(self.root, "mxCell", id=cell_id, value=value, style=style, 
                             vertex="1", parent="1")
        ET.SubElement(cell, "mxGeometry", x=str(x), y=str(y), width=str(w), height=str(h), **{"as": "geometry"})
        return cell

    def add_assoc(self, cell_id, source, target):
        style = "endArrow=none;html=1;rounded=1;strokeColor=#1e293b;strokeWidth=2;"
        edge = ET.SubElement(self.root, "mxCell", id=cell_id, value="", style=style, 
                             edge="1", parent="1", source=source, target=target)
        ET.SubElement(edge, "mxGeometry", relative="1", **{"as": "geometry"})
        return edge

    def add_rel(self, cell_id, value, source, target, rel_type="include"):
        if rel_type in ("include", "extend"):
            style = ("endArrow=open;endSize=8;dashed=1;dashPattern=6 4;html=1;strokeColor=#334155;strokeWidth=1.8;"
                     "fontSize=11;fontStyle=2;fontColor=#334155;labelBackgroundColor=#ffffff;labelBorderColor=#cbd5e1;")
            lbl = f"&laquo;{rel_type}&raquo;"
            if value:
                lbl += f"<br>{value}"
        elif rel_type == "generalization":
            style = "endArrow=block;endFill=0;endSize=10;html=1;rounded=0;strokeColor=#0284c7;strokeWidth=2;"
            lbl = value
        else:
            style = "edgeStyle=orthogonalEdgeStyle;rounded=1;html=1;endArrow=classic;strokeWidth=2;"
            lbl = value

        edge = ET.SubElement(self.root, "mxCell", id=cell_id, value=lbl, style=style, 
                             edge="1", parent="1", source=source, target=target)
        ET.SubElement(edge, "mxGeometry", relative="1", **{"as": "geometry"})
        return edge

    # ------------------ BFD SHAPES ------------------
    def add_bfd_node(self, cell_id, value, x, y, w, h, level=0):
        if level == 0:
            style = ("rounded=1;whiteSpace=wrap;html=1;fillColor=#1e3a8a;strokeColor=#0f172a;strokeWidth=2.5;"
                     "fontColor=#ffffff;fontStyle=1;fontSize=15;shadow=1;arcSize=10;align=center;")
        elif level == 1:
            style = ("rounded=1;whiteSpace=wrap;html=1;fillColor=#dbeafe;strokeColor=#1d4ed8;strokeWidth=2;"
                     "fontColor=#1e3a8a;fontStyle=1;fontSize=13;shadow=1;arcSize=8;align=center;")
        else:
            style = ("rounded=1;whiteSpace=wrap;html=1;fillColor=#f8fafc;strokeColor=#64748b;strokeWidth=1.5;"
                     "fontColor=#0f172a;fontSize=12;align=left;spacingLeft=12;shadow=1;arcSize=6;")
        
        cell = ET.SubElement(self.root, "mxCell", id=cell_id, value=value, style=style, 
                             vertex="1", parent="1")
        ET.SubElement(cell, "mxGeometry", x=str(x), y=str(y), width=str(w), height=str(h), **{"as": "geometry"})
        return cell

    def add_tree_edge(self, cell_id, source, target):
        style = ("edgeStyle=orthogonalEdgeStyle;rounded=1;orthogonalLoop=1;jettySize=auto;html=1;"
                 "strokeColor=#475569;strokeWidth=2;endArrow=none;")
        edge = ET.SubElement(self.root, "mxCell", id=cell_id, value="", style=style, 
                             edge="1", parent="1", source=source, target=target)
        ET.SubElement(edge, "mxGeometry", relative="1", **{"as": "geometry"})
        return edge

    # ------------------ DFD SHAPES & FLOWS ------------------
    def add_entity(self, cell_id, value, x, y, w=200, h=100):
        style = ("rounded=1;whiteSpace=wrap;html=1;fillColor=#e0f2fe;strokeColor=#0284c7;strokeWidth=2.5;"
                 "fontColor=#0369a1;fontStyle=1;fontSize=13;shadow=1;arcSize=8;align=center;spacing=6;")
        cell = ET.SubElement(self.root, "mxCell", id=cell_id, value=value, style=style, 
                             vertex="1", parent="1")
        ET.SubElement(cell, "mxGeometry", x=str(x), y=str(y), width=str(w), height=str(h), **{"as": "geometry"})
        return cell

    def add_process(self, cell_id, value, x, y, w=190, h=190):
        style = ("ellipse;whiteSpace=wrap;html=1;aspect=fixed;fillColor=#dcfce7;strokeColor=#15803d;strokeWidth=2.5;"
                 "fontColor=#14532d;fontStyle=1;fontSize=13;shadow=1;align=center;spacing=6;")
        cell = ET.SubElement(self.root, "mxCell", id=cell_id, value=value, style=style, 
                             vertex="1", parent="1")
        ET.SubElement(cell, "mxGeometry", x=str(x), y=str(y), width=str(w), height=str(h), **{"as": "geometry"})
        return cell

    def add_store(self, cell_id, value, x, y, w=240, h=65):
        style = ("shape=partialRectangle;top=0;bottom=0;fillColor=#fef3c7;strokeColor=#d97706;strokeWidth=2;"
                 "fontColor=#92400e;fontStyle=1;fontSize=12;align=left;spacingLeft=14;shadow=1;")
        cell = ET.SubElement(self.root, "mxCell", id=cell_id, value=value, style=style, 
                             vertex="1", parent="1")
        ET.SubElement(cell, "mxGeometry", x=str(x), y=str(y), width=str(w), height=str(h), **{"as": "geometry"})
        return cell

    def add_flow(self, cell_id, value, source, target, waypoints=None):
        # Mũi tên rõ ràng, strokeWidth 2, cỡ chữ 11-12, nền nhãn trắng có viền xám sáng
        style = ("edgeStyle=orthogonalEdgeStyle;rounded=1;orthogonalLoop=1;jettySize=auto;html=1;"
                 "strokeColor=#1e293b;strokeWidth=2;endArrow=classic;endSize=7;fontSize=11;fontColor=#0f172a;"
                 "labelBackgroundColor=#ffffff;labelBorderColor=#cbd5e1;spacing=2;")
        edge = ET.SubElement(self.root, "mxCell", id=cell_id, value=value, style=style, 
                             edge="1", parent="1", source=source, target=target)
        geo = ET.SubElement(edge, "mxGeometry", relative="1", **{"as": "geometry"})
        if waypoints:
            pts = ET.SubElement(geo, "Array", **{"as": "points"})
            for (px, py) in waypoints:
                ET.SubElement(pts, "mxPoint", x=str(px), y=str(py))
        return edge


def generate_all():
    mxfile = ET.Element("mxfile", host="app.diagrams.net", modified="2026-11-14T02:30:00.000Z", 
                        agent="Antigravity/2.0 (Windows)", version="24.0.0")

    # =========================================================================
    # TAB 1: BFD (Business Function Diagram) - KHOẢNG CÁCH RỘNG, CHỮ TO
    # =========================================================================
    bfd = BeautifulDiagramBuilder("diag_bfd", "BFD - Phân Rã Chức Năng", 2600, 1500)
    
    # Root
    bfd.add_bfd_node("bfd_root", "<b>HỆ THỐNG SÀN GIAO DỊCH ĐỒ CŨ (SECONDHAND MARKETPLACE)</b>", 
                     1080, 40, 440, 70, level=0)

    # 5 Major Modules (Level 1)
    modules = [
        ("bfd_m1", "<b>1.0 QUẢN TRỊ TÀI KHOẢN<br>& HỆ THỐNG</b>", 80, 180, 310, 65),
        ("bfd_m2", "<b>2.0 ĐĂNG BÁN, KIỂM DUYỆT<br>& TRỢ LÝ AI</b>", 580, 180, 310, 65),
        ("bfd_m3", "<b>3.0 MUA HÀNG, GIỎ HÀNG<br>& ĐẶT HÀNG ĐỘC BẢN</b>", 1080, 180, 310, 65),
        ("bfd_m4", "<b>4.0 ĐẤU GIÁ TRỰC TUYẾN<br>& TỰ ĐỘNG HÓA</b>", 1580, 180, 310, 65),
        ("bfd_m5", "<b>5.0 XỬ LÝ ĐƠN HÀNG,<br>TRANH CHẤP & HẬU MÃI</b>", 2080, 180, 310, 65),
    ]
    for mid, mval, mx, my, mw, mh in modules:
        bfd.add_bfd_node(mid, mval, mx, my, mw, mh, level=1)
        bfd.add_tree_edge(f"edge_root_{mid}", "bfd_root", mid)

    # Module 1 Sub-functions
    m1_subs = [
        ("1.1 Đăng ký tài khoản (Băm BCrypt)", 280),
        ("1.2 Đăng nhập Cookie & JWT Token API", 345),
        ("1.3 Quản lý hồ sơ cá nhân & Đổi mật khẩu", 410),
        ("1.4 Đăng ký & Quản lý Cửa hàng (sp_TaoCuaHang)", 475),
        ("1.5 Quản trị User/Shop (Khóa/mở kèm lý do)", 540),
        ("1.6 CRUD Danh mục & Tình trạng (Ẩn mềm)", 605),
        ("1.7 Nhật ký kiểm toán hệ thống (sp_GhiNhatKy)", 670),
    ]
    for i, (text, y) in enumerate(m1_subs):
        cid = f"bfd_m1_{i+1}"
        bfd.add_bfd_node(cid, text, 80, y, 310, 45, level=2)
        bfd.add_tree_edge(f"edge_m1_{i+1}", "bfd_m1", cid)

    # Module 2 Sub-functions
    m2_subs = [
        ("2.1 Đăng bán & Album ảnh (max 10 ảnh)", 280),
        ("2.2 Quản lý kho hàng shop (Nháp, Bán, Đã bán)", 345),
        ("2.3 Gửi duyệt tin (sp_GuiDuyetSanPham)", 410),
        ("2.4 Quét tự động từ cấm (fn_TimTuKhoaCam)", 475),
        ("2.5 Trợ lý AI (Gợi ý giá, soạn mô tả Anthropic)", 540),
        ("2.6 Admin duyệt tin (Duyệt, Từ chối, Phạt lỗi)", 605),
        ("2.7 Quản lý từ khóa cấm & Cấu hình AI", 670),
    ]
    for i, (text, y) in enumerate(m2_subs):
        cid = f"bfd_m2_{i+1}"
        bfd.add_bfd_node(cid, text, 580, y, 310, 45, level=2)
        bfd.add_tree_edge(f"edge_m2_{i+1}", "bfd_m2", cid)

    # Module 3 Sub-functions
    m3_subs = [
        ("3.1 Tìm kiếm & Lọc sản phẩm đa tiêu chí", 280),
        ("3.2 Xem chi tiết sản phẩm & Uy tín người bán", 345),
        ("3.3 Quản lý Giỏ hàng (sp_ThemVaoGio, chặn tự mua)", 410),
        ("3.4 Checkout & Giữ chỗ độc bản (UPDLOCK)", 475),
        ("3.5 Tách đơn hàng theo từng Shop (sp_TaoDonHang...)", 540),
        ("3.6 Quản lý Đơn mua & Tiến trình giao hàng", 605),
        ("3.7 Hủy đơn mua khi hàng chưa bàn giao", 670),
    ]
    for i, (text, y) in enumerate(m3_subs):
        cid = f"bfd_m3_{i+1}"
        bfd.add_bfd_node(cid, text, 1080, y, 310, 45, level=2)
        bfd.add_tree_edge(f"edge_m3_{i+1}", "bfd_m3", cid)

    # Module 4 Sub-functions
    m4_subs = [
        ("4.1 Khởi tạo phòng đấu giá (Giá sàn, bước giá)", 280),
        ("4.2 Khóa rút sản phẩm đấu giá (Trigger)", 345),
        ("4.3 Xem diễn biến & Lịch sử trả giá realtime", 410),
        ("4.4 Tiếp nhận lượt ra giá (sp_DatGia hợp lệ)", 475),
        ("4.5 Thông báo bị vượt giá (In-app Notification)", 540),
        ("4.6 Background Sweeper quét chu kỳ (60s)", 605),
        ("4.7 Chốt phiên & Tự động tạo Đơn hàng thắng", 670),
    ]
    for i, (text, y) in enumerate(m4_subs):
        cid = f"bfd_m4_{i+1}"
        bfd.add_bfd_node(cid, text, 1580, y, 310, 45, level=2)
        bfd.add_tree_edge(f"edge_m4_{i+1}", "bfd_m4", cid)

    # Module 5 Sub-functions
    m5_subs = [
        ("5.1 Shop duyệt đơn, đóng gói, bàn giao vận chuyển", 280),
        ("5.2 Kiểm soát chuyển trạng thái (fn_ChuyenTrangThai...)", 345),
        ("5.3 Mở khiếu nại tranh chấp (Tải ảnh chứng cứ)", 410),
        ("5.4 Người bán phản hồi giải trình tranh chấp", 475),
        ("5.5 Admin phán quyết tranh chấp (Hoàn / Trả tiền)", 540),
        ("5.6 Đánh giá chất lượng sau mua (Đơn COMPLETED)", 605),
        ("5.7 Báo cáo sai phạm toàn sàn (Sản phẩm, Shop)", 670),
        ("5.8 Thống kê doanh thu sàn & Báo cáo Shop", 735),
    ]
    for i, (text, y) in enumerate(m5_subs):
        cid = f"bfd_m5_{i+1}"
        bfd.add_bfd_node(cid, text, 2080, y, 310, 45, level=2)
        bfd.add_tree_edge(f"edge_m5_{i+1}", "bfd_m5", cid)

    mxfile.append(bfd.diagram_elem)

    # =========================================================================
    # TAB 2: USE CASE TỔNG QUAN HỆ THỐNG - RỘNG RÃI, BỐ CỤC ĐẸP
    # =========================================================================
    uc_all = BeautifulDiagramBuilder("diag_uc_all", "Use Case - Tổng Quan Hệ Thống", 2200, 1400)
    
    # Boundary
    uc_all.add_boundary("b_all", "HỆ THỐNG SÀN GIAO DỊCH ĐỒ CŨ (SECONDHAND MARKETPLACE)", 360, 40, 1380, 1260)

    # Actors
    uc_all.add_actor("act_guest", "Khách Vãng Lai<br>(Guest)", 100, 120)
    uc_all.add_actor("act_user", "Người Dùng<br>(User)", 100, 440)
    uc_all.add_actor("act_buyer", "Người Mua<br>(Buyer)", 100, 800)
    uc_all.add_actor("act_seller", "Người Bán<br>(Seller)", 1920, 440)
    uc_all.add_actor("act_admin", "Quản Trị Viên<br>(Admin)", 1920, 900)
    uc_all.add_actor("act_ai", "Dịch Vụ AI<br>(Anthropic API)", 1920, 160)
    uc_all.add_actor("act_timer", "Hệ Thống Nền<br>(Auction Sweeper)", 1920, 1150)

    # Generalizations
    uc_all.add_rel("rel_g1", "", "act_user", "act_guest", rel_type="generalization")
    uc_all.add_rel("rel_g2", "", "act_buyer", "act_user", rel_type="generalization")
    uc_all.add_rel("rel_g3", "", "act_seller", "act_user", rel_type="generalization")

    # Column 1 Use Cases (x=440)
    uc_all.add_usecase("uc_1_1", "UC1. Đăng ký & Đăng nhập<br>(Cookie / JWT API)", 440, 100, 260, 85)
    uc_all.add_usecase("uc_2_1", "UC4. Đăng tin & Album ảnh<br>(Tối đa 10 ảnh, chọn ảnh chính)", 440, 300, 260, 85)
    uc_all.add_usecase("uc_3_1", "UC7. Tìm kiếm & Lọc sản phẩm<br>(Độ mới, danh mục, giá)", 440, 520, 260, 85)
    uc_all.add_usecase("uc_4_1", "UC10. Tạo phòng đấu giá<br>(Khóa rút sản phẩm)", 440, 740, 260, 85)
    uc_all.add_usecase("uc_5_1", "UC13. Quản lý đơn bán & Vận chuyển<br>(Seller Orders)", 440, 960, 260, 85)

    # Column 2 Use Cases (x=780)
    uc_all.add_usecase("uc_1_2", "UC2. Quản lý Hồ sơ & Cửa hàng<br>(Profile & Shop)", 780, 100, 260, 85)
    uc_all.add_usecase("uc_2_2", "UC5. Kiểm duyệt & Quét từ cấm<br>(Auto Scan & Admin duyệt)", 780, 300, 260, 85)
    uc_all.add_usecase("uc_3_2", "UC8. Quản lý Giỏ hàng đồ cũ<br>(Chặn tự mua hàng mình)", 780, 520, 260, 85)
    uc_all.add_usecase("uc_4_2", "UC11. Tham gia trả giá (Bid)<br>(Kiểm tra bước giá realtime)", 780, 740, 260, 85)
    uc_all.add_usecase("uc_5_2", "UC14. Khiếu nại & Tranh chấp<br>(Đóng băng đơn & Phán quyết)", 780, 960, 260, 85)

    # Column 3 Use Cases (x=1120)
    uc_all.add_usecase("uc_1_3", "UC3. Quản trị User, Shop & Logs<br>(Admin bảo vệ & Khóa vi phạm)", 1120, 100, 260, 85)
    uc_all.add_usecase("uc_2_3", "UC6. Trợ lý AI tư vấn & soạn tin<br>(Anthropic Tool Use)", 1120, 300, 260, 85)
    uc_all.add_usecase("uc_3_3", "UC9. Checkout & Giữ chỗ độc bản<br>(Tách đơn theo từng Shop)", 1120, 520, 260, 85)
    uc_all.add_usecase("uc_4_3", "UC12. Chốt phiên tự động<br>(Background Sweeper 60s)", 1120, 740, 260, 85)
    uc_all.add_usecase("uc_5_3", "UC15. Đánh giá chất lượng sau mua<br>(Đơn hàng COMPLETED)", 1120, 960, 260, 85)
    uc_all.add_usecase("uc_5_4", "UC16. Báo cáo sai phạm & Cảnh báo<br>(Reports & Notifications)", 1120, 1140, 260, 85)

    # Connect Actors to Use Cases
    uc_all.add_assoc("a_g_1", "act_guest", "uc_1_1")
    uc_all.add_assoc("a_g_7", "act_guest", "uc_3_1")

    uc_all.add_assoc("a_u_2", "act_user", "uc_1_2")
    uc_all.add_assoc("a_u_6", "act_user", "uc_2_3")

    uc_all.add_assoc("a_b_8", "act_buyer", "uc_3_2")
    uc_all.add_assoc("a_b_9", "act_buyer", "uc_3_3")
    uc_all.add_assoc("a_b_11", "act_buyer", "uc_4_2")
    uc_all.add_assoc("a_b_14", "act_buyer", "uc_5_2")
    uc_all.add_assoc("a_b_15", "act_buyer", "uc_5_3")
    uc_all.add_assoc("a_b_16", "act_buyer", "uc_5_4")

    uc_all.add_assoc("a_s_2", "act_seller", "uc_1_2")
    uc_all.add_assoc("a_s_4", "act_seller", "uc_2_1")
    uc_all.add_assoc("a_s_10", "act_seller", "uc_4_1")
    uc_all.add_assoc("a_s_13", "act_seller", "uc_5_1")
    uc_all.add_assoc("a_s_14", "act_seller", "uc_5_2")

    uc_all.add_assoc("a_ad_3", "act_admin", "uc_1_3")
    uc_all.add_assoc("a_ad_5", "act_admin", "uc_2_2")
    uc_all.add_assoc("a_ad_14", "act_admin", "uc_5_2")
    uc_all.add_assoc("a_ad_16", "act_admin", "uc_5_4")

    uc_all.add_assoc("a_ai_6", "act_ai", "uc_2_3")
    uc_all.add_assoc("a_ti_12", "act_timer", "uc_4_3")

    mxfile.append(uc_all.diagram_elem)

    # =========================================================================
    # TAB 3: USE CASE PHÂN HỆ 1 - TÀI KHOẢN & QUẢN TRỊ
    # =========================================================================
    uc_m1 = BeautifulDiagramBuilder("diag_uc_m1", "Use Case - 1.0 Tài Khoản & Quản Trị", 2000, 1300)
    uc_m1.add_boundary("b_m1", "PHÂN HỆ 1: QUẢN LÝ TÀI KHOẢN, CỬA HÀNG & QUẢN TRỊ HỆ THỐNG", 360, 40, 1260, 1140)

    # Actors
    uc_m1.add_actor("act_guest", "Khách Vãng Lai", 100, 200)
    uc_m1.add_actor("act_user", "Người Dùng (User)", 100, 550)
    uc_m1.add_actor("act_admin", "Quản Trị Viên (Admin)", 1760, 550)

    # Use Cases
    uc_m1.add_usecase("uc1_reg", "Đăng ký tài khoản mới<br>(Băm mật khẩu BCrypt)", 460, 120, 260, 85)
    uc_m1.add_usecase("uc1_login", "Đăng nhập hệ thống<br>(Cookie MVC / JWT Bearer)", 460, 280, 260, 85)
    uc_m1.add_usecase("uc1_token", "Cấp phát JWT Bearer Token<br>(Endpoint /api/auth/login)", 840, 280, 260, 85)
    uc_m1.add_usecase("uc1_profile", "Cập nhật hồ sơ cá nhân<br>& Đổi mật khẩu", 460, 480, 260, 85)
    uc_m1.add_usecase("uc1_shop", "Đăng ký mở Cửa hàng (Shop)<br>(sp_TaoCuaHang, max 1 shop)", 460, 680, 260, 85)
    uc_m1.add_usecase("uc1_adm_user", "Quản lý Người dùng & Shop<br>(Khóa/Mở kèm lý do LyDoKhoa)", 1160, 480, 270, 85)
    uc_m1.add_usecase("uc1_adm_cat", "Quản lý Danh mục & Tình trạng<br>(Ẩn mềm IsActive khi có ràng buộc)", 1160, 680, 270, 85)
    uc_m1.add_usecase("uc1_adm_log", "Ghi & Tra cứu Audit Logs<br>(sp_GhiNhatKy: IP, action, date)", 1160, 880, 270, 85)

    # Connections & Includes
    uc_m1.add_assoc("a1_1", "act_guest", "uc1_reg")
    uc_m1.add_assoc("a1_2", "act_guest", "uc1_login")
    uc_m1.add_assoc("a1_3", "act_user", "uc1_login")
    uc_m1.add_assoc("a1_4", "act_user", "uc1_profile")
    uc_m1.add_assoc("a1_5", "act_user", "uc1_shop")

    uc_m1.add_assoc("a1_6", "act_admin", "uc1_adm_user")
    uc_m1.add_assoc("a1_7", "act_admin", "uc1_adm_cat")
    uc_m1.add_assoc("a1_8", "act_admin", "uc1_adm_log")

    uc_m1.add_rel("rel_login_jwt", "", "uc1_login", "uc1_token", rel_type="extend")
    uc_m1.add_rel("rel_adm_log", "", "uc1_adm_user", "uc1_adm_log", rel_type="include")

    mxfile.append(uc_m1.diagram_elem)

    # =========================================================================
    # TAB 4: USE CASE PHÂN HỆ 2 - ĐĂNG BÁN, KIỂM DUYỆT & TRỢ LÝ AI
    # =========================================================================
    uc_m2 = BeautifulDiagramBuilder("diag_uc_m2", "Use Case - 2.0 Đăng Bán & Kiểm Duyệt", 2100, 1300)
    uc_m2.add_boundary("b_m2", "PHÂN HỆ 2: ĐĂNG BÁN, KIỂM DUYỆT NỘI DUNG & TRỢ LÝ AI", 360, 40, 1320, 1180)

    # Actors
    uc_m2.add_actor("act_seller", "Người Bán (Seller)", 100, 250)
    uc_m2.add_actor("act_buyer", "Người Mua (Buyer)", 100, 820)
    uc_m2.add_actor("act_admin", "Quản Trị Viên (Admin)", 1820, 380)
    uc_m2.add_actor("act_ai", "Anthropic Claude API", 1820, 850)

    # Use Cases
    uc_m2.add_usecase("uc2_post", "Soạn tin đăng & Quản lý album ảnh<br>(trg_AnhSanPham_GioiHan: max 10)", 460, 120, 270, 85)
    uc_m2.add_usecase("uc2_submit", "Gửi duyệt sản phẩm<br>(sp_GuiDuyetSanPham)", 460, 300, 270, 85)
    uc_m2.add_usecase("uc2_scan", "Quét tự động từ khóa cấm<br>(fn_TimTuKhoaCam: HIGH/LOW)", 850, 300, 270, 85)
    uc_m2.add_usecase("uc2_mod", "Admin thẩm định duyệt tin<br>(sp_DuyetSanPham / sp_TuChoi)", 1220, 220, 270, 85)
    uc_m2.add_usecase("uc2_pen", "Xử phạt vi phạm & Khóa tài khoản<br>(trg_LuotKiemDuyet_GhiViPhamLow, 3 lỗi)", 1220, 400, 270, 85)
    uc_m2.add_usecase("uc2_kw", "Quản trị bộ từ khóa cấm<br>(CRUD TuKhoaCam HIGH/LOW)", 1220, 580, 270, 85)
    uc_m2.add_usecase("uc2_ai_chat", "Trò chuyện & Hỏi đáp trợ lý AI<br>(Xác thực kép Cookie/JWT)", 460, 820, 270, 85)
    uc_m2.add_usecase("uc2_ai_tool", "Function Calling (Tra cứu giá / Soạn tin)<br>(search_products, draft_desc)", 880, 820, 270, 85)
    uc_m2.add_usecase("uc2_ai_set", "Cấu hình mô hình & Prompt AI<br>(Bảng CauHinhAI)", 1220, 980, 270, 85)

    # Connections
    uc_m2.add_assoc("a2_1", "act_seller", "uc2_post")
    uc_m2.add_assoc("a2_2", "act_seller", "uc2_submit")
    uc_m2.add_assoc("a2_3", "act_buyer", "uc2_ai_chat")
    uc_m2.add_assoc("a2_4", "act_seller", "uc2_ai_chat")

    uc_m2.add_assoc("a2_5", "act_admin", "uc2_mod")
    uc_m2.add_assoc("a2_6", "act_admin", "uc2_kw")
    uc_m2.add_assoc("a2_7", "act_admin", "uc2_ai_set")

    uc_m2.add_assoc("a2_8", "act_ai", "uc2_ai_tool")

    # Includes & Extends
    uc_m2.add_rel("rel_post_sub", "", "uc2_post", "uc2_submit", rel_type="include")
    uc_m2.add_rel("rel_sub_scan", "", "uc2_submit", "uc2_scan", rel_type="include")
    uc_m2.add_rel("rel_mod_pen", "", "uc2_mod", "uc2_pen", rel_type="extend")
    uc_m2.add_rel("rel_ai_tool", "", "uc2_ai_chat", "uc2_ai_tool", rel_type="include")

    mxfile.append(uc_m2.diagram_elem)

    # =========================================================================
    # TAB 5: USE CASE PHÂN HỆ 3 - MUA HÀNG & ĐỘC BẢN
    # =========================================================================
    uc_m3 = BeautifulDiagramBuilder("diag_uc_m3", "Use Case - 3.0 Mua Hàng & Độc Bản", 2000, 1200)
    uc_m3.add_boundary("b_m3", "PHÂN HỆ 3: MUA HÀNG, GIỎ HÀNG & ĐẶT HÀNG ĐỘC BẢN", 340, 40, 1260, 1080)

    # Actors
    uc_m3.add_actor("act_guest", "Khách Vãng Lai", 100, 160)
    uc_m3.add_actor("act_buyer", "Người Mua (Buyer)", 100, 520)
    uc_m3.add_actor("act_seller", "Người Bán (Seller)", 1760, 750)

    # Use Cases
    uc_m3.add_usecase("uc3_search", "Tìm kiếm & Lọc sản phẩm đa tiêu chí<br>(Danh mục, giá, độ mới %)", 440, 140, 270, 85)
    uc_m3.add_usecase("uc3_view", "Xem chi tiết sản phẩm đồ cũ<br>& Thông tin uy tín Shop", 840, 140, 270, 85)
    uc_m3.add_usecase("uc3_cart", "Thêm vào giỏ hàng đồ cũ<br>(sp_ThemVaoGio, chặn mua hàng mình)", 440, 380, 270, 85)
    uc_m3.add_usecase("uc3_view_cart", "Xem giỏ hàng tự gom theo Shop<br>(vw_GioHangChiTiet)", 840, 380, 270, 85)
    uc_m3.add_usecase("uc3_checkout", "Checkout & Đặt hàng từ giỏ<br>(sp_DatHangTuGio)", 440, 620, 270, 85)
    uc_m3.add_usecase("uc3_lock", "Khóa giữ chỗ độc bản (Số lượng = 1)<br>(UPDLOCK, HOLDLOCK, UX_ChiTietDonHang...)", 900, 620, 280, 85)
    uc_m3.add_usecase("uc3_split", "Tách đơn hàng tự động theo từng Shop<br>(sp_TaoDonHangTheoShop)", 900, 780, 280, 85)
    uc_m3.add_usecase("uc3_orders", "Quản lý Đơn mua & Hủy đơn hàng<br>(Theo dõi tiến trình vận chuyển)", 440, 880, 270, 85)

    # Connections
    uc_m3.add_assoc("a3_1", "act_guest", "uc3_search")
    uc_m3.add_assoc("a3_2", "act_guest", "uc3_view")
    uc_m3.add_assoc("a3_3", "act_buyer", "uc3_cart")
    uc_m3.add_assoc("a3_4", "act_buyer", "uc3_view_cart")
    uc_m3.add_assoc("a3_5", "act_buyer", "uc3_checkout")
    uc_m3.add_assoc("a3_6", "act_buyer", "uc3_orders")

    uc_m3.add_assoc("a3_7", "act_seller", "uc3_split")

    # Includes
    uc_m3.add_rel("rel_search_view", "", "uc3_search", "uc3_view", rel_type="include")
    uc_m3.add_rel("rel_cart_view", "", "uc3_cart", "uc3_view_cart", rel_type="include")
    uc_m3.add_rel("rel_check_lock", "", "uc3_checkout", "uc3_lock", rel_type="include")
    uc_m3.add_rel("rel_check_split", "", "uc3_checkout", "uc3_split", rel_type="include")

    mxfile.append(uc_m3.diagram_elem)

    # =========================================================================
    # TAB 6: USE CASE PHÂN HỆ 4 - ĐẤU GIÁ TRỰC TUYẾN & BACKGROUND SWEEPER
    # =========================================================================
    uc_m4 = BeautifulDiagramBuilder("diag_uc_m4", "Use Case - 4.0 Đấu Giá Trực Tuyến", 2000, 1200)
    uc_m4.add_boundary("b_m4", "PHÂN HỆ 4: ĐẤU GIÁ TRỰC TUYẾN & TỰ ĐỘNG HÓA CHỐT PHIÊN", 340, 40, 1260, 1080)

    # Actors
    uc_m4.add_actor("act_seller", "Người Bán (Auctioneer)", 100, 220)
    uc_m4.add_actor("act_buyer", "Người Đấu Giá (Bidder)", 100, 680)
    uc_m4.add_actor("act_timer", "Auction Sweeper Service<br>(Background HostedService)", 1760, 480)

    # Use Cases
    uc_m4.add_usecase("uc4_create", "Khởi tạo phòng đấu giá sản phẩm<br>(Giá sàn, bước giá, thời hạn)", 440, 180, 270, 85)
    uc_m4.add_usecase("uc4_lock_prod", "Khóa rút sản phẩm khỏi đấu giá<br>(Trigger trg_SanPham_ChanRutKhoiDauGia)", 900, 180, 270, 85)
    uc_m4.add_usecase("uc4_view_auc", "Theo dõi phiên đấu giá trực tiếp<br>(Polling 3s giá cao nhất hiện tại)", 440, 420, 270, 85)
    uc_m4.add_usecase("uc4_bid", "Thực hiện trả giá (sp_DatGia)<br>(Check bước giá, chặn chủ shop bid)", 440, 660, 270, 85)
    uc_m4.add_usecase("uc4_outbid_notif", "Nhận thông báo bị vượt giá<br>(In-app Realtime Notification)", 900, 660, 270, 85)
    uc_m4.add_usecase("uc4_sweep", "Quét phiên hết giờ theo chu kỳ 60s<br>(AuctionBackgroundService)", 900, 420, 270, 85)
    uc_m4.add_usecase("uc4_close", "Chốt người thắng & Tự sinh DonHang<br>(Chuyển trạng thái ENDED / CANCELLED)", 900, 880, 270, 85)

    # Connections
    uc_m4.add_assoc("a4_1", "act_seller", "uc4_create")
    uc_m4.add_assoc("a4_2", "act_buyer", "uc4_view_auc")
    uc_m4.add_assoc("a4_3", "act_buyer", "uc4_bid")

    uc_m4.add_assoc("a4_4", "act_timer", "uc4_sweep")
    uc_m4.add_assoc("a4_5", "act_timer", "uc4_close")

    # Includes
    uc_m4.add_rel("rel_create_lock", "", "uc4_create", "uc4_lock_prod", rel_type="include")
    uc_m4.add_rel("rel_bid_notif", "", "uc4_bid", "uc4_outbid_notif", rel_type="include")
    uc_m4.add_rel("rel_sweep_close", "", "uc4_sweep", "uc4_close", rel_type="include")

    mxfile.append(uc_m4.diagram_elem)

    # =========================================================================
    # TAB 7: USE CASE PHÂN HỆ 5 - ĐƠN HÀNG, TRANH CHẤP, ĐÁNH GIÁ & BÁO CÁO
    # =========================================================================
    uc_m5 = BeautifulDiagramBuilder("diag_uc_m5", "Use Case - 5.0 Đơn Hàng & Hậu Mãi", 2100, 1300)
    uc_m5.add_boundary("b_m5", "PHÂN HỆ 5: QUẢN LÝ ĐƠN HÀNG, TRANH CHẤP, ĐÁNH GIÁ & BÁO CÁO", 340, 40, 1320, 1180)

    # Actors
    uc_m5.add_actor("act_seller", "Người Bán (Seller)", 100, 200)
    uc_m5.add_actor("act_buyer", "Người Mua (Buyer)", 100, 750)
    uc_m5.add_actor("act_admin", "Quản Trị Viên (Admin)", 1820, 520)

    # Use Cases
    uc_m5.add_usecase("uc5_ship", "Cập nhật tiến độ đơn bán hàng<br>(fn_ChuyenTrangThaiDonHangHopLe)", 440, 120, 270, 85)
    uc_m5.add_usecase("uc5_stats", "Xem thống kê doanh thu Shop<br>(Đơn hoàn tất, doanh thu tháng)", 850, 120, 270, 85)
    uc_m5.add_usecase("uc5_dispute_open", "Mở khiếu nại tranh chấp hàng lỗi<br>(Tải ảnh bằng chứng)", 440, 380, 270, 85)
    uc_m5.add_usecase("uc5_dispute_freeze", "Đóng băng đơn hàng (TRANH_CHAP)<br>(Ngăn chuyển tiền shop)", 880, 380, 270, 85)
    uc_m5.add_usecase("uc5_dispute_reply", "Người bán phản hồi giải trình<br>(Cung cấp bằng chứng đóng gói)", 440, 560, 270, 85)
    uc_m5.add_usecase("uc5_dispute_judge", "Admin phán quyết tranh chấp<br>(sp_PhanQuyetTranhChap: Hoàn / Trả)", 1180, 460, 280, 85)
    uc_m5.add_usecase("uc5_review", "Đánh giá chất lượng sau mua<br>(Chỉ đơn COMPLETED, 1-5 sao)", 440, 820, 270, 85)
    uc_m5.add_usecase("uc5_report", "Báo cáo gian lận Shop / Sản phẩm<br>(Report lừa đảo, hàng cấm)", 440, 1000, 270, 85)
    uc_m5.add_usecase("uc5_report_solve", "Admin thụ lý & Xử lý báo cáo<br>(Gỡ sản phẩm, khóa tài khoản)", 1180, 920, 280, 85)

    # Connections
    uc_m5.add_assoc("a5_1", "act_seller", "uc5_ship")
    uc_m5.add_assoc("a5_2", "act_seller", "uc5_stats")
    uc_m5.add_assoc("a5_3", "act_buyer", "uc5_dispute_open")
    uc_m5.add_assoc("a5_4", "act_seller", "uc5_dispute_reply")
    uc_m5.add_assoc("a5_5", "act_admin", "uc5_dispute_judge")
    uc_m5.add_assoc("a5_6", "act_buyer", "uc5_review")
    uc_m5.add_assoc("a5_7", "act_buyer", "uc5_report")
    uc_m5.add_assoc("a5_8", "act_admin", "uc5_report_solve")

    # Includes
    uc_m5.add_rel("rel_disp_freeze", "", "uc5_dispute_open", "uc5_dispute_freeze", rel_type="include")
    uc_m5.add_rel("rel_disp_judge", "", "uc5_dispute_freeze", "uc5_dispute_judge", rel_type="include")
    uc_m5.add_rel("rel_rep_solve", "", "uc5_report", "uc5_report_solve", rel_type="include")

    mxfile.append(uc_m5.diagram_elem)

    # =========================================================================
    # TAB 8: DFD MỨC 0 - NGỮ CẢNH (BẢO TOÀN TÁC NHÂN & LUỒNG, RỘNG RÃI)
    # =========================================================================
    d0 = BeautifulDiagramBuilder("diag_d0", "DFD Mức 0 - Ngữ Cảnh", 2400, 1500)
    
    # Process 0.0 ở chính giữa
    d0.add_process("p0", "<b>0.0 HỆ THỐNG SÀN THƯƠNG MẠI<br>ĐIỆN TỬ ĐỒ CŨ<br>(SECONDHAND MARKETPLACE)</b>", 
                   1050, 600, 280, 200)
    
    # 4 External Entities
    d0.add_entity("e1", "<b>E1: Người Mua<br>(Buyer)</b>", 100, 630, 220, 140)
    d0.add_entity("e2", "<b>E2: Người Bán<br>(Seller)</b>", 2060, 630, 220, 140)
    d0.add_entity("e3", "<b>E3: Quản Trị Viên<br>(Admin)</b>", 1080, 60, 220, 120)
    d0.add_entity("e4", "<b>E4: Dịch Vụ AI Bên Ngoài<br>(Anthropic Claude API)</b>", 1060, 1220, 260, 110)

    # Luồng giữa E1 và P0 (Inflows: E1 -> P0)
    d0.add_flow("f0_e1_p0_1", "1. Đăng ký / Đăng nhập / Đổi hồ sơ cá nhân", "e1", "p0", [(460, 650), (460, 660)])
    d0.add_flow("f0_e1_p0_2", "2. Tìm kiếm, Giỏ hàng & Checkout giữ chỗ độc bản", "e1", "p0", [(420, 690), (420, 700)])
    d0.add_flow("f0_e1_p0_3", "3. Lượt trả giá đấu giá (Bidding)", "e1", "p0", [(380, 730), (380, 740)])
    d0.add_flow("f0_e1_p0_4", "4. Hủy đơn, Khiếu nại tranh chấp, Đánh giá sau mua & Báo cáo", "e1", "p0", [(340, 770), (340, 780)])
    d0.add_flow("f0_e1_p0_5", "5. Câu hỏi tư vấn giá & Chatbot AI", "e1", "p0", [(300, 810), (300, 820)])

    # Outflows: P0 -> E1
    d0.add_flow("f0_p0_e1_1", "1. Kết quả xác thực (Cookie/JWT) & Thông tin Profile", "p0", "e1", [(460, 610), (460, 610)])
    d0.add_flow("f0_p0_e1_2", "2. DS sản phẩm, Giữ chỗ thành công & Hóa đơn đặt hàng", "p0", "e1", [(420, 570), (420, 570)])
    d0.add_flow("f0_p0_e1_3", "3. Diễn biến đấu giá & Thông báo trúng thầu", "p0", "e1", [(380, 530), (380, 530)])
    d0.add_flow("f0_p0_e1_4", "4. Kết quả phán quyết tranh chấp & Thông báo chuông", "p0", "e1", [(340, 490), (340, 490)])
    d0.add_flow("f0_p0_e1_5", "5. Phản hồi tư vấn, gợi ý sản phẩm phù hợp từ AI", "p0", "e1", [(300, 450), (300, 450)])

    # Luồng giữa E2 và P0 (Inflows: E2 -> P0)
    d0.add_flow("f0_e2_p0_1", "1. Hồ sơ Cửa hàng (Shop) & Địa chỉ kho hàng", "e2", "p0", [(1920, 650), (1920, 660)])
    d0.add_flow("f0_e2_p0_2", "2. Đăng bài sản phẩm, Upload album ảnh & Gửi duyệt", "e2", "p0", [(1960, 690), (1960, 700)])
    d0.add_flow("f0_e2_p0_3", "3. Cấu hình phòng đấu giá (Giá sàn, Bước giá, Hạn)", "e2", "p0", [(2000, 730), (2000, 740)])
    d0.add_flow("f0_e2_p0_4", "4. Cập nhật tiến độ đơn bán (Xác nhận, Giao vận)", "e2", "p0", [(2040, 770), (2040, 780)])
    d0.add_flow("f0_e2_p0_5", "5. Phản hồi giải trình khiếu nại tranh chấp", "e2", "p0", [(2080, 810), (2080, 820)])

    # Outflows: P0 -> E2
    d0.add_flow("f0_p0_e2_1", "1. Thẩm định Shop & Cấp quyền bán hàng", "p0", "e2", [(1920, 610), (1920, 610)])
    d0.add_flow("f0_p0_e2_2", "2. Kết quả kiểm duyệt tin (Duyệt/Từ chối + Lý do vi phạm)", "p0", "e2", [(1960, 570), (1960, 570)])
    d0.add_flow("f0_p0_e2_3", "3. Thông báo Đơn bán mới & Đơn đấu giá thắng", "p0", "e2", [(2000, 530), (2000, 530)])
    d0.add_flow("f0_p0_e2_4", "4. Quyết định phán quyết tranh chấp & Cảnh báo vi phạm", "p0", "e2", [(2040, 490), (2040, 490)])
    d0.add_flow("f0_p0_e2_5", "5. Báo cáo Doanh thu & Thống kê đơn bán của Shop", "p0", "e2", [(2080, 450), (2080, 450)])

    # Luồng giữa E3 và P0 (Inflows: E3 -> P0)
    d0.add_flow("f0_e3_p0_1", "1. Khóa/Mở tài khoản & Shop kèm lý do", "e3", "p0", [(1120, 360), (1120, 370)])
    d0.add_flow("f0_e3_p0_2", "2. CRUD Danh mục & Bảng chuẩn tình trạng", "e3", "p0", [(1170, 360), (1170, 370)])
    d0.add_flow("f0_e3_p0_3", "3. Quản lý Từ khóa cấm (Mức độ HIGH/LOW)", "e3", "p0", [(1220, 360), (1220, 370)])
    d0.add_flow("f0_e3_p0_4", "4. Duyệt/Từ chối sản phẩm & Gỡ bài vi phạm", "e3", "p0", [(1270, 360), (1270, 370)])
    d0.add_flow("f0_e3_p0_5", "5. Phán quyết tranh chấp, Xử lý Báo cáo & Cấu hình AI", "e3", "p0", [(1320, 360), (1320, 370)])

    # Outflows: P0 -> E3
    d0.add_flow("f0_p0_e3_1", "1. Báo cáo thống kê tổng quan sàn & Doanh thu", "p0", "e3", [(1060, 360), (1060, 360)])
    d0.add_flow("f0_p0_e3_2", "2. Hàng chờ duyệt sản phẩm (Kèm cờ cảnh báo AI/Từ cấm)", "p0", "e3", [(1010, 360), (1010, 360)])
    d0.add_flow("f0_p0_e3_3", "3. Danh sách Tranh chấp & Báo cáo vi phạm cần xử lý", "p0", "e3", [(960, 360), (960, 360)])
    d0.add_flow("f0_p0_e3_4", "4. Nhật ký kiểm toán toàn hệ thống (Audit Logs)", "p0", "e3", [(910, 360), (910, 360)])

    # Luồng giữa P0 và E4
    d0.add_flow("f0_p0_e4_1", "Yêu cầu phân tích prompt, metadata sản phẩm & Tool Call Request", "p0", "e4", [(1140, 1020), (1140, 1020)])
    d0.add_flow("f0_e4_p0_1", "Kết quả phân tích nhãn vi phạm, Gợi ý định giá & Chat completion payload", "e4", "p0", [(1240, 1020), (1240, 1020)])

    mxfile.append(d0.diagram_elem)

    # =========================================================================
    # TAB 9: DFD MỨC 1 - MỨC ĐỈNH (BẢO TOÀN 100% LUỒNG & TÁC NHÂN)
    # =========================================================================
    d1 = BeautifulDiagramBuilder("diag_d1", "DFD Mức 1 - Mức Đỉnh", 2700, 1800)
    
    # 5 Processes đỉnh
    d1.add_process("p1", "<b>1.0 QUẢN LÝ TÀI KHOẢN,<br>CỬA HÀNG & HỆ THỐNG</b>", 600, 260, 210, 210)
    d1.add_process("p2", "<b>2.0 ĐĂNG BÁN, KIỂM DUYỆT<br>NỘI DUNG & TRỢ LÝ AI</b>", 1500, 260, 210, 210)
    d1.add_process("p3", "<b>3.0 TÌM KIẾM, GIỎ HÀNG<br>& ĐẶT HÀNG ĐỘC BẢN</b>", 600, 920, 210, 210)
    d1.add_process("p4", "<b>4.0 ĐẤU GIÁ TRỰC TUYẾN<br>& CHỐT PHIÊN TỰ ĐỘNG</b>", 1500, 920, 210, 210)
    d1.add_process("p5", "<b>5.0 XỬ LÝ ĐƠN HÀNG,<br>TRANH CHẤP & HẬU MÃI</b>", 1050, 1400, 220, 220)

    # 4 External Entities
    d1.add_entity("e1", "<b>E1: Người Mua<br>(Buyer)</b>", 80, 700, 200, 120)
    d1.add_entity("e2", "<b>E2: Người Bán<br>(Seller)</b>", 2400, 700, 200, 120)
    d1.add_entity("e3", "<b>E3: Quản Trị Viên<br>(Admin)</b>", 1060, 40, 220, 100)
    d1.add_entity("e4", "<b>E4: Dịch Vụ AI Anthropic Claude API</b>", 2150, 240, 240, 90)

    # 7 Data Stores
    d1.add_store("ds1", "<b>D1: Người Dùng & Cửa Hàng</b><br>(NguoiDung, CuaHang)", 440, 600, 240, 65)
    d1.add_store("ds2", "<b>D2: Danh Mục & Sản Phẩm</b><br>(DanhMuc, SanPham, AnhSanPham)", 1040, 440, 250, 65)
    d1.add_store("ds3", "<b>D3: Giỏ Hàng & Đơn Hàng</b><br>(GioHang, DonHang, ChiTiet)", 680, 1240, 240, 65)
    d1.add_store("ds4", "<b>D4: Đấu Giá Trực Tuyến</b><br>(PhienDauGia, LuotDatGia)", 1850, 990, 230, 65)
    d1.add_store("ds5", "<b>D5: Kiểm Duyệt & Vi Phạm</b><br>(TuKhoaCam, LuotKiemDuyet, PhanTichAI)", 1850, 440, 250, 65)
    d1.add_store("ds6", "<b>D6: Khiếu Nại & Đánh Giá</b><br>(TranhChap, BaoCao, DanhGia)", 1400, 1400, 240, 65)
    d1.add_store("ds7", "<b>D7: Thông Báo & Nhật Ký</b><br>(ThongBao, NhatKy, CauHinhAI)", 1050, 780, 240, 65)

    # Connect Entities to Processes (Bảo toàn đầy đủ luồng Mức 0)
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
    d1.add_flow("f1_p2_e4", "Prompt, Metadata & Tool Call Request", "p2", "e4")
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
    # TAB 10: DFD MỨC 2 - GIAI ĐOẠN 1: QUẢN LÝ TÀI KHOẢN & HỆ THỐNG (1.0)
    # =========================================================================
    d2_1 = BeautifulDiagramBuilder("diag_d2_1", "DFD Mức 2 - 1.0 Tài Khoản & Cửa Hàng", 2300, 1500)
    
    # External Entities (Bảo toàn)
    d2_1.add_entity("e1", "<b>E1: Người Mua<br>(Buyer)</b>", 100, 320, 200, 120)
    d2_1.add_entity("e2", "<b>E2: Người Bán<br>(Seller)</b>", 100, 780, 200, 120)
    d2_1.add_entity("e3", "<b>E3: Quản Trị Viên<br>(Admin)</b>", 1980, 520, 220, 120)

    # Sub-processes
    d2_1.add_process("p1_1", "<b>1.1 Đăng Ký &<br>Xác Thực Tài Khoản</b><br><font size='1'>(Cookie MVC, JWT API,<br>BCrypt Hash)</font>", 480, 160, 200, 200)
    d2_1.add_process("p1_2", "<b>1.2 Quản Lý Hồ Sơ<br>& Đổi Mật Khẩu</b><br><font size='1'>(Địa chỉ nhận, SĐT)</font>", 480, 480, 200, 200)
    d2_1.add_process("p1_3", "<b>1.3 Đăng Ký & Quản Lý<br>Cửa Hàng (Shop)</b><br><font size='1'>(sp_TaoCuaHang, max 1 shop)</font>", 480, 800, 200, 200)
    d2_1.add_process("p1_4", "<b>1.4 Quản Trị Người Dùng<br>& Khóa / Mở Khóa</b><br><font size='1'>(Bảo vệ Admin, ghi lý do)</font>", 1450, 240, 200, 200)
    d2_1.add_process("p1_5", "<b>1.5 Quản Trị Danh Mục<br>& Tình Trạng Hàng</b><br><font size='1'>(Xóa mềm ẩn danh mục)</font>", 1450, 620, 200, 200)
    d2_1.add_process("p1_6", "<b>1.6 Ghi & Truy Vấn<br>Nhật Ký Kiểm Toán</b><br><font size='1'>(sp_GhiNhatKy, IP, Lọc log)</font>", 1450, 1020, 200, 200)

    # Data Stores
    d2_1.add_store("ds1_u", "<b>D1.1: Người Dùng</b><br>(NguoiDung: Email, PassHash, TrangThai)", 960, 260, 250, 65)
    d2_1.add_store("ds1_s", "<b>D1.2: Cửa Hàng</b><br>(CuaHang: TenShop, DiaChiKho)", 960, 800, 250, 65)
    d2_1.add_store("ds2_c", "<b>D2.1: Danh Mục & Tình Trạng</b><br>(DanhMuc, TinhTrangSanPham)", 960, 600, 250, 65)
    d2_1.add_store("ds7_l", "<b>D7.1: Nhật Ký Hệ Thống</b><br>(NhatKyHeThong: HanhDong, IP, Time)", 960, 1040, 250, 65)

    # Inflows / Outflows (Bảo toàn)
    d2_1.add_flow("f_e1_p1_1", "Yêu cầu Đăng ký / Đăng nhập", "e1", "p1_1")
    d2_1.add_flow("f_p1_1_e1", "Phiên xác thực Cookie / JWT Token", "p1_1", "e1")
    d2_1.add_flow("f_e1_p1_2", "Cập nhật thông tin cá nhân & Đổi pass", "e1", "p1_2")
    d2_1.add_flow("f_p1_2_e1", "Kết quả cập nhật hồ sơ cá nhân", "p1_2", "e1")
    
    d2_1.add_flow("f_e2_p1_3", "Hồ sơ mở Shop, Địa chỉ kho hàng", "e2", "p1_3")
    d2_1.add_flow("f_p1_3_e2", "Trạng thái Shop & Cấp quyền chủ shop", "p1_3", "e2")

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
    # TAB 11: DFD MỨC 2 - GIAI ĐOẠN 2: ĐĂNG BÁN, KIỂM DUYỆT & TRỢ LÝ AI (2.0)
    # =========================================================================
    d2_2 = BeautifulDiagramBuilder("diag_d2_2", "DFD Mức 2 - 2.0 Đăng Bán & Kiểm Duyệt", 2400, 1500)
    
    # External Entities
    d2_2.add_entity("e1", "<b>E1: Người Mua<br>(Buyer)</b>", 100, 950, 200, 120)
    d2_2.add_entity("e2", "<b>E2: Người Bán<br>(Seller)</b>", 100, 250, 200, 120)
    d2_2.add_entity("e3", "<b>E3: Quản Trị Viên<br>(Admin)</b>", 2050, 280, 220, 120)
    d2_2.add_entity("e4", "<b>E4: Dịch Vụ AI Anthropic Claude API</b>", 1100, 1250, 260, 100)

    # Sub-processes
    d2_2.add_process("p2_1", "<b>2.1 Soạn Tin Đăng &<br>Quản Lý Album Ảnh</b><br><font size='1'>(Tối đa 10 ảnh, chọn ảnh chính)</font>", 480, 180, 200, 200)
    d2_2.add_process("p2_2", "<b>2.2 Gửi Duyệt & Quét<br>Tự Động Từ Khóa Cấm</b><br><font size='1'>(sp_GuiDuyetSanPham,<br>fn_TimTuKhoaCam)</font>", 980, 180, 200, 200)
    d2_2.add_process("p2_3", "<b>2.3 Phân Tích & Trợ Lý<br>Thông Minh AI</b><br><font size='1'>(Tool Use: search, advise,<br>draft description)</font>", 680, 920, 210, 210)
    d2_2.add_process("p2_4", "<b>2.4 Quản Trị Từ Cấm<br>& Cấu Hình Tham Số AI</b><br><font size='1'>(CRUD TuKhoaCam HIGH/LOW,<br>Bảng CauHinhAI)</font>", 1520, 580, 210, 210)
    d2_2.add_process("p2_5", "<b>2.5 Admin Thẩm Định<br>Duyệt Tin & Chế Tài</b><br><font size='1'>(sp_DuyetSanPham, sp_TuChoi,<br>Khóa khi đủ 3 vi phạm)</font>", 1520, 180, 210, 210)

    # Data Stores
    d2_2.add_store("ds2_p", "<b>D2.2: Sản Phẩm & Ảnh</b><br>(SanPham, AnhSanPham)", 520, 560, 240, 65)
    d2_2.add_store("ds5_m", "<b>D5.1: Kiểm Duyệt & Từ Cấm</b><br>(TuKhoaCam, LuotKiemDuyet, PhanTichAI)", 1050, 560, 260, 65)
    d2_2.add_store("ds5_v", "<b>D5.2: Lịch Sử Vi Phạm</b><br>(LichSuViPham, MaLuotKiemDuyet Unique)", 1650, 40, 260, 65)
    d2_2.add_store("ds1_u", "<b>D1.1: Người Dùng (Seller)</b><br>(Cập nhật khóa tài khoản)", 1260, 40, 240, 65)
    d2_2.add_store("ds7_n", "<b>D7.2: Thông Báo & Cấu Hình</b><br>(ThongBao, CauHinhAI)", 1080, 880, 250, 65)

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
    # TAB 12: DFD MỨC 2 - GIAI ĐOẠN 3: TÌM KIẾM, GIỎ HÀNG & ĐẶT HÀNG ĐỘC BẢN (3.0)
    # =========================================================================
    d2_3 = BeautifulDiagramBuilder("diag_d2_3", "DFD Mức 2 - 3.0 Mua Hàng & Độc Bản", 2300, 1400)
    
    # External Entities
    d2_3.add_entity("e1", "<b>E1: Người Mua<br>(Buyer)</b>", 100, 480, 200, 120)
    d2_3.add_entity("e2", "<b>E2: Người Bán<br>(Seller)</b>", 2000, 720, 200, 120)

    # Sub-processes
    d2_3.add_process("p3_1", "<b>3.1 Tra Cứu, Tìm Kiếm<br>& Xem Chi Tiết</b><br><font size='1'>(Lọc danh mục, khoảng giá,<br>độ mới %, ảnh chi tiết)</font>", 500, 200, 210, 210)
    d2_3.add_process("p3_2", "<b>3.2 Quản Lý Giỏ Hàng<br>Đồ Cũ Độc Bản</b><br><font size='1'>(sp_ThemVaoGio, chặn người bán<br>tự mua hàng của mình)</font>", 500, 680, 210, 210)
    d2_3.add_process("p3_3", "<b>3.3 Checkout, Giữ Chỗ<br>& Tách Đơn Theo Shop</b><br><font size='1'>(sp_DatHangTuGio, UPDLOCK/HOLDLOCK,<br>Index UX_ChiTietDonHang_DangGiuCho)</font>", 1280, 680, 230, 230)

    # Data Stores
    d2_3.add_store("ds2_p", "<b>D2.2: Sản Phẩm Đang Bán</b><br>(SanPham ACTIVE, AnhSanPham)", 920, 220, 250, 65)
    d2_3.add_store("ds3_g", "<b>D3.1: Giỏ Hàng</b><br>(GioHang, ChiTietGioHang)", 920, 700, 240, 65)
    d2_3.add_store("ds3_d", "<b>D3.2: Đơn Hàng & Chi Tiết</b><br>(DonHang, ChiTietDonHang)", 1680, 460, 250, 65)
    d2_3.add_store("ds7_n", "<b>D7.2: Thông Báo Hệ Thống</b><br>(ThongBao: Đơn mới cho shop/buyer)", 1280, 1100, 250, 65)

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
    # TAB 13: DFD MỨC 2 - GIAI ĐOẠN 4: ĐẤU GIÁ TRỰC TUYẾN & CHỐT PHIÊN (4.0)
    # =========================================================================
    d2_4 = BeautifulDiagramBuilder("diag_d2_4", "DFD Mức 2 - 4.0 Đấu Giá Trực Tuyến", 2300, 1400)
    
    # External Entities
    d2_4.add_entity("e1", "<b>E1: Người Mua<br>(Bidder)</b>", 100, 620, 200, 120)
    d2_4.add_entity("e2", "<b>E2: Người Bán<br>(Auctioneer / Shop)</b>", 100, 220, 200, 120)

    # Sub-processes
    d2_4.add_process("p4_1", "<b>4.1 Thiết Lập Phòng<br>Đấu Giá Sản Phẩm</b><br><font size='1'>(Giá khởi điểm, bước giá,<br>hạn kết thúc, khóa rút hàng)</font>", 520, 180, 210, 210)
    d2_4.add_process("p4_2", "<b>4.2 Tiếp Nhận & Xác Thực<br>Lượt Ra Giá (Bid)</b><br><font size='1'>(sp_DatGia, kiểm tra bước giá,<br>chặn shop tự trả giá)</font>", 520, 600, 210, 210)
    d2_4.add_process("p4_3", "<b>4.3 Background Sweeper<br>& Tự Động Chốt Đơn</b><br><font size='1'>(AuctionBackgroundService 60s,<br>chốt phiên, sinh DonHang)</font>", 1320, 400, 220, 220)

    # Data Stores
    d2_4.add_store("ds2_p", "<b>D2.2: Sản Phẩm</b><br>(Khóa rút hàng: trg_SanPham_ChanRutKhoiDauGia)", 980, 160, 250, 65)
    d2_4.add_store("ds4_a", "<b>D4.1: Phiên & Lượt Đặt Giá</b><br>(PhienDauGia, LuotDatGia)", 980, 520, 250, 65)
    d2_4.add_store("ds3_d", "<b>D3.2: Đơn Hàng</b><br>(Tự sinh đơn thắng đấu giá)", 1750, 300, 240, 65)
    d2_4.add_store("ds7_n", "<b>D7.2: Thông Báo</b><br>(Vượt giá, trúng đấu giá, chốt phiên)", 1750, 680, 240, 65)

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
    # TAB 14: DFD MỨC 2 - GIAI ĐOẠN 5: ĐƠN HÀNG, TRANH CHẤP & HẬU MÃI (5.0)
    # =========================================================================
    d2_5 = BeautifulDiagramBuilder("diag_d2_5", "DFD Mức 2 - 5.0 Đơn Hàng & Tranh Chấp", 2400, 1500)
    
    # External Entities
    d2_5.add_entity("e1", "<b>E1: Người Mua<br>(Buyer)</b>", 100, 620, 200, 120)
    d2_5.add_entity("e2", "<b>E2: Người Bán<br>(Seller)</b>", 100, 200, 200, 120)
    d2_5.add_entity("e3", "<b>E3: Quản Trị Viên<br>(Admin)</b>", 2050, 620, 220, 120)

    # Sub-processes
    d2_5.add_process("p5_1", "<b>5.1 Quản Lý Vòng Đời<br>Đơn Bán & Giao Hàng</b><br><font size='1'>(fn_ChuyenTrangThaiDonHangHopLe:<br>PENDING->CONFIRMED->SHIPPING)</font>", 520, 150, 210, 210)
    d2_5.add_process("p5_2", "<b>5.2 Khởi Tạo & Phân Xử<br>Khiếu Nại Tranh Chấp</b><br><font size='1'>(Đóng băng đơn TRANH_CHAP,<br>Admin sp_PhanQuyetTranhChap)</font>", 1200, 420, 220, 220)
    d2_5.add_process("p5_3", "<b>5.3 Đánh Giá Chất Lượng<br>Sau Mua (Reviews)</b><br><font size='1'>(Chỉ đơn COMPLETED mới được đánh giá,<br>mỗi món 1 lần, 1-5 sao)</font>", 520, 880, 210, 210)
    d2_5.add_process("p5_4", "<b>5.4 Tiếp Nhận & Xử Lý<br>Báo Cáo Vi Phạm (Reports)</b><br><font size='1'>(Báo cáo sản phẩm/shop/user,<br>Admin gỡ tin/khóa tài khoản)</font>", 1200, 980, 220, 220)

    # Data Stores
    d2_5.add_store("ds3_d", "<b>D3.2: Đơn Hàng & Lịch Sử</b><br>(DonHang, LichSuTrangThaiDonHang)", 520, 540, 250, 65)
    d2_5.add_store("ds6_t", "<b>D6.1: Tranh Chấp Đơn Hàng</b><br>(TranhChap: LyDo, BangChungAnh, KetQua)", 1200, 160, 260, 65)
    d2_5.add_store("ds6_r", "<b>D6.2: Đánh Giá & Báo Cáo</b><br>(DanhGia, BaoCao: NoiDung, TrangThai)", 1200, 780, 250, 65)
    d2_5.add_store("ds1_u", "<b>D1.1: Người Dùng & Shop</b><br>(Khóa tài khoản vi phạm)", 1750, 1000, 240, 65)
    d2_5.add_store("ds7_n", "<b>D7.2: Thông Báo & Nhật Ký</b><br>(ThongBao, NhatKyHeThong)", 1750, 420, 240, 65)

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
    # GHI RA CẢ HAI FILE
    # =========================================================================
    xml_str = ET.tostring(mxfile, encoding="utf-8")
    dom = minidom.parseString(xml_str)
    pretty_xml = dom.toprettyxml(indent="  ")
    
    master_path = r"c:\Users\Admin\OneDrive\Documents\Secondhand-Marketplace\Secondhand_Marketplace_Architecture_Diagrams.xml"
    with open(master_path, "w", encoding="utf-8") as f:
        f.write(pretty_xml)
    print(f"Master Architecture Diagrams XML saved at: {master_path}")

    dfd_path = r"c:\Users\Admin\OneDrive\Documents\Secondhand-Marketplace\DFD_Secondhand_Marketplace.xml"
    with open(dfd_path, "w", encoding="utf-8") as f:
        f.write(pretty_xml)
    print(f"Updated DFD_Secondhand_Marketplace.xml with all 14 tabs!")

if __name__ == "__main__":
    generate_all()
