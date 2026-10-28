FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY CoreEngine/ CoreEngine/
COPY Services/HRKLANGUAGE/ Services/HRKLANGUAGE/
RUN dotnet restore Services/HRKLANGUAGE/HRK.LANGUAGE/HRK.LANGUAGE.csproj
RUN dotnet publish Services/HRKLANGUAGE/HRK.LANGUAGE/HRK.LANGUAGE.csproj -c Release -o /app/publish /p:UseAppHost=false --no-restore

FROM base AS final
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "HRK.LANGUAGE.dll"]
