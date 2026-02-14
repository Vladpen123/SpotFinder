window.mapHelper = {
    map: null,
    markers: [],
    dotNetRef: null,

    initMap: function (elementId, lat, lon, zoom, dotNetRef) {
        this.dotNetRef = dotNetRef;
        var container = document.getElementById(elementId);
        if (!container) return;

        if (this.map) {
            this.map.remove();
            this.map = null;
            this.markers = [];
        }

        this.map = L.map(elementId).setView([lat, lon], zoom);

        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '© OpenStreetMap contributors'
        }).addTo(this.map);

        this.map.on('click', () => {
            this.dotNetRef.invokeMethodAsync('OnSpotDeselected');
        });

        this.map.on('moveend', () => {
            const center = this.map.getCenter();
            const z = this.map.getZoom();
            this.dotNetRef.invokeMethodAsync('OnMapMoved', center.lat, center.lng, z);
        });
    },

    clearMarkers: function () {
        if (this.markers) {
            this.markers.forEach(m => this.map.removeLayer(m));
        }
        this.markers = [];
    },

    // ✅ Добавлен аргумент shouldBeOpen
    addMarker: function (id, lat, lon, price, reservedBy, status, shouldBeOpen) {
        if (!this.map) return;

        const marker = L.marker([lat, lon]).addTo(this.map);

        let popupContent = "";

        if (status === 'Paid') {
            popupContent = `
                <div style="text-align:center; color: #6c757d;">
                    <b>Spot #${id}</b><br>
                    🏁 <strong>PAID / OCCUPIED</strong>
                </div>
            `;
            marker.setOpacity(0.5);
        }
        else if (reservedBy) {
            popupContent = `
                <div style="text-align:center; color: #dc3545;">
                    <b>Spot #${id}</b><br>
                    ❌ Reserved by: <strong>${reservedBy}</strong>
                </div>
            `;
        } else {
            popupContent = `
                <div style="text-align:center">
                    <b>Spot #${id}</b><br>
                    Price: $${price}/hr<br>
                    <span style="color: green; font-weight: bold;">Available</span>
                </div>
            `;
        }

        marker.bindPopup(popupContent);

        // Обработка клика
        if (status !== 'Paid') {
            marker.on('click', () => {
                this.dotNetRef.invokeMethodAsync('OnSpotSelected', id);
            });
        }

        this.markers.push(marker);

        // ✅ Явное открытие, если Blazor сказал, что этот спот выбран
        if (shouldBeOpen) {
            setTimeout(() => marker.openPopup(), 50);
        }
    },

    getUserLocation: function () {
        return new Promise((resolve, reject) => {
            if (!navigator.geolocation) {
                reject("Geolocation is not supported by your browser");
            } else {
                navigator.geolocation.getCurrentPosition(
                    (position) => {
                        resolve({
                            lat: position.coords.latitude,
                            lon: position.coords.longitude
                        });
                    },
                    (error) => {
                        reject("Unable to retrieve your location");
                    }
                );
            }
        });
    },

    setView: function (lat, lon) {
        if (this.map) {
            this.map.flyTo([lat, lon], 13);
        }
    }
};