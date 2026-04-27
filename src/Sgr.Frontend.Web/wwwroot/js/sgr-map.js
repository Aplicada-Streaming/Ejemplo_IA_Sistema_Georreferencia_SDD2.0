// E.3.4 — puente Blazor ↔ Leaflet 1.9.x
// Cargado dinámicamente como módulo ESM por LeafletMap.razor.
// Asume que window.L existe (leaflet.js cargado vía <script> en index.html).

const instances = new Map();

// Sin íconos custom: usamos L.circleMarker (SVG puro de Leaflet) que renderiza
// igual en WebView móvil y Blazor Server, sin depender de divIcon/CSS/PNGs.
const MARKER_STYLE = {
    radius: 8,
    color: '#1976d2',
    weight: 2,
    fillColor: '#1976d2',
    fillOpacity: 0.85,
};

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

/**
 * @param {DotNet.DotNetObject|null} dotNetRef — si se pasa, cada marker invoca
 *   `OnMarkerClickedAsync(pointId)` cuando el usuario lo toca. Sirve para que
 *   Blazor abra una galería de fotos por punto, etc.
 */
export function setMarkers(elementId, markers, dotNetRef) {
    const inst = instances.get(elementId);
    if (!inst) return;
    const { map } = inst;

    console.log('[sgr-map] setMarkers called', {
        elementId,
        count: markers?.length ?? 0,
        hasDotNetRef: !!dotNetRef,
        firstPointId: markers?.[0]?.pointId,
    });

    // Limpiar marcadores previos.
    for (const m of inst.markers) m.remove();
    inst.markers = [];

    if (!markers || markers.length === 0) return;

    const bounds = [];
    for (const m of markers) {
        const lat = Number(m.lat);
        const lng = Number(m.lng);
        if (Number.isNaN(lat) || Number.isNaN(lng)) continue;
        const marker = L.circleMarker([lat, lng], MARKER_STYLE).addTo(map);

        const title = m.title ?? 'Punto';
        const sub = [
            m.description,
            m.accuracyM ? `±${m.accuracyM} m` : null,
            m.captureMode ? `modo: ${m.captureMode}` : null,
            m.createdAt ? new Date(m.createdAt).toLocaleString() : null,
        ].filter(Boolean).join(' · ');
        const html = `<strong>${escapeHtml(title)}</strong>${sub ? `<br/><small>${escapeHtml(sub)}</small>` : ''}`;
        marker.bindPopup(html);

        if (dotNetRef && m.pointId) {
            // Capturamos el pointId en una const local para que el closure lo retenga
            // correctamente en lugar de mirar el `m` del último iteración.
            const pointId = m.pointId;
            marker.on('click', () => {
                console.log('[sgr-map] marker clicked, invoking Blazor', pointId);
                dotNetRef.invokeMethodAsync('OnMarkerClickedAsync', pointId)
                    .then(() => console.log('[sgr-map] Blazor returned OK', pointId))
                    .catch(err => console.error('[sgr-map] Blazor invoke failed', err));
            });
        } else {
            console.warn('[sgr-map] click handler NOT wired', {
                hasDotNetRef: !!dotNetRef,
                pointId: m.pointId,
            });
        }

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
