CREATE INDEX temperature_idx_1 ON temperature (device_id, received_at);
CREATE INDEX humidity_idx_1 ON humidity (device_id, received_at);
CREATE INDEX calibration_idx_1 ON calibration (device_id, received_at);