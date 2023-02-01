import asyncio
import unittest
from unittest.mock import Mock

from testcontainers.rabbitmq import RabbitMqContainer

from app import Worker

class TestApp(unittest.IsolatedAsyncioTestCase):
    async def delayed_worker_cancellation(self, worker, sleep_in_seconds):
        await asyncio.sleep(sleep_in_seconds)
        await worker.cancel()

    async def test_createWorker(self):
        with RabbitMqContainer("rabbitmq:3.8.26-management") as rabbitmq:
            scd = Mock(data_available=False)
            sgp = Mock()
            worker = Worker(host = rabbitmq.get_connection_params().host,
                port = rabbitmq.get_connection_params().port,
                device_id = "test",
                user = rabbitmq.get_connection_params().credentials.username,
                password = rabbitmq.get_connection_params().credentials.password,
                scd = scd, sgp = sgp)


            coros = [worker.main(), self.delayed_worker_cancellation(worker, 10)]
            tasks = [asyncio.create_task(coro) for coro in coros]
            await asyncio.gather(*tasks)
            

if __name__ == '__main__':
    unittest.main()