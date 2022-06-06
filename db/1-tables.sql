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

CREATE TABLE weights_scd30_ppm (
    id integer NOT NULL REFERENCES sensor_data_scd30 ON DELETE CASCADE,
    weight numeric NOT NULL
);

CREATE TABLE weights_scd30_temperature (
    id integer NOT NULL REFERENCES sensor_data_scd30 ON DELETE CASCADE,
    weight numeric NOT NULL
);

CREATE TABLE weights_scd30_humidity (
    id integer NOT NULL REFERENCES sensor_data_scd30 ON DELETE CASCADE,
    weight numeric NOT NULL
);

CREATE TABLE weights_sgp40_voc (
    id integer NOT NULL REFERENCES sensor_data_sgp40 ON DELETE CASCADE,
    weight numeric NOT NULL
);