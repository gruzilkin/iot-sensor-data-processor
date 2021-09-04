CREATE TABLE sensor_data (
    id SERIAL PRIMARY KEY,
    device_id varchar(32) NOT NULL,
    temperature numeric NOT NULL,
    humidity numeric NOT NULL,
    ppm numeric,
    received_at timestamp NOT NULL
);

CREATE TABLE sensor_calibration_data (
    id SERIAL PRIMARY KEY,
    device_id varchar(32) NOT NULL,
    temperature numeric NOT NULL,
    humidity numeric NOT NULL,
    r0 numeric NOT NULL,
    ppm numeric NOT NULL,
    uptime interval NOT NULL,
    received_at timestamp NOT NULL,
    is_outlier boolean DEFAULT false
);

CREATE TABLE calibration_models (device_id varchar(32) PRIMARY KEY, model bytea NOT NULL, created_at timestamp NOT NULL);