import psycopg2, pika, json, datetime, sys, os
from retry import retry
from functools import partial

import numpy as np
from sklearn.linear_model import LinearRegression
from sklearn.preprocessing import PolynomialFeatures
from sklearn.preprocessing import StandardScaler
from sklearn.pipeline import make_pipeline

import pickle

def buildCalibrationModel(connection, device_id):
    cur = connection.cursor()
    read_calibration_data_command = "SELECT temperature, humidity, r0 " \
    "FROM calibration " \
    "WHERE device_id = %s "
    cur.execute(read_calibration_data_command, (device_id,))
    calibration_data = cur.fetchall()
    calibration_data = np.array(calibration_data, dtype=float)
    train_x = calibration_data[:, :2]
    train_y = calibration_data[:, -1].reshape(-1, 1)

    model = make_pipeline(StandardScaler(), PolynomialFeatures(degree = 2), LinearRegression())
    model.fit(train_x, train_y)

    return model

def calibrationModel(connection, device_id):
    cur = connection.cursor()
    
    sql_command = "SELECT cm.model " \
    "FROM calibration_models cm " \
    "WHERE cm.device_id = %s AND cm.created_at > ( " \
	"SELECT MAX(c.received_at) " \
	"FROM calibration c " \
	"WHERE c.device_id = %s)"

    cur.execute(sql_command, (device_id, device_id))
    row = cur.fetchone()
    cur.close()

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
        cur.close()
        return model        


def calculateR0(connection, device_id, temperature, humidity):
    pipe = calibrationModel(connection, device_id)

    input = np.array([temperature, humidity], dtype=float).reshape(1, -1)
    predicted_r0 = pipe.predict(input).item()

    return predicted_r0


def calibration_request_callback(ch, method, properties, body, args):
    (connection, ) = args
    
    print(" [x] Received %r" % body)

    device_id = method.routing_key.split('.')[-1]
    body_dict = json.loads(body)
    temperature = body_dict['temperature']
    humidity = body_dict['humidity']

    r0 = calculateR0(connection, device_id, temperature, humidity)
    response = json.dumps({'r0': r0})

    ch.basic_publish(exchange='amq.topic',
                     routing_key=f"calibration.response.{device_id}",
                     body=response)


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

    channel.queue_declare(queue='calibration.request', durable=True)
    channel.queue_bind(routing_key="calibration.request.*", queue='calibration.request', exchange="amq.topic")

    conn = create_connection()
    temperature_callback = partial(rabbit_callback, args=(conn, 'temperature', ['temperature']))
    humidity_callback = partial(rabbit_callback, args=(conn, 'humidity', ['humidity']))
    calibration_callback = partial(rabbit_callback, args=(conn, 'calibration', ['temperature', 'humidity', 'r0', 'ppm']))
    partial_calibration_request_callback = partial(calibration_request_callback, args=(conn,))
    channel.basic_consume(queue='temperature', on_message_callback=temperature_callback, auto_ack=True)
    channel.basic_consume(queue='humidity', on_message_callback=humidity_callback, auto_ack=True)
    channel.basic_consume(queue='calibration', on_message_callback=calibration_callback, auto_ack=True)
    channel.basic_consume(queue='calibration.request', on_message_callback=partial_calibration_request_callback, auto_ack=True)

    try:
        channel.start_consuming()
    finally:
        channel.close()
        conn.close()
    
main()
