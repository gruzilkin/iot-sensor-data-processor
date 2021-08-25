# syntax=docker/dockerfile:1

FROM python:3

WORKDIR /app

COPY requirements.txt requirements.txt
RUN pip3 install -r requirements.txt

COPY ./src/ .

EXPOSE 5678

CMD python3 -m debugpy --listen 0.0.0.0:5678 app.py