import os, asyncio
import psycopg2, psycopg2.extras

import numpy as np
import pandas as pd

import heapq as hq

import time

def create_connection():
    dbname = os.environ['DB_NAME']
    user = os.environ['DB_USER']
    password = os.environ['DB_PASSWORD']
    host = os.environ['DB_HOST']
    return psycopg2.connect(dbname=dbname, user=user, password=password, host=host)


def fetch_devices(connection, sensor):
	with connection.cursor() as cursor:
		cursor.execute(f"""SELECT DISTINCT device_id FROM sensor_data_{sensor}""")
		return [row[0] for row in cursor.fetchall()]


def fetch(connection, sensor, signal, device_id, include_timestamp=False):
	with connection.cursor() as cursor:
		sql = f"""SELECT id, {signal} as signal {', received_at ' if include_timestamp else ''} 
			FROM sensor_data_{sensor}
			WHERE device_id = %s AND received_at > now() - interval '1 day'
			ORDER BY received_at ASC"""
		cursor.execute(sql, (device_id,))
		
		columns = ['id', 'signal']
		if include_timestamp:
			columns.append('received_at')
		return pd.DataFrame.from_records(cursor.fetchall(), index=['id'], columns=columns)


def remove_old_data(connection, sensor):
	with connection.cursor() as cursor:
		cursor.execute(f"""DELETE FROM sensor_data_{sensor}
		WHERE received_at < now() - interval '1 day'""")


def truncate_weights(connection, sensor, signal):
	with connection.cursor() as cursor:
		cursor.execute(f"""TRUNCATE weights_{sensor}_{signal}""")


def update_weight(connection, sensor, signal, series):
	with connection.cursor() as cursor:
		sql = f"""INSERT INTO weights_{sensor}_{signal} (id, weight) VALUES %s"""
		data = [(id, weight) for id, weight in series.items()]
		psycopg2.extras.execute_values(cursor, sql, data)


def calculate_weights(data, ratio = 1):
    y = data
    y = (y - y.mean()) / y.std()
    x = np.arange(len(y))
    indeces = {0:0, len(y)-1:0}

    processed = 2
    limit = max(10, int(len(data) * ratio))

    queue = []
    hq.heappush(queue, (0, (0, len(y)-1)))

    while queue and processed < limit:
        _, (left, right) = hq.heappop(queue)

        if right - left == 1:
            continue

        y_range = y[left:right + 1]
        x_range = x[left:right + 1]
        
        x1, y1, x2, y2 = x_range[0], y_range[0], x_range[-1], y_range[-1]
        a = (y2 - y1) / (x2 - x1)
        b = -x1 * (y2 - y1) / (x2 - x1) + y1
        y_hat = a*x_range + b
        diff = np.abs(y_range - y_hat)
        diff = diff[1:-1]

        i = np.argmax(diff)
        error = diff[i]
        i += left + 1

        indeces[i] = error
        hq.heappush(queue, (-error, (left, i)))
        hq.heappush(queue, (-error, (i, right)))
        processed += 1 

    indeces = dict(sorted(indeces.items(), key=lambda item: item[0]))
    return np.array([indeces[x] if x in indeces.keys() else 0 for x in x])


def calculate_weights_for_series(series, ratio=0.1):
    data = series.to_numpy()
    start = time.time()
    weight = calculate_weights(data, ratio)
    end = time.time()
    print("weight calculation took ", end-start)
    
    weight = (weight - weight.min()) / weight.ptp()
    weight[0] = 1
    weight[-1] = 1

    return pd.Series(index=series.index, data=weight)


def process_weights():
    sensors = {'scd30': ['ppm', 'temperature', 'humidity'],'sgp40': ['voc']}
    with create_connection() as connection:
        for sensor, signals in sensors.items():
            remove_old_data(connection, sensor)
            devices = fetch_devices(connection, sensor)
            for signal in signals:
                truncate_weights(connection, sensor, signal)
                for device in devices:
                    df = fetch(connection, sensor, signal, device)
                    df.signal = df.signal.astype(float)
                    
                    weights = calculate_weights_for_series(df.signal)
                    weights = weights[weights > 0]
                    update_weight(connection, sensor, signal, weights)
        connection.commit()

async def calculate_weights_worker():
    while True:
        process_weights()
        await asyncio.sleep(60)

async def main():
    try:
        coros = [calculate_weights_worker()]
        tasks = [asyncio.create_task(coro) for coro in coros]
        await asyncio.gather(*tasks)
    finally:
        for task in tasks:
            task.cancel()
        await asyncio.gather(*tasks, return_exceptions=True)

asyncio.run(main())