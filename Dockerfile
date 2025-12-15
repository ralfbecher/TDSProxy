# Use Debian Buster with OpenSSL 1.1 and TLS 1.0 enabled
FROM mcr.microsoft.com/dotnet/sdk:6.0-buster-slim AS build
WORKDIR /src
COPY src/ .
RUN dotnet publish TDSProxy/TDSProxy.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/runtime:6.0-buster-slim
WORKDIR /app

# Enable TLS 1.0 in OpenSSL config
RUN sed -i 's/MinProtocol = TLSv1.2/MinProtocol = TLSv1/' /etc/ssl/openssl.cnf || true && \
    sed -i 's/SECLEVEL=2/SECLEVEL=0/' /etc/ssl/openssl.cnf || true

# Create logs directory
RUN mkdir -p /app/logs

COPY --from=build /app .

# Copy config and certificate (alternatively mount at runtime with -v)
COPY appsettings.json .
COPY proxy.pfx .

# Expose TDS proxy port
EXPOSE 1435

ENTRYPOINT ["dotnet", "TDSProxy.dll"]
