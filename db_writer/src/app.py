import psycopg2, pika, json, sys, os
from datetime import datetime
from retry import retry
from functools import partial

def rabbit_callback(ch, method, properties, body, connection):
    print(f"Received {body}")
    timestamp = properties.headers['timestamp_in_ms']
    body_dict = json.loads(body)
    
    received_at = datetime.fromtimestamp(timestamp/1000)
    device_id = method.routing_key.split('.')[-1]

    body_dict_keys=['temperature', 'humidity', 'ppm']
    data = [body_dict[key] for key in body_dict_keys]

    with connection.cursor() as cur:
        sql_args = (device_id, *data, received_at)
        cur.execute(f"INSERT INTO sensor_data (device_id, {','.join(body_dict_keys)}, received_at) VALUES ({','.join(['%s'] * len(sql_args))})", sql_args)
        connection.commit()

def create_connection():
    dbname = os.environ['DB_NAME']
    user = os.environ['DB_USER']
    password = os.environ['DB_PASSWORD']
    host = os.environ['DB_HOST']
    return psycopg2.connect(dbname=dbname, user=user, password=password, host=host)

@retry(pika.exceptions.AMQPConnectionError, delay=10, tries=3)
def main():
    host = os.environ['RABBITMQ_HOST']
    print(f"Pika connecting to {host}")
    connection = pika.BlockingConnection(pika.ConnectionParameters(host=host))
    print(f"Pika connected to {host}")

    with connection.channel() as channel:
        channel.queue_declare(queue='sensor.data', durable=True)
        channel.queue_bind(routing_key="sensor.data.*", queue='sensor.data', exchange="amq.topic")

        with create_connection() as conn:
            sensor_callback = partial(rabbit_callback, connection=conn)
            channel.basic_consume(queue='sensor.data', on_message_callback=sensor_callback, auto_ack=True)
            channel.start_consuming()
    
main()
