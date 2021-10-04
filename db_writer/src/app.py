import psycopg2, pika, json, sys, os
from datetime import datetime
from retry import retry
from functools import partial

def rabbit_callback(ch, method, properties, body, connection, table, body_dict_keys = []):
    print(f"Received {body}")
    timestamp = properties.headers['timestamp_in_ms']
    body_dict = json.loads(body)
    
    received_at = datetime.fromtimestamp(timestamp/1000)
    device_id = method.routing_key.split('.')[-1]

    data = [body_dict[key] for key in body_dict_keys]

    with connection.cursor() as cur:
        sql_args = (device_id, *data, received_at)
        cur.execute(f"INSERT INTO {table} (device_id, {','.join(body_dict_keys)}, received_at) VALUES ({','.join(['%s'] * len(sql_args))})", sql_args)
        connection.commit()

def create_connection():
    dbname = os.environ['DB_NAME']
    user = os.environ['DB_USER']
    password = os.environ['DB_PASSWORD']
    host = os.environ['DB_HOST']
    return psycopg2.connect(dbname=dbname, user=user, password=password, host=host)

@retry(pika.exceptions.AMQPConnectionError, delay=10, tries=3)
def main():
    connectionArgs = dict()
    user = os.environ['RABBITMQ_USER']
    password = os.environ['RABBITMQ_PASS']
    if user and password:
        connectionArgs['credentials'] = pika.PlainCredentials(user, password)

    host = os.environ['RABBITMQ_HOST']
    print(f"Pika connecting to {host}")
    connection = pika.BlockingConnection(pika.ConnectionParameters(host=host, **connectionArgs))
    print(f"Pika connected to {host}")

    with connection.channel() as channel:
        channel.queue_declare(queue='sensor.scd30', durable=True)
        channel.queue_bind(routing_key="sensor.scd30.*", queue='sensor.scd30', exchange="amq.topic")

        channel.queue_declare(queue='sensor.sgp40', durable=True)
        channel.queue_bind(routing_key="sensor.sgp40.*", queue='sensor.sgp40', exchange="amq.topic")

        with create_connection() as conn:
            scd30_callback = partial(rabbit_callback, connection=conn, table='sensor_data_scd30', body_dict_keys=['temperature', 'humidity', 'ppm'])
            sgp40_callback = partial(rabbit_callback, connection=conn, table='sensor_data_sgp40', body_dict_keys=['voc'])
            channel.basic_consume(queue='sensor.scd30', on_message_callback=scd30_callback, auto_ack=True)
            channel.basic_consume(queue='sensor.sgp40', on_message_callback=sgp40_callback, auto_ack=True)
            channel.start_consuming()
    
main()
