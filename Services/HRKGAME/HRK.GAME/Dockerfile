FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Services/HRKGAME/HRK.GAME/HRK.GAME.csproj", "Services/HRKGAME/HRK.GAME/"]
COPY ["Services/HRKGAME/GAME.Application/GAME.Application.csproj", "Services/HRKGAME/GAME.Application/"]
COPY ["Services/HRKGAME/GAME.Infrastructure/GAME.Infrastructure.csproj", "Services/HRKGAME/GAME.Infrastructure/"]
COPY ["Services/HRKGAME/GAME.Domain/GAME.Domain.csproj", "Services/HRKGAME/GAME.Domain/"]
COPY ["CoreEngine/Core.Common/Core.Common.csproj", "CoreEngine/Core.Common/"]
COPY ["CoreEngine/Core.Messaging.Contracts/Core.Messaging.Contracts.csproj", "CoreEngine/Core.Messaging.Contracts/"]
COPY ["CoreEngine/Core.RabbitMQ/Core.RabbitMQ.csproj", "CoreEngine/Core.RabbitMQ/"]
COPY ["CoreEngine/Core.TransactionalMessaging/Core.TransactionalMessaging.csproj", "CoreEngine/Core.TransactionalMessaging/"]
COPY ["CoreEngine/Oservability/Oservability.csproj", "CoreEngine/Oservability/"]
RUN dotnet restore "./Services/HRKGAME/HRK.GAME/HRK.GAME.csproj"
COPY . .
WORKDIR "/src/Services/HRKGAME/HRK.GAME"
RUN dotnet build "./HRK.GAME.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./HRK.GAME.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HRK.GAME.dll"]
