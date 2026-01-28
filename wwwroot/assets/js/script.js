//leadflet
let busRouteControl;
if (typeof L !== "undefined") {
    var map = L.map('map').setView([10.7712, 106.6980], 14);

    // 2. Thêm lớp nền bản đồ từ OpenStreetMap
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '&copy; OpenStreetMap contributors'
    }).addTo(map);

    // 3. Tạo icon tùy chỉnh (Màu vàng cho hợp với template của bạn)
    var busIcon = L.divIcon({
        html: '<i class="fas fa-bus" style="color: #ffb606; font-size: 24px;"></i>',
        iconSize: [30, 30],
        className: 'my-custom-icon'
    });

    // 4. Đánh dấu Điểm đi (Bến Thành) và Điểm đến (Chợ Lớn)
    var startMarker = L.marker([10.7712, 106.6980]).addTo(map)
        .bindPopup('<b>Điểm đi:</b> Bến Thành').openPopup();

    var endMarker = L.marker([10.7535, 106.6545]).addTo(map)
        .bindPopup('<b>Điểm đến:</b> Chợ Lớn');

    // 5. Vẽ đường lộ trình (Polyline) - Bạn có thể lấy danh sách tọa độ từ Database
    var latlngs = [
        [10.7712, 106.6980],
        [10.7690, 106.6850],
        [10.7600, 106.6700],
        [10.7535, 106.6545]
    ];

    // 5. Vẽ đường lộ trình tự động bám theo mặt đường
    busRouteControl = L.Routing.control({
        waypoints: [
            L.latLng(10.7712, 106.6980), // Điểm đi: Bến Thành
            L.latLng(10.7535, 106.6545) // Điểm đến: Chợ Lớn
        ],
        lineOptions: {
            styles: [{
                color: '#3b82f6',
                weight: 6,
                opacity: 0.8
            }]
        },
        router: L.Routing.osrmv1({
            serviceUrl: 'https://router.project-osrm.org/route/v1', // Server tính toán đường đi miễn phí
            profile: 'car'
        }),
        createMarker: function () {
            return null;
        },
        addWaypoints: false,
        routeWhileDragging: false,
        show: false,
        fitSelectedRoutes: true
    }).addTo(map);


    // Tạo icon chấm xanh (vị trí của bạn)
    var userIcon = L.divIcon({
        html: '<i class="fas fa-circle" style="color: #3b82f6; font-size: 16px; border: 3px solid white; border-radius: 50%; box-shadow: 0 0 5px rgba(0,0,0,0.5);"></i>',
        iconSize: [20, 20],
        className: 'user-location-icon'
    });

    var userMarker = L.marker([0, 0], {
        icon: userIcon
    }).addTo(map);

    map.on('locationfound', function (e) {
        var radius = e.accuracy / 2; // Độ sai số của GPS

        // Cập nhật vị trí chấm xanh
        userMarker.setLatLng(e.latlng);

        // Vẽ thêm một vòng tròn mờ xung quanh để thể hiện độ chính xác của GPS
        if (!window.accuracyCircle) {
            window.accuracyCircle = L.circle(e.latlng, radius, {
                color: '#3b82f6',
                fillColor: '#3b82f6',
                fillOpacity: 0.15,
                weight: 1
            }).addTo(map);
        } else {
            window.accuracyCircle.setLatLng(e.latlng);
            window.accuracyCircle.setRadius(radius);
        }
    });

    map.on('locationerror', function (e) {
        console.error("Không thể lấy vị trí của bạn: " + e.message);
    });
}
// end leadflet

// start navigation 
let isNavigating = false;
let routingToStation = null;
const buttonStart = document.querySelector("[button-start]");
const btnToggle = document.getElementById("btn-toggle-directions");

if (buttonStart) {
    buttonStart.addEventListener("click", function () {
        if (!isNavigating) {
            alert("Đang tìm đường đi bộ ra trạm...");

            map.locate({
                setView: true,
                watch: true,
                enableHighAccuracy: true
            });

            map.once('locationfound', function (e) {
                const userLocation = e.latlng;
                const startStation = L.latLng(10.7712, 106.6980);

                if (routingToStation) {
                    map.removeControl(routingToStation);
                }

                routingToStation = L.Routing.control({
                    waypoints: [userLocation, startStation],
                    lineOptions: {
                        styles: [{
                            color: '#3b82f6',
                            weight: 5,
                            opacity: 0.8,
                            dashArray: '10, 10'
                        }]
                    },
                    router: L.Routing.osrmv1({
                        profile: 'foot'
                    }),
                    createMarker: function () {
                        return null;
                    },
                    addWaypoints: false,
                    show: true
                }).addTo(map);

                let globalBusDistance = 0;

                busRouteControl.on('routesfound', function (e) {
                    globalBusDistance = e.routes[0].summary.totalDistance;
                    console.log("Đã lấy được chiều dài tuyến xe: " + globalBusDistance + "m");
                });

                routingToStation.on('routesfound', function (e) {
                    const routes = e.routes;
                    const summary = routes[0].summary;

                    // 1. Lấy dữ liệu từ OSRM (đơn vị: mét và giây)
                    const distance = Math.round(summary.totalDistance);
                    const time = Math.ceil(distance / 80);
                    const timeOnBus = Math.ceil(globalBusDistance / 333);
                    const timeWait = 5;

                    const totalDuration = time + timeWait + timeOnBus;

                    const listDistanceSpan = document.querySelectorAll("span[distance]");
                    const timeSpan = document.querySelector(".timeline-item .time");

                    if (listDistanceSpan.length > 0) {
                        listDistanceSpan.forEach(span => {
                            span.innerText = distance >= 1000 ?
                                (distance / 1000).toFixed(1) + " km" :
                                distance + " m";
                        })
                    }

                    if (timeSpan) {
                        timeSpan.innerText = time >= 60 ?
                            Math.floor(time / 60) + " hour " + (time % 60) + " minute" :
                            time + " minute";
                    }

                    const durationSummary = document.getElementById("total-duration");
                    if (durationSummary) {
                        durationSummary.innerText = totalDuration >= 60 ?
                            Math.floor(totalDuration / 60) + " hour " + (totalDuration % 60) + " minute" :
                            totalDuration + " minute";
                    }
                });

                setTimeout(() => {
                    const container = document.querySelector('.leaflet-right');
                    if (container) container.classList.add('routing-hidden');
                    btnToggle.classList.remove('d-none');
                }, 500);
            });

            this.innerHTML = "Stop Navigation";
            this.classList.replace("btn-warning", "btn-danger");
            isNavigating = true;

        } else {
            map.stopLocate();
            if (routingToStation) {
                map.removeControl(routingToStation);
                routingToStation = null;
            }

            map.setView([10.7712, 106.6980], 14);

            btnToggle.classList.add('d-none');
            this.innerHTML = "Start Navigation";
            this.classList.replace("btn-danger", "btn-warning");
            isNavigating = false;
        }
    });
}

// Logic cho nút Bật/Tắt bảng hướng dẫn
if (btnToggle) {
    btnToggle.addEventListener("click", function () {
        const container = document.querySelector('.leaflet-right');
        if (container) {
            container.classList.toggle('routing-hidden');
            this.innerHTML = container.classList.contains('routing-hidden') ?
                '<i class="fas fa-list-ul me-1"></i> See detailed instructions.' :
                '<i class="fas fa-times me-1"></i> Close instructions';
        }
    });
}
// end start navigation 