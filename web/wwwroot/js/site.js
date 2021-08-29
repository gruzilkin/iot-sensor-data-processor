function openWebSocket(device, onmessage) {
    var wsUri;
    if (window.location.protocol === "https:") {
        wsUri = "wss:";
    } else {
        wsUri = "ws:";
    }
    wsUri += "//" + window.location.host;
    wsUri += window.location.pathname + "sensordata/" + device;

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