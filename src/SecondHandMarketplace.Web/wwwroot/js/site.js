/**
 * JavaScript dùng chung cho Re:Market
 * Quản lý dropdowns, mobile menu, fallback ảnh, thông báo toast
 */
(() => {
    // Helper đóng toàn bộ dropdown đang mở
    const closeAllDropdowns = () => {
        document.querySelectorAll('.rm-dropdown-menu, .account-panel, #mobileDrawer').forEach(el => {
            el.hidden = true;
        });
        document.querySelectorAll('[aria-expanded="true"]').forEach(btn => {
            btn.setAttribute('aria-expanded', 'false');
        });
    };

    // Thiết lập một dropdown toggle
    const setupDropdown = (btnId, menuId) => {
        const btn = document.getElementById(btnId);
        const menu = document.getElementById(menuId);
        if (!btn || !menu) return;

        btn.addEventListener('click', (e) => {
            e.stopPropagation();
            const willOpen = menu.hidden;
            closeAllDropdowns();
            if (willOpen) {
                menu.hidden = false;
                btn.setAttribute('aria-expanded', 'true');
            }
        });
    };

    // Đăng ký các dropdown
    setupDropdown('catMenuBtn', 'catDropdownMenu');
    setupDropdown('adminMenuBtn', 'adminDropdownMenu');
    setupDropdown('userMenuBtn', 'userDropdownMenu');
    setupDropdown('accountButton', 'accountPanel');

    // Toggle menu mobile
    const mobileBtn = document.getElementById('mobileMenuBtn');
    const mobileDrawer = document.getElementById('mobileDrawer');
    if (mobileBtn && mobileDrawer) {
        mobileBtn.addEventListener('click', (e) => {
            e.stopPropagation();
            const willOpen = mobileDrawer.hidden;
            closeAllDropdowns();
            if (willOpen) {
                mobileDrawer.hidden = false;
                mobileBtn.setAttribute('aria-expanded', 'true');
            }
        });
    }

    // Đóng dropdown khi click ra ngoài
    document.addEventListener('click', (e) => {
        if (!e.target.closest('.rm-dropdown') &&
            !e.target.closest('.rm-user-menu') &&
            !e.target.closest('.account-menu') &&
            !e.target.closest('.rm-mobile-drawer') &&
            !e.target.closest('.rm-mobile-toggle')) {
            closeAllDropdowns();
        }
    });

    // Đóng dropdown khi nhấn Escape
    document.addEventListener('keydown', (e) => {
        if (e.key === 'Escape') {
            closeAllDropdowns();
        }
    });
})();

// Xử lý ảnh sản phẩm fallback (4:5) khi đường dẫn ảnh lỗi hoặc chưa có ảnh
(() => {
    const thayBangKhungDuPhong = (img) => {
        if (img.dataset.hasFallback) return;
        img.dataset.hasFallback = 'true';

        const fallback = document.createElement('div');
        fallback.className = 'product-art-fallback';
        fallback.setAttribute('aria-label', 'Chưa có ảnh sản phẩm');
        fallback.innerHTML = '<span class="fallback-icon">♻</span><span class="fallback-text">Ảnh sản phẩm 4:5</span>';
        img.replaceWith(fallback);
    };

    document.addEventListener('error', (event) => {
        const img = event.target;
        if (img instanceof HTMLImageElement && img.hasAttribute('data-fallback')) {
            thayBangKhungDuPhong(img);
        }
    }, true);

    window.addEventListener('load', () => {
        document.querySelectorAll('img[data-fallback]').forEach((img) => {
            if (img.complete && img.naturalWidth === 0) {
                thayBangKhungDuPhong(img);
            }
        });
    });
})();

// Tự động ẩn thông báo thành công sau 4 giây
(() => {
    window.addEventListener('DOMContentLoaded', () => {
        const successAlerts = document.querySelectorAll('.rm-alert-success');
        successAlerts.forEach(alert => {
            setTimeout(() => {
                alert.style.transition = 'opacity 0.4s ease, transform 0.4s ease';
                alert.style.opacity = '0';
                alert.style.transform = 'translateY(-8px)';
                setTimeout(() => alert.remove(), 400);
            }, 4000);
        });
    });
})();

