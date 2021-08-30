CREATE TABLE temperature (id SERIAL PRIMARY KEY, device_id varchar(32) NOT NULL, temperature numeric NOT NULL, received_at timestamp NOT NULL);
CREATE TABLE humidity (id SERIAL PRIMARY KEY, device_id varchar(32) NOT NULL, humidity numeric NOT NULL, received_at timestamp NOT NULL);
CREATE TABLE calibration (
    id SERIAL PRIMARY KEY,
    device_id varchar(32) NOT NULL,
    temperature numeric NOT NULL,
    humidity numeric NOT NULL,
    r0 numeric NOT NULL,
    ppm numeric NOT NULL,
    received_at timestamp NOT NULL
);