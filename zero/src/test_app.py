import asyncio
import unittest
from unittest.mock import Mock

import aio_pika

from testcontainers.rabbitmq import RabbitMqContainer

from app import Worker

class TestApp(unittest.IsolatedAsyncioTestCase):
    async def read_rabbit(self, host, port, user, password) -> None:
        queue_name = "test_queue"
        connection =  await aio_pika.connect(host=host, port=port, login=user, password=password)
        async with connection:
            # Creating channel
            channel = await connection.channel()

            # Declaring queue
            queue = await channel.declare_queue(queue_name, auto_delete=True)

            await queue.bind(routing_key="sensor.sgp40.*", exchange="amq.topic")

            async with queue.iterator() as queue_iter:
                async for message in queue_iter:
                    async with message.process():
                        print(message.body)
                        self.cancel()

    async def delayed_cancellation(self, worker, sleep_in_seconds):
        await asyncio.sleep(sleep_in_seconds)
        self.cancel()

    def cancel(self):
        # get all running tasks
        tasks = asyncio.all_tasks()
        # get the current task
        current = asyncio.current_task()
        # remove current task from all tasks
        tasks.remove(current)
        # cancel all remaining running tasks
        for task in tasks:
            task.cancel()

    async def test_createWorker(self):
        with RabbitMqContainer("rabbitmq:3.8.26-management") as rabbitmq:
            scd = Mock(data_available=True, temperature=25, relative_humidity=40, CO2=400)
            sgp = Mock()
            sgp.measure_index = Mock(return_value=1)

            host = rabbitmq.get_connection_params().host
            port = rabbitmq.get_connection_params().port
            user = rabbitmq.get_connection_params().credentials.username
            password = rabbitmq.get_connection_params().credentials.password

            worker = Worker(host = host,
                port = port,
                device_id = "test",
                user = user,
                password = password,
                scd = scd, sgp = sgp)

            coros = [worker.main(), self.read_rabbit(host, port, user, password), self.delayed_cancellation(worker, 5)]
            self.tasks = [asyncio.create_task(coro) for coro in coros]
            try:
                await asyncio.gather(*self.tasks, return_exceptions=True)
            except asyncio.exceptions.CancelledError:
                pass

if __name__ == '__main__':
    unittest.main()