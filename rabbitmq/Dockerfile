FROM rabbitmq:3.8-management
RUN rabbitmq-plugins enable rabbitmq_mqtt
COPY ./rabbitmq_message_timestamp-3.8.0.ez /opt/rabbitmq/plugins/
RUN rabbitmq-plugins enable rabbitmq_message_timestamp