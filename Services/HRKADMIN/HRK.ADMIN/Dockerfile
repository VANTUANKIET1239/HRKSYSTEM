FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY CoreEngine/ CoreEngine/
# Include API, Application and Infrastructure before restoring project references.
COPY Services/HRKADMIN/ Services/HRKADMIN/
RUN dotnet restore Services/HRKADMIN/HRK.ADMIN/HRK.ADMIN.csproj
RUN dotnet publish Services/HRKADMIN/HRK.ADMIN/HRK.ADMIN.csproj -c Release -o /app/publish /p:UseAppHost=false --no-restore

FROM base AS final
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "HRK.ADMIN.dll"]
