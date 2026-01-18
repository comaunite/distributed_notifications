FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["NotificationWorker.Push/NotificationWorker.Push.csproj", "NotificationWorker.Push/"]
RUN dotnet restore "NotificationWorker.Push/NotificationWorker.Push.csproj"
COPY . .
WORKDIR "/src/NotificationWorker.Push"
RUN dotnet build "./NotificationWorker.Push.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./NotificationWorker.Push.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NotificationWorker.Push.dll"]
