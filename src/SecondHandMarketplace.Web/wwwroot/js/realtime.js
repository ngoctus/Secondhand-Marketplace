// NotificationHub: toast + cập nhật số chưa đọc trên chuông + Popover Dropdown xem nhanh (Task C-02).
// Chỉ nạp khi đã đăng nhập (xem _Layout).
(() => {
    // 1. Quản lý số đếm & trạng thái chuông
    const bell = () => document.getElementById('notifyCount');
    const getCount = () => {
        const el = bell();
        return el && !el.hidden ? parseInt(el.textContent, 10) || 0 : 0;
    };

    const setCount = (n) => {
        const el = bell();
        if (el) {
            el.textContent = n > 99 ? '99+' : String(n);
            el.hidden = n <= 0;
        }

        const bellBtn = document.getElementById('notifyBellBtn');
        if (bellBtn) {
            bellBtn.setAttribute('aria-label', `Thông báo (${n} chưa đọc)`);
        }

        const unreadLabel = document.getElementById('notifyUnreadLabel');
        if (unreadLabel) {
            if (n > 0) {
                unreadLabel.textContent = `(${n > 99 ? '99+' : n} mới)`;
                unreadLabel.style.display = '';
            } else {
                unreadLabel.style.display = 'none';
            }
        }

        const markAllBtn = document.getElementById('markAllReadBtn');
        if (markAllBtn) {
            markAllBtn.disabled = n <= 0;
        }
    };

    const bump = () => {
        setCount(getCount() + 1);
    };

    // Helper lấy CSRF token
    const getCsrfToken = () => {
        return document.querySelector('#notifyCsrfForm input[name="__RequestVerificationToken"]')?.value ||
               document.querySelector('meta[name="rm-csrf"]')?.content ||
               document.querySelector('input[name="__RequestVerificationToken"]')?.value ||
               '';
    };

    // 2. Xử lý click từng thông báo để đánh dấu đã đọc
    const handleItemClick = (e) => {
        const item = e.currentTarget;
        if (item.dataset.unread !== 'true') return;

        const id = item.dataset.id;
        if (!id) return;

        // Cập nhật trực quan ngay lập tức
        item.classList.remove('is-unread');
        item.dataset.unread = 'false';
        item.querySelector('.rm-notify-badge-new')?.remove();
        item.querySelector('.rm-notify-unread-dot')?.remove();

        const newCount = Math.max(0, getCount() - 1);
        setCount(newCount);

        // Gửi POST đánh dấu đã đọc ngầm (keepalive giúp hoàn thành kể cả khi trang đang điều hướng)
        const csrf = getCsrfToken();
        const body = new URLSearchParams({ id: id, __RequestVerificationToken: csrf });

        try {
            fetch(`/User/Notifications/MarkRead?id=${encodeURIComponent(id)}`, {
                method: 'POST',
                headers: {
                    'X-Requested-With': 'XMLHttpRequest',
                    'RequestVerificationToken': csrf,
                    'Content-Type': 'application/x-www-form-urlencoded'
                },
                body: body.toString(),
                keepalive: true
            }).then(r => (r.ok ? r.json() : null)).then(data => {
                if (data && typeof data.unreadCount === 'number') {
                    setCount(data.unreadCount);
                }
            }).catch(() => {});
        } catch {
            // fallback an toàn nếu fetch thất bại
        }
    };

    // Gắn sự kiện click cho các item có sẵn do Razor render
    document.querySelectorAll('#notifyDropList .rm-notify-item').forEach(el => {
        el.addEventListener('click', handleItemClick);
    });

    // 3. Xử lý nút "Đã đọc tất cả"
    const markAllReadBtn = document.getElementById('markAllReadBtn');
    if (markAllReadBtn) {
        markAllReadBtn.addEventListener('click', async (e) => {
            e.preventDefault();
            e.stopPropagation();

            if (markAllReadBtn.disabled) return;
            markAllReadBtn.disabled = true;

            const csrf = getCsrfToken();
            const body = new URLSearchParams({ __RequestVerificationToken: csrf });

            try {
                const res = await fetch('/User/Notifications/MarkAllRead', {
                    method: 'POST',
                    headers: {
                        'X-Requested-With': 'XMLHttpRequest',
                        'RequestVerificationToken': csrf,
                        'Content-Type': 'application/x-www-form-urlencoded'
                    },
                    body: body.toString()
                });

                if (res.ok) {
                    setCount(0);
                    document.querySelectorAll('#notifyDropList .rm-notify-item.is-unread').forEach(el => {
                        el.classList.remove('is-unread');
                        el.dataset.unread = 'false';
                        el.querySelector('.rm-notify-badge-new')?.remove();
                        el.querySelector('.rm-notify-unread-dot')?.remove();
                    });
                } else {
                    markAllReadBtn.disabled = false;
                }
            } catch {
                markAllReadBtn.disabled = false;
            }
        });
    }

    // 4. SignalR Toast & Dynamic List Insertion
    const resolveUrl = (n) => {
        if (n.url) return n.url;
        if (n.id) return `/User/Notifications/ChiTiet/${n.id}`;
        return '/User/Notifications';
    };

    const resolveTypeName = (type) => {
        const t = (type || '').toUpperCase();
        if (t === 'ORDER') return 'Đơn hàng';
        if (t === 'PRODUCT') return 'Sản phẩm';
        return 'Hệ thống';
    };

    const toast = (n) => {
        let box = document.querySelector('.rm-toasts');
        if (!box) {
            box = document.createElement('div');
            box.className = 'rm-toasts';
            box.setAttribute('aria-live', 'polite');
            document.body.appendChild(box);
        }
        const el = document.createElement('a');
        el.className = 'rm-toast';
        el.href = resolveUrl(n);
        const title = document.createElement('b');
        title.textContent = n.title;          // textContent: tên sản phẩm do người dùng nhập, tránh XSS
        const body = document.createElement('span');
        body.textContent = n.content;
        el.append(title, body);
        box.appendChild(el);
        setTimeout(() => el.remove(), 8000);
    };

    const prependNotification = (n) => {
        const list = document.getElementById('notifyDropList');
        const empty = document.getElementById('notifyEmptyState');
        if (!list) return;

        if (empty) empty.style.display = 'none';
        list.style.display = '';

        const type = (n.type || n.loai || '').toUpperCase();
        const typeClass = type.toLowerCase();
        const url = resolveUrl(n);

        const a = document.createElement('a');
        a.href = url;
        a.className = 'rm-notify-item is-unread';
        if (n.id) a.dataset.id = String(n.id);
        a.dataset.unread = 'true';

        let iconSvg = '';
        if (type === 'ORDER') {
            iconSvg = '<svg viewBox="0 0 24 24" width="15" height="15" fill="none" stroke="currentColor" stroke-width="2"><path d="M6 2L3 6v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2V6l-3-4z"/><line x1="3" y1="6" x2="21" y2="6"/><path d="M16 10a4 4 0 0 1-8 0"/></svg>';
        } else if (type === 'PRODUCT') {
            iconSvg = '<svg viewBox="0 0 24 24" width="15" height="15" fill="none" stroke="currentColor" stroke-width="2"><path d="M20.59 13.41l-7.17 7.17a2 2 0 0 1-2.83 0L2 12V2h10l8.59 8.59a2 2 0 0 1 0 2.82z"/><line x1="7" y1="7" x2="7.01" y2="7"/></svg>';
        } else {
            iconSvg = '<svg viewBox="0 0 24 24" width="15" height="15" fill="none" stroke="currentColor" stroke-width="2"><circle cx="12" cy="12" r="10"/><line x1="12" y1="16" x2="12" y2="12"/><line x1="12" y1="8" x2="12.01" y2="8"/></svg>';
        }

        const iconDiv = document.createElement('div');
        iconDiv.className = `rm-notify-item-icon rm-notify-icon-${typeClass}`;
        iconDiv.innerHTML = iconSvg;

        const mainDiv = document.createElement('div');
        mainDiv.className = 'rm-notify-item-main';

        const titleDiv = document.createElement('div');
        titleDiv.className = 'rm-notify-item-title';
        const titleSpan = document.createElement('span');
        titleSpan.textContent = n.title || 'Thông báo mới';
        const badgeSpan = document.createElement('span');
        badgeSpan.className = 'rm-notify-badge-new';
        badgeSpan.textContent = 'MỚI';
        titleDiv.append(titleSpan, badgeSpan);

        const descDiv = document.createElement('div');
        descDiv.className = 'rm-notify-item-desc';
        descDiv.textContent = n.content || '';

        const metaDiv = document.createElement('div');
        metaDiv.className = 'rm-notify-item-meta';
        const typeSpan = document.createElement('span');
        typeSpan.className = 'rm-notify-meta-type';
        typeSpan.textContent = resolveTypeName(type);
        const sepSpan = document.createElement('span');
        sepSpan.className = 'rm-notify-meta-sep';
        sepSpan.textContent = '·';
        const timeSpan = document.createElement('span');
        timeSpan.className = 'rm-notify-meta-time';
        timeSpan.textContent = 'Vừa xong';
        metaDiv.append(typeSpan, sepSpan, timeSpan);

        mainDiv.append(titleDiv, descDiv, metaDiv);

        const dotDiv = document.createElement('div');
        dotDiv.className = 'rm-notify-unread-dot';
        dotDiv.title = 'Chưa đọc';

        a.append(iconDiv, mainDiv, dotDiv);
        a.addEventListener('click', handleItemClick);

        list.prepend(a);

        // Giữ danh sách tối đa 5 thông báo mới nhất
        while (list.children.length > 5) {
            list.lastElementChild.remove();
        }
    };

    // 5. Khởi tạo kết nối SignalR
    if (typeof signalR === 'undefined') return;
    const hubUrl = document.querySelector('script[data-hub]')?.dataset.hub ||
                   document.currentScript?.dataset.hub;
    if (!hubUrl) return;

    const conn = new signalR.HubConnectionBuilder()
        .withUrl(hubUrl)
        .withAutomaticReconnect()
        .build();

    conn.on('ReceiveNotification', (n) => {
        toast(n);
        prependNotification(n);
        if (typeof n.unreadCount === 'number') {
            setCount(n.unreadCount);
        } else {
            bump();
        }
    });

    conn.start().catch(() => { /* lỗi mạng: lần tải trang sau sẽ có số đúng từ server */ });
})();
