(() => {
    'use strict';

    const inputs = Array.from(document.querySelectorAll('input[data-gmaps-address="true"]'));
    if (inputs.length === 0) return;

    const apiKey = (document.querySelector('meta[name="rm-gmaps-key"]')?.getAttribute('content') || '').trim();
    const isEn = (document.documentElement.lang || '').toLowerCase().startsWith('en');

    const T = {
        pickMap: isEn ? 'Pick on Map' : 'Chọn trên bản đồ',
        useGps: isEn ? 'Current location' : 'Vị trí hiện tại',
        locating: isEn ? 'Locating...' : 'Đang định vị...',
        confirm: isEn ? 'Use this address' : 'Dùng địa chỉ này',
        cancel: isEn ? 'Close' : 'Đóng',
        modalTitle: isEn ? 'Select real address on Map' : 'Chọn địa chỉ chi tiết trên bản đồ',
        mapHint: isEn ? 'Search, click, or drag the pin on the map to pick your exact real address.' : 'Tìm kiếm, nhấn hoặc kéo ghim trên bản đồ để chọn chính xác vị trí thực tế của bạn.',
        searchPlaceholder: isEn ? 'Search street, building, address...' : 'Tìm tên đường, số nhà, tòa nhà...',
        searchBtn: isEn ? 'Search' : 'Tìm',
        gpsDenied: isEn ? 'Unable to access GPS location. Please allow location permission.' : 'Không lấy được vị trí hiện tại. Vui lòng cấp quyền định vị trên trình duyệt.',
        noResult: isEn ? 'No matching address found.' : 'Không tìm thấy địa chỉ phù hợp.',
        poweredGoogle: 'Google Maps',
        poweredOsm: 'OpenStreetMap'
    };

    let activeModalInput = null;
    let selectedAddressText = '';
    let modalEl = null;

    // Google Maps state
    let gmapInstance = null;
    let gmapMarker = null;
    let gmapGeocoder = null;

    // Leaflet (OpenStreetMap) state
    let leafletLoaded = false;
    let leafletMap = null;
    let leafletMarker = null;

    function extractStreetFromGooglePlace(place) {
        if (!place) return '';
        const comps = place.address_components || [];
        const get = (type) => comps.find(c => (c.types || []).includes(type))?.long_name || '';
        const streetNum = get('street_number');
        const route = get('route');
        const premise = get('premise') || get('subpremise') || get('point_of_interest');
        const parts = [];
        if (premise && !route.includes(premise)) parts.push(premise);
        if (streetNum && route) parts.push(`${streetNum} ${route}`);
        else if (route) parts.push(route);
        else if (streetNum) parts.push(streetNum);

        if (parts.length > 0) return parts.join(', ').slice(0, 300);
        return (place.formatted_address || place.name || '').slice(0, 300);
    }

    function extractStreetFromOsm(data) {
        if (!data) return '';
        const a = data.address || {};
        const road = a.road || a.pedestrian || a.residential || a.footway || a.neighbourhood || a.suburb || '';
        const building = a.amenity || a.building || a.shop || a.office || '';
        const streetPart = [a.house_number, road].filter(Boolean).join(' ');
        const parts = [];
        if (building && !streetPart.includes(building)) parts.push(building);
        if (streetPart) parts.push(streetPart);
        if (parts.length > 0) return parts.join(', ').slice(0, 300);
        return (data.display_name || '').slice(0, 300);
    }

    function setInputAddress(input, address) {
        if (!input || !address) return;
        input.value = address.trim().slice(0, 300);
        input.dispatchEvent(new Event('input', { bubbles: true }));
        input.dispatchEvent(new Event('change', { bubbles: true }));
    }

    function getContextRegionText(input) {
        const container = input.closest('fieldset, form, .card-body') || document;
        const selects = Array.from(container.querySelectorAll('select'));
        const names = [];
        for (const s of selects) {
            const name = (s.name || s.id || '').toLowerCase();
            if (name.includes('tinh') || name.includes('phuong') || name.includes('xa')) {
                const opt = s.options[s.selectedIndex];
                if (opt && opt.value && opt.text && !opt.text.startsWith('--')) {
                    names.push(opt.text.trim());
                }
            }
        }
        return names.join(', ');
    }

    function buildActionBar(input, useGoogle) {
        const host = input.closest('.input-group') || input;
        const bar = document.createElement('div');
        bar.className = 'rm-gmaps-actions';

        const mapBtn = document.createElement('button');
        mapBtn.type = 'button';
        mapBtn.className = 'rm-gmaps-btn';
        mapBtn.innerHTML = `<i class="bi bi-map"></i> <span>${T.pickMap}</span>`;
        mapBtn.addEventListener('click', () => {
            if (useGoogle && window.google?.maps) openGoogleMapModal(input);
            else openOsmMapModal(input);
        });
        bar.appendChild(mapBtn);

        const gpsBtn = document.createElement('button');
        gpsBtn.type = 'button';
        gpsBtn.className = 'rm-gmaps-btn';
        gpsBtn.innerHTML = `<i class="bi bi-crosshair"></i> <span>${T.useGps}</span>`;
        gpsBtn.addEventListener('click', () => locateCurrentAddress(input, gpsBtn, useGoogle));
        bar.appendChild(gpsBtn);

        const badge = document.createElement('span');
        badge.className = 'rm-gmaps-provider';
        badge.innerHTML = `<i class="bi bi-geo-alt-fill"></i> ${useGoogle ? T.poweredGoogle : T.poweredOsm}`;
        bar.appendChild(badge);

        host.insertAdjacentElement('afterend', bar);
    }

    function locateCurrentAddress(input, btn, useGoogle) {
        if (!navigator.geolocation) {
            alert(T.gpsDenied);
            return;
        }
        const origHtml = btn.innerHTML;
        btn.disabled = true;
        btn.innerHTML = `<i class="bi bi-hourglass-split"></i> <span>${T.locating}</span>`;

        navigator.geolocation.getCurrentPosition(
            async (pos) => {
                const { latitude, longitude } = pos.coords;
                try {
                    if (useGoogle && window.google?.maps) {
                        gmapGeocoder = gmapGeocoder || new google.maps.Geocoder();
                        gmapGeocoder.geocode({ location: { lat: latitude, lng: longitude } }, (results, status) => {
                            if (status === 'OK' && results && results[0]) {
                                setInputAddress(input, extractStreetFromGooglePlace(results[0]));
                            }
                            btn.disabled = false;
                            btn.innerHTML = origHtml;
                        });
                        return;
                    }
                    const resp = await fetch(`https://nominatim.openstreetmap.org/reverse?format=jsonv2&lat=${latitude}&lon=${longitude}&accept-language=vi`);
                    if (resp.ok) {
                        const data = await resp.json();
                        setInputAddress(input, extractStreetFromOsm(data));
                    }
                } catch {
                    // Best-effort
                } finally {
                    if (!useGoogle || !window.google?.maps) {
                        btn.disabled = false;
                        btn.innerHTML = origHtml;
                    }
                }
            },
            () => {
                btn.disabled = false;
                btn.innerHTML = origHtml;
                alert(T.gpsDenied);
            },
            { enableHighAccuracy: true, timeout: 10000 }
        );
    }

    // ==================== SHARED MODAL ====================
    function ensureMapModal(onSearch) {
        if (modalEl) return modalEl;
        modalEl = document.createElement('div');
        modalEl.className = 'rm-gmaps-modal-backdrop';
        modalEl.hidden = true;
        modalEl.innerHTML = `
            <div class="rm-gmaps-modal" role="dialog" aria-modal="true" aria-label="${T.modalTitle}">
                <div class="rm-gmaps-modal-head">
                    <h6 class="mb-0 fw-bold"><i class="bi bi-geo-alt-fill text-danger me-1"></i>${T.modalTitle}</h6>
                    <button type="button" class="btn-close" data-gmaps-close aria-label="${T.cancel}"></button>
                </div>
                <div class="rm-gmaps-modal-body">
                    <div class="input-group input-group-sm mb-2">
                        <input type="text" class="form-control" id="rmGmapsSearchInput" placeholder="${T.searchPlaceholder}" />
                        <button type="button" class="btn btn-outline-primary fw-semibold" id="rmGmapsSearchBtn">
                            <i class="bi bi-search me-1"></i>${T.searchBtn}
                        </button>
                    </div>
                    <p class="small text-muted mb-2">${T.mapHint}</p>
                    <div id="rmGmapsCanvas" class="rm-gmaps-canvas"></div>
                    <div class="rm-gmaps-picked mt-2" id="rmGmapsPickedText">—</div>
                </div>
                <div class="rm-gmaps-modal-foot">
                    <button type="button" class="btn btn-outline-secondary btn-sm rounded-pill px-3" data-gmaps-close>${T.cancel}</button>
                    <button type="button" class="btn btn-primary btn-sm rounded-pill px-4 fw-semibold" id="rmGmapsConfirmBtn">${T.confirm}</button>
                </div>
            </div>`;
        document.body.appendChild(modalEl);

        modalEl.querySelectorAll('[data-gmaps-close]').forEach(b => b.addEventListener('click', () => { modalEl.hidden = true; }));
        modalEl.addEventListener('click', (e) => { if (e.target === modalEl) modalEl.hidden = true; });
        modalEl.querySelector('#rmGmapsConfirmBtn').addEventListener('click', () => {
            if (activeModalInput && selectedAddressText) {
                setInputAddress(activeModalInput, selectedAddressText);
            }
            modalEl.hidden = true;
        });

        const searchInput = modalEl.querySelector('#rmGmapsSearchInput');
        const searchBtn = modalEl.querySelector('#rmGmapsSearchBtn');
        const triggerSearch = () => {
            const q = searchInput.value.trim();
            if (q && typeof onSearch === 'function') onSearch(q);
        };
        searchBtn.addEventListener('click', triggerSearch);
        searchInput.addEventListener('keydown', (e) => {
            if (e.key === 'Enter') {
                e.preventDefault();
                triggerSearch();
            }
        });
        return modalEl;
    }

    // ==================== GOOGLE MAPS MODE ====================
    function updateGooglePinAddress(latLng) {
        const pickedEl = modalEl?.querySelector('#rmGmapsPickedText');
        if (pickedEl) pickedEl.textContent = T.locating;
        gmapGeocoder = gmapGeocoder || new google.maps.Geocoder();
        gmapGeocoder.geocode({ location: latLng }, (results, status) => {
            if (status === 'OK' && results && results[0]) {
                selectedAddressText = extractStreetFromGooglePlace(results[0]);
                if (pickedEl) pickedEl.textContent = results[0].formatted_address || selectedAddressText;
            } else if (pickedEl) {
                pickedEl.textContent = T.noResult;
            }
        });
    }

    function geocodeGoogleQuery(queryText) {
        gmapGeocoder = gmapGeocoder || new google.maps.Geocoder();
        gmapGeocoder.geocode({ address: queryText }, (results, status) => {
            if (status === 'OK' && results && results[0]) {
                const loc = results[0].geometry.location;
                gmapInstance.setCenter(loc);
                gmapMarker.setPosition(loc);
                selectedAddressText = extractStreetFromGooglePlace(results[0]);
                modalEl.querySelector('#rmGmapsPickedText').textContent = results[0].formatted_address || selectedAddressText;
            }
        });
    }

    function openGoogleMapModal(input) {
        if (!window.google?.maps) return;
        activeModalInput = input;
        ensureMapModal((q) => geocodeGoogleQuery(`${q}, Việt Nam`));
        modalEl.hidden = false;

        const defaultCenter = { lat: 10.7769, lng: 106.7009 };
        const canvas = modalEl.querySelector('#rmGmapsCanvas');
        if (!gmapInstance) {
            gmapInstance = new google.maps.Map(canvas, { center: defaultCenter, zoom: 15, streetViewControl: false, mapTypeControl: false });
            gmapMarker = new google.maps.Marker({ position: defaultCenter, map: gmapInstance, draggable: true });
            gmapInstance.addListener('click', (e) => {
                gmapMarker.setPosition(e.latLng);
                updateGooglePinAddress(e.latLng);
            });
            gmapMarker.addListener('dragend', () => updateGooglePinAddress(gmapMarker.getPosition()));
        }

        const queryText = [input.value.trim(), getContextRegionText(input), 'Việt Nam'].filter(Boolean).join(', ');
        modalEl.querySelector('#rmGmapsSearchInput').value = input.value.trim();
        geocodeGoogleQuery(queryText);
    }

    function initGooglePlaces() {
        inputs.forEach(input => {
            buildActionBar(input, true);
            const ac = new google.maps.places.Autocomplete(input, {
                componentRestrictions: { country: 'vn' },
                fields: ['address_components', 'formatted_address', 'name', 'geometry']
            });
            ac.addListener('place_changed', () => {
                const place = ac.getPlace();
                const street = extractStreetFromGooglePlace(place);
                if (street) setInputAddress(input, street);
            });
        });
    }

    // ==================== LEAFLET + OPENSTREETMAP (FREE REAL DATA) ====================
    function loadLeaflet() {
        return new Promise((resolve, reject) => {
            if (leafletLoaded && window.L) { resolve(); return; }
            const css = document.createElement('link');
            css.rel = 'stylesheet';
            css.href = 'https://unpkg.com/leaflet@1.9.4/dist/leaflet.css';
            document.head.appendChild(css);

            const js = document.createElement('script');
            js.src = 'https://unpkg.com/leaflet@1.9.4/dist/leaflet.js';
            js.onload = () => { leafletLoaded = true; resolve(); };
            js.onerror = reject;
            document.head.appendChild(js);
        });
    }

    async function reverseGeocodeOsm(lat, lng) {
        const pickedEl = modalEl?.querySelector('#rmGmapsPickedText');
        if (pickedEl) pickedEl.textContent = T.locating;
        try {
            const resp = await fetch(`https://nominatim.openstreetmap.org/reverse?format=jsonv2&lat=${lat}&lon=${lng}&accept-language=vi`);
            if (resp.ok) {
                const data = await resp.json();
                selectedAddressText = extractStreetFromOsm(data);
                if (pickedEl) pickedEl.textContent = data.display_name || selectedAddressText || T.noResult;
                return;
            }
        } catch { }
        if (pickedEl) pickedEl.textContent = T.noResult;
    }

    async function searchOsmOnMap(queryText) {
        const pickedEl = modalEl?.querySelector('#rmGmapsPickedText');
        if (pickedEl) pickedEl.textContent = T.locating;
        try {
            const url = `https://nominatim.openstreetmap.org/search?format=jsonv2&addressdetails=1&countrycodes=vn&limit=1&accept-language=vi&q=${encodeURIComponent(queryText)}`;
            const resp = await fetch(url);
            if (resp.ok) {
                const arr = await resp.json();
                if (Array.isArray(arr) && arr.length > 0) {
                    const hit = arr[0];
                    const lat = parseFloat(hit.lat);
                    const lng = parseFloat(hit.lon);
                    if (leafletMap && leafletMarker) {
                        leafletMap.setView([lat, lng], 16);
                        leafletMarker.setLatLng([lat, lng]);
                    }
                    selectedAddressText = extractStreetFromOsm(hit);
                    if (pickedEl) pickedEl.textContent = hit.display_name || selectedAddressText;
                    return;
                }
            }
        } catch { }
        if (pickedEl) pickedEl.textContent = T.noResult;
    }

    async function openOsmMapModal(input) {
        activeModalInput = input;
        ensureMapModal((q) => {
            const region = getContextRegionText(input);
            searchOsmOnMap(region ? `${q}, ${region}, Vietnam` : `${q}, Vietnam`);
        });
        modalEl.hidden = false;
        modalEl.querySelector('#rmGmapsSearchInput').value = input.value.trim();

        try {
            await loadLeaflet();
        } catch {
            modalEl.querySelector('#rmGmapsPickedText').textContent = T.noResult;
            return;
        }

        const defaultLat = 10.7769;
        const defaultLng = 106.7009;
        if (!leafletMap) {
            leafletMap = L.map('rmGmapsCanvas').setView([defaultLat, defaultLng], 15);
            L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
                maxZoom: 19,
                attribution: '&copy; OpenStreetMap'
            }).addTo(leafletMap);
            leafletMarker = L.marker([defaultLat, defaultLng], { draggable: true }).addTo(leafletMap);
            leafletMap.on('click', (e) => {
                leafletMarker.setLatLng(e.latlng);
                reverseGeocodeOsm(e.latlng.lat, e.latlng.lng);
            });
            leafletMarker.on('dragend', () => {
                const pos = leafletMarker.getLatLng();
                reverseGeocodeOsm(pos.lat, pos.lng);
            });
        }

        setTimeout(() => leafletMap.invalidateSize(), 120);

        const initialQuery = [input.value.trim(), getContextRegionText(input), 'Vietnam'].filter(Boolean).join(', ');
        if (initialQuery && initialQuery !== 'Vietnam') {
            searchOsmOnMap(initialQuery);
        } else {
            reverseGeocodeOsm(defaultLat, defaultLng);
        }
    }

    function initOsmAutocomplete() {
        inputs.forEach(input => {
            buildActionBar(input, false);

            const list = document.createElement('div');
            list.className = 'rm-gmaps-suggest-list';
            list.hidden = true;
            (input.closest('.input-group') || input).insertAdjacentElement('afterend', list);

            let timer = null;
            let ctrl = null;

            input.addEventListener('input', () => {
                const q = input.value.trim();
                clearTimeout(timer);
                if (q.length < 3) {
                    list.hidden = true;
                    return;
                }
                timer = setTimeout(async () => {
                    if (ctrl) ctrl.abort();
                    ctrl = new AbortController();
                    const region = getContextRegionText(input);
                    const fullQ = region ? `${q}, ${region}, Vietnam` : `${q}, Vietnam`;
                    try {
                        const url = `https://nominatim.openstreetmap.org/search?format=jsonv2&addressdetails=1&countrycodes=vn&limit=5&accept-language=vi&q=${encodeURIComponent(fullQ)}`;
                        const resp = await fetch(url, { signal: ctrl.signal });
                        if (!resp.ok) return;
                        const data = await resp.json();
                        list.innerHTML = '';
                        if (!Array.isArray(data) || data.length === 0) {
                            list.hidden = true;
                            return;
                        }
                        data.forEach(item => {
                            const street = extractStreetFromOsm(item);
                            const btn = document.createElement('button');
                            btn.type = 'button';
                            btn.className = 'rm-gmaps-suggest-item';
                            btn.innerHTML = `<i class="bi bi-geo-alt text-primary"></i><span><strong>${street || item.name || q}</strong><small>${item.display_name}</small></span>`;
                            btn.addEventListener('mousedown', (e) => {
                                e.preventDefault();
                                setInputAddress(input, street || item.display_name);
                                list.hidden = true;
                            });
                            list.appendChild(btn);
                        });
                        list.hidden = false;
                    } catch {
                        // Ignore abort/network errors
                    }
                }, 350);
            });

            input.addEventListener('blur', () => {
                setTimeout(() => { list.hidden = true; }, 160);
            });
        });
    }

    if (apiKey) {
        window.__rmInitGoogleMapsAddress = initGooglePlaces;
        const script = document.createElement('script');
        script.src = `https://maps.googleapis.com/maps/api/js?key=${encodeURIComponent(apiKey)}&libraries=places&language=${isEn ? 'en' : 'vi'}&region=VN&callback=__rmInitGoogleMapsAddress`;
        script.async = true;
        script.defer = true;
        script.onerror = () => initOsmAutocomplete();
        document.head.appendChild(script);
    } else {
        initOsmAutocomplete();
    }
})();
