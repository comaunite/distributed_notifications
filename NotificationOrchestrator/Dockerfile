FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
USER $APP_UID
WORKDIR /app

# Install Kerberos / GSSAPI (required for libgssapi_krb5.so.2)
RUN apt-get update && \
    apt-get install -y libkrb5-3 && \
    rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["NotificationOrchestrator/NotificationOrchestrator.csproj", "NotificationOrchestrator/"]
RUN dotnet restore "NotificationOrchestrator/NotificationOrchestrator.csproj"
COPY . .
WORKDIR "/src/NotificationOrchestrator"
RUN dotnet build "./NotificationOrchestrator.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./NotificationOrchestrator.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NotificationOrchestrator.dll"]
