// SchoolTrack Mobile Parent Map Interop (Leaflet + OpenStreetMap)
window.parentMapInterop = {
    map: null,
    busMarker: null,
    pickupMarker: null,
    schoolMarker: null,
    routePolyline: null,
    upcomingPolyline: null,
    lastLat: 37.7749,
    lastLng: -122.4194,

    initParentMap: function (containerId, busLat, busLng, busLabel) {
        if (!document.getElementById(containerId)) return;

        if (this.map) {
            try {
                this.map.remove();
            } catch (e) { }
            this.map = null;
        }

        this.lastLat = busLat;
        this.lastLng = busLng;

        this.map = L.map(containerId, {
            zoomControl: false,
            attributionControl: false
        }).setView([busLat, busLng], 14);

        // Soft CartoDB Positron tiles for claymorphic aesthetics
        L.tileLayer('https://{s}.basemaps.cartocdn.com/light_all/{z}/{x}/{y}{r}.png', {
            maxZoom: 19
        }).addTo(this.map);

        // Add custom Clay Bus Marker with pulsing radar wave
        const busIconHtml = `
            <div style="position:relative; width:44px; height:44px; display:flex; align-items:center; justify-content:center;">
                <div style="position:absolute; inset:-8px; border-radius:9999px; background:rgba(155,131,246,0.25); animation:pulse 2s infinite ease-in-out;"></div>
                <div style="width:40px; height:40px; border-radius:50%; background:#ffd66b; color:#241a00; display:flex; align-items:center; justify-content:center; box-shadow:0 8px 16px rgba(118,91,0,0.35), inset 0 2px 2px #ffffff; transform:rotate(-5deg);">
                    <span class="material-symbols-outlined" style="font-size:22px;">airport_shuttle</span>
                </div>
            </div>
        `;

        const busIcon = L.divIcon({
            html: busIconHtml,
            className: 'clay-bus-marker',
            iconSize: [44, 44],
            iconAnchor: [22, 22]
        });

        this.busMarker = L.marker([busLat, busLng], { icon: busIcon }).addTo(this.map);
        this.busMarker.bindPopup(`<b>${busLabel || 'Bus #14'}</b><br>Live Tracking Active`);
    },

    updateBusPosition: function (lat, lng, speed, heading) {
        if (!this.map || !this.busMarker) return;
        this.lastLat = lat;
        this.lastLng = lng;
        this.busMarker.setLatLng([lat, lng]);
    },

    drawRoute: function (waypointsJson) {
        if (!this.map) return;
        try {
            const waypoints = typeof waypointsJson === 'string' ? JSON.parse(waypointsJson) : waypointsJson;
            if (!waypoints || waypoints.length === 0) return;

            const completedPts = [];
            const upcomingPts = [];
            let splitPoint = null;

            waypoints.forEach((wp, index) => {
                const pt = [wp.latitude, wp.longitude];
                if (wp.isCompleted) {
                    completedPts.push(pt);
                    splitPoint = pt;
                } else {
                    if (splitPoint && upcomingPts.length === 0) {
                        upcomingPts.push(splitPoint);
                    }
                    upcomingPts.push(pt);
                }

                // Add Child pickup stop marker
                if (wp.isChildStop && !this.pickupMarker) {
                    const pickupIconHtml = `
                        <div style="width:28px; height:28px; border-radius:50%; background:#0c6b55; color:#ffffff; display:flex; align-items:center; justify-content:center; box-shadow:0 4px 10px rgba(12,107,85,0.4), inset 0 1px 1px #ffffff; font-weight:bold; font-size:14px;">
                            ✓
                        </div>
                    `;
                    const pickupIcon = L.divIcon({
                        html: pickupIconHtml,
                        className: 'pickup-marker',
                        iconSize: [28, 28],
                        iconAnchor: [14, 14]
                    });
                    this.pickupMarker = L.marker(pt, { icon: pickupIcon }).addTo(this.map);
                    this.pickupMarker.bindPopup(`<b>Pickup Stop</b><br>${wp.stopName}`);
                }
            });

            // Destination School Marker
            const lastWp = waypoints[waypoints.length - 1];
            if (lastWp && !this.schoolMarker) {
                const schoolIconHtml = `
                    <div style="width:32px; height:32px; border-radius:50%; background:#644bba; color:#ffffff; display:flex; align-items:center; justify-content:center; box-shadow:0 6px 14px rgba(100,75,186,0.4), inset 0 2px 2px #ffffff;">
                        <span class="material-symbols-outlined" style="font-size:18px;">school</span>
                    </div>
                `;
                const schoolIcon = L.divIcon({
                    html: schoolIconHtml,
                    className: 'school-marker',
                    iconSize: [32, 32],
                    iconAnchor: [16, 16]
                });
                this.schoolMarker = L.marker([lastWp.latitude, lastWp.longitude], { icon: schoolIcon }).addTo(this.map);
                this.schoolMarker.bindPopup(`<b>Destination</b><br>${lastWp.stopName}`);
            }

            // Draw Completed Route segment in Safety Mint
            if (completedPts.length > 1) {
                if (this.routePolyline) this.map.removeLayer(this.routePolyline);
                this.routePolyline = L.polyline(completedPts, {
                    color: '#0c6b55',
                    weight: 6,
                    opacity: 0.9,
                    lineCap: 'round',
                    lineJoin: 'round'
                }).addTo(this.map);
            }

            // Draw Upcoming Route segment in Lavender Dashed
            if (upcomingPts.length > 1) {
                if (this.upcomingPolyline) this.map.removeLayer(this.upcomingPolyline);
                this.upcomingPolyline = L.polyline(upcomingPts, {
                    color: '#9b83f6',
                    weight: 6,
                    dashArray: '8, 8',
                    opacity: 0.85,
                    lineCap: 'round'
                }).addTo(this.map);
            }
        } catch (e) {
            console.error("Error drawing route on parent map", e);
        }
    },

    recenterMap: function () {
        if (this.map && this.lastLat && this.lastLng) {
            this.map.panTo([this.lastLat, this.lastLng], { animate: true });
        }
    }
};

