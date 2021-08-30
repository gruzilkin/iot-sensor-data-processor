function openWebSocket(queue, device, onmessage) {
    var wsUri = ((window.location.protocol === "https:") ? "wss://" : "ws://") + window.location.host + "/sensordata/" + queue + "/" + device;
    webSocket = new WebSocket(wsUri);
    webSocket.onmessage = onmessage;
}

function start(queue, device, messages) {
    openWebSocket(queue, device, function(e) {
        var li = document.createElement("li");
        li.appendChild(document.createTextNode(e.data));
        messages.appendChild(li);
    });
}