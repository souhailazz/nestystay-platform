# Standalone backend image. This file is intentionally self-contained so the
# backend directory can be pushed as its own repository.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY NestyStay.sln ./
COPY src ./src
RUN dotnet restore src/NestyStay.Api/NestyStay.Api.csproj
RUN dotnet publish src/NestyStay.Api/NestyStay.Api.csproj -c Release -o /out --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_EnableDiagnostics=0
COPY --from=build /out .
RUN mkdir -p /var/lib/nestystay/storage && chown -R 10001:10001 /var/lib/nestystay
USER 10001
EXPOSE 8080
ENTRYPOINT ["dotnet", "NestyStay.Api.dll"]
