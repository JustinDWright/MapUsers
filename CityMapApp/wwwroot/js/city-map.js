window.cityMapApp = window.cityMapApp || {};

window.cityMapApp.renderMap = function renderMap(elementId, pins) {
  if (!window.L) {
    return;
  }

  const root = document.getElementById(elementId);
  if (!root) {
    return;
  }

  root.innerHTML = "";
  const map = L.map(root).setView([39.8283, -98.5795], 4);

  L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
    maxZoom: 19,
    attribution: "&copy; OpenStreetMap contributors"
  }).addTo(map);

  const markerLayer = L.markerClusterGroup ? L.markerClusterGroup() : L.layerGroup();

  pins.forEach(function (pin) {
    const marker = L.marker([pin.latitude, pin.longitude]);
    marker.bindPopup(pin.city + ", " + pin.state + "<br/>" + pin.count + " submissions");
    markerLayer.addLayer(marker);
  });

  markerLayer.addTo(map);
};