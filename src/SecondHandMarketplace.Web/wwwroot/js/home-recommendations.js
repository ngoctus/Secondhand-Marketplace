// Gợi ý sản phẩm ngay trên trang chủ ("Có thể bạn sẽ thích"):
// Đổi chế độ (Mặc định / Mới đăng / Giá tốt / Giá tiết kiệm) hoặc danh mục sẽ cập nhật lưới 4 sản phẩm
// trực tiếp trên trang chủ; nút "Xem chi tiết" cập nhật URL tương ứng để chuyển sang trang /Products.
(() => {
    'use strict';

    const section = document.getElementById('rmHomeRecSection');
    const stage = document.getElementById('rmHomeRecStage');
    const seeMoreLink = document.getElementById('rmHomeSeeMore');
    if (!section || !stage || !seeMoreLink) return;

    const endpoint = section.dataset.endpoint || '/Home/SanPhamGoiY';
    const productsUrl = section.dataset.productsUrl || '/Products';
    const baseLabel = section.dataset.labelBase || 'Xem chi tiết';
    const moreTextEl = seeMoreLink.querySelector('[data-rm-more-text]');

    const modeButtons = Array.from(section.querySelectorAll('[data-rm-mode]'));
    const catButtons = Array.from(section.querySelectorAll('[data-rm-cat]'));

    let currentMode = 'default';
    let currentModeLabel = '';
    let currentCat = '';
    let currentCatLabel = '';
    let requestSeq = 0;

    // Cache HTML theo cặp "mode|cat" để chuyển qua lại giữa các bộ lọc tức thì, không nhấp nháy.
    const htmlCache = new Map();
    htmlCache.set('default|', stage.innerHTML);

    const buildProductsUrl = (mode, cat) => {
        const params = new URLSearchParams();
        if (cat) params.set('CategoryId', cat);
        if (mode === 'newest') {
            params.set('Sort', 'newest');
        } else if (mode === 'best_price') {
            params.set('PriceRange', '1');
        } else if (mode === 'bargain') {
            params.set('Sort', 'price_asc');
        }
        const qs = params.toString();
        return productsUrl + (qs ? '?' + qs : '');
    };

    const updateSeeMore = () => {
        seeMoreLink.setAttribute('href', buildProductsUrl(currentMode, currentCat));

        if (!moreTextEl) return;
        if (currentCatLabel && currentModeLabel) {
            moreTextEl.textContent = `${baseLabel} · ${currentCatLabel} (${currentModeLabel})`;
        } else if (currentCatLabel) {
            moreTextEl.textContent = `${baseLabel} · ${currentCatLabel}`;
        } else if (currentModeLabel) {
            moreTextEl.textContent = `${baseLabel} · ${currentModeLabel}`;
        } else {
            moreTextEl.textContent = baseLabel;
        }
    };

    const syncButtons = () => {
        modeButtons.forEach((btn) => {
            const active = btn.dataset.rmMode === currentMode;
            btn.classList.toggle('is-active', active);
            btn.setAttribute('aria-selected', String(active));
        });
        catButtons.forEach((btn) => {
            const active = (btn.dataset.rmCat || '') === currentCat;
            btn.classList.toggle('active', active);
            btn.setAttribute('aria-selected', String(active));
        });
    };

    const applyHtml = (html) => {
        stage.innerHTML = html;

        // Kích hoạt lại reveal & skeleton ảnh cho thẻ mới nạp.
        if (typeof window.rmHienThien === 'function') {
            window.rmHienThien(stage);
        }
        // Bảo đảm thẻ mới hiển thị ngay cả khi IntersectionObserver chưa bắn kịp.
        stage.querySelectorAll('[data-rm-reveal]').forEach((el) => el.classList.add('rm-reveal-in'));

        // Đồng bộ lại trạng thái tim yêu thích (site.js nghe sự kiện này).
        document.dispatchEvent(new CustomEvent('rm:ket-qua-da-thay'));
    };

    const loadRecommendations = async () => {
        syncButtons();
        updateSeeMore();

        const cacheKey = `${currentMode}|${currentCat}`;
        if (htmlCache.has(cacheKey)) {
            requestSeq++;
            stage.classList.remove('is-loading');
            applyHtml(htmlCache.get(cacheKey));
            return;
        }

        const seq = ++requestSeq;
        stage.classList.add('is-loading');

        try {
            const params = new URLSearchParams();
            if (currentMode && currentMode !== 'default') params.set('mode', currentMode);
            if (currentCat) params.set('categoryId', currentCat);
            const url = endpoint + (params.toString() ? '?' + params.toString() : '');

            const res = await fetch(url, {
                headers: { 'X-Requested-With': 'XMLHttpRequest' },
                credentials: 'same-origin'
            });
            if (!res.ok) throw new Error('HTTP ' + res.status);

            const html = await res.text();
            htmlCache.set(cacheKey, html);
            if (seq !== requestSeq) return;

            applyHtml(html);
        } catch {
            // Lỗi mạng: nếu người dùng bấm Xem chi tiết vẫn dẫn đúng sang /Products theo bộ lọc đã chọn.
        } finally {
            if (seq === requestSeq) stage.classList.remove('is-loading');
        }
    };

    modeButtons.forEach((btn) => {
        btn.addEventListener('click', () => {
            const nextMode = btn.dataset.rmMode || 'default';
            if (nextMode === currentMode) return;
            currentMode = nextMode;
            currentModeLabel = btn.dataset.rmModeLabel || '';
            loadRecommendations();
        });
    });

    catButtons.forEach((btn) => {
        btn.addEventListener('click', () => {
            const nextCat = btn.dataset.rmCat || '';
            if (nextCat === currentCat) return;
            currentCat = nextCat;
            currentCatLabel = btn.dataset.rmCatLabel || '';
            loadRecommendations();
        });
    });

    // Nút "Về mặc định" trong trạng thái rỗng khi danh mục + chế độ không có tin khớp.
    stage.addEventListener('click', (e) => {
        const resetBtn = e.target.closest('[data-rm-rec-reset]');
        if (!resetBtn) return;
        currentMode = 'default';
        currentModeLabel = '';
        currentCat = '';
        currentCatLabel = '';
        loadRecommendations();
    });

    updateSeeMore();
})();
