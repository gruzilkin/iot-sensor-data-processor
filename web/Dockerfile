# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build-env
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY *.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY ./ ./
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:7.0

WORKDIR /app

COPY wait-for-it.sh .
RUN chmod a+x wait-for-it.sh

COPY --from=build-env /app/out .

CMD ./wait-for-it.sh -t 600 $RABBITMQ_HOST:5672 && dotnet web.dll