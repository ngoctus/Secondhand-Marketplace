// Chat 1-1 Người mua ↔ Người bán (ChatHub). Widget góc phải màn hình, mọi class có tiền tố rmc-.
(() => {
    if (typeof signalR === 'undefined') return;
    const d = document.currentScript?.dataset;
    if (!d?.hubUrl) return;

    const cfg = {
        hub: d.hubUrl,
        conversations: d.conversationsUrl,
        start: d.startUrl,
        unread: d.unreadUrl,
        me: Number(d.userId),
        csrf: d.csrfToken
    };
    const PAGE = 30;

    const el = (tag, cls, text) => {
        const e = document.createElement(tag);
        if (cls) e.className = cls;
        if (text != null) e.textContent = text;   // textContent: nội dung do người dùng nhập, tránh XSS
        return e;
    };
    const timeText = (iso) => {
        const t = new Date(iso), now = new Date();
        const hm = t.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' });
        return t.toDateString() === now.toDateString()
            ? hm
            : t.toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit' }) + ' ' + hm;
    };
    async function api(url, { method = 'GET', form } = {}) {
        const headers = { Accept: 'application/json' };
        if (form) {
            headers['Content-Type'] = 'application/x-www-form-urlencoded';
            headers.RequestVerificationToken = cfg.csrf;
        }
        const res = await fetch(url, { method, headers, body: form ? new URLSearchParams(form) : undefined });
        if (!(res.headers.get('content-type') || '').includes('json'))
            throw new Error('Phiên đăng nhập đã hết hạn, vui lòng tải lại trang.');
        return { ok: res.ok, data: await res.json() };
    }

    const st = { open: false, view: 'list', convs: [], cur: null, oldestId: null, hasMore: false, unread: 0, conn: null };

    // ----- Giao diện -----
    const root = el('div', 'rmc');
    const launcher = el('button', 'rmc-launcher');
    launcher.type = 'button';
    launcher.setAttribute('aria-expanded', 'false');
    const badge = el('span', 'rmc-badge');
    badge.hidden = true;
    launcher.append(el('span', 'rmc-launcher-icon', '💬'), el('span', null, 'Tin nhắn'), badge);

    const panel = el('section', 'rmc-panel');
    panel.hidden = true;
    panel.setAttribute('role', 'dialog');
    panel.setAttribute('aria-label', 'Tin nhắn với người bán');

    const head = el('div', 'rmc-head');
    const back = el('button', 'rmc-icon', '←');
    back.type = 'button';
    back.hidden = true;
    back.setAttribute('aria-label', 'Quay lại danh sách');
    const titleBox = el('div', 'rmc-title-box');
    const title = el('div', 'rmc-title', 'Tin nhắn');
    const sub = el('div', 'rmc-sub');
    sub.hidden = true;
    titleBox.append(title, sub);
    const close = el('button', 'rmc-icon', '✕');
    close.type = 'button';
    close.setAttribute('aria-label', 'Đóng');
    head.append(back, titleBox, close);

    const list = el('div', 'rmc-list');
    const thread = el('div', 'rmc-thread');
    thread.hidden = true;
    const msgs = el('div', 'rmc-msgs');
    const more = el('button', 'rmc-more', 'Xem tin cũ hơn');
    more.type = 'button';
    more.hidden = true;
    const hint = el('div', 'rmc-hint');
    hint.hidden = true;
    const form = el('form', 'rmc-composer');
    const input = el('textarea', 'rmc-input');
    input.rows = 1;
    input.maxLength = 1000;
    input.placeholder = 'Nhập tin nhắn...';
    const sendBtn = el('button', 'rmc-send', 'Gửi');
    sendBtn.type = 'submit';
    form.append(input, sendBtn);
    thread.append(msgs, hint, form);

    panel.append(head, list, thread);
    root.append(panel, launcher);
    document.body.append(root);

    // ----- Tiện ích hiển thị -----
    const setBadge = () => {
        badge.textContent = st.unread > 99 ? '99+' : String(st.unread);
        badge.hidden = st.unread <= 0;
    };
    let hintTimer = null;
    const showHint = (text) => {
        hint.textContent = text || '';
        hint.hidden = !text;
        clearTimeout(hintTimer);
        if (text) hintTimer = setTimeout(() => { hint.hidden = true; }, 4000);
    };
    const nearBottom = () => msgs.scrollHeight - msgs.scrollTop - msgs.clientHeight < 80;
    const toBottom = () => { msgs.scrollTop = msgs.scrollHeight; };

    function bubble(m) {
        const mine = m.senderId === cfg.me;
        const row = el('div', 'rmc-row ' + (mine ? 'rmc-mine' : 'rmc-theirs'));
        row.dataset.id = m.id;
        const b = el('div', 'rmc-bubble');
        b.append(el('div', 'rmc-text', m.content));
        const meta = el('div', 'rmc-meta', timeText(m.sentAtUtc));
        if (mine) meta.append(el('span', 'rmc-tick' + (m.read ? ' rmc-read' : ''), m.read ? ' ✓✓' : ' ✓'));
        b.append(meta);
        row.append(b);
        return row;
    }
    function appendMessage(m) {
        if (msgs.querySelector('[data-id="' + m.id + '"]')) return;   // chống trùng khi tải lại
        msgs.querySelector('.rmc-empty')?.remove();
        const stick = nearBottom() || m.senderId === cfg.me;
        msgs.append(bubble(m));
        if (stick) toBottom();
    }
    function markTicksRead() {
        msgs.querySelectorAll('.rmc-tick').forEach((t) => { t.textContent = ' ✓✓'; t.classList.add('rmc-read'); });
    }

    // ----- Danh sách hội thoại -----
    function renderConversations() {
        if (!st.convs.length) {
            list.replaceChildren(el('div', 'rmc-empty', 'Chưa có cuộc trò chuyện nào. Mở một sản phẩm và bấm "Chat" để nhắn cho người bán.'));
            return;
        }
        list.replaceChildren(...st.convs.map((c) => {
            const row = el('button', 'rmc-conv');
            row.type = 'button';
            const body = el('span', 'rmc-conv-body');
            const top = el('span', 'rmc-conv-top');
            top.append(el('b', 'rmc-conv-name', c.title), el('span', 'rmc-conv-time', timeText(c.lastAtUtc)));
            body.append(top, el('span', 'rmc-conv-last', c.lastMessage || 'Chưa có tin nhắn'));
            row.append(el('span', 'rmc-avatar', (c.title || '?').trim().charAt(0).toUpperCase()), body);
            if (c.unread > 0) row.append(el('span', 'rmc-unread', c.unread > 99 ? '99+' : String(c.unread)));
            row.addEventListener('click', () => showThread(c));
            return row;
        }));
    }
    async function loadConversations() {
        try {
            const { ok, data } = await api(cfg.conversations);
            if (!ok) throw new Error(data.message);
            st.convs = data.data;
            renderConversations();
        } catch (e) {
            list.replaceChildren(el('div', 'rmc-empty', e.message || 'Không tải được danh sách.'));
        }
    }
    async function refreshUnread() {
        try {
            const { ok, data } = await api(cfg.unread);
            if (ok) { st.unread = data.data.count; setBadge(); }
        } catch { /* bỏ qua: lần sau sẽ cập nhật */ }
    }

    // ----- Cuộc trò chuyện -----
    async function loadMessages(older) {
        const conv = st.cur;
        if (!conv) return;
        const q = new URLSearchParams({ take: String(PAGE) });
        if (older && st.oldestId) q.set('before', String(st.oldestId));
        const { ok, data } = await api(cfg.conversations + '/' + conv.id + '/messages?' + q);
        if (conv !== st.cur) return;   // đã chuyển cuộc trò chuyện khác trong lúc tải
        if (!ok) { showHint(data.message || 'Không tải được tin nhắn.'); return; }

        const items = data.data;
        if (items.length) st.oldestId = items[0].id;
        st.hasMore = items.length === PAGE;
        if (older) {
            const prevHeight = msgs.scrollHeight;
            const frag = document.createDocumentFragment();
            items.forEach((m) => { if (!msgs.querySelector('[data-id="' + m.id + '"]')) frag.append(bubble(m)); });
            more.after(frag);
            msgs.scrollTop += msgs.scrollHeight - prevHeight;   // giữ nguyên vị trí đang đọc
        } else {
            msgs.replaceChildren(more);
            if (!items.length) msgs.append(el('div', 'rmc-empty', 'Chưa có tin nhắn. Hãy gửi lời chào!'));
            items.forEach((m) => msgs.append(bubble(m)));
            toBottom();
        }
        more.hidden = !st.hasMore;
    }
    async function markRead(id) {
        const c = st.convs.find((x) => x.id === id);
        if (c) c.unread = 0;
        if (st.conn?.state === signalR.HubConnectionState.Connected) {
            try { await st.conn.invoke('MarkRead', id); } catch { /* không quan trọng */ }
        }
        refreshUnread();
    }
    async function showThread(conv) {
        st.cur = conv;
        st.view = 'thread';
        st.oldestId = null;
        st.hasMore = false;
        title.textContent = conv.title;
        sub.textContent = conv.shop || '';
        sub.hidden = !conv.shop;
        back.hidden = false;
        list.hidden = true;
        thread.hidden = false;
        msgs.replaceChildren(more);
        more.hidden = true;
        showHint('');
        try { await loadMessages(false); } catch (e) { showHint(e.message); }
        markRead(conv.id);
        input.focus();
    }
    async function showList() {
        st.cur = null;
        st.view = 'list';
        title.textContent = 'Tin nhắn';
        sub.hidden = true;
        back.hidden = true;
        thread.hidden = true;
        list.hidden = false;
        await loadConversations();
    }
    async function openById(id) {
        if (!st.convs.some((c) => c.id === id)) await loadConversations();
        const c = st.convs.find((x) => x.id === id);
        if (c) await showThread(c);
    }

    let closingAi = false;
    function setOpen(open) {
        st.open = open;
        panel.hidden = !open;
        launcher.setAttribute('aria-expanded', String(open));
        if (!open) return;
        // Chỉ mở một cửa sổ một lúc: đóng cửa sổ Trợ lý AI nếu đang mở.
        const ai = document.querySelector('.rmchat-toggle[aria-expanded="true"]');
        if (ai) { closingAi = true; ai.click(); closingAi = false; }
        if (st.view === 'thread' && st.cur) showThread(st.cur);   // tải lại phòng trường hợp có tin đến khi đang đóng
        else showList();
    }

    // ----- Thông báo tin mới khi cửa sổ đang đóng -----
    function toast(m) {
        let box = document.querySelector('.rm-toasts');
        if (!box) {
            box = el('div', 'rm-toasts');
            box.setAttribute('aria-live', 'polite');
            document.body.append(box);
        }
        const a = el('a', 'rm-toast');
        a.href = '#';
        const text = m.content.length > 80 ? m.content.slice(0, 77) + '...' : m.content;
        a.append(el('b', null, 'Tin nhắn mới từ ' + (m.senderName || 'người dùng')), el('span', null, text));
        a.addEventListener('click', async (e) => {
            e.preventDefault();
            a.remove();
            st.view = 'list';
            setOpen(true);
            await openById(m.conversationId);
        });
        box.append(a);
        setTimeout(() => a.remove(), 8000);
    }

    function onReceive(m) {
        const mine = m.senderId === cfg.me;
        const viewing = st.open && st.view === 'thread' && st.cur && st.cur.id === m.conversationId;
        if (viewing) {
            appendMessage(m);
            if (!mine) markRead(m.conversationId);
        } else if (!mine) {
            st.unread++;
            setBadge();
            toast(m);
        }
        if (st.open && st.view === 'list') loadConversations();
    }

    // ----- SignalR -----
    function connect() {
        const conn = new signalR.HubConnectionBuilder().withUrl(cfg.hub).withAutomaticReconnect().build();
        conn.on('ReceiveMessage', onReceive);
        conn.on('MessagesRead', (p) => { if (st.cur && st.cur.id === p.conversationId) markTicksRead(); });
        conn.onreconnected(() => {
            refreshUnread();
            if (st.open && st.view === 'thread' && st.cur) showThread(st.cur);   // lấy lại tin bị lỡ khi mất kết nối
        });
        const run = () => conn.start().catch(() => setTimeout(run, 5000));
        run();
        st.conn = conn;
    }

    // ----- Sự kiện -----
    launcher.addEventListener('click', () => setOpen(!st.open));
    close.addEventListener('click', () => setOpen(false));
    back.addEventListener('click', showList);
    more.addEventListener('click', () => loadMessages(true).catch((e) => showHint(e.message)));
    document.addEventListener('keydown', (e) => { if (e.key === 'Escape' && st.open) setOpen(false); });
    document.addEventListener('click', (e) => {
        if (!closingAi && st.open && e.target.closest('.rmchat-toggle')) setOpen(false);
    });

    input.addEventListener('input', () => {
        input.style.height = 'auto';
        input.style.height = Math.min(input.scrollHeight, 96) + 'px';
    });
    input.addEventListener('keydown', (e) => {
        if (e.key === 'Enter' && !e.shiftKey && !e.isComposing) { e.preventDefault(); form.requestSubmit(); }
    });
    form.addEventListener('submit', async (e) => {
        e.preventDefault();
        const text = input.value.trim();
        if (!text || !st.cur) return;
        if (st.conn?.state !== signalR.HubConnectionState.Connected) { showHint('Đang kết nối lại, vui lòng thử lại sau giây lát.'); return; }
        sendBtn.disabled = true;
        try {
            await st.conn.invoke('SendMessage', st.cur.id, text);
            input.value = '';
            input.style.height = 'auto';
        } catch (err) {
            const msg = String(err?.message || '');
            const i = msg.indexOf('HubException: ');
            showHint(i >= 0 ? msg.slice(i + 14) : 'Không gửi được tin nhắn.');
        } finally {
            sendBtn.disabled = false;
            input.focus();
        }
    });

    // Nút "Chat với người bán" ở trang sản phẩm / phiên đấu giá: <button class="js-chat-seller" data-product-id data-prefill>
    document.addEventListener('click', async (e) => {
        const btn = e.target.closest('.js-chat-seller');
        if (!btn) return;
        e.preventDefault();
        btn.disabled = true;
        try {
            const { ok, data } = await api(cfg.start, { method: 'POST', form: { productId: btn.dataset.productId } });
            if (!ok || !data.success) { alert(data.message || 'Không mở được cuộc trò chuyện.'); return; }
            setOpen(true);
            await showThread(data.data);
            if (btn.dataset.prefill && !input.value) input.value = btn.dataset.prefill;
            input.focus();
        } catch (err) {
            alert(err.message);
        } finally {
            btn.disabled = false;
        }
    });

    connect();
    refreshUnread();
})();