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

            temperatureChart.setOption({
                series: {
                    data: temperature
                }
            });
        }

        if (message.Humidity) {
            var newHumidityPoint = {
                name: message.ReceivedAt,
                value: [message.ReceivedAt, message.Humidity]
            }
            humidity.push(newHumidityPoint)

            humidityChart.setOption({
                series: {
                    data: humidity
                }
            });
        }

        if (message.Ppm) {
            var newPpmPoint = {
                name: message.ReceivedAt,
                value: [message.ReceivedAt, message.Ppm]
            }
            ppm.push(newPpmPoint)

            ppmChart.setOption({
                series: {
                    data: ppm
                }
            });
        }
    });
}

var temperatureChart;
var humidityChart;
var ppmChart;
var temperature = [];
var humidity = [];
var ppm = [];

var baseOption = {
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
        min: function (value) {
            return value.max - 5 * 60 * 1000;
        },
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
    }
};

var temperatureOption = {
    title: {
        text: 'Temperature'
    },
    series: {
        name: 'temperature',
        type: 'line',
        showSymbol: false,
        hoverAnimation: false,
        data: temperature,
        lineStyle: {
            color: '#00FF00'
        }
    }
};

var humidityOption = {
    title: {
        text: 'Humidity'
    },
    series: {
        name: 'humidity',
        type: 'line',
        showSymbol: false,
        hoverAnimation: false,
        data: humidity,
        lineStyle: {
            color: '#0000FF'
        }
    }
};

var ppmOption = {
    title: {
        text: 'Ppm'
    },
    series: {
        name: 'ppm',
        type: 'line',
        showSymbol: false,
        hoverAnimation: false,
        data: ppm,
        lineStyle: {
            color: '#FF0000'
        }
    }
};

window.onload = function() {
    start("iskra", document.getElementById("messages"));

    temperatureChart = echarts.init(document.getElementById('temperatureChart'));
    temperatureChart.setOption(baseOption);
    temperatureChart.setOption(temperatureOption);
    humidityChart = echarts.init(document.getElementById('humidityChart'));
    humidityChart.setOption(baseOption);
    humidityChart.setOption(humidityOption);
    ppmChart = echarts.init(document.getElementById('ppmChart'));
    ppmChart.setOption(baseOption);
    ppmChart.setOption(ppmOption);
};