function openWebSocket(device, onmessage) {
    var wsUri = ((window.location.protocol === "https:") ? "wss://" : "ws://") + window.location.host + "/sensordata/" + device;
    webSocket = new WebSocket(wsUri);
    webSocket.onmessage = onmessage;
}

function start(device, messages) {
    openWebSocket(device, function(e) {
        var li = document.createElement("li");
        li.appendChild(document.createTextNode(e.data));
        messages.appendChild(li);
    });
}

window.onload = function() {
    start("iskra", document.getElementById("messages"));
};