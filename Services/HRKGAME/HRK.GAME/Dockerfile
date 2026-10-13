FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Services/HRKGAME/HRK.GAME/HRK.GAME.csproj", "Services/HRKGAME/HRK.GAME/"]
COPY ["CoreEngine/Core.TransactionalMessaging/Core.TransactionalMessaging.csproj", "CoreEngine/Core.TransactionalMessaging/"]
COPY ["CoreEngine/Core.RabbitMQ/Core.RabbitMQ.csproj", "CoreEngine/Core.RabbitMQ/"]
COPY ["CoreEngine/Oservability/Oservability.csproj", "CoreEngine/Oservability/"]
COPY ["Services/HRKGAME/GAME.Application/GAME.Application.csproj", "Services/HRKGAME/GAME.Application/"]
COPY ["Services/HRKGAME/GAME.Domain/GAME.Domain.csproj", "Services/HRKGAME/GAME.Domain/"]
COPY ["CoreEngine/Core.Common/Core.Common.csproj", "CoreEngine/Core.Common/"]
COPY ["CoreEngine/Core.Application/Core.Application.csproj", "CoreEngine/Core.Application/"]
COPY ["CoreEngine/Core.Web/Core.Web.csproj", "CoreEngine/Core.Web/"]
COPY ["CoreEngine/Core.Persistence.Abstractions/Core.Persistence.Abstractions.csproj", "CoreEngine/Core.Persistence.Abstractions/"]
COPY ["Services/HRKGAME/GAME.Infrastructure/GAME.Infrastructure.csproj", "Services/HRKGAME/GAME.Infrastructure/"]
COPY ["CoreEngine/Core.Messaging.Contracts/Core.Messaging.Contracts.csproj", "CoreEngine/Core.Messaging.Contracts/"]
COPY ["CoreEngine/Core.Caching/Core.Caching.csproj", "CoreEngine/Core.Caching/"]
COPY ["CoreEngine/Core.Persistence/Core.Persistence.csproj", "CoreEngine/Core.Persistence/"]
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
