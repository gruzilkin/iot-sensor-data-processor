CREATE TABLE temperature(id serial, device_id varchar(32), value numeric, received_at timestamp);
CREATE TABLE humidity(id serial, device_id varchar(32), value numeric, received_at timestamp);