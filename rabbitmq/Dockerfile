FROM rabbitmq:3.8.26-management
COPY ./rabbitmq_message_timestamp-3.8.0.ez /opt/rabbitmq/plugins/
COPY ./rabbitmq/rabbitmq.conf /etc/rabbitmq/
RUN rabbitmq-plugins enable rabbitmq_message_timestamp
