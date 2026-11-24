/**
 * JavaScript dùng chung cho Re:Market
 * Quản lý dropdowns, mobile menu, fallback ảnh, thông báo toast
 */

// B-05: JS sinh chuỗi hiển thị nên không đi qua Razor localizer. _Layout đổ từ điển ngôn ngữ hiện tại vào
// #rm-i18n; thiếu khối đó (ví dụ _LayoutAdmin) thì rmT rơi về fallback tiếng Việt để script không hỏng.
const RM_TEXT = (() => {
    try { return JSON.parse(document.getElementById('rm-i18n')?.textContent || '{}'); } catch { return {}; }
})();
window.rmT = (key, fallback, ...args) => {
    let s = RM_TEXT[key] ?? fallback ?? key;
    args.forEach((arg, i) => { s = s.split('{' + i + '}').join(String(arg)); });
    return s;
};

(() => {
    // Helper đóng toàn bộ dropdown đang mở
    const closeAllDropdowns = () => {
        document.querySelectorAll('.rm-dropdown-menu, .account-panel, #mobileDrawer').forEach(el => {
            el.hidden = true;
        });
        // Đóng notify dropdown riêng (không có class rm-dropdown-menu)
        const notifyDrop = document.getElementById('notifyDropdownMenu');
        if (notifyDrop) notifyDrop.hidden = true;
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
    setupDropdown('notifyBellBtn', 'notifyDropdownMenu');

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
            !e.target.closest('.rm-notify-wrapper') &&
            !e.target.closest('.rm-notify-slot') &&
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
        fallback.setAttribute('aria-label', window.rmT('Js_ImageMissing', 'Chưa có ảnh sản phẩm'));
        fallback.innerHTML = `<span class="fallback-icon">♻</span><span class="fallback-text">${window.rmT('Card_ImagePlaceholder', 'Ảnh sản phẩm 4:5')}</span>`;
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

// =========================================================================
// HỆ THỐNG THÔNG BÁO TOAST & MODAL DIALOG TRUNG TÂM (Task C-01)
// =========================================================================
(() => {
    const escapeHtml = (str) => {
        if (!str) return '';
        const div = document.createElement('div');
        div.textContent = str;
        return div.innerHTML;
    };

    /**
     * Hiển thị Toast thông báo trôi nổi góc trên bên phải màn hình
     * @param {string} message Nội dung thông báo
     * @param {'success'|'info'|'warning'|'error'} type Loại thông báo
     * @param {number} duration Thời gian hiển thị (ms), mặc định 3500ms
     */
    window.showToast = (message, type = 'success', duration = 3500) => {
        if (!message) return;
        let box = document.querySelector('.rm-toasts');
        if (!box) {
            box = document.createElement('div');
            box.className = 'rm-toasts';
            box.setAttribute('aria-live', 'polite');
            document.body.appendChild(box);
        }

        const toast = document.createElement('div');
        toast.className = `rm-toast rm-toast-${type}`;
        toast.setAttribute('role', type === 'error' ? 'alert' : 'status');

        let iconSvg = '';
        if (type === 'success') {
            iconSvg = '<svg viewBox="0 0 20 20" width="18" height="18" fill="currentColor"><path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd" /></svg>';
        } else if (type === 'error') {
            iconSvg = '<svg viewBox="0 0 20 20" width="18" height="18" fill="currentColor"><path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7 4a1 1 0 11-2 0 1 1 0 012 0zm-1-9a1 1 0 00-1 1v4a1 1 0 102 0V6a1 1 0 00-1-1z" clip-rule="evenodd" /></svg>';
        } else if (type === 'warning') {
            iconSvg = '<svg viewBox="0 0 20 20" width="18" height="18" fill="currentColor"><path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clip-rule="evenodd" /></svg>';
        } else {
            iconSvg = '<svg viewBox="0 0 20 20" width="18" height="18" fill="currentColor"><path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clip-rule="evenodd" /></svg>';
        }

        toast.innerHTML = `
            <div class="rm-toast-icon" aria-hidden="true">${iconSvg}</div>
            <div class="rm-toast-body">
                <span class="rm-toast-msg">${escapeHtml(message)}</span>
            </div>
            <button type="button" class="rm-toast-close" aria-label="${window.rmT('Layout_Close', 'Đóng')}">&times;</button>
        `;

        const dismiss = () => {
            if (toast.dataset.dismissing) return;
            toast.dataset.dismissing = 'true';
            toast.classList.add('rm-toast-hiding');
            setTimeout(() => toast.remove(), 250);
        };

        toast.querySelector('.rm-toast-close').addEventListener('click', dismiss);
        box.appendChild(toast);

        if (duration > 0) {
            setTimeout(dismiss, duration);
        }
    };

    /**
     * Hiển thị Modal Dialog xác nhận xóa / hủy thay thế confirm() mặc định của trình duyệt
     * @returns {Promise<boolean>}
     */
    window.rmConfirm = (message, options = {}) => {
        return new Promise((resolve) => {
            const title = options.title || window.rmT('Js_ConfirmTitle', 'Xác nhận thao tác');
            const confirmText = options.confirmText || window.rmT('Js_ConfirmBtn', 'Xác nhận');
            const cancelText = options.cancelText || window.rmT('Js_CancelBtn', 'Hủy bỏ');
            const isDanger = options.danger !== false;

            const overlay = document.createElement('div');
            overlay.className = 'rm-modal-overlay';
            overlay.setAttribute('role', 'dialog');
            overlay.setAttribute('aria-modal', 'true');
            overlay.innerHTML = `
                <div class="rm-modal-card">
                    <div class="rm-modal-header">
                        <div class="rm-modal-icon-badge ${isDanger ? 'danger' : 'warning'}" aria-hidden="true">
                            <svg viewBox="0 0 24 24" width="24" height="24" fill="none" stroke="currentColor" stroke-width="2">
                                ${isDanger
                                    ? '<circle cx="12" cy="12" r="10"></circle><line x1="12" y1="8" x2="12" y2="12"></line><line x1="12" y1="16" x2="12.01" y2="16"></line>'
                                    : '<path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"></path><line x1="12" y1="9" x2="12" y2="13"></line><line x1="12" y1="17" x2="12.01" y2="17"></line>'}
                            </svg>
                        </div>
                        <h5 class="rm-modal-title">${escapeHtml(title)}</h5>
                    </div>
                    <div class="rm-modal-body">
                        <p class="mb-0">${escapeHtml(message)}</p>
                    </div>
                    <div class="rm-modal-footer">
                        <button type="button" class="btn btn-outline-secondary rounded-pill px-3 py-1 btn-modal-cancel" style="font-size:13.5px;">${escapeHtml(cancelText)}</button>
                        <button type="button" class="btn ${isDanger ? 'btn-danger' : 'btn-primary'} rounded-pill px-4 py-1 btn-modal-confirm" style="font-size:13.5px;">${escapeHtml(confirmText)}</button>
                    </div>
                </div>
            `;

            const close = (result) => {
                overlay.remove();
                document.body.classList.remove('rm-modal-open');
                resolve(result);
            };

            overlay.querySelector('.btn-modal-cancel').addEventListener('click', () => close(false));
            overlay.querySelector('.btn-modal-confirm').addEventListener('click', () => close(true));
            overlay.addEventListener('click', (e) => {
                if (e.target === overlay) close(false);
            });

            document.body.appendChild(overlay);
            document.body.classList.add('rm-modal-open');
        });
    };

    // Bắt sự kiện submit form có attribute data-confirm
    document.addEventListener('submit', async (e) => {
        const form = e.target;
        const msg = form.getAttribute('data-confirm');
        if (!msg) return;

        e.preventDefault();
        const ok = await window.rmConfirm(msg, {
            title: form.getAttribute('data-confirm-title') || window.rmT('Js_ConfirmTitle', 'Xác nhận thao tác'),
            confirmText: form.getAttribute('data-confirm-btn') || window.rmT('Js_ConfirmBtn', 'Xác nhận'),
            danger: form.getAttribute('data-confirm-danger') !== 'false'
        });

        if (ok) {
            form.removeAttribute('data-confirm');
            form.submit();
        }
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
            btn.setAttribute('aria-label', isFavorite ? window.rmT('Card_UnfavoriteAria', 'Bỏ yêu thích') : window.rmT('Card_FavoriteAria', 'Thêm vào yêu thích'));
            btn.setAttribute('aria-pressed', String(isFavorite));
        });
    };

    const thongBao = (message, type = 'info', title = null) => {
        if (window.showNotifyBox) {
            window.showNotifyBox({
                title: title || (type === 'success' ? window.rmT('Js_FavSavedTitle', 'Đã lưu yêu thích') : type === 'error' ? window.rmT('Js_Error', 'Lỗi') : window.rmT('Js_Notice', 'Thông báo')),
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
                thongBao(window.rmT('Js_FavListNotUpdated', 'Chưa cập nhật được danh sách yêu thích. Vui lòng thử lại.'), 'warning', window.rmT('Js_Note', 'Cần lưu ý'));
                return;
            }
            const data = await res.json();
            if (!data.success) {
                thongBao(data.message || window.rmT('Js_FavSaveFailed', 'Không thể lưu sản phẩm yêu thích này.'), 'error', window.rmT('Js_FavFailedTitle', 'Không thành công'));
                return;
            }
            danhDau(productId, data.isFavorite);
            thongBao(
                data.isFavorite ? window.rmT('Js_FavAddedMsg', 'Sản phẩm đã được thêm vào danh sách yêu thích của bạn!') : window.rmT('Js_FavRemovedMsg', 'Đã bỏ sản phẩm khỏi danh sách yêu thích.'),
                data.isFavorite ? 'success' : 'info',
                data.isFavorite ? window.rmT('Nav_Favorites', 'Yêu thích') : window.rmT('Js_FavRemovedTitle', 'Đã bỏ yêu thích')
            );
            if (!data.isFavorite && window.location.pathname.toLowerCase().startsWith('/user/favorites')) {
                const item = btn.closest('.col') ?? btn.closest('.rm-product-card');
                item?.remove();
                if (!document.querySelector('.rm-product-card')) window.location.reload();
            }
        } catch {
            thongBao(window.rmT('Js_NetworkInterrupt', 'Mạng đang gián đoạn. Sản phẩm chưa được cập nhật.'), 'error', window.rmT('Js_ConnectionError', 'Lỗi kết nối'));
        } finally {
            btn.disabled = false;
        }
    });

    const napTrangThaiYeuThich = async () => {
        if (!csrf || !document.querySelector('.rm-card-favorite-btn')) return;
        try {
            const res = await fetch(idsUrl, { headers: { 'X-Requested-With': 'XMLHttpRequest' } });
            const ids = await res.json();
            ids.forEach(id => danhDau(id, true));
        } catch { /* chưa đăng nhập hoặc lỗi mạng: bỏ qua */ }
    };

    window.addEventListener('load', napTrangThaiYeuThich);
    // B-04: lưới kết quả thay bằng AJAX không phát sự kiện load, nên bo-loc-nhanh.js phát sự kiện này.
    document.addEventListener('rm:ket-qua-da-thay', napTrangThaiYeuThich);
})();


// =========================================================================
// Lightbox Modal phóng to ảnh (B-05) + Gallery dải thumbnail cuộn ngang
// =========================================================================
(() => {
    let modal = null;
    let imgEl = null;
    let capEl = null;
    let demEl = null;
    let danhSach = [];
    let viTri = 0;
    let chuThich = '';
    let nguonMo = null;

    const hienAnh = () => {
        if (!danhSach.length) return;
        viTri = (viTri + danhSach.length) % danhSach.length;
        imgEl.src = danhSach[viTri];
        capEl.textContent = chuThich;
        demEl.textContent = danhSach.length > 1 ? `${viTri + 1} / ${danhSach.length}` : '';
        modal.classList.toggle('rm-lightbox-mot-anh', danhSach.length < 2);
    };

    const createModal = () => {
        if (modal) return;
        modal = document.createElement('div');
        modal.id = 'rmImageLightbox';
        modal.className = 'rm-lightbox-overlay';
        modal.hidden = true;
        modal.setAttribute('role', 'dialog');
        modal.setAttribute('aria-modal', 'true');
        modal.setAttribute('aria-label', window.rmT('Js_LightboxAria', 'Ảnh phóng to'));
        modal.innerHTML = `
            <div class="rm-lightbox-container">
                <button type="button" class="rm-lightbox-close" aria-label="${window.rmT('Layout_Close', 'Đóng')}">&times;</button>
                <button type="button" class="rm-lightbox-nav rm-lightbox-prev" aria-label="${window.rmT('Detail_PrevImage', 'Ảnh trước')}">&#8249;</button>
                <button type="button" class="rm-lightbox-nav rm-lightbox-next" aria-label="${window.rmT('Detail_NextImage', 'Ảnh tiếp theo')}">&#8250;</button>
                <div class="rm-lightbox-body">
                    <img class="rm-lightbox-img" src="" alt="${window.rmT('Js_LightboxAria', 'Ảnh phóng to')}" />
                    <div class="rm-lightbox-caption"></div>
                    <div class="rm-lightbox-counter" aria-live="polite"></div>
                </div>
            </div>
        `;
        document.body.appendChild(modal);

        imgEl = modal.querySelector('.rm-lightbox-img');
        capEl = modal.querySelector('.rm-lightbox-caption');
        demEl = modal.querySelector('.rm-lightbox-counter');

        const closeLightbox = () => {
            modal.hidden = true;
            imgEl.src = '';
            document.body.classList.remove('rm-modal-open');
            nguonMo?.focus();
            nguonMo = null;
        };
        const chuyen = (bac) => {
            viTri += bac;
            hienAnh();
        };

        modal.querySelector('.rm-lightbox-close').addEventListener('click', closeLightbox);
        modal.querySelector('.rm-lightbox-prev').addEventListener('click', () => chuyen(-1));
        modal.querySelector('.rm-lightbox-next').addEventListener('click', () => chuyen(1));
        modal.addEventListener('click', (e) => {
            if (!e.target.closest('.rm-lightbox-img') && !e.target.closest('.rm-lightbox-nav')) {
                closeLightbox();
            }
        });

        document.addEventListener('keydown', (e) => {
            if (!modal || modal.hidden) return;
            if (e.key === 'Escape') closeLightbox();
            else if (e.key === 'ArrowLeft') chuyen(-1);
            else if (e.key === 'ArrowRight') chuyen(1);
        });
    };

    const moLightbox = (anh, index, caption, nguon) => {
        if (!anh.length) return;
        createModal();
        danhSach = anh;
        viTri = index;
        chuThich = caption || '';
        nguonMo = nguon || null;
        hienAnh();
        modal.hidden = false;
        document.body.classList.add('rm-modal-open');
        modal.querySelector('.rm-lightbox-close').focus();
    };

    window.rmPhongToAnh = moLightbox;

    // Trang đấu giá và mọi ảnh đánh dấu data-zoomable ngoài gallery: phóng to đơn lẻ.
    document.addEventListener('click', (e) => {
        const zoomImg = e.target.closest('.auction-main-img, img[data-zoomable]');
        if (!zoomImg || zoomImg.closest('[data-anh-bo-suu]')) return;
        e.preventDefault();
        const caption = zoomImg.alt || zoomImg.title || document.querySelector('.detail-title, .bid-title')?.textContent?.trim() || '';
        moLightbox([zoomImg.src], 0, caption, zoomImg);
    });

    // Click thumbnail ngoài gallery (trang đấu giá) vẫn đổi ảnh chính như trước.
    document.addEventListener('click', (e) => {
        const thumb = e.target.closest('.auction-thumbs img');
        if (!thumb) return;
        const mainImg = document.querySelector('#mainImage, .auction-main-img');
        if (mainImg) mainImg.src = thumb.src;
    });
})();

// =========================================================================
// Gallery trang chi tiết sản phẩm (B-05): thumbnail cuộn ngang, mũi tên đổi
// ảnh chính và đồng vị "3 / 12". Bấm ảnh chính hoặc ảnh đang xem sẽ mở lightbox.
// =========================================================================
(() => {
    const gallery = document.querySelector('[data-anh-bo-suu]');
    if (!gallery) return;

    const mainImg = gallery.querySelector('.detail-main-img img');
    if (!mainImg) return;

    const thumbs = Array.from(gallery.querySelectorAll('.thumbnail-item'));
    const danhSach = thumbs.map(t => t.querySelector('img')?.src).filter(Boolean);
    const dem = gallery.querySelector('[data-anh-dem]');
    const kiemChuyenDong = window.matchMedia?.('(prefers-reduced-motion: reduce)').matches ?? false;
    let viTri = 0;

    // Không đủ ảnh để điều hướng thì chỉ còn đường dẫn mở lightbox.
    if (danhSach.length < 2) {
        mainImg.addEventListener('click', () => {
            window.rmPhongToAnh([mainImg.src], 0, mainImg.alt, mainImg);
        });
        return;
    }

    const chuyenAnh = (index, giuThumbTrongMat) => {
        viTri = (index + danhSach.length) % danhSach.length;
        mainImg.src = danhSach[viTri];
        thumbs.forEach((t, i) => {
            t.classList.toggle('active', i === viTri);
            if (i === viTri) t.setAttribute('aria-current', 'true');
            else t.removeAttribute('aria-current');
        });
        if (dem) dem.textContent = `${viTri + 1} / ${danhSach.length}`;
        if (giuThumbTrongMat) thumbs[viTri].scrollIntoView?.({ behavior: kiemChuyenDong ? 'auto' : 'smooth', block: 'nearest', inline: 'nearest' });
    };

    gallery.addEventListener('click', (e) => {
        const bac = e.target.closest('[data-anh-bac]');
        if (bac) {
            e.preventDefault();
            chuyenAnh(viTri + Number(bac.dataset.anhBac), true);
            return;
        }
        const thumb = e.target.closest('.thumbnail-item');
        if (thumb && thumb.dataset.anhIndex !== undefined) {
            chuyenAnh(Number(thumb.dataset.anhIndex), false);
        }
    });

    mainImg.addEventListener('click', () => {
        window.rmPhongToAnh(danhSach, viTri, mainImg.alt, mainImg);
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
        if (so) so.textContent = window.rmT('Js_CharCount', `${dai}/${toiDa} ký tự`, dai, toiDa);
        if (!trangThai) return;
        if (dai === 0) {
            trangThai.textContent = window.rmT('Js_NoDescriptionYet', 'Chưa có mô tả');
            trangThai.className = 'text-muted';
        } else if (dai < toiThieu) {
            trangThai.textContent = window.rmT('Js_DescriptionShortBy', `Còn thiếu ${toiThieu - dai} ký tự để gửi duyệt`, toiThieu - dai);
            trangThai.className = 'text-danger';
        } else {
            trangThai.textContent = window.rmT('Js_DescriptionLongEnough', 'Đủ độ dài để gửi duyệt');
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
    const tenLoc = soLoc > 0 ? window.rmT('Js_FiltersCount', `bộ lọc (${soLoc})`, soLoc) : window.rmT('Js_Filters', 'bộ lọc');
    const capNhat = () => {
        const mo = panel.classList.contains('show');
        btn.textContent = `${mo ? window.rmT('Common_Hide', 'Ẩn') : window.rmT('Common_Show', 'Hiện')} ${tenLoc}`;
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

    const updateCartCount = async (exactCount) => {
        const applyCount = (c) => {
            document.querySelectorAll('.rm-cart-badge, #rmCartBadge, .rm-cart-badge-mobile').forEach(badge => {
                if (c > 0) {
                    badge.textContent = c > 99 ? '99+' : String(c);
                    badge.style.display = badge.classList.contains('rm-cart-badge-mobile') ? 'inline-block' : 'inline-flex';
                    badge.hidden = false;
                } else {
                    badge.style.display = 'none';
                }
            });
        };

        if (typeof exactCount === 'number') {
            applyCount(exactCount);
            return;
        }

        try {
            const res = await fetch('/User/Cart/DemSoLuong');
            if (res.ok) {
                const data = await res.json();
                if (typeof data.count === 'number') {
                    applyCount(data.count);
                }
            }
        } catch { }
    };

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
                const prodName = form.dataset.productName || document.querySelector('.detail-title, .product-title')?.textContent?.trim() || window.rmT('Js_Product', 'Sản phẩm');
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
                        title: window.rmT('Layout_CartAdded', 'Đã thêm vào giỏ hàng!'),
                        message: window.rmT('Js_CartAddedMsg', `Đã thêm "${prodName}" vào giỏ hàng thành công.`, prodName),
                        type: 'success',
                        buttonText: window.rmT('Js_Ok', 'Đồng ý')
                    });
                }

                updateCartCount(typeof data.cartCount === 'number' ? data.cartCount : undefined);
            } else {
                window.showNotifyBox({
                    title: window.rmT('Js_CartNotice', 'Thông báo giỏ hàng'),
                    message: data.message || window.rmT('Js_CartAddFailed', 'Không thể thêm sản phẩm vào giỏ hàng.'),
                    type: 'warning',
                    buttonText: window.rmT('Js_GotIt', 'Đã hiểu')
                });
            }
        } catch {
            window.showNotifyBox({
                title: window.rmT('Js_ConnectionError', 'Lỗi kết nối'),
                message: window.rmT('Js_CannotConnect', 'Không thể kết nối máy chủ. Vui lòng thử lại sau.'),
                type: 'error',
                buttonText: window.rmT('Layout_Close', 'Đóng')
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
                    <button type="button" class="rm-notify-close-x" id="rmNotifyCloseX" aria-label="${window.rmT('Layout_Close', 'Đóng')}">&times;</button>
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
        const title = opts.title || (type === 'success' ? window.rmT('Js_Success', 'Thành công!') : type === 'error' ? window.rmT('Js_ErrorOccurred', 'Đã xảy ra lỗi') : type === 'warning' ? window.rmT('Js_Note', 'Cần lưu ý') : window.rmT('Js_Notice', 'Thông báo'));
        const message = opts.message || '';
        const btnText = opts.buttonText || window.rmT('Js_Ok', 'Đồng ý');

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

// =========================================================================
// B-01 (vòng 2): form banner Admin — xem trước ảnh vừa chọn và chặn sớm file
// sai định dạng/dung lượng theo đúng luật của UploadAnhAnToan phía server.
// =========================================================================
(() => {
    const forms = document.querySelectorAll('[data-banner-form]');
    if (!forms.length) return;

    const dinhDangChoPhep = ['image/jpeg', 'image/png', 'image/webp'];
    const dungLuongToiDa = 5 * 1024 * 1024;

    forms.forEach((form) => {
        const input = form.querySelector('[data-banner-file]');
        const khoiXemTruoc = form.querySelector('[data-banner-preview]');
        if (!input || !khoiXemTruoc) return;

        let duongDanTam = '';
        const hienKhung = (noiDung) => {
            if (duongDanTam) URL.revokeObjectURL(duongDanTam);
            duongDanTam = '';
            khoiXemTruoc.textContent = noiDung;
            khoiXemTruoc.hidden = !noiDung;
        };

        input.addEventListener('change', () => {
            const file = input.files && input.files[0];
            if (!file) return hienKhung('');

            const loi = !dinhDangChoPhep.includes(file.type)
                ? window.rmT('Js_BannerOnlyFormats', 'Chỉ nhận ảnh JPG, PNG hoặc WEBP.')
                : file.size > dungLuongToiDa
                    ? window.rmT('Js_BannerTooLarge', 'Ảnh banner tối đa 5MB.')
                    : null;
            if (loi) {
                input.value = '';
                return hienKhung(loi);
            }

            duongDanTam = URL.createObjectURL(file);
            khoiXemTruoc.hidden = false;
            khoiXemTruoc.textContent = '';
            const img = document.createElement('img');
            img.alt = window.rmT('Js_BannerPreview', 'Xem trước banner');
            img.className = 'mt-2 rounded border';
            img.style.cssText = 'max-height:90px;max-width:100%;object-fit:cover;';
            img.src = duongDanTam;
            khoiXemTruoc.appendChild(img);
        });
    });
})();

