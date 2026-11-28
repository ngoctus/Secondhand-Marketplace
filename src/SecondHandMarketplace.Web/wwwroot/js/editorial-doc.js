// Tương tác cho các trang văn bản dài (Trung tâm hỗ trợ, Điều khoản & Chính sách, FAQ):
// thanh tiến độ đọc, mục lục scroll-spy, hiệu ứng hiện thẻ khi cuộn, tìm kiếm & lọc FAQ.
(() => {
    'use strict';

    // 1. Thanh tiến độ đọc trên cùng
    const bar = document.getElementById('rmReadingProgressBar');
    if (bar) {
        let ticking = false;
        const updateProgress = () => {
            const docHeight = document.documentElement.scrollHeight - window.innerHeight;
            const pct = docHeight > 0 ? Math.min(100, Math.max(0, (window.scrollY / docHeight) * 100)) : 0;
            bar.style.width = pct.toFixed(2) + '%';
            ticking = false;
        };
        window.addEventListener('scroll', () => {
            if (!ticking) {
                window.requestAnimationFrame(updateProgress);
                ticking = true;
            }
        }, { passive: true });
        updateProgress();
    }

    // 2. Hiệu ứng reveal mượt khi cuộn vào vùng nhìn
    const reveals = Array.from(document.querySelectorAll('.rm-doc-reveal'));
    if (reveals.length > 0 && 'IntersectionObserver' in window) {
        const revealObs = new IntersectionObserver((entries) => {
            entries.forEach((entry) => {
                if (entry.isIntersecting) {
                    entry.target.classList.add('is-visible');
                    revealObs.unobserve(entry.target);
                }
            });
        }, { rootMargin: '0px 0px -40px 0px', threshold: 0.05 });

        reveals.forEach((el) => revealObs.observe(el));
    } else {
        reveals.forEach((el) => el.classList.add('is-visible'));
    }

    // 3. Mục lục Scroll-Spy bên trái
    const tocLinks = Array.from(document.querySelectorAll('.rm-doc-toc-link[href^="#"]'));
    if (tocLinks.length > 0) {
        const sections = tocLinks
            .map((link) => {
                const id = link.getAttribute('href')?.slice(1);
                return id ? { link, el: document.getElementById(id) } : null;
            })
            .filter((x) => x && x.el);

        const setActive = (id) => {
            tocLinks.forEach((l) => l.classList.toggle('is-active', l.getAttribute('href') === '#' + id));
        };

        if ('IntersectionObserver' in window && sections.length > 0) {
            const spyObs = new IntersectionObserver((entries) => {
                const visible = entries
                    .filter((e) => e.isIntersecting)
                    .sort((a, b) => a.boundingClientRect.top - b.boundingClientRect.top);
                if (visible.length > 0) {
                    setActive(visible[0].target.id);
                }
            }, { rootMargin: '-18% 0px -65% 0px', threshold: 0 });

            sections.forEach((s) => spyObs.observe(s.el));
        }

        const initialHash = window.location.hash ? window.location.hash.slice(1) : sections[0]?.el.id;
        if (initialHash) setActive(initialHash);
    }

    // 4. Tự động cuộn mượt tới mục chỉ định (ví dụ /chinh-sach-bao-mat -> #bao-mat)
    const autoTarget = document.querySelector('[data-rm-auto-scroll]')?.getAttribute('data-rm-auto-scroll');
    if (autoTarget && !window.location.hash) {
        const targetEl = document.getElementById(autoTarget);
        if (targetEl) {
            window.requestAnimationFrame(() => {
                targetEl.scrollIntoView({ behavior: 'smooth', block: 'start' });
            });
        }
    }

    // 5. Bộ lọc & tìm kiếm FAQ trực tiếp
    document.querySelectorAll('[data-rm-faq-root]').forEach((root) => {
        const searchInput = root.querySelector('[data-rm-faq-search]');
        const catButtons = Array.from(root.querySelectorAll('[data-rm-faq-cat]'));
        const items = Array.from(root.querySelectorAll('.rm-faq-item'));
        const emptyBox = root.querySelector('[data-rm-faq-empty]');
        const toggleAllBtn = root.querySelector('[data-rm-faq-toggle-all]');
        let activeCat = 'all';

        const normalize = (s) => (s || '')
            .toLowerCase()
            .normalize('NFD')
            .replace(/[\u0300-\u036f]/g, '')
            .replace(/đ/g, 'd')
            .trim();

        const applyFilter = () => {
            const q = normalize(searchInput?.value || '');
            let visibleCount = 0;
            items.forEach((item) => {
                const itemCat = item.dataset.cat || 'all';
                const catMatch = activeCat === 'all' || itemCat === activeCat;
                const textMatch = !q || normalize(item.textContent).includes(q);
                const show = catMatch && textMatch;
                item.hidden = !show;
                if (show) visibleCount++;
            });
            if (emptyBox) emptyBox.hidden = visibleCount > 0;
        };

        catButtons.forEach((btn) => {
            btn.addEventListener('click', () => {
                activeCat = btn.dataset.rmFaqCat || 'all';
                catButtons.forEach((b) => b.classList.toggle('is-active', b === btn));
                applyFilter();
            });
        });

        if (searchInput) {
            searchInput.addEventListener('input', applyFilter);
        }

        if (toggleAllBtn) {
            let expanded = false;
            toggleAllBtn.addEventListener('click', () => {
                expanded = !expanded;
                items.forEach((item) => {
                    if (!item.hidden) item.open = expanded;
                });
                const labelOpen = toggleAllBtn.dataset.labelExpand || 'Expand all';
                const labelClose = toggleAllBtn.dataset.labelCollapse || 'Collapse all';
                toggleAllBtn.textContent = expanded ? labelClose : labelOpen;
            });
        }
    });
})();
