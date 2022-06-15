CREATE TABLE sensor_data_scd30 (
    id SERIAL PRIMARY KEY,
    device_id varchar(32) NOT NULL,
    temperature numeric(3, 1) NOT NULL,
    humidity numeric(3, 1) NOT NULL,
    ppm numeric(4) NOT NULL,
    received_at timestamp with time zone NOT NULL
);

CREATE TABLE sensor_data_sgp40 (
    id SERIAL PRIMARY KEY,
    device_id varchar(32) NOT NULL,
    voc numeric(3) NOT NULL,
    received_at timestamp with time zone NOT NULL
);

CREATE TABLE weights_scd30_ppm (
    id integer PRIMARY KEY REFERENCES sensor_data_scd30 ON DELETE CASCADE,
    weight numeric NOT NULL
);

CREATE TABLE weights_scd30_temperature (
    id integer PRIMARY KEY REFERENCES sensor_data_scd30 ON DELETE CASCADE,
    weight numeric NOT NULL
);

CREATE TABLE weights_scd30_humidity (
    id integer PRIMARY KEY REFERENCES sensor_data_scd30 ON DELETE CASCADE,
    weight numeric NOT NULL
);

CREATE TABLE weights_sgp40_voc (
    id integer PRIMARY KEY REFERENCES sensor_data_sgp40 ON DELETE CASCADE,
    weight numeric NOT NULL
);