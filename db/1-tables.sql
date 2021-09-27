CREATE TABLE sensor_data (
    id SERIAL PRIMARY KEY,
    device_id varchar(32) NOT NULL,
    temperature numeric NOT NULL,
    humidity numeric NOT NULL,
    ppm numeric NOT NULL,
    received_at timestamp NOT NULL
);