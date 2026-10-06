FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Services/HRKREALTIME/HRK.REALTIME/HRK.REALTIME.csproj", "Services/HRKREALTIME/HRK.REALTIME/"]
COPY ["CoreEngine/Core.RabbitMQ/Core.RabbitMQ.csproj", "CoreEngine/Core.RabbitMQ/"]
COPY ["CoreEngine/Core.Common/Core.Common.csproj", "CoreEngine/Core.Common/"]
COPY ["CoreEngine/Core.Messaging.Contracts/Core.Messaging.Contracts.csproj", "CoreEngine/Core.Messaging.Contracts/"]
COPY ["CoreEngine/Oservability/Oservability.csproj", "CoreEngine/Oservability/"]
RUN dotnet restore "./Services/HRKREALTIME/HRK.REALTIME/HRK.REALTIME.csproj"
COPY . .
WORKDIR "/src/Services/HRKREALTIME/HRK.REALTIME"
RUN dotnet build "./HRK.REALTIME.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./HRK.REALTIME.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HRK.REALTIME.dll"]
