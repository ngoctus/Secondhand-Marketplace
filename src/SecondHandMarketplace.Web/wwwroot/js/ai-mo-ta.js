(() => {
    const script = document.querySelector('script[data-ai-mota-chat-url]');
    const container = document.getElementById('aiGoiYMota');
    if (!script || !container) return;

    const chatUrl = script.dataset.aiMotaChatUrl;
    const statusUrl = script.dataset.aiMotaStatusUrl;

    const form = container.closest('form');
    const csrfToken = form && form.querySelector('input[name="__RequestVerificationToken"]')
        ? form.querySelector('input[name="__RequestVerificationToken"]').value
        : '';
    const textarea = form && form.querySelector('#MoTa');
    const inputTen = form && form.querySelector('#Ten');
    const selDanhMuc = form && form.querySelector('#MaDanhMuc');
    const selTinhTrang = form && form.querySelector('#MaTinhTrang');
    const inputGia = form && form.querySelector('#Gia');
    if (!textarea || !inputTen || !selDanhMuc || !selTinhTrang) return;

    const btnGoi = container.querySelector('[data-ai-mota-goi]');
    const TrangThai = container.querySelector('[data-ai-mota-trang-thai]');
    const XemTruoc = container.querySelector('[data-ai-mota-xem-truoc]');
    const NoiDung = container.querySelector('[data-ai-mota-noi-dung]');
    const btnDien = container.querySelector('[data-ai-mota-dien]');
    const btnBoQua = container.querySelector('[data-ai-mota-bo-qua]');

    let dangGoi = false;

    const baiBoCuc = (lyDo) => {
        container.hidden = true;
        if (lyDo) TrangThai.textContent = lyDo;
    };

    const tenCua = (select) => (select.selectedIndex > 0 ? select.options[select.selectedIndex].text.trim() : '');

    const soSanh = () => {
        const thieu = [];
        if (inputTen.value.trim().length < 5) thieu.push('tên sản phẩm (từ 5 ký tự)');
        if (!tenCua(selDanhMuc)) thieu.push('danh mục');
        if (!tenCua(selTinhTrang)) thieu.push('tình trạng');
        return thieu;
    };

    const soanCauHoi = () => {
        const gia = inputGia && inputGia.value ? `\n- Giá đang định bán: ${inputGia.value} đồng` : '';
        return 'Hãy soạn mô tả đăng bán cho sản phẩm cũ này bằng công cụ draft_product_description'
            + ' với đúng các thông tin dưới đây, không tự thêm dữ kiện khác:'
            + `\n- Tên: ${inputTen.value.trim().slice(0, 150)}`
            + `\n- Danh mục: ${tenCua(selDanhMuc)}`
            + `\n- Tình trạng: ${tenCua(selTinhTrang)}`
            + gia
            + '\nViết 3-5 câu tiếng Việt, đủ các ý: tình trạng thực tế, thời gian đã dùng, phụ kiện đi kèm,'
            + ' lỗi/hư hại còn lại, lý do bán và cách giao hàng.'
            + '\nÝ nào chưa có thông tin thì nhắc người bán bổ sung thay vì bịa.'
            + '\nTrả lời chỉ bằng nội dung mô tả, không lời dẫn đầu hay cuối.';
    };

    const hienLoi = (thongBao) => {
        TrangThai.textContent = thongBao;
        XemTruoc.hidden = true;
    };

    const goiAI = async () => {
        if (dangGoi) return;

        const thieu = soSanh();
        if (thieu.length > 0) {
            hienLoi(`Cần có ${thieu.join(', ')} trước khi nhờ AI gợi ý.`);
            return;
        }

        dangGoi = true;
        btnGoi.disabled = true;
        TrangThai.textContent = 'Đang soạn mô tả...';

        try {
            const resp = await fetch(chatUrl, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': csrfToken },
                body: JSON.stringify({ message: soanCauHoi() })
            });
            const ketQua = await resp.json().catch(() => null);

            if (resp.status === 503 && ketQua && ketQua.data && ketQua.data.disabled) {
                baiBoCuc();
                return;
            }
            if (!resp.ok) {
                if (resp.status === 429 && ketQua && ketQua.data && ketQua.data.retryAfterSeconds) {
                    hienLoi(`${ketQua.message || 'Bạn gửi hơi nhanh.'} Thử lại sau ${ketQua.data.retryAfterSeconds} giây.`);
                } else {
                    hienLoi(ketQua && ketQua.message ? ketQua.message : 'AI tạm thời không phản hồi, bạn thử lại sau nhé.');
                }
                return;
            }

            const traLoi = ketQua && ketQua.data ? (ketQua.data.reply || '').trim() : '';
            if (!traLoi) {
                hienLoi('AI chưa trả về nội dung nào, bạn thử lại lần nữa nhé.');
                return;
            }

            NoiDung.textContent = traLoi;
            XemTruoc.hidden = false;
            TrangThai.textContent = '';
        } catch (e) {
            hienLoi('Không kết nối được tới trợ lý AI, kiểm tra mạng rồi thử lại.');
        } finally {
            dangGoi = false;
            btnGoi.disabled = false;
        }
    };

    btnGoi.addEventListener('click', goiAI);

    btnBoQua.addEventListener('click', () => {
        XemTruoc.hidden = true;
        TrangThai.textContent = '';
    });

    btnDien.addEventListener('click', () => {
        const noiDungMoi = NoiDung.textContent.trim();
        if (!noiDungMoi) return;
        if (textarea.value.trim().length > 0 && !confirm('Đè lên mô tả đang có bằng bản nháp của AI?')) return;

        textarea.value = noiDungMoi;
        textarea.dispatchEvent(new Event('input', { bubbles: true }));
        XemTruoc.hidden = true;
        TrangThai.textContent = 'Đã điền bản nháp. Bạn bổ sung thông tin còn thiếu rồi hãy gửi duyệt.';
    });

    if (!statusUrl) {
        btnGoi.hidden = false;
        return;
    }

    fetch(statusUrl, { headers: { 'RequestVerificationToken': csrfToken } })
        .then((r) => (r.ok ? r.json() : null))
        .then((k) => {
            if (k && k.data && k.data.enabled) btnGoi.hidden = false;
            else baiBoCuc();
        })
        .catch(() => baiBoCuc());
})();
