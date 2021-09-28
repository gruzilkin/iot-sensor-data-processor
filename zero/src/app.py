import pika, json, os

from retry import retry

import time
import board
import busio
import adafruit_scd30

@retry(pika.exceptions.AMQPConnectionError, delay=10, tries=3)
def main():
    host = os.environ['RABBITMQ_HOST']
    print(f"RabbitMQ at {host}")
    device_id = os.environ['DEVICE_ID']
    print(f"Device ID is {device_id}")

    i2c = busio.I2C(board.SCL, board.SDA, frequency=50000)
    scd = adafruit_scd30.SCD30(i2c)

    with pika.BlockingConnection(pika.ConnectionParameters(host=host)) as connection:
        with connection.channel() as channel:
            while True:
                if scd.data_available:
                    data = {"ppm":scd.CO2, "temperature":scd.temperature, "humidity":scd.relative_humidity}
                    channel.basic_publish(exchange='amq.topic', routing_key=f"sensor.data.{device_id}", body=json.dumps(data))

                time.sleep(0.5)

main()