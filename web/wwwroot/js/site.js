function openWebSocket(device, onmessage) {
    var wsUri = ((window.location.protocol === "https:") ? "wss://" : "ws://") + window.location.host + "/sensordata/" + device;
    webSocket = new WebSocket(wsUri);
    webSocket.onmessage = onmessage;
}

function start(device) {
    openWebSocket(device, function(e) {
        var message = JSON.parse(e.data)
        console.log(message)

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

        if (updateId) {
            clearTimeout(updateId)
        }
        updateId = setTimeout(function() {
            chart.setOption({
                series: [
                    {
                        data: temperature
                    },
                    {
                        data: humidity
                    },
                    {
                        data: ppm
                    }
                ]
            })
        }, 0)
        
    });
}

var chart;
var temperature = [];
var humidity = [];
var ppm = [];

var updateId;

var option = {
    legend: {
        data: ['Temperature', 'Humidity', 'Ppm']
      },
    tooltip: {
        trigger: 'axis',
        formatter: function (params) {
            return params.map(p => p.value).map(v => v[1]).join(' / ')
        },
        axisPointer: {
            type: 'cross'
        }
    },
    xAxis: {
        type: 'time'
    },
    yAxis: [
        {
            type: 'value',
            name: 'Temperature',
            scale: true,
            position: 'left'
        },
        {
            type: 'value',
            name: 'Humidity',
            scale: true,
            position: 'left',
            offset: 60,
        },
        {
            type: 'value',
            name: 'Ppm',
            scale: true,
            position: 'right'
        }
    ],
    dataZoom: [
        {
            type: 'slider'
        },
        {
            type: 'inside'
        }
    ],
    title: [
        {
            text: 'Sensor data'
        }
    ],
    series: [
        {
            name: 'Temperature',
            type: 'line',
            showSymbol: false,
            hoverAnimation: false,
            data: temperature,
            yAxisIndex: 0,
            lineStyle: {
                color: '#00FF00'
            }
        },
        {
            name: 'Humidity',
            type: 'line',
            showSymbol: false,
            hoverAnimation: false,
            data: humidity,
            yAxisIndex: 1,
            lineStyle: {
                color: '#0000FF'
            }
        },
        {
            name: 'Ppm',
            type: 'line',
            showSymbol: false,
            hoverAnimation: false,
            data: ppm,
            yAxisIndex: 2,
            lineStyle: {
                color: '#FF0000'
            }
        }
    ]
};

window.onload = function() {
    start("zero");

    chart = echarts.init(document.getElementById('chart'));
    chart.setOption(option);
};