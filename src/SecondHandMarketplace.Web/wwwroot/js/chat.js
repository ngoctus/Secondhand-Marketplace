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
        csrf: d.csrfToken,
        lang: d.lang === 'en' || document.documentElement.lang === 'en' ? 'en' : 'vi'
    };
    const isEn = cfg.lang === 'en';
    const LOCALE = isEn ? 'en-US' : 'vi-VN';
    const T = isEn
        ? {
            messages: 'Messages',
            dialogAria: 'Messages with sellers',
            backAria: 'Back to conversations',
            closeAria: 'Close',
            loadOlder: 'Load older messages',
            placeholder: 'Type a message...',
            send: 'Send',
            sessionExpired: 'Your session has expired. Please refresh the page.',
            emptyList: 'No conversations yet. Open any listing and click "Chat" to message the seller.',
            noMessagesShort: 'No messages yet',
            loadListError: 'Could not load conversations.',
            loadMsgsError: 'Could not load messages.',
            emptyThread: 'No messages yet. Say hello!',
            newMsgFrom: (name) => 'New message from ' + (name || 'user'),
            reconnecting: 'Reconnecting, please try again in a moment.',
            sendFailed: 'Could not send message.',
            cannotOpenTitle: 'Unable to open chat',
            cannotOpenMsg: 'Could not start the conversation.',
            errorTitle: 'An error occurred',
            connectError: 'Chat connection error.'
        }
        : {
            messages: 'Tin nhắn',
            dialogAria: 'Tin nhắn với người bán',
            backAria: 'Quay lại danh sách',
            closeAria: 'Đóng',
            loadOlder: 'Xem tin cũ hơn',
            placeholder: 'Nhập tin nhắn...',
            send: 'Gửi',
            sessionExpired: 'Phiên đăng nhập đã hết hạn, vui lòng tải lại trang.',
            emptyList: 'Chưa có cuộc trò chuyện nào. Mở một sản phẩm và bấm "Chat" để nhắn cho người bán.',
            noMessagesShort: 'Chưa có tin nhắn',
            loadListError: 'Không tải được danh sách.',
            loadMsgsError: 'Không tải được tin nhắn.',
            emptyThread: 'Chưa có tin nhắn. Hãy gửi lời chào!',
            newMsgFrom: (name) => 'Tin nhắn mới từ ' + (name || 'người dùng'),
            reconnecting: 'Đang kết nối lại, vui lòng thử lại sau giây lát.',
            sendFailed: 'Không gửi được tin nhắn.',
            cannotOpenTitle: 'Không mở được trò chuyện',
            cannotOpenMsg: 'Không mở được cuộc trò chuyện.',
            errorTitle: 'Đã xảy ra lỗi',
            connectError: 'Lỗi kết nối trò chuyện.'
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
        const hm = t.toLocaleTimeString(LOCALE, { hour: '2-digit', minute: '2-digit' });
        return t.toDateString() === now.toDateString()
            ? hm
            : t.toLocaleDateString(LOCALE, { day: '2-digit', month: '2-digit' }) + ' ' + hm;
    };
    async function api(url, { method = 'GET', form } = {}) {
        const headers = { Accept: 'application/json' };
        if (form) {
            headers['Content-Type'] = 'application/x-www-form-urlencoded';
            headers.RequestVerificationToken = cfg.csrf;
        }
        const res = await fetch(url, { method, headers, body: form ? new URLSearchParams(form) : undefined });
        if (!(res.headers.get('content-type') || '').includes('json'))
            throw new Error(T.sessionExpired);
        return { ok: res.ok, data: await res.json() };
    }

    const st = { open: false, view: 'list', convs: [], cur: null, oldestId: null, hasMore: false, unread: 0, conn: null };

    // ----- Giao diện -----
    const root = el('div', 'rmc');
    const launcher = el('button', 'rmc-launcher');
    launcher.type = 'button';
    launcher.setAttribute('aria-expanded', 'false');
    launcher.setAttribute('aria-label', T.messages);
    launcher.title = T.messages;
    const badge = el('span', 'rmc-badge');
    badge.hidden = true;
    const launcherIcon = el('span', 'rmc-launcher-icon');
    launcherIcon.setAttribute('aria-hidden', 'true');
    launcherIcon.innerHTML = '<svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z"/></svg>';
    launcher.append(launcherIcon, badge);

    const panel = el('section', 'rmc-panel');
    panel.hidden = true;
    panel.setAttribute('role', 'dialog');
    panel.setAttribute('aria-label', T.dialogAria);

    const head = el('div', 'rmc-head');
    const back = el('button', 'rmc-icon', '←');
    back.type = 'button';
    back.hidden = true;
    back.setAttribute('aria-label', T.backAria);
    const titleBox = el('div', 'rmc-title-box');
    const title = el('div', 'rmc-title', T.messages);
    const sub = el('div', 'rmc-sub');
    sub.hidden = true;
    titleBox.append(title, sub);
    const close = el('button', 'rmc-icon', '✕');
    close.type = 'button';
    close.setAttribute('aria-label', T.closeAria);
    head.append(back, titleBox, close);

    const list = el('div', 'rmc-list');
    const thread = el('div', 'rmc-thread');
    thread.hidden = true;
    const msgs = el('div', 'rmc-msgs');
    const more = el('button', 'rmc-more', T.loadOlder);
    more.type = 'button';
    more.hidden = true;
    const hint = el('div', 'rmc-hint');
    hint.hidden = true;
    const form = el('form', 'rmc-composer');
    const input = el('textarea', 'rmc-input');
    input.rows = 1;
    input.maxLength = 1000;
    input.placeholder = T.placeholder;
    const sendBtn = el('button', 'rmc-send', T.send);
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
            list.replaceChildren(el('div', 'rmc-empty', T.emptyList));
            return;
        }
        list.replaceChildren(...st.convs.map((c) => {
            const row = el('button', 'rmc-conv');
            row.type = 'button';
            const body = el('span', 'rmc-conv-body');
            const top = el('span', 'rmc-conv-top');
            top.append(el('b', 'rmc-conv-name', c.title), el('span', 'rmc-conv-time', timeText(c.lastAtUtc)));
            body.append(top, el('span', 'rmc-conv-last', c.lastMessage || T.noMessagesShort));
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
            list.replaceChildren(el('div', 'rmc-empty', e.message || T.loadListError));
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
        if (!ok) { showHint(data.message || T.loadMsgsError); return; }

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
            if (!items.length) msgs.append(el('div', 'rmc-empty', T.emptyThread));
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
        updateMobileViewport();
        toBottom();
        if (window.innerWidth >= 768) {
            input.focus();
        }
    }
    async function showList() {
        st.cur = null;
        st.view = 'list';
        title.textContent = T.messages;
        sub.hidden = true;
        back.hidden = true;
        thread.hidden = true;
        list.hidden = false;
        if (document.activeElement === input) {
            input.blur();
        }
        updateMobileViewport();
        await loadConversations();
    }
    async function openById(id) {
        if (!st.convs.some((c) => c.id === id)) await loadConversations();
        const c = st.convs.find((x) => x.id === id);
        if (c) await showThread(c);
    }

    // Xử lý đồng bộ kích thước viewport cho giao diện Full-screen và chống bàn phím ảo che khuất
    function updateMobileViewport() {
        if (!st.open || window.innerWidth >= 768) {
            panel.style.top = '';
            panel.style.height = '';
            panel.style.bottom = '';
            panel.classList.remove('rmc-keyboard-open');
            return;
        }

        if (window.visualViewport) {
            const vv = window.visualViewport;
            panel.style.top = `${vv.offsetTop}px`;
            panel.style.height = `${vv.height}px`;
            panel.style.bottom = 'auto';

            // Khi visual viewport nhỏ hơn đáng kể so với innerHeight tức là bàn phím ảo đang mở
            const isKeyboardOpen = vv.height < (window.innerHeight - 120);
            if (isKeyboardOpen) {
                panel.classList.add('rmc-keyboard-open');
            } else {
                panel.classList.remove('rmc-keyboard-open');
            }
        } else {
            panel.style.top = '0px';
            panel.style.height = '100dvh';
            panel.style.bottom = '0px';
        }
    }

    function setOpen(open) {
        st.open = open;
        panel.hidden = !open;
        launcher.setAttribute('aria-expanded', String(open));
        if (open) {
            // Không cho bật đồng thời Tin nhắn và Trợ lý AI
            window.dispatchEvent(new CustomEvent('rm:chat-widget-open', { detail: { source: 'p2p' } }));
            root.classList.add('rmc-active');
            if (window.innerWidth < 768) {
                document.body.classList.add('rmc-open');
            }
            updateMobileViewport();
        } else {
            root.classList.remove('rmc-active');
            document.body.classList.remove('rmc-open');
            panel.style.top = '';
            panel.style.height = '';
            panel.style.bottom = '';
            panel.classList.remove('rmc-keyboard-open');
            return;
        }
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
        a.append(el('b', null, T.newMsgFrom(m.senderName)), el('span', null, text));
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
    window.addEventListener('rm:chat-widget-open', (e) => {
        if (e.detail?.source !== 'p2p' && st.open) setOpen(false);
    });

    input.addEventListener('input', () => {
        input.style.height = 'auto';
        input.style.height = Math.min(input.scrollHeight, 96) + 'px';
    });
    input.addEventListener('keydown', (e) => {
        if (e.key === 'Enter' && !e.shiftKey && !e.isComposing) { e.preventDefault(); form.requestSubmit(); }
    });

    // Khi chạm hoặc cuộn xem lịch sử tin nhắn: tự động hạ bàn phím ảo để giải phóng không gian đọc
    msgs.addEventListener('touchstart', () => {
        if (document.activeElement === input) {
            input.blur();
        }
    }, { passive: true });

    // Khi người dùng bấm vào ô nhập trên mobile: bàn phím ảo đẩy lên -> cuộn ngay tin nhắn mới nhất và cập nhật viewport
    input.addEventListener('focus', () => {
        if (window.innerWidth < 768) {
            window.scrollTo(0, 0);
            setTimeout(() => {
                window.scrollTo(0, 0);
                updateMobileViewport();
                toBottom();
            }, 100);
            setTimeout(() => {
                window.scrollTo(0, 0);
                updateMobileViewport();
                toBottom();
            }, 300);
        }
    });

    input.addEventListener('blur', () => {
        if (window.innerWidth < 768) {
            setTimeout(() => {
                window.scrollTo(0, 0);
                updateMobileViewport();
            }, 100);
        }
    });

    form.addEventListener('submit', async (e) => {
        e.preventDefault();
        const text = input.value.trim();
        if (!text || !st.cur) return;
        if (st.conn?.state !== signalR.HubConnectionState.Connected) { showHint(T.reconnecting); return; }
        sendBtn.disabled = true;
        try {
            await st.conn.invoke('SendMessage', st.cur.id, text);
            input.value = '';
            input.style.height = 'auto';
            toBottom();
        } catch (err) {
            const msg = String(err?.message || '');
            const i = msg.indexOf('HubException: ');
            showHint(i >= 0 ? msg.slice(i + 14) : T.sendFailed);
        } finally {
            sendBtn.disabled = false;
            if (window.innerWidth >= 768 || document.activeElement === input) {
                input.focus();
            }
            updateMobileViewport();
        }
    });

    // Lắng nghe thay đổi kích thước do bàn phím ảo bật/tắt (qua VisualViewport API chuẩn mobile)
    if (window.visualViewport) {
        window.visualViewport.addEventListener('resize', () => {
            if (st.open) {
                updateMobileViewport();
                if (st.view === 'thread') toBottom();
            }
        });
        window.visualViewport.addEventListener('scroll', () => {
            if (st.open) {
                updateMobileViewport();
            }
        });
    }

    // Lắng nghe resize cửa sổ trình duyệt (khi xoay màn hình hoặc đổi kích thước trên desktop/dev tools)
    window.addEventListener('resize', () => {
        if (st.open) {
            if (window.innerWidth < 768) {
                document.body.classList.add('rmc-open');
            } else {
                document.body.classList.remove('rmc-open');
            }
            updateMobileViewport();
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
            if (!ok || !data.success) {
                if (window.showNotifyBox) {
                    window.showNotifyBox({ title: T.cannotOpenTitle, message: data.message || T.cannotOpenMsg, type: 'warning' });
                } else {
                    alert(data.message || T.cannotOpenMsg);
                }
                return;
            }
            setOpen(true);
            await showThread(data.data);
            if (btn.dataset.prefill && !input.value) input.value = btn.dataset.prefill;
            if (window.innerWidth >= 768) {
                input.focus();
            }
        } catch (err) {
            if (window.showNotifyBox) {
                window.showNotifyBox({ title: T.errorTitle, message: err.message || T.connectError, type: 'error' });
            } else {
                alert(err.message);
            }
        } finally {
            btn.disabled = false;
        }
    });

    connect();
    refreshUnread();
})();