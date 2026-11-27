(() => {
    'use strict';

    const script = document.querySelector('script[data-ai-mota-url], script[data-ai-mota-chat-url]');
    const motaBox = document.getElementById('aiGoiYMota');
    const giaBox = document.getElementById('aiGoiYGia');
    if (!script || (!motaBox && !giaBox)) return;

    const draftUrl = script.dataset.aiMotaUrl || script.dataset.aiMotaChatUrl || '';
    const priceUrl = script.dataset.aiGiaUrl || '';
    const statusUrl = script.dataset.aiMotaStatusUrl;
    const lang = script.dataset.lang === 'en' ? 'en' : 'vi';
    const isEn = lang === 'en';

    const form = (motaBox || giaBox).closest('form');
    const csrfToken = form && form.querySelector('input[name="__RequestVerificationToken"]')
        ? form.querySelector('input[name="__RequestVerificationToken"]').value
        : '';
    const textarea = form && form.querySelector('#MoTa');
    const inputTen = form && form.querySelector('#Ten');
    const selDanhMuc = form && form.querySelector('#MaDanhMuc');
    const selTinhTrang = form && form.querySelector('#MaTinhTrang');
    const inputGia = form && form.querySelector('#Gia');
    if (!inputTen || !selDanhMuc || !selTinhTrang) return;

    const T = isEn
        ? {
            fieldName: 'product name (at least 5 characters)',
            fieldCategory: 'category',
            fieldCondition: 'condition',
            missingPrefix: 'Please provide ',
            missingDescSuffix: ' before requesting an AI description.',
            missingPriceSuffix: ' before requesting an AI price suggestion.',
            drafting: 'Drafting description…',
            pricing: 'Analyzing marketplace prices…',
            rateFast: 'You are sending requests too quickly.',
            retryAfter: (s) => `Try again in ${s}s.`,
            aiUnavailable: 'AI is temporarily unavailable. Please try again later.',
            emptyReply: 'AI returned no content. Please try again.',
            networkError: 'Could not connect to the AI assistant. Check your connection and try again.',
            confirmOverwriteDesc: 'Replace the existing description with the AI draft?',
            descApplied: 'Draft applied. Please fill in any missing details before submitting for review.',
            priceApplied: (p) => `Applied suggested price (${p}). You can still adjust it before saving.`,
            rangeLabel: (min, max) => `Marketplace range: ${min} – ${max}`,
            medianLabel: (med) => ` · Median: ${med}`,
            suggestedLabel: (rec) => `Suggested: ${rec}`,
            generalEstimateLabel: (rec) => `General AI estimate: ${rec}`,
            noMarketData: 'No comparable listings on Re:market yet',
            samplesTitle: 'Comparable listings on Re:market:',
            fallbackNote: ' (from marketplace stats)',
            applyBtnWithPrice: (p) => `Apply ${p}`,
            applyBtnDefault: 'Apply suggested price'
        }
        : {
            fieldName: 'tên sản phẩm (từ 5 ký tự)',
            fieldCategory: 'danh mục',
            fieldCondition: 'tình trạng',
            missingPrefix: 'Cần có ',
            missingDescSuffix: ' trước khi nhờ AI gợi ý mô tả.',
            missingPriceSuffix: ' trước khi nhờ AI tham khảo giá.',
            drafting: 'Đang soạn mô tả…',
            pricing: 'Đang tham khảo giá trên sàn…',
            rateFast: 'Bạn gửi hơi nhanh.',
            retryAfter: (s) => `Thử lại sau ${s} giây.`,
            aiUnavailable: 'AI tạm thời không phản hồi, bạn thử lại sau nhé.',
            emptyReply: 'AI chưa trả về nội dung nào, bạn thử lại lần nữa nhé.',
            networkError: 'Không kết nối được tới trợ lý AI, kiểm tra mạng rồi thử lại.',
            confirmOverwriteDesc: 'Đè lên mô tả đang có bằng bản nháp của AI?',
            descApplied: 'Đã điền bản nháp. Bạn bổ sung thông tin còn thiếu rồi hãy gửi duyệt.',
            priceApplied: (p) => `Đã điền giá gợi ý (${p}). Bạn có thể chỉnh lại trước khi lưu.`,
            rangeLabel: (min, max) => `Khoảng giá trên sàn: ${min} – ${max}`,
            medianLabel: (med) => ` · Trung vị: ${med}`,
            suggestedLabel: (rec) => `Đề xuất: ${rec}`,
            generalEstimateLabel: (rec) => `Ước lượng tham khảo từ AI: ${rec}`,
            noMarketData: 'Chưa có tin tương tự trên Re:market',
            samplesTitle: 'Tin đang bán để đối chiếu:',
            fallbackNote: ' (từ thống kê dữ liệu sàn)',
            applyBtnWithPrice: (p) => `Áp dụng ${p}`,
            applyBtnDefault: 'Áp dụng giá gợi ý'
        };

    const tenCua = (select) => (select && select.selectedIndex > 0 ? select.options[select.selectedIndex].text.trim() : '');
    const idCua = (select) => {
        if (!select || !select.value) return null;
        const n = Number.parseInt(select.value, 10);
        return Number.isFinite(n) && n > 0 ? n : null;
    };
    const formatVnd = (value) => {
        const num = Number(value);
        if (!Number.isFinite(num)) return '';
        const formatted = Math.round(num).toLocaleString(isEn ? 'en-US' : 'vi-VN');
        return isEn ? `${formatted} VND` : `${formatted}đ`;
    };

    const hideAllBoxes = () => {
        if (motaBox) motaBox.hidden = true;
        if (giaBox) giaBox.hidden = true;
    };

    // ------------------------------------------------------------------ 1. AI Gợi ý mô tả (VI | EN)

    let btnMota = null;
    if (motaBox && textarea && draftUrl) {
        btnMota = motaBox.querySelector('[data-ai-mota-goi]');
        const trangThai = motaBox.querySelector('[data-ai-mota-trang-thai]');
        const xemTruoc = motaBox.querySelector('[data-ai-mota-xem-truoc]');
        const noiDung = motaBox.querySelector('[data-ai-mota-noi-dung]');
        const btnDien = motaBox.querySelector('[data-ai-mota-dien]');
        const btnBoQua = motaBox.querySelector('[data-ai-mota-bo-qua]');

        let dangGoiMota = false;

        const soSanhMota = () => {
            const thieu = [];
            if (inputTen.value.trim().length < 5) thieu.push(T.fieldName);
            if (!tenCua(selDanhMuc)) thieu.push(T.fieldCategory);
            if (!tenCua(selTinhTrang)) thieu.push(T.fieldCondition);
            return thieu;
        };

        const hienLoiMota = (msg) => {
            if (trangThai) trangThai.textContent = msg;
            if (xemTruoc) xemTruoc.hidden = true;
        };

        const parsePrice = () => {
            if (!inputGia || !inputGia.value) return null;
            const n = Number(inputGia.value);
            return Number.isFinite(n) && n >= 1000 ? n : null;
        };

        const goiAiMota = async () => {
            if (dangGoiMota) return;

            const thieu = soSanhMota();
            if (thieu.length > 0) {
                hienLoiMota(`${T.missingPrefix}${thieu.join(', ')}${T.missingDescSuffix}`);
                return;
            }

            dangGoiMota = true;
            if (btnMota) btnMota.disabled = true;
            if (trangThai) trangThai.textContent = T.drafting;

            try {
                const payload = {
                    name: inputTen.value.trim().slice(0, 200),
                    categoryId: idCua(selDanhMuc),
                    conditionId: idCua(selTinhTrang),
                    categoryName: tenCua(selDanhMuc) || null,
                    conditionName: tenCua(selTinhTrang) || null,
                    price: parsePrice(),
                    currentDescription: textarea.value.trim().slice(0, 1500) || null,
                    language: lang
                };

                const resp = await fetch(draftUrl, {
                    method: 'POST',
                    credentials: 'same-origin',
                    headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': csrfToken },
                    body: JSON.stringify(payload)
                });
                const ketQua = await resp.json().catch(() => null);

                if (resp.status === 503 && ketQua && ketQua.data && ketQua.data.disabled) {
                    hideAllBoxes();
                    return;
                }
                if (!resp.ok) {
                    if (resp.status === 429 && ketQua && ketQua.data && ketQua.data.retryAfterSeconds) {
                        hienLoiMota(`${ketQua.message || T.rateFast} ${T.retryAfter(ketQua.data.retryAfterSeconds)}`);
                    } else {
                        hienLoiMota(ketQua && ketQua.message ? ketQua.message : T.aiUnavailable);
                    }
                    return;
                }

                const traLoi = ketQua && ketQua.data ? (ketQua.data.draft || ketQua.data.reply || '').trim() : '';
                if (!traLoi) {
                    hienLoiMota(T.emptyReply);
                    return;
                }

                if (noiDung) noiDung.textContent = traLoi;
                if (xemTruoc) xemTruoc.hidden = false;
                if (trangThai) trangThai.textContent = '';
            } catch {
                hienLoiMota(T.networkError);
            } finally {
                dangGoiMota = false;
                if (btnMota) btnMota.disabled = false;
            }
        };

        if (btnMota) btnMota.addEventListener('click', goiAiMota);

        if (btnBoQua) {
            btnBoQua.addEventListener('click', () => {
                if (xemTruoc) xemTruoc.hidden = true;
                if (trangThai) trangThai.textContent = '';
            });
        }

        if (btnDien) {
            btnDien.addEventListener('click', () => {
                const noiDungMoi = noiDung ? noiDung.textContent.trim() : '';
                if (!noiDungMoi) return;
                if (textarea.value.trim().length > 0 && !confirm(T.confirmOverwriteDesc)) return;

                textarea.value = noiDungMoi;
                textarea.dispatchEvent(new Event('input', { bubbles: true }));
                if (xemTruoc) xemTruoc.hidden = true;
                if (trangThai) trangThai.textContent = T.descApplied;
            });
        }
    }

    // ------------------------------------------------------------------ 2. AI Tham khảo khoảng giá bán (C-03)

    let btnGia = null;
    if (giaBox && inputGia && priceUrl) {
        btnGia = giaBox.querySelector('[data-ai-gia-goi]');
        const giaTrangThai = giaBox.querySelector('[data-ai-gia-trang-thai]');
        const giaXemTruoc = giaBox.querySelector('[data-ai-gia-xem-truoc]');
        const giaKhoang = giaBox.querySelector('[data-ai-gia-khoang]');
        const giaNoiDung = giaBox.querySelector('[data-ai-gia-noi-dung]');
        const giaMau = giaBox.querySelector('[data-ai-gia-mau]');
        const btnApDungGia = giaBox.querySelector('[data-ai-gia-ap-dung]');
        const btnBoQuaGia = giaBox.querySelector('[data-ai-gia-bo-qua]');

        let dangGoiGia = false;
        let recommendedPrice = null;

        const hienLoiGia = (msg) => {
            if (giaTrangThai) giaTrangThai.textContent = msg;
            if (giaXemTruoc) giaXemTruoc.hidden = true;
        };

        const renderSamples = (samples) => {
            if (!giaMau) return;
            giaMau.replaceChildren();
            if (!Array.isArray(samples) || samples.length === 0) {
                giaMau.hidden = true;
                return;
            }

            const title = document.createElement('div');
            title.className = 'text-muted mb-1';
            title.textContent = T.samplesTitle;
            giaMau.append(title);

            const list = document.createElement('ul');
            list.className = 'mb-0 ps-3';
            const safeUrlPattern = /^\/Products\/Details\/\d{1,9}$/;

            for (const item of samples) {
                const li = document.createElement('li');
                const priceStr = formatVnd(item.priceVnd);
                if (typeof item.url === 'string' && safeUrlPattern.test(item.url)) {
                    const a = document.createElement('a');
                    a.href = item.url;
                    a.target = '_blank';
                    a.rel = 'noopener noreferrer';
                    a.textContent = item.name || item.url;
                    li.append(a);
                } else {
                    li.append(document.createTextNode(item.name || ''));
                }
                const suffix = item.condition ? ` — ${priceStr} (${item.condition})` : ` — ${priceStr}`;
                li.append(document.createTextNode(suffix));
                list.append(li);
            }

            giaMau.append(list);
            giaMau.hidden = false;
        };

        const goiAiGia = async () => {
            if (dangGoiGia) return;

            const thieu = [];
            if (inputTen.value.trim().length < 5) thieu.push(T.fieldName);
            if (!tenCua(selDanhMuc)) thieu.push(T.fieldCategory);
            if (thieu.length > 0) {
                hienLoiGia(`${T.missingPrefix}${thieu.join(', ')}${T.missingPriceSuffix}`);
                return;
            }

            dangGoiGia = true;
            if (btnGia) btnGia.disabled = true;
            if (giaTrangThai) giaTrangThai.textContent = T.pricing;

            try {
                const payload = {
                    name: inputTen.value.trim().slice(0, 200),
                    categoryId: idCua(selDanhMuc),
                    conditionId: idCua(selTinhTrang),
                    categoryName: tenCua(selDanhMuc) || null,
                    conditionName: tenCua(selTinhTrang) || null,
                    description: textarea && textarea.value ? textarea.value.trim().slice(0, 500) : null,
                    language: lang
                };

                const resp = await fetch(priceUrl, {
                    method: 'POST',
                    credentials: 'same-origin',
                    headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': csrfToken },
                    body: JSON.stringify(payload)
                });
                const ketQua = await resp.json().catch(() => null);

                if (resp.status === 503 && ketQua && ketQua.data && ketQua.data.disabled) {
                    hideAllBoxes();
                    return;
                }
                if (!resp.ok || !ketQua || ketQua.success !== true || !ketQua.data) {
                    if (resp.status === 429 && ketQua && ketQua.data && ketQua.data.retryAfterSeconds) {
                        hienLoiGia(`${ketQua.message || T.rateFast} ${T.retryAfter(ketQua.data.retryAfterSeconds)}`);
                    } else {
                        hienLoiGia(ketQua && ketQua.message ? ketQua.message : T.aiUnavailable);
                    }
                    return;
                }

                const d = ketQua.data;
                recommendedPrice = typeof d.recommendedPriceVnd === 'number' && d.recommendedPriceVnd >= 1000
                    ? Math.round(d.recommendedPriceVnd)
                    : null;

                if (giaKhoang) {
                    const parts = [];
                    if (d.sampleCount > 0 && d.minPriceVnd != null && d.maxPriceVnd != null) {
                        let rangeText = T.rangeLabel(formatVnd(d.minPriceVnd), formatVnd(d.maxPriceVnd));
                        if (d.medianPriceVnd != null) rangeText += T.medianLabel(formatVnd(d.medianPriceVnd));
                        parts.push(rangeText);
                    } else {
                        parts.push(T.noMarketData);
                    }
                    if (recommendedPrice !== null) {
                        if (d.sampleCount === 0 && d.usedAi) {
                            parts.push(T.generalEstimateLabel(formatVnd(recommendedPrice)));
                        } else {
                            parts.push(T.suggestedLabel(formatVnd(recommendedPrice)) + (d.usedAi ? '' : T.fallbackNote));
                        }
                    }
                    giaKhoang.textContent = parts.join(' | ');
                }

                if (giaNoiDung) giaNoiDung.textContent = d.explanation || '';
                renderSamples(d.samples);

                if (btnApDungGia) {
                    if (recommendedPrice !== null) {
                        btnApDungGia.textContent = T.applyBtnWithPrice(formatVnd(recommendedPrice));
                        btnApDungGia.hidden = false;
                    } else {
                        btnApDungGia.textContent = T.applyBtnDefault;
                        btnApDungGia.hidden = true;
                    }
                }

                if (giaXemTruoc) giaXemTruoc.hidden = false;
                if (giaTrangThai) giaTrangThai.textContent = '';
            } catch {
                hienLoiGia(T.networkError);
            } finally {
                dangGoiGia = false;
                if (btnGia) btnGia.disabled = false;
            }
        };

        if (btnGia) btnGia.addEventListener('click', goiAiGia);

        if (btnBoQuaGia) {
            btnBoQuaGia.addEventListener('click', () => {
                if (giaXemTruoc) giaXemTruoc.hidden = true;
                if (giaTrangThai) giaTrangThai.textContent = '';
            });
        }

        if (btnApDungGia) {
            btnApDungGia.addEventListener('click', () => {
                if (recommendedPrice === null) return;
                inputGia.value = String(recommendedPrice);
                inputGia.dispatchEvent(new Event('input', { bubbles: true }));
                inputGia.dispatchEvent(new Event('change', { bubbles: true }));
                inputGia.focus();
                if (giaXemTruoc) giaXemTruoc.hidden = true;
                if (giaTrangThai) giaTrangThai.textContent = T.priceApplied(formatVnd(recommendedPrice));
            });
        }
    }

    // ------------------------------------------------------------------ Kiểm tra trạng thái bật/tắt AI (dùng chung)

    const revealButtons = () => {
        if (btnMota) btnMota.hidden = false;
        if (btnGia) btnGia.hidden = false;
    };

    if (!statusUrl) {
        revealButtons();
        return;
    }

    fetch(statusUrl, { credentials: 'same-origin', headers: { 'RequestVerificationToken': csrfToken } })
        .then((r) => (r.ok ? r.json() : null))
        .then((k) => {
            const sellerEnabled = k && k.data
                ? (typeof k.data.sellerAssistantEnabled === 'boolean' ? k.data.sellerAssistantEnabled : k.data.enabled)
                : false;
            if (sellerEnabled) revealButtons();
            else hideAllBoxes();
        })
        .catch(() => hideAllBoxes());
})();
