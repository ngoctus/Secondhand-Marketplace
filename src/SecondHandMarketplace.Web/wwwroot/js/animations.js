(function () {
    'use strict';

    var reduced = window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    if (reduced || typeof IntersectionObserver === 'undefined') return;

    document.documentElement.classList.add('rm-motion');

    function khiSanSang(fn) {
        if (document.readyState !== 'loading') fn();
        else document.addEventListener('DOMContentLoaded', fn);
    }

    function quanSat(nodes, onEnter, options) {
        var io = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (!entry.isIntersecting) return;
                onEnter(entry.target);
                io.unobserve(entry.target);
            });
        }, options || { rootMargin: '0px 0px -8% 0px', threshold: 0.08 });
        nodes.forEach(function (el) { io.observe(el); });
        return io;
    }

    function dinhDangSo(n) {
        try { return n.toLocaleString('vi-VN'); } catch (e) { return String(Math.round(n)); }
    }

    function demTangDan(el) {
        var dich = parseFloat(el.getAttribute('data-rm-count'));
        if (isNaN(dich) || dich <= 0) return;
        var hoan = el.getAttribute('data-rm-suffix') || '';
        var thoiGian = parseInt(el.getAttribute('data-rm-duration'), 10) || 1100;
        var batDau = performance.now();

        function buoc(moc) {
            var tienDo = Math.min((moc - batDau) / thoiGian, 1);
            var giaTri = dich * (1 - Math.pow(1 - tienDo, 3));
            el.textContent = dinhDangSo(giaTri) + hoan;
            if (tienDo < 1) requestAnimationFrame(buoc);
        }

        el.textContent = '0' + hoan;
        requestAnimationFrame(buoc);
    }

    function skeletonAnh(box) {
        var img = box.querySelector('img');
        if (!img || (img.complete && img.naturalWidth > 0)) return;

        var skeleton = document.createElement('span');
        skeleton.className = 'rm-skeleton';
        skeleton.setAttribute('aria-hidden', 'true');
        box.appendChild(skeleton);
        img.classList.add('rm-img-loading');

        function ketThuc() {
            img.classList.remove('rm-img-loading');
            img.classList.add('rm-img-loaded');
            if (skeleton.parentNode) skeleton.parentNode.removeChild(skeleton);
        }

        img.addEventListener('load', ketThuc, { once: true });
        img.addEventListener('error', function () {
            img.classList.remove('rm-img-loading');
            if (skeleton.parentNode) skeleton.parentNode.removeChild(skeleton);
        }, { once: true });
    }

    khiSanSang(function () {
        var theHienDan = document.querySelectorAll('[data-rm-reveal]');
        theHienDan.forEach(function (el) {
            var c = el.getAttribute('data-rm-reveal');
            el.setAttribute('data-rm-reveal', c === '' ? 'up' : c);
        });

        document.querySelectorAll('[data-rm-stagger]').forEach(function (grid) {
            Array.prototype.slice.call(grid.children).forEach(function (child, i) {
                if (child.nodeType !== 1) return;
                if (!child.hasAttribute('data-rm-reveal')) child.setAttribute('data-rm-reveal', 'up');
                child.style.setProperty('--rm-delay', (Math.min(i, 11) * 45) + 'ms');
            });
        });

        var tatCa = document.querySelectorAll('[data-rm-reveal]');
        quanSat(tatCa, function (el) { el.classList.add('rm-reveal-in'); });
        quanSat(document.querySelectorAll('[data-rm-count]'), demTangDan,
            { rootMargin: '0px', threshold: 0.4 });
        document.querySelectorAll('[data-rm-media]').forEach(skeletonAnh);
    });
})();
