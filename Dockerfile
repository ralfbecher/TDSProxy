# Use Debian Bullseye with OpenSSL configured for TLS 1.0
FROM mcr.microsoft.com/dotnet/sdk:6.0-bullseye-slim AS build
WORKDIR /src
COPY src/ .
RUN dotnet publish TDSProxy/TDSProxy.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/runtime:6.0-bullseye-slim
WORKDIR /app

# Create custom OpenSSL config that enables TLS 1.0
RUN echo 'openssl_conf = openssl_init\n\
[openssl_init]\n\
ssl_conf = ssl_sect\n\
[ssl_sect]\n\
system_default = system_default_sect\n\
[system_default_sect]\n\
MinProtocol = TLSv1\n\
CipherString = DEFAULT:@SECLEVEL=0' > /etc/ssl/openssl-tls1.cnf

# Set environment to use custom OpenSSL config
ENV OPENSSL_CONF=/etc/ssl/openssl-tls1.cnf

# Create logs directory
RUN mkdir -p /app/logs

COPY --from=build /app .

# Copy config and certificate
COPY appsettings.json .
COPY proxy.pfx .

# Expose TDS proxy port
EXPOSE 1435

ENTRYPOINT ["dotnet", "TDSProxy.dll"]
