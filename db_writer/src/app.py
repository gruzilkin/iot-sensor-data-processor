import psycopg2, pika, json, datetime, sys, os
from retry import retry
from functools import partial

from cachetools import cached, TTLCache

import numpy as np
from sklearn.linear_model import LinearRegression
from sklearn.preprocessing import PolynomialFeatures
from sklearn.preprocessing import StandardScaler
from sklearn.pipeline import make_pipeline

import pickle

def buildCalibrationModel(connection, device_id):
    with connection.cursor() as cur:
        read_calibration_data_command = "SELECT temperature, humidity, r0 " \
        "FROM sensor_calibration_data " \
        "WHERE device_id = %s "
        cur.execute(read_calibration_data_command, (device_id,))
        calibration_data = cur.fetchall()
        calibration_data = np.array(calibration_data, dtype=float)
        train_x = calibration_data[:, :2]
        train_y = calibration_data[:, -1].reshape(-1, 1)

        model = make_pipeline(StandardScaler(), PolynomialFeatures(degree = 2), LinearRegression())
        model.fit(train_x, train_y)

        return model

@cached(cache=TTLCache(maxsize=32, ttl=600), key= lambda connection, device_id: device_id)
def calibrationModel(connection, device_id):
    with connection.cursor() as cur:
        sql_command = "SELECT cm.model " \
        "FROM calibration_models cm " \
        "WHERE cm.device_id = %s AND cm.created_at > ( " \
        "SELECT MAX(c.received_at) " \
        "FROM sensor_calibration_data c " \
        "WHERE c.device_id = %s)"

        cur.execute(sql_command, (device_id, device_id))
        row = cur.fetchone()

        if row is not None:
            return pickle.loads(row[0])
        else:
            model = buildCalibrationModel(connection, device_id)
            serialized_model = pickle.dumps(model)
            save_model_sql_command = "INSERT INTO calibration_models (device_id, model, created_at)" \
                                    "VALUES (%s, %s, now()) " \
                                    "ON CONFLICT(device_id) DO UPDATE SET model=%s, created_at=now();"
            cur.execute(save_model_sql_command, (device_id, serialized_model, serialized_model))
            connection.commit()
            return model        


def calculateR0(connection, device_id, temperature, humidity):
    pipe = calibrationModel(connection, device_id)

    input = np.array([temperature, humidity], dtype=float).reshape(1, -1)
    predicted_r0 = pipe.predict(input).item()

    return predicted_r0


def calibration_request_callback(ch, method, properties, body, args):
    print(f"Received {body}")
    (connection, ) = args

    device_id = method.routing_key.split('.')[-1]
    body_dict = json.loads(body)
    temperature = body_dict['temperature']
    humidity = body_dict['humidity']

    r0 = calculateR0(connection, device_id, temperature, humidity)
    response = json.dumps({'temperature': temperature, 'humidity': humidity, 'r0': r0})
    print(f"Sent {response}")

    ch.basic_publish(exchange='amq.topic',
                     routing_key=f"sensor.calibration.response.{device_id}",
                     body=response)


def rabbit_callback(ch, method, properties, body, args):
    print(f"Received {body}")
    (connection, table_name, body_dict_keys) = args
        
    timestamp = properties.headers['timestamp_in_ms']
    body_dict = json.loads(body)
    
    received_at = datetime.datetime.fromtimestamp(timestamp/1000)
    device_id = method.routing_key.split('.')[-1]

    body_dict_keys = [key for key in body_dict_keys if key in body_dict.keys()]
    data = [body_dict[key] for key in body_dict_keys]

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

    channel.queue_declare(queue='sensor.live.data', durable=True)
    channel.queue_bind(routing_key="sensor.live.data.*", queue='sensor.live.data', exchange="amq.topic")

    channel.queue_declare(queue='sensor.calibration.data', durable=True)
    channel.queue_bind(routing_key="sensor.calibration.data.*", queue='sensor.calibration.data', exchange="amq.topic")

    channel.queue_declare(queue='sensor.calibration.request', durable=True)
    channel.queue_bind(routing_key="sensor.calibration.request.*", queue='sensor.calibration.request', exchange="amq.topic")

    conn = create_connection()
    sensor_callback = partial(rabbit_callback, args=(conn, 'sensor_data', ['temperature', 'humidity', 'ppm']))
    calibration_callback = partial(rabbit_callback, args=(conn, 'sensor_calibration_data', ['temperature', 'humidity', 'r0', 'ppm']))
    partial_calibration_request_callback = partial(calibration_request_callback, args=(conn,))
    channel.basic_consume(queue='sensor.live.data', on_message_callback=sensor_callback, auto_ack=True)
    channel.basic_consume(queue='sensor.calibration.data', on_message_callback=calibration_callback, auto_ack=True)
    channel.basic_consume(queue='sensor.calibration.request', on_message_callback=partial_calibration_request_callback, auto_ack=True)

    try:
        channel.start_consuming()
    finally:
        channel.close()
        conn.close()
    
main()
