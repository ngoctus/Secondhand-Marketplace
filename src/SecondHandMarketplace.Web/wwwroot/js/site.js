(() => {
    const button = document.getElementById('accountButton');
    const panel = document.getElementById('accountPanel');

    if (!button || !panel) return;

    const closePanel = () => {
        panel.hidden = true;
        button.setAttribute('aria-expanded', 'false');
    };

    button.addEventListener('click', (event) => {
        event.stopPropagation();
        panel.hidden = !panel.hidden;
        button.setAttribute('aria-expanded', String(!panel.hidden));
    });

    document.addEventListener('click', (event) => {
        if (!panel.contains(event.target) && !button.contains(event.target)) {
            closePanel();
        }
    });

    document.addEventListener('keydown', (event) => {
        if (event.key === 'Escape') closePanel();
    });
})();

// Ảnh sản phẩm bị lỗi / chưa có file: thay bằng khung dự phòng thay vì hiện icon ảnh hỏng.
(() => {
    const thayBangKhungDuPhong = (img) => {
        const fallback = document.createElement('div');
        fallback.className = 'product-art-fallback';
        fallback.setAttribute('aria-label', 'Chưa có ảnh sản phẩm');
        fallback.textContent = '♻';
        img.replaceWith(fallback);
    };

    // Sự kiện error của <img> không nổi bọt lên, nên lắng nghe ở pha capture.
    document.addEventListener('error', (event) => {
        const img = event.target;
        if (img instanceof HTMLImageElement && img.hasAttribute('data-fallback')) {
            thayBangKhungDuPhong(img);
        }
    }, true);

    // Ảnh đã lỗi trước khi script chạy thì không còn bắn sự kiện, nên kiểm tra lại một lần.
    window.addEventListener('load', () => {
        document.querySelectorAll('img[data-fallback]').forEach((img) => {
            if (img.complete && img.naturalWidth === 0) thayBangKhungDuPhong(img);
        });
    });
})();