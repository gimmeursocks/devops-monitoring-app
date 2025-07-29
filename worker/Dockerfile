FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine AS builder

WORKDIR /src

COPY Worker.csproj ./

RUN dotnet restore "./Worker.csproj" --runtime alpine-x64

COPY . .

RUN dotnet publish "Worker.csproj" -c Release -o /app/publish \
  --no-restore \
  --runtime alpine-x64 \
  --self-contained true \
  /p:PublishTrimmed=true \
  /p:PublishSingleFile=true

FROM mcr.microsoft.com/dotnet/aspnet:6.0-alpine

RUN adduser --disabled-password \
  --home /app \
  --gecos '' dotnetuser && chown -R dotnetuser /app

USER dotnetuser
WORKDIR /app

COPY --from=builder /app/publish .

ENTRYPOINT ["./Worker"]