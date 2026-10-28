# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Services/AUTH/HRK.AUTH/HRK.AUTH.csproj", "Services/AUTH/HRK.AUTH/"]
COPY ["CoreEngine/Core.RabbitMQ/Core.RabbitMQ.csproj", "CoreEngine/Core.RabbitMQ/"]
COPY ["CoreEngine/Oservability/Oservability.csproj", "CoreEngine/Oservability/"]
COPY ["CoreEngine/Core.InternalApi/Core.InternalApi.csproj", "CoreEngine/Core.InternalApi/"]
COPY ["CoreEngine/Core.Localization/Core.Localization.csproj", "CoreEngine/Core.Localization/"]
COPY ["CoreEngine/Core.Caching/Core.Caching.csproj", "CoreEngine/Core.Caching/"]
COPY ["Services/AUTH/AUTH.Application/AUTH.Application.csproj", "Services/AUTH/AUTH.Application/"]
COPY ["Services/AUTH/AUTH.Domain/AUTH.Domain.csproj", "Services/AUTH/AUTH.Domain/"]
COPY ["CoreEngine/Core.Common/Core.Common.csproj", "CoreEngine/Core.Common/"]
COPY ["CoreEngine/Core.Application/Core.Application.csproj", "CoreEngine/Core.Application/"]
COPY ["CoreEngine/Core.Web/Core.Web.csproj", "CoreEngine/Core.Web/"]
COPY ["CoreEngine/Core.AUTH/Core.AUTH.csproj", "CoreEngine/Core.AUTH/"]
COPY ["CoreEngine/Core.Persistence.Abstractions/Core.Persistence.Abstractions.csproj", "CoreEngine/Core.Persistence.Abstractions/"]
COPY ["Services/AUTH/AUTH.Infrastructure/AUTH.Infrastructure.csproj", "Services/AUTH/AUTH.Infrastructure/"]
COPY ["CoreEngine/Core.Persistence/Core.Persistence.csproj", "CoreEngine/Core.Persistence/"]
COPY ["CoreEngine/Core.HealthChecks/Core.HealthChecks.csproj", "CoreEngine/Core.HealthChecks/"]
RUN dotnet restore "./Services/AUTH/HRK.AUTH/HRK.AUTH.csproj"
COPY . .
WORKDIR "/src/Services/AUTH/HRK.AUTH"
RUN dotnet build "./HRK.AUTH.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./HRK.AUTH.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HRK.AUTH.dll"]
