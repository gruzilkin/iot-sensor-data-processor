import psycopg2, pika, json, sys, os
from datetime import datetime
from retry import retry
from functools import partial

def rabbit_callback(ch, method, properties, body, connection, table_name, body_dict_keys, body_dict_parsers={}):
    print(f"Received {body}")
    timestamp = properties.headers['timestamp_in_ms']
    body_dict = json.loads(body)
    
    received_at = datetime.fromtimestamp(timestamp/1000)
    device_id = method.routing_key.split('.')[-1]

    body_dict_keys = [key for key in body_dict_keys if key in body_dict.keys()]
    data = [body_dict_parsers[key](body_dict[key]) if key in body_dict_parsers.keys() else body_dict[key] for key in body_dict_keys]

    cur = connection.cursor()
    sql_args = (device_id, *data, received_at)
    cur.execute(f"INSERT INTO {table_name} (device_id, {','.join(body_dict_keys)}, received_at) VALUES ({','.join(['%s'] *len(sql_args))})", sql_args)
    connection.commit()
    cur.close()

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

    channel = connection.channel()

    channel.queue_declare(queue='sensor.data', durable=True)
    channel.queue_bind(routing_key="sensor.data.*", queue='sensor.data', exchange="amq.topic")

    conn = create_connection()
    sensor_callback = partial(rabbit_callback, connection=conn, table_name='sensor_data',
        body_dict_keys=['temperature', 'humidity', 'ppm'])

    channel.basic_consume(queue='sensor.data', on_message_callback=sensor_callback, auto_ack=True)

    try:
        channel.start_consuming()
    finally:
        channel.close()
        conn.close()
    
main()
