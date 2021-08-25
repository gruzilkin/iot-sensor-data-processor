import psycopg2, pika, json, datetime, sys, os
from retry import retry
from functools import partial

def temperature_callback(ch, method, properties, body, connection):
    timestamp = properties.headers['timestamp_in_ms']
    body_dict = json.loads(body)
    
    received_at = datetime.datetime.fromtimestamp(timestamp/1000)
    device = body_dict['device']
    temperature = body_dict['temperature']

    args = (device, temperature, received_at)

    cur = connection.cursor()
    cur.execute("INSERT INTO temperature (device_id, value, received_at) VALUES (%s, %s, %s)", args)
    connection.commit()
    cur.close()

    print(" [x] Received %r" % body)

def humidity_callback(ch, method, properties, body, connection):
    timestamp = properties.headers['timestamp_in_ms']
    body_dict = json.loads(body)
    
    received_at = datetime.datetime.fromtimestamp(timestamp/1000)
    device = body_dict['device']
    humidity = body_dict['humidity']

    args = (device, humidity, received_at)

    cur = connection.cursor()
    cur.execute("INSERT INTO humidity (device_id, value, received_at) VALUES (%s, %s, %s)", args)
    connection.commit()
    cur.close()

    print(" [x] Received %r" % body)

def create_connection():
    dbname = os.environ['DB_NAME']
    user = os.environ['DB_USER']
    password = os.environ['DB_PASSWORD']
    host = os.environ['DB_HOST']
    return psycopg2.connect(dbname=dbname, user=user, password=password, host=host)

@retry(pika.exceptions.AMQPConnectionError, delay=10, tries=30)
def main():
    host = os.environ['RABBITMQ_HOST']
    print("Pika connecting to %s" % host)
    connection = pika.BlockingConnection(pika.ConnectionParameters(host=host))
    print("Pika connected to %s" % host)

    channel = connection.channel()

    channel.queue_declare(queue='Temperature', durable=True)
    channel.queue_bind(queue='Temperature', exchange="amq.topic")

    channel.queue_declare(queue='Humidity', durable=True)
    channel.queue_bind(queue='Humidity', exchange="amq.topic")

    conn = create_connection()
    channel.basic_consume( queue='Temperature', on_message_callback=partial(temperature_callback, connection=conn), auto_ack=True)
    channel.basic_consume(queue='Humidity', on_message_callback=partial(humidity_callback, connection=conn), auto_ack=True)

    channel.start_consuming()
    
main()
