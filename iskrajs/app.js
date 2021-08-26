var SSID = 'WIFI SSID';
var PSWD = 'WIFI PASSWORD';

var malinaConfig = {MQTT_SERVER: "192.168.1.2"};
var pcConfig = {MQTT_SERVER: "192.168.1.3"};
var configSwitch = true;
var config = malinaConfig;

// button click causes configuration switch
require('@amperka/button').connect(C4).on('press', function() {
  configSwitch = !configSwitch;
  config = configSwitch ? malinaConfig : pcConfig;
  shutdownMqtt();
});


var DEVICE_ID = "iskra";
var interval_id;
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
    initMqtt();
  });
});


function shutdownMqtt() {
  if(mqtt) {
    console.log("shutting down MQTT");
    mqtt.disconnect();
  }
}

function initMqtt() {
  ensureNoSenderProcess();

  console.log("MQTT init with ", config);

  var options = {
    client_id : DEVICE_ID
  };
  mqtt = require("MQTT").create(config.MQTT_SERVER, options);

  mqtt.on("connected", function(){
      console.log("MQTT connected");
      sendTemperatureAndHumidity();
  });

  mqtt.on("message", function(msg){
      console.log(JSON.stringify(msg));
  });

  mqtt.on("publish", function(pub){
      console.log(JSON.stringify(pub));
  });

  mqtt.on("disconnected", function(){
      console.log("MQTT disconnected");
      initMqtt();
  });

  mqtt.connect();
}

function ensureNoSenderProcess() {
  if (interval_id)
  {
    clearInterval(interval_id);
  }
}

function sendTemperatureAndHumidity() {  
  var dht = require("DHT22").connect(P3);

  interval_id = setInterval(function() {
  dht.read(function (a) {
    console.log(a);
    var temperatureData = {device: DEVICE_ID, temperature: a.temp};
    var humidityData = {device: DEVICE_ID, humidity: a.rh};
    mqtt.publish("Temperature", JSON.stringify(temperatureData));
    mqtt.publish("Humidity", JSON.stringify(humidityData));
  });
  }, 1000 * 1);
}