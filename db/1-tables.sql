CREATE TABLE sensor_data_scd30 (
    id SERIAL PRIMARY KEY,
    device_id varchar(32) NOT NULL,
    temperature numeric NOT NULL,
    humidity numeric NOT NULL,
    ppm numeric NOT NULL,
    received_at timestamp with time zone NOT NULL
);

CREATE TABLE sensor_data_sgp40 (
    id SERIAL PRIMARY KEY,
    device_id varchar(32) NOT NULL,
    voc numeric NOT NULL,
    received_at timestamp with time zone NOT NULL
);