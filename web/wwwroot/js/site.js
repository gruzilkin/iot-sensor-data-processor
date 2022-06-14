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
            chartData.temperature.push(newTemperaturePoint)

            document.getElementById("temperature").innerText = `${message.Temperature} C` 
        }

        if (message.Humidity) {
            var newHumidityPoint = {
                name: message.ReceivedAt,
                value: [message.ReceivedAt, message.Humidity]
            }
            chartData.humidity.push(newHumidityPoint)

            document.getElementById("humidity").innerText = `${message.Humidity} %`
        }

        if (message.Ppm) {
            var newPpmPoint = {
                name: message.ReceivedAt,
                value: [message.ReceivedAt, message.Ppm]
            }
            chartData.ppm.push(newPpmPoint)

            document.getElementById("ppm").innerText = `${message.Ppm} ppm`
        }

        if (message.Voc) {
            var newVocPoint = {
                name: message.ReceivedAt,
                value: [message.ReceivedAt, message.Voc]
            }
            chartData.voc.push(newVocPoint)

            document.getElementById("voc").innerText = message.Voc
        }

        renderChart();        
    });
}

var renderTaskId;

function renderChart() {
    if (renderTaskId) {
        clearTimeout(renderTaskId);
    }
    renderTaskId = setTimeout(function () {
        chart.setOption({
            series: [
                {
                    data: chartData.temperature
                },
                {
                    data: chartData.humidity
                },
                {
                    data: chartData.ppm
                },
                {
                    data: chartData.voc
                }
            ]
        });
    }, 1);
}

var chart;
var chartData = {temperature: [], humidity: [], ppm: [], voc: []}

var option = {
    animation: false,
    color: ['green', 'lightblue', 'red', 'orange'],
    legend: {
        data: ['Temperature', 'Humidity', 'Ppm', 'Voc']
      },
    tooltip: {
        trigger: 'axis',
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
            data: chartData.temperature,
            yAxisIndex: 0
        },
        {
            name: 'Humidity',
            type: 'line',
            showSymbol: false,
            hoverAnimation: false,
            data: chartData.humidity,
            yAxisIndex: 1
        },
        {
            name: 'Ppm',
            type: 'line',
            showSymbol: false,
            hoverAnimation: false,
            data: chartData.ppm,
            yAxisIndex: 2
        },
        {
            name: 'Voc',
            type: 'line',
            showSymbol: false,
            hoverAnimation: false,
            data: chartData.voc,
            yAxisIndex: 3
        }
    ]
};

window.addEventListener("load", function() {
    chart = echarts.init(document.getElementById('chart'));
    chart.setOption(option);
});