// SchoolTrack Leaflet JS Interop for Live Radar Map
window.schoolTrackMap = {
  mapInstance: null,
  markers: {},
  routeLines: {},
  geofenceLayers: {},
  dotNetHelper: null,

  initMap: function (elementId, centerLat, centerLng, zoom, dotNetRef) {
    this.dotNetHelper = dotNetRef;
    const container = document.getElementById(elementId);
    if (!container) return;

    if (this.mapInstance) {
      this.mapInstance.remove();
      this.mapInstance = null;
      this.markers = {};
      this.routeLines = {};
      this.geofenceLayers = {};
    }

    // Initialize Leaflet map
    this.mapInstance = L.map(elementId, {
      zoomControl: false,
      attributionControl: false
    }).setView([centerLat, centerLng], zoom);

    // Add high-clarity CartoDB Positron / OSM tiles suitable for light clay UI
    L.tileLayer('https://{s}.basemaps.cartocdn.com/rastertiles/voyager/{z}/{x}/{y}{r}.png', {
      maxZoom: 19,
      subdomains: 'abcd',
    }).addTo(this.mapInstance);

    // Add custom zoom control at bottom right
    L.control.zoom({ position: 'bottomright' }).addTo(this.mapInstance);

    // Invalidate size once rendered
    setTimeout(() => {
      if (this.mapInstance) {
        this.mapInstance.invalidateSize();
      }
    }, 250);
  },

  updateBusMarker: function (busId, lat, lng, busNumber, speed, status, routeName, isSelected) {
    if (!this.mapInstance) return;

    const markerKey = busId.toString();
    const primaryColor = isSelected ? '#644BBA' : (status === 'Delayed' ? '#CCA741' : '#0C6B55');
    const containerColor = isSelected ? '#9B83F6' : (status === 'Delayed' ? '#FFD66B' : '#A1F3D7');
    const badgeText = `${busNumber} • ${Math.round(speed)} mph`;

    const customHtml = `
      <div class="bus-clay-marker" style="display:flex; flex-direction:column; align-items:center; cursor:pointer; transform: translate(-50%, -100%);">
        <div style="background:#ffffff; color:#181930; font-family:'Plus Jakarta Sans', sans-serif; font-size:11px; font-weight:700; padding:3px 8px; border-radius:9999px; box-shadow:0 4px 10px rgba(41,42,66,0.15), inset 0 2px 2px rgba(255,255,255,0.9); margin-bottom:4px; white-space:nowrap; display:flex; align-items:center; gap:4px;">
          <span style="width:6px; height:6px; border-radius:50%; background:${primaryColor}; display:inline-block;"></span>
          ${badgeText}
        </div>
        <div style="width:36px; height:36px; border-radius:50%; background:${primaryColor}; color:#ffffff; display:flex; align-items:center; justify-content:center; box-shadow:0 8px 16px rgba(41,42,66,0.25), inset 0 2px 2px rgba(255,255,255,0.5), inset 0 -3px 0 rgba(0,0,0,0.2); border:2px solid ${containerColor};">
          <span class="material-symbols-outlined" style="font-size:20px;">directions_bus</span>
        </div>
      </div>
    `;

    const icon = L.divIcon({
      html: customHtml,
      className: 'custom-bus-div-icon',
      iconSize: [36, 60],
      iconAnchor: [18, 55]
    });

    if (this.markers[markerKey]) {
      this.markers[markerKey].setLatLng([lat, lng]);
      this.markers[markerKey].setIcon(icon);
    } else {
      const marker = L.marker([lat, lng], { icon: icon }).addTo(this.mapInstance);
      marker.on('click', () => {
        if (this.dotNetHelper) {
          this.dotNetHelper.invokeMethodAsync('OnBusMarkerClicked', busId);
        }
      });
      this.markers[markerKey] = marker;
    }
  },

  drawGeofence: function (id, name, lat, lng, radiusMeters, fillColor, strokeColor) {
    if (!this.mapInstance) return;
    const key = id.toString();
    if (this.geofenceLayers[key]) {
      this.mapInstance.removeLayer(this.geofenceLayers[key]);
    }

    const circle = L.circle([lat, lng], {
      color: strokeColor || '#0C6B55',
      fillColor: fillColor || '#A1F3D7',
      fillOpacity: 0.25,
      weight: 2,
      dashArray: '6, 6',
      radius: radiusMeters
    }).addTo(this.mapInstance);

    circle.bindTooltip(`<b>${name}</b><br/>Safety Perimeter (${radiusMeters}m)`, {
      permanent: false,
      direction: 'top',
      className: 'clay-tooltip'
    });

    this.geofenceLayers[key] = circle;
  },

  drawRoutePath: function (routeId, latLngPairs, colorHex) {
    if (!this.mapInstance) return;
    const key = routeId.toString();
    if (this.routeLines[key]) {
      this.mapInstance.removeLayer(this.routeLines[key]);
    }

    const line = L.polyline(latLngPairs, {
      color: colorHex || '#9B83F6',
      weight: 5,
      opacity: 0.85,
      lineCap: 'round',
      lineJoin: 'round',
      dashArray: '10, 8'
    }).addTo(this.mapInstance);

    this.routeLines[key] = line;
  },

  centerOnBus: function (busId) {
    const key = busId.toString();
    if (this.markers[key] && this.mapInstance) {
      const latLng = this.markers[key].getLatLng();
      this.mapInstance.setView(latLng, 16, { animate: true });
    }
  },

  fitAll: function () {
    if (!this.mapInstance) return;
    const group = [];
    for (const key in this.markers) {
      group.push(this.markers[key].getLatLng());
    }
    for (const key in this.geofenceLayers) {
      group.push(this.geofenceLayers[key].getLatLng());
    }
    if (group.length > 0) {
      const bounds = L.latLngBounds(group);
      this.mapInstance.fitBounds(bounds, { padding: [50, 50], maxZoom: 16 });
    }
  }
};

