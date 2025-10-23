FROM mcr.microsoft.com/dotnet/sdk:8.0

WORKDIR /app

ENV SERVER_HOSTNAME=server
ENV SERVER_PORT=5000
ENV RECEIVE_DIRECTORY=Files

COPY Server/*.csproj ./Server/
COPY Common/*.csproj ./Common/

RUN dotnet restore ./Server

COPY Server/. ./Server
COPY Common/. ./Common

RUN dotnet publish ./Server/Server.csproj -c Release -o /app/publish

ENTRYPOINT ["dotnet", "/app/publish/Server.dll"]