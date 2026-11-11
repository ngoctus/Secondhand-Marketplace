// Dropdown khu vực phụ thuộc: đổi tỉnh/thành thì tải lại danh sách xã/phường qua GET /KhuVuc/PhuongXa?maTinh=
// Dùng cho mọi phần tử có thuộc tính data-khuvuc (xem Views/Shared/_ChonKhuVuc.cshtml).
(function () {
    'use strict';

    function init(box) {
        var tinh = document.querySelector(box.dataset.tinh);
        var xa = document.querySelector(box.dataset.xa);
        var url = box.dataset.url;
        if (!tinh || !xa || !url) return;

        var labelXa = box.dataset.labelXa || '-- Chọn phường/xã --';
        var token = 0;   // bỏ qua phản hồi cũ khi người dùng đổi tỉnh liên tục

        function reset(text, disabled) {
            xa.innerHTML = '';
            var opt = document.createElement('option');
            opt.value = '';
            opt.textContent = text;
            xa.appendChild(opt);
            xa.disabled = disabled;
        }

        tinh.addEventListener('change', function () {
            var maTinh = tinh.value;
            var current = ++token;
            if (!maTinh) { reset(labelXa, true); return; }

            reset('Đang tải...', true);
            fetch(url + '?maTinh=' + encodeURIComponent(maTinh), { headers: { 'Accept': 'application/json' } })
                .then(function (r) { if (!r.ok) throw new Error(r.status); return r.json(); })
                .then(function (items) {
                    if (current !== token) return;
                    reset(labelXa, false);
                    items.forEach(function (it) {
                        var opt = document.createElement('option');
                        opt.value = it.ma;
                        opt.textContent = it.ten;
                        xa.appendChild(opt);
                    });
                })
                .catch(function () {
                    if (current !== token) return;
                    reset('Không tải được danh sách, thử lại', false);
                });
        });
    }

    document.addEventListener('DOMContentLoaded', function () {
        document.querySelectorAll('[data-khuvuc]').forEach(init);
    });
})();
