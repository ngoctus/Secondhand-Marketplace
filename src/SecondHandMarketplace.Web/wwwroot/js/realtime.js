// NotificationHub: toast + cập nhật số chưa đọc trên chuông. Chỉ nạp khi đã đăng nhập (xem _Layout).
(() => {
    if (typeof signalR === 'undefined') return;
    const hubUrl = document.currentScript?.dataset.hub;   // phải đọc ngay khi script chạy
    if (!hubUrl) return;

    const bell = () => document.getElementById('notifyCount');
    const setCount = (n) => {
        const el = bell();
        if (!el) return;
        el.textContent = n > 99 ? '99+' : String(n);
        el.hidden = n <= 0;
    };
    const bump = () => {
        const el = bell();
        const cur = el && !el.hidden ? parseInt(el.textContent, 10) || 0 : 0;
        setCount(cur + 1);
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
        el.href = n.url || '/User/Notifications';
        const title = document.createElement('b');
        title.textContent = n.title;          // textContent: tên sản phẩm do người dùng nhập, tránh XSS
        const body = document.createElement('span');
        body.textContent = n.content;
        el.append(title, body);
        box.appendChild(el);
        setTimeout(() => el.remove(), 8000);
    };

    const conn = new signalR.HubConnectionBuilder()
        .withUrl(hubUrl)
        .withAutomaticReconnect()
        .build();

    conn.on('ReceiveNotification', (n) => {
        toast(n);
        if (typeof n.unreadCount === 'number') setCount(n.unreadCount); else bump();
    });

    conn.start().catch(() => { /* lỗi mạng: lần tải trang sau sẽ có số đúng từ server */ });
})();