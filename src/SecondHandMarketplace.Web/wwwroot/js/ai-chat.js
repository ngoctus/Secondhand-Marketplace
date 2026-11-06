// Widget Chatbox AI của Re:market. Tự dựng giao diện bằng DOM API (textContent, không innerHTML) nên nội dung
// từ AI hay từ người dùng không thể chèn HTML/script. Không lưu lịch sử hội thoại ở đâu cả.
// Được nhúng bởi Views/Shared/_AiChatWidget.cshtml (kèm data-chat-url và data-status-url).
(() => {
    'use strict';

    const script = document.querySelector('script[data-chat-url]');
    if (!script || document.getElementById('rmchat')) return;

    const CHAT_URL = script.dataset.chatUrl;
    const STATUS_URL = script.dataset.statusUrl;
    const CSRF_TOKEN = script.dataset.csrfToken || '';   // Antiforgery token do _AiChatWidget.cshtml cấp cho phiên Cookie
    const MAX_LENGTH = 1000;            // khớp AiChatService.MessageMaxLength
    const CLIENT_TIMEOUT_MS = 90000;    // lớn hơn thời gian chờ hàng đợi (30 giây) + xử lý (50 giây) phía máy chủ
    const MAX_ERROR_LENGTH = 300;
    const SITE_BASE = CHAT_URL.replace(/api\/ai\/chat\/?$/i, '');
    const PRODUCT_PATH = /\/Products\/Details\/(\d{1,9})/g;

    const el = (tag, className, text) => {
        const node = document.createElement(tag);
        if (className) node.className = className;
        if (text !== undefined) node.textContent = text;
        return node;
    };

    // ---- Dựng giao diện ---------------------------------------------------------------------------------------
    const root = el('div', 'rmchat');
    root.id = 'rmchat';

    const toggle = el('button', 'rmchat-toggle');
    toggle.type = 'button';
    toggle.setAttribute('aria-expanded', 'false');
    toggle.setAttribute('aria-controls', 'rmchat-panel');
    toggle.append(el('span', 'rmchat-toggle-icon', '✦'), el('span', 'rmchat-toggle-label', 'Trợ lý AI'));

    const panel = el('section', 'rmchat-panel');
    panel.id = 'rmchat-panel';
    panel.hidden = true;
    panel.setAttribute('role', 'dialog');
    panel.setAttribute('aria-label', 'Trợ lý AI Re:market');

    const header = el('div', 'rmchat-header');
    const heading = el('div', 'rmchat-heading');
    heading.append(el('strong', 'rmchat-title', 'Trợ lý AI'), el('span', 'rmchat-subtitle', 'Hỏi về sản phẩm trên Re:market'));
    const closeButton = el('button', 'rmchat-close', '×');
    closeButton.type = 'button';
    closeButton.setAttribute('aria-label', 'Đóng trợ lý AI');
    header.append(heading, closeButton);

    const log = el('div', 'rmchat-log');
    log.setAttribute('role', 'log');
    log.setAttribute('aria-live', 'polite');

    const composer = el('div', 'rmchat-composer');
    const input = el('textarea', 'rmchat-input');
    input.rows = 1;
    input.maxLength = MAX_LENGTH;
    input.placeholder = 'Nhập câu hỏi của bạn…';
    input.setAttribute('aria-label', 'Nội dung tin nhắn');
    const sendButton = el('button', 'rmchat-send', 'Gửi');
    sendButton.type = 'button';
    composer.append(input, sendButton);

    const note = el('p', 'rmchat-note', 'Trợ lý AI có thể nhầm. Hãy kiểm tra lại thông tin trước khi mua.');

    panel.append(header, log, composer, note);
    root.append(panel, toggle);

    // ---- Hiển thị tin nhắn ------------------------------------------------------------------------------------
    // Chỉ biến đường dẫn sản phẩm nội bộ (/Products/Details/<số>) thành liên kết; mọi thứ khác là văn bản thuần.
    const appendRichText = (container, text) => {
        let last = 0;
        for (const match of text.matchAll(PRODUCT_PATH)) {
            if (match.index > last) container.append(document.createTextNode(text.slice(last, match.index)));
            const link = el('a', 'rmchat-link', match[0]);
            link.href = SITE_BASE + 'Products/Details/' + match[1];
            container.append(link);
            last = match.index + match[0].length;
        }
        if (last < text.length) container.append(document.createTextNode(text.slice(last)));
    };

    const scrollToEnd = () => { log.scrollTop = log.scrollHeight; };

    const addMessage = (kind, text) => {
        const row = el('div', 'rmchat-msg rmchat-msg-' + kind);
        const bubble = el('div', 'rmchat-bubble');
        if (kind === 'user') bubble.textContent = text;
        else appendRichText(bubble, text);
        row.append(bubble);
        log.append(row);
        scrollToEnd();
        return row;
    };

    let typingRow = null;
    const showTyping = () => {
        typingRow = el('div', 'rmchat-msg rmchat-msg-bot rmchat-typing');
        typingRow.setAttribute('role', 'status');
        const bubble = el('div', 'rmchat-bubble');
        bubble.append(el('span', 'rmchat-typing-text', 'Đang xử lý'), el('span', 'rmchat-dot'), el('span', 'rmchat-dot'), el('span', 'rmchat-dot'));
        typingRow.append(bubble);
        log.append(typingRow);
        scrollToEnd();
    };
    const hideTyping = () => {
        if (typingRow) typingRow.remove();
        typingRow = null;
    };

    // ---- Trạng thái -------------------------------------------------------------------------------------------
    let busy = false;
    let locked = false;

    const setBusy = (value) => {
        busy = value;
        sendButton.disabled = value || locked;
        input.disabled = value || locked;
        if (value) showTyping(); else hideTyping();
    };

    const lock = (message) => {
        locked = true;
        sendButton.disabled = true;
        input.disabled = true;
        addMessage('error', message);
    };

    const autosize = () => {
        input.style.height = 'auto';
        input.style.height = Math.min(input.scrollHeight, 110) + 'px';
    };

    const setOpen = (open, restoreFocus) => {
        panel.hidden = !open;
        toggle.setAttribute('aria-expanded', String(open));
        toggle.classList.toggle('is-open', open);
        if (open) {
            if (log.children.length === 0) {
                addMessage('bot', 'Xin chào! Mình có thể giúp bạn tìm sản phẩm, gợi ý theo nhu cầu và ngân sách, hoặc viết mô tả bán hàng. Bạn cần gì nào?');
            }
            if (!input.disabled) input.focus();
        } else if (restoreFocus) {
            toggle.focus();
        }
    };

    const readJson = async (response) => {
        try { return await response.json(); } catch { return null; }
    };

    const safeServerMessage = (body) =>
        body && typeof body.message === 'string' && body.message.trim()
            ? body.message.trim().slice(0, MAX_ERROR_LENGTH)
            : null;

    const handleFailure = (response, body) => {
        const serverMessage = safeServerMessage(body);
        if (response.status === 401) {
            addMessage('error', 'Bạn cần đăng nhập để dùng trợ lý AI.');
        } else if (response.status === 503 && body && body.data && body.data.disabled === true) {
            lock(serverMessage || 'Trợ lý AI đang tạm tắt.');
        } else {
            addMessage('error', serverMessage || 'Đã xảy ra lỗi. Vui lòng thử lại sau.');
        }
    };

    const send = async () => {
        if (busy || locked) return;

        const text = input.value.trim();
        if (!text) { input.focus(); return; }
        if (text.length > MAX_LENGTH) {
            addMessage('error', 'Tin nhắn tối đa ' + MAX_LENGTH + ' ký tự.');
            return;
        }

        addMessage('user', text);
        input.value = '';
        autosize();
        setBusy(true);

        const controller = new AbortController();
        const timer = setTimeout(() => controller.abort(), CLIENT_TIMEOUT_MS);
        try {
            const response = await fetch(CHAT_URL, {
                method: 'POST',
                credentials: 'same-origin',
                headers: Object.assign(
                    { 'Content-Type': 'application/json', 'Accept': 'application/json' },
                    CSRF_TOKEN ? { 'RequestVerificationToken': CSRF_TOKEN } : {}),
                body: JSON.stringify({ message: text }),
                signal: controller.signal
            });
            const body = await readJson(response);
            if (response.ok && body && body.success === true && body.data && typeof body.data.reply === 'string') {
                addMessage('bot', body.data.reply);
            } else {
                handleFailure(response, body);
            }
        } catch (error) {
            addMessage('error', error && error.name === 'AbortError'
                ? 'Trợ lý phản hồi quá lâu. Bạn thử lại nhé.'
                : 'Không kết nối được máy chủ. Vui lòng kiểm tra mạng và thử lại.');
        } finally {
            clearTimeout(timer);
            setBusy(false);
            if (!panel.hidden && !locked) input.focus();
        }
    };

    // Admin có thể tắt Chatbox: nếu máy chủ báo đã tắt thì ẩn hẳn widget. Lỗi khác (mạng, chưa xác thực...) thì giữ nguyên
    // để người dùng thấy lỗi cụ thể khi gửi tin nhắn.
    const checkStatus = async () => {
        if (!STATUS_URL) return;
        try {
            const response = await fetch(STATUS_URL, { credentials: 'same-origin', headers: { 'Accept': 'application/json' } });
            if (!response.ok) return;
            const body = await readJson(response);
            if (body && body.data && body.data.enabled === false) root.hidden = true;
        } catch {
            // Bỏ qua: không kiểm tra được trạng thái thì vẫn hiển thị.
        }
    };

    // ---- Sự kiện ----------------------------------------------------------------------------------------------
    toggle.addEventListener('click', () => setOpen(panel.hidden, false));
    closeButton.addEventListener('click', () => setOpen(false, true));
    sendButton.addEventListener('click', send);
    input.addEventListener('input', autosize);
    input.addEventListener('keydown', (event) => {
        // isComposing: không gửi khi đang gõ dở bằng bộ gõ tiếng Việt (Telex/VNI).
        if (event.key === 'Enter' && !event.shiftKey && !event.isComposing) {
            event.preventDefault();
            send();
        }
    });
    // Gắn ở document (không phải panel): khi ô nhập bị khóa thì focus rơi ra ngoài panel nhưng Esc vẫn phải đóng được.
    document.addEventListener('keydown', (event) => {
        if (event.key === 'Escape' && !panel.hidden) setOpen(false, true);
    });

    document.body.append(root);
    checkStatus();
})();
