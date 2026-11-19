// Dropdown khu vực phụ thuộc: đổi tỉnh/thành thì tải lại danh sách xã/phường qua GET /KhuVuc/PhuongXa?maTinh=
// Dùng cho mọi phần tử có thuộc tính data-khuvuc (xem Views/Shared/_ChonKhuVuc.cshtml).
(function () {
    'use strict';

    var dieuKhien = [];

    function init(box) {
        var tinh = document.querySelector(box.dataset.tinh);
        var xa = document.querySelector(box.dataset.xa);
        var url = box.dataset.url;
        if (!tinh || !xa || !url) return;

        var labelXa = box.dataset.labelXa || '-- Chọn phường/xã --';
        var token = 0;   // bỏ qua phản hồi cũ khi người dùng đổi tỉnh liên tục
        var xaCho = null; // xã cần chọn sau khi danh sách tải xong
        var dangTai = false;

        function reset(text, disabled) {
            xa.innerHTML = '';
            var opt = document.createElement('option');
            opt.value = '';
            opt.textContent = text;
            xa.appendChild(opt);
            xa.disabled = disabled;
        }

        function taiXa(maTinh, chonXa) {
            var current = ++token;
            xaCho = chonXa || null;
            if (!maTinh) { dangTai = false; reset(labelXa, true); xaCho = null; return; }

            dangTai = true;
            reset('Đang tải...', true);
            fetch(url + '?maTinh=' + encodeURIComponent(maTinh), { headers: { 'Accept': 'application/json' } })
                .then(function (r) { if (!r.ok) throw new Error(r.status); return r.json(); })
                .then(function (items) {
                    if (current !== token) return;
                    dangTai = false;
                    reset(labelXa, false);
                    items.forEach(function (it) {
                        var opt = document.createElement('option');
                        opt.value = it.ma;
                        opt.textContent = it.ten;
                        xa.appendChild(opt);
                    });
                    if (xaCho) { xa.value = xaCho; xaCho = null; }
                })
                .catch(function () {
                    if (current !== token) return;
                    dangTai = false;
                    reset('Không tải được danh sách, thử lại', false);
                });
        }

        tinh.addEventListener('change', function () {
            taiXa(tinh.value, null);
        });

        dieuKhien.push({
            co: function () { return tinh; },
            dat: function (maTinh, maXa) {
                maTinh = maTinh || '';
                if (tinh.value === maTinh) {
                    // Đang tải danh sách xã: chỉ đổi đích đến, không ghi đè placeholder.
                    if (dangTai) xaCho = maXa || null;
                    else xa.value = maXa || '';
                    return;
                }
                tinh.value = maTinh;
                taiXa(maTinh, maXa);
            }
        });
    }

    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('[data-khuvuc]').forEach(init);
    });

    // B-04: bo-loc-nhanh.js gọi hàm này sau mỗi lần lọc AJAX để tỉnh/phường khớp lại URL.
    window.rmChonKhuVuc = function (maTinh, maXa) {
        dieuKhien.forEach(function (d) { d.dat(maTinh, maXa); });
    };
})();
