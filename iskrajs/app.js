var SSID = 'Gruzilkin';
var PSWD = 'bakabaka123';

var malinaConfig = {MQTT_SERVER: "192.168.11.7", DEVICE_ID: "iskra", CALIBRATION: false};
var pcConfig = {MQTT_SERVER: "192.168.11.17", DEVICE_ID: "iskra", CALIBRATION: false};
var configSwitch = true;
var config = malinaConfig;

var startTime = new Date();


// button click causes configuration switch
require('@amperka/button').connect(C4).on('press', function() {
  console.log("confguration button press event");
  configSwitch = !configSwitch;
  config = configSwitch ? malinaConfig : pcConfig;
  LED1.write(config.CALIBRATION);
  shutdownMqtt();
});
require('@amperka/button').connect(A3).on('press', function() {
  console.log("calibration button press event");
  config.CALIBRATION = !config.CALIBRATION;
  LED1.write(config.CALIBRATION);
  shutdownMqtt();
});


var sender_id;
var mqtt_start_id;
var mqtt;
var r0_calibration = {};


PrimarySerial.setup(115200);
var wifi = require('@amperka/wifi').setup(PrimarySerial, function(err) {
  if (err) {
    print(err);
    return;
  }
  else {
    print('wifi init completed');
  }

  wifi.connect(SSID, PSWD, function(err) {
    if (err) {
      print(err);
      return;
    } else {
      print('wifi connected');
    }

    // once wifi is connected initialize MQTT
    delayedInitMqtt();
  });
});


function shutdownMqtt() {
  if(mqtt) {
    console.log("shutting down MQTT");
    mqtt.disconnect();
  }
}

function delayedInitMqtt() {
  if (mqtt_start_id) {
    clearInterval(mqtt_start_id);
  }
  if (sender_id) {
    clearInterval(sender_id);
  }
  setTimeout(function() {
    initMqtt();
  }, 1000);
}

function initMqtt() {
  console.log("MQTT init with ", config);

  var options = {
    client_id : config.DEVICE_ID
  };
  mqtt = require("MQTT").create(config.MQTT_SERVER, options);

  mqtt.on("connected", function(){
      console.log("MQTT connected");
      console.log("calibration " + config.CALIBRATION);
      if (config.CALIBRATION) {
        sendCalibration();
      }
      else {
        mqtt.subscribe("sensor/calibration/response/" + config.DEVICE_ID);
        sendLiveData();
      }
  });

  mqtt.on("message", function(topic, msg){
    console.log("calibration data received: ", msg);
    var response = JSON.parse(msg);
    var calibrationKey = response.temperature.toString() + '-' + response.humidity.toString();
    r0_calibration[calibrationKey] = response.r0;
  });

  mqtt.on("disconnected", function(){
      console.log("MQTT disconnected");
      delayedInitMqtt();
  });

  mqtt.connect();
}

var dht = require("DHT22").connect(P3);
var gasSensor = require('@amperka/gas-sensor').connect({
  dataPin: A1, // разъём SVG
  heatPin: P12, // разъём GHE
  model: 'MQ135'
});
gasSensor.heat(true);

function sendLiveData() {
  sender_id = setInterval(function() {
    dht.read(function (a) {
      var data = {temperature: a.temp, humidity: a.rh};
      var calibrationKey = data.temperature.toString() + '-' + data.humidity.toString();
      if (r0_calibration[calibrationKey]) {
        gasSensor.calibrate(r0_calibration[calibrationKey]);
        var ppm = gasSensor.read();
        data.ppm = ppm;
      }
      else {
        console.log("request new calibration", data);
        mqtt.publish("sensor/calibration/request/" + config.DEVICE_ID, JSON.stringify(data));
      }
      console.log(data);
      mqtt.publish("sensor/live/data/" + config.DEVICE_ID, JSON.stringify(data));
    });
  }, 1000 * 1);
}

function sendCalibration() {
  sender_id = setInterval(function() {
    dht.read(function (a) {
      var r0 = gasSensor.calibrate();
      var ppm = gasSensor.read();
      var uptime = Math.round(new Date() - startTime);
      var calibrationData = {temperature: a.temp, humidity: a.rh, r0: r0, ppm: ppm, uptime: uptime };
      console.log(calibrationData);
      mqtt.publish("sensor/calibration/data/" + config.DEVICE_ID, JSON.stringify(calibrationData));
    });
    }, 1000 * 1);
}