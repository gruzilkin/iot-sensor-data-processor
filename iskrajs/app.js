var SSID = 'WIFI SSID';
var PSWD = 'WIFI PASSWORD';

var malinaConfig = {MQTT_SERVER: "192.168.11.4", DEVICE_ID: "iskra", CALIBRATION: false};
var pcConfig = {MQTT_SERVER: "192.168.11.17", DEVICE_ID: "iskra", CALIBRATION: false};
var configSwitch = true;
var config = malinaConfig;


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
  gasSensor.heat(false);
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
      sendTemperatureAndHumidity();
      }
  });

  mqtt.on("message", function(msg){
      console.log(JSON.stringify(msg));
  });

  mqtt.on("publish", function(pub){
      console.log(JSON.stringify(pub));
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

function sendTemperatureAndHumidity() {
  sender_id = setInterval(function() {
  dht.read(function (a) {
    console.log(a);
    var temperatureData = {temperature: a.temp};
    var humidityData = {humidity: a.rh};
    mqtt.publish("temperature/" + config.DEVICE_ID, JSON.stringify(temperatureData));
    mqtt.publish("humidity/" + config.DEVICE_ID, JSON.stringify(humidityData));
  });
  }, 1000 * 1);
}

function sendCalibration() {
  gasSensor.preheat(function() {
    sender_id = setInterval(function() {
      dht.read(function (a) {
        var r0 = gasSensor.calibrate();
        var ppm = gasSensor.read();
        var calibrationData = {temperature: a.temp, humidity: a.rh, r0: r0, ppm: ppm };
        console.log(calibrationData);
        mqtt.publish("calibration/" + config.DEVICE_ID, JSON.stringify(calibrationData));
      });
      }, 1000 * 1);
  });
}