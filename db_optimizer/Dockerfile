# syntax=docker/dockerfile:1

FROM python:3.7-buster

WORKDIR /app

RUN apt update && apt install -y libatlas-base-dev && rm -rf /var/lib/apt/lists/*

COPY requirements.txt requirements.txt
RUN pip install -r requirements.txt --no-cache-dir --index-url=https://www.piwheels.org/simple

COPY wait-for-it.sh .
RUN chmod a+x wait-for-it.sh

COPY ./src/ .

EXPOSE 5678

CMD ./wait-for-it.sh -t 600 $RABBITMQ_HOST:5672 && ./wait-for-it.sh -t 600 $DB_HOST:5432 && python3 -m debugpy --listen 0.0.0.0:5678 app.py