// Nút tim yêu thích (B4): cần đăng nhập; meta[name=rm-csrf] chỉ được layout cấp khi đã đăng nhập.
(() => {
    const csrf = document.querySelector('meta[name="rm-csrf"]')?.content;
    const toggleUrl = '/User/Favorites/Toggle';
    const idsUrl = '/User/Favorites/DanhSachIds';

    const danhDau = (productId, isFavorite) => {
        document.querySelectorAll(`.rm-card-favorite-btn[data-product-id="${productId}"]`).forEach(btn => {
            btn.classList.toggle('is-favorite', isFavorite);
            btn.setAttribute('aria-label', isFavorite ? 'Bỏ yêu thích' : 'Thêm vào yêu thích');
        });
    };

    document.addEventListener('click', async (e) => {
        const btn = e.target.closest('.rm-card-favorite-btn');
        if (!btn) return;
        e.preventDefault();

        if (!csrf) {
            window.location.href = '/Account/Login?ReturnUrl=' + encodeURIComponent(window.location.pathname + window.location.search);
            return;
        }

        const productId = btn.dataset.productId;
        const body = new URLSearchParams({ maSanPham: productId, __RequestVerificationToken: csrf });
        try {
            const res = await fetch(toggleUrl, {
                method: 'POST',
                headers: { 'Content-Type': 'application/x-www-form-urlencoded', 'X-Requested-With': 'XMLHttpRequest' },
                body
            });
            const data = await res.json();
            if (data.success) danhDau(productId, data.isFavorite);
        } catch {
            // Mạng lỗi: giữ nguyên trạng thái, không đổi tim.
        }
    });

    if (csrf) {
        window.addEventListener('load', async () => {
            if (!document.querySelector('.rm-card-favorite-btn')) return;
            try {
                const res = await fetch(idsUrl, { headers: { 'X-Requested-With': 'XMLHttpRequest' } });
                const ids = await res.json();
                ids.forEach(id => danhDau(id, true));
            } catch { /* chưa đăng nhập hoặc lỗi mạng: bỏ qua */ }
        });
    }
})();


// =========================================================================
// Lightbox Modal phóng to ảnh không tràn viền & Chuyển ảnh Thumbnail
// =========================================================================
(() => {
    let modal = null;
    let imgEl = null;
    let capEl = null;

    const createModal = () => {
        if (modal) return;
        modal = document.createElement('div');
        modal.id = 'rmImageLightbox';
        modal.className = 'rm-lightbox-overlay';
        modal.hidden = true;
        modal.innerHTML = `
            <div class="rm-lightbox-container">
                <button type="button" class="rm-lightbox-close" aria-label="Đóng">&times;</button>
                <div class="rm-lightbox-body">
                    <img class="rm-lightbox-img" src="" alt="Ảnh phóng to" />
                    <div class="rm-lightbox-caption"></div>
                </div>
            </div>
        `;
        document.body.appendChild(modal);

        imgEl = modal.querySelector('.rm-lightbox-img');
        capEl = modal.querySelector('.rm-lightbox-caption');
        const closeBtn = modal.querySelector('.rm-lightbox-close');

        const closeLightbox = () => {
            modal.hidden = true;
            if (imgEl) imgEl.src = '';
            document.body.classList.remove('rm-modal-open');
        };

        closeBtn.addEventListener('click', closeLightbox);
        modal.addEventListener('click', (e) => {
            if (!e.target.closest('.rm-lightbox-img')) {
                closeLightbox();
            }
        });

        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape' && modal && !modal.hidden) {
                closeLightbox();
            }
        });
    };

    const openLightbox = (src, caption) => {
        if (!src) return;
        createModal();
        imgEl.src = src;
        capEl.textContent = caption || '';
        modal.hidden = false;
        document.body.classList.add('rm-modal-open');
    };

    // Bắt sự kiện click vào ảnh phóng to
    document.addEventListener('click', (e) => {
        const zoomImg = e.target.closest('.detail-main-img img, .auction-main-img, img[data-zoomable]');
        if (zoomImg) {
            e.preventDefault();
            const caption = zoomImg.alt || zoomImg.title || document.querySelector('.detail-title, .bid-title')?.textContent?.trim() || '';
            openLightbox(zoomImg.src, caption);
        }
    });

    // Bắt sự kiện click vào thumbnail để đổi ảnh chính
    document.addEventListener('click', (e) => {
        const thumb = e.target.closest('.detail-thumbnails .thumbnail-item, .auction-thumbs img');
        if (!thumb) return;

        const thumbImg = thumb.tagName === 'IMG' ? thumb : thumb.querySelector('img');
        if (!thumbImg) return;

        const mainImg = document.querySelector('#mainProductImg, .detail-main-img img, #mainImage, .auction-main-img');
        if (mainImg) {
            mainImg.src = thumbImg.src;
            document.querySelectorAll('.detail-thumbnails .thumbnail-item, .auction-thumbs img').forEach(t => t.classList.remove('active'));
            thumb.classList.add('active');
        }
    });
})();
