document.addEventListener("DOMContentLoaded", function () {
    const map = L.map('map').setView([10.7760, 106.6990], 14);

    L.tileLayer('https://{s}.basemaps.cartocdn.com/light_all/{z}/{x}/{y}{r}.png', {
        attribution: '&copy; OpenStreetMap'
    }).addTo(map);

    // --- HÀM VẼ ĐƯỜNG CHẠY THỰC TẾ TRÊN PHỐ ---
    async function drawStreetRoute(path, color, name) {
        // Chuyển đổi tọa độ sang định dạng OSRM (lng,lat)
        const osrmCoords = path.map(p => `${p[1]},${p[0]}`).join(';');
        const url = `https://router.project-osrm.org/route/v1/driving/${osrmCoords}?overview=full&geometries=geojson`;

        try {
            const response = await fetch(url);
            const data = await response.json();
            if (data.routes && data.routes.length > 0) {
                const coordinates = data.routes[0].geometry.coordinates.map(c => [c[1], c[0]]);

                L.polyline(coordinates, {
                    color: color,
                    weight: 6,
                    opacity: 0.8,
                    lineJoin: 'round'
                }).addTo(map).bindTooltip(name);
            }
        } catch (error) {
            console.error("Lỗi vẽ đường thực tế:", error);
        }
    }

    // 3. DỮ LIỆU MẪU: CÁC TUYẾN ĐƯỜNG (Chỉ cần điểm đầu, điểm giữa và điểm cuối)
    const routes = [
        {
            name: "Tuyến 01: Bến Thành - Chợ Lớn",
            color: "#4e73df",
            path: [[10.7712, 106.6976], [10.7620, 106.6820], [10.7550, 106.6700]]
        },
        {
            name: "Tuyến 15: Lê Thánh Tôn - Tân Định",
            color: "#e74a3b",
            path: [[10.7785, 106.6995], [10.7850, 106.6950], [10.7920, 106.6920]]
        },
        {
            name: "Tuyến 31: Đồng Khởi - Bình Thạnh",
            color: "#f6c23e",
            path: [[10.7760, 106.7020], [10.7820, 106.7080], [10.7950, 106.7150]]
        },
        {
            name: "Tuyến 45: Hàm Nghi - Quận 4",
            color: "#1cc88a",
            path: [[10.7700, 106.7030], [10.7650, 106.7050], [10.7580, 106.7080]]
        },
        {
            name: "Tuyến 06: Quận 5 - Đại học Y Dược",
            color: "#6610f2", // Tím
            path: [[10.7580, 106.6750], [10.7550, 106.6650], [10.7520, 106.6580]]
        },
        {
            name: "Tuyến 72: Quận 1 - Phú Mỹ Hưng (Q7)",
            color: "#fd7e14", // Cam
            path: [[10.7680, 106.7050], [10.7500, 106.7150], [10.7300, 106.7200]]
        },
        {
            name: "Tuyến 03: Công viên 23/9 - Thảo Cầm Viên",
            color: "#20c997", // Xanh ngọc
            path: [[10.7695, 106.6930], [10.7750, 106.7000], [10.7880, 106.7050]]
        }
    ];

    // Vẽ từng tuyến uốn lượn theo đường phố
    routes.forEach(route => {
        drawStreetRoute(route.path, route.color, route.name);
    });

    // 4. VỊ TRÍ XE (Giữ nguyên phần Marker của bạn)
    const vehicles = [
        { id: "51B-123.45", lat: 10.7620, lng: 106.6820, status: "Đúng giờ", route: "Tuyến 01" },
        { id: "51B-999.99", lat: 10.7850, lng: 106.6950, status: "Trễ 5p", route: "Tuyến 15" },
        { id: "51B-888.22", lat: 10.7820, lng: 106.7080, status: "Đang đón khách", route: "Tuyến 31" },
        { id: "51B-444.11", lat: 10.7650, lng: 106.7050, status: "Đúng giờ", route: "Tuyến 45" },
        { id: "51B-555.66", lat: 10.7550, lng: 106.6650, status: "Đúng giờ", route: "Tuyến 06" },
        { id: "51B-777.88", lat: 10.7400, lng: 106.7180, status: "Trễ 12p", route: "Tuyến 72" },
        { id: "51B-222.33", lat: 10.7750, lng: 106.7000, status: "Đúng giờ", route: "Tuyến 03" },
        { id: "51B-333.44", lat: 10.7600, lng: 106.6780, status: "Sự cố kỹ thuật", route: "Tuyến 01" }
    ];

    vehicles.forEach(bus => {
        const statusColor = bus.status.includes("Trễ") ? "#dc3545" : "#198754";
        const customIcon = L.divIcon({
            html: `<div class="custom-bus-icon" style="background-color: ${statusColor}; border: 2px solid white; border-radius: 50%; display: flex; align-items: center; justify-content: center; width: 30px; height: 30px; box-shadow: 0 2px 5px rgba(0,0,0,0.3);">
                     <i class="fas fa-bus" style="color: white; font-size: 12px;"></i>
                   </div>`,
            className: '',
            iconSize: [30, 30],
            iconAnchor: [15, 15]
        });

        L.marker([bus.lat, bus.lng], { icon: customIcon })
            .addTo(map)
            .bindPopup(`<b>Xe: ${bus.id}</b><br>Trạng thái: ${bus.status}`);
    });
});

document.addEventListener("DOMContentLoaded", function () {
    // --- 1. TRAFFIC FLOW CHART (Chart.js) ---
    const ctx = document.getElementById('trafficChart').getContext('2d');

    // Tạo gradient cho biểu đồ giống hình mẫu
    const gradient = ctx.createLinearGradient(0, 0, 0, 400);
    gradient.addColorStop(0, 'rgba(28, 200, 138, 0.4)');
    gradient.addColorStop(1, 'rgba(28, 200, 138, 0)');

    new Chart(ctx, {
        type: 'line',
        data: {
            labels: ['00:00', '04:00', '08:00', '12:00', '16:00', '20:00', '23:00'],
            datasets: [{
                label: 'Passenger Volume',
                data: [150, 100, 850, 600, 950, 700, 300], // Dữ liệu khớp với biểu đồ mẫu
                borderColor: '#1cc88a',
                backgroundColor: gradient,
                fill: true,
                tension: 0.4,
                pointRadius: 4,
                pointBackgroundColor: '#1cc88a'
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: { display: false }
            },
            scales: {
                x: { grid: { display: false } },
                y: {
                    beginAtZero: true,
                    grid: { color: '#f8f9fa' }
                }
            }
        }
    });

    // --- 2. RECENT TRIP STATUS DATA ---
    const trips = [
        { route: "Route 15", status: "Arrived", delay: "On Time", color: "success" },
        { route: "Route 1B", status: "En Route", delay: "+3 min", color: "warning" },
        { route: "Route 45", status: "Delayed", delay: "+12 min", color: "danger" },
        { route: "Route 06", status: "Arrived", delay: "On Time", color: "success" },
        { route: "Route 72", status: "En Route", delay: "On Time", color: "success" }
    ];

    const tableBody = document.getElementById('tripStatusBody');
    trips.forEach(trip => {
        const row = `
            <tr class="border-bottom">
                <td class="ps-4 py-3">
                    <span class="fw-bold text-primary">${trip.route}</span>
                </td>
                <td>
                    <span class="badge rounded-pill bg-light text-dark border">${trip.status}</span>
                </td>
                <td class="pe-4 text-end">
                    <span class="text-${trip.color} small fw-bold">${trip.delay}</span>
                </td>
            </tr>
        `;
        tableBody.innerHTML += row;
    });
});