CREATE TABLE temperature(id SERIAL PRIMARY KEY, device_id varchar(32) NOT NULL, value numeric NOT NULL, received_at timestamp NOT NULL);
CREATE TABLE humidity(id SERIAL PRIMARY KEY, device_id varchar(32) NOT NULL, value numeric NOT NULL, received_at timestamp NOT NULL);