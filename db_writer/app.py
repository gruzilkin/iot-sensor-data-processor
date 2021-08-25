import pika, sys, os
from retry import retry

@retry(pika.exceptions.AMQPConnectionError, delay=10, tries=30)
def main():
    host = os.environ['RABBITMQ_HOST']
    print("Pika connecting to %s" % host)
    connection = pika.BlockingConnection(pika.ConnectionParameters(host=host))
    print("Pika connected to %s" % host)

    channel = connection.channel()

    channel.queue_declare(queue='Temperature', durable=True)
    channel.queue_bind(queue='Temperature', exchange="amq.topic")

    channel.queue_declare(queue='Humidity', durable=True)
    channel.queue_bind(queue='Humidity', exchange="amq.topic")

    def callback(ch, method, properties, body):
        print(" [x] Received %r" % body)

    channel.basic_consume( queue='Temperature', on_message_callback=callback, auto_ack=True)
    channel.basic_consume(queue='Humidity', on_message_callback=callback, auto_ack=True)

    channel.start_consuming()
    
main()