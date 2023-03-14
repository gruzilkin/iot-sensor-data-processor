import os, json, asyncio
import aio_pika
import board, busio, adafruit_scd30, adafruit_sgp40

i2c = busio.I2C(board.SCL, board.SDA, frequency=50000)
scd = adafruit_scd30.SCD30(i2c)
sgp = adafruit_sgp40.SGP40(i2c)

temperature, humidity = None, None

async def sender(queue):
    host = os.environ['RABBITMQ_HOST']
    print(f"RabbitMQ at {host}")
    device_id = os.environ['DEVICE_ID']
    print(f"Device ID is {device_id}")

    user = os.environ['RABBITMQ_USER']
    password = os.environ['RABBITMQ_PASS']

    connection =  await aio_pika.connect(host=host, login=user, password=password)
    async with connection:
        channel = await connection.channel()
        async with channel:
            topic = await channel.get_exchange("amq.topic")
            while True:
                name, data = await queue.get()
                await topic.publish(
                    aio_pika.Message(body=json.dumps(data).encode()),
                    routing_key=f"sensor.{name}.{device_id}")
                print(data)

async def read_sgp40(queue):
    while True:
        if temperature and humidity:
            voc_index = sgp.measure_index(temperature=temperature, relative_humidity=humidity)
            if voc_index != 0:
                data = {"voc":voc_index}
                await queue.put(("sgp40", data))
        await asyncio.sleep(1)

async def read_scd30(queue):
    while True:
        if scd.data_available:
            global temperature, humidity
            temperature = scd.temperature
            humidity = scd.relative_humidity
            data = {"ppm":scd.CO2, "temperature":scd.temperature, "humidity":scd.relative_humidity}
            await queue.put(("scd30", data))
        await asyncio.sleep(2.1)

def init_sensors():
    temperature_offset = os.environ['TEMPERATURE_OFFSET']
    if temperature_offset:
        scd.temperature_offset = int(temperature_offset)

    altitude = os.environ['ALTITUDE']
    if altitude:
        scd.altitude = int(altitude)

    print("SCD30 Temperature offset: ", scd.temperature_offset)
    print("SCD30 Measurement interval: ", scd.measurement_interval)
    print("SCD30 Self-calibration enabled: ", scd.self_calibration_enabled)
    print("SCD30 Ambient Pressure: ", scd.ambient_pressure)
    print("SCD30 Altitude: ", scd.altitude, " meters above sea level")

async def main():
    init_sensors()

    try:
        queue = asyncio.Queue()
        coros = [read_sgp40(queue), read_scd30(queue), sender(queue)]
        tasks = [asyncio.create_task(coro) for coro in coros]
        await asyncio.gather(*tasks)
    finally:
        for task in tasks:
            task.cancel()
        await asyncio.gather(*tasks, return_exceptions=True)

asyncio.run(main())