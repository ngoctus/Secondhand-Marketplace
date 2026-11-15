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
            btn.setAttribute('aria-pressed', String(isFavorite));
        });
    };

    const thongBao = (message, type = 'info', title = null) => {
        if (window.showNotifyBox) {
            window.showNotifyBox({
                title: title || (type === 'success' ? 'Đã lưu yêu thích' : type === 'error' ? 'Lỗi' : 'Thông báo'),
                message: message,
                type: type
            });
        } else if (window.alertBox) {
            window.alertBox(message, type);
        }
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
        btn.disabled = true;
        try {
            const res = await fetch(toggleUrl, {
                method: 'POST',
                headers: { 'Content-Type': 'application/x-www-form-urlencoded', 'X-Requested-With': 'XMLHttpRequest' },
                body
            });
            if (res.redirected) {
                window.location.href = res.url;
                return;
            }
            if (!res.ok) {
                thongBao('Chưa cập nhật được danh sách yêu thích. Vui lòng thử lại.', 'warning', 'Cần lưu ý');
                return;
            }
            const data = await res.json();
            if (!data.success) {
                thongBao(data.message || 'Không thể lưu sản phẩm yêu thích này.', 'error', 'Không thành công');
                return;
            }
            danhDau(productId, data.isFavorite);
            thongBao(
                data.isFavorite ? 'Sản phẩm đã được thêm vào danh sách yêu thích của bạn!' : 'Đã bỏ sản phẩm khỏi danh sách yêu thích.',
                data.isFavorite ? 'success' : 'info',
                data.isFavorite ? 'Yêu thích' : 'Đã bỏ yêu thích'
            );
            if (!data.isFavorite && window.location.pathname.toLowerCase().startsWith('/user/favorites')) {
                const item = btn.closest('.col') ?? btn.closest('.rm-product-card');
                item?.remove();
                if (!document.querySelector('.rm-product-card')) window.location.reload();
            }
        } catch {
            thongBao('Mạng đang gián đoạn. Sản phẩm chưa được cập nhật.', 'error', 'Lỗi kết nối');
        } finally {
            btn.disabled = false;
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

// =========================================================================
// Bộ đếm mô tả trong biểu mẫu đăng bán: luật duyệt yêu cầu >= 20 ký tự,
// báo ngay khi gõ để người bán không phải nhận lỗi sau khi bấm gửi duyệt.
// =========================================================================
(() => {
    const o = document.querySelector('textarea[data-mota-toithieu]');
    if (!o) return;

    const so = document.getElementById(o.dataset.motaDem);
    const trangThai = document.getElementById(o.dataset.motaTrangThai);
    const toiThieu = Number(o.dataset.motaToithieu);
    const toiDa = Number(o.getAttribute('maxlength') || 0);

    const capNhat = () => {
        const dai = o.value.trim().length;
        if (so) so.textContent = `${dai}/${toiDa} ký tự`;
        if (!trangThai) return;
        if (dai === 0) {
            trangThai.textContent = 'Chưa có mô tả';
            trangThai.className = 'text-muted';
        } else if (dai < toiThieu) {
            trangThai.textContent = `Còn thiếu ${toiThieu - dai} ký tự để gửi duyệt`;
            trangThai.className = 'text-danger';
        } else {
            trangThai.textContent = 'Đủ độ dài để gửi duyệt';
            trangThai.className = 'text-success';
        }
    };

    o.addEventListener('input', capNhat);
    capNhat();
})();

// =========================================================================
// B7: nút đóng/mở bảng lọc trên trang tìm kiếm (chỉ hiện trên điện thoại)
// =========================================================================
(() => {
    const panel = document.getElementById('filterPanel');
    const btn = document.querySelector('.filter-toggle');
    if (!panel || !btn) return;

    const soLoc = Number(btn.dataset.soBoLoc || 0);
    const tenLoc = soLoc > 0 ? `bộ lọc (${soLoc})` : 'bộ lọc';
    const capNhat = () => {
        const mo = panel.classList.contains('show');
        btn.textContent = `${mo ? 'Ẩn' : 'Hiện'} ${tenLoc}`;
        btn.setAttribute('aria-expanded', String(mo));
    };

    panel.addEventListener('shown.bs.collapse', capNhat);
    panel.addEventListener('hidden.bs.collapse', capNhat);
    capNhat();
})();

// Chuyển động xuất hiện nhẹ khi cuộn; giữ nguyên nội dung nếu người dùng giảm chuyển động.
(() => {
    if (window.matchMedia?.('(prefers-reduced-motion: reduce)').matches) return;

    const targets = document.querySelectorAll(
        '.landing-section, .category-card, .rm-product-card, .search-page-layout, .help-section, .detail-info-panel, .rm-auth-card, .panel, .rm-panel, .kpi'
    );
    if (!targets.length) return;

    targets.forEach(target => target.classList.add('rm-reveal'));
    document.documentElement.classList.add('rm-motion-ready');

    if (!('IntersectionObserver' in window)) {
        targets.forEach(target => target.classList.add('is-visible'));
        return;
    }

    const observer = new IntersectionObserver(entries => {
        entries.forEach(entry => {
            if (!entry.isIntersecting) return;
            entry.target.classList.add('is-visible');
            observer.unobserve(entry.target);
            const clearReveal = event => {
                if (event.target !== entry.target) return;
                entry.target.classList.remove('rm-reveal', 'is-visible');
                entry.target.removeEventListener('animationend', clearReveal);
            };
            entry.target.addEventListener('animationend', clearReveal);
        });
    }, { threshold: 0.08, rootMargin: '0px 0px -24px 0px' });

    targets.forEach(target => observer.observe(target));
})();

// Quản lý Modal Box "Thêm vào giỏ hàng" & Thông báo Box
(() => {
    let autoCloseTimer = null;

    const closeModal = () => {
        const modal = document.getElementById('rmCartModal');
        if (!modal) return;
        modal.classList.remove('is-active');
        document.body.classList.remove('rm-modal-open');
        if (autoCloseTimer) {
            clearTimeout(autoCloseTimer);
            autoCloseTimer = null;
        }
    };

    // Đăng ký sự kiện đóng modal
    document.addEventListener('click', (e) => {
        if (e.target.closest('#rmCartModalCloseBtn') || e.target.closest('#rmCartModalCloseX')) {
            closeModal();
            return;
        }
        const modal = document.getElementById('rmCartModal');
        if (modal && e.target === modal) {
            closeModal();
        }
    });

    document.addEventListener('keydown', (e) => {
        if (e.key === 'Escape') {
            const modal = document.getElementById('rmCartModal');
            if (modal && modal.classList.contains('is-active')) {
                closeModal();
            }
        }
    });

    const updateCartCount = async () => {
        const cartBadge = document.getElementById('rmCartBadge');
        try {
            const res = await fetch('/User/Cart/DemSoLuong');
            if (res.ok) {
                const data = await res.json();
                if (cartBadge) {
                    if (data.count > 0) {
                        cartBadge.textContent = data.count > 99 ? '99+' : data.count;
                        cartBadge.style.display = 'inline-block';
                    } else {
                        cartBadge.style.display = 'none';
                    }
                }
            }
        } catch { }
    };

    // Tự động load số lượng giỏ hàng khi có đăng nhập
    if (document.querySelector('meta[name="rm-csrf"]')) {
        updateCartCount();
    }

    // Bắt sự kiện submit form Thêm vào giỏ
    document.addEventListener('submit', async (e) => {
        const form = e.target.closest('form[action*="/Cart/Them"], form.rm-add-to-cart-form');
        if (!form) return;

        const csrf = form.querySelector('input[name="__RequestVerificationToken"]')?.value || document.querySelector('meta[name="rm-csrf"]')?.content;
        if (!csrf) return; // Khách chưa đăng nhập: để form submit chuẩn để chuyển hướng sang trang Login

        e.preventDefault();

        const submitBtn = form.querySelector('button[type="submit"]');
        const origBtnHtml = submitBtn ? submitBtn.innerHTML : '';
        if (submitBtn) {
            submitBtn.disabled = true;
            submitBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-1" role="status" style="width:14px;height:14px;"></span> Đang thêm...';
        }

        try {
            const formData = new FormData(form);
            const res = await fetch(form.action, {
                method: 'POST',
                headers: {
                    'X-Requested-With': 'XMLHttpRequest'
                },
                body: new URLSearchParams(formData)
            });

            if (res.redirected) {
                window.location.href = res.url;
                return;
            }

            const data = await res.json();

            if (data.success) {
                const prodName = form.dataset.productName || document.querySelector('.detail-title, .product-title')?.textContent?.trim() || 'Sản phẩm';
                const prodPrice = form.dataset.productPrice || document.querySelector('.main-price, .detail-pricing-row .main-price')?.textContent?.trim() || '';
                const prodImg = form.dataset.productImage || document.querySelector('.detail-main-img img, .rm-card-image')?.src || '';

                const modal = document.getElementById('rmCartModal');
                const modalName = document.getElementById('rmCartModalName');
                const modalPrice = document.getElementById('rmCartModalPrice');
                const modalImg = document.getElementById('rmCartModalImg');

                if (modal) {
                    if (modalName) modalName.textContent = prodName;
                    if (modalPrice) modalPrice.textContent = prodPrice;
                    if (modalImg) {
                        if (prodImg) {
                            modalImg.src = prodImg;
                            modalImg.style.display = 'block';
                        } else {
                            modalImg.style.display = 'none';
                        }
                    }
                    modal.classList.add('is-active');
                    document.body.classList.add('rm-modal-open');
                    if (autoCloseTimer) clearTimeout(autoCloseTimer);
                    autoCloseTimer = setTimeout(closeModal, 6000);
                } else {
                    window.showNotifyBox({
                        title: 'Đã thêm vào giỏ hàng!',
                        message: `Đã thêm "${prodName}" vào giỏ hàng thành công.`,
                        type: 'success',
                        buttonText: 'Đồng ý'
                    });
                }

                updateCartCount();
            } else {
                window.showNotifyBox({
                    title: 'Thông báo giỏ hàng',
                    message: data.message || 'Không thể thêm sản phẩm vào giỏ hàng.',
                    type: 'warning',
                    buttonText: 'Đã hiểu'
                });
            }
        } catch {
            window.showNotifyBox({
                title: 'Lỗi kết nối',
                message: 'Không thể kết nối máy chủ. Vui lòng thử lại sau.',
                type: 'error',
                buttonText: 'Đóng'
            });
        } finally {
            if (submitBtn) {
                submitBtn.disabled = false;
                submitBtn.innerHTML = origBtnHtml;
            }
        }
    });

    // =========================================================================
    // HỘP THÔNG BÁO TOÀN DIỆN (Universal Notification Box Modal) chống chìm web
    // =========================================================================
    let notifyAutoTimer = null;

    window.closeNotifyBox = () => {
        const modal = document.getElementById('rmNotifyModal');
        if (!modal) return;
        if (notifyAutoTimer) {
            clearTimeout(notifyAutoTimer);
            notifyAutoTimer = null;
        }
        modal.classList.remove('is-active');
        document.body.classList.remove('rm-modal-open');
        setTimeout(() => {
            if (!modal.classList.contains('is-active')) {
                modal.style.display = 'none';
            }
        }, 260);
    };

    window.showNotifyBox = (opts) => {
        if (typeof opts === 'string') {
            opts = { message: opts, type: arguments[1] || 'success', title: arguments[2] || null };
        }
        let modal = document.getElementById('rmNotifyModal');
        if (!modal) {
            // Tự tạo modal nếu trang chưa có sẵn
            modal = document.createElement('div');
            modal.id = 'rmNotifyModal';
            modal.className = 'rm-notify-backdrop';
            modal.setAttribute('role', 'dialog');
            modal.setAttribute('aria-modal', 'true');
            modal.innerHTML = `
                <div class="rm-notify-box rm-notify-box-success">
                    <button type="button" class="rm-notify-close-x" id="rmNotifyCloseX" aria-label="Đóng">&times;</button>
                    <div class="rm-notify-icon-wrap">
                        <div class="rm-notify-icon rm-notify-icon-success" id="rmNotifyIcon">
                            <i class="bi bi-check-circle-fill"></i>
                        </div>
                    </div>
                    <div class="rm-notify-content">
                        <h4 class="rm-notify-title" id="rmNotifyTitle">Thành công!</h4>
                        <div class="rm-notify-message" id="rmNotifyDesc"></div>
                    </div>
                    <div class="rm-notify-actions">
                        <button type="button" class="rm-notify-btn rm-notify-btn-success" id="rmNotifyBtn">Đồng ý</button>
                    </div>
                </div>
            `;
            document.body.appendChild(modal);
            initNotifyModalEvents(modal);
        }

        const type = opts.type || 'success';
        const title = opts.title || (type === 'success' ? 'Thành công!' : type === 'error' ? 'Đã xảy ra lỗi' : type === 'warning' ? 'Cần lưu ý' : 'Thông báo');
        const message = opts.message || '';
        const btnText = opts.buttonText || 'Đồng ý';

        const boxEl = modal.querySelector('.rm-notify-box');
        if (boxEl) {
            boxEl.className = `rm-notify-box rm-notify-box-${type}`;
        }

        const iconEl = document.getElementById('rmNotifyIcon');
        if (iconEl) {
            iconEl.className = `rm-notify-icon rm-notify-icon-${type}`;
            let iconHtml = '<i class="bi bi-check-circle-fill"></i>';
            if (type === 'error') iconHtml = '<i class="bi bi-x-circle-fill"></i>';
            else if (type === 'warning') iconHtml = '<i class="bi bi-exclamation-triangle-fill"></i>';
            else if (type === 'info') iconHtml = '<i class="bi bi-info-circle-fill"></i>';
            iconEl.innerHTML = iconHtml;
        }

        const titleEl = document.getElementById('rmNotifyTitle');
        if (titleEl) titleEl.textContent = title;

        const descEl = document.getElementById('rmNotifyDesc');
        if (descEl) descEl.textContent = message;

        const btnEl = document.getElementById('rmNotifyBtn');
        if (btnEl) {
            btnEl.textContent = btnText;
            btnEl.className = `rm-notify-btn rm-notify-btn-${type}`;
        }

        modal.style.display = 'flex';
        void modal.offsetWidth; // force reflow
        modal.classList.add('is-active');
        document.body.classList.add('rm-modal-open');

        if (notifyAutoTimer) clearTimeout(notifyAutoTimer);
        // Tự động đóng sau 4.5s với loại thông báo success/info
        if (opts.autoCloseMs || (type === 'success' || type === 'info')) {
            const ms = opts.autoCloseMs || 4500;
            notifyAutoTimer = setTimeout(window.closeNotifyBox, ms);
        }
    };

    const initNotifyModalEvents = (modal) => {
        if (!modal || modal.dataset.initEvents) return;
        modal.dataset.initEvents = 'true';

        const closeBtn = modal.querySelector('#rmNotifyBtn');
        const closeX = modal.querySelector('#rmNotifyCloseX');

        if (closeBtn) closeBtn.addEventListener('click', window.closeNotifyBox);
        if (closeX) closeX.addEventListener('click', window.closeNotifyBox);

        modal.addEventListener('click', (e) => {
            if (e.target === modal) {
                window.closeNotifyBox();
            }
        });

        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape' && modal.classList.contains('is-active')) {
                window.closeNotifyBox();
            }
        });
    };

    // Khởi tạo sự kiện cho modal đã render từ Razor server TempData
    const serverNotifyModal = document.getElementById('rmNotifyModal');
    if (serverNotifyModal) {
        initNotifyModalEvents(serverNotifyModal);
        if (serverNotifyModal.classList.contains('is-active')) {
            document.body.classList.add('rm-modal-open');
            const isErrorOrWarn = serverNotifyModal.querySelector('.rm-notify-box-error, .rm-notify-box-warning');
            if (!isErrorOrWarn) {
                notifyAutoTimer = setTimeout(window.closeNotifyBox, 4500);
            }
        }
    }

    // Tương thích ngược: alertBox chuyển thành hiển thị Hộp thông báo Box nổi bật
    window.alertBox = (message, type = 'success', title = null) => {
        window.showNotifyBox({ message, type, title });
    };
})();

