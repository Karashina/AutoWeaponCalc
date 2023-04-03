ARG ARCH=amd64
ARG TAG=6.0-bullseye-slim-$ARCH

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /app

# copy csproj and restore as distinct layers
COPY *.sln .
COPY src/*.csproj ./src/
RUN dotnet restore

# copy everything else and build app
COPY src/. ./src/

COPY resource/ ./resource/
RUN chmod 755 ./resource/execBinary/gcsim
WORKDIR /app/src

RUN dotnet publish -c release -o /app/bin --no-restore