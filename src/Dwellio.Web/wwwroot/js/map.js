// Leaflet map driven from Blazor (LiveMap.razor). Leaflet itself is bundled in /lib/leaflet,
// map tiles come from openstreetmap.org. If either fails, the page keeps working without a map.
const maps = new WeakMap();

const icons = {
    destination: "🎯", home: "🏠", area: "⭐", bus_stop: "🚌", board: "🟢", alight: "🔴",
    gym: "🏋️", spa: "🧖", restaurant: "🍽️", cafe: "☕", groceries: "🛒", pharmacy: "💊", park: "🌳",
    sports: "⚽", study: "📚", health: "🏥", bank: "🏧", mall: "🛍️", kids: "🧸", point: "📍"
};

function escapeHtml(text) {
    const div = document.createElement("div");
    div.textContent = text ?? "";
    return div.innerHTML;
}

export function create(element, dotnet, options) {
    if (!window.L) {
        element.classList.add("map-failed");
        element.textContent = "The map could not load. All information is still listed on this page.";
        return false;
    }
    const map = L.map(element, { zoomControl: true, scrollWheelZoom: options.scrollZoom ?? false, attributionControl: true })
        .setView([options.lat, options.lon], options.zoom ?? 14);
    const tiles = L.tileLayer("https://tile.openstreetmap.org/{z}/{x}/{y}.png", {
        maxZoom: 19,
        attribution: '&copy; <a href="https://www.openstreetmap.org/copyright" target="_blank" rel="noopener">OpenStreetMap</a> contributors'
    }).addTo(map);
    let tileErrors = 0;
    tiles.on("tileerror", () => {
        if (++tileErrors === 6) element.closest(".map-shell")?.classList.add("tiles-offline");
    });
    const layer = L.layerGroup().addTo(map);
    if (options.clickable) {
        map.on("click", e => dotnet.invokeMethodAsync("OnMapClick", e.latlng.lat, e.latlng.lng));
    }
    maps.set(element, { map, layer, dotnet });
    // Containers inside collapsible/tabbed layouts may resize after first paint.
    new ResizeObserver(() => map.invalidateSize()).observe(element);
    return true;
}

export function update(element, data) {
    const state = maps.get(element);
    if (!state) return;
    const { map, layer, dotnet } = state;
    layer.clearLayers();
    const bounds = [];

    for (const c of data.circles ?? []) {
        const circle = L.circle([c.lat, c.lon], {
            radius: c.radius, color: c.color ?? "#5b5bd6", weight: 2, fillOpacity: 0.12
        }).addTo(layer);
        if (c.label) circle.bindTooltip(escapeHtml(c.label));
        if (c.id) circle.on("click", () => dotnet.invokeMethodAsync("OnMarkerClick", c.id));
        bounds.push([c.lat, c.lon]);
    }
    if (data.line?.length > 1) {
        L.polyline(data.line, { color: "#5b5bd6", weight: 5, opacity: 0.75 }).addTo(layer);
        data.line.forEach(p => bounds.push(p));
    }
    for (const m of data.markers ?? []) {
        const icon = L.divIcon({
            className: "",
            html: `<span class="map-pin pin-${m.kind}${m.emphasis ? " pin-strong" : ""}" aria-hidden="true">${icons[m.kind] ?? icons.point}</span>`,
            iconSize: m.emphasis ? [38, 38] : [28, 28],
            iconAnchor: m.emphasis ? [19, 19] : [14, 14]
        });
        const marker = L.marker([m.lat, m.lon], { icon, title: m.label, keyboard: !!m.id, zIndexOffset: m.emphasis ? 1000 : 0 }).addTo(layer);
        if (m.popup || m.label) marker.bindPopup(`<strong>${escapeHtml(m.label)}</strong>${m.popup ? `<br>${escapeHtml(m.popup)}` : ""}`);
        if (m.id) marker.on("click", () => dotnet.invokeMethodAsync("OnMarkerClick", m.id));
        if (m.emphasis || !data.fitEmphasisOnly) bounds.push([m.lat, m.lon]);
    }
    if (data.fit && bounds.length > 1) map.fitBounds(bounds, { padding: [36, 36], maxZoom: 16 });
    else if (data.lat && data.lon) map.setView([data.lat, data.lon], data.zoom ?? map.getZoom());
}

export function dispose(element) {
    const state = maps.get(element);
    if (state) { state.map.remove(); maps.delete(element); }
}
