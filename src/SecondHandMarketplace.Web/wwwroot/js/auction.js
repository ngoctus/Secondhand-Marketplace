// Đấu giá: đếm ngược + SignalR (polling chỉ là dự phòng).
// Trang chi tiết: <div id="auction" data-id data-status data-status-url data-bid-url data-start data-end data-now data-min data-step>
// Trang danh sách: các thẻ <span class="js-countdown" data-end="ISO">.
(() => {
    const fmtTime = (ms) => {
        if (ms <= 0) return '00:00:00';
        const s = Math.floor(ms / 1000), d = Math.floor(s / 86400);
        const h = String(Math.floor((s % 86400) / 3600)).padStart(2, '0');
        const m = String(Math.floor((s % 3600) / 60)).padStart(2, '0');
        const sec = String(s % 60).padStart(2, '0');
        return (d > 0 ? d + ' ngày ' : '') + `${h}:${m}:${sec}`;
    };
    const fmtMoney = (n) => Number(n).toLocaleString('vi-VN') + 'đ';

    // Đếm ngược ở trang danh sách
    document.querySelectorAll('.js-countdown').forEach((el) => {
        const end = new Date(el.dataset.end).getTime();
        const tick = () => { el.textContent = 'Còn ' + fmtTime(end - Date.now()); };
        tick();
        setInterval(tick, 1000);
    });

    const root = document.getElementById('auction');
    if (!root) return;

    const $ = (id) => document.getElementById(id);
    const st = {
        status: root.dataset.status,
        start: new Date(root.dataset.start).getTime(),
        end: new Date(root.dataset.end).getTime(),
        min: Number(root.dataset.min),
        step: Number(root.dataset.step),
        offset: new Date(root.dataset.now).getTime() - Date.now(), // lệch giữa giờ server và máy khách
        timer: null
    };
    const now = () => Date.now() + st.offset;
    const isOpen = () => ['RUNNING', 'SCHEDULED', 'ENDING'].includes(st.status);

    function renderCountdown() {
        const box = $('countdown'), label = $('countdown-label');
        if (!box) return;
        if (!isOpen()) { label.textContent = 'Phiên đã'; box.textContent = st.status === 'CANCELLED' ? 'BỊ HỦY' : 'KẾT THÚC'; return; }
        if (now() < st.start) { label.textContent = 'Bắt đầu sau'; box.textContent = fmtTime(st.start - now()); return; }
        const left = st.end - now();
        label.textContent = left > 0 ? 'Kết thúc sau' : 'Đang chốt kết quả...';
        box.textContent = fmtTime(left);
        box.classList.toggle('urgent', left > 0 && left < 5 * 60 * 1000);
    }

    function renderBids(bids) {
        const body = $('bid-list'), empty = $('bid-empty');
        if (!body) return;
        body.replaceChildren(...bids.map((b) => {
            const tr = document.createElement('tr');
            if (b.mine) tr.className = 'mine';
            [b.bidder, b.amountText, b.time].forEach((t, i) => {
                const td = document.createElement('td');
                if (i === 1) { const bold = document.createElement('b'); bold.textContent = t; td.appendChild(bold); }
                else td.textContent = t;
                tr.appendChild(td);
            });
            return tr;
        }));
        if (empty) empty.hidden = bids.length > 0;
    }

    function apply(d, neutral = false) {
        const oldPrice = $('current-price').textContent;
        st.status = d.status;
        st.min = d.minNextBid;
        st.end = new Date(d.endAtUtc).getTime();
        st.start = new Date(d.startAtUtc).getTime();
        st.offset = new Date(d.serverNowUtc).getTime() - Date.now();

        $('current-price').textContent = d.currentPriceText;
        if (oldPrice !== d.currentPriceText) {
            $('current-price').classList.remove('flash'); void $('current-price').offsetWidth; $('current-price').classList.add('flash');
        }
        $('bid-count').textContent = d.bidCount;
        if ($('min-bid')) $('min-bid').textContent = d.minNextBidText;
        const badge = $('status-badge');
        badge.textContent = d.statusText;
        badge.className = 'st st-' + d.status.toLowerCase();
        if (!neutral) {   // payload từ hub không có dữ liệu riêng của người xem
            $('leader-badge').hidden = !(d.isLeader && isOpen());
            renderBids(d.bids);
        }

        // Phiên kết thúc hoặc bị hủy: tải lại trang để hiện người thắng / lý do
        if (!isOpen()) { clearInterval(st.timer); setTimeout(() => location.reload(), 800); }
    }

    async function poll() {
        if (document.hidden) return;
        try {
            const res = await fetch(root.dataset.statusUrl, { headers: { Accept: 'application/json' }, cache: 'no-store' });
            if (res.ok) apply(await res.json());
        } catch { /* mất mạng tạm thời: lần poll sau tự thử lại */ }
    }

    renderCountdown();
    setInterval(renderCountdown, 1000);
    document.addEventListener('visibilitychange', () => { if (!document.hidden) poll(); });

    // ----- SignalR: server đẩy cập nhật; polling chậm lại làm dự phòng -----
    const FAST = 3000, SLOW = 20000;
    const setPolling = (ms) => { clearInterval(st.timer); st.timer = setInterval(poll, ms); };
    let pollSoon = null;
    const pollDebounced = () => { clearTimeout(pollSoon); pollSoon = setTimeout(poll, 250); };

    const setLive = (state) => {
        const dot = $('live-dot');
        if (!dot) return;
        dot.dataset.state = state;
        dot.textContent = state === 'on' ? '● Trực tiếp' : state === 'connecting' ? '● Đang kết nối...' : '● Mất kết nối';
    };

    function startHub() {
        if (typeof signalR === 'undefined' || !isOpen() || !root.dataset.hubUrl) { setPolling(FAST); return; }

        const conn = new signalR.HubConnectionBuilder()
            .withUrl(root.dataset.hubUrl)
            .withAutomaticReconnect([0, 1000, 3000, 5000, 10000])
            .build();

        // Sau khi reconnect connectionId đổi nên phải JoinAuction lại, nếu không sẽ không nhận được tin nữa.
        const join = async () => {
            await conn.invoke('JoinAuction', Number(root.dataset.id));
            setLive('on');
            setPolling(SLOW);
            poll();
        };

        conn.on('ReceiveBidUpdate', (d) => { apply(d, true); pollDebounced(); });
        conn.on('ViewerCount', (n) => { const el = $('viewer-count'); if (el) el.textContent = n; });
        conn.onreconnecting(() => { setLive('connecting'); setPolling(FAST); });
        conn.onreconnected(() => join().catch(() => setLive('off')));
        conn.onclose(() => { setLive('off'); setPolling(FAST); });

        setLive('connecting');
        setPolling(FAST);   // chạy polling nhanh cho tới khi hub kết nối xong
        conn.start().then(join).catch(() => { setLive('off'); setPolling(FAST); });
    }
    startHub();

    // ----- Đặt giá -----
    const form = $('bid-form');
    if (!form) return;
    const input = $('bid-amount'), msg = $('bid-msg'), submit = $('bid-submit');
    const digits = (s) => (s || '').replace(/\D/g, '');

    input.addEventListener('input', () => {
        const v = digits(input.value);
        input.value = v ? Number(v).toLocaleString('vi-VN') : '';
    });
    form.querySelectorAll('.quick-bids button').forEach((btn) => btn.addEventListener('click', () => {
        input.value = (st.min + Number(btn.dataset.k) * st.step).toLocaleString('vi-VN');
        input.focus();
    }));

    const say = (text, ok) => { msg.textContent = text; msg.className = 'bid-msg ' + (ok ? 'ok' : 'err'); };

    form.addEventListener('submit', async (e) => {
        e.preventDefault();
        const amount = Number(digits(input.value));
        if (!amount) return say('Vui lòng nhập số tiền hợp lệ.', false);
        if (amount < st.min) return say('Giá đặt tối thiểu là ' + fmtMoney(st.min) + '.', false);
        if (!confirm('Xác nhận đặt giá ' + fmtMoney(amount) + '?')) return;

        submit.disabled = true;
        try {
            const res = await fetch(root.dataset.bidUrl, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded',
                    'RequestVerificationToken': form.querySelector('input[name="__RequestVerificationToken"]').value,
                    Accept: 'application/json'
                },
                body: new URLSearchParams({ soTien: String(amount) })
            });
            // Hết phiên đăng nhập: server chuyển hướng về trang login (trả HTML)
            if (res.redirected || !(res.headers.get('content-type') || '').includes('json')) {
                location.href = '/Account/Login?returnUrl=' + encodeURIComponent(location.pathname);
                return;
            }
            const json = await res.json();
            say(json.message, res.ok && json.success);
            if (res.ok && json.success) { input.value = ''; apply(json.data); }
        } catch {
            say('Không kết nối được máy chủ, vui lòng thử lại.', false);
        } finally {
            submit.disabled = false;
        }
    });
})();