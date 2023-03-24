ARG ARCH=amd64
ARG TAG=6.0-bullseye-slim-$ARCH

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /source

# copy csproj and restore as distinct layers
COPY *.sln .
COPY AutoWeaponCalc/*.csproj ./AutoWeaponCalc/
RUN dotnet restore

# copy everything else and build app
COPY AutoWeaponCalc/. ./AutoWeaponCalc/

WORKDIR /source/AutoWeaponCalc

COPY resource/input .
COPY resource/weaponData .
COPY resource/gcsim .

RUN chmod 755 gcsim

RUN dotnet publish -c release -o /app --no-restore