import psycopg2, pika, json, datetime, sys, os
from retry import retry
from functools import partial

def rabbit_callback(ch, method, properties, body, args):
    (connection, table_name, body_dict_keys) = args
    
    timestamp = properties.headers['timestamp_in_ms']
    body_dict = json.loads(body)
    
    received_at = datetime.datetime.fromtimestamp(timestamp/1000)
    device_id = method.routing_key.split('.')[1]
    data = [body_dict[key] for key in body_dict_keys]

    cur = connection.cursor()
    sql_args = (device_id, *data, received_at)
    cur.execute(f"INSERT INTO {table_name} (device_id, {','.join(body_dict_keys)}, received_at) VALUES ({','.join(['%s'] *len(sql_args))})", sql_args)
    connection.commit()
    cur.close()

    print(" [x] Received %r" % body)

def create_connection():
    dbname = os.environ['DB_NAME']
    user = os.environ['DB_USER']
    password = os.environ['DB_PASSWORD']
    host = os.environ['DB_HOST']
    return psycopg2.connect(dbname=dbname, user=user, password=password, host=host)

@retry(pika.exceptions.AMQPConnectionError, delay=10, tries=3)
def main():
    host = os.environ['RABBITMQ_HOST']
    print("Pika connecting to %s" % host)
    connection = pika.BlockingConnection(pika.ConnectionParameters(host=host))
    print("Pika connected to %s" % host)

    channel = connection.channel()

    channel.queue_declare(queue='temperature', durable=True)
    channel.queue_bind(routing_key="temperature.*", queue='temperature', exchange="amq.topic")

    channel.queue_declare(queue='humidity', durable=True)
    channel.queue_bind(routing_key="humidity.*", queue='humidity', exchange="amq.topic")

    channel.queue_declare(queue='calibration', durable=True)
    channel.queue_bind(routing_key="calibration.*", queue='calibration', exchange="amq.topic")

    conn = create_connection()
    temperature_callback = partial(rabbit_callback, args=(conn, 'temperature', ['temperature']))
    humidity_callback = partial(rabbit_callback, args=(conn, 'humidity', ['humidity']))
    calibration_callback = partial(rabbit_callback, args=(conn, 'calibration', ['temperature', 'humidity', 'r0', 'ppm']))
    channel.basic_consume(queue='temperature', on_message_callback=temperature_callback, auto_ack=True)
    channel.basic_consume(queue='humidity', on_message_callback=humidity_callback, auto_ack=True)
    channel.basic_consume(queue='calibration', on_message_callback=calibration_callback, auto_ack=True)

    try:
        channel.start_consuming()
    finally:
        channel.close()
        conn.close()
    
main()
