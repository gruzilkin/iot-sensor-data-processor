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

            document.getElementById("temperature").innerText = `${message.Temperature} C` 
        }

        if (message.Humidity) {
            var newHumidityPoint = {
                name: message.ReceivedAt,
                value: [message.ReceivedAt, message.Humidity]
            }
            humidity.push(newHumidityPoint)

            document.getElementById("humidity").innerText = `${message.Humidity} %`
        }

        if (message.Ppm) {
            var newPpmPoint = {
                name: message.ReceivedAt,
                value: [message.ReceivedAt, message.Ppm]
            }
            ppm.push(newPpmPoint)

            document.getElementById("ppm").innerText = `${message.Ppm} ppm`
        }

        if (message.Voc) {
            var newVocPoint = {
                name: message.ReceivedAt,
                value: [message.ReceivedAt, message.Voc]
            }
            voc.push(newVocPoint)

            document.getElementById("voc").innerText = message.Voc
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
                    },
                    {
                        data: voc
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
var voc = [];

var updateId;

var option = {
    color: ['green', 'lightblue', 'red', 'orange'],
    legend: {
        data: ['Temperature', 'Humidity', 'Ppm', 'Voc']
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
            scale: true,
            position: 'left'
        },
        {
            type: 'value',
            scale: true,
            position: 'left',
            offset: 30,
        },
        {
            type: 'value',
            name: 'Ppm',
            scale: true,
            position: 'right'
        },
        {
            type: 'value',
            name: 'Voc',
            scale: true,
            position: 'left',
            offset: 60,
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
            yAxisIndex: 0
        },
        {
            name: 'Humidity',
            type: 'line',
            showSymbol: false,
            hoverAnimation: false,
            data: humidity,
            yAxisIndex: 1
        },
        {
            name: 'Ppm',
            type: 'line',
            showSymbol: false,
            hoverAnimation: false,
            data: ppm,
            yAxisIndex: 2
        },
        {
            name: 'Voc',
            type: 'line',
            showSymbol: false,
            hoverAnimation: false,
            data: voc,
            yAxisIndex: 3
        }
    ]
};

window.onload = function() {
    start("zero");

    chart = echarts.init(document.getElementById('chart'));
    chart.setOption(option);
};