// B-04: lọc tìm kiếm không reload trang. form #filterPanel và lưới #ketQuaTimKiem vẫn là
// HTML/GET chuẩn (link chia sẻ, tắt JS, Enter ở ô từ khóa đều chạy); JS chỉ chặn submit/change
// rồi xin lại partial "_SearchResults" và thay nội dung khối kết quả.
(() => {
    'use strict';

    const form = document.getElementById('filterPanel');
    const khoiKetQua = document.getElementById('ketQuaTimKiem');
    if (!form || !khoiKetQua) return;

    const mucTieu = khoiKetQua.dataset.mucTieu || window.location.pathname;
    const rangeLo = form.querySelector('[data-gia-lo]');
    const rangeCao = form.querySelector('[data-gia-cao]');
    const readout = form.querySelector('[data-gia-readout]');
    const band = form.querySelector('[data-gia-band]');

    // PriceRange là 6 nút radio cũ; grid render lại nên select sắp xếp phải tra cứu mỗi lần dùng.
    const LOAI_PARAM = ['PriceRange'];
    const nutSapXep = () => document.getElementById('sortOrder');

    let buoc = 1, bienMin = 0, bienMax = 0;
    let lanGoi = 0, henDu = null;

    const dinhDangGia = (n) => n.toLocaleString('vi-VN') + 'đ';

    // Form có sẵn input hidden "Sort" (giữ sắp xếp khi tắt JS). Map phân biệt hoa thường nên phải
    // xóa khóa cũ cùng tên trước khi ghi, nếu không URL mang cả Sort lẫn sort và binder gộp
    // hai giá trị thành một chuỗi không hợp lệ.
    const datParam = (map, key, value) => {
        Array.from(map.keys())
            .filter(k => k.toLowerCase() === key.toLowerCase())
            .forEach(k => map.delete(k));
        map.set(key, value);
    };

    const layBoLoc = (page) => {
        const giaTri = new Map();
        new FormData(form).forEach((v, k) => {
            if (LOAI_PARAM.indexOf(k) >= 0) return;
            const s = v === null || v === undefined ? '' : String(v);
            if (s !== '') giaTri.set(k, s);
        });

        const sort = nutSapXep();
        if (sort && sort.value && sort.value !== 'newest') datParam(giaTri, 'sort', sort.value);
        if (page && page !== '1') datParam(giaTri, 'page', page);

        const p = new URLSearchParams();
        giaTri.forEach((v, k) => p.append(k, v));
        const qs = p.toString();
        return mucTieu + (qs ? '?' + qs : '');
    };

    function veSlider() {
        if (!rangeLo || !rangeCao) return;
        bienMin = parseFloat(rangeLo.min);
        bienMax = parseFloat(rangeLo.max);
        buoc = parseFloat(rangeLo.step) || 1;

        let lo = parseFloat(rangeLo.value);
        let cao = parseFloat(rangeCao.value);
        if (isNaN(lo)) lo = bienMin;
        if (isNaN(cao)) cao = bienMax;

        // Hai tay kéo không được chéo nhau: đầu đang focus giữ nguyên, đầu kia bị kẹp theo.
        if (lo > cao) {
            if (document.activeElement === rangeLo) { cao = lo; rangeCao.value = String(cao); }
            else { lo = cao; rangeLo.value = String(lo); }
        }

        const le = (g) => ((g - bienMin) / ((bienMax - bienMin) || 1)) * 100;
        if (band) {
            band.style.left = le(lo) + '%';
            band.style.width = (le(cao) - le(lo)) + '%';
        }
        if (readout) {
            readout.textContent = (lo <= bienMin && cao >= bienMax)
                ? 'Tất cả mức giá'
                : dinhDangGia(lo) + ' – ' + dinhDangGia(cao);
        }
    }

    // Chỉ gửi giá khi tay kéo lệch khỏi biên: nằm hết cỡ hai đầu nghĩa là không lọc giá,
    // nhờ đó URL chia sẻ được vẫn ngắn như lúc dùng 6 nút radio.
    function chuanBiGiaGui() {
        if (!rangeLo || !rangeCao) return;
        const lo = parseFloat(rangeLo.value), cao = parseFloat(rangeCao.value);
        rangeLo.value = (isNaN(lo) || lo <= bienMin) ? '' : String(Math.round(lo / buoc) * buoc);
        rangeCao.value = (isNaN(cao) || cao >= bienMax) ? '' : String(Math.round(cao / buoc) * buoc);
    }

    async function tai(url, ghiLichSu) {
        const current = ++lanGoi;
        khoiKetQua.classList.add('rm-kq-dang-tai');
        try {
            const phanHoi = await fetch(url, {
                headers: { 'X-Requested-With': 'XMLHttpRequest' },
                credentials: 'same-origin'
            });
            if (!phanHoi.ok) throw new Error('http ' + phanHoi.status);
            const vanBan = await phanHoi.text();
            if (current !== lanGoi) return;

            khoiKetQua.innerHTML = vanBan;
            if (ghiLichSu) window.history.pushState({ loc: url }, '', url);

            // animations.js gắn reveal khi trang đầu tiên vẽ xong; nội dung mới cần gắn tiếp.
            if (typeof window.rmHienThien === 'function') window.rmHienThien(khoiKetQua);

            // Đổi trang thì đưa người mua về đầu lưới như load trang đầy đủ; đổi bộ lọc thì giữ nguyên.
            if (new URL(url, window.location.href).searchParams.get('page'))
                khoiKetQua.scrollIntoView({ behavior: 'smooth', block: 'start' });
        } catch (loi) {
            // Mất mạng hoặc server lỗi: để link/form thật dẫn đường, trình duyệt tải cả trang.
            window.location.href = url;
        } finally {
            if (current === lanGoi) khoiKetQua.classList.remove('rm-kq-dang-tai');
        }
    }

    function apDung() {
        chuanBiGiaGui();
        tai(layBoLoc('1'), true);
    }

    function henApDung() {
        window.clearTimeout(henDu);
        henDu = window.setTimeout(apDung, 400);
    }

    // --- thanh lọc bên trái ---
    form.addEventListener('submit', (e) => { e.preventDefault(); apDung(); });

    form.addEventListener('input', (e) => {
        const el = e.target;
        if (el === rangeLo || el === rangeCao) { veSlider(); return; }
        if (el.type === 'search' || el.type === 'text') henApDung();
    });

    form.addEventListener('change', (e) => {
        const el = e.target;
        if (el === rangeLo || el === rangeCao) { veSlider(); apDung(); return; }
        if (el.type === 'radio' || el.tagName === 'SELECT') apDung();
    });

    // --- khối kết quả: sắp xếp, phân trang, link xoá bộ lọc, form sắp xếp ---
    document.addEventListener('change', (e) => {
        if (e.target && e.target.id === 'sortOrder') apDung();
    });

    document.addEventListener('submit', (e) => {
        if (e.target && e.target.closest('#ketQuaTimKiem')) { e.preventDefault(); apDung(); }
    });

    document.addEventListener('click', (e) => {
        const a = e.target.closest ? e.target.closest('a') : null;
        if (!a || !a.closest('#ketQuaTimKiem')) return;
        const href = a.getAttribute('href');
        if (!href || href.indexOf(mucTieu) !== 0) return;
        e.preventDefault();
        tai(href, true);
    });

    // Nút back/forward: URL đã đổi nên chỉ việc xin lại partial theo URL hiện hành.
    window.addEventListener('popstate', () => {
        tai(window.location.pathname + window.location.search, false);
    });

    veSlider();
})();
