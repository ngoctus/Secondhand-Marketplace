// Hàm gọi API dùng chung cho mọi endpoint dưới /api.
// Kiểu trả về khớp Common/KetQua.cs: { success, message, data }.

import { hienThongBao } from "./thongbao.js";

export interface KetQua<T = unknown> {
    success: boolean;
    message: string;
    data: T | null;
}

export interface TuyChonApi {
    method?: "GET" | "POST" | "PUT" | "PATCH" | "DELETE";
    /** Object/mảng được gửi dạng JSON; FormData được gửi nguyên (dùng cho upload ảnh). */
    body?: unknown;
    /** Mặc định true: thất bại thì tự hiện thông báo lỗi. Đặt false nếu trang tự xử lý message. */
    hienLoi?: boolean;
    signal?: AbortSignal;
}

const DUONG_DAN_DANG_NHAP = "/Account/Login";

const THONG_DIEP = {
    matKetNoi: "Không kết nối được máy chủ. Vui lòng kiểm tra mạng và thử lại.",
    hetPhien: "Phiên đăng nhập đã hết hạn. Đang chuyển đến trang đăng nhập...",
    khongCoQuyen: "Bạn không có quyền thực hiện thao tác này.",
    loiMayChu: "Hệ thống gặp sự cố. Vui lòng thử lại sau.",
    khongHopLe: "Dữ liệu gửi lên không hợp lệ.",
    khongTimThay: "Không tìm thấy dữ liệu yêu cầu.",
} as const;

/**
 * Gọi endpoint /api và luôn trả về KetQua (không ném lỗi vì HTTP lỗi hay mất mạng).
 * - 401: chuyển sang trang đăng nhập, kèm ReturnUrl là trang hiện tại.
 * - 403: success = false, message "không có quyền".
 * - Endpoint không trả đúng dạng KetQua (ví dụ { reply } của /api/ai/chat): HTTP thành công thì
 *   success = true và toàn bộ nội dung nằm trong data.
 */
export async function goiApi<T = unknown>(url: string, tuyChon: TuyChonApi = {}): Promise<KetQua<T>> {
    const { method = "GET", body, hienLoi = true, signal } = tuyChon;

    const headers: Record<string, string> = { Accept: "application/json" };
    let noiDung: BodyInit | undefined;
    if (body instanceof FormData) {
        noiDung = body; // trình duyệt tự đặt Content-Type kèm boundary
    } else if (body !== undefined) {
        headers["Content-Type"] = "application/json";
        noiDung = JSON.stringify(body);
    }

    let phanHoi: Response;
    try {
        phanHoi = await fetch(url, { method, headers, body: noiDung, credentials: "same-origin", signal });
    } catch (loi) {
        if (loi instanceof DOMException && loi.name === "AbortError") throw loi; // người gọi tự hủy, không phải lỗi
        return thatBai<T>(THONG_DIEP.matKetNoi, hienLoi);
    }

    if (phanHoi.status === 401) {
        chuyenDenDangNhap();
        return thatBai<T>(THONG_DIEP.hetPhien, false);
    }
    if (phanHoi.status === 403) {
        return thatBai<T>(THONG_DIEP.khongCoQuyen, hienLoi);
    }

    const duLieu = await docJson(phanHoi);

    if (laKetQua(duLieu)) {
        const ketQua: KetQua<T> = {
            success: duLieu.success,
            message: duLieu.message ?? "",
            data: (duLieu.data ?? null) as T | null,
        };
        if (!ketQua.success && hienLoi) hienThongBao("danger", ketQua.message || THONG_DIEP.loiMayChu);
        return ketQua;
    }

    if (phanHoi.ok) {
        return { success: true, message: "", data: (duLieu ?? null) as T | null };
    }

    return thatBai<T>(layThongDiepLoi(duLieu, phanHoi.status), hienLoi);
}

/** Cách viết ngắn: api.get<T>(url), api.post<T>(url, body), api.put, api.delete. */
export const api = {
    get: <T = unknown>(url: string, tuyChon?: Omit<TuyChonApi, "method" | "body">) =>
        goiApi<T>(url, { ...tuyChon, method: "GET" }),
    post: <T = unknown>(url: string, body?: unknown, tuyChon?: Omit<TuyChonApi, "method" | "body">) =>
        goiApi<T>(url, { ...tuyChon, method: "POST", body }),
    put: <T = unknown>(url: string, body?: unknown, tuyChon?: Omit<TuyChonApi, "method" | "body">) =>
        goiApi<T>(url, { ...tuyChon, method: "PUT", body }),
    delete: <T = unknown>(url: string, tuyChon?: Omit<TuyChonApi, "method" | "body">) =>
        goiApi<T>(url, { ...tuyChon, method: "DELETE" }),
};

function thatBai<T>(message: string, hienLoi: boolean): KetQua<T> {
    if (hienLoi) hienThongBao("danger", message);
    return { success: false, message, data: null };
}

function chuyenDenDangNhap(): void {
    const trangHienTai = window.location.pathname + window.location.search;
    window.location.assign(`${DUONG_DAN_DANG_NHAP}?ReturnUrl=${encodeURIComponent(trangHienTai)}`);
}

// Trang lỗi HTML (UseStatusCodePagesWithReExecute) hay body rỗng đều về null thay vì ném lỗi.
async function docJson(phanHoi: Response): Promise<unknown> {
    const loaiNoiDung = phanHoi.headers.get("content-type") ?? "";
    if (!loaiNoiDung.includes("json")) return null; // gồm cả application/problem+json
    try {
        return await phanHoi.json();
    } catch {
        return null;
    }
}

function laKetQua(x: unknown): x is { success: boolean; message?: string; data?: unknown } {
    return typeof x === "object" && x !== null && typeof (x as { success?: unknown }).success === "boolean";
}

// [ApiController] tự trả ProblemDetails khi model sai: { title, errors: { Truong: ["thông báo"] } }.
function layThongDiepLoi(duLieu: unknown, maHttp: number): string {
    if (typeof duLieu === "object" && duLieu !== null) {
        const { errors, title } = duLieu as { errors?: Record<string, unknown>; title?: unknown };
        if (errors && typeof errors === "object") {
            for (const dsLoi of Object.values(errors)) {
                if (Array.isArray(dsLoi) && typeof dsLoi[0] === "string") return dsLoi[0];
            }
        }
        if (typeof title === "string" && title) return title;
    }
    if (maHttp === 404) return THONG_DIEP.khongTimThay;
    if (maHttp >= 400 && maHttp < 500) return THONG_DIEP.khongHopLe;
    return THONG_DIEP.loiMayChu;
}
