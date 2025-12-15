# Use Debian Bullseye with OpenSSL configured for TLS 1.0
FROM mcr.microsoft.com/dotnet/sdk:6.0-bullseye-slim AS build
WORKDIR /src
COPY src/ .
RUN dotnet publish TDSProxy/TDSProxy.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/runtime:6.0-bullseye-slim
WORKDIR /app

# Enable TLS 1.0 in OpenSSL config
RUN sed -i 's/MinProtocol = TLSv1.2/MinProtocol = TLSv1/g' /etc/ssl/openssl.cnf && \
    sed -i 's/CipherString = DEFAULT:@SECLEVEL=2/CipherString = DEFAULT:@SECLEVEL=0/g' /etc/ssl/openssl.cnf && \
    echo "Options = UnsafeLegacyRenegotiation" >> /etc/ssl/openssl.cnf

# Create logs directory
RUN mkdir -p /app/logs

COPY --from=build /app .

# Copy config and certificate
COPY appsettings.json .
COPY proxy.pfx .

# Expose TDS proxy port
EXPOSE 1435

ENTRYPOINT ["dotnet", "TDSProxy.dll"]
