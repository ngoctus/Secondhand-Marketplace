// B-03: vùng kéo thả ảnh + xem trước khi người bán chọn file, chọn ảnh bìa bằng click/kéo,
// và gọi AJAX cho nút "Đặt làm bìa" / "Xóa" ở chế độ sửa thay vì POST cả form.
(() => {
    'use strict';

    const DinhDang = ['.jpg', '.jpeg', '.png', '.webp', '.gif'];
    const DungLuongToiDa = 5 * 1024 * 1024;
    const SoAnhToiDaCuaTin = 10;

    const vung = document.querySelector('[data-anh-vung]');
    if (!vung) return;

    const input = vung.querySelector('[data-anh-input]');
    const khungLoi = document.querySelector('[data-anh-loi]');
    const khoiMoi = document.querySelector('[data-anh-preview]');
    const luoiMoi = khoiMoi ? khoiMoi.querySelector('.rm-anh-luoi') : null;
    const khoiDaLuu = document.querySelector('[data-anh-da-luu]');
    const luoiDaLuu = khoiDaLuu ? khoiDaLuu.querySelector('.rm-anh-luoi') : null;
    const form = vung.closest('form');

    // Ảnh đã lưu không đổi thứ tự bằng JS được (server giữ ThuTu), nên suất còn lại
    // chỉ tính cho đợt ảnh mới.
    const docCaiConLai = () => {
        const daLuu = luoiDaLuu ? luoiDaLuu.children.length : 0;
        return Math.max(0, SoAnhToiDaCuaTin - daLuu);
    };

    let soLuongToiDa = parseInt(input.dataset.anhConLai, 10);
    if (isNaN(soLuongToiDa)) soLuongToiDa = docCaiConLai();

    // Safari cũ không gán được FileList: khi đó chỉ xem trước, thứ tự gửi theo thứ tự chọn.
    const coTheSapXep = typeof DataTransfer !== 'undefined';

    /** @type {File[]} */
    let danhSach = [];
    let keoTuIndex = null;

    const baoLoi = (thongBao) => {
        if (!khungLoi) return;
        khungLoi.textContent = thongBao || '';
        khungLoi.hidden = !thongBao;
    };

    const daCo = (f) => danhSach.some(x => x.name === f.name && x.size === f.size && x.lastModified === f.lastModified);

    const kiemTraFile = (f) => {
        const ten = f.name.toLowerCase();
        if (!DinhDang.some(d => ten.endsWith(d))) return 'định dạng không hỗ trợ';
        if (f.size > DungLuongToiDa) return 'lớn hơn 5MB';
        return null;
    };

    // Ghi thứ tự hiện tại vào input: ảnh đầu danh sách là ảnh gửi đầu tiên => thành ảnh bìa.
    const dongBoInput = () => {
        if (!coTheSapXep) return;
        const trungGian = new DataTransfer();
        danhSach.forEach(f => trungGian.items.add(f));
        input.files = trungGian.files;
    };

    const taoItem = (f, i) => {
        const item = document.createElement('figure');
        item.className = 'rm-anh-item' + (i === 0 ? ' is-bia' : '');
        item.draggable = coTheSapXep;
        item.dataset.index = String(i);

        const anh = document.createElement('img');
        anh.src = URL.createObjectURL(f);
        anh.alt = f.name;
        anh.loading = 'lazy';
        item.appendChild(anh);

        const temBia = document.createElement('span');
        temBia.className = 'rm-anh-bia';
        temBia.textContent = 'Ảnh bìa';
        item.appendChild(temBia);

        const temSo = document.createElement('span');
        temSo.className = 'rm-anh-so';
        temSo.textContent = String(i + 1);
        item.appendChild(temSo);

        const ten = document.createElement('figcaption');
        ten.className = 'rm-anh-ten';
        ten.textContent = f.name;
        item.appendChild(ten);

        const nutBo = document.createElement('button');
        nutBo.type = 'button';
        nutBo.className = 'rm-anh-bo';
        nutBo.setAttribute('aria-label', 'Bỏ ảnh ' + f.name);
        nutBo.textContent = '×';
        nutBo.dataset.anhBo = String(i);
        item.appendChild(nutBo);

        return item;
    };

    const veDanhSach = () => {
        if (!luoiMoi) return;
        // Thu hồi URL cũ trước khi dựng lại để không giữ cả thớt ảnh trong bộ nhớ.
        luoiMoi.querySelectorAll('img').forEach(img => URL.revokeObjectURL(img.src));
        luoiMoi.innerHTML = '';
        danhSach.forEach((f, i) => luoiMoi.appendChild(taoItem(f, i)));
        if (khoiMoi) khoiMoi.hidden = danhSach.length === 0;
        dongBoInput();
    };

    const themFile = (files) => {
        const boLo = [];
        for (const f of files) {
            const loi = kiemTraFile(f);
            if (loi) { boLo.push(f.name + ': ' + loi); continue; }
            if (daCo(f)) continue;
            if (danhSach.length >= soLuongToiDa) { boLo.push('chỉ thêm được ' + soLuongToiDa + ' ảnh mới'); break; }
            danhSach.push(f);
        }
        baoLoi(boLo.length ? 'Bỏ qua: ' + boLo.slice(0, 3).join(' · ') : '');
        veDanhSach();
    };

    // --- chọn file: input phủ kín vùng nên click/Enter mặc định của trình duyệt vẫn dùng được ---
    input.addEventListener('change', () => {
        if (input.files && input.files.length) themFile(Array.from(input.files));
    });

    ['dragenter', 'dragover'].forEach(ten => vung.addEventListener(ten, (e) => {
        e.preventDefault();
        vung.classList.add('is-drag');
    }));
    ['dragleave', 'drop'].forEach(ten => vung.addEventListener(ten, (e) => {
        e.preventDefault();
        vung.classList.remove('is-drag');
        if (ten === 'drop' && e.dataTransfer && e.dataTransfer.files.length) {
            themFile(Array.from(e.dataTransfer.files));
        }
    }));

    // --- sắp xếp ảnh mới: click để đưa lên bìa, kéo thả để đổi chỗ ---
    if (luoiMoi) {
        luoiMoi.addEventListener('click', (e) => {
            const nutBo = e.target.closest('[data-anh-bo]');
            if (nutBo) {
                danhSach.splice(parseInt(nutBo.dataset.anhBo, 10), 1);
                veDanhSach();
                return;
            }
            const item = e.target.closest('.rm-anh-item');
            if (!item || !coTheSapXep) return;
            const i = parseInt(item.dataset.index, 10);
            if (i > 0) {
                danhSach.unshift(danhSach.splice(i, 1)[0]);
                veDanhSach();
            }
        });

        luoiMoi.addEventListener('dragstart', (e) => {
            const item = e.target.closest('.rm-anh-item');
            if (!item) return;
            keoTuIndex = parseInt(item.dataset.index, 10);
            item.classList.add('is-dragging');
            if (e.dataTransfer) e.dataTransfer.effectAllowed = 'move';
        });
        luoiMoi.addEventListener('drop', (e) => {
            if (keoTuIndex === null) return;
            e.preventDefault();
            const item = e.target.closest('.rm-anh-item');
            const toiIndex = item ? parseInt(item.dataset.index, 10) : -1;
            if (toiIndex >= 0 && toiIndex !== keoTuIndex) {
                danhSach.splice(toiIndex, 0, danhSach.splice(keoTuIndex, 1)[0]);
                veDanhSach();
            }
            keoTuIndex = null;
        });
        luoiMoi.addEventListener('dragend', () => {
            keoTuIndex = null;
            luoiMoi.querySelectorAll('.is-dragging').forEach(el => el.classList.remove('is-dragging'));
        });
    }

    // --- ảnh đã lưu (chế độ sửa): gọi AJAX, tự cập nhật lưới, không reload trang ---
    const layToken = () => {
        const hidden = form ? form.querySelector('input[name="__RequestVerificationToken"]') : null;
        return hidden ? hidden.value : '';
    };

    if (luoiDaLuu) {
        luoiDaLuu.addEventListener('click', async (e) => {
            const nut = e.target.closest('[data-anh-dat-chinh], [data-anh-xoa]');
            if (!nut) return;

            const item = nut.closest('.rm-anh-item');
            const laXoa = nut.hasAttribute('data-anh-xoa');
            if (laXoa && !window.confirm('Xóa ảnh này?')) return;

            const formDuLieu = new FormData();
            formDuLieu.append('__RequestVerificationToken', layToken());

            nut.disabled = true;
            baoLoi('');
            try {
                const phanHoi = await fetch(nut.dataset.url, {
                    method: 'POST',
                    headers: { 'X-Requested-With': 'XMLHttpRequest' },
                    body: formDuLieu,
                    credentials: 'same-origin'
                });
                if (!phanHoi.ok) throw new Error('máy chủ trả về lỗi ' + phanHoi.status);

                const ketQua = await phanHoi.json();
                if (!ketQua.ok) throw new Error(ketQua.error || 'thao tác không thành công');

                if (laXoa) {
                    item.remove();
                    luoiDaLuu.querySelectorAll('.rm-anh-item').forEach(x => x.classList.remove('is-bia'));
                    // Server thăng ảnh có ThuTu nhỏ nhất (không phải ảnh đứng đầu lưới), nên lấy id
                    // server trả về thay vì đoán từ vị trí.
                    if (ketQua.coverId) {
                        const moi = luoiDaLuu.querySelector(`.rm-anh-item[data-ma-anh="${ketQua.coverId}"]`);
                        if (moi) moi.classList.add('is-bia');
                    }
                    if (!luoiDaLuu.children.length) khoiDaLuu.hidden = true;
                } else {
                    luoiDaLuu.querySelectorAll('.rm-anh-item').forEach(x => x.classList.remove('is-bia'));
                    item.classList.add('is-bia');
                    luoiDaLuu.insertBefore(item, luoiDaLuu.firstElementChild);
                }

                soLuongToiDa = docCaiConLai();
                input.dataset.anhConLai = String(soLuongToiDa);
                if (danhSach.length > soLuongToiDa) {
                    danhSach.length = soLuongToiDa;
                    veDanhSach();
                    baoLoi('Đã bỏ bớt ảnh mới vì tin vượt quá 10 ảnh.');
                }
            } catch (loi) {
                baoLoi('Không lưu được thay đổi (' + loi.message + '). Ảnh trên màn hình vẫn là ảnh cũ.');
            } finally {
                nut.disabled = false;
            }
        });
    }
})();
