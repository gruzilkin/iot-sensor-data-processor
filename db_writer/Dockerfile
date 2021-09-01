# syntax=docker/dockerfile:1

FROM python:3.7-buster

WORKDIR /app

COPY dist-packages.pth /usr/local/lib/python3.7/site-packages/

RUN apt-get update && apt-get install -y \
  python3-sklearn \
  python3-sklearn-lib \
  && rm -rf /var/lib/apt/lists/*

COPY requirements.txt requirements.txt
RUN pip3 install -r requirements.txt

COPY ./src/ .

EXPOSE 5678

CMD python3 -m debugpy --listen 0.0.0.0:5678 app.py