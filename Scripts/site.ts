// Điểm vào JS chung của toàn site, được _Layout nạp dạng module: <script type="module" src="~/dist/site.js">.
// Trang riêng cần gọi API thì tự import trong @section Scripts:
//   <script type="module">
//       import { api } from "/dist/api.js";
//       const kq = await api.post("/api/...", { ... });
//   </script>

import { api, goiApi } from "./api.js";
import { hienThongBao, thongBao, tuTatThongBaoCoSan } from "./thongbao.js";

declare global {
    interface Window {
        /** Bản gắn sẵn cho script không dùng import. Lưu ý: script module chạy sau khi trang parse xong. */
        SecondHand: {
            goiApi: typeof goiApi;
            api: typeof api;
            hienThongBao: typeof hienThongBao;
            thongBao: typeof thongBao;
        };
    }
}

window.SecondHand = { goiApi, api, hienThongBao, thongBao };

tuTatThongBaoCoSan();
