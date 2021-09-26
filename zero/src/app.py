import pika, json, os

import time
import board
import busio
import adafruit_scd30

def main():
    host = os.environ['RABBITMQ_HOST']
    print(f"RabbitMQ at {host}")
    device_id = os.environ['DEVICE_ID']
    print(f"Device ID is {device_id}")

    i2c = busio.I2C(board.SCL, board.SDA, frequency=50000)
    scd = adafruit_scd30.SCD30(i2c)

    with pika.BlockingConnection(pika.ConnectionParameters(host=host)) as connection:
        while True:
            if scd.data_available:
                data = {"ppm":scd.CO2, "temperature":scd.temperature, "humidity":scd.relative_humidity}
                channel = connection.channel()
                channel.basic_publish('amq.topic',
                      f"sensor/live/data/{device_id}",
                      json.dumps(data))

            time.sleep(0.5)

main()