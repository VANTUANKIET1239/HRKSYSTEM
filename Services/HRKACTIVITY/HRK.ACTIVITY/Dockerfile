FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["Services/HRKACTIVITY/HRK.ACTIVITY/HRK.ACTIVITY.csproj", "Services/HRKACTIVITY/HRK.ACTIVITY/"]
COPY ["Services/HRKACTIVITY/Activity.Infrastructure/Activity.Infrastructure.csproj", "Services/HRKACTIVITY/Activity.Infrastructure/"]
COPY ["Services/HRKACTIVITY/Activity.Application/Activity.Application.csproj", "Services/HRKACTIVITY/Activity.Application/"]
COPY ["Services/HRKACTIVITY/Activity.Domain/Activity.Domain.csproj", "Services/HRKACTIVITY/Activity.Domain/"]
COPY ["CoreEngine/Core.TransactionalMessaging/Core.TransactionalMessaging.csproj", "CoreEngine/Core.TransactionalMessaging/"]
COPY ["CoreEngine/Core.RabbitMQ/Core.RabbitMQ.csproj", "CoreEngine/Core.RabbitMQ/"]
COPY ["CoreEngine/Core.Common/Core.Common.csproj", "CoreEngine/Core.Common/"]
COPY ["CoreEngine/Core.Messaging.Contracts/Core.Messaging.Contracts.csproj", "CoreEngine/Core.Messaging.Contracts/"]
COPY ["CoreEngine/Oservability/Oservability.csproj", "CoreEngine/Oservability/"]
RUN dotnet restore "Services/HRKACTIVITY/HRK.ACTIVITY/HRK.ACTIVITY.csproj"
COPY . .
RUN dotnet publish "Services/HRKACTIVITY/HRK.ACTIVITY/HRK.ACTIVITY.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "HRK.ACTIVITY.dll"]
