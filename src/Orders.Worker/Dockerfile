# Build from the repository root:  docker build -f src/Orders.Worker/Dockerfile -t orders-worker:local .
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/Orders.Domain/Orders.Domain.csproj src/Orders.Domain/
COPY src/Orders.Infrastructure/Orders.Infrastructure.csproj src/Orders.Infrastructure/
COPY src/Orders.Worker/Orders.Worker.csproj src/Orders.Worker/
RUN dotnet restore src/Orders.Worker/Orders.Worker.csproj

COPY src/ src/
RUN dotnet publish src/Orders.Worker/Orders.Worker.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
ENTRYPOINT ["dotnet", "Orders.Worker.dll"]
