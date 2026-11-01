// Hộp thông báo dùng chung. Vùng chứa #vung-thong-bao nằm trong Views/Shared/_ThongBao.cshtml
// (đã được _Layout nhúng), nên trang nào cũng gọi được.

export type LoaiThongBao = "success" | "danger" | "warning" | "info";

// Thông báo thành công/thông tin tự tắt sau ít giây; lỗi và cảnh báo giữ nguyên để người dùng kịp đọc.
const THOI_GIAN_TU_TAT_MS = 5000;
const ID_VUNG_THONG_BAO = "vung-thong-bao";

export function hienThongBao(loai: LoaiThongBao, noiDung: string): void {
    const vung = document.getElementById(ID_VUNG_THONG_BAO);
    if (!vung) {
        window.alert(noiDung);
        return;
    }

    const hop = document.createElement("div");
    hop.className = `alert alert-${loai} alert-dismissible fade show`;
    hop.setAttribute("role", "alert");
    hop.append(noiDung); // append(string) chèn dạng text, không diễn giải HTML

    const nutDong = document.createElement("button");
    nutDong.type = "button";
    nutDong.className = "btn-close";
    nutDong.setAttribute("data-bs-dismiss", "alert");
    nutDong.setAttribute("aria-label", "Đóng");
    hop.append(nutDong);

    vung.append(hop);
    tuTat(hop, loai);
}

export const thongBao = {
    thanhCong: (noiDung: string) => hienThongBao("success", noiDung),
    loi: (noiDung: string) => hienThongBao("danger", noiDung),
    canhBao: (noiDung: string) => hienThongBao("warning", noiDung),
    thongTin: (noiDung: string) => hienThongBao("info", noiDung),
};

// Các thông báo thành công do server render (TempData) cũng tự tắt giống thông báo tạo bằng JS.
export function tuTatThongBaoCoSan(): void {
    const vung = document.getElementById(ID_VUNG_THONG_BAO);
    vung?.querySelectorAll<HTMLElement>(".alert-success, .alert-info").forEach((hop) => tuTat(hop, "success"));
}

function tuTat(hop: HTMLElement, loai: LoaiThongBao): void {
    if (loai !== "success" && loai !== "info") return;
    window.setTimeout(() => hop.remove(), THOI_GIAN_TU_TAT_MS);
}
