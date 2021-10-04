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
            while True:
                name, data = await queue.get()
                topic = await channel.get_exchange("amq.topic")
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

async def main():
    try:
        queue = asyncio.Queue()
        coros = [read_sgp40(queue), read_scd30(queue), sender(queue)]
        tasks = [asyncio.create_task(coro) for coro in coros]
        await asyncio.gather(*tasks)
    except Exception:
        print("shutting down")
        for task in tasks:
            task.cancel()
        print("cancelling tasks")
        await asyncio.gather(*tasks, return_exceptions=True)
        print("tasks cancelled")

asyncio.run(main())