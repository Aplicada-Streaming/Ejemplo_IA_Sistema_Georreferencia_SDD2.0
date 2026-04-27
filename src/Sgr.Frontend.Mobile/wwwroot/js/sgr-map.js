// E.3.4 — puente Blazor ↔ Leaflet 1.9.x
// Cargado dinámicamente como módulo ESM por LeafletMap.razor.
// Asume que window.L existe (leaflet.js cargado vía <script> en index.html).

const instances = new Map();

// Marker pin con CSS puro: evita la dependencia de PNGs (que en WebView de MAUI
// no resuelven bien con paths relativos). Es un círculo con cola triangular,
// estilado vía clase `.sgr-pin` inyectada al body.
let stylesInjected = false;
function ensureStyles() {
    if (stylesInjected) return;
    stylesInjected = true;
    const css = `
        .sgr-pin {
            width: 22px; height: 22px; border-radius: 50% 50% 50% 0;
            background: #1976d2; border: 2px solid white;
            transform: rotate(-45deg);
            box-shadow: 0 1px 3px rgba(0,0,0,0.4);
        }
        .sgr-pin::after {
            content: ""; position: absolute; top: 4px; left: 4px;
            width: 10px; height: 10px; border-radius: 50%;
            background: white; transform: rotate(45deg);
        }`;
    const style = document.createElement('style');
    style.textContent = css;
    document.head.appendChild(style);
}

function makePinIcon() {
    ensureStyles();
    return L.divIcon({
        className: '',
        html: '<div class="sgr-pin"></div>',
        iconSize: [22, 22],
        iconAnchor: [11, 22],     // base de la cola del pin
        popupAnchor: [0, -22],
    });
}

export function init(elementId, opts) {
    if (instances.has(elementId)) {
        // Idempotente: si ya existe (re-render del componente), sólo invalidamos size.
        instances.get(elementId).map.invalidateSize();
        return;
    }

    const center = opts?.center ?? { lat: -34.6037, lng: -58.3816 }; // Buenos Aires fallback
    const zoom = opts?.zoom ?? 13;

    const map = L.map(elementId, {
        zoomControl: true,
        attributionControl: true,
    }).setView([center.lat, center.lng], zoom);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        maxZoom: 19,
        attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>',
    }).addTo(map);

    instances.set(elementId, { map, markers: [] });

    // Tras un layout completo el contenedor puede no tener su tamaño definitivo todavía;
    // re-pintamos en el próximo tick para evitar tiles "rotos".
    setTimeout(() => map.invalidateSize(), 0);
}

export function setMarkers(elementId, markers) {
    const inst = instances.get(elementId);
    if (!inst) return;
    const { map } = inst;

    // Limpiar marcadores previos.
    for (const m of inst.markers) m.remove();
    inst.markers = [];

    if (!markers || markers.length === 0) return;

    const bounds = [];
    for (const m of markers) {
        const lat = Number(m.lat);
        const lng = Number(m.lng);
        if (Number.isNaN(lat) || Number.isNaN(lng)) continue;
        const marker = L.marker([lat, lng], { icon: makePinIcon() }).addTo(map);

        const title = m.title ?? 'Punto';
        const sub = [
            m.description,
            m.accuracyM ? `±${m.accuracyM} m` : null,
            m.captureMode ? `modo: ${m.captureMode}` : null,
            m.createdAt ? new Date(m.createdAt).toLocaleString() : null,
        ].filter(Boolean).join(' · ');
        const html = `<strong>${escapeHtml(title)}</strong>${sub ? `<br/><small>${escapeHtml(sub)}</small>` : ''}`;
        marker.bindPopup(html);

        inst.markers.push(marker);
        bounds.push([lat, lng]);
    }

    if (bounds.length === 1) {
        map.setView(bounds[0], 16);
    } else if (bounds.length > 1) {
        map.fitBounds(bounds, { padding: [40, 40] });
    }
}

export function panTo(elementId, lat, lng, zoom) {
    const inst = instances.get(elementId);
    if (!inst) return;
    inst.map.setView([lat, lng], zoom ?? inst.map.getZoom());
}

export function destroy(elementId) {
    const inst = instances.get(elementId);
    if (!inst) return;
    for (const m of inst.markers) m.remove();
    inst.map.remove();
    instances.delete(elementId);
}

function escapeHtml(s) {
    return String(s).replace(/[&<>"']/g, c => ({
        '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;',
    }[c]));
}
