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

        var message = JSON.parse(e.data)

        if (message.Temperature) {
            var newTemperaturePoint = {
                name: message.ReceivedAt,
                value: [message.ReceivedAt, message.Temperature]
            }
            temperature.push(newTemperaturePoint)
        }

        if (message.Humidity) {
            var newHumidityPoint = {
                name: message.ReceivedAt,
                value: [message.ReceivedAt, message.Humidity]
            }
            humidity.push(newHumidityPoint)
        }

        if (message.Ppm) {
            var newPpmPoint = {
                name: message.ReceivedAt,
                value: [message.ReceivedAt, message.Ppm]
            }
            ppm.push(newPpmPoint)
        }

        myChart.setOption({
            series: [{
                data: temperature
            },
            {
                data: humidity
            },
            {
                data: ppm
            }]
        });
    });
}

var myChart;
var temperature = [];
var humidity = [];
var ppm = [];


var option = {
    title: {
        text: 'Sensor data'
    },
    tooltip: {
        trigger: 'axis',
        formatter: function (params) {
            return params.map(p => p.value).map(v => v[1]).join(' / ')
        },
        axisPointer: {
            animation: false
        }
    },
    xAxis: {
        type: 'time',
        splitLine: {
            show: false
        }
    },
    yAxis: {
        type: 'value',
        boundaryGap: [0, '100%'],
        splitLine: {
            show: false
        }
    },
    series: [{
        name: 'temperature',
        type: 'line',
        showSymbol: false,
        hoverAnimation: false,
        data: temperature
    },
    {
        name: 'humidity',
        type: 'line',
        showSymbol: false,
        hoverAnimation: false,
        data: humidity
    },
    {
        name: 'ppm',
        type: 'line',
        showSymbol: false,
        hoverAnimation: false,
        data: ppm
    }]
};

window.onload = function() {
    start("iskra", document.getElementById("messages"));

    var chartDom = document.getElementById('chart');
    myChart = echarts.init(chartDom);
    option && myChart.setOption(option);
};