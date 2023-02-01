import os, json, asyncio
import aio_pika
import adafruit_scd30, adafruit_sgp40

class Worker:
    def __init__(self, host = None, port = None, device_id = None, user = None, password = None, scd: adafruit_scd30.SCD30 = None, sgp: adafruit_sgp40.SGP40 = None):
        if scd and sgp:
            self.scd = scd
            self.sgp = sgp
        else:
            import board, busio
            i2c = busio.I2C(board.SCL, board.SDA, frequency=50000)
            self.scd = adafruit_scd30.SCD30(i2c)
            self.sgp = adafruit_sgp40.SGP40(i2c)
        self.temperature, self.humidity = None, None

        self.host = host if host is not None else os.environ['RABBITMQ_HOST']
        print(f"RabbitMQ at {self.host}")
        self.port = port if port is not None else os.environ['RABBITMQ_PORT']
        print(f"RabbitMQ at {self.port}")
        self.device_id = device_id if device_id is not None else os.environ['DEVICE_ID']
        print(f"Device ID is {self.device_id}")

        self.user = user if user is not None else os.environ['RABBITMQ_USER']
        self.password = password if password is not None else os.environ['RABBITMQ_PASS']


    async def sender(self, queue):
        connection =  await aio_pika.connect(host=self.host, port=self.port, login=self.user, password=self.password)
        async with connection:
            channel = await connection.channel()
            async with channel:
                topic = await channel.get_exchange("amq.topic")
                while True:
                    name, data = await queue.get()
                    await topic.publish(
                        aio_pika.Message(body=json.dumps(data).encode()),
                        routing_key=f"sensor.{name}.{self.device_id}")
                    print(data)

    async def read_sgp40(self, queue):
        while True:
            if self.temperature and self.humidity:
                voc_index = self.sgp.measure_index(temperature=self.temperature, relative_humidity=self.humidity)
                if voc_index != 0:
                    data = {"voc":voc_index}
                    await queue.put(("sgp40", data))
            await asyncio.sleep(1)

    async def read_scd30(self, queue):
        while True:
            if self.scd.data_available:
                self.temperature = self.scd.temperature
                self.humidity = self.scd.relative_humidity
                data = {"ppm":self.scd.CO2, "temperature":self.temperature, "humidity":self.humidity}
                await queue.put(("scd30", data))
            await asyncio.sleep(2.1)

    async def main(self):
        try:
            queue = asyncio.Queue()
            coros = [self.read_sgp40(queue), self.read_scd30(queue), self.sender(queue)]
            self.tasks = [asyncio.create_task(coro) for coro in coros]
            await asyncio.gather(*self.tasks, return_exceptions=True)
        except asyncio.exceptions.CancelledError:
            pass
        finally:
            await self.cancel()            
    
    async def cancel(self):
        try:
            for task in self.tasks:
                task.cancel()
            await asyncio.gather(*self.tasks, return_exceptions=True)
        except asyncio.exceptions.CancelledError:
            pass

if __name__ == '__main__':
    worker = Worker()
    asyncio.run(worker.main())