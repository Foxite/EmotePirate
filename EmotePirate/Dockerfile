﻿FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["EmotePirate.csproj", "./"]
RUN dotnet restore "EmotePirate.csproj"
COPY . .
WORKDIR "/src"
RUN dotnet build "EmotePirate.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EmotePirate.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["./EmotePirate"]
