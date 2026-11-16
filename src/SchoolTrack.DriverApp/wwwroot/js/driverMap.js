// SchoolTrack In-Cab Driver Navigation Map Interop (Leaflet + OpenStreetMap)
window.driverMapInterop = {
    map: null,
    busMarker: null,
    stopMarkers: [],
    routePolyline: null,
    completedPolyline: null,
    lastLat: 37.7735,
    lastLng: -122.4205,

    initDriverMap: function (containerId, busLat, busLng, busLabel) {
        if (!document.getElementById(containerId)) return;

        if (this.map) {
            try {
                this.map.remove();
            } catch (e) { }
            this.map = null;
            this.stopMarkers = [];
        }

        this.lastLat = busLat || 37.7735;
        this.lastLng = busLng || -122.4205;

        this.map = L.map(containerId, {
            zoomControl: false,
            attributionControl: false
        }).setView([this.lastLat, this.lastLng], 15);

        // High-contrast clean CartoDB Voyager tiles for driver readability
        L.tileLayer('https://{s}.basemaps.cartocdn.com/rastertiles/voyager/{z}/{x}/{y}{r}.png', {
            maxZoom: 19
        }).addTo(this.map);

        // In-cab large directional bus icon
        const busIconHtml = `
            <div style="position:relative; width:52px; height:52px; display:flex; align-items:center; justify-content:center;">
                <div style="position:absolute; inset:-6px; border-radius:50%; background:rgba(12,107,85,0.3); animation:pulse 1.8s infinite ease-in-out;"></div>
                <div style="width:44px; height:44px; border-radius:50%; background:#0c6b55; color:#ffffff; display:flex; align-items:center; justify-content:center; box-shadow:0 8px 18px rgba(12,107,85,0.45), inset 0 2px 3px rgba(255,255,255,0.7); border:3px solid #ffffff;">
                    <span class="material-symbols-outlined" style="font-size:26px;">navigation</span>
                </div>
            </div>
        `;

        const busIcon = L.divIcon({
            html: busIconHtml,
            className: 'clay-driver-bus-marker',
            iconSize: [52, 52],
            iconAnchor: [26, 26]
        });

        this.busMarker = L.marker([this.lastLat, this.lastLng], { icon: busIcon }).addTo(this.map);
        this.busMarker.bindPopup(`<b>${busLabel || 'Bus #14'}</b><br>Active GPS Navigation`);
    },

    updateBusPosition: function (lat, lng, speed, heading) {
        if (!this.map || !this.busMarker) return;
        this.lastLat = lat;
        this.lastLng = lng;
        this.busMarker.setLatLng([lat, lng]);

        if (heading !== undefined && heading !== null) {
            const iconElem = this.busMarker.getElement();
            if (iconElem) {
                const navIcon = iconElem.querySelector('.material-symbols-outlined');
                if (navIcon) {
                    navIcon.style.transform = `rotate(${heading}deg)`;
                    navIcon.style.transition = 'transform 0.4s ease';
                }
            }
        }
    },

    recenterBus: function () {
        if (!this.map || !this.busMarker) return;
        this.map.panTo([this.lastLat, this.lastLng], { animate: true });
    },

    renderRouteStops: function (stopsJson) {
        if (!this.map) return;
        try {
            const stops = typeof stopsJson === 'string' ? JSON.parse(stopsJson) : stopsJson;
            if (!stops || stops.length === 0) return;

            // Clear previous stop markers
            this.stopMarkers.forEach(m => this.map.removeLayer(m));
            this.stopMarkers = [];

            if (this.routePolyline) this.map.removeLayer(this.routePolyline);
            if (this.completedPolyline) this.map.removeLayer(this.completedPolyline);

            const allPoints = [];
            const completedPoints = [];
            const upcomingPoints = [];
            let splitReached = false;

            stops.forEach((s) => {
                const pt = [s.latitude, s.longitude];
                allPoints.push(pt);

                if (s.isCompleted) {
                    completedPoints.push(pt);
                } else {
                    if (!splitReached && completedPoints.length > 0) {
                        upcomingPoints.push(completedPoints[completedPoints.length - 1]);
                        splitReached = true;
                    }
                    upcomingPoints.push(pt);
                }

                // Create Stop Pin HTML
                let markerHtml = '';
                if (s.isCompleted) {
                    markerHtml = `
                        <div style="width:26px; height:26px; border-radius:50%; background:#d8d7f7; color:#644bba; display:flex; align-items:center; justify-content:center; box-shadow:0 3px 6px rgba(0,0,0,0.15); font-size:12px; font-weight:700;">
                            ✓
                        </div>`;
                } else if (s.isActiveCurrent) {
                    markerHtml = `
                        <div style="position:relative; width:44px; height:44px; display:flex; align-items:center; justify-content:center;">
                            <div style="position:absolute; inset:-4px; border-radius:50%; background:rgba(255,214,107,0.5); animation:pulse 1.5s infinite;"></div>
                            <div style="width:36px; height:36px; border-radius:50%; background:#ffd66b; color:#241a00; display:flex; flex-direction:column; align-items:center; justify-content:center; box-shadow:0 6px 14px rgba(118,91,0,0.4); font-weight:800; font-size:13px; border:2px solid #ffffff;">
                                <span>${s.sequenceOrder}</span>
                            </div>
                        </div>`;
                } else {
                    markerHtml = `
                        <div style="width:30px; height:30px; border-radius:50%; background:#ffffff; color:#181930; display:flex; align-items:center; justify-content:center; box-shadow:0 4px 8px rgba(0,0,0,0.12); font-weight:700; font-size:12px; border:2px solid #644bba;">
                            ${s.sequenceOrder}
                        </div>`;
                }

                const stopIcon = L.divIcon({
                    html: markerHtml,
                    className: 'driver-stop-marker',
                    iconSize: s.isActiveCurrent ? [44, 44] : [30, 30],
                    iconAnchor: s.isActiveCurrent ? [22, 22] : [15, 15]
                });

                const m = L.marker(pt, { icon: stopIcon }).addTo(this.map);
                m.bindPopup(`<b>Stop ${s.sequenceOrder}: ${s.stopName}</b><br>Sched: ${s.scheduledTime}<br>Students: ${s.totalStudentsAtStop}`);
                this.stopMarkers.push(m);
            });

            // Draw polyline
            if (completedPoints.length > 1) {
                this.completedPolyline = L.polyline(completedPoints, {
                    color: '#9b83f6',
                    weight: 6,
                    opacity: 0.6,
                    dashArray: '4, 8'
                }).addTo(this.map);
            }

            if (upcomingPoints.length > 1) {
                this.routePolyline = L.polyline(upcomingPoints, {
                    color: '#0c6b55',
                    weight: 7,
                    opacity: 0.9,
                    lineJoin: 'round'
                }).addTo(this.map);
            } else if (allPoints.length > 1) {
                this.routePolyline = L.polyline(allPoints, {
                    color: '#0c6b55',
                    weight: 7,
                    opacity: 0.9,
                    lineJoin: 'round'
                }).addTo(this.map);
            }
        } catch (e) {
            console.error('Error drawing driver route stops:', e);
        }
    }
};

